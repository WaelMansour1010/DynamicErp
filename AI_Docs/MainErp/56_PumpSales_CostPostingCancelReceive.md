# Pump Sales Cost Posting, Cancellation, and Receive Reversal

Date: 2026-05-07

## Implemented

### Actual Inventory Cost Posting

`dbo.MainErp_PumpSales_Post` now supports actual posting with:

- `@IncludeInventoryCost = 1`
- Debit: `branches.a1` cost of sales
- Credit: `branches.a0` inventory
- Amount: `SUM(Transaction_Details.CostPrice * ISNULL(Quantity, ShowQty))`

UI command:

- `post-cost`
- Button label: `ترحيل مع قيد التكلفة`

Safety:

- The normal `post` command still uses `@IncludeInventoryCost = 0`.
- Cost posting requires admin + `CanPostPumpInv` through the current MainErp permission gate.

### Posted Invoice Cancellation

Created:

- `dbo.MainErp_PumpSales_CancelPosted`

Behavior:

- Dry-run mode previews reversal.
- Actual mode creates a reversal Note.
- Actual mode creates reversed `DOUBLE_ENTREY_VOUCHERS` rows.
- Actual mode creates a linked receive/reversal inventory document:
  - `Transactions.Transaction_Type = 20`
  - `Transactions.nots = source Transaction_ID`
- Source invoice is marked `Closed = 1`.
- Audit row is written to `MainErp_AuditLog`.

UI command:

- `CancelPosted`
- Button label: `إلغاء فعلي بعكس القيد`

Safety:

- Rejects unposted invoices.
- Rejects repeated cancellation if a linked `Transaction_Type = 20` already exists.
- Rejects unbalanced source voucher.
- Does not delete original invoice, original note, original voucher, or original issue document.

### Audit UI

The invoice details page now displays:

- operation name
- user id/name
- date
- correlation id
- message
- before snapshot
- after snapshot

`MainErp_AuditLog` was extended with:

- `BeforeSnapshot`
- `AfterSnapshot`

## Nagahat Test

Database:

- `Wael\Sql2019 / Nagahat`

Sample:

- `Transaction_ID = 74004`

Actual actions performed:

1. Rebuilt posting with cost:
   - `dbo.MainErp_PumpSales_Post`
   - `@ForceRebuild = 1`
   - `@DryRun = 0`
   - `@IncludeInventoryCost = 1`
   - Result: voucher rebuilt with cost entries.

2. Cancelled posted invoice:
   - `dbo.MainErp_PumpSales_CancelPosted`
   - `@DryRun = 0`
   - Result:
     - cancel note: `79418`
     - cancel voucher group: `156010`
     - receive/reversal transaction: `109513`
     - source invoice closed: `Closed = 1`

Verification:

- Source invoice `74004`
  - `IsPosted = 1`
  - `Closed = 1`
  - voucher line count including source and reversal: `26`
  - linked receive docs: `1`
  - audit rows: `3`

## PG Follow-Up Still Needed

The core PG path now supports sales/payment/VAT plus optional cost/inventory. Remaining PG-dependent branches still need dedicated samples before activation:

- complex line/header discounts beyond current saved fields
- prepaid/advance payment effects
- cheque/receivable paper flows from `FgCheques`
- project/vendor/customer-contract effects
- `SystemOptions` branches such as customer contract pricing and multi-payment constraints

## Safety

- No `Areas\Pos` files were modified in this phase.
- No `AllScripts.sql` change.
- SQL remains isolated under `Areas\MainErp\Sql`.
