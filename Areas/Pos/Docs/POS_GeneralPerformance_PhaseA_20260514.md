# POS General Performance Phase A - 2026-05-14

Scope: `Areas\Pos` only.

Goal: reduce chatty AJAX, broad searches, duplicate requests, and repeated panel reloads without changing business logic or invoice/accounting save behavior.

## Auto Calls Removed Or Restricted

- POS today invoice panel:
  - Added an explicit `بحث` button beside `todayInvoiceSearch`.
  - Typing in the today invoice search box no longer fires AJAX.
  - Changing type/date/Excel filters no longer auto-runs the search; it shows a prompt to press search.
  - Enter in the search input still runs the search.
- POS sales invoice index:
  - Typing in `salesSearchText` no longer fires AJAX.
  - Changing sales index filters no longer auto-runs the search; it shows a prompt to press `بحث البيانات`.
  - Enter in the search input still runs the search.
- KYC manual search:
  - Manual search runs only from the Search button or Enter in `kycSearchTerm`.
  - Existing exact-length auto lookup for complete phone/national-id/card token remains, with stronger debounce and stale request protection.
- Available Keshni card/token lookup:
  - Auto lookup while typing now waits for at least 3 characters.
  - Store is required before the lookup runs.
  - Overlapping token lookup requests are aborted before a new request is sent.
- Dashboard:
  - Advanced dashboard still loads only by explicit `تحميل مؤشرات الأداء`.
  - Duplicate dashboard clicks are blocked while the request is running.

## Search Minimums Applied

- Manual KYC search: minimum 3 characters on client and server.
- Invoice broad search: minimum 3 characters unless the term looks exact (`numeric >= 3` or length >= 8).
- Available card/token search: minimum 3 characters for typed token lookup; explicit first-available list remains available through the button when a store exists.
- Payments search: requires a date range or a specific search term of at least 3 characters.
- Purchase invoice search: requires a date range or invoice/supplier term of at least 3 characters.
- Stock transfer search: requires a date range or voucher/item/serial term of at least 3 characters.

## Duplicate-Click Protections Applied

- `salesSearchBtn`
- `searchTodayInvoicesBtn`
- `kycSearchBtn`
- `searchPaymentBtn`
- `purchaseSearchBtn`
- `stockSearchBtn`
- `reloadAdminDashboardBtn`

Buttons are disabled and show a loading label while their request is in flight.

## Admin All-Branch Protection

For invoice search, admin/all-branch requests now require one of:

- a selected branch,
- a date range of 7 days or less,
- or an exact-looking search term.

This is enforced in both `pos-transaction.js` and `PosTransactionController.GetTodayInvoices`.

## After Save/Delete/Cancel

- After invoice save:
  - The code no longer reloads the today invoice list automatically.
  - It refreshes balances only and marks invoice lists as needing manual refresh.
- After invoice delete:
  - Matching rows are removed from the cached today/sales lists instead of reloading both lists.
- After invoice cancel:
  - Matching cached rows are marked cancelled and re-rendered.
- Risky full reload left unchanged:
  - `DeleteExcelInvoicesForRange` still reloads sales index and today invoices because it can affect many invoices at once and a local cache patch would be risky.
  - Opening an invoice after cancel still calls `loadInvoiceForReview(transactionId)` because the invoice detail panel must reflect the persisted cancellation state.

## Expected User-Visible Improvement

- POS sale screen feels quieter because typing no longer triggers invoice searches.
- KYC search is less likely to flood `TblCusCsh` while a user types.
- Admin invoice search avoids accidental all-branch long-range scans.
- Payments, purchase, and stock search buttons cannot fire duplicate requests.
- Save/delete/cancel no longer blindly refresh every invoice panel, reducing post-write UI delay.

## Files Changed

- `Areas\Pos\Controllers\PosTransactionController.cs`
- `Areas\Pos\Controllers\PaymentsController.cs`
- `Areas\Pos\Controllers\PurchaseInvoiceController.cs`
- `Areas\Pos\Controllers\StockTransferController.cs`
- `Areas\Pos\Scripts\pos-transaction.js`
- `Areas\Pos\Scripts\purchase-invoice.js`
- `Areas\Pos\Scripts\stock-transfer.js`
- `Areas\Pos\Views\Payments\Index.cshtml`
- `Areas\Pos\Views\PosDashboard\Index.cshtml`
- `Areas\Pos\Views\PosTransaction\Index.cshtml`
