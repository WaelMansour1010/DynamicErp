# Dynamic Report Designer Implementation Report

## Phase 1 - Print Preview + Canvas UX Clarity

Date: 2026-05-09

Branch: `claude/improve-report-designer-iAq77`

### Files Changed

- `Areas/Reports/Controllers/ViewerController.cs`
- `Areas/Reports/Views/Viewer/_ViewerBody.cshtml`
- `Areas/Reports/Views/Viewer/Print.cshtml`
- `Areas/Reports/Content/dynamic-reports.css`
- `Areas/Reports/Content/dynamic-reports-print.css`
- `Areas/Reports/Scripts/dynamic-reports-viewer.js`
- `MyERP.csproj`
- `Docs/Dynamic_Report_Designer_Smoke_Baseline.md`
- `Docs/Dynamic_Report_Designer_Implementation_Report.md`

### Implemented

- Added `ViewerController.Print(...)` as a POST action inherited by Web, POS, and MainErp routes.
- Added pure print view with `Layout = null`, RTL HTML, report header, parameters, generation date, user name, and full result table.
- Added print stylesheet with A4 page setup, repeated table header/footer behavior, printable table styling, and on-screen print preview frame.
- Added `معاينة طباعة` button to the viewer command bar.
- Added JavaScript form POST to open print preview in a new tab.
- Added visible A4-style `.dr-canvas-page` around the grid.
- Added `.dr-drop-zone--active` during group-area dragover.
- Added `.dr-selected` outline for selected field/header.

### Phase 0 Baseline

See `Docs/Dynamic_Report_Designer_Smoke_Baseline.md`.

### Manual Test Results

| Test | Result | Notes |
| --- | --- | --- |
| Web `/Reports/Viewer/Index` | Pass/Redirect | HTTP 302 to Web login without authenticated session |
| Web print `/Reports/Viewer/Print` | Pass/Redirect | HTTP 302 to Web login; route exists and is not 404 |
| POS `/Pos/DynamicReports/Index` | Pass/Redirect | HTTP 302 to POS login; correct area context |
| POS print `/Pos/DynamicReports/Print` | Pass/Redirect | HTTP 302 to POS login; inherited route resolves |
| MainErp `/MainErp/DynamicReports/Index` | Pass/Redirect | HTTP 302 to MainErp login; correct area context |
| MainErp print `/MainErp/DynamicReports/Print` | Pass/Redirect | HTTP 302 to MainErp login; inherited route resolves |
| JavaScript syntax | Pass | `node --check Areas\Reports\Scripts\dynamic-reports-viewer.js` |
| Build | Pass | `MSBuild.exe MyERP.csproj /t:Build /p:Configuration=Debug /p:Platform="AnyCPU" /m:1 /v:minimal` |
| Authenticated layout save/load/print | Blocked | No authenticated browser session was available in this execution context |
| Console check | Blocked | Requires authenticated interactive browser session |
| Ctrl+P print browser verification | Blocked | Requires authenticated print preview page |

### Deviations

- The print form action uses `state.apiBase + "/Print"` rather than `state.apiBase + "/Viewer/Print"` because the current application stores `state.apiBase` as the controller URL (`/Reports/Viewer`, `/Pos/DynamicReports`, `/MainErp/DynamicReports`). This keeps Web/POS/MainErp routes correct without changing existing API helpers.
- `layoutId` is tracked as `state.currentLayoutId` instead of persisting `state.layout.currentId`, to keep LayoutJson schema unchanged at `designVersion = 2`.

### Notes For Later Phases

- Some existing Arabic strings in the current branch display as mojibake before Phase 1; broad encoding cleanup is outside the Phase 1 hard limit.
- `aspnet_compiler` from repo root is blocked by a nested `.claude\worktrees\...\web.config` application-level config. This is outside the Phase 1 allowed file set.
- Authenticated browser smoke should be repeated by a user with valid Web, POS, and MainErp sessions before merging.

## Phase 2 - Permissions UI + Role Mapping + Security Hardening

Date: 2026-05-09

Branch: `claude/improve-report-designer-iAq77`

### Pre-flight

