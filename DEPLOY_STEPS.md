# Deploy Steps - Original Web Minimal Deploy (2026-05-11)

## Target
Original DynamicErp web application only.

## SQL Execution Order
1. Run `Scripts/2026-05-11_Add_Sync_Module_SystemPage.sql` on the target `MyErp` database.
2. Run `Scripts/VERIFY_AllowedModule_Sync_2026-05-11.sql` to verify the Sync module row and AllowedModule mappings.

## File Deployment
1. Backup the current production `bin/MyERP.dll`.
2. Copy `bin/MyERP.dll` from this package to the website `bin` folder.
3. Copy `Areas/Sync/Views/Shared/_SyncErpMenuItem.cshtml` to the same relative path on the website.
4. Recycle the IIS application pool.

## Verification
- Confirm the Sync menu appears according to AllowedModule permissions.
- Confirm AllowedModule changes persist per user.
- For CashReceiptVoucher save failures, check:
  `App_Data/Logs/CashReceiptVoucher_Save.log`

## Notes
- Do not deploy this package to POS/Kishny.
- This package intentionally excludes source controllers and full-site content.
