# Pump Deferred Customer Distribution Edit Screen

Date: 2026-05-07

## Scope

Added a MainErp-only screen for the delicate pump deferred-customer split that VB6 opens through `FrmitemShowDet`.

Route:

- `/MainErp/PumpSales/DeferredDistribution?transactionId={id}&lineId={lineId}`

This screen belongs to the Main ERP migration of `FrmSaleBill6`; it is not Kishny/POS logic.

## VB6 Behavior Preserved

`FrmitemShowDet.frm` writes a serialized customer distribution string back into the selected `FrmSaleBill6` grid row:

`CustomerId#UnitId#Amount#CustomerName#Qty#UnitPrice#RecNo#@`

The web screen parses and rebuilds the same `DetailsPump` format.

## Implemented

- Loads the selected pump invoice line from `Transaction_Details`.
- Displays existing `DetailsPump` customer allocations.
- Shows current line `Deferred` and `DeferredQty`.
- Lets the user edit customer id/name, unit, quantity, price, amount, and reference number.
- Rebuilds the legacy `DetailsPump` string.
- Saves through `dbo.MainErp_SalesInvoice_SavePumpDeferredDistribution`.
- The stored procedure updates only:
  - `Transaction_Details.DetailsPump`
  - `Transaction_Details.Deferred`
  - `Transaction_Details.DeferredQty`

## Safety

The save is intentionally narrow.

It does not write:

- `Transactions`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- inventory issue/receive vouchers
- posting rows

It refuses update when the invoice is closed, posted, or approved.

## Still Pending

- Customer lookup popup/search.
- Unit lookup.
- Automatic amount recalculation in JavaScript.
- Final full invoice save/post flow.
- Accounting posting and inventory voucher creation.