- Branch check: Pass (`claude/improve-report-designer-iAq77`).
- Phase 1 commit check: Pass (`feat(reports): Phase 1 — Print preview + canvas UX clarity` found in recent history).
- Encoding sanity check: Pass for the required files; no `Ã` or replacement-character markers were found in:
  - `Areas/Reports/Views/Viewer/_ViewerBody.cshtml`
  - `Areas/Reports/Views/Admin/_AdminBody.cshtml`
  - `Areas/Reports/Views/Viewer/Print.cshtml`
- Added the `state.apiBase` controller-URL contract comment to `dynamic-reports-admin.js`.

### Files Changed

| File | Delta | Notes |
| --- | ---: | --- |
| `Areas/Reports/Controllers/AdminController.cs` | +125 / -0 | Added permissions endpoints and shared designer guard. |
| `Areas/Reports/Services/ReportPermissionService.cs` | +370 / -8 | Added permission listing, grant/revoke, effective resolver, role/user lookup, and validation. |
| `Areas/Reports/Models/DynamicReportModels.cs` | +33 / -0 | Added permission DTOs only. |
| `Areas/Reports/Scripts/dynamic-reports-admin.js` | +163 / -1 | Added admin permissions UI client module. |
| `Areas/Reports/Views/Admin/_AdminBody.cshtml` | +2 / -0 | Injected permissions panel below report definition. |
| `Areas/Reports/Views/Admin/_PermissionsPanel.cshtml` | +39 / -0 | New RTL admin permissions panel. |
| `Areas/Reports/Content/dynamic-reports.css` | +53 / -0 | Added permissions panel styling. |
| `Docs/Dynamic_Report_Designer_Implementation_Report.md` | updated | Added Phase 2 implementation and test notes. |

### Implemented

- Added `ListPermissions`, `ListRoles`, `ListUsersLite`, `SavePermission`, and `DeletePermission` admin endpoints.
- Added `RequireDesigner(scope)` server-side guard so every new admin action re-checks design/admin permission before reading or writing.
- Extended `ReportPermissionService` with direct-user and role grant support.
- Updated `CanView(...)` to use `Resolve(...)`, preserving admin full access while adding direct and role effective permissions.
- Added transactional upsert for `DynamicReportPermissions`; duplicate `(ReportId, ProjectScope, UserId|RoleId)` updates the existing row instead of inserting a second row.
- Added validation for report id, scope, exact actor selection, positive actor id, and at least one permission flag.
- Added role and user discovery:
  - Web: `dbo.ERPRole` and `dbo.ERPUser`.
  - POS/MainErp: `dbo.TblUsers.UserType` as role-like mapping, and `dbo.TblUsers` for users.
- Added an RTL permissions panel with user search, role picker, flags, save, list, and delete actions.
- No SQL schema or stored procedure changes were made.

### Manual Test Results

| Test | Result | Notes |
| --- | --- | --- |
| Build | Pass | MSBuild completed and produced `bin\MyERP.dll`; existing project warnings remain outside Phase 2 files. |
| JS syntax | Pass | `node --check Areas\Reports\Scripts\dynamic-reports-admin.js`. |
| T1 Web direct user permission | Blocked | Requires authenticated Web admin and test user sessions. |
| T2 Web role permission | Blocked | Requires authenticated Web users with known role assignment. |
| T3 POS scope isolation | Blocked | Requires authenticated POS admin session. |
| T4 MainErp scope | Blocked | Requires authenticated MainErp admin session. |
| T5 Validation | Partially verified by code/build | Server-side validation implemented; HTTP exercise requires authenticated admin session. |
| T6 Backward compatibility | Partially verified by build | Admin full access path preserved; layout v2 untouched. Authenticated viewer smoke remains required. |
| T7 Console & Network | Blocked | Requires authenticated browser session. |

### Acceptance Notes

- Build passed. The build output contains many existing warnings from unrelated controllers/reports; no build error was introduced by Phase 2.
- `git diff --check` passed for the Phase 2 file set.
- `DynamicReportLayouts.LayoutJson` schema was not changed.
- `Areas/Reports/Sql` was not touched.
- POS/MainErp wrapper controllers were not touched; they inherit the admin routes through the existing controller setup.

### Deviations

