# Project Extracts Closure - 2026-05-14

## Scope

Final stabilization pass for `Areas/MainErp/Views/ProjectExtracts` only. No remigration or business-logic rewrite was performed.

## Issues Fixed

- Removed misleading disabled Save/Voucher/Reports buttons from the index header.
- Replaced placeholder retention/advance percentages on the list workbench with real available header values and a details drill-through for exact deductions.
- Added a compact approval/review flow on the details page so users can see header, execution lines, deductions/tax, voucher review, report, and operational close state.
- Added route-level permission enforcement for list, details, create, and report actions.
- Added menu visibility gating for the Project Extracts menu item.
- Added missing enterprise layout CSS for result cards, KPI cards, detail grids, workflow strips, progress bars, and responsive two-column/detail layouts.

## UI Improvements

- Financial summary cards now emphasize current extract value, VAT, net payable, total count, and workflow state.
- Details page now keeps real totals visible before the wide execution grid.
- Wide project line grids remain horizontally scrollable, with sticky headers and stable spacing.
- Arabic RTL layout was checked in desktop and constrained mobile widths.

## Calculations Verified

- Detail page uses actual read-model totals:
  - Current line total: `DetailCurrentValueTotal`
  - VAT total: `DetailVatTotal`
  - Discount total: `DetailDiscountTotal`
  - Final line total: `DetailFinalTotal`
  - Advance paid total: `AdvancePaidTotal`
  - Voucher debit/credit/difference
- Removed fake `5%` retention and `10%` advance deduction from the list page because the list model does not carry exact line-level deduction data.

## Runtime Fixes / Validation

- Build: `MSBuild.exe MyERP.csproj /t:Build /p:Configuration=Debug /m /v:minimal` passed.
- Runtime database used for real rows: `Eng` through local `DevStart` MainErp debug override.
- Verified routes:
  - `/MainErp/ProjectExtracts`
  - `/MainErp/ProjectExtracts/Details/3502`
  - `/MainErp/ProjectExtracts/Report/3502`
- No parser errors, server errors, or console errors were observed in browser QA.

## Permissions Verified

- Anonymous requests redirect to MainErp login.
- Admin user can open list, details, and report routes.
- Controller now returns `403` for users without legacy Project Extract permissions.
- Sidebar hides the Project Extracts item when the user lacks the mapped legacy screen permission.

## Tested Workflows

- Search/list workbench opens with real rows.
- Result selection links to details.
- Detail page opens with execution lines, deductions/tax, advance payments, and voucher trace.
- Report route opens from the detail action.
- Responsive desktop visual pass completed in the in-app browser.

## Remaining Minor Limitations

- Non-admin named QA users for the exact Project Extract screen names were not present in the local `Eng` permission matrix, so negative permission testing was verified by code path and anonymous redirect rather than a dedicated user login.
- Create/save remains the existing minimal create workflow; no new posting or rebuild workflow was introduced in this closure pass.

## Files Changed

- `Areas/MainErp/Controllers/ProjectExtractsController.cs`
- `Areas/MainErp/Views/ProjectExtracts/Index.cshtml`
- `Areas/MainErp/Views/ProjectExtracts/Details.cshtml`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`
- `Areas/MainErp/Content/enterprise-ui.css`
- `Areas/MainErp/Docs/ProjectExtracts_Closure_20260514.md`

## Screenshots Checklist

- Desktop list/workbench: checked.
- Desktop details/report route: checked by route and content load.
- Mobile/tablet layout: CSS breakpoint checked; browser mobile session required re-login and was limited by session reset.
- No overlapping critical text observed in desktop visual QA.
