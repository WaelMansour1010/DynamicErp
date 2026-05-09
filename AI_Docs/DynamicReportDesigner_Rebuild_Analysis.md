# Dynamic Report Designer Rebuild Analysis

## Reviewed Files

- `Areas/Reports/Controllers/AdminController.cs`
- `Areas/Reports/Controllers/ViewerController.cs`
- `Areas/Reports/Services/*`
- `Areas/Reports/Models/DynamicReportModels.cs`
- `Areas/Reports/Views/Admin/*`
- `Areas/Reports/Views/Viewer/*`
- `Areas/Reports/Scripts/dynamic-reports-admin.js`
- `Areas/Reports/Scripts/dynamic-reports-viewer.js`
- `Areas/Reports/Content/dynamic-reports.css`
- `Areas/Reports/Sql/*`
- POS wrappers under `Areas/Pos/Controllers/DynamicReports*.cs`
- MainErp wrappers under `Areas/MainErp/Controllers/DynamicReports*.cs`

## Current Tables

- `DynamicReportDefinitions`
- `DynamicReportParameters`
- `DynamicReportColumns`
- `DynamicReportLayouts`
- `DynamicReportPermissions`

## What Worked Before Rebuild

- Shared Reports area and shared services existed.
- Admin could define report source and load metadata.
- Viewer could select a report and execute it safely.
- User layout persistence existed.
- POS and MainErp wrapper routing existed after the auth fix.

## What Was Missing

- The viewer was a grid screen, not a report designer.
- No clear View Mode versus Design Mode.
- No field chooser experience beyond basic checkboxes.
- No visible grouping area.
- No properties panel for column captions, width, format, alignment, summaries.
- No filter builder.
- Layout JSON stored only basic visibility/caption state.
- Arabic labels had mojibake in several views and scripts.
- Export was not available.

## UX Problems

The user could not tell where design actions happen. The screen did not explain the workflow, and actions like column ordering, grouping, summaries, formatting, and saved layouts were either unavailable or hidden inside a basic table interaction.

## Routing/Auth Notes

Routing must remain through Area-local wrappers:

- POS: `/Pos/DynamicReports/Index`, `/Pos/DynamicReportsAdmin/Index`
- MainErp: `/MainErp/DynamicReports/Index`, `/MainErp/DynamicReportsAdmin/Index`
- Web: `/Reports/Viewer?scope=Web`, `/Reports/Admin?scope=Web`

The shared controllers stay `AllowAnonymous` + `SkipERPAuthorize` and perform scope-aware context validation manually.

## Rebuild Plan

1. Keep the shared core services and database tables.
2. Replace viewer UX with a real designer surface.
3. Implement layout state client-side without free SQL.
4. Persist a complete LayoutJson.
5. Keep POS/MainErp wrappers untouched except inherited shared behavior.
6. Add delete layout endpoint scoped to current user.
7. Improve Admin labels and management screen clarity.
8. Document completed features and known limitations.
