# Kishny POS Release Review - 2026-05-08

## Scope
- Source reviewed: `F:\Source Code\DynamicErp`
- Customer target: `C:\WWWSite\cayshny\`
- Commands reviewed: `git status --short`, `git diff --stat`, `git diff --name-only`, `git ls-files --others --exclude-standard`
- Decision: **NO-GO for direct deploy from current worktree**. The worktree mixes POS fixes, POS risky feature work, MainErp migration, local debug config, backup files, and uploaded Excel data.

## Category Key
- A: Kishny/POS production-safe
- B: Kishny/POS deadlock/save hardening
- C: Kishny/POS UI/report fixes
- D: MainErp migration only
- E: debug/run-mode/local-only
- F: unrelated/risky/hold-back
- G: config/deployment-sensitive

## Changed File Classification

| File path | Category | Reason | Decision | Risk |
|---|---|---|---|---|
| `AI_Docs/MainErp/06_MainErp_Implementation_Log.md` | D | MainErp implementation notes only. | Hold | Low |
| `AI_Docs/MainErp/31_LC_GridVoucherMapping.md` | D | MainErp LC documentation. | Hold | Low |
| `AI_Docs/MainErp/40_SalesInvoice_AccountingInventoryFlow.md` | D | MainErp sales invoice notes. | Hold | Low |
| `AI_Docs/MainErp/49_LC_SaveEditAndOpeningVoucher.md` | D | New MainErp LC doc. | Hold | Low |
| `AI_Docs/MainErp/50_PumpWorkshopSales_CurrentGapsAndCustomerDeferred.md` | D | MainErp pump/workshop doc. | Hold | Low |
| `AI_Docs/MainErp/51_PumpSales_FullDraftSave.md` | D | MainErp pump sales doc. | Hold | Low |
| `AI_Docs/MainErp/52_PumpSales_PostingInventoryAudit.md` | D | MainErp pump sales doc. | Hold | Low |
| `AI_Docs/MainErp/53_PumpSales_DraftDeleteSafety.md` | D | MainErp pump sales doc. | Hold | Low |
| `AI_Docs/MainErp/54_PumpSales_PermissionsCancelReceiveCostPG.md` | D | MainErp pump sales doc. | Hold | Low |
| `AI_Docs/MainErp/55_PumpSales_InventoryCostAndAudit.md` | D | MainErp pump sales doc. | Hold | Low |
| `AI_Docs/MainErp/56_PumpSales_CostPostingCancelReceive.md` | D | MainErp pump sales doc. | Hold | Low |
| `AI_Docs/MainErp/57_LC_CompletionPass.md` | D | MainErp LC doc. | Hold | Low |
| `AI_Docs/MainErp/58_LC_GridVoucherPosting.md` | D | MainErp LC doc. | Hold | Low |
| `AI_Docs/SharedMigration/*` | D | Shared MainErp/payment/cashing migration research. | Hold | Low |
| `App_Data/PosExcelImports/*.xlsx` | E | Local uploaded/import test data. | Hold | High |
| `Areas/MainErp/**` | D | MainErp controllers, repositories, SQL, views, CSS, security, LC/pump/payment/cashing migration. | Hold | High |
| `Areas/Pos/Content/pos-transaction.css` | C | POS visual/login/transaction UI changes. | Needs review | Medium |
| `Areas/Pos/Controllers/ExcelImportController.cs` | F | Large Excel import feature, not required for deadlock release. | Hold | High |
| `Areas/Pos/Controllers/HtmlReportsController.cs` | C | Report/export presentation changes. | Needs review | Medium |
| `Areas/Pos/Controllers/PaymentsController.cs` | F | Payment voucher changes share MainErp models/repositories. | Hold | High |
| `Areas/Pos/Controllers/CashingController.cs` | F | New cashing screen; depends on migrated payment/cashing logic. | Hold | High |
| `Areas/Pos/Controllers/PosDashboardController.cs` | C | Dashboard/sidebar/report adjustments. | Needs review | Medium |
| `Areas/Pos/Controllers/PosReportsController.cs` | C | Report fixes. | Needs review | Medium |
| `Areas/Pos/Controllers/PosTransactionController.cs` | B/F | Includes IPN duplicate validation and admin delete/Excel delete flows. Deadlock-safe portions are indirect; delete features are risky. | Needs split | High |
| `Areas/Pos/Data/PosSqlRepository.cs` | B/F | Contains deadlock retry/logging, but also Excel import, delete invoice, report, payment/cashing support. | Needs split | High |
| `Areas/Pos/Models/PosExcelImportModels.cs` | F | Excel import feature. | Hold | High |
| `Areas/Pos/Models/PosSaveTransactionRequest.cs` | B/F | Save request additions support POS save/import; verify required fields before shipping. | Needs review | Medium |
| `Areas/Pos/PosAreaRegistration.cs` | G/F | POS route changes may expose new payment/cashing/import routes. | Needs review | High |
| `Areas/Pos/Scripts/pos-transaction.js` | C/F | UI save validation and admin delete/Excel delete UI. | Needs split | High |
| `Areas/Pos/Services/PosExcelImport*.cs` | F | Excel import/preflight/commit/marker feature. | Hold | High |
| `Areas/Pos/Services/PosLegacyScreenPermissionService.cs` | F | Shared legacy screen permission support for new voucher screens. | Hold | Medium |
| `Areas/Pos/Sql/27_POS_ReportStoredProcedures.sql` | C | Report stored procedure updates. | Needs review | Medium |
| `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql` | B | Save procedure, Transaction_ID allocation and IPN duplicate hardening. | Ship after DB backup/review | Medium |
| `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText_Backup_20260507_1918.sql` | E | Backup copy. | Hold | High |
| `Areas/Pos/Sql/31_POS_GetNextID_FromSequence_Concurrency.sql` | B | Concurrency-safe sequence/id allocation using `sp_getapplock`. | Ship after DB backup/review | Medium |
| `Areas/Pos/Sql/39_POS_Deadlock_Diagnostics.sql` | B | Diagnostic read script for deadlocks. | Optional | Low |
| `Areas/Pos/Sql/46_POS_SaveTransaction_ConcurrencyIndexes.sql` | B | Narrow indexes to reduce save lock duration. | Ship after index impact review | Medium |
| `Areas/Pos/Sql/47_POS_SaveAttemptLog.sql` | B | Save retry/audit logging table and indexes. | Ship | Low |
| `Areas/Pos/Sql/50_POS_ExcelImportCommitAudit.sql` | F | Excel import audit. | Hold | High |
| `Areas/Pos/Sql/51_POS_PaymentCashing_ReadProcedures.sql` | F | Payment/cashing reads. | Hold | High |
| `Areas/Pos/Views/ExcelImport/*` | F | Excel import UI, plus backup files. | Hold | High |
| `Areas/Pos/Views/Payments/*` | F | Payment voucher UI. | Hold | High |
| `Areas/Pos/Views/Cashing/*` | F | Cashing UI. | Hold | High |
| `Areas/Pos/Views/Shared/_VoucherScreenStyles.cshtml` | F | Shared migrated voucher UI. | Hold | Medium |
| `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml` | C/G | Menu/sidebar may expose new screens. | Needs review | High |
| `Areas/Pos/Views/PosReports/Index.cshtml` | C | Report UI changes. | Needs review | Medium |
| `Areas/Pos/Views/PosTransaction/Index.cshtml` | C/F | POS transaction UI plus delete/import hooks. | Needs split | High |
| `Areas/Pos/AI_Docs/*` | A/F | POS notes, some about Excel import. | Hold from deployment | Low |
| `Excel/*.xlsx` | E | Local test/source spreadsheets. | Hold | High |
| `MyERP.csproj` | G/D/F | Adds MainErp files and POS risky feature files to build/content. | Hold or create clean release csproj/package | Critical |
| `MyERP_Backup_20260507_1903.csproj` | E | Local backup project file. | Hold | High |

## Safe Candidate Set
Only these look related to the requested POS deadlock/save hardening release:
- `Areas/Pos/Data/PosSqlRepository.cs` **only the retry/logging portions**
- `Areas/Pos/Models/PosSystemErrorLogModels.cs` if changed in release branch and needed for log UI
- `Areas/Pos/Controllers/PosSystemErrorLogController.cs` if changed in release branch and needed for log UI
- `Areas/Pos/Views/PosSystemErrorLog/Index.cshtml` if changed in release branch and needed for log UI
- `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql`
- `Areas/Pos/Sql/31_POS_GetNextID_FromSequence_Concurrency.sql`
- `Areas/Pos/Sql/39_POS_Deadlock_Diagnostics.sql`
- `Areas/Pos/Sql/46_POS_SaveTransaction_ConcurrencyIndexes.sql`
- `Areas/Pos/Sql/47_POS_SaveAttemptLog.sql`

## Hold Back Set
- All `Areas/MainErp/**`
- All `AI_Docs/MainErp/**` and `AI_Docs/SharedMigration/**`
- All `App_Data/PosExcelImports/**` and `Excel/**`
- All `*_Backup_202605*.cs`, `*_Backup_202605*.cshtml`, `*_Backup_202605*.sql`, `MyERP_Backup_*.csproj`
- POS Excel import, payment/cashing, admin invoice delete, route/menu expansion, and project-file additions unless explicitly approved.

## Release Gate Result
Direct deployment from current worktree: **NO-GO**.

Required before deployment:
1. Create a clean POS release branch/package containing only approved POS save/deadlock files.
2. Remove or exclude MainErp/debug/local files from the customer package.
3. Use a production Web.config/template; never copy current root `Web.config`.
4. Run build and smoke tests from the clean package, not from the mixed tree.
