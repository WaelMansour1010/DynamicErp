# LC Accounting Flow

Scope: الاعتمادات المستندية / Letters of Credit. Active source: `F:\Source Code\SatriahMain\Frm\New frm\FrmLC.frm`.

## Active Entry Points

| VB6 member | Lines observed | Purpose |
| --- | ---: | --- |
| `Cmd_Click(0)` | 9420-9489 | New LC initialization; assigns `TblLCID` with `new_id("TblLC", "TblLCiD", "", True)` and defaults parent account from `get_account_code_branch(51, my_branch)`. |
| `Cmd_Click(1)` | 9494-9525 | Edit mode; blocks edit when `TxtNoteID2` has a voucher. |
| `Cmd_Click(2)` | 9527-9533 | Save command; calls `SaveData`. |
| `Cmd_Click(4)` | 9537-9550 | Delete; blocks delete when `TXTNoteID` has a voucher, then calls `Del_Trans`. |
| `SaveData` | 8692-9412 | Main create/edit transaction. |
| `createVoucher` | 4989-5157 | Creates LC `Notes` rows and calls `CREATE_VOUCHER_GE`. |
| `CREATE_VOUCHER_GE` | 5194 onward | Creates normal LC accounting voucher rows in `DOUBLE_ENTREY_VOUCHERS`. |
| `createVoucher2` | 5491-5708 | Creates note rows for margin/history/opening-balance grid entries. |
| `CREATE_VOUCHER_GE2` | 5767 onward | Creates voucher rows for grid-driven LC margin/history/opening-balance actions. |

## SaveData Sequence

1. Validation runs before transaction:
   - Requires branch-linked account from `get_account_code_branch(62, my_branch)`.
   - Requires bank (`Dcbank`), branch (`dcBranch`), LC expense parent (`cmbAccountExpensParent`), LC number, LC type, currency.
   - Requires margin parent (`cmbAccountMarginParent`).
   - Requires acceptance parent (`cmbAccountAcceptanceParent`) when enabled.

2. `Cn.BeginTrans` starts at line 8847.

3. Branch account links are checked:
   - `get_account_code_branch(225, my_branch)` for LC-related account.
   - `get_account_code_branch(226, my_branch)` for another LC/bank guarantee related account.

4. Bank parent/account defaults are read from `BanksData` by `BankId`.

5. New LC (`TxtModFlg="N"`):
   - Opens `TBLLC`.
   - Adds a row with `TblLCID = TXTTblLCID`.
   - Creates child accounts using `ModAccounts.AddNewAccount`.
   - Account creation can populate `ACCOUNTS.TblLCID`.
   - Created account roles:
     - `AcceptAccount_Code` from `cmbAccountAcceptanceParent`.
     - `Account_CodeMargin` from `cmbAccountMarginParent`.
     - `LCAccount_Code` from `cmbAccountLGParent`.
     - `AccountExpensCode` from `cmbAccountExpensParent`.
   - Final `Account_Code` is set to `LCAccount_Code`.

6. Edit LC (`TxtModFlg="E"`):
   - Existing LC-linked accounts are edited with `ModAccounts.EditAccount`.
   - Missing accounts are created where needed.
   - Existing notes and accounting artifacts are rebuilt, not patched in place:
     - Deletes `Notes` where `TblLCID = current`.
     - Deletes `DOUBLE_ENTREY_VOUCHERS1` where `Notes_ID` is in `Notes1` for this `TblLCID`.
     - Deletes `Notes1` where `TblLCID = current`.
     - Deletes `TBLLCHistory`, `TBLLCMargin`, `TBLLCMargin2`, and `tblLCOpenB` for this `TblLCID`.

7. The `TblLC` row is populated with LC header, account, note, branch, project, opening balance, and RowId fields.

8. Opening balance:
   - If no open balance is entered, `txtopening_balance_voucher_id` is reset to `0`.
   - If debit or credit opening balance is selected and voucher id is missing, `get_opening_balance_voucher_id` is called.
   - `OpenBalanceType=0` for debit option, `1` for credit option, and `Null` when no opening balance.
   - `opening_balance_voucher_id` is stored on `TblLC`.

9. Related grids are saved while the transaction is still open:
   - `TBLLCHistory` from `GrdBondHistory`.
   - `TBLLCMargin2` from `GrdMargin4`.
   - `TBLLCMargin` from `GrdMargin2`.
   - `tblLCOpenB` from `GrdMargin3`.

10. First `CommitTrans` occurs at line 9280.

11. Grids are reloaded and `StillAmount` / `IsFullPayed` values are recalculated visually.

12. A second transaction starts at line 9364:
   - `CmdCreateV_Click` is called.
   - `Command3_Click` is called when `optTypeLCLG(0)` is selected.
   - These commands create notes/vouchers after header/grid persistence.
   - Second `CommitTrans` occurs at line 9370.

13. Error handler rolls back the active transaction if `BeginTrans=True`.

## Account Creation Logic

`ModAccounts.AddNewAccount` in `F:\Source Code\SatriahMain\Bas\ModAccounts.bas`:

