# Deployment Steps - AllowedModule Sync (2026-05-11)

## Target
Original **DynamicErp web** application only (not POS/Kishny).

## Files to Deploy
- `Controllers/SystemSettings/SystemSettingController.cs`
- `Controllers/HelperController.cs`
- `Areas/Sync/Views/Shared/_SyncErpMenuItem.cshtml`
- `AI_Docs/AllowedModule_Stabilization_Reference_2026-05-11.md`

## SQL Files in This Release
### Required SQL
1. `Scripts/2026-05-11_Add_Sync_Module_SystemPage.sql`
Purpose: Ensure Sync module entry exists in `dbo.SystemPage` so AllowedModule can control it.

### Optional SQL
- None in this release.

### Verification SQL
- `Scripts/VERIFY.sql`
Purpose: Safe `SELECT`-only checks to confirm deployment success.

## SQL Execution Order
1. Run `Scripts/2026-05-11_Add_Sync_Module_SystemPage.sql` on target `MyErp` database.
2. Run `Scripts/VERIFY.sql` and review results.

## Application Deployment Order
1. Backup target application files and database.
2. Apply required SQL in the order above.
3. Deploy changed web files only.
4. Recycle IIS app pool for DynamicErp web app.
5. Validate Sync menu visibility and AllowedModule behavior with a test user.

## Notes
- SQL scripts are provided separately and must be executed manually by DBA/release engineer.
- This package does not contain auto-run SQL, full publish output, or unrelated modules.
