# Sales Invoice Premium Read-Only And Save Preview

Date: 2026-05-07

## Scope

Upgraded the MainErp sales invoice migration screens from technical read-only views into operational ERP workspaces.

Routes:

- `/MainErp/WorkshopSales`
- `/MainErp/WorkshopSales/Details/{id}`
- `/MainErp/PumpSales`
- `/MainErp/PumpSales/Details/{id}`

This phase remains read-only. No save, post, delete, voucher creation, inventory posting, or stored procedure execution was added.

## UI Changes

The detail screen now uses a premium invoice workspace structure:

- cockpit header
- top KPI cards
- invoice summary tab
- detail lines tab
- payment tab
- inventory impact tab
- accounting trace tab
- save-preview tab
- print/attachments placeholder tab

## Accounting Trace

The screen displays linked voucher data from `DOUBLE_ENTREY_VOUCHERS` where available.

Displayed indicators:

- voucher line count
- affected account count
- total debit
- total credit
- balance difference
- balanced/unbalanced badge
- open journal links through `/MainErp/JournalEntries/DetailsByNote/{noteId}`

Account display follows the MainErp rule:

- `Account_Serial - Account_Name`
- raw `Account_Code` remains internal

## Inventory Impact

The screen displays read-only inventory impact from `Transaction_Details`.

Displayed fields include:

- item code/name
- unit
- quantity
- price
- discount
- VAT
- total
- store
- cost
- total estimated cost

Exact final inventory effect still requires validating the VB6 routines:

- `CreateIssueVoucher2`
- `CreateRecieveVoucher`

## Save Preview Only

The "معاينة تأثير الحفظ" area is a simulation only.

It summarizes:

- expected `Transactions` header scope
- expected `Transaction_Details` detail/inventory scope
- currently linked voucher rows
- debit/credit totals
- warnings for missing lines, missing vouchers, or unbalanced rows

It does not write:

- `Transactions`
- `Transaction_Details`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- inventory transaction tables

## Pump Diagnostics

Because Eng may not contain pump samples, `/MainErp/PumpSales` now shows a diagnostic panel.

The panel displays:

- configured database name
- filter used: `Transactions.Transaction_Type IN (21,42,38,9) AND ISNULL(TypeInvoice,0)=2`
- row count found
- `TypeInvoice` and `Transaction_Type` breakdown
- close candidates where details contain `PumpId` or `DetailsPump`

No fake pump data is created or displayed.

## VB6 Mapping Notes

Additional confirmation from `FrmSaleBill6.frm`:

- pump report SQL filters `Transaction_Type = 21`
- pump report SQL filters `TypeInvoice = 2`
- pump detail rows join `tblPumpType`
- pump detail values are stored in `Transaction_Details`

## Safety

- No database writes.
- No `AllScripts.sql` change.
- No `Areas\Pos` change.
- No Cayshny/Kishny source used.
- MainErp continues to use `MainErp_ConnectionString`.
