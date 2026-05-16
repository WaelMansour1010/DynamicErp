# POS Save Phase 1 Cash Test Results - 2026-05-15

## Result

Recommendation: **needs more work before staged deployment**.

Script `100_POS_SaveTransaction_Phase1_LockWindow.sql` applied successfully to the test database `Cash`, and the first controlled low-user smoke run showed no deadlocks, no SQL timeouts, no duplicate branch-scope voucher numbers, and balanced accounting rows for the successful cash-in/cash-out saves.

Testing stopped before medium/peak concurrency because a required smoke path failed:

- `violations` POS save failed with SQL error `50000`: `Invoice accounting account mapping is missing for one or more required rows.`
- The automated `card` smoke attempt did not reach save execution because the harness timed out while discovering a card candidate.

This is not enough evidence to approve staged deployment.

## Exact DB / Server Used

- SQL Server: `Wael\Sql2019`
- Database: `Cash`
- Config source: `F:\Source Code\DynamicErp\Web.config`
- Connection string name: `KishnyCashConnection`

## Backup Confirmation

A fresh copy-only backup was already available and referenced for this test:

```text
C:\Program Files\Microsoft SQL Server\MSSQL15.SQL2019\MSSQL\Backup\Cash_Phase1_Pre100_20260515_223224.bak
```

This backup was taken and verified before script 99/script 100 testing.

## Scripts Applied

Applied prerequisite script earlier in this validation sequence:

```text
F:\Source Code\DynamicErp\Areas\Pos\Sql\99_POS_VoucherSerialScope.sql
```

Applied in this controlled test:

```text
F:\Source Code\DynamicErp\Areas\Pos\Sql\100_POS_SaveTransaction_Phase1_LockWindow.sql
```

No other SQL script was intentionally applied during script 100 testing.

## Script 100 Verification

After applying script 100:

- `TblOptions.POSVoucherSerialScope = Branch`
- `dbo.usp_POS_SaveTransaction` was recreated successfully.
- Procedure create/modify date after apply: `2026-05-15 22:42:47.207`
- Rollback table exists: `dbo.POS_SaveTransactionPhase1Rollback`
- Rollback capture row exists:
  - `BackupId = 1`
  - `DatabaseName = Cash`
  - `ProcedureName = usp_POS_SaveTransaction`
  - `ChangeName = Phase1_LockWindow_20260515`
  - previous procedure definition bytes: `113380`
- `TblOptions` column count remained `396`.

Verification logs:

```text
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\04_before_100_state.txt
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\05_after_100_state.txt
```

## Warm-Up

Branch-scope counter warm-up happened through the first controlled saves. New `SerialCounters_V2` rows were created for the successful tested branch/service-type combinations.

Observed warmed branch-scope counters included transaction types `19` and `21` for tested branches including `23`, `29`, `39`, `45`, `61`, and `88`.

## Tests Executed

Executed:

1. Confirmed `Wael\Sql2019 / Cash`.
2. Verified backup reference.
3. Verified prerequisite script 99 state.
4. Captured pre-script-100 state.
5. Applied script 100 only.
6. Verified Branch voucher scope and rollback capture.
7. Attempted card-enabled smoke.
8. Ran low-user controlled non-card smoke with:
   - cash-in
   - cash-out
   - violations
   - issue voucher path for successful saves
   - accounting entry verification for successful saves
   - duplicate branch-scope voucher query
9. Checked allocation-stage diagnostics.
10. Checked system-health deadlock XML after the run.

Not completed:

- Card save smoke, because candidate discovery timed out before save execution.
- Receipt/print smoke, because the direct SQL harness does not exercise printing.
- Duplicate submit protection, because the stopped validation run used `DoubleClickPercent = 0` and the direct SQL harness is not a full MVC/browser duplicate-submit path.
- Medium concurrency.
- Peak-like concurrency.

## Harness Runs

Card-enabled smoke attempt:

```text
Users=8
InvoicesPerUser=1
MaxDegreeOfParallelism=2
IncludeCardInvoices=True
IncludeViolations=True
SimulateUserFlow=True
SkipApplyPosSqlScripts=True
```

Result: stopped before saves due to candidate discovery timeout.

Low-user non-card smoke:

```text
RunId=20260515224600
Users=8
InvoicesPerUser=1
MaxDegreeOfParallelism=2
IncludeViolations=True
SimulateUserFlow=True
SkipApplyPosSqlScripts=True
DoubleClickPercent=0
```

Artifacts:

```text
F:\Source Code\DynamicErp\_Releases\POS_LoadTest_20260515224600\summary.json
F:\Source Code\DynamicErp\_Releases\POS_LoadTest_20260515224600\results.csv
F:\Source Code\DynamicErp\_Releases\POS_LoadTest_20260515224600\failed-sample.json
```

## Low Smoke Summary

```text
Requests: 8
Success: 6
Failed: 2
Deadlocks: 0
Timeouts: 0
TotalDurationMs: 14934
AvgSuccessMs: 2312.83
MaxSuccessMs: 10603
RetriedSuccess: 0
MaxAttempts: 1
DuplicateSubmits: 0
DuplicateCreatedInvoices: 0
SavedTransactions: 6
DetailRows: 6
NotesRows: 6
DevRows: 38
IssueVouchers: 6
```

Successful transaction IDs:

```text
1299494 cash-out
1299497 cash-in
1299496 cash-in
1299500 cash-out
1299503 cash-in
1299505 cash-in
```

Failed smoke cases:

```text
violations, BranchId=67, StoreID=64, UserID=88, SqlError=50000
violations, BranchId=44, StoreID=43, UserID=65, SqlError=50000
```

Error:

```text
Invoice accounting account mapping is missing for one or more required rows.
```

