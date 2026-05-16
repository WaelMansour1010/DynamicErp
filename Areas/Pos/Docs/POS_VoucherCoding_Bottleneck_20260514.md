# POS Voucher Coding Bottleneck Investigation - 2026-05-14

## Current Finding

`dbo.usp_Voucher_coding_V2` is the measured p99 hotspot after removing `DEV_Serial` from the hot path.

Recent full-save stage samples on the test copy showed:

| Stage | Samples | Avg ms | Max ms |
| --- | ---: | ---: | ---: |
| Invoice voucher coding allocation | 287 | 2806 | 15206 |
| Issue voucher coding allocation | 214 | 0 | 26 |

The accounting insert itself was previously measured in the low milliseconds range, so the slow path is the invoice voucher coding allocator, not `DOUBLE_ENTREY_VOUCHERS` row insertion.

## Independent Benchmark

Tool:

`Areas/Pos/Tools/Invoke-PosVoucherCodingBenchmark.ps1`

Test database:

`Cash_FullSaveDEV_20260514`

Mode:

- same branch/store/date
- `Transaction_Type = 21`, `Sanad_No = 7`
- `Transaction_Type = 19`, `Sanad_No = 10`
- one allocation per user per type
- copied/test DB only

| Users | Type | Attempts | Success | Deadlocks | Timeouts | Avg ms | Max ms | p95 ms | p99 ms |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 10 | 21 | 10 | 10 | 0 | 0 | 155 | 242 | 242 | 242 |
| 25 | 21 | 25 | 25 | 0 | 0 | 99 | 136 | 128 | 136 |
| 50 | 21 | 50 | 50 | 0 | 0 | 96 | 142 | 129 | 142 |
| 100 | 21 | 100 | 100 | 0 | 0 | 96 | 144 | 123 | 141 |
| 150 | 21 | 150 | 150 | 0 | 0 | 94 | 160 | 119 | 158 |
| 10 | 19 | 10 | 10 | 0 | 0 | 15 | 41 | 41 | 41 |
| 25 | 19 | 25 | 25 | 0 | 0 | 5 | 18 | 18 | 18 |
| 50 | 19 | 50 | 50 | 0 | 0 | 4 | 12 | 10 | 12 |
| 100 | 19 | 100 | 100 | 0 | 0 | 4 | 48 | 9 | 23 |
| 150 | 19 | 150 | 150 | 0 | 0 | 4 | 41 | 17 | 26 |

Conclusion: isolated voucher coding did not reproduce the 13-15 second full-save stage latency. The issue is not cleared; the evidence points to amplification when the allocator runs inside the full POS save transaction and competes with the rest of the save flow.

## Code Path

```text
POS save
  -> dbo.usp_POS_SaveTransaction
    -> dbo.usp_Voucher_coding_V2
      -> dbo.usp_GetNextSerial_V2
        -> dbo.SerialCounters_V2 UPDATE WITH (UPDLOCK, SERIALIZABLE)
        -> first-use fallback MAX(NoteSerial1) from Transactions/Notes
```

Invoice call:

```text
Transaction_Type = 21
Sanad_No = 7
SourceTable = Transactions
StoreID = @StoreID
```

Card issue voucher call:

```text
Transaction_Type = 19
Sanad_No = 10
SourceTable = Transactions
StoreID = @IssueStoreID
```

## Bottleneck Mechanics

The allocator row is not global across the whole company, but it is centralized per hot POS scope:

```text
SourceTable + BranchID + TypeCode + Prefix + StoreID + YearNum + MonthNum
```

For busy branches, all concurrent invoice saves for the same branch/store/type/month update the same `SerialCounters_V2` row. Under a 100-150 cashier peak, this turns voucher numbering into a queue before the invoice row is inserted.

Additional risks found:

- The update predicate uses `ISNULL(Prefix, '')` and `ISNULL(StoreID, 0)`, which can reduce index seek quality and widen locking.
- `Prefix` and `StoreID` are nullable inside the unique index, so logical uniqueness depends on the procedure's normalization rather than a clean non-null key.
- First-use fallback uses dynamic `MAX(...)` with `YEAR()` and `MONTH()` around the date column, which is non-sargable and can scan large historical ranges when a counter row is missing.
- POS `Sanad_No = 7` and `Sanad_No = 10` are configured with only 3 tail digits, and historical duplicates exist in high-volume branch/month data. This is a data/numbering-rule risk separate from locking.

