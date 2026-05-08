# FrmPayments Live Schema Mapping

Representative DB inspected: `Wael\Sql2019`, database `Eng`.

POS comparison DB inspected: `Wael\Sql2019`, database `Cash`.

## Header

- `Notes.NoteID`: voucher key
- `Notes.NoteType = 5`: payment voucher
- `Notes.NoteSerial`, `Notes.NoteSerial1`: visible serial
- `Notes.NoteDate`, `Notes.NoteDateH`: dates
- `Notes.Note_Value`, `Notes.VAT`, `Notes.TotalValue`: amount/tax/total
- `Notes.CusID`, `Notes.person`, `Notes.too`, `Notes.renterName`: related party fallbacks
- `Notes.BoxID`, `Notes.BankID`, `Notes.BankName`: cashbox/bank
- `Notes.PaymentType`: payment method
- `Notes.branch_no`: branch
- `Notes.AccountPaym`, `Notes.Account_DebitSide`, `Notes.Account_CreditSide`, `Notes.Account_Code1`, `Notes.Account_Code2`: account references

## Display joins

- `TblCustemers.CusID`: party name
- `TblBranchesData.branch_id`: branch name
- `TblBoxesData.BoxID`: cashbox
- `BanksData.BankID`: bank/report
- `ACCOUNTS.Account_Code`: internal join only
- UI account display: `Account_Serial + ' - ' + Account_Name`

## Allocations

- `TblNotesBillBuyPayment.NoteID1`
- `TblNotesBillProjectPayment.NoteID1`
- `TblNotesBillVindorPayment.NoteID1`

## Accounting trace

- `DOUBLE_ENTREY_VOUCHERS.Notes_ID`
- Debit when `Credit_Or_Debit = 0`
- Credit when `Credit_Or_Debit = 1`

No database changes were made.
