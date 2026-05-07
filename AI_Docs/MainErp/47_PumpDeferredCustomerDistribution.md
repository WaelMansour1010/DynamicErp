# Pump Deferred Customer Distribution

Date: 2026-05-07

## Requirement

Pump invoices have a sensitive deferred-payment workflow. The deferred value in the pump grid is not only a number; VB6 opens a small distribution screen where the deferred amount and quantity are split across customers.

Active source:

- `F:\Source Code\SatriahMain\Frm\FrmSaleBill6.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\FrmitemShowDet.frm`

## VB6 Behavior

`FrmItemShowDet.save` handles the small distribution screen.

When `mIndex = 1`, it writes the distribution back to the current `FrmSaleBill6` pump grid row:

- `DetailsPump`
- `DeferredQty`
- `Deferred`
- recalculates remaining pump quantity

The saved string format is:

```text
CustomerId#UnitId#Amount#CustomerName#Qty#UnitPrice#RecNo#@
```

Multiple rows are concatenated with `@` and carriage-return/newline.

Example from `Nagahat`:

```text
69#1#6640#شركة الدانوب للموارد الغزائية والكماليات#4000#1.66#14367#@
69#1#7470#شركة الدانوب للموارد الغزائية والكماليات#4500#1.66#14365#@
748##1503#شركة السدحان التجارية#900#1.67#13742#@
```

## Web Implementation

The MainErp pump invoice details page now parses `Transaction_Details.DetailsPump` into a customer distribution table under each pump line.

Displayed per allocation:

- Customer id
- Customer name
- Quantity
- Unit price
- Deferred amount
- Reference number
- Customer account display

Customer account display follows the MainErp standard:

`Account_Serial - Account_Name`

Raw `Account_Code` remains internal.

## Totals

The screen now shows:

- total deferred distribution amount
- total deferred distribution quantity
- distinct deferred customer count
- line-level comparison against `Deferred` and `DeferredQty`

This helps detect whether the stored `DetailsPump` distribution matches the pump line totals.

## Stored Procedure

Added to:

`Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`

Procedure:

`dbo.MainErp_SalesInvoice_SavePumpDeferredDistribution`

Purpose:

- controlled save gate for the pump line deferred customer distribution only

Safety defaults:

- `@DryRun = 1`
- `@EnableDraftWrite = 0`

By default it returns the payload and safety message only. It does not write.

If explicitly enabled later, it can update only:

- `Transaction_Details.DetailsPump`
- `Transaction_Details.Deferred`
- `Transaction_Details.DeferredQty`

It does not create:

- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- issue vouchers
- receive vouchers
- posting

It also refuses to update if the invoice is closed, posted, or approved.

## Nagahat Validation

Confirmed real data in `Nagahat`.

Sample:

- `Transaction_ID = 95484`
- Line `ID = 845857`
- `Deferred = 116.5`
- `DeferredQty = 50`
- Parsed allocation:
  - CustomerId `4`
  - Customer `نجاحات للتجارة والنقل`
  - Amount `116.5`
  - Qty `50`
  - UnitPrice `2.33`
  - RecNo `255`

Dry-run test of `MainErp_SalesInvoice_SavePumpDeferredDistribution` succeeded and did not write.

## Safety

- A dedicated edit screen was added in `48_PumpDeferredDistribution_EditScreen.md`.
- The enabled save is limited to the pump deferred customer distribution only.
- It updates only `DetailsPump`, `Deferred`, and `DeferredQty` through the guarded stored procedure.
- No database write was executed during validation.
- No `AllScripts.sql` change.
- No POS/Kishny change.
