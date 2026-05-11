# Release Notes - AllowedModule Sync (2026-05-11)

## Scope
This package is a **minimal release** for the **original DynamicErp web project** (ASP.NET MVC), focused on stabilizing AllowedModule behavior for the Sync module menu visibility/authorization path.

## Included Changes
1. Code updates:
- `Controllers/SystemSettings/SystemSettingController.cs`
- `Controllers/HelperController.cs`
- `Areas/Sync/Views/Shared/_SyncErpMenuItem.cshtml`

2. Documentation:
- `AI_Docs/AllowedModule_Stabilization_Reference_2026-05-11.md`

3. Database script:
- `Scripts/2026-05-11_Add_Sync_Module_SystemPage.sql`

## Database Notes
- The SQL script is idempotent and ensures the `Sync` module root row exists in `dbo.SystemPage` for AllowedModule control.
- Script is included as a standalone file and is **not** auto-executed by this release package.

## Exclusions Confirmed
- No full-site publish output.
- No unchanged files.
- No POS/Kishny files included.
- No `bin`, `obj`, `packages`, `node_modules`, temp, or cache files included.