- `_RolePicker.cshtml` was not created because the role selector is small and is contained in `_PermissionsPanel.cshtml`.
- `MyERP.csproj` was not edited manually. The new partial is a Razor view file under the existing view folder; this keeps the Phase 2 hard limit intact.
- Legacy POS/MainErp roles use `TblUsers.UserType` values greater than zero. UserType `0` is not offered for grants because Phase 2 validation requires a positive actor id and system admins already keep full access.

### Follow-up For Later Phases

- Authenticated manual smoke is still required for Web, POS, and MainErp.
- If deployment depends strictly on `.csproj` Content entries for Razor views, add `_PermissionsPanel.cshtml` in a project-file maintenance pass outside the Phase 2 hard limit.

## Phase 3 - Legacy Importer with Catalog + Classification

Date: 2026-05-09

Branch: `claude/improve-report-designer-iAq77`

### Pre-flight

- Branch check: Pass (`claude/improve-report-designer-iAq77`).
- Phase 1 and Phase 2 commits: Pass.
- Encoding check: Pass for `_AdminBody.cshtml` and `_PermissionsPanel.cshtml`.
- NuGet check: no packages added; implementation uses ADO.NET / `System.Data.SqlClient`.

### Files Changed

| File | Notes |
| --- | --- |
| `Areas/Reports/Sql/05_DynamicReports_Catalog.sql` | New idempotent catalog table/index script. |
| `Areas/Reports/Models/CatalogModels.cs` | New catalog DTOs. |
| `Areas/Reports/Models/DynamicReportModels.cs` | Added catalog status constants only. |
| `Areas/Reports/Services/ReportClassificationEngine.cs` | New pure classification engine. |
| `Areas/Reports/Services/ReportCatalogService.cs` | New discovery/list/detail/approve/reject/import service. |
| `Areas/Reports/Controllers/AdminController.cs` | Added catalog endpoints only. |
| `Areas/Reports/Views/Admin/_CatalogPanel.cshtml` | New admin catalog panel. |
| `Areas/Reports/Views/Admin/_AdminBody.cshtml` | Added catalog panel partial below permissions. |
| `Areas/Reports/Scripts/dynamic-reports-admin.js` | Added `dr.catalog` namespace. |
| `Areas/Reports/Content/dynamic-reports.css` | Added catalog styles. |
| `MyERP.csproj` | Added required Compile/Content entries for classic MVC project build. |

### SQL Application

`05_DynamicReports_Catalog.sql` was applied twice successfully to prove idempotence:

| Scope | Database | Result |
| --- | --- | --- |
| Web | `MyErp` | Pass |
| POS | `Cash` | Pass |
| MainErp | `Eng` | Pass |

### Discovery Results

Discovery was run first for all scopes before any approve/import action.

| Scope | Total | Approved | Pending | Risky | Rejected | Imported |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Web | 495 | 19 before import, 18 after | 114 | 35 | 327 | 1 |
| POS | 308 | 4 before import, 3 after | 72 | 194 before import, 193 after | 38 | 2 |
| MainErp | 241 | 4 | 58 | 165 | 14 | 0 |

Discovery run output:

- Web: `Discovered=495, Updated=0, Errors=0`; rerun after classifier correction: `Discovered=0, Updated=495, Errors=0`.
- POS: `Discovered=308, Updated=0, Errors=0`; rerun after classifier correction: `Discovered=0, Updated=308, Errors=0`.
- MainErp: `Discovered=241, Updated=0, Errors=0`; rerun after classifier correction: `Discovered=0, Updated=241, Errors=0`.

### Catalog Examples

