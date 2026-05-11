# CashReceiptVoucher Save Diagnostics - Deploy Steps

Target: original DynamicErp web only. Do not deploy to Kishny POS.

## Package Contents

- `bin/MyERP.dll`

No `.cshtml` or JavaScript files are required for this diagnostic build.

## Deploy

1. Back up the current production `bin/MyERP.dll`.
2. Copy the package `bin/MyERP.dll` over the production original-web `bin/MyERP.dll`.
3. Recycle the IIS application pool.
4. Reproduce the failed save from `CashReceiptVoucher/AddEdit`.

## Diagnostics

The save endpoint now writes temporary diagnostics to:

`App_Data/Logs/CashReceiptVoucher_Save.log`

The browser still receives a safe response only. Raw exceptions are not exposed to users. If the save fails inside the instrumented flow, the AJAX response includes:

- `success: "false"`
- `diagnosticId`
- `stage`

Use the `diagnosticId` to find the matching log block.

## What Is Logged

- Exception message, inner exceptions, stack traces
- SQL exception number, severity, state, procedure, line number, message
- ModelState validation errors
- Stored procedure return values for `CashReceiptVoucher_Insert` / `CashReceiptVoucher_Update`
- `ExecuteSqlCommand` return values for service invoice actual payments and customer party repair
- `SaveChanges` return value after property batch delivery-state updates
- Current user
- Current database name, SQL login, database user, original login, host, app name
- Physical IIS app path, mapped app root, AppDomain base directory
- Masked connection string
- Request form keys and selected critical voucher values
- Invoice-selection mode and selected sales/service invoice payment rows
- Null checks for key payment collections

## If No Log File Appears

Check production permissions for the IIS app pool identity on:

`App_Data/Logs`

Also confirm the logged/actual IIS physical path matches the application being tested. If the DLL is loaded but the folder is not writable, the save response may still show `success: "false"` with `diagnosticId` and `stage`, but the file cannot be created.

## Rollback

After the production root cause is captured, restore the backed-up DLL or deploy a normal release build without temporary diagnostics.
