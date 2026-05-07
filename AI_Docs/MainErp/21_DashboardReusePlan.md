# Dashboard Reuse Plan

## Dashboard Sources Reviewed

- `Areas\Pos\Controllers\PosDashboardController.cs`
- `Areas\Pos\Views\PosDashboard\Index.cshtml`
- `Areas\Pos\Views\PosDashboard\_Sidebar.cshtml`
- `Areas\Pos\Controllers\FinancialIntelligenceController.cs`
- `Areas\Pos\Data\PosFinancialIntelligenceRepository.cs`
- `Areas\Pos\Scripts\financial-intelligence.js`

## Reusable For MainErp

- Sales totals and trend cards when based on general sales invoices, not POS card/service transactions.
- Purchase totals and inventory movement metrics after purchase/stock schemas are reviewed.
- Accounting balances and trial-balance style indicators.
- Branch performance if it is branch/company neutral.
- User activity if not cashier/session specific.
- CFO financial KPI ideas from `FinancialIntelligenceController` such as cash flow, expenses, branch performance, abnormal journal detection, and root cause analysis.

## Not Reusable

- POS shell screen switching.
- POS session restore and teller/default forcing.
- KYC and KYC bank follow-up widgets.
- POS closing, cashier settlement, and shift closing widgets.
- Card/token distribution or card balance widgets.
- Commission and violation widgets.
- POS system health, POS deadlock, POS save-performance, and POS deployment widgets.
- Print template shortcuts tied to Kishny/POS receipts.

## Implementation Plan

Phase 1 creates `MainErp/Dashboard` as a boardroom-style shell with only neutral module cards. It displays migration readiness and links to MainErp-owned pages. No POS endpoint, POS repository, or POS stored procedure is called.

Future dashboard data should be served by MainErp repositories with MainErp permissions and SQL under `Areas\MainErp\Sql`, after comparing legacy source and `AllScripts.sql`.
