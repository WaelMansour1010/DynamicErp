# Kishny/POS Reusable Modules Inventory

## Scope

This inventory treats `Areas\Pos` as a reference source only. MainErp must not reference `MyERP.Areas.Pos`, POS login/session restoration, POS permissions, card flows, KYC, commission logic, cashier closing, or POS reports directly.

## Candidate Modules

| Module | POS files inspected | Dependencies found | Classification | Reuse decision | Risk |
| --- | --- | --- | --- | --- | --- |
| Purchases | `Areas\Pos\Controllers\PurchaseInvoiceController.cs`, `Views\PurchaseInvoice\Index.cshtml`, `Scripts\purchase-invoice.js`, `Areas\Pos\Sql\41_POS_PurchaseInvoice.sql`, `PosSqlRepository.SearchPurchaseInvoices`, `GetPurchaseInvoiceDetail`, `SavePurchaseInvoice` | `PosSqlRepository`, `PosUserContext`, `PosLoginController.RestorePosContext`, POS `CanSave`, branch/store/box defaults, Excel import, serial-aware item logic | General ERP business area with POS-coupled implementation | Do not copy controller/service directly. Rebuild under MainErp with read-only search first, then review save procedure separately. | High |
| Stock transfers | `Areas\Pos\Controllers\StockTransferController.cs`, `Views\StockTransfer\Index.cshtml`, `Scripts\stock-transfer.js`, `Areas\Pos\Sql\42_POS_StockTransfer.sql`, `PosSqlRepository.SearchStockTransfers`, `GetStockTransferDetail`, `SaveStockTransfer` | POS session, POS branch forcing, serial import, store defaults, POS permission shape | General ERP business area with POS-coupled implementation | Do not copy save/import. Reuse screen concepts and SQL after schema review. | High |
| Journal entries | `Areas\Pos\Controllers\JournalEntriesController.cs`, `Views\JournalEntries\Index.cshtml`, `PosSqlRepository.SearchJournalEntries`, `GetJournalEntryByNoteId`, `SaveManualJournalEntry` | POS session, POS permission names, POS admin password fallback, write path to Notes/vouchers | General accounting area, mixed with POS security and manual save behavior | Import read-only/search concept only. MainErp implementation must use its own repository and permissions. | Medium |
| Accounting reports | `Areas\Pos\Controllers\AccountingReportsController.cs`, `Views\AccountingReports\Index.cshtml`, `PosSqlRepository.RunAccountingReport` | POS session and POS report permissions; report keys are generic: trial balance, income statement, account statement, general ledger assistant | General ERP reporting area with POS shell/security coupling | Reuse report definitions and Excel/export concept later. First MainErp wave is shell only. | Medium |
| Sales reports | `Areas\Pos\Controllers\PosReportsController.cs`, `Areas\Pos\Controllers\HtmlReportsController.cs`, `Views\PosReports\Index.cshtml`, `Views\HtmlReports\Index.cshtml`, `PosSqlRepository.RunPosReport` | Mixed generic sales reports with POS/Kishny operational reports, store serials, web invoice audit, closing reports | Mixed | Reuse only general sales report names after procedure review. Exclude closing, serial/token/card/KYC/cashier reports. | High |
| Dashboards | `Areas\Pos\Controllers\PosDashboardController.cs`, `Views\PosDashboard\Index.cshtml`, `_Sidebar.cshtml`, `PosSqlRepository.GetAdminDashboardSummary`, `FinancialIntelligenceController.cs`, `PosFinancialIntelligenceRepository.cs`, `financial-intelligence.js` | POS shell, POS screen routing, KYC, closing, payments, system health, print templates, POS performance logger | Mixed | Build MainErp dashboard shell independently. Reuse financial KPI ideas only after query review. | High |
| Financial intelligence | `Areas\Pos\Controllers\FinancialIntelligenceController.cs`, `Views\FinancialIntelligence\*`, `Areas\Pos\Data\PosFinancialIntelligenceRepository.cs`, `Areas\Pos\Sql\45_POS_FinancialIntelligenceReports.sql` | Generic CFO-style methods but stored procedures are `usp_POS_FI_*`; POS branch locking/security | Potentially reusable analytics concepts, not reusable code as-is | Needs rewrite/rename under MainErp SQL after AllScripts/live schema review. | Medium |

## POS-Only Modules To Exclude

- Card/token flows, including card issuance vouchers and card balance reports.
- KYC attachment and KYC bank follow-up.
- Commissions and violations.
- POS transaction service flows and POS invoice save flows.
- Payments tied to POS teller/cashier behavior.
- POS closing, shift closing, cashier settlement, and closing reports.
- POS system health, POS deadlock widgets, POS save-performance, and POS deployment widgets.
- POS receipt layouts and Kishny print templates.

## First Safe Import Decision

The first wave should create MainErp-owned shells and a read-only journal-entry search. It should not copy POS controllers, repositories, scripts, or SQL into MainErp. This preserves isolation while giving MainErp a route/menu foundation for the reusable ERP modules.
