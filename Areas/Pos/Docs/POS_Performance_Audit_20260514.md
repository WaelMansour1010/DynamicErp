# POS Performance Audit - 20260514

Scope: `Areas\Pos` only. Database checked through the configured `KishnyCashConnection` against the local `Cash` database on 2026-05-14.

## Live Baseline

| Table | Approx rows |
| --- | ---: |
| `Transactions` | 1,248,549 |
| `Transaction_Details` | 2,014,078 |
| `Notes` | 1,336,415 |
| `DOUBLE_ENTREY_VOUCHERS` | 7,227,939 |
| `TBLClosePos` | 53,631 |
| `TblBranchesData` | 115 |
| `TblStore` | 115 |
| `TblUsers` | 139 |

Verified large-table columns used by POS reports/closing: `Transactions.Transaction_Date`, `Transactions.Transaction_Type`, `Transactions.BranchId`, `Transactions.UserID`, `Transaction_Details.Transaction_ID`, `Transaction_Details.Item_ID`, `Transaction_Details.ItemSerial`, `TBLClosePos.OrderDate`, `TBLClosePos.BranchID`, `TBLClosePos.UserID`, `Notes.NoteDate`, `Notes.NoteType`, `DOUBLE_ENTREY_VOUCHERS.Notes_ID`, and `DOUBLE_ENTREY_VOUCHERS.RecordDate`.

## Affected Screens And Routes

| Screen / route | Controller/action | View / JS | SQL path | Current defaults | Loads on page open? | Row-volume risk | First recommended fix |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `/Pos/PosClosing/Index` | `PosClosingController.Index`, `LoadValues`, `ExecuteClosing` | `Views\PosClosing\Index.cshtml`, `Scripts\pos-closing.js` | Inline SQL in `Data\PosClosingSqlRepository.cs` | `ClosingDate = today`, branch from POS context | No heavy load on open. User must press update. | High when update is clicked: repeated same-day aggregate queries over `Transactions`, `Transaction_Details`, `Notes`, and account voucher tables. | Keep manual update only, validate date/branch, disable Update/Execute during requests, then later convert read-only value calculation to a stored procedure or daily summary. |
| `/Pos/PosReports/Index` | `PosReportsController.Index`, `Run`, `Export`, `Lookups` | `Views\PosReports\Index.cshtml` inline JS | `dbo.usp_POS_Report_RunClosing`, `dbo.usp_POS_Report_RunOperationalSales`, `dbo.usp_POS_Report_Run`, `dbo.usp_POS_Report_StoreSerials`, `dbo.usp_POS_Report_WebInvoices`, `dbo.usp_POS_Report_NonWebLoginUsers` via `Data\PosSqlRepository.cs` | `FromDate = today`, `ToDate = today`, non-admin branch locked to current branch, admin may choose all branches | No report load on open. Lookups are lazy-loaded. | High for admin/all-branch reports, month ranges, serial reports without store/search, and print/export because they run the same heavy report. | Keep report buttons manual only, enforce required dates server-side, block duplicate run/export/print clicks, require store or search text for serial report. |
| `/Pos/HtmlReports/Index` | `HtmlReportsController.Index`, `Search`, `Export`, `Lookups` | `Views\HtmlReports\Index.cshtml`, `_FilterToolbar.cshtml`, `_ReportActions.cshtml` | Same POS report stored procedures through `Data\PosSqlRepository.cs` | `FromDate = today`, `ToDate = today`, non-admin branch locked to current branch | No report load on open. User must select a report and submit Search. | Medium-high: older HTML report page can still export a selected report and lazy branch/store lists may grow over time. | Keep manual search only, validate required filters before search/export, disable duplicate submit/export. |
| Operational report buttons: daily transactions, sales complete, general sales, revenues | `PosReportsController.Run` -> `LoadReportTable` | `Views\PosReports\Index.cshtml` | `Areas\Pos\Sql\76_POS_OperationalSalesReports_Performance.sql` | Today-to-today, branch from context for cashier | No load on open. | High on broad date range and admin all-branch mode because base table is `Transactions`. | Keep date filters required and defaulted to today, use existing sargable procedure, review execution plans before adding more indexes. |
| Closing report buttons: `finance-closing`, `finance-closing-discounts` | `PosReportsController.Run` -> `RunPosClosingReport` | `Views\PosReports\Index.cshtml` | `Areas\Pos\Sql\75_POS_ClosingReports_Performance.sql` | Today-to-today, branch from context for cashier | No load on open. | High for `finance-closing-discounts` all-branch mode because it joins `TBLClosePos` and `Transactions` returns. | Candidate for Phase 3 first report. Procedure already exists and uses date range filtering; next step is compare totals with previous report output. |
| Store serial report | `PosReportsController.Run` -> `RunPosStoreSerialsReport` | `Views\PosReports\Index.cshtml` | `Areas\Pos\Sql\78_POS_StoreSerialsReport_Performance.sql` | Today filters exist on page, store/search optional before this change | No load on open. | High if run without store or search against serial inventory history. | Require store or at least 3 search characters before run/export. |
| Branch/store/user dropdowns | `PosReportsController.Lookups`, `HtmlReportsController.Lookups` | POS and HTML report views | `GetBranches`, `GetStoresByBranch`, `GetPosReportUsers` | Branches/stores/users are empty on initial admin page | Lazy, not on open except current branch for non-admin. | Medium: branch lookup union includes `Transactions`, but currently capped and lazy. User lookup can grow. | Keep lazy loading and caps; consider search-term lookups if user count grows. |

## Slow SQL Patterns Found

- `PosClosingSqlRepository.GetClosingValues` still contains multiple same-day aggregate queries with `Transaction_Date = @date`. This is safe only if the column stores date-only values. For Phase 3, prefer `Transaction_Date >= @FromDate AND Transaction_Date < DATEADD(DAY, 1, @ToDate)` to stay sargable if time components exist.
- `finance-closing` and `finance-closing-discounts` already route through `dbo.usp_POS_Report_RunClosing` from `75_POS_ClosingReports_Performance.sql`.
- Operational sales reports already route through `dbo.usp_POS_Report_RunOperationalSales` from `76_POS_OperationalSalesReports_Performance.sql`.
- Existing POS index coverage is already broad on the local Cash database, including `IX_POS_TBLClosePos_Report_Branch_Order_User`, `IX_POS_TBLClosePos_Report_Order_Branch_User`, `IX_POS_Transactions_Report_Returns`, `IX_POS_Transactions_Report_Returns_Branch`, `IX_POS_Transactions_Report_ServiceSearch`, and `IX_POS_Transactions_OperationalSales_Report`. Do not add more report indexes without an actual execution plan.

## Phase 2 Changes Applied

- `Scripts\pos-closing.js`: validates closing date and branch, prevents duplicate update/execute clicks, disables both closing buttons while a request is active, and shows Arabic required-filter messages.
- `Views\PosReports\Index.cshtml`: validates required date filters and serial-report filters before run/export/print, blocks duplicate clicks, and disables all report action/export/print buttons while any report request is active.
- `Controllers\PosReportsController.cs`: server-side required date validation, invalid date-range validation, and serial-report guard.
- `Views\HtmlReports\Index.cshtml`: validates required report/date filters and serial-report filters before search/export, and disables the export button while Excel is being prepared.

## Phase 3 Candidate

The safest first Phase 3 candidate is `finance-closing` because it is already isolated behind `dbo.usp_POS_Report_RunClosing` and reads from `TBLClosePos`/`Notes` with explicit parameters. Before changing it further, compare old and new totals for today, one week, and one month, then capture actual execution plans on the production-like database.

