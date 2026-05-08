# Pump Sales Inventory Cost And Audit

Date: 2026-05-07

## Scope

This phase starts the inventory cost voucher work for MainErp pump sales without changing the existing default posting behavior.

Implemented:

- Added controlled `IncludeInventoryCost` support to `dbo.MainErp_PumpSales_Post`.
- Added cost-of-sales preview from `Transaction_Details.CostPrice * Quantity`.
- Added PG account mapping from `branches`:
  - `branches.a1` = cost of sales debit account.
  - `branches.a0` = inventory credit account.
- Added a UI action for inventory cost preview only: `معاينة قيد التكلفة`.
- Added a read-only Audit section inside the invoice details workspace from `MainErp_AuditLog`.

## Safety Decision

The current default posting path still uses:

- sales/payment/VAT entries only
- `IncludeInventoryCost = 0`

Reason: legacy Nagahat samples such as transaction `74004` and `28946` show existing posted sales vouchers without cost-of-sales lines in the same main voucher, even though the PG account mapping exists. Therefore actual cost posting is implemented as a controlled stored procedure option and exposed from the UI as preview only until final approval.

## Cost Posting Preview Logic

When `IncludeInventoryCost = 1`, the posting preview adds:

- Debit: `branches.a1` cost of sales.
- Credit: `branches.a0` inventory.
- Amount: `SUM(Transaction_Details.CostPrice * ISNULL(Quantity, ShowQty))`.

The entries are included in the same validation pipeline as the existing voucher:

- missing account rejection
- zero/empty account rejection
- balanced voucher validation
- dry-run without database writes

## Nagahat Validation

Tested against:

- Server: `Wael\Sql2019`
- Database: `Nagahat`
- Sample invoice: `Transaction_ID = 74004`

Dry-run results:

- `IncludeInventoryCost = 0`
  - voucher lines: `11`
  - debit: `431.5201`
  - credit: `431.5201`
- `IncludeInventoryCost = 1`
  - voucher lines: `13`
  - inventory cost value: `407.0136`
  - debit: `838.5337`
  - credit: `838.5337`

No write was executed for the preview validation.

## Audit UI

The details page now reads:

- `MainErp_AuditLog.OperationName`
- `EntityName`
- `EntityKey`
- `UserId`
- `CorrelationId`
- `Message`
- `CreatedAt`

Displayed operations include current and future rows such as:

- `PumpSales.Post`
- `PumpSales.DeleteDraft`
- future cancel/rebuild/audit actions

## Pending

- Actual cost posting button is still not exposed.
- Posted invoice cancellation/reversal is still preview only.
- `CreateRecieveVoucher` for sales returns / `Transaction_Type = 20` is not implemented yet.
- PG discount/prepaid/cheque/project/system-option effects still need a separate trace phase.
- Final audit UI filters and user-name join are pending.

## Safety

- No `Areas\Pos` changes.
- No `AllScripts.sql` changes.
- No production configuration changes.