- Starts at line 821.
- Rejects creation under a parent marked as last account using `CHECK_LAST_ACCOUNT`.
- Generates account code through `GetNewAcountCode`.
- Inserts into `ACCOUNTS`, including `Account_Code`, names, parent, serial levels, branch/user fields, and optional `TblLCID`.

`GetNewAcountCode` builds a child code by taking the parent account code, appending/incrementing an `a` suffix, and looping until the code is unused.

## Voucher Posting Helpers

`ModAccounts.AddNewDev` starts at line 1473:

- `opening_balance=False` inserts into `DOUBLE_ENTREY_VOUCHERS`.
- `opening_balance=True` inserts into `DOUBLE_ENTREY_VOUCHERS1`.
- Rounded zero values are skipped.
- `Credit_Or_Debit=0` means debit.
- `Credit_Or_Debit=1` means credit.
- When opening balance is true, `opening_balance_voucher_id` is written to the voucher row.

`get_opening_balance_voucher_id` in `Class\registry.bas` currently returns `MyTime`; older max/id logic is commented out. This needs careful replacement or preservation in .NET because it is not a normal identity sequence.

## createVoucher Flow

`createVoucher(Optional IsClose As Boolean=False, Optional notytype As Integer=0)`:

1. Uses `tablename = "TBLLC"` and `Filedname = "TblLCID"` for note metadata.
2. Uses `TXTTblLCID` as the note serial reference.
3. Determines note type:
   - `22001` for normal/open LC voucher.
   - `22005` for close voucher.
   - `22010` for opening value / LC opening expenses.
4. Selects note id/serial fields:
   - Normal: `NoteID`, `NoteSerial`, `NoteIDRowId`.
   - Close: `NoteID2`, `NoteSerial2`, `NoteID2RowId`.
   - Open value: `NoteIDOpen`, `NoteSerialOpen`, `NoteIDOpenRowId`.
5. Checks for duplicate `Notes.NoteSerial` excluding current `TblLCID`.
6. Calls `CreateNotes`.
7. Calls `CREATE_VOUCHER_GE` with the created note id.

## CREATE_VOUCHER_GE Posting Logic

`CREATE_VOUCHER_GE`:

1. Allocates `LngDevID = new_id("DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID", "")`.
2. Reads LC accounts from `tblLc`: `Account_Code`, `Account_code2`, `Account_CodeMargin`, `AcceptAccount_Code`, `LCAccount_Code`, `AccountExpensCode`.
3. For `notytype=22010`:
   - Uses `txtOPenValue * currency rate`.
   - Debits `AccountExpensCode`.
   - Calculates VAT through `PercentgValueAddedAccount_Transec`.
   - Debits VAT account from `GetValueAddedAccount`.
   - Credits bank account from `get_bank_Account(Dcbank.BoundText, "Account_Code")`.
   - Calls `updateNotesValueAndNobytext`.
4. For close voucher:
   - Debits bank.
   - Credits margin account.
5. For normal open/margin voucher:
   - Debits margin account.
   - Credits bank.
6. Additional LC expense sections debit expense/prepaid accounts and VAT where applicable, and credit bank.

## createVoucher2 / CREATE_VOUCHER_GE2

This path handles grid-driven LC transactions such as margins, margin payments, LC history, and opening-balance grid rows.

1. `createVoucher2` chooses metadata table by grid type:
   - `TBLLCMargin`
   - `TBLLCHistory`
   - `tblLCOpenB`
   - `TBLLCMargin2`
2. Creates `Notes`/`Notes1` metadata through `CreateNotes`.
3. Calls `CREATE_VOUCHER_GE2`.
4. `CREATE_VOUCHER_GE2` chooses table by `mIsOpenBalance`:
   - `DOUBLE_ENTREY_VOUCHERS1` when true.
   - `DOUBLE_ENTREY_VOUCHERS` when false.
5. Standard margin flow:
   - When `mIsPay=0`: debit margin/financing account, credit bank.
   - When `mIsPay<>0`: debit bank, credit margin or second bank/margin account depending on grid type.
6. For type `4`, insurance/expense rows include VAT extraction and credit bank.
7. For type `5`, margin value is debited to margin and credited to bank.
8. Negative `Notevalue` flips debit/credit account selection before posting absolute value.

## Edit/Delete/Rollback Behavior

- Edit rebuilds notes and voucher rows by deleting LC-linked note/voucher/grid rows and recreating them.
- Delete is blocked if `TXTNoteID` has a value, then `Del_Trans` is invoked.
- Save has two transaction scopes: one for header/grid persistence, another for voucher generation.
- Any error inside an active transaction calls `Cn.RollbackTrans`.

## Migration Risks

- `TblLCID`, `NoteID`, and voucher ids are manually generated, not identity driven.
- `get_opening_balance_voucher_id` currently returns `MyTime`; this is collision-prone unless understood.
- VB6 uses both `Notes` and `Notes1` for LC/opening-balance behavior.
- Edit deletes and recreates accounting rows; .NET must keep this atomic and auditable.
- Account generation has side effects in `ACCOUNTS`; do not implement LC save until account-code rules are fully tested against production data.
