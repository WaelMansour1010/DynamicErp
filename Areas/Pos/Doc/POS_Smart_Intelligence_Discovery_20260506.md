# POS Smart Intelligence Discovery - 2026-05-06

## Scope

Build smart diagnostic reporting above traditional reports for POS sales, branch performance, inventory/card serial health, cash/treasury movement, employee/custody exposure, and accounting review.

## Mandatory Boundaries

- POS SQL is kept only under `F:\Source Code\DynamicErp\Areas\Pos\Sql`.
- `F:\Source Code\SatriahMain\Main Script\AllScripts.sql` is not used for this POS work.
- The implementation is read-only diagnostics. It does not post accounting entries, change stock, create issue vouchers, or modify save logic.
- Stored procedures use SQL Server 2012-compatible `DROP` + `CREATE`.

## Project Findings

- DynamicErp POS uses MVC area `Areas/Pos` with session restoration via `POSCTX`.
- POS save flow remains centered on `PosTransactionController.Save`, `PosSqlRepository.SaveTransaction`, and `dbo.usp_POS_SaveTransaction`.
- Existing accounting and stock tables used for diagnostics:
  - `Transactions`
  - `Transaction_Details`
  - `TransactionTypes`
  - `Notes`
  - `DOUBLE_ENTREY_VOUCHERS`
  - `TBLClosePos`
  - `TblBranchesData`
  - `TblUsers`
  - `TblEmployee`
  - `TblStore`
  - `TblItems`
- Serial/card evidence is available primarily through `Transaction_Details.ItemSerial`, `Transactions.VisaNumber`, card flags, issue/receive serial fields, and stock effect from `TransactionTypes.StockEffect`.
- Legacy VB6 inspection confirmed that serial and stock behavior depends on `Transaction_Details.ItemSerial`, stock-effect transaction types, `NOTS` issue voucher linkage, and cost/quantity helper logic such as `GetItemQuantityStock` and `UpdateTransactionsCost`. The new work reads those outputs rather than reimplementing the posting logic.

## Implemented Diagnostics

- CFO/accounting health dashboard.
- Branch performance comparison.
- Sales and collection efficiency intelligence.
- Cash/treasury pressure analysis.
- Expense anomaly analysis.
- Inventory, stock, and serial profitability diagnostics.
- Employee receivable diagnostics.
- Custody/advance diagnostics.
- Abnormal journal detection.
- Root-cause analyzer and journal drill-down.

## Risk Notes

- Account-category detection for cash, expense, receivable, and custody diagnostics uses conservative account-name heuristics unless parent account serial filters are supplied.
- Serial diagnostics are based on transaction history and `TransactionTypes.StockEffect`; this matches the reporting pattern already used in POS SQL, but operational approval should compare sample serials against the legacy VB6 serial search screens.
- No indexes were added. Large-range performance should be tested with realistic production filters before enabling broad management use.

## Manual Test Notes

- Applied `Areas/Pos/Sql/45_POS_FinancialIntelligenceReports.sql` successfully to local `Cash`.
- Smoke-tested main procedures for `2026-05-01` through `2026-05-06`.
- Built `F:\Source Code\DynamicErp\MyERP.sln` successfully with MSBuild Debug Any CPU.
