# POS Sequence / Voucher Allocator Contention Investigation - 2026-05-14

## Scope

Project: `F:\Source Code\DynamicErp`

Area: `Areas\Pos` only, with inspection of shared SQL objects used by POS save.

Goal: prove or disprove whether production POS deadlocks/freezes are mainly caused by centralized allocation around `Transaction_ID`, `NoteSerial`, voucher coding, `GetNextID_FromSequence`, or `POS_DEVSerialAllocator`.

## Exact Save Path

UI save path:

`Areas/Pos/Scripts/pos-transaction.js`
-> `Areas/Pos/Controllers/PosTransactionController.cs`
-> `Areas/Pos/Data/PosSqlRepository.cs`
-> `dbo.usp_POS_SaveTransaction`

SQL allocation path inside `dbo.usp_POS_SaveTransaction`:

1. `dbo.GetNextID_FromSequence('Transactions', 'Transaction_ID')`
2. `dbo.usp_Voucher_coding_V2` for POS invoice voucher coding, `Transaction_Type = 21`, `Sanad_No = 7`
3. `dbo.GetNextID_FromSequence('Notes', 'NoteID')`
4. `dbo.usp_Notes_coding_V2`
5. `dbo.GetNextID_FromSequence('DOUBLE_ENTREY_VOUCHERS', 'Double_Entry_Vouchers_ID')`
6. `dbo.POS_DEVSerialAllocator` update by `SerialDate`
7. `INSERT dbo.DOUBLE_ENTREY_VOUCHERS`
8. Card/stock issue path, when applicable:
   - `dbo.GetNextID_FromSequence('Transactions', 'Transaction_ID')`
   - `dbo.GetNextID_FromSequence('Notes', 'NoteID')`
   - `dbo.usp_Notes_coding_V2`
   - `dbo.usp_Voucher_coding_V2`, `Transaction_Type = 19`, `Sanad_No = 10`

## Allocator Definition Findings

`dbo.GetNextID_FromSequence` uses:

- `sys.sp_getapplock`
- `@LockMode = 'Exclusive'`
- `@LockOwner = 'Session'`
- resource name: `GetNextID_FromSequence:dbo.<TableName>.<FieldName>`
- SQL Server sequence object named `dbo.seq_<TableName>_<FieldName>`
- `NO CACHE`
- on first create or drift repair: `MAX(...)` over the target table with `HOLDLOCK, UPDLOCK`
- candidate existence check against the target table

The allocation is global by table and field. It is not partitioned by branch, store, year, month, transaction type, user, or cashbox.

## Immediate Evidence

| Hypothesis | Evidence | Result |
|---|---|---|
| All users allocating POS invoice `Transaction_ID` compete on one shared allocator | `GetNextID_FromSequence` builds one app-lock resource for `dbo.Transactions.Transaction_ID`; no branch/store/date partition exists | Confirmed architecturally |
| All users allocating `Notes.NoteID` compete on one shared allocator | Same global app-lock pattern for `dbo.Notes.NoteID` | Confirmed architecturally |
| All users allocating `DOUBLE_ENTREY_VOUCHERS.Double_Entry_Vouchers_ID` compete on one shared allocator | Same global app-lock pattern for `dbo.DOUBLE_ENTREY_VOUCHERS.Double_Entry_Vouchers_ID` | Confirmed architecturally |
| `POS_DEVSerialAllocator` is globally serialized | It is partitioned by `SerialDate`, so all users on the same day update the same row | Confirmed for same-day DEV serial |
| Allocator alone reproduces production deadlocks on local `Cash` | 100 and 150 concurrent allocator-only tests produced 0 deadlocks and 0 timeouts | Rejected on local benchmark |
| Allocator alone can become a latency choke point | 150-user `Transaction_ID` test showed p95 390 ms and p99 406 ms, while other allocators stayed mostly below 26 ms p99 | Partially confirmed: serialization exists and can slow, but did not deadlock alone |

## Local Benchmark Results

Database used: local `Cash` from `Web.config`.

Important: benchmark helper scripts can consume sequence/voucher numbers. These runs were local/test evidence only.

100 concurrent users, 5 iterations each:

| Allocator | Attempts | Success | Deadlocks | Timeouts | Avg ms | Max ms | p95 ms | p99 ms |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| TransactionID | 500 | 500 | 0 | 0 | 0 | 20 | 3 | 3 |
| NotesNoteID | 500 | 500 | 0 | 0 | 0 | 3 | 3 | 3 |
| DoubleEntryVoucherID | 500 | 500 | 0 | 0 | 1 | 16 | 3 | 3 |
| DEVSerial | 500 | 500 | 0 | 0 | 0 | 6 | 3 | 3 |
| VoucherCoding21 | 500 | 500 | 0 | 0 | 1 | 16 | 3 | 10 |
| NotesCoding | 500 | 500 | 0 | 0 | 0 | 6 | 3 | 3 |

150 concurrent users, 3 iterations each:

| Allocator | Attempts | Success | Deadlocks | Timeouts | Avg ms | Max ms | p95 ms | p99 ms |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| TransactionID | 450 | 450 | 0 | 0 | 200 | 616 | 390 | 406 |
| NotesNoteID | 450 | 450 | 0 | 0 | 0 | 10 | 3 | 3 |
| DoubleEntryVoucherID | 450 | 450 | 0 | 0 | 2 | 56 | 6 | 26 |
| DEVSerial | 450 | 450 | 0 | 0 | 0 | 16 | 3 | 6 |
| VoucherCoding21 | 450 | 450 | 0 | 0 | 1 | 26 | 6 | 13 |
| NotesCoding | 450 | 450 | 0 | 0 | 1 | 23 | 6 | 13 |

