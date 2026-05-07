# FrmSaleBill6 Operational Workbench Phase 2

Date: 2026-05-07

## Goal

Continue the Main ERP `FrmSaleBill6` migration by making the read-only web screens more operationally useful, while still preventing all financial/inventory writes.

Routes covered:

- `/MainErp/WorkshopSales`
- `/MainErp/WorkshopSales/Details/{id}`
- `/MainErp/PumpSales`
- `/MainErp/PumpSales/Details/{id}`

## Implemented

### VB6 Routine Stage Panel

The invoice summary now shows the current safe migration stage:

- `SaveData`: disabled.
- `CreateIssueVoucher2`: trace only.
- `CreateRecieveVoucher`: trace only.
- `CREATE_VOUCHER_GE/createVoucher2`: read-only voucher display only.

This is intended to make the screen feel like a real migration of `FrmSaleBill6`, not a disconnected data viewer.

### Payment Readiness

The details screen now compares:

- `TblSalesPayment` rows total.
- Header `PayedValue`.
- Header `RemainValue`.
- Difference between payment row total and header paid value.

This prepares the later payment-save migration without writing any payment rows.

### Pump Line Enhancement

Pump detail rows now display more of the pump-specific fields:

- `PumpId`
- `PumpName`
- `PrevQty`
- `CurrentQty`
- `Cash`
- `Mada`
- `Visa`
- `Deferred`
- `DetailsPump`
- `IsOther`
- `ColorID`
- commission account from `Account_CodeComm`

Account display still follows:

`Account_Serial - Account_Name`

Raw account code remains internal.

### Report/Print Map

The print tab now maps known VB6 print/report entry points:

- `PrintReport`
- `cmdPrint*`
- `PrintCash`
- `DailyPumpR.Rpt`

No Crystal Report is executed yet.

## Safety

Still read-only:

- no `INSERT`
- no `UPDATE`
- no `DELETE`
- no stored procedure execution
- no inventory voucher creation
- no accounting voucher creation
- no report execution

No `Areas\Pos` files were modified for this phase.

No `AllScripts.sql` changes were made.

## Pending

Before any write/save phase:

- map `SaveSalesPayment` exactly
- map `SaveItemsData`
- map `SaveSalesMixItems`
- map exact inventory issue/receive detail insert behavior
- map VAT and QR/e-invoice branches
- map delete/reversal behavior
- validate pump data samples in a database that contains `TypeInvoice = 2`
