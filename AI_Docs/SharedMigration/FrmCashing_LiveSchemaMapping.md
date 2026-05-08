# FrmCashing Live Schema Mapping

Representative DB inspected: `Wael\Sql2019`, database `Eng`.

POS comparison DB inspected: `Wael\Sql2019`, database `Cash`.

## Header

- `Notes.NoteID`: voucher key
- `Notes.NoteType = 4`: receipt voucher
- `Notes.NoteSerial`, `Notes.NoteSerial1`: visible serial
- `Notes.NoteDate`, `Notes.NoteDateH`: dates
- `Notes.Note_Value`, `Notes.VAT`, `Notes.TotalValue`: amount/tax/total
- `Notes.CusID`, `Notes.ContNo`, `Notes.ContractNo`, `Notes.akarid`, `Notes.unittype`, `Notes.UnitNo`: customer/property allocation references
- `Notes.BoxID`, `Notes.BankID`, `Notes.BankName`: cashbox/bank
- `Notes.PaymentType`, `Notes.CashingType`, `Notes.NoteCashingType`, `Notes.NCashingType`: receipt/payment classification
- `Notes.branch_no`: branch

## Allocation

- `ContracttBillInstallmentsDone.NoteID`
- `TblContractInstallments`: source installment details and paid components
- `TblContract`: contract/customer/property context

## Accounting trace

- `DOUBLE_ENTREY_VOUCHERS.Notes_ID`
- Account joins use `ACCOUNTS.Account_Code`
- UI account display uses `Account_Serial + ' - ' + Account_Name`

No database changes were made.