## Evidence Table

| Hypothesis | Evidence | Result |
| --- | --- | --- |
| Voucher coding is the current save bottleneck | Stage log showed invoice voucher coding max 15.2 seconds while issue voucher coding stayed below 26 ms in the sampled run. | Confirmed for invoice path. |
| Voucher coding alone reaches 13-15 seconds | Dedicated same-branch benchmark to 150 users showed type 21 p99 158 ms and no deadlocks/timeouts. | Rejected as standalone behavior. |
| Voucher coding latency is amplified inside full save | Full-save stage log is seconds while isolated procedure benchmark is sub-second. | Confirmed as next focus. |
| Bottleneck is `DOUBLE_ENTREY_VOUCHERS` insert | Prior measurement showed accounting insert around 10-16 ms, while voucher coding reached seconds. | Rejected as primary current hotspot. |
| All POS saves update one company-wide voucher row | Counter key includes branch/type/store/year/month. | Rejected globally. |
| Busy users in the same branch/store/type/month serialize on one row | `usp_GetNextSerial_V2` updates one `SerialCounters_V2` row with `UPDLOCK, SERIALIZABLE` for that scope. | Confirmed. |
| VB6 required the same strict serialization | VB6 used `MAX(NoteSerial1)` filtered by branch/type/date and no explicit serializable/update lock in the inspected path. | Rejected. |
| The number is display-only and can be removed | `NoteSerial1` is used in POS UI, reports, descriptions, and printing. | Rejected. Preserve visible number. |
| Gap-free numbering is required | VB6 `MAX + 1`, rollback behavior, and duplicate/gap evidence do not prove gap-free semantics. | Rejected. |

## Safe Optimization Options

### Option A - Keep Semantics, Shorten Lock Path

Keep the same visible number and counter scope, but make the counter update a narrow seek/update:

- avoid `ISNULL()` expressions in the counter predicate
- normalize `Prefix` and `StoreID` into non-null key values
- use a unique key that exactly matches the update predicate
- keep the transaction around the counter row as short as possible

This is the least risky first fix.

### Option B - Partition Further Only If Proven

Partition by branch/store/date/type only where `sanad_numbering` and real data already prove that scope. POS currently already passes store, but `StoreCoding = 0` in sampled `sanad_numbering`, so changing store semantics must be tested carefully.

### Option C - Reserve Ranges

Reserve blocks per branch/store/type/month to reduce update frequency. This improves peak throughput but increases gap risk and requires clearer operational acceptance.

### Option D - SQL Server SEQUENCE

SQL Server 2012 supports `SEQUENCE`, but one sequence per dynamic branch/type/store/month scope creates deployment and rollover complexity. It is not the first production-safe step.

### Option E - Separate Formatting

Keep allocation as a numeric tail and format `NoteSerial1` outside the locked section. This helps only if formatting or settings reads are inside the critical section; it does not solve the single-row update by itself.

## Next Proof Needed

Run `Areas/Pos/Tools/Invoke-PosVoucherCodingBenchmark.ps1` on a copied database and capture waits with `Areas/Pos/Sql/MANUAL_98_POS_VoucherCoding_Diagnostics.sql`.

The proof target is:

- p95/p99 per concurrency level
- whether wait type is mostly `LCK_M_U` / `LCK_M_X` on `SerialCounters_V2`
- whether contention concentrates on the same branch/store/type/month row
- whether first-use fallback scans appear during counter creation

No production optimization for `dbo.usp_Voucher_coding_V2` should be applied before this specific evidence is captured.

## Configurable Scope Implementation

Added:

`Areas/Pos/Sql/99_POS_VoucherSerialScope.sql`

The script:

- adds `TblOptions.POSVoucherSerialScope`
- default value: `Company`
- allowed effective values: `Company`, `Branch`, `BranchStore`
- recreates `dbo.usp_GetNextSerial_V2` with DROP + CREATE
- resolves effective scope using both the configured setting and `sanad_numbering.StoreCoding`
- keeps the visible serial format unchanged
- records scope/key hints in `SerialCounters_V2.UpdatedByUser` for diagnostics

