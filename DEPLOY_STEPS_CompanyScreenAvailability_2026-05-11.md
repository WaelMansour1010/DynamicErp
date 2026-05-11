# Deploy Steps - Company Screen Availability

Date: 2026-05-11

## 1. Backup

Before deploying:

- Backup the `MyErp` database.
- Backup the current web application folder.

## 2. Database

Run the SQL script:

```sql
Scripts/CompanyAllowedPages_Setup.sql
```

Expected result:

- `dbo.CompanyAllowedPages` exists.
- FK to `dbo.SystemPage` exists.
- Unique index on `SystemPageId` exists.
- Verification queries return successfully.

## 3. Web Files

Deploy these web files:

- `bin\MyERP.dll`
- `Views\Shared\_Layout.cshtml`
- `Views\SystemSetting\AllowedScreens.cshtml`

Source-only files included for reference:

- `ViewModels\AllowedScreensViewModel.cs`
- `Controllers\HelperController.cs`
- `Controllers\SystemSettings\SystemSettingController.cs`
- `Utils\ERPAuthorize.cs`
- `AI_Docs\CompanyScreenAvailability_Reference_2026-05-11.md`

## 4. Post-Deploy Check

Open:

```text
/SystemSetting/AllowedScreens
```

Expected:

- Screen list loads from `SystemPage`.
- Critical system screens are checked and disabled.
- Saving selected screens writes rows into `dbo.CompanyAllowedPages`.

## 5. Behavior Check

- Confirm allowed modules still follow `AllowedModule`.
- Hide one non-critical screen in `AllowedScreens`.
- Confirm the menu item is hidden after reload.
- Confirm direct URL access redirects to Unauthorized.
- Re-enable the screen and confirm access returns, subject to normal user/role permissions.

## 6. Rollback

To roll back web behavior:

- Restore previous `bin\MyERP.dll`.
- Restore previous `Views\Shared\_Layout.cshtml`.
- Remove or ignore `Views\SystemSetting\AllowedScreens.cshtml`.

Database rollback is optional. Leaving `dbo.CompanyAllowedPages` in place is safe if the old DLL is restored.
