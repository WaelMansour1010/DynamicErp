# LC Save/Edit and Controlled Voucher Implementation

Date: 2026-05-07

## Scope

Implemented the first write-enabled MainErp migration phase for `FrmLC.frm`, then extended it with the confirmed non-grid voucher paths.

Enabled:

- LC create.
- LC edit.
- Optional creation of missing linked accounts under selected parent accounts.
- Controlled creation of the main LC opening voucher `NoteType = 22001`.
- Controlled creation of the LC open-expense voucher `NoteType = 22010`.
- Controlled creation of the LC close voucher `NoteType = 22005`.
- Controlled creation of LC opening-balance voucher into `Notes1` + `DOUBLE_ENTREY_VOUCHERS1`.
- Protected rebuild of core LC vouchers.
- Protected delete of an LC and its directly linked accounting/grid rows.

Still disabled:

- Grid voucher generation from `TBLLCHistory`, `TBLLCMargin`, `TBLLCMargin2`, and `tblLCOpenB`.
- Detailed row-by-row voucher generation from `tblLCOpenB`; the header opening-balance voucher is implemented, but the detailed VB6 grid voucher paths still need a separate matching pass.

## Routes

- `GET /MainErp/LC/New`
- `GET /MainErp/LC/Edit/{id}`
- `POST /MainErp/LC/Save`
- `POST /MainErp/LC/CreateVoucher/{id}`
- `POST /MainErp/LC/CreateOpenExpenseVoucher/{id}`
- `POST /MainErp/LC/CloseLc/{id}`
- `POST /MainErp/LC/CreateOpeningBalanceVoucher/{id}`
- `POST /MainErp/LC/RebuildVouchers/{id}`
- `POST /MainErp/LC/Delete/{id}`

## Header Save

Writes to `TblLC` only.

Implemented fields include:

- `TblLCID`
- `LCNO`
- `LCTyperId`
- `BankId`
- `BankID2`
- `BoxID`
- `Value`
- `OpenValue`
- `CurrencyId`
- `Currency_rate`
- `PercentV`
- `VendorId`
- `CountryId`
- `FromDate`
- `Todate`
- `CloseDate`
- `LastParcilDate`
- `OpenBalanceDate`
- `OpenBalance`
- `OpenBalanceType`
- `opening_balance_voucher_id`
- `BranchID`
- `userid`
- `Remarks`
- `project_id`
- `projectName`
- `PaymentTypeID`
- `ChequeNumber`
- `ChequeDueDate`
- `Locked`
- LC linked account fields.

## Account Creation

When `AutoCreateMissingAccounts` is enabled and an account field is empty, the system creates a child account under the selected parent account.

Safety:

- Uses transaction-level `sp_getapplock`.
- Blocks creation under a parent marked `last_account = 1`.
- Writes `ACCOUNTS.TblLCID`.
- Generates account code with the same broad legacy pattern: parent code + `a` + next number.
- Generates `Account_Serial` from the max sibling serial where possible.

## Voucher Creation

Implemented confirmed LC voucher paths.

### Normal LC Opening

- `Notes.NoteType = 22001`
- debit: LC margin account
- credit: bank account from `BanksData.Account_Code`
- amount: `TblLC.Value * TblLC.PercentV / 100 * TblLC.Currency_rate`

The implementation creates:

- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- updates `TblLC.NoteID`, `TblLC.NoteSerial`, and `TblLC.NoteIDRowId`

It does not create duplicate vouchers if voucher rows already exist for the LC note.

### LC Open Expenses

- `Notes.NoteType = 22010`
- debit: LC expense account
- debit: VAT input account when VAT is calculated
- credit: bank account from `BanksData.Account_Code`
- amount: `TblLC.OpenValue * TblLC.Currency_rate`
- VAT split currently follows the observed Eng/VB6 behavior: total is VAT-inclusive, using VAT percent from `TblVATSettingsDet` when available, otherwise 15%.

The implementation updates:

