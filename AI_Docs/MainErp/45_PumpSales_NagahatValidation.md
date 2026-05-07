# Pump Sales Nagahat Validation

Date: 2026-05-07

## Purpose

The initial pump-sales validation against `Eng` did not contain useful pump invoice samples. The representative database for pump sales testing is now confirmed as:

- Server: `Wael\Sql2019`
- Database: `Nagahat`

## Validation Summary

Read-only SQL checks against `Nagahat` found real pump invoice data:

- `Transactions.Transaction_Type = 21`
- `Transactions.TypeInvoice = 2`
- Pump invoice count found: `279`
- Latest sample: `Transaction_ID = 95484`
- Sample invoice serial: `NoteSerial1 = 1526030001`
- Sample date: `2026-03-11`
- Sample detail lines: `24`

Breakdown from `Transactions`:

| TypeInvoice | Transaction_Type | Rows |
| --- | --- | ---: |
| 0 | 9 | 24 |
| 0 | 21 | 1 |
| 1 | 21 | 29164 |
| 2 | 21 | 279 |

## Sample Pump Invoice

`Transaction_ID = 95484`

Header values observed:

- `Transaction_Type = 21`
- `TypeInvoice = 2`
- `NoteSerial1 = 1526030001`
- `NetValue = 2353.3000`
- `PayedValue = 2353.3000`
- `RemainValue = 0.0000`
- `Fullcode = 1526030001`
- `Transaction_NetValue = 2353.3`

Detail values observed:

- `DetailLines = 24`
- `Qty = 1113`
- Pump payment total from line fields = `2469.8`

Sample line fields:

- `PumpId`
- `tblPumpType.Name`
- `PrevQty`
- `CurrentQty`
- `Cash`
- `Mada`
- `Visa`
- `Deferred`
- `AmountH`
- `AmountHComm`
- `Account_CodeComm`
- `IsOther`
- `ColorID`

## Fixes Applied

- Added `Nagahat` to the debug-only MainErp database selector in `/DevStart`.
- Fixed pump diagnostics SQL alias from `RowCount` to `RowsFound` to avoid SQL keyword parsing issues.
- Added direct "فتح" links for pump diagnostic candidate rows.
- Updated the pump empty state so it no longer says `Eng`; it now points developers to select `Nagahat` for local pump testing.

## How To Test Locally

1. Open `/DevStart`.
2. In the MainErp database selector choose `Nagahat`.
3. Click `تطبيق وفتح MainErp`.
4. Open `/MainErp/PumpSales`.
5. Confirm rows are listed.
6. Open sample `/MainErp/PumpSales/Details/95484`.

## Safety

This validation and implementation remain read-only:

- no `INSERT`
- no `UPDATE`
- no `DELETE`
- no stored procedure execution
- no `AllScripts.sql` changes
- no POS/Kishny changes
