# Release Notes - Company Screen Availability

Date: 2026-05-11
Scope: Original DynamicErp web application only

## Summary

This release adds a company/client-level screen availability layer on top of existing global module availability and existing user/role permissions.

## What Changed

- `AllowedModule` remains the global purchased/available module layer.
- Added `CompanyAllowedPages` as the global company-level screen availability table.
- Added management screen:
  - `/SystemSetting/AllowedScreens`
- Added menu filtering in the main web layout:
  - module visibility is still applied first from `AllowedModule`
  - screen visibility is then applied from `CompanyAllowedPages`
- Added safe direct URL protection in `ERPAuthorize`.
- Added lockout protection for critical system screens.

## Critical Screens Always Available

- `SystemSetting`
- `AllowedModule`
- `AllowedScreens`
- `ERPUsers`
- `ERPRoles`
- `RolePrivilege`
- `UserPrivilege`

## Backward Compatibility

- If `dbo.CompanyAllowedPages` does not exist, behavior remains unchanged.
- If `dbo.CompanyAllowedPages` exists but has no rows, behavior remains unchanged.
- Existing `RolePrivilege` and `UserPrivilege` behavior is preserved.
- No POS/Kishny changes are included in this release.

## Database

Run:

```sql
Scripts/CompanyAllowedPages_Setup.sql
```

The script is SQL Server 2012 compatible, idempotent, and does not delete existing data.

## Verification

Build verified successfully with Visual Studio MSBuild:

```text
MyERP -> bin\MyERP.dll
```

The build completed with existing warnings only.
