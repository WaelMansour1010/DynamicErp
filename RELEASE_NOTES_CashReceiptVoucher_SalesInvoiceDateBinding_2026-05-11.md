# CashReceiptVoucher SalesInvoiceDate Binding Fix

## Scope

Original DynamicErp web only.

## Root Cause

Production was rejecting `CashReceiptVoucher/AddEdit` POST before save because MVC model binding added a `ModelState` error for:

`cashReceiptVoucher.SalesInvoiceActualPayments[n].SalesInvoiceDate`

The posted value can come from display text in the invoice grid with leading spaces, for example:

`  12/29/2025`

That made `ModelState.IsValid` false, so the save flow returned the generic not-saved response before the stored procedure save logic.

## Fix

The CashReceiptVoucher POST now normalizes only `SalesInvoiceActualPayments[].SalesInvoiceDate` before the `ModelState.IsValid` check:

- trims incoming date text
- accepts `MM/dd/yyyy`
- accepts `dd/MM/yyyy`
- accepts `yyyy-MM-dd`
- treats blank values as `null`
- clears only the matching date binding error when parsing succeeds or value is blank
- leaves ModelState invalid and logs a targeted diagnostic if parsing still fails

No global culture setting was changed.

## Files Changed

- `Controllers/AccountSettings/CashReceiptVoucherController.cs`

## Deploy

Copy `bin/MyERP.dll` to the original DynamicErp web `bin` folder and recycle the IIS application pool.

No SQL, `.cshtml`, or JavaScript deployment is required for this fix.
