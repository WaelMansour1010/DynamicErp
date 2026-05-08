# Kishny POS Feature Release SQL Apply Plan - 20260508

Target: Kishny POS database only. Do not apply any `Areas/MainErp/Sql` script.

## Required Order

1. `00_BACKUP_BEFORE_APPLY.sql` - capture current procedure/index/table state before changes.
2. `31_POS_GetNextID_FromSequence_Concurrency.sql` - concurrency-safe id allocation.
3. `47_POS_SaveAttemptLog.sql` - save attempt logging table/procedure support.
4. `46_POS_SaveTransaction_ConcurrencyIndexes.sql` - indexes for POS save concurrency.
5. `30_POS_SaveTransaction_UnicodeText.sql` - save procedure text/unicode hardening.
6. `27_POS_ReportStoredProcedures.sql` - Kishny POS report procedures, if report changes are approved for this customer update.
7. `41_POS_PurchaseInvoice.sql` - purchase invoice procedures, if not already applied.
8. `42_POS_StockTransfer.sql` - stock transfer procedures, if not already applied.
9. `45_POS_ExcelImport.sql` - Excel import support/preflight objects, if Excel import is approved.
10. `50_POS_ExcelImportCommitAudit.sql` - Excel import commit audit/batch objects.

Optional/manual only: `39_POS_Deadlock_Diagnostics.sql` for diagnostics during monitoring; do not schedule as a mandatory deploy script.

## Backup Notes

Back up the full customer database before applying. At minimum, script current definitions of `dbo.GetNextID_FromSequence`, `dbo.usp_POS_SaveTransaction`, report/purchase/transfer/Excel procedures, existing indexes touched by script 46, and existing `POS_SaveAttemptLog`, `POS_ImportBatch`, `POS_ImportBatchRow` objects if present.

## Compatibility

Scripts are T-SQL for SQL Server; review on customer version before apply. No MainErp SQL is included. Payment/cashing SQL is intentionally excluded.

## Rollback

Preferred rollback is full DB restore. If point restore is unavailable, restore pre-apply procedure definitions and remove/disable new indexes only after confirming they caused the issue. Keep save/import logs exported before any cleanup.