| Scope | Bucket | CatalogId | Source | Score | RiskFlags |
| --- | --- | ---: | --- | ---: | --- |
| Web | Approved | 144 | `dbo.GetAllRatingAndEvaluation` | 80 | empty |
| Web | Pending | 137 | `dbo.GetAccountStatement` | 75 | `MultiResultSet` |
| Web | Risky | 27 | `dbo.CashIssueAndReceipt_Get` | 45 | `MultiResultSet` |
| Web | Rejected | 217 | `dbo.GetItemTransactions` | 75 | `HasInsert,HasUpdate,MultiResultSet` |
| POS | Approved | 289 | `dbo.VIEW2` | 80 | empty |
| POS | Pending | 9 | `dbo.RPT_CloseReportTotal` | 75 | `MultiResultSet` |
| POS | Risky | 2 | `dbo.DiscountDetails_Report` | 45 | `MultiResultSet` |
| POS | Rejected | 13 | `dbo.RPT_SalesSummary_Main` | 75 | `HasInsert,MultiResultSet` |
| MainErp | Approved | 225 | `dbo.VIEW2` | 80 | empty |
| MainErp | Pending | 216 | `dbo.View_Booking` | 65 | `CrossDb` |
| MainErp | Risky | 6 | `dbo.MainErp_SalesInvoice_Search` | 45 | `MultiResultSet` |
| MainErp | Rejected | 24 | `dbo.usp_DynamicErpVoucher_Post` | 35 | `HasUpdate,NoStandardSelect` |

### Import Verification

No bulk import was performed. Three sources were manually selected and imported after discovery:

| Type | Scope | CatalogId | Source | Imported ReportId | ReportCode | Execute Result |
| --- | --- | ---: | --- | ---: | --- | --- |
| Simple View | POS | 289 | `dbo.VIEW2` | 4 | `IMP_POS_dbo_VIEW2` | Pass, 0 rows, 5 columns |
| Read-only SP | POS | 18 | `dbo.sp_ErrorLog_DailySummary` | 5 | `IMP_POS_dbo_sp_ErrorLog_DailySummary` | Pass, 1 row, 6 columns |
| SP with simple parameter | Web | 153 | `dbo.GetBankAccountByBankId` | 4 | `IMP_Web_dbo_GetBankAccountByBankId` | Pass with `BankId=1`, 0 rows, 14 columns |

Database verification:

- `DynamicReportDefinitions`: all three imported as `IsActive=0` draft.
- `DynamicReportColumns`: POS view = 5, POS SP = 6, Web SP = 14.
- `DynamicReportParameters`: Web SP has `BankId` as required `Int`; the selected view and no-param SP correctly have no parameters.

### Manual Test Results

| Test | Result | Notes |
| --- | --- | --- |
| Build | Pass | `MSBuild.exe MyERP.csproj /t:Build`; existing warnings only from unrelated files. |
| JS syntax | Pass | `node --check Areas\Reports\Scripts\dynamic-reports-admin.js`. |
| SQL idempotence | Pass | Script applied twice to Web/POS/MainErp. |
| T1 Discover initial | Pass | Catalog populated and counts recorded for all scopes. |
| T2 Import flow | Partial Pass | Three manual imports succeeded as draft and executed via backend service; authenticated Viewer UI/Print Preview still requires browser session. |
| T3 Reject flow | Blocked | Not executed to avoid changing catalog decisions beyond selected import test. |
| T4 Body excerpt safety | Code verified | Detail API returns text and JS inserts it with `.text()` inside `<pre>`, so HTML is encoded by jQuery. |
| T5 POS scope | Pass | POS catalog populated from `Cash` only. |
| T6 MainErp scope | Pass | MainErp catalog populated from `Eng` only. |
| T7 Re-discovery idempotence | Pass | Second discovery updated existing rows without duplicates. |
| T8 Console/Network | Blocked | Requires authenticated browser session. |

### Deviations And Decisions

- `MyERP.csproj` was updated even though the prompt preferred no manual project edits. This was required because the repo is a classic ASP.NET MVC project with explicit `Compile` entries; without it, new C# files are not compiled.
- The first discovery classified every object as `Rejected` because SQL Server `OBJECT_DEFINITION` includes the object header (`CREATE PROCEDURE/VIEW`). The classifier now strips only that header before DDL risk checks, while preserving rejection for DDL inside the executable body.
- Imported reports remain draft (`IsActive=0`) per the security rule. Full Viewer list and Print Preview UI require an authenticated session and, if desired, explicit admin activation after review.

### Phase 4 Notes

- Parameter Lookups can build on the imported parameter metadata.
- Authenticated UI smoke should verify Catalog panel interactions, Viewer activation flow, and Print Preview for the three imported drafts after admin review.

## Phase 4 - Draft Activation Workflow + Validation + Suggestions

Date: 2026-05-09

Branch: `claude/improve-report-designer-iAq77`

### Pre-flight

