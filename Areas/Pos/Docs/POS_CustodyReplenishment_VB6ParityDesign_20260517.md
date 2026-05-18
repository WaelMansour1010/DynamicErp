# POS Custody Replenishment - VB6 Parity Design

Date: 2026-05-17

Scope: POS Kishny Web custody replenishment / treasury funding screen, compared with old VB6 `FrmPayments1.frm`.

Runtime code was not changed for this task. This document is the focused follow-up to `POS_CustodyReplenishment_Discovery_20260517.md`.

## Source of Truth

The VB6 source of truth for this module is:

- `F:\Source Code\_backup_frm\Frm\FrmPayments1.frm`
- Screen Arabic name: `استعاضة عهده وتمويل خزينة`
- Screen English name: `Bt Cash`
- Header table: `dbo.Notes`
- Accounting table: `dbo.DOUBLE_ENTREY_VOUCHERS`
- Note type: `Notes.NoteType = 50`
- Operation mapping:
  - `DCboCashType.ListIndex = 0` => `CashingType = 5` => `استعاضه عهده` / `Bety Cash`
  - `DCboCashType.ListIndex = 1` => `CashingType = 6` => `تمويل خزينة` / `Box Recharge`

## Side-by-Side Comparison

| Area | VB6 Kishny `FrmPayments1` | Current POS Web | Parity Decision |
|---|---|---|---|
| Screen purpose | One screen for custody replenishment and treasury funding. | `/Pos/Payments/Index` for custody/funding/refund. | Keep as one screen, but align save behavior to VB6. |
| Header record | Inserts/updates one row in `Notes` with `NoteType = 50`. | Inserts/updates one row in `Notes` with `NoteType = 50`. | Match. |
| Operation type | Saves `CashingType = DCboCashType.ListIndex + 5`, only `5` or `6`. | Saves request `CashingType`, validates `5` or `6`. | Match. |
| Main account/custody holder | `DBCboClientName` is loaded from `TblBoxesData` by `Dcombos.GetBoxAccounts`; type `1` for custody replenishment, type `0` for treasury funding. Saves account to `Notes.BTCashAccountcode`. | Main account lookup uses `TblBoxesData.Account_Code` by box type. Saves to `Notes.BTCashAccountcode`. Also has employee custody lookup. | Main account must remain `TblBoxesData.Account_Code`. Employee is not the VB6 custody holder. |
| Employee fields | VB6 custody replenishment does not require `TblEmployee`. It does not save `Emp_ID`, `AccountEmpCode`, `boxValue`, or `EmpValue` for this flow. | Web can save `Emp_ID`, `AccountEmpCode`, `boxValue`, and `EmpValue`, and can post an employee custody credit line. | Disable or ignore employee split for VB6 parity. Keep employee fields only for a separately approved enhanced flow. |
| Branch | Requires branch, sets `my_branch`, saves `Notes.branch_no`, DEV `branch_id`. | Requires `BranchId`, saves `Notes.branch_no`, DEV `branch_id`. | Match. |
| Date | Uses `XPDtbTrans`; saves `Notes.NoteDate`, DEV `RecordDate`, cheque content dates. | Uses `PaymentDate`; saves `Notes.NoteDate`, DEV `RecordDate`. | Match; also require reference date for non-cash like VB6. |
| Amount | Requires numeric `XPTxtVal`; saves `Notes.Note_Value`, both DEV line values. | Requires positive `Value`; saves `Notes.Note_Value`, DEV values. | Match. |
| Notes/person | Saves `Remark`, `general_des_notes`, and `person` using manual person text or selected account text. | Saves `Remarks`, `GeneralDescription`, and `NameText` to `person`. | Match; make sure `NameText` defaults to selected account display when blank. |
| Serial: `NoteSerial` | New only: `Notes_coding(branch,date)`; edit preserves existing serial. | New: `GenerateNotesSerial(branch,date)`; edit preserves existing serial. | Verify `GenerateNotesSerial` is equivalent to `Notes_coding`. |
| Serial: `NoteSerial1` | New only: `Voucher_coding(branch,date,5,50)`; edit preserves existing serial. Also writes `OldNoteSerial1`. | New: global `MAX(NoteSerial1)+1` for all `NoteType=50`; edit preserves existing serial. Writes `OldNoteSerial1` on insert. | Web is not VB6 parity. Replace with `Voucher_coding(branch,date,5,50)` equivalent. |
| Numbering metadata | Saves `numbering_type = sand_numbering_type(0)`, `numbering_type1 = sand_numbering_type(4)`, `sanad_year`, `sanad_month`. | Inserts `numbering_type = 0`, `numbering_type1 = 0`, year/month. | Web is not VB6 parity for numbering types. |
| Payment method | `NoteCashingType`: 0 cash, 1 cheque, 2 transfer, 3 paid cheque. | Same numeric values. | Match. |
| Cash source | Requires `DcboBox`; saves `Notes.BoxID`; credit account from `TblBoxesData.Account_Code`. Calls `CheckBoxAccount` before save. | Requires `BoxId`; saves `Notes.BoxID`; credit account from `TblBoxesData.Account_Code`. No visible `CheckBoxAccount` equivalent in this flow. | Add VB6 cashbox-balance validation before save. |
| Bank source | Requires bank and reference/date for cheque/transfer/paid cheque; saves `BankID`, `ChqueNum`, `DueDate`. | Requires bank and reference; SQL preview requires reference date, C# validation does not explicitly require date. Saves `BankID`, `ChqueNum`, `DueDate`. | Require reference date consistently in C# and SQL. |
| Bank account mapping | Default credit account is `BanksData.Account_Code`. When `SystemOptions.banks_Accounts3 = True`, cheque uses `get_bank_Account(BankID,"Account_Code2")`; transfer/paid cheque force `BanksData.Account_Code`. | Preview uses only `BanksData.Account_Code` for payment methods 1, 2, 3. | Missing VB6 cheque mapping when `banks_Accounts3` is enabled. |
| Accounting record | Creates one `Double_Entry_Vouchers_ID` with two lines. Debit selected main account; credit source cashbox/bank account. | Creates one voucher id and one row per preview line. Usually two lines, but employee split can create three lines. | VB6 parity requires exactly two lines for this flow. |
| DEV line fields | Sets `DEV_ID_Line_No`, `DEV_ID_Line_No1 = setfoxy_Line`, `Account_Code`, `Value`, `Credit_Or_Debit`, description, `RecordDate`, `Notes_ID`, `UserID`, `Posted`, `Account_Interval_ID`, `branch_id`. | Sets `DEV_ID_Line_No`, account/value/debit-credit/description/date/note/user/branch, plus currency/rate/due date. Does not set `DEV_ID_Line_No1`, `Posted`, or `Account_Interval_ID`. | Missing line serial/posting/interval fields may affect legacy reports/accounting. |
| Posting status | If `CheckAprroveScreen(Me.Name) = True`, DEV `Posted = 1`; otherwise null. Header `NotePosted` is not observed in VB6 save. | Save does not set DEV `Posted`; no explicit approval-screen parity. | Add parity rule for DEV `Posted`; do not invent `NotePosted` behavior unless agreed. |
| Custody max check | Calls `CheckBoxmaxVaue(BTCashAccountcode, amount, maxvalue)`. If exceeded, user can confirm and continue. | No visible equivalent. | Add warning/override flow if parity is required. |
| Cashbox balance check | For cash payment, calls `CheckBoxAccount(BoxID, amount, date[, noteId])`; failure blocks save. | No visible equivalent in custody flow. | Add hard-block validation. |
| Cheque side-effect | If `SystemOptions.banks_Accounts3 = True` and payment method is cheque only, deletes/inserts `TblChecqueBoxContent1`. | No visible equivalent. | Missing if cheque custody vouchers are expected to appear in cheque box workflow. |
| Cost center side-effect | If general cost center is selected, deletes/inserts two rows in `marakes_taklefa_temp`. | No visible equivalent or UI field in this screen. | Keep out unless cost center UI is added; if added, mirror VB6. |
| Edit behavior | Loads existing `NoteType=50`, preserves serials, deletes/recreates DEV rows, deletes cost-center temp rows, recreates cheque content. | Preserves serials, deletes/recreates DEV rows. Does not recreate cheque content/cost center rows. | Add missing side-effect cleanup/recreate when implementing those features. |

