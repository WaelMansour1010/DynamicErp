# POS DOUBLE_ENTREY_VOUCHERS Deadlock Investigation - 2026-05-14

## Scope

Project: `F:\Source Code\DynamicErp`

Area: `Areas\Pos`

Primary suspect: `dbo.DOUBLE_ENTREY_VOUCHERS` journal/accounting insert inside `dbo.usp_POS_SaveTransaction`.

Test database: `Cash_FullSaveDEV_20260514`, copied from local `Cash`.

## Objects Inspected

`dbo.DOUBLE_ENTREY_VOUCHERS`:

- No triggers found.
- Foreign keys:
  - `FK_DOUBLE_ENTREY_VOUCHERS_ACCOUNTS` to `ACCOUNTS`, update cascade.
  - `FK_DOUBLE_ENTREY_VOUCHERS_Notes` to `Notes`, delete/update cascade.
  - `FK_DOUBLE_ENTREY_VOUCHERS_ReceiptQest` to `ReceiptQest`, delete/update cascade.
- Clustered PK: `(Double_Entry_Vouchers_ID, DEV_ID_Line_No)`, fill factor 80.
- Many nonclustered indexes exist, including several wide indexes with many included columns:
  - `IDX_DOUBLE_ENTREY_VOUCHERS_Account_Credit_Date`
  - `<IndexSearchEntry, sysname,>`
  - `<IndexFast, sysname,>`
  - `<ccIndex, sysname,>`
  - `IX_DEV_RecordDate`
  - `IX_POS_DEV_RecordDate`
  - `IX_DOUBLE_ENTREY_VOUCHERS_Notes_ID`
  - `IX_DOUBLE_ENTREY_VOUCHERS_Transaction_ID`

## POS Save Lock Path

Inside `dbo.usp_POS_SaveTransaction`, accounting is inserted while the main save transaction is still open.

The procedure already holds or has touched:

1. `Transactions`
2. voucher coding logic
3. `Notes`
4. `DOUBLE_ENTREY_VOUCHERS` ID allocation
5. `POS_DEVSerialAllocator`
6. `DOUBLE_ENTREY_VOUCHERS` insert
7. issue voucher rows, when applicable

So yes: POS save inserts into `DOUBLE_ENTREY_VOUCHERS` while inside the larger transaction and after prior allocation/voucher/notes work.

## Full Save Load Test

Command shape:

```powershell
& "F:\Source Code\DynamicErp\Areas\Pos\Tools\Invoke-PosSaveLoadTest.ps1" `
  -WebConfigPath "F:\Source Code\DynamicErp\Web.config" `
  -ConnectionStringName "KishnyCashConnection" `
  -Users 150 `
  -InvoicesPerUser 1 `
  -MaxDegreeOfParallelism 150 `
  -TestDatabaseName "Cash_FullSaveDEV_20260514" `
  -AllowMutatingTarget `
  -SimulateUserFlow `
  -DoubleClickPercent 20 `
  -MaxThinkTimeMs 1200 `
  -CommandTimeoutSeconds 120
