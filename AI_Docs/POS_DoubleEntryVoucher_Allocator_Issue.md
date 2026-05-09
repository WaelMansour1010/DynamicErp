# POS Double Entry Voucher Allocator Issue

Date: 2026-05-09
Database checked: `CASH`
Area: `Areas\Pos`

## Summary

The local `CASH` database had `dbo.seq_DOUBLE_ENTREY_VOUCHERS_Double_Entry_Vouchers_ID` present, but its `current_value` was behind the table:

- `MAX(dbo.DOUBLE_ENTREY_VOUCHERS.Double_Entry_Vouchers_ID) = 1406550`
- Sequence `current_value = 1406549` before repair
- Required next value: `1406551`

This means the next allocation could return an already used voucher id and then fail on `PK_DOUBLE_ENTREY_VOUCHERS (Double_Entry_Vouchers_ID, DEV_ID_Line_No)`.

## Root Cause

The issue was not a wrong table/field name. The sequence name is correct:

`dbo.seq_DOUBLE_ENTREY_VOUCHERS_Double_Entry_Vouchers_ID`

The real local cause was sequence drift: the sequence existed but was lower than the current table max. The production-style error text:

`Cannot alter the sequence ..., because it does not exist or you do not have permission`

also points to a permissions/deploy risk. `dbo.GetNextID_FromSequence` uses dynamic SQL for `CREATE SEQUENCE`, `ALTER SEQUENCE`, and `NEXT VALUE FOR`. Without ownership execution or sequence permissions, an application SQL user can execute the procedure but still fail when the procedure tries to alter or read the sequence.

## Why PK Duplicates Appeared

The allocator sequence returned or was about to return a candidate id that already existed in `DOUBLE_ENTREY_VOUCHERS`. When the procedure tried to correct the sequence with `ALTER SEQUENCE`, that operation could fail if the sequence was missing in that database or the caller had insufficient permissions. If the save flow then continued or retried while the sequence remained behind, subsequent inserts attempted duplicate `(Double_Entry_Vouchers_ID, DEV_ID_Line_No)` keys.

The current `dbo.usp_POS_SaveTransaction` is transaction-wrapped and raises allocation errors before insert when allocation fails. The failure mode was therefore allocator state/permissions, not voucher serial logic.

## What Was Changed

SQL scripts added under `Areas\Pos\Sql`:

- `53_POS_DoubleEntryVoucherSequence_Diagnostics.sql`
  - Read-only diagnostics for sequence existence, current value, table max, PK columns, recent voucher ids, POS save logs, and possible POS transactions without DEV rows.
- `54_POS_DoubleEntryVoucherSequence_Fix.sql`
  - Takes an application lock.
  - Reads `MAX(Double_Entry_Vouchers_ID)` under `HOLDLOCK, UPDLOCK`.
  - Creates or restarts the sequence with `START/RESTART WITH MAX + 1`.
  - Does not restart an already safe sequence backwards.
  - Provides optional explicit grants for a configured app database user.
- `55_POS_SaveTransaction_Allocator_Hardening.sql`
  - Recreates `dbo.GetNextID_FromSequence`.
  - Uses `WITH EXECUTE AS OWNER`.
  - Uses `sp_getapplock` per table/field.
  - Uses sequence allocation and only uses `MAX + 1` under locks to initialize or repair the sequence, never as an unlocked concurrent fallback.
- `56_POS_RechargeValue_Validation.sql`
  - Read-only helper to find oversized `RechargeValue` rows and recent commission overflow logs.

Application validation added:

- `Areas\Pos\Controllers\PosTransactionController.cs`
  - Rejects oversized commission/save amounts with a clear Arabic validation message.
  - Supports optional `appSettings["PosMaxRechargeValue"]`; default is `1000000`.
- `Areas\Pos\Data\PosSqlRepository.cs`
  - Guards commission range lookups before `Convert.ToInt32`, preventing raw Int32 overflow exceptions.
