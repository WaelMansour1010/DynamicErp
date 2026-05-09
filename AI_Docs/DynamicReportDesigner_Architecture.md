# Dynamic Report Designer Architecture

## Scope

Build one shared Dynamic Report Designer under `Areas/Reports` for:

- Web: the existing MVC application.
- POS: `Areas/Pos` / Kishny POS.
- MainErp: `Areas/MainErp`.

The module must avoid three duplicated report systems. Differences between projects are represented by `ProjectScope`, route entry points, layout selection, and permissions.

## Existing System Review

Reviewed areas and files:

- `AI_Docs/Pos/*`
- `AI_Docs/MainErp/*`
- `Areas/Pos/PosAreaRegistration.cs`
- `Areas/Pos/Controllers/PosDashboardController.cs`
- `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`
- `Areas/MainErp/MainErpAreaRegistration.cs`
- `Areas/MainErp/Controllers/MainErpControllerBase.cs`
- `Areas/MainErp/Views/Shared/_MainErpLayout.cshtml`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`
- `Controllers/UserReportsController.cs`
- `Controllers/ERPRoleReportsController.cs`
- `Models/UserReport.cs`
- `Models/ERPRoleReport.cs`
- `Models/SystemPage.cs`
- `Models/PageAction.cs`
- `Models/ErpDBModel.Context.cs`
- `Web.config`

## Shared and Different Parts

Shared:

- MVC 5 application shape.
- SQL Server backend.
- Claims-based web user id/role id in the original web app.
- Existing report permission concept through user and role report privileges.
- Existing layouts built with Razor views and jQuery/bootstrap style assets.

Different:

- POS uses its own session context and `KishnyCashConnection`.
- MainErp uses `MainErp.UserContext` session and a separate login/layout.
- Main web uses `_Layout.cshtml`, `MySoftERPEntity`, and claims.
- POS and MainErp use modern shell sidebars and separate active-screen handling.

## Module Location

Chosen location: `Areas/Reports`.

Reasons:

- It is shared by all projects without copying code.
- MVC area routes isolate the designer from existing controllers.
- It can serve Web, POS, and MainErp through one controller/service stack.
- SQL can live beside the module in `Areas/Reports/Sql`.

## Database Tables

Core tables:

- `DynamicReportDefinitions`: one row per report.
- `DynamicReportParameters`: safe typed parameters accepted by a report.
- `DynamicReportColumns`: visible metadata and designer capabilities per field.
- `DynamicReportLayouts`: user-saved layouts as JSON.
- `DynamicReportPermissions`: optional report-level grants for user/role/project scope.

Important columns:

- `ProjectScope`: `Web`, `POS`, `MainErp`, or `Shared`.
- `SourceType`: initially `StoredProcedure` or `View`.
- `SourceName`: database object name only, not free SQL.
- `MaxRows`, `CommandTimeoutSeconds`, and `RequireDateRange` for performance safety.

## Services

- `ReportDefinitionService`: CRUD for definitions, parameters, and columns.
- `ReportMetadataService`: reads metadata safely from stored procedures/views.
- `ReportExecutionService`: executes only active predefined sources with typed parameters.
- `ReportLayoutService`: saves, loads, deletes, and marks default user layouts.
- `ReportPermissionService`: checks admin, project scope, explicit report grants, and current context.

Services use ADO.NET and parameterized commands instead of extending the generated EDMX, avoiding Database First regeneration risk.

## Controllers

- `ReportsAreaRegistration`: route isolation.
- `Reports/AdminController`: admin designer screens and JSON endpoints.
- `Reports/ViewerController`: user report list, execution, and layout endpoints.

The controllers are shared. Project differences are passed as `scope=Web|POS|MainErp`, inferred from route/referrer when practical.

## UI Components

- Admin UI:
  - report list
  - add/edit definition
  - source type/source name
  - project scope
  - load columns from metadata
  - edit captions and field capabilities
  - active/inactive flag
  - permission entries
- User UI:
  - report selector
  - parameter form
  - HTML grid
  - column visibility
  - drag reorder
  - captions
  - filter/sort/group client-side for returned page
  - totals for summable numeric columns
  - save/load/default layouts

## Security Model

- Users never submit SQL.
- Definitions only accept `StoredProcedure` or `View`.
- Object names are validated as SQL identifiers (`schema.name`) and cannot contain whitespace, semicolon, comment markers, or expressions.
- Stored procedures are executed by name with parameters defined in metadata.
- Views are read through `SELECT TOP (@MaxRows) * FROM [schema].[view]`.
- Raw SQL exceptions are logged only in server-side detail and returned as friendly messages.
- All execution checks active definition, project scope, and permission.

## Permission Model

The module does not replace existing permissions. It layers report-specific permissions:

- Admin users can manage and run reports.
- Web can use claims id/role id and existing ERP superuser rule `UserId = 1`.
- POS can use POS context admin flags.
- MainErp can use MainErp context admin flags.
- `DynamicReportPermissions` supports user, role, and project-level grants.

The legacy `UserReport/ERPRoleReport` concept remains untouched. A future bridge can map existing report ids into `DynamicReportPermissions`.

## Layout Persistence

Layouts are stored in `DynamicReportLayouts.LayoutJson` by report and user.

JSON contains:

- ordered columns
- hidden columns
- captions
- widths
- sort rules
- filter values
- group fields
- summary choices

Only the owner can modify/delete their layouts. Admins do not override user layouts from the viewer screen.

## Performance Plan

- `MaxRows` defaults to 1000.
- `CommandTimeoutSeconds` defaults to 30.
- `RequireDateRange` can require date parameters before execution.
- Paging is supported at service model level; initial implementation returns a bounded page.
- Metadata loading uses schema-only commands where possible.
- Heavy reports should be implemented as stored procedures with indexed filters.

## Compatibility Notes

- SQL scripts are SQL Server 2012 compatible.
- Scripts use `IF OBJECT_ID(...) IS NOT NULL DROP ...` before `CREATE PROCEDURE`.
- Tables are created with `IF OBJECT_ID(...) IS NULL` because dropping data tables during deployment would be destructive.
- No edits to `AllScripts.sql`.
- No edits to POS serial, voucher, or invoice logic.
- No EDMX regeneration.

## Risks

- Existing database permissions may block metadata discovery for some stored procedures.
- Some stored procedures require non-null parameters before metadata can be inferred.
- POS and MainErp may use different databases depending on environment. The module resolves connection string by scope, but shared reporting definitions must be applied to the target database used by that scope.
- Client-side grouping/filtering is intentionally bounded by `MaxRows`; server-side advanced filtering can be added later with whitelisted column operations.
