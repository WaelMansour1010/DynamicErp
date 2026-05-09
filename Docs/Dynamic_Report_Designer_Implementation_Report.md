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