```

Result:

| Metric | Value |
|---|---:|
| Requests | 150 |
| Success | 150 |
| Failed | 0 |
| Deadlocks | 0 |
| Timeouts | 0 |
| Avg success ms | 7047 |
| Max success ms | 16058 |
| Duplicate submits | 26 |
| Duplicate created invoices | 8 |
| Saved transactions | 150 |
| Journal rows | 974 |
| Issue vouchers | 150 |

Important note: this tool calls `dbo.usp_POS_SaveTransaction` directly, so duplicate-submit idempotency implemented in the MVC controller/repository is bypassed. The duplicate-created invoices in this direct SQL harness prove that the stored procedure itself is not idempotent by `@NoID`; the web endpoint still has the protection layer.

## Full Save Stage Evidence

150-user full save stage summary:

| Stage | Service | Samples | Avg ms | Max ms | p95 ms | p99 ms |
|---|---|---:|---:|---:|---:|---:|
| Invoice voucher coding allocation | cash-out | 46 | 5201 | 14640 | 13500 | 14640 |
| Invoice voucher coding allocation | cash-in | 130 | 3061 | 15206 | 11316 | 13126 |
| DEV_Serial allocation | cash-in | 112 | 3225 | 4726 | 4590 | 4713 |
| DEV_Serial allocation | cash-out | 46 | 2802 | 4646 | 4606 | 4646 |
| Double entry voucher ID allocation | cash-out | 46 | 3 | 33 | 16 | 33 |
| Double entry voucher ID allocation | cash-in | 112 | 2 | 46 | 6 | 26 |
| Accounting insert | cash-in | 112 | 2 | 33 | 6 | 16 |
| Accounting insert | cash-out | 46 | 2 | 10 | 6 | 10 |

The slowest full-save stage was not the actual `DOUBLE_ENTREY_VOUCHERS` insert. It was:

1. `dbo.usp_Voucher_coding_V2`, invoice `Transaction_Type = 21`, `Sanad_No = 7`
2. `dbo.POS_DEVSerialAllocator`, same-day row allocation

## Targeted DOUBLE_ENTREY_VOUCHERS Insert Benchmark

Tool:

`Areas/Pos/Tools/Invoke-PosDoubleEntryVoucherBenchmark.ps1`

Benchmark modes:

- `cash`: 2 journal rows per voucher.
- `card`: 4 journal rows per voucher, simulating heavier card accounting.

Results:

| Mode | Users | Attempts | Deadlocks | Timeouts | Avg insert ms | Max insert ms | p95 insert ms | p99 insert ms |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| cash | 10 | 50 | 0 | 0 | 0 | 3 | 3 | 3 |
| cash | 25 | 125 | 0 | 0 | 0 | 13 | 3 | 3 |
| cash | 50 | 250 | 0 | 0 | 0 | 3 | 3 | 3 |
| cash | 100 | 500 | 0 | 0 | 0 | 20 | 3 | 6 |
| cash | 150 | 750 | 0 | 0 | 0 | 40 | 3 | 6 |
| card | 10 | 50 | 0 | 0 | 0 | 3 | 3 | 3 |
| card | 25 | 125 | 0 | 0 | 0 | 13 | 3 | 13 |
| card | 50 | 250 | 0 | 0 | 0 | 20 | 3 | 6 |
| card | 100 | 500 | 0 | 0 | 2 | 60 | 13 | 36 |
| card | 150 | 750 | 0 | 0 | 1 | 83 | 3 | 40 |

Targeted insert benchmark did not reproduce deadlocks. Card mode is heavier but still p99 stayed around 40 ms at 150 users on the test database.

## Wait / Index Evidence

After benchmark, index operational stats showed:

| Index | leaf inserts | leaf allocations | page latch waits | page latch wait ms | page lock waits | page lock wait ms |
|---|---:|---:|---:|---:|---:|---:|
| `PK_DOUBLE_ENTREY_VOUCHERS` | 11394 | 727 | 346 | 1152 | 0 | 0 |
| `<ccIndex, sysname,>` | 11394 | 313 | 0 | 0 | 0 | 0 |
| `<IndexFast, sysname,>` | 11394 | 178 | 0 | 0 | 0 | 0 |
| `IDX_DOUBLE_ENTREY_VOUCHERS_Account_Credit_Date` | 11394 | 145 | 1 | 0 | 34 | 953 |

Wait samples during benchmark included:

- `LCK_M_X` samples on `DOUBLE_ENTREY_VOUCHERS` indexes, especially in card mode.
- `SEQUENCE_GENERATION` samples near insert/allocation windows.
- Many granted locks on wide nonclustered indexes.
- No deadlocks and no long waits in the isolated journal insert benchmark.

This confirms index maintenance pressure exists, but it did not dominate the measured full-save latency on the local test.

## Evidence Table

| Hypothesis | Evidence | Result |
|---|---|---|
| POS save inserts into `DOUBLE_ENTREY_VOUCHERS` while holding prior locks | `usp_POS_SaveTransaction` performs voucher coding, `Notes`, `DoubleEntryVoucherID`, `DEVSerial`, then journal insert inside one transaction | Confirmed |
| Hot clustered index last page contention | Operational stats show `PK_DOUBLE_ENTREY_VOUCHERS` page latch waits: 346 waits / 1152 ms after benchmark | Partially confirmed as pressure, not root cause in test |
| Missing/wide indexes make journal insert expensive | Table has many wide NC indexes; benchmark showed granted X/IX locks across several DEV indexes | Confirmed as write amplification risk |
| Actual journal insert is the slowest stage in full save | Full save p99 accounting insert was 10-16 ms, while voucher coding p99 was 13-14.6 sec and DEVSerial p99 was 4.6-4.7 sec | Rejected on local test |
| DoubleEntryVoucherID allocation is harmless but insert/index maintenance is slow | DoubleEntryVoucherID p99 26-33 ms; accounting insert p99 10-16 ms in full save | Mostly rejected on local test |
| Card accounting rows are heavier | Dedicated card insert p99 reached 36-40 ms at 100-150 users vs cash 6 ms | Confirmed, but not deadlocking in test |
| Triggers cause balance updates/deadlocks | No triggers found on `DOUBLE_ENTREY_VOUCHERS` | Rejected |
| Cascading constraints participate | FKs exist to `ACCOUNTS`, `Notes`, `ReceiptQest`; update/delete cascade exists on Notes/ReceiptQest but insert only validates parent rows | Possible contributor only if parent rows are locked elsewhere |
| Ledger/report reads can block POS inserts | Many SQL modules read `DOUBLE_ENTREY_VOUCHERS`; no deadlock XML captured locally showing report SELECT blocking POS insert | Unproven; needs production deadlock XML |
| Lock order differs in card issue voucher path | Full card save with real KYC data could not be reproduced because card selection query timed out / no valid card token prepared; targeted card journal insert was tested separately | Incomplete |

## Current Conclusion

`DOUBLE_ENTREY_VOUCHERS` is not cleared. It has real write amplification and latch/index pressure.

However, local evidence points more strongly to two chokepoints before/around accounting:

1. `dbo.usp_Voucher_coding_V2` for POS invoice voucher coding.
2. `dbo.POS_DEVSerialAllocator` same-day row allocation.

The actual `INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS` was fast in both full save and isolated benchmark.

## Recommended Fix Strategy

Phase 1:

- Keep accounting semantics unchanged.
- Capture production deadlock XML using `MANUAL_90_POS_DoubleEntryVoucher_Diagnostics.sql`.
- Add production stage-log query around incidents and compare `Accounting insert`, `DEV_Serial allocation`, and `Invoice voucher coding allocation`.
- Review `dbo.usp_Voucher_coding_V2` next, because it is the measured p99 hotspot.

Phase 2:

- If production deadlock XML shows report SELECTs blocking DEV writes, add narrow covering indexes for those exact SELECT predicates only.
- If XML shows `PK_DOUBLE_ENTREY_VOUCHERS` page latch pressure, evaluate fill factor or key strategy with DBA approval.
- If XML shows `IDX_DOUBLE_ENTREY_VOUCHERS_Account_Credit_Date` range locks, review ledger/report isolation and predicates.

Phase 3:

- Consider partitioning/reserving ranges only for numbers already logically scoped by date/branch/store/type.
- Do not move accounting async unless business owner explicitly accepts delayed journal posting.
