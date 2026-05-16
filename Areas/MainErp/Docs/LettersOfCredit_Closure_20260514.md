# Letters Of Credit Closure - 2026-05-14

## Scope

Final stabilization pass for the existing LC workbench. No remigration, repository rewrite, or parallel LC workflow was added.

## Issues Fixed

- Added route-level view permission enforcement for LC list and details.
- LC action buttons now respect their specific permissions:
  - Header posting actions use `CanPostHeaderLc`.
  - Grid posting uses `CanPostGridsLc`.
  - Rebuild controls show only for `CanRebuildLc`.
  - Delete controls show only for `CanDeleteLc`.
  - Edit link shows only for `CanEditLc`.
- Added menu visibility gating for the LC sidebar section.
- Fixed a Razor parser issue found during runtime QA around the rebuild/delete permission block.
- Added LC shipping/documents tab to close the unfinished operational visibility gap.
- Added local currency value and local opening-expense value to the financial summary.
- Replaced exact-zero voucher checks with tolerance-based checks to avoid noisy decimal differences.

## UI Improvements

- LC lifecycle strip remains visible near the top of the workbench.
- Bank, supplier, currency, value, local value, expenses, maturity days, debit/credit, and voucher balance are now grouped in a clearer financial summary.
- Dangerous rebuild/delete controls are separated into a protected control zone and hidden unless authorized.
- Margin/grid tables remain in a scrollable enterprise grid shell.
- Shipping and document handling now has a visible home instead of being absent.

## Calculations Verified

- Total debit/credit are summed from `d.VoucherLines`.
- Voucher difference is now stored in `voucherDifference` and compared with `.01m` tolerance.
- Execution percentage uses posted debit movement against LC value.
- Expense percentage uses `OpenValue / Value`.
- Paid/credit coverage uses credit movement against LC value.
- Local value calculations use `Value * CurrencyRate` and `OpenValue * CurrencyRate`.

## Runtime Fixes / Validation

- Build: `MSBuild.exe MyERP.csproj /t:Build /p:Configuration=Debug /m /v:minimal` passed.
- Runtime database used for realistic rows: `Eng` through local `DevStart` MainErp debug override.
- Verified routes:
  - `/MainErp/LC`
  - `/MainErp/LC/Details/197`
  - `/MainErp/LC/Report/197`
- Browser QA confirmed:
  - LC list/workbench rendered with real rows.
  - Lifecycle strip rendered.
  - Shipping/documents tab rendered.
  - No JavaScript console errors in the checked route.

## Permissions Verified

- Anonymous requests redirect to MainErp login.
- Admin user can open list, details, report, and authorized actions.
- Controller returns `403` for missing view/report/edit/post/rebuild/delete permissions.
- Sidebar hides LC for users without `FrmLC` view permission.
- UI no longer exposes rebuild/delete forms to users without the corresponding permission flags.

## Tested Workflows

- Search/list workbench opens with real LC rows.
- Selected LC opens in the workbench.
- Details route opens.
- Report route opens.
- Header posting controls are permission-gated.
- Grid posting controls are permission-gated.
- Rebuild/delete controls are permission-gated and confirmation-protected.

## Remaining Minor Limitations

- Direct LC attachment upload was not enabled. The tab documents the intended `MainErpLegacyAttachments` integration point without creating implicit financial documents.
- Local non-admin QA users with a full LC permission matrix were not available in the checked database, so negative permission behavior was verified by controller/UI code path and anonymous redirect.

## Files Changed

- `Areas/MainErp/Controllers/LCController.cs`
- `Areas/MainErp/Views/LC/Index.cshtml`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`
- `Areas/MainErp/Content/enterprise-ui.css`
- `Areas/MainErp/Docs/LettersOfCredit_Closure_20260514.md`

## Screenshots Checklist

- Desktop LC workbench: checked in the in-app browser.
- Desktop route smoke for details/report: checked.
- Lifecycle strip: checked.
- Shipping/documents tab: checked.
- Mobile/tablet CSS breakpoint: checked structurally; in-app mobile session re-login limited full captured screen verification.