- Branch check: Pass (`claude/improve-report-designer-iAq77`).
- Phase 1, Phase 2, Phase 3 commits: Pass.
- Encoding check: existing admin partials still contain pre-existing Arabic mojibake in several labels. New Phase 4 files were created with readable Arabic text.
- NuGet check: no packages added; implementation uses existing MVC/Razor, ADO.NET, and jQuery.

### Files Changed

| File | Delta / Notes |
| --- | --- |
| `Areas/Reports/Sql/06_DynamicReports_Lifecycle.sql` | New, 29 lines. Adds lifecycle columns and index. |
| `Areas/Reports/Models/ReviewModels.cs` | New, 95 lines. Review/validation/suggestion/lifecycle DTOs. |
| `Areas/Reports/Services/ReportValidationService.cs` | New, 324 lines. Metadata, columns, params, sample execution, print-render checks. |
| `Areas/Reports/Services/ReportSuggestionService.cs` | New, 173 lines. Pure AR captions/format/sort/group suggestions, 115 dictionary entries. |
| `Areas/Reports/Services/ReportLifecycleService.cs` | New, 158 lines. Server-side transition rules and activation gate. |
| `Areas/Reports/Views/Admin/Review.cshtml` | New, 160 lines. Review page with validation, suggestions, sample, print link, risk flags. |
| `Areas/Reports/Views/Admin/_StatusBadge.cshtml` | New, 5 lines. Shared status pill. |
| `Areas/Reports/Scripts/dynamic-reports-review.js` | New, 130 lines. Review page AJAX/actions. |
| `Areas/Reports/Content/dynamic-reports-review.css` | New, 111 lines. Review UI styles. |
| `Areas/Reports/Controllers/AdminController.cs` | +241 lines. Review endpoints and lifecycle actions. |
| `Areas/Reports/Models/DynamicReportModels.cs` | +5 lines. Lifecycle properties on `DynamicReportDefinition`. |
| `Areas/Reports/Services/ReportDefinitionService.cs` | +12 / -6. Reads lifecycle columns; inserts new definitions as Draft; general save no longer flips lifecycle. |
| `Areas/Reports/Views/Admin/_AdminBody.cshtml` | +1 / -1. Adds Lifecycle column. |
| `Areas/Reports/Scripts/dynamic-reports-admin.js` | +16 / -2. Status badge and Review link; import success shows Review URL. |
| `Areas/Reports/Content/dynamic-reports.css` | +18. Shared status pill styles. |
| `MyERP.csproj` | +9. Required explicit Compile/Content entries for classic MVC project. |

### SQL Migration

`06_DynamicReports_Lifecycle.sql` was applied twice successfully to prove idempotence.

| Scope | Database | Before | After |
| --- | --- | --- | --- |
| Web | `MyErp` | `IsActive=0: 1`, `IsActive=1: 3` | `Disabled/0: 1`, `Active/1: 3` |
| POS | `Cash` | `IsActive=0: 2`, `IsActive=1: 3` | `Disabled/0: 2`, `Active/1: 3` |
| MainErp | `Eng` | `IsActive=1: 3` | `Active/1: 3` |

Column verification passed on all three databases:

- `LifecycleStatus`
- `LastValidatedAt`
- `ActivatedBy`
- `ActivatedAt`

### Implementation Notes

- `LifecycleStatus` is now the authoritative state. `IsActive` is synchronized only by `ReportLifecycleService`.
- New manual definitions inserted through `ReportDefinitionService.SaveDefinition` are `Draft` and `IsActive=0`.
- Imported reports from Phase 3 also inherit `LifecycleStatus='Draft'` through the DB default after script 06 is applied.
- `ReportDefinitionService.UpdateDefinition` does not activate/deactivate reports. This prevents bypassing the activation gate from the old checkbox.
- Activation to `Active` always runs full validation first. Any validation error moves the report to `ValidationErrors` and keeps `IsActive=0`.
- Formatting suggestions are calculated and exposed in Review UI. Because Phase 4 does not add DB columns for column format strings, `ApplySuggestions` persists captions, groupable hints, summable hints, and suggested sort order only. Full persistent formatting belongs to a later schema phase.
- `ReportExecutionService` does not support temporary MaxRows/timeout override. Sample execution therefore sends safe default parameter values but still uses the report definition's configured execution limits. This is documented as the R1 mitigation item for later improvement.