## How VB6 Records Custody Replenishment

VB6 writes a custody replenishment as a payment-style note with `NoteType = 50` and `CashingType = 5`.

Header fields written to `Notes`:

- `NoteID`: `new_id("Notes","NoteID","",True)` on new.
- `branch_no`: selected branch.
- `NoteSerial`: `Notes_coding(branch,date)`.
- `NoteSerial1`: `Voucher_coding(branch,date,5,50)`.
- `OldNoteSerial1`: same voucher number on insert, preserved on edit.
- `Note_Value`: entered amount.
- `Remark`: remarks textbox.
- `general_des_notes`: general description textbox.
- `person`: manual person text, otherwise selected main account text.
- `NoteType`: `50`.
- `NoteDate`: transaction date.
- `CashingType`: `5` for custody replenishment, `6` for treasury funding.
- `BTCashAccountcode`: selected `TblBoxesData.Account_Code`.
- `BoxID`: cash payment source box, otherwise null.
- `BankID`: bank payment source, otherwise null.
- `ChqueNum`: cheque/transfer/paid cheque reference, otherwise null.
- `DueDate`: cheque/transfer/paid cheque date, otherwise null.
- `NoteCashingType`: `0` cash, `1` cheque, `2` transfer, `3` paid cheque.
- `UserID`: current user.
- `numbering_type`: `sand_numbering_type(0)`.
- `numbering_type1`: `sand_numbering_type(4)`.
- `sanad_year`, `sanad_month`: derived from transaction date.
- `note_value_by_characters`: amount text.

