# Pump Sales Draft Delete Safety

Date: 2026-05-07

## Scope

This phase adds a safe delete path for MainErp pump sales invoices.

Implemented operation:

- `dbo.MainErp_PumpSales_DeleteDraft`
- `PumpSalesController.DeleteDraft`
- `SalesInvoiceReadRepository.DeletePumpInvoiceDraft`
- UI buttons on the pump invoice details workspace:
  - معاينة حذف المسودة
  - حذف المسودة

## Safety Rules

The delete procedure only allows deleting an unposted draft pump invoice.

It blocks deletion when any of these are true:

- `Transactions.Closed` is set.
- `Transactions.Posted` is set.
- `Transactions.Approved` is set.
- `Transactions.IsPosted` is set.
- A linked `Notes` row exists.
- A linked `DOUBLE_ENTREY_VOUCHERS` row exists.
- A linked inventory issue/receive transaction exists through `Transactions.nots`.

Posted invoices still require a separate reviewed cancel/reversal workflow. They are not deleted by this phase.

## Rows Deleted

For an allowed draft only, the procedure deletes:

- `Transaction_DetailsPump`
- `TblSalesPayment`
- `Transaction_Details`
- `Transactions`

It writes an audit row into:

- `MainErp_AuditLog`

## Pump Reading Rollback

The procedure rolls back `tblPumpType.PercentV` to the line `PrevQty` only when the current `PercentV` still equals the invoice line `CurrentQty`.

This guard avoids overwriting a newer pump reading that may have been recorded after the draft.

## What Is Still Pending

- Posted invoice cancellation/reversal.
- Destructive rebuild/delete equivalent to legacy VB6.
- Final permission matrix for post/rebuild/delete buttons.
- Full audit UI.

## Database Changes

No legacy `AllScripts.sql` change was made.

The SQL lives only in:

- `Areas/MainErp/Sql/03_SalesInvoice_ReadWrite_Procedures.sql`
