# Kishny POS Approved Feature SQL Apply Plan - 2026-05-08

Target database: Kishny POS customer DB only. Do not apply MainErp SQL. Do not apply Excel import or Cashing SQL.

## Required order
1. `00_BACKUP_BEFORE_APPLY.sql` - backup/helper script before any change.
2. `31_POS_GetNextID_FromSequence_Concurrency.sql` - safe Transaction_ID allocation.
3. `47_POS_SaveAttemptLog.sql` - save attempt logging table/support objects.
4. `46_POS_SaveTransaction_ConcurrencyIndexes.sql` - concurrency/performance indexes for POS save.
5. `30_POS_SaveTransaction_UnicodeText.sql` - Unicode text handling in save procedure.
6. `37_POS_SystemErrorLog.sql` - system error monitoring support, if not already present.
7. `27_POS_ReportStoredProcedures.sql` - Kishny/accounting/operational report procedures.
8. `32_POS_WebInvoiceAuditReport.sql` - operational invoice audit report.
9. `33_POS_JournalEntryAuditColumns.sql` - journal entry audit metadata, if not already present.
10. `28_POS_Payments_Audit.sql` - custody replenishment audit support.
11. `40_POS_Dashboard_DailySnapshots.sql` - monitoring/dashboard report support, if approved for DB size.
12. `45_POS_FinancialIntelligenceReports.sql` - smart/financial intelligence reports.
13. `48_POS_SalesRepresentativesPerformanceDashboard.sql` - operational sales performance report.
14. `49_POS_SalesRepresentativeTargets.sql` - sales targets report.

## Optional/manual
- `39_POS_Deadlock_Diagnostics.sql` - manual diagnostics only; safe to keep for DBA review, not required for normal deployment.
- `39_POS_NonWebLoginUsersReport.sql` - apply only if this operational report is enabled for Kishny.

## Explicitly excluded
- `Areas/MainErp/Sql/*`
- `45_POS_ExcelImport.sql`
- `50_POS_ExcelImportCommitAudit.sql`
- `51_POS_PaymentCashing_ReadProcedures.sql`
- `41_POS_PurchaseInvoice.sql`
- `42_POS_StockTransfer.sql`
- SQL backup files

## Rollback
Before apply, capture old definitions of `dbo.GetNextID_FromSequence`, `dbo.usp_POS_SaveTransaction`, report procedures touched by script 27/32/45/48/49, and current indexes listed by the helper script. Rollback is restore old procedure definitions and only drop newly-created indexes/tables if they cause a confirmed issue.
