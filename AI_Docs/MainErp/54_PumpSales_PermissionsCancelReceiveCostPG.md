# Pump Sales Permissions, Cancellation Preview, Receive Voucher, Cost, and PG Review

Date: 2026-05-07

## Scope

This phase starts the remaining high-risk pump sales work after draft save, posting, and draft delete.

Implemented now:

- MainErp button permission gating.
- `CanPostPumpInv` loading from `TblUsers`.
- Cancellation/reversal preview for posted pump invoices.
- Documentation of `CreateRecieveVoucher`, cost postings, and `PG`.

No posted invoice cancellation is executed yet.

## Permission Mapping

MainErp now uses:

- `ScreenJuncUser.CanAdd` / `CanEdit` for create/edit draft actions.
- `ScreenJuncUser.CanDelete` for draft delete.
- `TblUsers.CanPostPumpInv` for post and cancellation preview.
- Admin user can use all actions.
- Rebuild requires admin plus `CanPostPumpInv`.

These are MainErp checks around the Main ERP legacy screen `FrmSaleBill6`.

## Cancellation/Reversal Preview

Added:

- `dbo.MainErp_PumpSales_CancelPreview`
- `PumpSalesController.CancelPreview`
- `SalesInvoiceReadRepository.PreviewPumpInvoiceCancellation`

The preview:

- Requires an existing posted pump invoice.
- Reads the linked `Notes` and `DOUBLE_ENTREY_VOUCHERS`.
- Shows reversal direction for voucher lines.
- Shows linked issue voucher count.
- Shows estimated inventory reversal candidates from `Transaction_Details`.

It does not write:

- no new `Notes`
- no new `DOUBLE_ENTREY_VOUCHERS`
- no new receive/reversal `Transactions`
- no status change

## CreateRecieveVoucher Review

VB6 source:

- `FrmSaleBill6.frm`, `CreateRecieveVoucher`, around line `27670`.

Finding:

- This is not the normal daily pump sale path.
- It is tied to sales returns through `CboRetrunType`.
- It creates a receive voucher:
  - `Transactions.Transaction_Type = 20`
  - linked to the source invoice through `nots = XPTxtBillID`
  - `nots2 = TxtNoteSerial1`
  - `Notes.NoteType = 160`
  - then calls `CREATE_VOUCHER_GE`

Decision:

- Do not execute this automatically from pump posting.
- Implement it later as a reviewed return/cancellation workflow.

## Cost Posting Review

VB6 `PG` includes cost/inventory lines for `InvType = 2`:

- Debit sales cost account: `get_account_code_branch(1, branch)`
- Credit stock account: `get_account_code_branch(0, branch)`

For pump invoices, the legacy code has special calculations around `mTypeInvoice = 2`, VAT, and `LblTotalAll`.

Decision:

- Do not add cost lines to actual posting until sample parity is validated against multiple posted Nagahat pump invoices.
- The cancellation preview now exposes estimated cost from `Transaction_Details.CostPrice * Quantity`.

## PG Review

VB6 `cmdPosted_Click` calls:

- `PG 0, 0, usedaccount, 0, 0`

`PG` handles:

- note creation
- payment debit lines
- sales revenue credit lines
- VAT logic
- cost/inventory lines
- pump-specific payment distribution

Current MainErp posting covers the main payment/sales/VAT behavior and issue voucher creation. Cost line parity and returns remain pending.

## Pending Before Actual Cancellation

- Decide whether cancellation should:
  - create a reversal `Notes` only, or
  - create reversal `Notes` plus a `Transaction_Type=20` receive/reversal inventory document.
- Define final status fields:
  - `Closed`
  - `Posted`
  - `IsPosted`
  - `Approved`
- Confirm whether original voucher should remain untouched and only reversed.
- Add audit UI for cancellation/reversal.

## Safety

- No `Areas/Pos` changes.
- No `AllScripts.sql` changes.
- No posted invoice is deleted.
- No cancellation writes are executed.
