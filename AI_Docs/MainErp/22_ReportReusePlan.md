# Report Reuse Plan

## Report Sources Reviewed

- `Areas\Pos\Controllers\AccountingReportsController.cs`
- `Areas\Pos\Controllers\HtmlReportsController.cs`
- `Areas\Pos\Controllers\PosReportsController.cs`
- `Areas\Pos\Views\AccountingReports\Index.cshtml`
- `Areas\Pos\Views\HtmlReports\Index.cshtml`
- `Areas\Pos\Views\PosReports\Index.cshtml`
- `Areas\Pos\Sql\27_POS_ReportStoredProcedures.sql`
- `Areas\Pos\Sql\32_POS_WebInvoiceAuditReport.sql`
- `Areas\Pos\Sql\34_POS_PerformanceStoredProcedures.sql`
- `Areas\Pos\Sql\45_POS_FinancialIntelligenceReports.sql`

## Reusable Accounting Reports

- Trial balance.
- Income statement.
- Account statement.
- General ledger assistant.
- Journal-entry listing/search.
- Excel export pattern, after moving it to a neutral MainErp helper or keeping a local MainErp implementation.

## Reusable Sales Reports After Review

- General sales report.
- Comprehensive sales reports when not limited to POS services/cards.
- Branch/user sales summaries if based on main ERP sales tables.
- Source distribution only if sources are main ERP neutral.

## Excluded Reports

- Card/token reports.
- KYC reports.
- Commission reports.
- Cashier closing and shift reports.
- POS receipt layouts.
- POS web invoice audit reports.
- Store serial reports tied to POS operational inventory assumptions.
- POS health/deadlock/performance reports.
- Reports using POS-only permissions or `PosUserContext`.

## Technical Method

Accounting report definitions can be copied conceptually, but MainErp must use its own controllers, permissions, and SQL. Existing POS stored procedures are not imported as-is because their names, branch filters, and security context are POS-oriented.

Phase 1 created an Accounting Reports shell and a Sales Reports shell under `Areas\MainErp`.

The first working read-only report wave now implements:

- `/MainErp/AccountingReports/JournalEntries`
- `/MainErp/AccountingReports/AccountMovement`
- `/MainErp/SalesReports/SalesSummary`

These reports use inline parameterized read-only SQL against the configured MainErp database. They do not import POS stored procedures and do not create SQL objects.
