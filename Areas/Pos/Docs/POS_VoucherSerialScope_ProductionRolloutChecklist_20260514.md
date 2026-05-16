# POS Voucher Serial Scope Production Rollout Checklist - 2026-05-14

## Purpose

Roll out branch-scoped POS invoice serial allocation safely for Kishny without changing numbering format or accounting logic.

## Files To Deploy

- `Areas/Pos/Sql/99_POS_VoucherSerialScope.sql`
- `Areas/Pos/Sql/MANUAL_98_POS_VoucherCoding_Diagnostics.sql`
- `Areas/Pos/Sql/POS_SQL_AutoUpdate_Manifest.json`
- `Areas/Pos/Data/PosSqlRepository.cs`
- `Areas/Pos/Models/PosSaveTransactionRequest.cs`
- rebuilt `bin/MyERP.dll`

## Pre-Rollout

1. Take a database backup.
2. Deploy the application files and `MyERP.dll`.
3. Apply POS SQL auto-update script order 99, or run `Areas/Pos/Sql/99_POS_VoucherSerialScope.sql` manually.
4. Confirm the new option exists and still preserves default behavior:

```sql
SELECT TOP (1) POSVoucherSerialScope
FROM dbo.TblOptions;
```

Expected default after script install is `Company` unless explicitly changed.

## Kishny Setting

Kishny invoice numbering should be branch-scoped. After confirming deployment, run:

```sql
UPDATE dbo.TblOptions
SET POSVoucherSerialScope = N'Branch';
```

Do not set `BranchStore` unless `sanad_numbering.StoreCoding = 1` for the relevant sanad.

## Required Verification

Run:

```sql
SELECT TOP (1) POSVoucherSerialScope
FROM dbo.TblOptions;

SELECT
    sanad_no,
    MIN(CONVERT(INT, ISNULL(StoreCoding, 0))) AS MinStoreCoding,
    MAX(CONVERT(INT, ISNULL(StoreCoding, 0))) AS MaxStoreCoding
FROM dbo.sanad_numbering
WHERE sanad_no IN (7, 10)
GROUP BY sanad_no;
```

For current Kishny data, expected:

- `POSVoucherSerialScope = Branch`
- `StoreCoding = 0` for `Sanad_No = 7`
- `StoreCoding = 0` for `Sanad_No = 10`

Then run `Areas/Pos/Sql/MANUAL_98_POS_VoucherCoding_Diagnostics.sql` and confirm:

- configured scope is `Branch`
- effective scope is `Branch`
- `EffectiveStoreKey = 0` when `StoreCoding = 0`
- final key is `SourceTable + BranchID + TypeCode + Prefix + StoreID=0 + YearNum + MonthNum`

## Counter Warm-Up Note

The final sanity benchmark showed the first multi-branch run was slower because many tested branch-scoped `SerialCounters_V2` rows did not exist yet. Once those rows existed, the warm multi-branch run had no deadlocks/timeouts and normalized p95/p99.

Apply the change off-peak. The first invoice per active branch/type/month may create or seed the new branch counter. If zero first-use latency is required, prepare a separate reviewed seeding script instead of consuming serials casually.

## Monitoring After Rollout

Watch:

- `POS_SaveAllocationStageLog` for invoice voucher coding duration.
- SQL errors 1205 and timeout errors on POS save.
- `SerialCounters_V2.UpdatedByUser` diagnostics for `Scope=Branch;BranchKey=...;StoreKey=0`.
- Duplicate `NoteSerial1` only inside the selected branch/type/year/month scope.

## Rollback

Fast behavior rollback without restoring binaries:

```sql
UPDATE dbo.TblOptions
SET POSVoucherSerialScope = N'Company';
```

This returns allocation to company-wide scope using the new compatible procedure. Full rollback to the old stored procedure requires restoring from the database backup or redeploying the previous SQL definition.

## Sanity Result

The unusual benchmark result was explained by cold/missing branch counters and first-use seeding, not by cross-branch blocking. With warm counters and `StoreCoding = 0`, branch-scoped allocation locks only the branch/type/month counter row and keeps `EffectiveStoreID = 0`.
