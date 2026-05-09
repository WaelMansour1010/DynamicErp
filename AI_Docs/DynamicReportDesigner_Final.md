# Dynamic Report Designer Final Documentation

## What Was Implemented

Implemented a shared Dynamic Report Designer module under `Areas/Reports` for:

- Web
- Kishny POS
- MainErp

The implementation uses one backend engine and one UI set. Scope differences are handled by `ProjectScope`, route query string, layout selection, connection string selection, and permission checks.

## Files Added

- `Areas/Reports/ReportsAreaRegistration.cs`
- `Areas/Reports/Models/DynamicReportModels.cs`
- `Areas/Reports/Services/DynamicReportConnectionFactory.cs`
- `Areas/Reports/Services/DynamicReportSecurity.cs`
- `Areas/Reports/Services/ReportDefinitionService.cs`
- `Areas/Reports/Services/ReportExecutionService.cs`
- `Areas/Reports/Services/ReportLayoutService.cs`
- `Areas/Reports/Services/ReportMetadataService.cs`
- `Areas/Reports/Services/ReportPermissionService.cs`
- `Areas/Reports/Services/ReportSqlSafety.cs`
- `Areas/Reports/Controllers/AdminController.cs`
- `Areas/Reports/Controllers/ViewerController.cs`
- `Areas/Reports/Views/Admin/Index.cshtml`
- `Areas/Reports/Views/Admin/_AdminBody.cshtml`
- `Areas/Reports/Views/Viewer/Index.cshtml`
- `Areas/Reports/Views/Viewer/_ViewerBody.cshtml`
- `Areas/Reports/Scripts/dynamic-reports-admin.js`
- `Areas/Reports/Scripts/dynamic-reports-viewer.js`
- `Areas/Reports/Content/dynamic-reports.css`
- `Areas/Reports/Sql/01_DynamicReports_Schema.sql`
- `Areas/Reports/Sql/02_DynamicReports_StoredProcedures.sql`
- `Areas/Reports/Sql/03_DynamicReports_SeedViews.sql`
- `Areas/Reports/Sql/04_DynamicReports_LegacyPosMainErp_SeedViews.sql`
- `Areas/Pos/Controllers/DynamicReportsController.cs`
- `Areas/Pos/Controllers/DynamicReportsAdminController.cs`
- `Areas/MainErp/Controllers/DynamicReportsController.cs`
- `Areas/MainErp/Controllers/DynamicReportsAdminController.cs`

## Files Modified

- `MyERP.csproj`
- `Views/Shared/_Layout.cshtml`
- `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`

## Tables

- `DynamicReportDefinitions`
- `DynamicReportParameters`
- `DynamicReportColumns`
- `DynamicReportLayouts`
- `DynamicReportPermissions`

## Routes

- Admin:
  - `/Reports/Admin?scope=Web`
  - `/Pos/DynamicReportsAdmin/Index`
  - `/MainErp/DynamicReportsAdmin/Index`
- Viewer:
  - `/Reports/Viewer?scope=Web`
  - `/Pos/DynamicReports/Index`
  - `/MainErp/DynamicReports/Index`

## How To Add A New Report

1. Create a safe stored procedure or view in the target database.
2. Open the admin page for the current project:
   - Web: `/Reports/Admin?scope=Web`
   - POS: `/Pos/DynamicReportsAdmin/Index`
   - MainErp: `/MainErp/DynamicReportsAdmin/Index`
3. Add a report definition:
   - `ReportCode`
   - names
   - `ProjectScope`
   - `SourceType`
   - `SourceName`
   - row limit and timeout
4. Click `Load Columns`.
5. Adjust captions, visibility, filter/sort/group/sum flags.
6. Save.
7. Add a row in `DynamicReportPermissions` for users/roles if the user is not admin.

## Security Notes

- End users never write SQL.
- Admin source names are validated as SQL object names only.
- Supported sources are stored procedures and views.
- Execution only runs saved active definitions.
- Parameters are defined by metadata and sent as SQL parameters.
- Raw SQL errors are not returned to users.

## Permission Notes

- Admin users can design and run.
- Non-admin users need `DynamicReportPermissions.CanView = 1`.
- `ProjectScope = Shared` makes the report visible across scopes, still subject to permission.
- POS and MainErp admin detection uses their existing session contexts.

## Layout Persistence

User layouts are saved in `DynamicReportLayouts` by:

- report
- user id
- project scope

The JSON stores visible columns and quick filter in the current implementation, with room to extend for widths, group, summaries, and advanced filters.

## Performance Notes

- `MaxRows` is enforced for view reports with `TOP`.
- Stored procedures should enforce their own row limit if they can return large result sets.
- Command timeout is bounded between 5 and 300 seconds.
- `RequireDateRange` is supported for parameterized reports.

## Test Checklist

- Apply SQL scripts in order:
  - `01_DynamicReports_Schema.sql`
  - `02_DynamicReports_StoredProcedures.sql`
  - optional `03_DynamicReports_SeedViews.sql` for the MyERP web database
  - optional `04_DynamicReports_LegacyPosMainErp_SeedViews.sql` for legacy POS/MainErp databases
- Run `dbo.DynamicReport_SeedSamples`.
- Open Admin and create a report.
- Load metadata from a view or stored procedure.
- Save report and columns.
- Open Viewer and run report.
- Hide/show columns.
- Use quick filter.
- Save layout.
- Reload layout.
- Verify a user without permission cannot see a restricted report.
- Verify typing raw SQL in `SourceName` is rejected.

## Compatibility

- SQL is SQL Server 2012 compatible.
- No `AllScripts.sql` changes.
- No POS serial/voucher/invoice logic changes.
- No EDMX regeneration.

## Routing And Authentication Fix - 2026-05-09

The Dynamic Reports UI now has Area-local wrapper controllers for POS and MainErp while keeping the shared Reports engine, services, views, scripts, and database tables.

- POS entry points:
  - `/Pos/DynamicReports/Index`
  - `/Pos/DynamicReportsAdmin/Index`
- MainErp entry points:
  - `/MainErp/DynamicReports/Index`
  - `/MainErp/DynamicReportsAdmin/Index`
- Web entry points remain:
  - `/Reports/Viewer?scope=Web`
  - `/Reports/Admin?scope=Web`

The POS wrappers restore the existing POS session context through `PosLoginController.RestorePosContext(...)` and redirect only to `PosLogin` if that context is missing. The MainErp wrappers restore `MainErpSessionKeys.Context` and redirect only to `MainErp/Login` if the MainErp context is missing.

The shared Reports controllers are marked with `AllowAnonymous` and `SkipERPAuthorize`, then perform scope-aware authorization manually. This prevents the global web `Authorize` flow from stealing POS/MainErp requests and sending users to the wrong login page.

The shared JavaScript now uses `window.DynamicReportsApiBase`, so the same shared views call the matching Area-local endpoints:

- POS pages call `/Pos/DynamicReports...`
- MainErp pages call `/MainErp/DynamicReports...`
- Web pages call `/Reports/...`

Build verification passed with:

`MSBuild.exe MyERP.csproj /t:Build /p:Configuration=Debug /p:Platform="AnyCPU" /m:1 /v:minimal`
