# Dynamic Report Designer Implementation Plan

## Reviewed Files

- `AI_Docs/Pos/*`
- `AI_Docs/MainErp/*`
- `Areas/Pos`
- `Areas/MainErp`
- `Controllers/UserReportsController.cs`
- `Controllers/ERPRoleReportsController.cs`
- `Models/UserReport.cs`
- `Models/ERPRoleReport.cs`
- `Models/SystemPage.cs`
- `Models/PageAction.cs`
- `Models/ErpDBModel.Context.cs`
- `Views/Shared/_Layout.cshtml`
- `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`

## Proposed Design

Implement a shared MVC area:

- `Areas/Reports`
- shared ADO.NET services
- shared report definition tables
- shared admin UI
- shared user designer UI
- scope-specific links only

## Tables

- `DynamicReportDefinitions`
- `DynamicReportParameters`
- `DynamicReportColumns`
- `DynamicReportLayouts`
- `DynamicReportPermissions`

## Screens

- `/Reports/Admin?scope=Web`
- `/Reports/Viewer?scope=Web`
- `/Pos/DynamicReportsAdmin/Index`
- `/Pos/DynamicReports/Index`
- `/MainErp/DynamicReportsAdmin/Index`
- `/MainErp/DynamicReports/Index`

## Execution Plan

1. Create architecture docs.
2. Create SQL schema and seed scripts under `Areas/Reports/Sql`.
3. Add shared models and services.
4. Add admin and viewer controllers.
5. Add Razor views, JavaScript, and CSS.
6. Add navigation links to Web, POS, and MainErp.
7. Add final docs and testing notes.
8. Build the solution.

## Progress

- 2026-05-09: `git pull --ff-only` completed successfully.
- 2026-05-09: Repository structure and permission/reporting patterns reviewed.
- 2026-05-09: Architecture selected: shared `Areas/Reports` with ADO.NET services and SQL module scripts.
- 2026-05-09: SQL schema and sample seed scripts added under `Areas/Reports/Sql`.
- 2026-05-09: Shared backend services and MVC controllers added under `Areas/Reports`.
- 2026-05-09: Admin and viewer UI added with shared CSS/JavaScript.
- 2026-05-09: Navigation links added for Web, POS, and MainErp.
- 2026-05-09: SQL schema/procedures applied to configured Web, POS, and MainErp databases; sample views/definitions seeded for local verification.
- 2026-05-09: Routing/auth fix added with POS and MainErp wrapper controllers so each Area uses its own session, layout, and login path while sharing the Reports engine.