## Save Duration p95 / p99

Successful save durations from the low smoke:

```text
373, 424, 428, 995, 1054, 10603 ms
```

Nearest-rank metrics on this small sample:

```text
Count: 6
Average: 2312.83 ms
P50: 428 ms
P95: 10603 ms
P99: 10603 ms
Max: 10603 ms
```

The first successful cash-out appears to include cold branch-scope counter creation/warm-up cost and should not be treated as steady-state proof.

## Voucher / Allocation Stage Durations

From `dbo.POS_SaveAllocationStageLog` after the low smoke:

```text
Transaction_ID allocation cash-out: samples=2, avg=176 ms, max=353 ms
Transaction_ID allocation violations: samples=2, avg=171 ms, max=343 ms
Invoice voucher coding allocation cash-out: samples=2, avg=141 ms, max=276 ms
Accounting insert cash-out: samples=2, avg=116 ms, max=170 ms
Invoice voucher coding allocation violations: samples=2, avg=83 ms, max=120 ms
Double entry voucher ID allocation violations: samples=2, avg=34 ms, max=66 ms
Invoice NoteID allocation violations: samples=2, avg=21 ms, max=43 ms
Invoice NoteID allocation cash-out: samples=2, avg=21 ms, max=43 ms
Double entry voucher ID allocation cash-out: samples=2, avg=20 ms, max=40 ms
Invoice voucher coding allocation cash-in: samples=4, avg=17 ms, max=36 ms
Accounting insert cash-in: samples=4, avg=13 ms, max=30 ms
Double entry voucher ID allocation cash-in: samples=4, avg=1 ms, max=3 ms
```

The successful rows support the Phase 1 assumption that `DOUBLE_ENTREY_VOUCHERS` raw ID allocation is not the dominant observed hotspot in this small run. Voucher coding and first-use branch-scope counter creation still deserve attention under larger load.

## Deadlocks / Timeouts

Harness summary:

```text
Deadlocks: 0
Timeouts: 0
```

System-health deadlock XML recheck returned no rows in the available file target result set after the smoke run.

Log:

```text
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\08_deadlock_after_smoke_recheck.txt
```

## Duplicate Serial Findings

No duplicate `NoteSerial1` rows were found within the tested Branch scope/type/month query after the low smoke.

Successful branch-scope `NoteSerial1` examples:

```text
Branch 61, Type 21, NoteSerial1 612605425
Branch 88, Type 19, NoteSerial1 882605248
Branch 45, Type 19, NoteSerial1 452605803
Branch 29, Type 21, NoteSerial1 292605312
Branch 39, Type 19, NoteSerial1 392605349
Branch 23, Type 19, NoteSerial1 232605715
```

This confirms no duplicate in the small sample only. It does not replace medium/peak concurrency validation.

## Accounting Verification

Successful saves had balanced accounting rows:

```text
Transaction 1299494: DevRows=5, Debit=431, Credit=431
Transaction 1299496: DevRows=7, Debit=1048.9, Credit=1048.9
Transaction 1299497: DevRows=7, Debit=1047.9, Credit=1047.9
Transaction 1299500: DevRows=5, Debit=431, Credit=431
Transaction 1299503: DevRows=7, Debit=1051.9, Credit=1051.9
Transaction 1299505: DevRows=7, Debit=1052.9, Credit=1052.9
```

Accounting was not verified for failed violations saves because those transactions rolled back.

## POS_SaveAttemptLog / Stage Logs

`POS_SaveAllocationStageLog` captured the direct stored procedure harness stages for this run.

`POS_SaveAttemptLog` was not populated by this direct stored procedure harness. Its schema exists in Cash, but it records application/repository save attempts rather than the direct SQL harness flow used here.

Schema sample log:

```text
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\09_attemptlog_schema_sample.txt
```

## Errors Found

1. `violations` smoke failed twice with SQL error `50000`.

   Error text:

   ```text
   Invoice accounting account mapping is missing for one or more required rows.
   ```

   This meets the stop criteria because a required save path failed.

2. Card-enabled smoke did not reach save execution.

   The harness timed out during card candidate discovery. This blocks card validation but does not prove a script 100 save failure.

3. Receipt/print and full duplicate-submit protection were not validated by the direct SQL harness.

## Rollback Status

Rollback was **not performed**.

Current Cash test state after this run:

- `TblOptions.POSVoucherSerialScope = Branch`
- `dbo.usp_POS_SaveTransaction` is the Phase 1 script 100 version.
- `dbo.POS_SaveTransactionPhase1Rollback` contains the previous procedure definition.
- Fresh backup remains available:

```text
C:\Program Files\Microsoft SQL Server\MSSQL15.SQL2019\MSSQL\Backup\Cash_Phase1_Pre100_20260515_223224.bak
```

Rollback options:

1. Restore `Cash` from the backup above.
2. Or restore only `dbo.usp_POS_SaveTransaction` from `dbo.POS_SaveTransactionPhase1Rollback` and set:

```sql
UPDATE dbo.TblOptions
SET POSVoucherSerialScope = N'Company';
```

Use full database restore if test-generated rows must also be removed.

## Recommendation

Status: **needs more work**.

Script 100 is technically applied and the successful cash-in/cash-out smoke cases look healthy, but deployment approval should wait until:

1. The `violations` accounting mapping failure is understood and either fixed or proven to be test-data-only.
2. Card save smoke is executed with a deterministic card test case instead of the timed-out candidate discovery query.
3. Issue voucher path is rechecked after the violations/card gaps are closed.
4. Duplicate submit protection is tested through the application/repository path.
5. Medium and peak-like concurrency are rerun only after all required smoke paths pass.

Do not proceed to production/staged deployment from this result alone.
