# Company Screen Availability

## Layers

1. `AllowedModule`
   - Global company/client module availability.
   - Represents purchased or enabled modules.
   - Used by the existing module menu filter through `/Helper/GetNotActiveModules`.

2. `CompanyAllowedPages`
   - Global company/client screen availability inside enabled modules.
   - Managed from `/SystemSetting/AllowedScreens`.
   - Uses `SystemPage` as the source of screens.
   - Not user-specific and not role-specific.

3. `RolePrivilege` and `UserPrivilege`
   - Existing per-role and per-user authorization layers.
   - These remain unchanged and still decide which user can access an available screen/action.

## Menu Behavior

The main web layout keeps the existing module filter first:

- `/Helper/GetNotActiveModules`
- `AllowedModule.SystemPageId`
- hidden module ids in `Views/Shared/_Layout.cshtml`

Then it applies company-level screen availability:

- `/Helper/GetCompanyAllowedScreensForMenu`
- `CompanyAllowedPages`
- `SystemPage.ControllerName`
- existing `pv` values are parsed only to identify the controller part of the menu URL

If `CompanyAllowedPages` does not exist or has no rows, screen filtering is skipped for backward compatibility.

## Direct URL Behavior

`ERPAuthorize` now performs a safe company-level availability check before the existing role/user permission checks.

Fail-open cases:

- `CompanyAllowedPages` table is missing
- `CompanyAllowedPages` has no rows
- no matching active `SystemPage` exists for the controller

Always available critical controllers:

- `SystemSetting`
- `ERPUsers`
- `ERPRoles`
- `RolePrivilege`
- `UserPrivilege`

These prevent accidental lockout from the screens needed to recover access.

## Deployment

Run:

```sql
Scripts/CompanyAllowedPages_Setup.sql
```

The script is idempotent and does not delete existing data.
