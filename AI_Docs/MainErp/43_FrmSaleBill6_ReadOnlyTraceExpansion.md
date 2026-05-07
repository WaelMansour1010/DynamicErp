# FrmSaleBill6 Read-Only Trace Expansion

Date: 2026-05-07

## Scope

This phase continues the Main ERP `FrmSaleBill6` migration without enabling any write behavior.

Implemented only:

- Read-only operational header trace from `Transactions`.
- Read-only linked accounting trace from `DOUBLE_ENTREY_VOUCHERS`.
- Read-only linked inventory transaction trace for generated issue/receive vouchers.
- Save preview text updated to mention linked inventory documents.

Not implemented:

- Save.
- Delete.
- Posting.
- Inventory issue/receive creation.
- Notes creation.
- Report execution.
- Any database migration.

## VB6 Source Points Confirmed

Active source:

`F:\Source Code\SatriahMain\Frm\FrmSaleBill6.frm`

Important traced routines/flags:

- `C1Tab1` hosts the two operational modes.
- `mTypeInvoice = 1` maps to workshop sales.
- `mTypeInvoice = 2` maps to pump sales.
- Search/list logic uses `Transactions.Transaction_Type IN (21,42,38,9)` with `TypeInvoice`.
- Pump reporting uses `DailyPumpR.Rpt`.
- Pump details join `tblPumpType`.
- `CreateIssueVoucher2` creates linked `Transactions` rows with `Transaction_Type = 19`.
- `CreateRecieveVoucher` creates linked `Transactions` rows with `Transaction_Type = 20`.
- The generated inventory transactions are linked back through `nots = source Transaction_ID` and `nots2 = source NoteSerial1`.

## Added Header Trace Fields

The detail workspace now reads additional `Transactions` columns where available:

- `Posted`
- `Approved`
- `Prefix`
- `Fullcode`
- `CBoBasedON`
- `POSBillType`
- `Transaction_NetValue`
- `SumValueLine`
- `SumVATLine`
- `DateRec`

These are displayed as operational trace fields only. They are not editable and do not drive posting.

## Added Inventory Trace

The detail page now loads related inventory documents:

```sql
FROM Transactions
WHERE Transaction_Type IN (19, 20)
  AND (
      CONVERT(nvarchar(100), nots) = @TransactionId
      OR CONVERT(nvarchar(100), nots2) = @NoteSerial1
  )
```

Displayed columns:

- Related transaction id.
- Transaction type.
- Date.
- Transaction serial.
- Note serial.
- Store id/name fallback.
- Total.
- Net value.
- Link reason.
- Journal link when `NoteId` exists.

## Safety

This implementation is read-only. It does not call:

- `CreateIssueVoucher2`
- `CreateRecieveVoucher`
- `SaveData`
- `DELETE`
- `INSERT`
- `UPDATE`

No `Areas\Pos` files were modified for this phase.

No `AllScripts.sql` changes were made.

## Remaining Gaps

- Exact inventory quantity direction still needs final VB6 validation before write migration.
- Some generated inventory documents may not be found if older data does not preserve `nots`/`nots2`.
- Pump sample data in `Eng` may be absent or sparse; diagnostics should continue to show filter details rather than fake data.
- Report execution for `DailyPumpR.Rpt` is still pending.