Conclusion from local benchmark:

The allocator design is definitely centralized. However, isolated allocator calls did not reproduce production deadlocks. `Transaction_ID` became visibly serialized at 150 users, so it remains a real bottleneck candidate, but the production deadlock likely requires the full save transaction: invoice allocation plus journal rows plus issue voucher/card path plus other concurrent reads/writes.

## Full Save Timing Added

`dbo.usp_POS_SaveTransaction` now records allocation stages into `dbo.POS_SaveAllocationStageLog`:

- `Transaction_ID allocation`
- `Invoice voucher coding allocation`
- `Invoice NoteID allocation`
- `Invoice NoteSerial allocation`
- `Double entry voucher ID allocation`
- `DEV_Serial allocation`
- `Accounting insert`
- `Issue voucher Transaction_ID allocation`
- `Issue voucher NoteID allocation`
- `Issue voucher NoteSerial allocation`
- `Issue voucher coding allocation`

No sensitive customer/card data is logged. Logged context: transaction id, client request id, branch id, store id, user id, service type, stage name, duration, success/error.

Useful query after real or load-test saves:

```sql
SELECT TOP (500)
    CreatedAt,
    Transaction_ID,
    ClientRequestId,
    BranchId,
    StoreID,
    UserID,
    ServiceType,
    StageName,
    DurationMs,
    Success,
    ErrorNumber,
    ErrorMessage
FROM dbo.POS_SaveAllocationStageLog
ORDER BY Id DESC;
```

Stage summary:

```sql
;WITH x AS
(
    SELECT
        StageName,
        DurationMs,
        ROW_NUMBER() OVER (PARTITION BY StageName ORDER BY DurationMs) AS rn,
        COUNT(*) OVER (PARTITION BY StageName) AS cnt
    FROM dbo.POS_SaveAllocationStageLog
    WHERE CreatedAt >= DATEADD(HOUR, -2, GETDATE())
)
SELECT
    StageName,
    COUNT(*) AS samples,
    AVG(DurationMs) AS avg_ms,
    MAX(DurationMs) AS max_ms,
    MIN(CASE WHEN rn >= CEILING(cnt * 0.95) THEN DurationMs END) AS p95_ms,
    MIN(CASE WHEN rn >= CEILING(cnt * 0.99) THEN DurationMs END) AS p99_ms
FROM x
GROUP BY StageName
ORDER BY p99_ms DESC;
```

## Diagnostic / Benchmark Deliverables

- `Areas/Pos/Sql/MANUAL_87_POS_SequenceAllocator_Benchmark.sql`
  - installs benchmark helper tables/procedures
  - extracts current definitions
  - captures waits, locks, blockers, system_health deadlock XML indicators

- `Areas/Pos/Tools/Invoke-PosSequenceAllocatorBenchmark.ps1`
  - runs concurrent allocator tests
  - supports 1, 10, 25, 50, 100, and 150 users
  - writes CSV to `Areas/Pos/Logs`

- `Areas/Pos/Sql/88_POS_SequenceAllocator_ExperimentalFix.sql`
  - test-only alternatives:
    - partitioned allocator
    - SQL Server 2012 sequence object option
    - single-row `UPDATE ... OUTPUT` allocator
  - does not replace production logic

## How To Run Benchmark

Example against a test copy:

```powershell
& "F:\Source Code\DynamicErp\Areas\Pos\Tools\Invoke-PosSequenceAllocatorBenchmark.ps1" `
  -ConnectionString "Data Source=SERVER;Initial Catalog=Cash_Test;User ID=sa;Password=***;MultipleActiveResultSets=False;TrustServerCertificate=True" `
  -UserLevels @(1,10,25,50,100,150) `
  -IterationsPerUser 20 `
  -Allocators @('TransactionID','NotesNoteID','DoubleEntryVoucherID','DEVSerial','VoucherCoding21','NotesCoding') `
  -CommandTimeoutSeconds 90
```

For production evidence, do not run the mutating allocator benchmark on the live database. Instead, use the full save stage log and the read-only deadlock diagnostics.

## How To Collect Deadlock Graphs

Use `Areas/Pos/Sql/MANUAL_87_POS_SequenceAllocator_Benchmark.sql` summary/deadlock section or `MANUAL_86_POS_Save_Deadlock_Diagnostics.sql`.

The deadlock query highlights whether XML contains:

- `GetNextID_FromSequence`
- `usp_Voucher_coding_V2`
- `usp_Notes_coding_V2`
- `POS_DEVSerialAllocator`

## Risk / Next Step

Before changing production numbering semantics, collect full-save stage data during peak or a realistic save load test. If the stage log shows `Transaction_ID allocation` or `DEV_Serial allocation` p95/p99 climbing under real saves, the safest SQL Server 2012-compatible mitigation is:

1. Keep existing numbering semantics first.
2. Move to a very short single-row allocator transaction where possible.
3. Evaluate partitioning only for numbers that are already branch/store/date scoped.
4. Avoid radical accounting changes until the full-save stage log proves which stage blocks.
