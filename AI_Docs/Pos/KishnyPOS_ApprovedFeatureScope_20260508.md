# Kishny POS Approved Feature Scope - 20260508

Baseline/package source: clean worktree `F:\Source Code\DynamicErp_release_kishny_pos_deadlock_20260508`, based on `release/kishny-pos-deadlock-20260508` with POS-only feature additions copied from `F:\Source Code\DynamicErp`.

| Path | Category | Include/Exclude | Reason | Dependency risk | SQL required |
|---|---|---:|---|---|---|
| App_Start/RouteConfig.cs | G - route/config safety | Include | Blocks MainErp, DevStart, RunMode, and payment/cashing when production flags are false. | Low | No |
| Areas/MainErp/** | F - MainErp | Exclude from package content | Migration module not approved for Kishny POS. Route registration blocked by `EnableMainErpMigration=false`. | High if exposed | No |
| Areas/Pos/PosAreaRegistration.cs | A - POS route safety | Include in build | POS area controlled by `EnableKishnyPos`. | Low | No |
| Areas/MainErp/MainErpAreaRegistration.cs | F - MainErp isolation | Build only | MainErp area returns without registering unless enabled. No `Areas/MainErp` content is packaged. | Low after config | No |
| Areas/Pos/Data/PosSqlRepository.cs | A/E - save hardening + Excel support | Include | Contains KishnyCashConnection repository, save retry logging, Excel preflight/commit audit helpers, purchases/transfers/report helpers. | Medium: contains unused Excel rollback/delete helper internals; no admin delete controller/view shipped. | Yes |
| Areas/Pos/Controllers/PosTransactionController.cs | A - deadlock/save hardening | Include build/package views only | Save path uses repository hardening; admin delete UI/actions were not added to package scope. | Medium | 30,31,46,47 |
| Areas/Pos/Models/PosSystemErrorLogModels.cs | A - deadlock/save log UI | Include | Supports SaveAttemptLog viewing. | Low | 47 |
| Areas/Pos/Controllers/PosSystemErrorLogController.cs | A - deadlock/save log UI | Include | POS-only controller; reads POS logs. | Low | 47 |
| Areas/Pos/Views/PosSystemErrorLog/Index.cshtml | A - deadlock/save log UI | Include | Customer can inspect save attempt/deadlock logs. | Low | 47 |
| Areas/Pos/Controllers/PurchaseInvoiceController.cs | B - Kishny purchases | Include | POS area only; repository uses KishnyCashConnection. | Low | 41 |
| Areas/Pos/Views/PurchaseInvoice/Index.cshtml | B - Kishny purchases | Include | POS-only purchase UI. | Low | 41 |
| Areas/Pos/Scripts/purchase-invoice.js | B - Kishny purchases | Include | POS-only purchase screen script. | Low | 41 |
| Areas/Pos/Controllers/StockTransferController.cs | C - Kishny stock transfers | Include | POS area only; repository uses KishnyCashConnection. | Low | 42 |
| Areas/Pos/Views/StockTransfer/Index.cshtml | C - Kishny stock transfers | Include | POS-only stock transfer UI. | Low | 42 |
| Areas/Pos/Scripts/stock-transfer.js | C - Kishny stock transfers | Include | POS-only transfer screen script. | Low | 42 |
| Areas/Pos/Controllers/HtmlReportsController.cs | D - Kishny reports | Include | POS report controller; no MainErp connection found. | Low | 27 |
| Areas/Pos/Controllers/PosReportsController.cs | D - Kishny reports | Include | POS reports page; no MainErp dependency found. | Low | 27 |
| Areas/Pos/Views/HtmlReports/** | D - Kishny reports | Include | POS report UI. | Low | 27 |
| Areas/Pos/Views/PosReports/Index.cshtml | D - Kishny reports | Include | POS report menu/page. | Low | 27 |
| Areas/Pos/Controllers/ExcelImportController.cs | E - Kishny Excel import | Include | POS-only controller; upload path is `~/App_Data/PosExcelImports`; no local hardcoded paths found. | Medium: commit/rollback must be tested on safe DB only. | 45,50 |
| Areas/Pos/Models/PosExcelImportModels.cs | E - Kishny Excel import | Include | Preview/commit/progress models. | Low | 45,50 |
| Areas/Pos/Models/PosSaveTransactionRequest.cs | E/A shared POS models | Include | Adds `CanImportExcel` and branch code fields needed by Excel preflight; POS-only. | Medium: contains unused delete DTOs, but no admin delete endpoint/view shipped. | 50 |
| Areas/Pos/Services/PosExcelImportParser.cs | E - Kishny Excel import | Include | Parses workbook and validates rows. | Low | 45 |
| Areas/Pos/Services/PosExcelImportPreflightService.cs | E - Kishny Excel import | Include | Applies POS default/preflight checks through repository. | Low | 45 |
| Areas/Pos/Services/PosExcelImportCommitService.cs | E - Kishny Excel import | Include | Commits approved rows through existing POS save path. | Medium: DB-write path requires safe DB smoke before customer deploy. | 50 |
| Areas/Pos/Services/PosExcelImportWorkbookMarker.cs | E - Kishny Excel import | Include | Writes marked workbook only under App_Data work folder. | Low | No |
| Areas/Pos/Views/ExcelImport/Index.cshtml | E - Kishny Excel import | Include | POS-only upload screen. | Low | 45,50 |
| Areas/Pos/Views/ExcelImport/Preview.cshtml | E - Kishny Excel import | Include | POS-only preview/commit progress screen. | Medium | 45,50 |
| Areas/Pos/Sql/31_POS_GetNextID_FromSequence_Concurrency.sql | A - deadlock/save | Include SQL | Safe transaction id allocation. | Medium | Apply |
| Areas/Pos/Sql/47_POS_SaveAttemptLog.sql | A - save logging | Include SQL | Creates/updates save attempt log support. | Low | Apply |
| Areas/Pos/Sql/46_POS_SaveTransaction_ConcurrencyIndexes.sql | A - save concurrency | Include SQL | Adds save-path indexes. | Medium | Apply |
| Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql | A - save procedure | Include SQL | Save procedure text/unicode hardening. | Medium | Apply |
| Areas/Pos/Sql/27_POS_ReportStoredProcedures.sql | D - reports | Include SQL | Report stored procedure updates. | Medium | Apply if reports approved |
| Areas/Pos/Sql/41_POS_PurchaseInvoice.sql | B - purchases | Include SQL | Purchase screen procedures. | Medium | Apply if not already present |
| Areas/Pos/Sql/42_POS_StockTransfer.sql | C - stock transfers | Include SQL | Transfer screen procedures. | Medium | Apply if not already present |
| Areas/Pos/Sql/45_POS_ExcelImport.sql | E - Excel import | Include SQL | Excel import support/preflight objects. | Medium | Apply if Excel feature approved |
| Areas/Pos/Sql/50_POS_ExcelImportCommitAudit.sql | E - Excel import audit | Include SQL | Import batch/row audit support. | Medium | Apply |
| Areas/Pos/Sql/39_POS_Deadlock_Diagnostics.sql | A - diagnostics | Optional/manual | Diagnostics only, not required for normal deploy. | Low if manual | Optional |
| Areas/Pos/Views/Payments/**, Areas/Pos/Views/Cashing/** | H - payment/cashing | Exclude | Not approved in this feature release. Routes return 404 when `EnablePosPaymentsCashing=false`. | High if exposed | No |
| Areas/Pos/Sql/51_POS_PaymentCashing_ReadProcedures.sql | H - payment/cashing | Exclude | Payment/cashing release not approved. | High | No |
| DevStart/RunMode files/routes | G - debug/runmode | Exclude/block | Production config disables route exposure. | High if enabled | No |
| Excel/** and App_Data/PosExcelImports uploaded files | G - local data | Exclude | Local uploads/data must not ship. | High | No |
| *_Backup_*, Backup_*, *.bak, *.xlsx, *.xls | G - local/backup | Exclude | No local backups or workbook data in package. | High | No |