Accounting records written to `DOUBLE_ENTREY_VOUCHERS`:

- One `Double_Entry_Vouchers_ID`.
- Line 1 debit:
  - `DEV_ID_Line_No = 1`
  - `DEV_ID_Line_No1 = setfoxy_Line`
  - `Account_Code = DcboDebitSide.BoundText`
  - This is the selected main account, same account stored in `Notes.BTCashAccountcode`.
  - `Value = amount`
  - `Credit_Or_Debit = 0`
  - `Posted = 1` only when `CheckAprroveScreen(Me.Name)` is true; otherwise null.
- Line 2 credit:
  - `DEV_ID_Line_No = 2`
  - `DEV_ID_Line_No1 = setfoxy_Line`
  - `Account_Code = DcboCreditSide.BoundText`
  - Cash payment: selected `TblBoxesData.Account_Code`.
  - Bank payment: selected bank account, with special cheque behavior when `banks_Accounts3` is enabled.
  - `Value = amount`
  - `Credit_Or_Debit = 1`
  - Same `Posted` rule.

Payment/detail side effects:

- `TblChecqueBoxContent1`: only for cheque payment when `SystemOptions.banks_Accounts3 = True`.
- `marakes_taklefa_temp`: only if a general cost center is selected.
- Approval/log helper calls exist after save, but the persisted accounting status observed in the save routine is DEV `Posted`.

## Missing Behavior in POS Web

1. `NoteSerial1` generation does not match VB6. Web uses a global `MAX(NoteSerial1)+1`; VB6 uses `Voucher_coding(branch,date,5,50)`.
2. `numbering_type` and `numbering_type1` do not match VB6. Web writes `0,0`; VB6 calls `sand_numbering_type(0)` and `sand_numbering_type(4)`.
3. Web employee split is not a VB6 behavior. It can add a credit line to `TblEmployee.Account_Code`, creating a different journal effect.
4. Web does not visibly run the VB6 custody maximum check `CheckBoxmaxVaue`.
5. Web does not visibly run the VB6 cashbox balance check `CheckBoxAccount`.
6. Web bank credit account mapping does not include the VB6 `banks_Accounts3` cheque path to `BanksData.Account_Code2`.
7. Web DEV lines do not set `DEV_ID_Line_No1`, `Posted`, or `Account_Interval_ID`.
8. Web does not create `TblChecqueBoxContent1` rows for cheque custody vouchers.
9. Web does not recreate cost-center temp rows, which is acceptable only if this screen has no cost-center field.
10. C# validation does not explicitly require non-cash reference date, while the SQL preview does.

## Fields That Must Match VB6

These fields should be treated as parity-critical:

| Field | Required VB6 meaning |
|---|---|
| `Notes.NoteType` | Always `50`. |
| `Notes.CashingType` | `5` custody replenishment, `6` treasury funding. |
| `Notes.branch_no` | Selected branch. |
| `Notes.BTCashAccountcode` | Selected `TblBoxesData.Account_Code`, not employee account. |
| `Notes.Note_Value` | Full amount. |
| `Notes.NoteDate` | Transaction date. |
| `Notes.person` | Display/person name from manual text or selected main account. |
| `Notes.Remark` | User remarks. |
| `Notes.general_des_notes` | General description. |
| `Notes.BoxID` | Required for cash payment, null otherwise. |
| `Notes.BankID` | Required for non-cash payment, null for cash. |
| `Notes.ChqueNum` | Required for cheque/transfer/paid cheque. |
| `Notes.DueDate` | Required for cheque/transfer/paid cheque. |
| `Notes.NoteCashingType` | `0`, `1`, `2`, `3` payment method. |
| `Notes.NoteSerial` | Generated as `Notes_coding(branch,date)`. |
| `Notes.NoteSerial1` | Generated as `Voucher_coding(branch,date,5,50)`. |
| `Notes.OldNoteSerial1` | Same as voucher number on insert, preserved later. |
| `Notes.numbering_type` | `sand_numbering_type(0)`. |
| `Notes.numbering_type1` | `sand_numbering_type(4)`. |
| `Notes.sanad_year`, `Notes.sanad_month` | From transaction date. |
| DEV debit line | Debit selected main account for full amount. |
| DEV credit line | Credit selected cashbox/bank account for full amount. |
| DEV `Posted` | `1` when approval screen says immediate posting; otherwise null. |
| DEV `branch_id` | Same branch as header. |