- `Areas\Pos\Scripts\pos-transaction.js`
  - Adds client-side validation for recharge and violation amounts above the same default limit.

## Applied To Local CASH

Applied:

1. `55_POS_SaveTransaction_Allocator_Hardening.sql`
2. `54_POS_DoubleEntryVoucherSequence_Fix.sql`

Result:

- Repaired sequence with `RestartedWith = 1406551`
- Test allocations returned `1406551`, `1406552`, and `1406573`
- Parallel allocator smoke test returned 20 unique values from `1406553` through `1406572`
- `MAX(Double_Entry_Vouchers_ID)` remained `1406550`
- Re-running the fix reported `NO_CHANGE_ALREADY_SAFE`
- Allocator definition now contains `WITH EXECUTE AS OWNER`, `ANSI_NULLS ON`, and `QUOTED_IDENTIFIER ON`

The allocator tests consumed `1406551` through `1406573`, so the next real save will allocate a higher safe value. Gaps in sequence values are acceptable and safer than collisions.

Additional local verification on 2026-05-09:

- `PosTransactionsWithoutDevLast2Days = 0`
- `OversizedRechargeRows = 0`
- `RecentRelevantErrors = 0`
- Voucher save/edit/post/delete smoke tests passed on `CASH` and `ENG` inside rolled-back transactions.

## What Was Not Changed

- `Voucher_coding`, `DEV_Serial`, and POS serial logic were not changed.
- `AllScripts.sql` was not touched.
- `dbo.usp_POS_SaveTransaction` was not recreated in this fix because the deployed definition already uses `BEGIN TRANSACTION`, `ROLLBACK`, `dbo.GetNextID_FromSequence`, and explicit allocation failure errors for `DOUBLE_ENTREY_VOUCHERS`.

## Production Apply Steps

1. Backup `CASH`.
2. Capture current definitions:
   - `dbo.GetNextID_FromSequence`
   - `dbo.usp_POS_SaveTransaction`
3. Run `53_POS_DoubleEntryVoucherSequence_Diagnostics.sql`.
4. If the app uses a non-dbo SQL user, edit `@AppDbUser` in `54_POS_DoubleEntryVoucherSequence_Fix.sql`.
5. Run `55_POS_SaveTransaction_Allocator_Hardening.sql`.
6. Run `54_POS_DoubleEntryVoucherSequence_Fix.sql`.
7. Run `53_POS_DoubleEntryVoucherSequence_Diagnostics.sql` again.
8. Confirm:
   - sequence exists
   - sequence current value is greater than current max after any test allocation
   - no duplicate PK rows
   - no recent POS save allocation errors

## Verification Queries

```sql
SELECT MAX(CONVERT(BIGINT, Double_Entry_Vouchers_ID)) AS MaxDevId
FROM dbo.DOUBLE_ENTREY_VOUCHERS;

SELECT name, current_value
FROM sys.sequences
WHERE name = N'seq_DOUBLE_ENTREY_VOUCHERS_Double_Entry_Vouchers_ID';

DECLARE @v BIGINT, @e NVARCHAR(500);
EXEC dbo.GetNextID_FromSequence
    @TableName = N'DOUBLE_ENTREY_VOUCHERS',
    @FieldName = N'Double_Entry_Vouchers_ID',
    @NextValue = @v OUTPUT,
    @ErrorMsg = @e OUTPUT;
SELECT @v AS AllocatedValue, @e AS ErrorMsg;
```

## Manual Repair Candidates

The local `POS_SystemErrorLog` did not contain the reported 44 errors for 2026-05-09, so no local invoice list requiring manual repair was found from logs. Use `53_POS_DoubleEntryVoucherSequence_Diagnostics.sql` on the affected production database to list POS transactions from the last two days that have zero `DOUBLE_ENTREY_VOUCHERS` rows.