Effective counter key examples:

| Configured Scope | StoreCoding | Counter Key |
| --- | ---: | --- |
| Company | 0/1 | `SourceTable + BranchID=0 + TypeCode + Prefix + StoreID=0 + Year + Month` |
| Branch | 0/1 | `SourceTable + BranchID + TypeCode + Prefix + StoreID=0 + Year + Month` |
| BranchStore | 0 | `SourceTable + BranchID + TypeCode + Prefix + StoreID=0 + Year + Month` |
| BranchStore | 1 | `SourceTable + BranchID + TypeCode + Prefix + StoreID + Year + Month` |

Current Kishny data:

- `Sanad_No = 7`: `StoreCoding = 0`
- `Sanad_No = 10`: `StoreCoding = 0`

Therefore the expected Kishny setting is `Branch`; `BranchStore` would also resolve to `Branch` unless `StoreCoding` is enabled later.

## Scope Benchmark After Change

Test database:

`Cash_FullSaveDEV_20260514`

Configured setting:

`POSVoucherSerialScope = Branch`

### 100 Users Across Branches - Cold Counters

| Type | Attempts | Success | Deadlocks | Timeouts | Avg ms | Max ms | p95 ms | p99 ms |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 21 | 100 | 100 | 0 | 0 | 139 | 336 | 263 | 336 |
| 19 | 100 | 100 | 0 | 0 | 193 | 630 | 368 | 592 |

This first multi-branch run was slower than the same-branch run because many tested branches did not already have `SerialCounters_V2` rows for the new branch-scoped key. The procedure had to take the first-use path and create/seed counter rows. Recent counter rows showed `UpdateCount = 1` and `UpdatedByUser` diagnostics such as `Scope=Branch;BranchKey=...;StoreKey=0`, proving those keys were created during the benchmark.

### 100 Users Across Branches - Warm Counters

The benchmark was repeated once after the counters existed. No further heavy loop was needed.

| Type | Attempts | Success | Deadlocks | Timeouts | Avg ms | Max ms | p95 ms | p99 ms |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 21 | 100 | 100 | 0 | 0 | 30 | 154 | 137 | 152 |
| 19 | 100 | 100 | 0 | 0 | 26 | 154 | 125 | 151 |

The warm result confirms the unusual slower multi-branch result was caused by cold counter setup / first-use seeding, not by cross-branch locking. With `POSVoucherSerialScope = Branch`, each branch locks its own `SerialCounters_V2` key.

### 100 Users Same Branch

| Type | Attempts | Success | Deadlocks | Timeouts | Avg ms | Max ms | p95 ms | p99 ms |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 21 | 100 | 100 | 0 | 0 | 18 | 109 | 50 | 79 |
| 19 | 100 | 100 | 0 | 0 | 8 | 59 | 22 | 32 |

Generated serials in the benchmark result files had no duplicate `(TransactionType, BranchId, Serial)` values.

Historical production/test transaction data still contains old duplicate `NoteSerial1` values in some branch/month ranges. Those are pre-existing data issues; the benchmark did not create transaction rows, so this check is about generated allocator output, not historical persisted duplicates.

## Final Sanity Check

| Check | Result |
| --- | --- |
| `POSVoucherSerialScope` read from `TblOptions` | Confirmed as `Branch` on `Cash_FullSaveDEV_20260514`. |
| Production default | Remains `Company` through `DF_TblOptions_POSVoucherSerialScope`, preserving old behavior until changed. |
| `StoreCoding` for `Sanad_No = 7` | Confirmed `0`. |
| `StoreCoding` for `Sanad_No = 10` | Confirmed `0`. |
| Effective store key | Confirmed `0` / `NULL` in counter rows while `StoreCoding = 0`. |
| Final counter key | `SourceTable + BranchID + TypeCode + Prefix + EffectiveStoreID + YearNum + MonthNum`. |
| Cross-branch blocking | Rejected after warm counter run; different branches use different counter rows. |
| Duplicate generated serials in benchmark output | None found within `(TransactionType, BranchId, Serial)`. |

For Kishny production the recommended setting is:

```sql
UPDATE dbo.TblOptions
SET POSVoucherSerialScope = N'Branch';
```
