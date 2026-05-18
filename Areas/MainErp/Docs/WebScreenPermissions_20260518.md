# Web Screen Permissions - 2026-05-18

## What Changed

- Added a web-only catalog: `WebModules`, `WebScreens`, `WebScreenPermissions`.
- Added optional support tables: `WebPermissionRoleTemplates`, `WebPermissionRoleTemplateItems`, `WebScreenLegacyMap`.
- POS and MainERP are separated in the customer matrix. POS shows POS screens only; MainERP shows MainERP screens only.
- Rebuilt `/MainErp/Permissions` as a shared Arabic RTL matrix for MainERP and POS-hosted access.
- POS opens the POS-native wrapper `/Pos/WebPermissions/Index`, using `KishnyCashConnection` and `POS` catalog rows only. MainERP keeps `/MainErp/Permissions`.
- The UI displays Arabic web captions and route/screen key metadata. It does not list legacy `Frm...` names.

## Legacy Compatibility

- `ScreenJuncUser` is untouched.
- Existing POS flags such as `CanEditSalesInvoice`, `CanEditSalesInvoicePos`, KYC print/edit flags, report flags, and temporary POS permissions remain in the current POS flow.
- `WebScreenLegacyMap` maps legacy names such as `FrmSaleBill6 -> POS.Sales.Index`; it is used only to seed the new web permissions, while the UI keeps Arabic web captions.
- `POS_UserPermissions` seeds POS defaults for teller, KYC, reports, payments/custody, journal, Excel, and limited invoice edit permissions.

## Runtime Enforcement Status

- New central service: `WebScreenPermissionService`.
- New action filter: `WebScreenAuthorizeAttribute`.
- The permissions screen itself is protected by `POS.Admin.WebPermissions` from POS and `MainERP.Admin.WebPermissions` from MainERP.
- MainERP and POS sidebar entries for the web permissions page are hidden when the matching project-specific `CanView` is false.
- Existing legacy/POS runtime checks remain active until each screen is migrated to the new `WebScreenAuthorizeAttribute` or a route-level adapter.

## SQL Deployment

- Script: `Areas/Pos/Sql/106_WebScreenPermissions_Catalog.sql`
- POS manifest: `Areas/Pos/Sql/POS_SQL_AutoUpdate_Manifest.json`
- Procedure uses `DROP PROCEDURE` + `CREATE PROCEDURE`: `dbo.usp_WebScreenPermissions_SeedCatalog`.

## Testing Checklist

- Open permissions from POS: `/Pos/WebPermissions/Index`.
- Open permissions from MainERP: `/MainErp/Permissions`.
- Assign `POS.KYC.Index` view/edit/print permission to a test user.
- Remove `POS.KYC.Index` view permission and verify the permissions matrix records it as disabled.
- Verify direct URL protection on `/MainErp/Permissions` for MainERP and `/Pos/WebPermissions/Index` for POS non-admin users without the matching web permission.
- Assign print/export permission and verify the matrix export reflects it.
- Copy permissions from an admin-like user to a test user.
- Apply a POS KYC or MainERP Accountant template to a user.
- Confirm no old `Frm...` names appear in the main permissions UI.
