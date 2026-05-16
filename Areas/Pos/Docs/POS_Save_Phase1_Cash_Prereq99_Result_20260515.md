# POS Save Phase 1 Cash Prereq 99 Result - 2026-05-15

## Result

Prerequisite script `99_POS_VoucherSerialScope.sql` was applied successfully to `Cash`.

Status: **Cash is now ready for script 100 testing**, with one note: after script `99`, `POSVoucherSerialScope` remains `Company` by design. Script `100_POS_SaveTransaction_Phase1_LockWindow.sql` is the next planned step that sets the scope to `Branch` for the Phase 1 test.

No script `100` was applied. No mutating POS save/load tests were run.

## DB / Server

- SQL Server: `Wael\Sql2019`
- Database: `Cash`
- Connection source: `Web.config`, `KishnyCashConnection`

## Backup Reference

Existing verified backup from the stopped pre-100 test:

```text
C:\Program Files\Microsoft SQL Server\MSSQL15.SQL2019\MSSQL\Backup\Cash_Phase1_Pre100_20260515_223224.bak
```

This backup was taken with `COPY_ONLY`, `COMPRESSION`, and `CHECKSUM`, then verified with `RESTORE VERIFYONLY`.

## Script Applied

```text
F:\Source Code\DynamicErp\Areas\Pos\Sql\99_POS_VoucherSerialScope.sql
```

Applied to:

```text
Wael\Sql2019 / Cash
```

Apply result:

- `sqlcmd` completed with exit code `0`.
- No SQL errors were returned.

## Safety / Idempotency Review

Script `99` was read before execution.

It is safe/idempotent for this prerequisite because it:

- Adds `dbo.TblOptions.POSVoucherSerialScope` only when the column is missing.
- Sets `POSVoucherSerialScope = N'Company'` only for NULL/blank existing values.
- Drops and recreates `dbo.usp_GetNextSerial_V2` using DROP + CREATE.
- Does not mutate `Transactions`, `Notes`, or `SerialCounters_V2` during script apply.
- Does not create or consume voucher numbers during script apply.
- Preserves serial format and keeps default behavior as Company until explicitly changed.

Important:

- The script references `SerialCounters_V2`, `SerialTableMapping`, and `sanad_numbering`, but does not create them.
- These related objects already existed on `Cash`.

## Before State

Captured in:

```text
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\02_before_99_state.txt
```

Key before results:

| Check | Before |
| --- | --- |
| `TblOptions.POSVoucherSerialScope` | Missing |
| `TblOptions` column count | 395 |
| `dbo.usp_GetNextSerial_V2` | Existed |
| `dbo.usp_GetNextSerial_V2` scope logic | Not present |
| `dbo.SerialCounters_V2` | Existed |
| `dbo.SerialTableMapping` | Existed |
| `dbo.sanad_numbering` | Existed |

Before procedure metadata:

| Item | Value |
| --- | --- |
| `usp_GetNextSerial_V2` create date | `2026-02-05 00:52:15.427` |
| `usp_GetNextSerial_V2` modify date | `2026-02-05 00:52:15.427` |
| Definition bytes | `23846` |
| First-4000 checksum | `-1877599852` |

## After State

Captured in:

```text
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\03_after_99_state.txt
```

Key after results:

| Check | After |
| --- | --- |
| `TblOptions.POSVoucherSerialScope` | Exists |
| Current `POSVoucherSerialScope` value | `Company` |
| Column type | `nvarchar(20)` |
| Column nullable | Yes |
| Default constraint | `DF_TblOptions_POSVoucherSerialScope` |
| Default value | `N'Company'` |
| `TblOptions` column count | 396 |
| `dbo.usp_GetNextSerial_V2` | Exists, recreated |
| `dbo.usp_GetNextSerial_V2` scope logic | Present |
| `dbo.SerialCounters_V2` | Exists |
| `dbo.SerialTableMapping` | Exists |
| `dbo.sanad_numbering` | Exists |

After procedure metadata:

