# Sales Invoice Accounting And Inventory Flow

## Source Scope

Source of truth:

- `F:\Source Code\SatriahMain\Frm\FrmSaleBill6.frm`

This document describes the first traced behavior for the Main ERP form only. Kishny/Cayshny invoice behavior is excluded.

## Important VB6 Routines

Detected routines and responsibilities:

- `SaveData`
  - main save sequence for the invoice header and workflow
- `SaveItemsData`
  - writes invoice item/detail rows
- `SaveSalesPayment`
  - writes linked payment rows
- `SaveSalesMixItems`
  - handles mixed/composite sales item behavior
- `CREATE_VOUCHER_GE`
  - creates accounting voucher rows
- `createVoucher2`
  - secondary voucher creation path
- `CreateIssueVoucher2`
  - inventory issue voucher path
- `CreateRecieveVoucher`
  - inventory receive voucher path
- `Del_TransAction`
  - transaction deletion/rollback behavior
- `PrintReport` and `PrintCash`
  - report/print behavior

## Transaction Modes

Observed transaction mode assignments include:

- `mTransaction_Type = 21` normal invoice
- `mTransaction_Type = 38` internal order / alternate Main ERP mode
- `mTransaction_Type = 9` alternate/return invoice mode
- `mTransaction_Type = 42` appears in retrieval and mode mapping

Retrieval in the read-only web wave uses:

- `Transactions.Transaction_Type IN (21, 42, 38, 9)`

Pump behavior is guarded by:

- `mTypeInvoice = 2`

Additional pump-specific confirmation from the active form:

- Daily pump report SQL uses `Transactions.Transaction_Type = 21`.
- The same report path filters `Transactions.TypeInvoice = 2`.
- Pump detail rows are read from `Transaction_Details` with `PumpId`, cash/mada/visa/deferred amount fields, and pump quantity fields.
- `tblPumpType` is joined for pump identity/name.

## Workshop Sales Flow

Expected legacy sequence before any write migration:

1. User enters invoice header.
2. User enters detail lines.
3. VB6 validates customer, branch, store, payment, totals, and invoice mode.
4. Header is written to `Transactions`.
5. Details are written to `Transaction_Details`.
6. Payment rows are written through sales payment logic when applicable.
7. Inventory issue/receive behavior is executed depending on transaction mode.
8. Accounting voucher rows are created through voucher routines.
9. Printing/report routines may run after save.

First web wave implements only read-only steps after data already exists:

- read invoice header
- read detail lines
- read payment rows
- read linked accounting entries
- show inventory impact summary from line quantities/costs

## Pump Sales Flow

Expected legacy sequence before any write migration:

1. User works in the pump tab of `C1Tab1`.
2. Pump line fields are populated in `Transaction_Details`.
3. Pump-specific quantities and payment splits are calculated.
4. Header/detail rows are saved using the common transaction tables.
5. Voucher creation and inventory impact follow the active mode and pump values.

First web wave implements:

- read-only list filtered to `TypeInvoice = 2`
- read-only details showing pump line columns when present
- read-only accounting and inventory summaries

The diagnostic panel now shows:

- the exact filter used by the web migration
- row count found in Eng
- `TypeInvoice` / `Transaction_Type` breakdown
- close candidates where detail rows include `PumpId` or `DetailsPump`

## Accounting Impact

The first web implementation does not create or rebuild any voucher.

Read-only voucher display is based on:

- `DOUBLE_ENTREY_VOUCHERS`
- linked transaction id columns
- linked note id where available

The web screen shows:

- debit total
- credit total
- balance difference
- balanced/unbalanced badge
- account display as `Account_Serial - Account_Name`

## Inventory Impact

The first web implementation does not update inventory.

It shows a read-only impact summary from line data:

- line count
- quantity totals
- estimated cost totals
- store id
- item/unit references

Inventory posting routines such as `CreateIssueVoucher2` and `CreateRecieveVoucher` remain pending.

## Pending Before Write Phase

The following must be traced in more depth before any save/post phase:

- exact ID allocation behavior for `Transactions`
- invoice serial generation
- edit behavior and rollback order
- delete behavior and inventory rollback
- exact debit/credit accounts by transaction mode
- VAT account selection
- payment posting behavior
- cost-of-sales posting behavior
- pump-specific quantity and amount calculations
- report and print parameters

## Safety Confirmation

Current implementation is read-only:

- no `INSERT`
- no `UPDATE`
- no `DELETE`
- no stored procedure execution
- no `AllScripts.sql` changes
- no POS/Kishny logic dependency

## Save Preview Only

The current web implementation includes a safe save-preview panel. It does not call any VB6 write equivalent and does not write to the database.

The preview summarizes:

- expected header scope from `Transactions`
- detail/inventory impact from `Transaction_Details`
- currently linked voucher lines from `DOUBLE_ENTREY_VOUCHERS`
- warnings for missing lines, missing vouchers, or unbalanced voucher totals

## Read-Only Trace Expansion - 2026-05-07

The web details workspace now exposes more of the real `FrmSaleBill6` lifecycle without enabling writes:

- Header operational flags from `Transactions`: `Posted`, `Approved`, `Prefix`, `Fullcode`, `CBoBasedON`, `POSBillType`, `Transaction_NetValue`, `SumValueLine`, `SumVATLine`, and `DateRec`.
- Related inventory transaction trace from `Transactions` where `Transaction_Type IN (19,20)`.
- Link detection uses the VB6-generated relation:
  - `nots = source Transaction_ID`
  - `nots2 = source NoteSerial1`
- `Transaction_Type = 19` is displayed as an issue voucher generated by `CreateIssueVoucher2`.
- `Transaction_Type = 20` is displayed as a receive voucher generated by `CreateRecieveVoucher`.

This is still read-only. The web code does not call or reproduce `CreateIssueVoucher2` or `CreateRecieveVoucher` yet.
# 2026-05-07 Addendum - Pump Deferred Customer Distribution

`FrmSaleBill6.frm` pump invoices use the VB6 mini form `FrmItemShowDet` for deferred customer distribution inside the grid.

Confirmed behavior:

- `DetailsPump` stores rows as:

```text
CustomerId#UnitId#Amount#CustomerName#Qty#UnitPrice#RecNo#@
```

- Pump save also persists these rows in `Transaction_DetailsPump`.
- `Transaction_DetailsPump.LineID` follows the VB6 grid row / `Transaction_Details.LineID`, not always `Transaction_Details.ID`.
- Current MainErp implementation now reads both:
  - `Transaction_Details.DetailsPump`
  - `Transaction_DetailsPump`
- Persisted `Transaction_DetailsPump` rows take priority for display when available.

Current safe write scope:

- The pump deferred editor can update only the deferred customer distribution fields/tables.
- It does not create or modify:
  - `Notes`
  - `DOUBLE_ENTREY_VOUCHERS`
  - inventory issue/receive transactions
  - full invoice header/detail posting

Pending before full pump save migration:

- Complete migration of `tblPumpType.PercentV` update.
- Complete accounting voucher generation.
- Complete inventory issue/receive voucher generation.
- Full audit and permission gates for save/post/delete.
