# Release Notes - Original Web Minimal Deploy (2026-05-11)

## Scope
Original DynamicErp web application only. This package is not for Kishny POS.

## Included Changes
- AllowedModule stabilization for per-user module selections with legacy fallback.
- Sync menu integration with AllowedModule using the Sync module `SystemPage` id.
- CashReceiptVoucher save diagnostic logging in the compiled application.

## Deployment Contents
- `bin/MyERP.dll`
- `Areas/Sync/Views/Shared/_SyncErpMenuItem.cshtml`
- `Scripts/2026-05-11_Add_Sync_Module_SystemPage.sql`
- `Scripts/VERIFY_AllowedModule_Sync_2026-05-11.sql`
- `AI_Docs/AllowedModule_Stabilization_Reference_2026-05-11.md`
- `RELEASE_NOTES.md`
- `DEPLOY_STEPS.md`

## CashReceiptVoucher Diagnostic Logging
The diagnostic logging is included in `bin/MyERP.dll`.

Log file path on the deployed site:
`App_Data/Logs/CashReceiptVoucher_Save.log`

Raw exceptions are not shown to end users.

## Exclusions
- No full website publish.
- No source `Controllers/*.cs`.
- No POS/Kishny deploy files.
- No `obj`, `packages`, or old `bin` extras.