| Item | Value |
| --- | --- |
| `usp_GetNextSerial_V2` create date | `2026-05-15 22:40:01.503` |
| `usp_GetNextSerial_V2` modify date | `2026-05-15 22:40:01.503` |
| Definition bytes | `29868` |
| First-4000 checksum | `-933432510` |
| Scope logic check | `1` |

Sanad numbering verification:

| Sanad | Min StoreCoding | Max StoreCoding |
| ---: | ---: | ---: |
| 7 | 0 | 0 |
| 10 | 0 | 0 |

This confirms the next Branch-scope test should effectively use Branch scope, not BranchStore, for Kishny POS voucher coding.

## Verification Queries / Results

Column existence:

```sql
SELECT HasPOSVoucherSerialScope =
    CASE WHEN COL_LENGTH(N'dbo.TblOptions', N'POSVoucherSerialScope') IS NULL THEN 0 ELSE 1 END;
```

Result:

```text
1
```

Current option:

```sql
SELECT TOP (1) POSVoucherSerialScope
FROM dbo.TblOptions WITH (READCOMMITTEDLOCK);
```

Result:

```text
Company
```

Procedure/object checks:

```sql
SELECT OBJECT_ID(N'dbo.usp_GetNextSerial_V2', N'P');
SELECT OBJECT_ID(N'dbo.SerialCounters_V2', N'U');
SELECT OBJECT_ID(N'dbo.SerialTableMapping', N'U');
SELECT OBJECT_ID(N'dbo.sanad_numbering', N'U');
```

Result:

- `dbo.usp_GetNextSerial_V2`: exists.
- `dbo.SerialCounters_V2`: exists.
- `dbo.SerialTableMapping`: exists.
- `dbo.sanad_numbering`: exists.

Scope logic:

```sql
SELECT HasScopeLogic =
    CASE
        WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_GetNextSerial_V2')) LIKE N'%POSVoucherSerialScope%'
         AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_GetNextSerial_V2')) LIKE N'%EffectiveScope%'
        THEN 1 ELSE 0
    END;
```

Result:

```text
1
```

## Unrelated Schema Change Check

Observed expected changes only:

- `TblOptions` column count changed from `395` to `396`.
- Added column: `POSVoucherSerialScope`.
- Recreated procedure: `dbo.usp_GetNextSerial_V2`.

No POS saves were performed. No voucher numbers were consumed by this step. No `SerialCounters_V2` rows were updated by applying script `99`.

## Current Value Correctness

Current value after script `99`:

```text
Company
```

This is correct for the prerequisite-only step because script `99` intentionally preserves backward-compatible behavior until the reviewed rollout changes the setting.

For Phase 1 testing, script `100_POS_SaveTransaction_Phase1_LockWindow.sql` is expected to set:

```sql
UPDATE dbo.TblOptions
SET POSVoucherSerialScope = N'Branch';
```

Do not manually change the value before script `100` unless the deployment plan is updated.

## Readiness For Script 100 Testing

Cash is now ready for the next controlled test step:

```text
Areas\Pos\Sql\100_POS_SaveTransaction_Phase1_LockWindow.sql
```

Prerequisites now satisfied:

- Backup exists and was verified.
- `TblOptions.POSVoucherSerialScope` exists.
- `dbo.usp_GetNextSerial_V2` exists with scope logic.
- `SerialCounters_V2` exists.
- `SerialTableMapping` exists.
- `sanad_numbering` exists.
- `Sanad_No = 7` and `10` have `StoreCoding = 0`.

## Rollback Notes

If rolling back script `99` only:

- The safest behavioral rollback is to keep the new procedure but set:

```sql
UPDATE dbo.TblOptions
SET POSVoucherSerialScope = N'Company';
```

- Full schema rollback would require restoring from the verified backup or explicitly removing the added column and restoring the previous `dbo.usp_GetNextSerial_V2` definition. That is not recommended unless required, because the default `Company` setting preserves old behavior.

Backup available:

```text
C:\Program Files\Microsoft SQL Server\MSSQL15.SQL2019\MSSQL\Backup\Cash_Phase1_Pre100_20260515_223224.bak
```

