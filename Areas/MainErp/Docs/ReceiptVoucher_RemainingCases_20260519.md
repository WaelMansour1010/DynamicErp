# Receipt Voucher Remaining Cases - 2026-05-19

Scope: continue receipt voucher hardening after the authenticated POS/MainERP cash create/edit/delete validation.

Source of truth: `F:\Source Code\SatriahMain\Frm\FrmCashing1.frm`.

## VB6 Sections Traced

| VB6 section | Business meaning |
| --- | --- |
| `SaveData` | Saves `Notes` with `NoteType = 4`, `NoteSerial = Notes_coding(...)`, and `NoteSerial1 = Voucher_coding(..., 2, 4)`. It writes payment method fields including `NoteCashingType`, `BoxID`, `BankID`, `ChqueNum`, `DueDate`, `ChequeBoxID`, `DebitSide`, and `CreditSide`. |
| `CboPayMentType_Change` | Payment method 0 enables box, method 1 enables cheque/bank fields, method 2 enables bank transfer fields and labels the number as transfer number, method 3 enables cheque-like bank fields, method 4 enables direct account. |
| `DcboBankName_Click` | Bank methods normally debit `BanksData.Account_Code`; with legacy options it may use `Account_Code1` for bank cheque behavior. Methods 2 and 3 force `Account_Code`. |
| `DcChequeBox_Change` | Cheque-box mode debits `TblBoxesData.Account_Code1`. |
| `saveChequeBoxContents` | Rebuilds `TblChecqueBoxContent` for receipt cheque-box rows with `Deposited = 0`, `Collected = 0`, `CreditAccount`, and optional customer/project account snapshots. |
| `SaveJL` | Receipt journal starts with debit source account (`DcboDebitSide`) and credit party/revenue/account (`DcboCreditSide`), then adds VAT/property/commission/contract lines for complex categories. |
| `Del_Trans` | Deletes owned receipt linked rows including `ReciveDetails`, `ContracttBillInstallmentsDone`, `TblChecqueBoxContent`, `TblNotesSales`, property/project tables, and the receipt `Notes` row. |

## Safe Enable / Block Matrix

| Area | Current decision | Reason |
| --- | --- | --- |
| Cash receipt, cashing types `0,1,2,7` | Enabled | Accounting intent is the simple receipt pattern: debit box, credit selected party/account, optional VAT credit, balance asserted. |
| Bank transfer receipt, cashing types `0,1,2,7` | Enabled | VB6 method 2 uses bank account as debit side and stores transfer number/date. SQL now preserves `ChqueNum`, `DueDate`, `TxtChequeNumber1`, and `DtpChequeDueDate1`. |
| Bank transfer without transfer number | Blocked | VB6 requires `TxtChequeNumber` for method 2. SQL raises: `يجب إدخال رقم الحوالة لطريقة التحويل البنكي.` |
| Cheque method 1 | Blocked | Needs `SystemOptions.ChequeBox` branching, `ChequeBoxID`, cheque bank name, `TblChecqueBoxContent` rebuild, and deposited/collected lifecycle UI. Current web model does not carry all required fields. |
| Bank cheque / method 3 | Blocked | VB6 handles it separately from transfer and cheque-box. Needs sample journals and lifecycle validation before enabling. |
| Direct account method 4 | Blocked | Requires a separate selected debit account (`DcbAccount`) rather than box/bank. Current web save shape requires box or bank. |
| Receipt cashing types `3,4,5,6,8,9,10,11,12,13` | Blocked | These paths have revenue/project/employee/contract/property/settlement-specific links and journal lines. Saving them through the simple journal would be fake accounting. |

## SQL Change Applied

File changed:

- `Areas/MainErp/Sql/17_PaymentVoucher_Accounting_Rebuild.sql`

Procedure changed:

- `dbo.usp_DynamicErpVoucher_Save`

Behavior changed:

- For supported bank transfer saves (`@paymentMethod = 2`), require a transfer number.
- Preserve transfer number/date in `Notes.ChqueNum`, `Notes.DueDate`, `Notes.TxtChequeNumber1`, and `Notes.DtpChequeDueDate1`.
- Keep cheque method `1` blocked until the cheque-box lifecycle is implemented end to end.

Intentional modernization:

- VB6 treats date picker values as always present. SQL accepts a missing transfer date by storing the voucher date, because the business-significant required value is the transfer number and bank; this avoids null report fields without inventing another date source.

## DB Validation Evidence

Script installed successfully on allowed DBs:

| DB | Install result |
| --- | --- |
| Eng | Success |
| Cash | Success |
| Dania | Success |

Rollback transfer tests:

| DB | Temp NoteID | NoteSerial1 | Journal | Transfer fields | Rollback |
| --- | ---: | ---: | --- | --- | --- |
| Eng | 222143 | 1345 | 2 lines, debit 12.34, credit 12.34 | `TRN-RTL-001`, `2026-05-20` | `NotesLeft = 0` |
| Cash | 1457142 | 12605009 | 2 lines, debit 12.34, credit 12.34 | `TRN-RTL-001`, `2026-05-20` | `NotesLeft = 0` |
| Dania | 197835 | 1026050009 | 2 lines, debit 12.34, credit 12.34 | `TRN-RTL-001`, `2026-05-20` | `NotesLeft = 0` |

Blocked validation:

| DB | Case | Result |
| --- | --- | --- |
| Eng | Bank transfer without number | Blocked with Arabic validation. |
| Cash | Bank transfer without number | Blocked with Arabic validation. |
| Dania | Bank transfer without number | Blocked with Arabic validation. |
| Eng/Cash/Dania | Cheque method `1` | Still blocked with Arabic unsupported-method validation. |

## Remaining Work

1. Add receipt cheque-box model fields and lookup only after mapping `SystemOptions.ChequeBox`, `BanksData.Account_Code1`, and `TblBoxesData.Account_Code1` per DB.
2. Implement `TblChecqueBoxContent` rebuild inside the same transaction as `Notes` and `DOUBLE_ENTREY_VOUCHERS`.
3. Keep delete blocked for deposited/collected receipt cheques.
4. Trace and implement method `3` and method `4` separately; do not reuse method `2`.
5. Trace complex cashing types before enabling their linked tables and multi-line accounting.
