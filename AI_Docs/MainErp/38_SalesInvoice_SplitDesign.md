# Sales Invoice Split Design

## Source

Active Main ERP VB6 form:

- `F:\Source Code\SatriahMain\Frm\FrmSaleBill6.frm`

The source was confirmed from:

- `F:\Source Code\SatriahMain\Account.vbp`
- reference: `Form=Frm\FrmSaleBill6.frm`

Kishny/Cayshny copies of `FrmSaleBill6.frm` are not used as the source for this migration.

## VB6 To Web Split

`FrmSaleBill6.frm` contains two business modes inside one VB6 form through `C1Tab1`.

The web migration splits them into two MainErp screens:

- `/MainErp/WorkshopSales` - فاتورة مبيعات الورشة
- `/MainErp/PumpSales` - فاتورة مبيعات المضخات

This avoids one oversized web page while preserving the real Main ERP source behavior.

## Shared Read-Only Structure

Both screens share the same read-only invoice workspace pattern:

- list/search by date, invoice serial, customer, and branch
- invoice header
- customer and branch information
- payment totals
- detail lines from `Transaction_Details`
- inventory impact summary
- payment rows from `TblSalesPayment`
- journal/voucher rows from `DOUBLE_ENTREY_VOUCHERS`
- safe links to journal details when a note id exists

No save, edit, post, delete, or inventory update operation is enabled in this wave.

## Workshop Sales Invoice

Expected source mapping:

- Header table: `Transactions`
- Detail table: `Transaction_Details`
- Customer lookup: `TblCustemers`
- Branch lookup: `TblBranchesData`
- Store lookup: `TblStoresData` if available in target database
- Items lookup: `TblItems`
- Units lookup: `TblUnites`
- Payment rows: `TblSalesPayment`
- Accounting rows: `DOUBLE_ENTREY_VOUCHERS`

Initial filter:

- `Transactions.Transaction_Type IN (21, 42, 38, 9)`
- `ISNULL(Transactions.TypeInvoice, 0) <> 2`

Important displayed sections:

- بيانات الفاتورة
- بنود الفاتورة
- أثر المخزون
- المدفوعات
- القيود المحاسبية
- الطباعة والتقارير

## Pump Sales Invoice

Expected source mapping:

- Header table: `Transactions`
- Detail table: `Transaction_Details`
- Pump-specific line fields:
  - `PumpId`
  - `PrevQty`
  - `CurrentQty`
  - `Cash`
  - `Mada`
  - `Visa`
  - `Deferred`
  - `CashQty`
  - `MadaQty`
  - `VisaQty`
  - `DeferredQty`
  - `DetailsPump`
- Pump lookup: `tblPumpType`

Initial filter:

- `Transactions.Transaction_Type IN (21, 42, 38, 9)`
- `ISNULL(Transactions.TypeInvoice, 0) = 2`

Important displayed sections:

- بيانات الفاتورة
- بنود الفاتورة مع بيانات المضخة
- أثر المخزون والكمية
- المدفوعات
- القيود المحاسبية
- الطباعة والتقارير

## Excluded In This Wave

The following are intentionally not migrated yet:

- `SaveData`
- `SaveItemsData`
- `SaveSalesPayment`
- `SaveSalesMixItems`
- `CREATE_VOUCHER_GE`
- `createVoucher2`
- receive/issue voucher creation
- edit/delete behavior
- final print/report execution
- any POS/Kishny card, token, KYC, commission, cashier-close, or service-specific behavior

## UI Rule

The screen should feel like a modernized Main ERP invoice workspace, not a POS invoice screen and not a generic CRUD page.

Dangerous commands are disabled until the accounting and inventory behavior is fully mapped and approved.
