# Pump Sales Full Draft Save - MainErp

Date: 2026-05-07

## Scope Implemented

This phase starts the Main ERP `FrmSaleBill6` pump invoice write path under `Areas\MainErp` only.

Implemented now:

- `/MainErp/PumpSales/New`
- `/MainErp/PumpSales/Edit/{id}`
- Full draft save for pump invoice header in `Transactions`.
- Full draft save for pump lines in `Transaction_Details`.
- Full draft save for payment rows in `TblSalesPayment`.
- Rebuild of pump deferred customer allocations in `Transaction_DetailsPump` from the VB6 `DetailsPump` format.
- Update of `tblPumpType.PercentV` from each pump line current quantity.
- Draft locking: closed, posted, approved, or `IsPosted` invoices are blocked.
- Save preview path that validates the same data without writing.

## Stored Procedure

Created in:

`Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`

Procedure:

- `dbo.MainErp_PumpSales_SaveDraftFull`

The procedure is SQL Server 2012 compatible and uses XML input for line/payment batches.

## VB6 Behavior Mapped

Source:

`F:\Source Code\SatriahMain\Frm\FrmSaleBill6.frm`

Mapped behavior:

- `mTypeInvoice = 2` represents pump sales.
- Pump line payment buckets: `Cash`, `Mada`, `Visa`, `Deferred`.
- Pump line quantities: `CashQty`, `MadaQty`, `VisaQty`, `DeferredQty`.
- Save validation requires:
  `CurrentQty - PrevQty - CashQty - MadaQty - VisaQty - DeferredQty = 0`
- Deferred customer distribution uses VB6 row format:
  `CustomerId#UnitId#Amount#CustomerName#Qty#UnitPrice#RecNo#@`
- `tblPumpType.PercentV` is updated to the saved `CurrentQty`.

## Explicitly Not Implemented Yet

These remain pending because they create real financial/inventory effects:

- `CreateIssueVoucher2`
- `CreateRecieveVoucher`
- `PG`
- `Notes` creation.
- `DOUBLE_ENTREY_VOUCHERS` creation.
- `Transaction_Type = 19` issue voucher creation.
- `Transaction_Type = 20` receive voucher creation.
- `DailyPumpR.Rpt` real Crystal report binding.
- Final audit table write.
- Final posted/approved workflow.
- Delete workflow.

## Nagahat Validation

Connection used:

`Data Source=Wael\Sql2019;Initial Catalog=Nagahat;Integrated Security=True;MultipleActiveResultSets=True;`

Validation samples:

- Transaction `95484`: read succeeded, but save preview correctly failed because existing pump quantities are not fully distributed.
- Transaction `74004`: read/edit loaded successfully.

Transaction `74004` dry-run:

- Lines: `12` UI rows, 1 active saved line.
- BranchId: `14`
- StoreId: `11`
- BoxId: `2`
- CustomerId: `2`
- Still quantity total: `0`
- Result: success, no write.

Transaction `74004` actual draft save:

- Before details count: `1`
- After details count: `1`
- Before payment count: `4`
- After payment count: `4`
- Before line total: `427.28`
- After line total: `427.28`
- Still quantity after save: `0`

No Notes, vouchers, or inventory documents were created by this phase.

## Next Required Phase

Before enabling real posting, migrate and validate these from VB6:

- Note numbering and `CreateNotes` parameters for pump invoices.
- `PG` debit/credit lines, including cash, Mada, Visa, deferred, VAT, customer accounts, and commission accounts.
- Inventory issue/receive generation through `CreateIssueVoucher2` and `CreateRecieveVoucher`.
- Safe rebuild/delete rules for linked vouchers.
- Audit trail for save/post/edit.
- `DailyPumpR.Rpt` parameters and data source.
