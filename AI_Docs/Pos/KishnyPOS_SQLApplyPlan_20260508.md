# Kishny POS SQL Apply Plan - 2026-05-08

## Rule
Do not apply any `Areas/MainErp/Sql` script to the Kishny POS customer database.

## Required for Deadlock/Save Release

| Script | Purpose | Required on customer | Safety | Rollback note | Compatibility |
|---|---|---:|---|---|---|
| `Areas/Pos/Sql/31_POS_GetNextID_FromSequence_Concurrency.sql` | Recreates `dbo.GetNextID_FromSequence` with `sp_getapplock` and sequence allocation. | Yes | Medium | Backup old procedure definition first; restore old proc if issues occur. | SQL Server 2012+ due to sequences. |
| `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql` | Recreates `dbo.usp_POS_SaveTransaction`; includes allocation hardening and duplicate IPN checks. | Yes | Medium/High | Backup old procedure definition first; restore old proc if save regressions occur. | SQL Server 2012+ if dependent sequence proc is used. |
| `Areas/Pos/Sql/46_POS_SaveTransaction_ConcurrencyIndexes.sql` | Adds narrow indexes to reduce lock duration during save/edit/delete paths. | Recommended | Medium | Use `Areas/Pos/Sql/36_POS_PerformanceIndexRollback.sql` only after confirming it drops matching indexes, or manually drop added indexes. | Script states SQL Server 2012 compatible. |
| `Areas/Pos/Sql/47_POS_SaveAttemptLog.sql` | Creates `dbo.POS_SaveAttemptLog` and supporting indexes. | Yes if retry/log UI is shipped | Low | Table can remain harmless; rollback by dropping table only after exporting needed logs. | SQL Server 2012 compatible. |
| `Areas/Pos/Sql/39_POS_Deadlock_Diagnostics.sql` | Read/diagnostic script for deadlocks. | Optional/manual only | Low | No persistent changes expected. | SQL Server 2012 compatible. |

## Hold Back SQL
- `Areas/Pos/Sql/27_POS_ReportStoredProcedures.sql`: report changes; not part of deadlock release unless report fixes are separately approved.
- `Areas/Pos/Sql/50_POS_ExcelImportCommitAudit.sql`: Excel import feature; hold.
- `Areas/Pos/Sql/51_POS_PaymentCashing_ReadProcedures.sql`: payment/cashing feature; hold.
- `Areas/Pos/Sql/45_*`, `48_*`, `49_*`, dashboard/performance/server tuning scripts: hold unless included in a separate approved release.
- Any backup SQL file: hold.
- All `Areas/MainErp/Sql/*`: hold.

## Before Applying
1. Backup full customer database, or at minimum script out:
   - `dbo.GetNextID_FromSequence`
   - `dbo.usp_POS_SaveTransaction`
   - existing indexes on `DOUBLE_ENTREY_VOUCHERS`, `Notes`, `Transaction_Details`, `TblSalesPayment`
2. Confirm SQL Server version is 2012+.
3. Confirm app SQL user has permission to execute `sp_getapplock`, create/alter procedures, create sequence, create table, and create indexes during deployment.
4. Run scripts first on a restored/customer-like test database.

## Apply Order
1. `31_POS_GetNextID_FromSequence_Concurrency.sql`
2. `47_POS_SaveAttemptLog.sql`
3. `46_POS_SaveTransaction_ConcurrencyIndexes.sql`
4. `30_POS_SaveTransaction_UnicodeText.sql`
5. Optional: run `39_POS_Deadlock_Diagnostics.sql` after smoke testing.

## Applied Locally
Not verified. I did not apply scripts to any database.
