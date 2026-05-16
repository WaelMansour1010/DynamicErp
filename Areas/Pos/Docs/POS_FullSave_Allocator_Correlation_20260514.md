# POS Full Save Allocator Correlation - 2026-05-14

## Purpose

The allocator-only benchmark confirmed centralized allocation but did not reproduce production deadlocks. This report correlates allocator/accounting stage timings inside full POS save.

## Instrumentation

`dbo.usp_POS_SaveTransaction` writes stage timings to `dbo.POS_SaveAllocationStageLog`:

- `Transaction_ID allocation`
- `Invoice voucher coding allocation`
- `Invoice NoteID allocation`
- `Invoice NoteSerial allocation`
- `Double entry voucher ID allocation`
- `DEV_Serial allocation`
- `Accounting insert`
- issue voucher allocation/coding stages

`Areas/Pos/Tools/Invoke-PosSaveLoadTest.ps1` now sends a real `ClientRequestId` through `@NoID`, so load-test result rows can be correlated with `POS_SaveAllocationStageLog.ClientRequestId`.

## Load Test

Test database: `Cash_FullSaveDEV_20260514`

150 concurrent direct stored procedure saves, simulated print/read flow, 20% duplicate submit simulation.

| Metric | Value |
|---|---:|
| Requests | 150 |
| Success | 150 |
| Failed | 0 |
| Deadlocks | 0 |
| Timeouts | 0 |
| Avg success duration | 7047 ms |
| Max success duration | 16058 ms |
| Duplicate submits | 26 |
| Duplicate created invoices | 8 |
| Journal rows | 974 |
| Issue vouchers | 150 |

Important: the direct stored-procedure harness bypasses the MVC/repository idempotency layer, so duplicate-created invoices in this harness show that DB-only `@NoID` idempotency does not exist. It does not mean the web endpoint duplicate guard failed.

## Stage p95/p99

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

## Correlation Result

Slow saves were dominated by `Invoice voucher coding allocation`, not ordinary `Transaction_ID` allocation and not `DOUBLE_ENTREY_VOUCHERS` insert.

The second largest stage was `DEV_Serial allocation`, which uses `POS_DEVSerialAllocator` by day and therefore concentrates all same-day saves on one row.

`Double entry voucher ID allocation` and `Accounting insert` stayed low in this test.

## Required Query

Use this after a production incident or realistic web-driven load test:

`Areas/Pos/Sql/MANUAL_89_POS_FullSave_AllocationCorrelation.sql`

It answers:

- Are slow saves mostly card/cash-in/cash-out?
- What is the slowest stage per slow save?
- Does accounting insert dominate?
- Does `DoubleEntryVoucherID` allocation rise with accounting insert?
- Does `DEV_Serial` show same-day contention?
- Are full-save p95/p99 worse than isolated allocator p95/p99?

## Evidence Table

| Hypothesis | Evidence | Result |
|---|---|---|
| Allocator latency only matters inside full save | Full save p99 for voucher coding reached 13-14.6 sec while isolated allocator p99 was milliseconds | Confirmed for voucher coding path |
| `Transaction_ID` allocation dominates full save | Full save `Transaction_ID allocation` p99 was 10-13 ms | Rejected on this test |
| `DoubleEntryVoucherID` allocation dominates full save | p99 was 26-33 ms | Rejected on this test |
| `Accounting insert` dominates full save | p99 was 10-16 ms | Rejected on this test |
| `DEVSerial` same-day row is a choke point | p99 was 4.6-4.7 sec under 150 users | Confirmed |
| Duplicate browser submits are harmless at DB-only level | Direct SP duplicate submit created duplicate invoices because DB proc does not enforce idempotency by `@NoID` | Rejected for direct DB path; web endpoint still protected |

## Next Fix Candidate

The safest next investigation/fix is not changing accounting totals. It is:

1. Inspect and benchmark `dbo.usp_Voucher_coding_V2`.
2. Shorten/partition `POS_DEVSerialAllocator` only if numbering rules allow it.
3. Keep `DOUBLE_ENTREY_VOUCHERS` insert logic unchanged unless production deadlock XML specifically names a DEV index/key/page as waiter/owner.
