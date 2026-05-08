# Kishny POS Approved Feature Scope - 2026-05-08

Baseline: clean release worktree `F:\Source Code\DynamicErp_release_kishny_pos_deadlock_20260508`, built in Release mode. Package output: `F:\Source Code\DynamicErp\Releases\KishnyPOS_ApprovedFeatureRelease_20260508`.

| File/path | Category | Include? | Reason | Dependency risk | SQL required |
|---|---|---:|---|---|---|
| Areas/Pos/Controllers/PosTransactionController.cs | A/B Sales invoice + deadlock UX | Include | Structured save responses, safe busy/deadlock messages, attempt id | Low; POS controller only | 31,47,46,30 |
| Areas/Pos/Data/PosSqlRepository.cs | A/B/J save hardening + reports + monitoring + custody | Include | Deadlock retry, save attempt logging, POS report and custody repository methods | Medium; large POS repository, no MainErp connection used | 27,28,31,32,37,40,45 FI,46,47,48,49 |
| Areas/Pos/Models/PosSaveTransactionRequest.cs | A/B | Include | Adds SaveAttemptId result metadata | Low | No |
| Areas/Pos/Scripts/pos-transaction.js | B Sales invoice UX | Include | Save overlay, duplicate-click prevention, server-busy countdown/retry UX | Low | No |
| Areas/Pos/Content/pos-transaction.css | B Sales invoice UX | Include | Busy retry and validation highlighting styles | Low | No |
| Areas/Pos/Views/PosTransaction/** | B/C | Include | Sales invoice screen/search UI | Low | No |
| Areas/Pos/Views/JournalEntries/** | D | Include | POS journal entry UI | Low; POS route/view only | 33 if audit columns not already present |
| Areas/Pos/Views/Payments/** | E استعاضة العهدة | Include | Approved POS custody replenishment screen | Medium; enabled by `EnablePosCustodyReplenishment=true`, `/Pos/Cashing` remains blocked | 28 |
| Areas/Pos/Views/AccountingReports/** | F | Include | POS accounting reports | Low | 27,45 FI as needed |
| Areas/Pos/Views/PosReports/** and Areas/Pos/Views/HtmlReports/** | G/I | Include | Kishny and operational POS reports | Low | 27,32,39 NonWeb,48,49 as needed |
| Areas/Pos/Views/FinancialIntelligence/** | H | Include | Smart reports / financial intelligence | Low | 45_POS_FinancialIntelligenceReports.sql |
| Areas/Pos/Views/SalesRepresentativesPerformance/** | H/I | Include | Operational sales performance reports | Low | 48 |
| Areas/Pos/Views/SalesTargets/** | H/I | Include | Operational targets reports | Low | 49 |
| Areas/Pos/Controllers/PosSystemErrorLogController.cs | J | Include | Error/save-attempt monitoring | Low | 37,47 |
| Areas/Pos/Models/PosSystemErrorLogModels.cs | J | Include | Error/save-attempt monitoring models | Low | 37,47 |
| Areas/Pos/Views/PosSystemErrorLog/** | J | Include | POS error log and save attempt UI | Low | 37,47 |
| Areas/Pos/Views/PosSystemHealth/** | J | Include | POS-only system monitoring | Low; no password display | 35/37 if missing locally; package uses 37 |
| App_Start/RouteConfig.cs | G | Include | Blocks MainErp, DevStart, RunMode, Excel import, and Cashing by config | Low | No |
| Areas/Pos/PosAreaRegistration.cs | G | Include | POS area exposed only when `EnableKishnyPos=true` | Low | No |
| ConfigTemplates/Web.KishnyPOS.Production.config.example | G | Include | Production-safe flags and DB placeholders | Low | No |
| Areas/MainErp/** | K Exclude MainErp | Exclude | MainErp migration not part of Kishny POS | High if included | No |
| AI_Docs/MainErp/**, AI_Docs/SharedMigration/** | K Exclude MainErp docs | Exclude | Not customer deployment material | High if included | No |
| DevStart/RunMode controllers/routes | L Exclude debug/runmode | Exclude/blocked | Disabled by production config and route gate | High if enabled | No |
| Areas/Pos/Controllers/ExcelImportController.cs and Views/ExcelImport/** | N Exclude Excel import | Exclude | Latest release scope says Excel import is not approved separately | Medium | Excluded 45 Excel, 50 |
| Areas/Pos/Services/PosExcelImport*.cs | N Exclude Excel import | Exclude from compile/package | Prevents Excel import feature from shipping in this package | Medium | No |
| Areas/Pos/Views/Cashing/** and Cashing SQL | M Exclude Payment/Cashing | Exclude | Cashing/payment experiment not approved; custody screen only | High | Excluded 51 |
| PurchaseInvoice / StockTransfer views | O Exclude unrelated/risky for latest scope | Exclude from package | Earlier scope item, not in latest approved list | Medium | Excluded 41/42 |
| Backup files, local Excel files, App_Data uploads | L/O | Exclude | Local/debug/customer-risk artifacts | High | No |
