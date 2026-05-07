# FrmSaleBill6 Main ERP Source Map

Date: 2026-05-07

## Active Source

`Account.vbp` references:

- `Form=Frm\FrmSaleBill6.frm`

Active source used:

- `F:\Source Code\SatriahMain\Frm\FrmSaleBill6.frm`

Other copies found but not used as source:

- `F:\Source Code\SatriahMain\Cayshny\Frm\FrmSaleBill6.frm`
- `F:\Source Code\SatriahMain\Cayshny\_backup_frm\Frm\FrmSaleBill6.frm`

## Screen Structure

The form contains `C1Tab1` with two business tabs. The migration splits them into:

- Workshop Sales: `/MainErp/WorkshopSales`
- Pump Sales: `/MainErp/PumpSales`

The VB6 form is very large and contains legacy optional paths. The first web phase targets useful read-only business behavior only.

## Key VB6 Routines

- `Option2_Click`: switches sales transaction modes and sets `mTransaction_Type`.
- `SaveData`: main save routine for `Transactions`, `Transaction_Details`, VAT, payments, notes, QR/e-invoice, inventory and accounting side effects.
- `SaveItemsData`: linked item/inventory behavior.
- `SaveSalesPayment`: writes `TblSalesPayment`.
- `SaveSalesMixItems`: writes `TblSalesMixItems`.
- `CREATE_VOUCHER_GE` / `createVoucher2`: accounting voucher creation.
- `CreateIssueVoucher2`: issue/inventory voucher behavior.
- `CreateRecieveVoucher`: receive/return style voucher behavior.
- `Del_TransAction`: delete/reversal behavior.
- `PrintReport`, `PrintCash`, `cmdPrint*`: printing/report flows.

## Transaction Modes

Observed in the active form:

- `mTransaction_Type = 21` for normal invoice mode.
- `mTransaction_Type = 38` for internal order mode.
- `mTransaction_Type = 9` for return/alternate invoice mode.
- `mTransaction_Type = 42` appears in retrieval and mode mapping.

The VB6 retrieval uses:

- `Transactions.Transaction_Type IN (21, 42, 38, 9)`
- `Transactions.TypeInvoice = mTypeInvoice`

Pump-specific logic is guarded by:

- `If mTypeInvoice = 2 Then ...`

## Tables Touched By VB6

Core:

- `Transactions`
- `Transaction_Details`
- `TblCustemers`
- `TblBranchesData`
- `TblItems`
- `TblUnites`
- `TblItemsUnits`

Accounting:

- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- `ACCOUNTS`

Payments/VAT:

- `TblSalesPayment`
- `TransactionValueAdded`
- `TblNotesBillBuyPayment2`
- `TblBillBuyPayment2`

Pump-specific:

- `tblPumpType`
- pump fields inside `Transaction_Details`: `PumpId`, `PrevQty`, `CurrentQty`, `Cash`, `Mada`, `Visa`, `Deferred`, `CashQty`, `MadaQty`, `VisaQty`, `DeferredQty`, `AmountH`, `AmountHComm`, `DetailsPump`, `Account_Code`, `Account_CodeComm`, `IsOther`.

## Excluded In First Wave

- Any save/edit/post/delete behavior.
- QR/e-invoice status updates.
- Crystal report execution.
- payment creation or allocation.
- inventory issue/receive creation.
- obsolete UI-only controls and old optional searches.
- Cayshny/Kishny POS-specific behavior.

## Phase 2 Operational Workbench Additions

The web migration now displays a routine-stage panel so users can see which VB6 behaviors are mapped and which remain disabled:

- `SaveData`: disabled.
- `SaveSalesPayment`: read-only comparison through `TblSalesPayment`.
- `CreateIssueVoucher2`: related `Transaction_Type=19` rows are shown as trace only.
- `CreateRecieveVoucher`: related `Transaction_Type=20` rows are shown as trace only.
- `CREATE_VOUCHER_GE/createVoucher2`: linked `DOUBLE_ENTREY_VOUCHERS` rows are shown only.
- `PrintReport`, `PrintCash`, `DailyPumpR.Rpt`: documented in the report tab but not executed.

Additional pump line fields are now surfaced when present:

- `Account_CodeComm`
- `IsOther`
- `ColorID`
- `DetailsPump`

These additions remain read-only and do not change database state.
