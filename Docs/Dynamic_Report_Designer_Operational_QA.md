# Dynamic Report Designer Operational QA

Date: 2026-05-10

## Environment

- Repo: `F:\Source Code\DynamicErp`
- Branch observed: `main`
- Server: IIS Express, `http://localhost:63735`
- Build: `MyERP.sln`, Debug, Any CPU
- Databases:
  - Web: `Wael\Sql2019 / MyErp`
  - POS: `Wael\Sql2019 / Cash`
  - MainErp: `Wael\Sql2019 / Eng`
- Authenticated users used:
  - Web: `ErpAdmin`, `UserId=1`, `RoleId=1`
  - POS: `admin`, `UserID=1`, `UserType=0`
  - MainErp: `admin`, `UserID=1`, `UserType=0`
- Passwords: not recorded in this report.

## Operational Fixes Applied

1. `Areas/Reports/Views/Admin/Index.cshtml`
   - Removed layout-dependent `@section scripts` usage for the layout path branch.
   - Added inline Reports Admin script loading after the page body.
   - Added a local jQuery fallback before `dynamic-reports-admin.js`.

2. `Areas/Reports/Views/Admin/Review.cshtml`
   - Removed layout-dependent `@section scripts` usage for the layout path branch.
   - Added inline Reports Review script loading after the page body.
   - Added a local jQuery fallback before `dynamic-reports-review.js`.

3. `Areas/Reports/Controllers/AdminController.cs`
   - Added `Response.TrySkipIisCustomErrors = true` for Admin JSON `400/403` responses so IIS does not replace operational JSON error messages.

Root cause:
- The original missing partial symptom was caused by incomplete view/runtime integration. The actual authenticated Web blocker was a Razor layout error: Reports Admin/Review defined a `scripts` section, but `~/Views/Shared/_Layout.cshtml` does not render that section.
- After fixing that, MainErp exposed a second wiring issue: its layout did not provide `window.jQuery` before the Reports scripts, causing `$ is not defined`.

## Screenshots

- Web Admin: `Docs/Operational_QA_Screenshots/web-admin.png`
- POS Admin: `Docs/Operational_QA_Screenshots/pos-admin.png`
- MainErp Admin: `Docs/Operational_QA_Screenshots/mainerp-admin.png`
- Web Review: `Docs/Operational_QA_Screenshots/web-review-1.png`
- POS Review: `Docs/Operational_QA_Screenshots/pos-review-2.png`
- MainErp Review: `Docs/Operational_QA_Screenshots/mainerp-review-3.png`
- MainErp Viewer: `Docs/Operational_QA_Screenshots/mainerp-viewer.png`

## Test Results

| ID | Test | Result | Notes |
|---|---|---:|---|
| W1 | Web `/Reports/Admin/Index` after login | Pass | HTTP 200; Permissions/Catalog/Reports table rendered. |
| W2 | POS `/Pos/DynamicReportsAdmin/Index` after login | Pass | HTTP 200; Permissions/Catalog/Reports table rendered. |
| W3 | MainErp `/MainErp/DynamicReportsAdmin/Index` after login | Pass | HTTP 200; jQuery fallback fixed Review/Status rendering. |
| W4 | Review routes open | Pass | Web `id=1`, POS `id=2`, MainErp `id=3` all HTTP 200 with Review shell/actions. |
| W5 | Permissions panel | Pass | `#drPermissionsPanel` present in all three Admin screens. |
| W6 | Catalog panel | Pass | `#drCatalogPanel` present in all three Admin screens. |
| W7 | Missing partials | Pass | No `_PermissionsPanel`, `_CatalogPanel`, badge, or Review missing partial errors after fixes. |
| W8 | Console/Network | Pass | Final CDP retest: `events=[]`, `net=[]` for Admin/Review pages. |
| S1 | Web scope isolation | Pass | Web Admin list returned only `Web` reports. |
| S2 | POS scope isolation | Pass | POS Admin list returned only `POS` reports. |
| S3 | MainErp scope isolation | Pass | MainErp Admin list returned only `MainErp` reports. |
| I1 | Pending import rejection | Pass/Partial | HTTP 400 rejection confirmed. UI message body should be rechecked manually after IIS JSON fix. |
| I2 | Imported import does not duplicate | Pass/Partial | HTTP 400 rejection confirmed; no duplicate created. Does not yet provide Review link. |
| I3 | Approved import succeeds | Pass | MainErp Catalog `221` imported as ReportId `5`. |
| F1 | Open Review for imported report | Pass | `/MainErp/DynamicReportsAdmin/Review?id=5&scope=MainErp` rendered. |
| F2 | Run Validation | Pass | ErrorCount `0`, WarningCount `1` (0 sample rows), status `ReadyForActivation`. |
| F3 | Activate | Pass | ReportId `5` transitioned to `Active`, `IsActive=1`. |
| F4 | Viewer lists activated report | Pass | Retest showed ReportId `5` in `/MainErp/DynamicReports/List?scope=MainErp`. |
| F5 | Viewer Execute | Pass | Execute returned `Success=true`, 9 columns, 0 rows. |
| F6 | Print Preview | Pass | Print endpoint returned HTTP 200 and printable HTML. |

## Bugs Found

### Critical

- None remaining after the operational fixes in this pass.

### High

- Imported Catalog rows currently reject re-import without returning an “Open Review” link. This satisfies “no duplicate”, but does not satisfy the requested UX of showing the existing Review link.
- Existing Arabic mojibake is still visible in several Reports Admin/Review labels. This is pre-existing and client-visible.

### Medium

- MainErp imported report `VIEW10` validates and activates but sample execution returns 0 rows. This is allowed as a warning, but should be reviewed with real business parameters/data.
- The login POST tests show redirect exceptions in PowerShell when `MaximumRedirection=0`; the authenticated page checks prove login succeeded. This is a test harness artifact, not an application bug.

### Low

- Build still emits many legacy warnings outside Dynamic Reports. No new build errors were introduced.

## Hotfix Recommendations

1. Add an explicit “Open Review” response for already imported Catalog rows without creating duplicates.
2. Run a focused Arabic encoding cleanup before any client demo or PDF/XLSX work.
3. Keep the current Reports-local jQuery fallback until layouts are normalized across Web/POS/MainErp.

## Verification Artifacts

- Browser/CDP result: `Docs/Dynamic_Report_Designer_Operational_QA_results.json`
- HTTP workflow result: `Docs/Dynamic_Report_Designer_Operational_QA_http_results.json`
- Extra import/activation result: `Docs/Dynamic_Report_Designer_Operational_QA_http_extra.json`
- Console retest: `Docs/Dynamic_Report_Designer_Operational_QA_console_retest.json`