## Fields That Can Be Improved in Web

These are useful web improvements, but they should not change VB6 accounting meaning:

- `LastModifiedByUserId` and `LastModifiedDate` audit columns.
- Branch-scoped lookup validation before save.
- Previewing the generated debit/credit lines before save.
- Friendlier validation messages.
- Clear UI wording that the main account is the custody/treasury account from `TblBoxesData`.
- Optional employee information as read-only context, but not as a posting split in VB6 parity mode.
- Separate future enhanced mode for employee custody split, with its own design and accounting approval.

## Final Agreed Save/Posting Design to Implement

Implement the current screen in VB6 parity mode first.

1. Validate required fields:
   - Branch.
   - Operation type `5` or `6`.
   - Main custody/treasury account from `TblBoxesData.Account_Code`, filtered by `Type = 1` for `CashingType = 5` and `Type = 0` for `CashingType = 6`.
   - Amount greater than zero.
   - Date.
   - Cash payment requires `BoxID`.
   - Cheque/transfer/paid cheque require `BankID`, reference number, and reference date.

2. Validate VB6 financial controls:
   - Run a `CheckBoxmaxVaue` equivalent for the selected main account and amount. If exceeded, return a warning that the UI must explicitly confirm before continuing.
   - Run a `CheckBoxAccount` equivalent for cash payments. Failure blocks save.

3. Generate serials:
   - New record: generate `NoteSerial` with the `Notes_coding(branch,date)` equivalent.
   - New record: generate `NoteSerial1` with the `Voucher_coding(branch,date,5,50)` equivalent.
   - Insert `OldNoteSerial1 = NoteSerial1`.
   - Edit record: preserve existing serial fields.
   - Set `numbering_type = sand_numbering_type(0)` and `numbering_type1 = sand_numbering_type(4)`.

4. Save `Notes` header:
   - Save the VB6 fields listed above.
   - In parity mode, set employee split fields to null/zero:
     - `Emp_ID = NULL`
     - `AccountEmpCode = NULL` or empty, matching existing column conventions
     - `boxValue = 0`
     - `EmpValue = 0`

5. Determine source credit account:
   - Cash: `TblBoxesData.Account_Code` for selected `BoxID`.
   - Transfer or paid cheque: `BanksData.Account_Code`.
   - Cheque:
     - If `SystemOptions.banks_Accounts3 = True`, use the same account as VB6 `get_bank_Account(BankID,"Account_Code2")`.
     - Otherwise use `BanksData.Account_Code`.

6. Save accounting:
   - Delete old DEV rows on edit.
   - Insert exactly two DEV rows under one `Double_Entry_Vouchers_ID`.
   - Debit selected main account full amount.
   - Credit selected source account full amount.
   - Set `DEV_ID_Line_No = 1/2`.
   - Set `DEV_ID_Line_No1` using the same line allocator as VB6 `setfoxy_Line`.
   - Set `Posted` using the same rule as `CheckAprroveScreen("FrmPayments1")`.
   - Set `Account_Interval_ID` consistently with the active interval used by VB6.
   - Set `branch_id`, `RecordDate`, `Notes_ID`, `UserID`, and description.

7. Save side effects:
   - For cheque only and `banks_Accounts3 = True`, delete/reinsert `TblChecqueBoxContent1` for the note.
   - Do not add cost-center rows unless the web screen adds a general cost center selector. If added, mirror `save_General_cost_center`.

8. Post-save behavior:
   - Commit header, accounting, cheque content, and cost-center side effects in one transaction.
   - Return `NoteID`, `NoteSerial`, `NoteSerial1`, and the two accounting lines.
   - Preserve current web audit fields as an improvement.

## Accounting Risks

- Employee split risk: posting to `TblEmployee.Account_Code` changes balances versus VB6 and can under-credit the cashbox/bank source.
- Serial risk: global `MAX(NoteSerial1)+1` can break legacy branch/date voucher numbering and reconciliation.
- Bank cheque risk: ignoring `Account_Code2` under `banks_Accounts3` can post cheque custody vouchers to the wrong bank/cheque clearing account.
- Posting-status risk: missing DEV `Posted` can make vouchers absent from reports that filter posted entries, or visible as unposted when VB6 would mark them posted.
- Cashbox-balance risk: missing `CheckBoxAccount` can allow cash source overdrafts that VB6 blocked.
- Custody-limit risk: missing `CheckBoxmaxVaue` can allow over-limit custody replenishment without the old explicit override.
- Line metadata risk: missing `DEV_ID_Line_No1` and `Account_Interval_ID` can affect legacy audit/report joins.
- Cheque workflow risk: missing `TblChecqueBoxContent1` means cheque custody vouchers may not appear in cheque-box follow-up screens.
