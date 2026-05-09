# Dynamic Report Designer - Operational Wiring Fix

Date: 2026-05-10

Scope: stabilization only. No new feature work was added.

## Issue

Opening Dynamic Report Admin failed with:

`The partial view '_PermissionsPanel' was not found`

On the active `main` working tree, the operational issue had two layers:

1. The Phase 2-4 Reports UI files were absent from the tree after merge, including `_PermissionsPanel.cshtml`, `_CatalogPanel.cshtml`, `Review.cshtml`, and related scripts/services.
2. The restored shared Reports views still used short partial names. Because the shared Reports admin view is reused from Web, POS, and MainErp routes, short partial names can be resolved against the current area/controller search paths before the shared Reports path.

## Fix

- `_AdminBody.cshtml` now loads shared panels by absolute virtual path:
  - `~/Areas/Reports/Views/Admin/_PermissionsPanel.cshtml`
  - `~/Areas/Reports/Views/Admin/_CatalogPanel.cshtml`
- `Review.cshtml` now loads badge partials by absolute virtual path:
  - `~/Areas/Reports/Views/Admin/_LifecycleBadge.cshtml`
  - `~/Areas/Reports/Views/Admin/_CertificationBadge.cshtml`
- `AdminController.Review` GET now follows the same login behavior as Admin Index:
  - Web redirects to `/Login`.
  - POS redirects to `/Pos/Login`.
  - MainErp redirects to `/MainErp/Login`.
  - POST actions still require designer permission and return 403 when unauthorized.
- The missing Phase 1-4 Reports files were restored into `Areas/Reports` and the classic MVC `MyERP.csproj` was updated with the required `Compile` and `Content` entries.

## Ready-To-Test URLs

Use these after logging into the matching scope:

| Scope | Admin URL | Review URL examples |
| --- | --- | --- |
| Web | `http://localhost:63735/Reports/Admin/Index` | `http://localhost:63735/Reports/Admin/Review?id=1&scope=Web`, `http://localhost:63735/Reports/Admin/Review?id=4&scope=Web` |
| POS | `http://localhost:63735/Pos/DynamicReportsAdmin/Index` | `http://localhost:63735/Pos/DynamicReportsAdmin/Review?id=4&scope=POS`, `http://localhost:63735/Pos/DynamicReportsAdmin/Review?id=5&scope=POS` |
| MainErp | `http://localhost:63735/MainErp/DynamicReportsAdmin/Index` | `http://localhost:63735/MainErp/DynamicReportsAdmin/Review?id=3&scope=MainErp` |

## Existing Definition IDs

| DB | Scope | IDs |
| --- | --- | --- |
| `MyErp` | Web | `1 WEB_USERS_SAMPLE Active`, `4 IMP_Web_dbo_GetBankAccountByBankId Disabled` |
| `Cash` | POS | `4 IMP_POS_dbo_VIEW2 Disabled`, `5 IMP_POS_dbo_sp_ErrorLog_DailySummary Disabled` |
| `Eng` | MainErp | `3 MAINERP_JOURNAL_SAMPLE Active` |

## What Should Be Visible

Admin Index:

- Definition panel at the top.
- Permissions panel under the definition panel.
- Catalog panel under permissions.
- Reports list at the bottom with a `Lifecycle` column and a `Review` link in the actions cell.

Review page:

- Header with lifecycle badge and certification badge.
- Action bar: Validate, Apply Suggestions, Activate, Disable, Archive, Draft, Mark as Reviewed, Revert Review.
- Validation summary counters.
- Metadata, columns, parameters, sample execution, print preview button.
- Risk flags panel when the report came from Catalog import.

## Verification

| Check | Result |
| --- | --- |
| `_PermissionsPanel.cshtml` exists | Pass |
| `_CatalogPanel.cshtml` exists | Pass |
| Review/badge partials exist | Pass |
| All relevant partials included in `MyERP.csproj` | Pass |
| No remaining short-name `Html.Partial("_...")` in Reports Admin/Viewer views | Pass |
| JS syntax | Pass |
| Build | Pass, with existing unrelated legacy warnings |
| `/Reports/Admin/Index` unauthenticated | 302 to `/Login?ReturnUrl=...` |
| `/Pos/DynamicReportsAdmin/Index` unauthenticated | 302 to `/Pos/Login?returnUrl=...` |
| `/MainErp/DynamicReportsAdmin/Index` unauthenticated | 302 to `/MainErp/Login?returnUrl=...` |
| `/Reports/Admin/Review?id=1&scope=Web` unauthenticated | 302 to Web login after fix |
| `/Pos/DynamicReportsAdmin/Review?id=4&scope=POS` unauthenticated | 302 to POS login after fix |
| `/MainErp/DynamicReportsAdmin/Review?id=1&scope=MainErp` unauthenticated | 302 to MainErp login after fix |

## Remaining Manual Step

An authenticated browser session is still required to visually confirm the panels and Review screen. The unauthenticated smoke now proves routing and auth redirection are correct and no longer fail at MVC partial discovery.
