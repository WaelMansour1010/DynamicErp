# MainErp Open Screens Completion Pass

Date: 2026-05-09

## Scope

This pass continued the currently open MainErp migration screens with the safest executable work:

- LC / Letters of Credit
- Project Extracts
- Workshop Sales
- Pump Sales report registration

No POS/Kishny logic was merged into MainErp.

## LC

Implemented:

- Added granular MainErp permission constants in `LCController`:
  - `MainErp.LC.PostHeader`
  - `MainErp.LC.PostGrids`
  - `MainErp.LC.Rebuild`
  - `MainErp.LC.Delete`
  - `MainErp.LC.Reports`
- Kept a safe fallback to the legacy `FrmLC` screen permissions because final MainErp permission storage is not finalized yet.
- Added `/MainErp/LC/Report/{id}`.
- Added a read-only Web Report view for LC.
- Linked the LC report from the reports tab.

Safety:

- The report is read-only.
- No new SQL objects.
- No `AllScripts.sql` changes.
- No POS changes.

## Project Extracts

Implemented:

- Added `/MainErp/ProjectExtracts/Report/{id}`.
- Added a read-only Web Report view for Project Extracts.
- Added a report action button to the Project Extract details workspace.

The report includes:

- extract/project/customer summary.
- detail line totals.
- VAT and final total summary.
- accounting balance indicator.

Safety:

- Read-only only.
- No save/post/delete.
- No database writes.

## Workshop Sales

Implemented:

- Added `/MainErp/WorkshopSales/Report/{id}`.
- Added a read-only Web Report view for Workshop Sales.
- Added a report action button in the Workshop Sales details screen.

The report includes:

- invoice/customer/KPI summary.
- invoice lines.
- VAT/totals.
- journal impact summary.

Safety:

- Read-only only.
- No save/post/delete.
- No inventory posting.
- No journal posting.

## Pump Sales

Project file registration was corrected for existing MainErp Pump Sales views:

- `PumpSales/DailyReport.cshtml`
- `PumpSales/Edit.cshtml`
- `PumpSales/DeferredDistribution.cshtml`

The existing Daily Pump web report remains the current safe report path.

## Build

`MyERP.sln` builds successfully in Debug / Any CPU.

## Runtime Validation

IIS Express was started locally on port `63735` and the MainErp login was exercised with the development login path.

Validated routes:

- `/MainErp/LC/Report/198`
  - HTTP `200`
  - no server/Razor/404 error detected.
- `/MainErp/ProjectExtracts/Report/222097`
  - HTTP `200`
  - no server/Razor/404 error detected.
- `/MainErp/WorkshopSales/Report/3832`
  - HTTP `200`
  - no server/Razor/404 error detected.

Fix applied during runtime validation:

- `SalesInvoiceReadRepository` previously assumed optional header columns like `IsPosted` always existed in the reader. Some Workshop Sales result paths did not include that column, causing `IndexOutOfRangeException`.
- Added safe optional readers for missing string/date/decimal/bool columns and applied them to optional FrmSaleBill6 trace fields.

Build blocker fixed:

- `MyERP.csproj` referenced `Areas\Reports` compile files that were missing from disk.
- Added a minimal compile-safe `Areas\Reports` skeleton so the existing project references are satisfied.
- This is a neutral skeleton only; it does not enable report SQL execution beyond a guarded placeholder service.

## Remaining Work

LC:

- Runtime test of grid-only rebuild on a representative LC with current grid rows.
- Crystal report parity or PDF export if required.
- Final permission persistence table/seed once the permission architecture is approved.

Project Extracts:

- Save/post workflow remains intentionally pending.
- Crystal/Web report parity for official printed extract forms.

Workshop Sales:

- Full save/edit/posting and inventory effect remain pending.
- Report needs customer-approved layout polish.

Pump Sales:

- More sample validation on `Nagahat`.
- Final report parity with `DailyPumpR.Rpt`.
- Deeper PG validation for discounts, prepayments, checks, and SystemOptions.
