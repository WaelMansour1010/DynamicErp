# Sales Invoice Live Schema Mapping

Test database:

- Server: `Wael\Sql2019`
- Database: `Eng`
- MainErp connection: `MainErp_ConnectionString`

## Transactions

Role:

- Invoice header table.

Important fields discovered:

- `Transaction_ID`
- `Transaction_Serial`
- `Transaction_Date`
- `Transaction_Type`
- `CusID`
- `StoreID`
- `Total`
- `BranchId`
- `PayedValue`
- `NetValue`
- `RemainValue`
- `PaymentNetid`
- `ManualNO`
- `CBoBasedON`
- `Posted`
- `POSBillType`
- `Prefix`
- `Fullcode`
- `Approved`
- `DateRec`
- `SumValueLine`
- `SumVATLine`
- `Transaction_NetValue`
- `TypeInvoice`
- `VAT`

Initial migration filter:

- `Transaction_Type IN (21, 42, 38, 9)`
- Workshop: `ISNULL(TypeInvoice, 0) <> 2`
- Pump: `ISNULL(TypeInvoice, 0) = 2`

Eng validation result:

- `TypeInvoice=0`, `Transaction_Type=38`, count `1`, max `Transaction_ID=3832`
- No pump rows were found with the initial `TypeInvoice = 2` filter during this validation pass.

## Transaction_Details

Role:

- Invoice line/detail table.

Important generic fields:

- `Transaction_ID`
- `Item_ID`
- `Quantity`
- `Price`
- `CostPrice`
- `ID`
- `UnitId`
- `ShowQty`
- `showPrice`
- `AccountCode`
- `discountvalue`
- `TotalDiscountPerLine`
- `Vat`
- `Vatyo`

Important pump-related fields:

- `LineID`
- `Account_Code`
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
- `AmountH`
- `AmountHComm`
- `IsOther`
- `Account_CodeComm`
- `DetailsPump`

## TblItems

Role:

- Item/product lookup.

Important fields used by the first wave:

- `Item_ID`
- `Item_Name`
- `Item_NameE` if present
- `Item_Code` or equivalent code field when present

## TblUnites

Role:

- Unit lookup for invoice detail lines.

Important fields used:

- `UnitId`
- Arabic/English unit names when available

## tblPumpType

Role:

- Pump lookup for pump sales details.

Used only where `Transaction_Details.PumpId` is populated.

## TblSalesPayment

Role:

- Payment rows linked to sales invoice transactions.

Used read-only to show payment method/value/status where present.

## DOUBLE_ENTREY_VOUCHERS

Role:

- Accounting lines linked to the sales invoice.

Read-only linkage attempts:

- `Transaction_ID = Transactions.Transaction_ID`
- `Transaction_ID1 = Transactions.Transaction_ID`
- `Notes_ID = Transactions.NoteID` when a header note id exists

Display rule:

- debit/credit values are calculated from `Credit_Or_Debit`
- account display uses `Account_Serial - Account_Name`
- raw `Account_Code` stays internal

## Notes

Role:

- Journal/note header table where linked note ids exist.

Used for opening the journal details route when a reliable `NoteID` is available.

## Tables Not Assumed

No new object was created.

The migration does not assume unknown tables or stored procedures. Any missing table/column is handled as a schema warning instead of a crash where the repository can detect it safely.