- `TblLC.NoteIDOpen`
- `TblLC.NoteSerialOpen`
- `TblLC.NoteIDOpenRowId`

### LC Close

- `Notes.NoteType = 22005`
- debit: bank account from `BanksData.Account_Code`
- credit: LC margin account
- amount: `TblLC.Value * TblLC.PercentV / 100 * TblLC.Currency_rate`

The implementation updates:

- `TblLC.NoteID2`
- `TblLC.NoteSerial2`
- `TblLC.NoteID2RowId`
- `TblLC.Locked = 1`
- `TblLC.CloseDate` when missing

### Opening Balance

- `Notes1.NoteType = 101`
- `DOUBLE_ENTREY_VOUCHERS1`
- source fields: `TblLC.OpenBalance`, `TblLC.OpenBalanceType`, `TblLC.OpenBalanceDate`
- `OpenBalanceType = 0`: debit LC account, credit bank account
- `OpenBalanceType = 1`: debit bank account, credit LC account
- updates `TblLC.opening_balance_voucher_id` and `TblLC.OpenBalanceDate`

Manual ids are protected through the MainErp manual id generator:

- `Notes1.NoteID`
- `DOUBLE_ENTREY_VOUCHERS1.Double_Entry_Vouchers_ID`

### Protected Rebuild

The rebuild action requires exact confirmation:

`REBUILD-LC-{TblLCID}`

It deletes existing core LC notes/vouchers from `Notes`, `Notes1`, `DOUBLE_ENTREY_VOUCHERS`, and `DOUBLE_ENTREY_VOUCHERS1`, clears the LC note references, then recreates:

- normal LC opening voucher
- LC open-expense voucher when `OpenValue > 0`
- header opening-balance voucher when `OpenBalance > 0`

Grid voucher recreation is still pending because the VB6 grid paths need a separate row-level audit.

### Protected Delete

The delete action requires exact confirmation:

`DELETE-LC-{TblLCID}`

It removes:

- `DOUBLE_ENTREY_VOUCHERS`
- `DOUBLE_ENTREY_VOUCHERS1`
- `Notes`
- `Notes1`
- `TBLLCHistory`
- `TBLLCMargin`
- `TBLLCMargin2`
- `tblLCOpenB`
- LC-generated `ACCOUNTS` rows by `TblLCID`
- `TblLC`

## Eng Smoke Test

Test database: `Wael\Sql2019 / Eng`

Created test LC:

- `TblLCID = 198`
- `LCNO = WEBTEST-LC-20260507184202`
- `NoteID = 222099`, voucher `393525`, debit/credit `500.0000`
- `NoteIDOpen = 222100`, voucher `393526`, debit/credit `1150.0000`
- `NoteID2 = 222101`, voucher `393527`, debit/credit `500.0000`

Validation results:

- LC header saved to `TblLC`.
- Missing linked accounts were created under selected parent accounts.
- All three notes were created in `Notes`.
- All generated voucher groups in `DOUBLE_ENTREY_VOUCHERS` are balanced.
- Open-expense voucher split `1150` into expense `1000`, VAT input `150`, and bank credit `1150`.
- Opening-balance test posted `222.22` for LC `198`.
- Generated `Notes1.NoteID = 716`.
- Generated `DOUBLE_ENTREY_VOUCHERS1.Double_Entry_Vouchers_ID = 6577`.
- Opening-balance voucher result: 2 lines, debit `222.2200`, credit `222.2200`.
- Protected rebuild on LC `198` recreated core normal vouchers and opening balance; totals remained balanced.
- Protected delete tested on temporary LC `999198`; final `TblLC` row count for that id was `0`.

## Safety Notes

No changes were made to:

- `AllScripts.sql`
- `Areas\Pos`
- Kishny/POS login, routes, SQL, reports, or workflows.

Destructive operations are now available only behind explicit confirmation text and should remain restricted to authorized users/test data until an audit table and approval policy are finalized.