### Suggestion Verification

Backend suggestion smoke passed after build:

| Input | Result |
| --- | --- |
| Dictionary size | `115` |
| `InvoiceNo` | `رقم الفاتورة` |
| `CustomerName` | `اسم العميل` |
| `Total` | `الإجمالي` |
| `CreatedAt` | `تاريخ الإنشاء` |
| `XyzAbc` | `⚠ Xyz Abc` |
| Decimal format | `#,##0.00` |
| DateTime format | `yyyy-MM-dd HH:mm` |
| `BranchId` groupable | `True` |

### Backend Smoke

- `ReportDefinitionService.GetDefinitions(scope, true)` passed after migration for Web/POS/MainErp.
- Sample output:
  - Web: `IMP_Web_dbo_GetBankAccountByBankId: Disabled, IsActive=False`
  - POS: `IMP_POS_dbo_sp_ErrorLog_DailySummary: Disabled, IsActive=False`
  - MainErp: `MAINERP_JOURNAL_SAMPLE: Active, IsActive=True`

### Manual Test Results

| Test | Result | Notes |
| --- | --- | --- |
| Build | Pass | Normal MSBuild passed; existing unrelated warnings remain. |
| JS syntax | Pass | `node --check` passed for `dynamic-reports-admin.js` and `dynamic-reports-review.js`. |
| SQL idempotence | Pass | Script 06 applied twice to Web/POS/MainErp. |
| T1 Migration verification | Pass | Counts before/after recorded above. |
| T2 Review existing Active report | Backend partial | Active reports remain `Active` after migration; authenticated Review UI requires browser session. |
| T3 Import → Review → Activate | Blocked | Requires authenticated admin browser session and a fresh approved/imported catalog item. Backend services are implemented. |
| T4 Anti-Garbage | Code verified | `meta.nameAr`, `source.name`, visible columns, and exec errors are server-side Errors. UI exercise blocked by auth session. |
| T5 Bad Performance | Blocked | No known >30s safe report was executed; timeout override not available in `ReportExecutionService`. |
| T6 Disabled → Re-activation | Blocked | Requires authenticated UI/session and safe test report mutation. |
| T7 Archived flow | Code verified | `Archived → Active` is rejected in `ReportLifecycleService`; UI exercise blocked by auth session. |
| T8 POS scope | Blocked | Requires authenticated POS admin session. |
| T9 MainErp scope | Blocked | Requires authenticated MainErp admin session. |
| T10 Suggestions accuracy | Pass | Backend smoke table above. |
| T11 Console & Network | Blocked | Requires authenticated browser session. |

### Additional Verification Notes

- `git diff --check` passed for Phase 4 files. It reported only line-ending warnings and also surfaced pre-existing/unrelated dirty POS SQL/tool files that were not touched by this phase.
- `MvcBuildViews=true` was attempted, but failed before view compilation because an unrelated nested file `.claude\worktrees\flamboyant-bartik\web.config` is treated as below application level and raises `ASPCONFIG`. Normal project build succeeds.

### Deviations And Decisions

- `MyERP.csproj` was updated manually because this classic MVC project requires explicit C# Compile and Razor/asset Content entries.
- Persistent column formatting was not added because Phase 4 allows only lifecycle SQL. Suggestions are shown and can influence existing column flags/order; true persisted format strings should be handled in a later schema phase.
- Review print preview opens the existing Phase 1 print endpoint in a new tab. The iframe is present but intentionally lazy/empty until the user opens preview, matching the prompt's no-auto-load requirement.
- Existing mojibake in older admin partials was not fixed because it predates Phase 4 and is outside this phase's allowed scope.

### Phase 5 Notes

- Parameter Lookups can now use `LifecycleStatus` to keep incomplete imported reports in Draft/NeedsMapping until lookup defaults and captions are ready.
- A future execution-service enhancement should allow validation-only MaxRows=50 and CommandTimeout=10 without changing the saved definition.
- If formatting persistence is required before Conditional Formatting, add explicit format/decimal/date-format columns or a metadata JSON field in a dedicated schema phase.
