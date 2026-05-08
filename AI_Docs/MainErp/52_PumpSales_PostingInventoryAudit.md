# Pump Sales Posting, Inventory, Report, and Audit

Date: 2026-05-07

## Implemented

Stored procedures were added under:

`Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`

### `dbo.MainErp_PumpSales_Post`

Implements the first real posting path for Main ERP pump sales:

- Validates the invoice is a Main ERP pump invoice:
  `Transactions.Transaction_Type = 21` and `TypeInvoice = 2`.
- Blocks closed, approved, or `Posted` invoices.
- Blocks already `IsPosted` invoices unless `@ForceRebuild = 1`.
- Validates pump quantities are fully distributed:
  `CurrentQty - PrevQty - CashQty - MadaQty - VisaQty - DeferredQty = 0`.
- Creates or updates the invoice `Notes` row.
- Deletes/rebuilds linked `DOUBLE_ENTREY_VOUCHERS` rows for the invoice note.
- Generates debit/credit lines for:
  - cash
  - Visa
  - Mada
  - bank commission
  - commission VAT split
  - deferred customer allocations from `Transaction_DetailsPump`
  - sales revenue from `branches.a2`
  - output VAT from `TblSettsReqLimK` for `TransType = 21`
- Sets:
  - `Transactions.IsPosted = 1`
  - `Transactions.UserPosted = @UserId`
- Creates a linked inventory issue transaction `Transaction_Type = 19` if one is not already linked by `Transactions.nots`.
- Writes audit to `dbo.MainErp_AuditLog`.

### `dbo.MainErp_AuditLog`

Created as a MainErp migration audit table when missing.

Columns:

- `AuditId`
- `OperationName`
- `EntityName`
- `EntityKey`
- `UserId`
- `CorrelationId`
- `Message`
- `CreatedAt`

## UI Wiring

Updated:

- `/MainErp/PumpSales/Details/{id}`

Added actions:

- `معاينة الترحيل`
- `ترحيل فعلي`
- `إعادة بناء القيود`
- `تقرير المضخات`

Added route:

- `/MainErp/PumpSales/DailyReport/{id}`

The report view reproduces the data shape used by `DailyPumpR.Rpt`:

- pump summary by pump/product
- previous/current readings
- sold liters
- cash/Mada/Visa/deferred quantities and amounts
- grouped invoice line display

## Nagahat Validation

Database:

`Nagahat`

### Posting Preview

Transaction:

- `74004`

Result:

- Voucher lines: `11`
- Debit total: `431.5201`
- Credit total: `431.5201`
- No database write in dry run.

### Actual Rebuild/Post Test

Transaction:

- `74004`

Result:

- `NoteId = 46407`
- New rebuilt `VoucherId = 156009`
- Voucher lines: `11`
- Debit total: `431.5201`
- Credit total: `431.5201`
- Issue voucher count linked by `nots = 74004`: `1`
- Audit row created with operation `PumpSales.Post`.

### Invalid Sample Protection

Transaction:

- `95484`

Result:

- Posting preview was blocked because pump quantities are not fully distributed.

### Edit Protection

Draft save preview against posted transaction `74004` is now blocked with:

`Pump invoice is already closed/posted/approved and cannot be edited.`

## Still Pending

Not fully migrated yet:

- Full `CreateRecieveVoucher` return-flow behavior.
- Full cost accounting voucher for inventory issue if the VB6 configuration requires it.
- Full Crystal Reports execution engine for `.rpt`; current implementation provides a web report equivalent for the `DailyPumpR.Rpt` datasets.
- Delete invoice workflow. It should be implemented separately with stronger audit and reversal rules.
- Permission matrix enforcement for:
  - `MainErp.PumpSales.Post`
  - `MainErp.PumpSales.Rebuild`
  - `MainErp.PumpSales.Delete`

## Safety Notes

- `Areas\Pos` was not intentionally modified for this phase.
- `AllScripts.sql` was not modified.
- Posting logic is isolated under `Areas\MainErp`.
