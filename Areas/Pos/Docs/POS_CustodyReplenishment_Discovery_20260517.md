# POS Kishny Web - Custody Replenishment Discovery

Date: 2026-05-17

Scope: `Areas/Pos`, database `Cash`, and legacy VB6 Kishny custody replenishment workflow.

## 1. Current POS Web Flow

Primary screen:

- `Areas/Pos/Views/Payments/Index.cshtml`
- `Areas/Pos/Controllers/PaymentsController.cs`
- `Areas/Pos/Data/PosSqlRepository.cs`
- SQL support: `Areas/Pos/Sql/28_POS_Payments_Audit.sql`, `Areas/Pos/Sql/73_POS_CustodyFundingRefund_Relationships.sql`

The sidebar exposes the screen as `/Pos/Payments/Index` under الحسابات with text `العهد / كيشني كارت`. The screen title and entry flow use `تمويل العهدة`.

Runtime flow:

1. User opens `/Pos/Payments/Index`.
2. `PaymentsController.Index` requires POS login and `CanOpenPayments` unless full access.
3. Search panel queries previous movements through `Search`, which calls `PosSqlRepository.SearchPosPayments` and stored procedure `dbo.usp_POS_Payments_Search`.
4. New/edit panel collects:
   - Branch
   - Date
   - Operation type: `5 = استعاضة عهدة`, `6 = تمويل خزينة`
   - Main account/name account
   - Payment method: `0 = نقدي`, `1 = شيك`, `2 = حوالة`, `3 = شيك مسدد`
   - Cashbox or bank
   - Reference number/date for non-cash
   - Optional employee custody split: employee, employee custody account, `BoxValue`, `EmpValue`
   - Remarks/general description
5. Lookups and relationship validation use `dbo.usp_POS_CustodyFundingRefund_GetLookups`.
6. Account balance uses `dbo.usp_POS_CustodyFundingRefund_GetAccountBalance`.
7. Employee custody account uses `dbo.usp_POS_CustodyFundingRefund_GetEmployeeCustodyAccount`.
8. Preview uses `dbo.usp_POS_CustodyFundingRefund_Preview`.
9. Save uses `PosSqlRepository.SavePosPayment`.

Save behavior:

- Creates/updates `dbo.Notes` with `NoteType = 50`.
- Stores operation in `Notes.CashingType` as `5` or `6`.
- Stores main account in `Notes.BTCashAccountcode`.
- Stores payment method in `Notes.NoteCashingType`.
- Stores cashbox/bank in `Notes.BoxID` or `Notes.BankID`.
- Stores branch/user/date/value/remarks in legacy note fields.
- Stores the new split fields in `Notes.BoxValue`, `Notes.EmpValue`, and `Notes.AccountEmpCode`.
- Deletes and recreates `dbo.DOUBLE_ENTREY_VOUCHERS` rows on edit.
- Generates new note serial with `GenerateNotesSerial`, but generates `NoteSerial1` with `MAX(NoteSerial1) + 1` over all `NoteType = 50`.

Current web GL preview/save shape:

- Main account is debit for the full amount.
- Payment source is credit.
- If no split values are entered, the cashbox/bank account is credited for the full amount.
- If split values are entered:
  - Cashbox/bank account is credited for `BoxValue`.
  - Employee custody account is credited for `EmpValue`.
- Total debits and credits must match.

## 2. Old VB6 Custody Replenishment Flow

Legacy screen:

- `F:\Source Code\_backup_frm\Frm\FrmPayments1.frm`
- Screen registration text:
  - Arabic: `استعاضة عهده وتمويل خزينة`
  - English: `Bt Cash`
- Menu language:
  - `استعاضه عهده`
  - `تمويل خزينة`
  - English equivalents: `Bety Cash`, `Box Recharge`

Legacy list/open behavior:

- Opens `Notes` where `NoteType = 50`.
- A previous query comment shows an older compatibility idea: `NoteType = 50 OR (NoteType = 5 AND CashingType IN (5, 6))`, but the active VB6 query is only `NoteType = 50`.

Legacy entry/save workflow:

1. User selects operation type in `DCboCashType`.
   - List index `0` maps to `CashingType = 5` (`استعاضه عهده`).
   - List index `1` maps to `CashingType = 6` (`تمويل خزينة`).
2. The name/account combo loads box accounts:
   - Case `5`: `GetBoxAccounts(..., 1)` for petty cash/custody.
   - Case `6`: `GetBoxAccounts(..., 0)` for treasury/box recharge.
3. User selects payment method:
   - `0 = نقدي`
   - `1 = شيك`
   - `2 = حوالة`
   - `3 = شيك مسدد`
4. Cash payment requires a cashbox. Non-cash requires a bank and check/transfer number.
5. VB6 checks source cashbox balance through `CheckBoxAccount`.
6. VB6 checks maximum custody threshold through `CheckBoxmaxVaue`; if exceeded, it asks for confirmation and allows continuation.
7. VB6 generates:
   - GL serial through `Notes_coding(branch, date)`.
   - Voucher serial through `Voucher_coding(branch, date, 5, 50)`.
8. VB6 writes `dbo.Notes`:
   - `NoteType = 50`
   - `CashingType = DCboCashType.ListIndex + 5`
   - `BTCashAccountcode = selected box account`
   - `BoxID`/`BankID`
   - `NoteCashingType`
   - `branch_no`, `UserID`, `NoteDate`, `Note_Value`, remarks, serials, numbering fields
9. VB6 posts exactly two `dbo.DOUBLE_ENTREY_VOUCHERS` lines:
   - Debit: `DcboDebitSide`
   - Credit: `DcboCreditSide`
   - Same amount on both lines
10. VB6 optionally writes check-box content through `saveChequeBoxContents1`.
11. VB6 optionally writes general cost center rows to `marakes_taklefa_temp`.

Important old-flow detail:

- `FrmPayments1.frm` does not post separate employee custody split lines using `BoxValue`/`EmpValue`.
- It treats the selected name/account as the main debit account and the selected payment source as the credit account.

## 3. Related Tables And Fields

Core voucher header:

- `dbo.Notes`
  - Identity/key: `NoteID`
  - Dates/serials: `NoteDate`, `NoteSerial`, `NoteSerial1`, `OldNoteSerial1`, `sanad_year`, `sanad_month`
  - Type fields: `NoteType`, `CashingType`, `NoteCashingType`
  - Amount/text: `Note_Value`, `note_value_by_characters`, `Remark`, `general_des_notes`, `person`
  - Branch/user: `branch_no`, `UserID`, `LastModifiedByUserId`, `LastModifiedDate`
  - Payment source: `BoxID`, `BankID`, `ChqueNum`, `DueDate`
  - Main account: `BTCashAccountcode`
  - Employee/custody fields: `Emp_ID`, `EmpId`, `EmployeeID`, `EmpAccountCode`, `AccountEmpCode`, `BoxValue`, `EmpValue`

GL detail:

- `dbo.DOUBLE_ENTREY_VOUCHERS`
  - `Double_Entry_Vouchers_ID`, `DEV_ID_Line_No`, `DEV_ID_Line_No1`
  - `Account_Code`, `NextAccount_Code`
  - `Value`, `Credit_Or_Debit`
  - `Double_Entry_Vouchers_Description`
  - `RecordDate`, `Notes_ID`, `UserID`, `Posted`, `Account_Interval_ID`, `branch_id`

Accounts:

- `dbo.ACCOUNTS`
  - `Account_ID`, `Account_Code`, `Account_Serial`, `Account_Name`, `Account_NameEng`, `last_account`, `BranchID`

Cashboxes/treasuries:

- `dbo.TblBoxesData`
  - `BoxID`, `BoxName`, `BoxNameE`, `Account_Code`, `Type`, `BranchId`, `empid`, `BTtype`, `Account_Code2`
  - Legacy interpretation in this flow: `Type = 1` for custody/petty cash account list, `Type = 0` for treasury/box recharge account list.

Banks:

- `dbo.BanksData`
  - `BankID`, `BankName`, `BankNamee`, `Account_Code`, `Account_Code2`, `BranchId`

Employees:

- `dbo.TblEmployee`
  - `Emp_ID`, `Emp_Name`, `Emp_Namee`, `Fullcode`, `BranchId`, `Account_code`, `Account_Code2`
  - POS Web uses `Account_code` as the employee custody account.

Branches/users/POS boxes:

- `dbo.TblBranchesData`: `branch_id`, `branch_Code`, `branch_name`, `branch_namee`, `Account_Code`, `Account_Code2`, `StoreId`
- `dbo.TblUsers`: `UserID`, `UserName`, `BranchId`, `StoreID`, `BoxID`, `BankID`, `Empid`, `BoxID2`, `Account_Code`
- `dbo.Tblposdata`: `BoxID`, `BranchId`, `BoxName`, `BoxNamee`, `EmpID`, `Account_Code`

Support tables:

- `dbo.TblChecqueBoxContent1` is touched by old `saveChequeBoxContents1` for cheque/payment tracking.
- `dbo.marakes_taklefa_temp` is touched by old general cost center distribution.

Current live `Cash` snapshot:

- `dbo.Notes` has `NoteType = 50` records.
- Recent rows use `CashingType = 5`, bank payment (`NoteCashingType = 2`), `BankID = 8`.
- Aggregate by `NoteType = 50` at discovery time:
  - `CashingType = 5`: 9,887 rows, total about 196,126,340.26
  - `CashingType = 6`: 5 rows, total 18,939.00

## 4. Current POS Web Gaps

1. Old max-custody warning is not matched.
   - VB6 calls `CheckBoxmaxVaue` and allows override after confirmation.
   - POS Web validates account relationships but does not appear to implement the same maximum-custody warning/override.

2. Old cashbox balance check is not clearly matched.
   - VB6 calls `CheckBoxAccount` before save for cash payment.
   - POS Web validates selected cashbox/bank relationship and balances the GL, but I did not find an equivalent cashbox available-balance check in the custody save path.

3. GL split behavior differs from VB6.
   - VB6 posts two GL lines only.
   - POS Web can post three lines by splitting credit between cashbox/bank and employee custody account.
   - This may be intended, but it is not old-flow parity.

4. `BoxValue`/`EmpValue` are new behavioral fields.
   - They exist in `dbo.Notes`, and POS Web reads/writes them.
   - VB6 `FrmPayments1.frm` does not populate them in the observed save path.

5. Serial allocation may not match old voucher coding exactly.
   - VB6 uses `Voucher_coding(branch, date, 5, 50)`.
   - POS Web uses `GenerateNotesSerial` for `NoteSerial`, but `NoteSerial1` is `MAX(NoteSerial1) + 1` for all `NoteType = 50`.
   - If Kishny requires branch/month scoped voucher serials, this is a risk.

6. Cost center and cheque-box side effects are not fully matched.
   - VB6 writes `marakes_taklefa_temp` when a general cost center is selected.
   - VB6 calls `saveChequeBoxContents1`.
   - POS Web custody save does not appear to call these legacy side-effect routines.

7. Posting/approval behavior may differ.
   - VB6 sets `DOUBLE_ENTREY_VOUCHERS.Posted = 1` when `CheckAprroveScreen(Me.Name)` is true.
   - POS Web custody save does not appear to set `Posted` in `AddPosPaymentDevLine` based on the same screen approval rule.

8. Naming mismatch in Web UI.
   - Web title is mostly `تمويل العهدة`.
   - Old screen is explicitly `استعاضة عهده وتمويل خزينة`.
   - Web does include both operation choices, but the screen title may underrepresent `تمويل خزينة`.

## 5. Exact Implementation Plan

1. Decide parity target for accounting shape.
   - Option A: strict VB6 parity: keep two GL lines only and remove/disable employee split for this module.
   - Option B: enhanced POS Web behavior: keep `BoxValue`/`EmpValue` split, but document it as a deliberate extension and reconcile reports accordingly.

2. Reproduce VB6 validations in SQL/application layer.
   - Identify and port `CheckBoxmaxVaue` logic.
   - Identify and port `CheckBoxAccount` logic.
   - Add preview warnings separately from hard validation where VB6 allowed override.

3. Align serial allocation.
   - Compare `Voucher_coding(branch, date, 5, 50)` with POS Web `NoteSerial` and `NoteSerial1` generation.
   - If needed, switch `NoteSerial1` generation to the existing POS voucher allocator or a stored procedure that respects the same branch/month/type scope.

4. Align save side effects.
   - Confirm whether `TblChecqueBoxContent1` matters for Kishny bank/check reporting.
   - If yes, implement equivalent write/delete on save/edit.
   - Confirm whether general cost center is needed in POS Web custody. If yes, add fields and save `marakes_taklefa_temp` rows.

5. Harden `SavePosPayment`.
   - Keep all validation and save work inside one transaction where possible.
   - Avoid preview-then-save race by performing validation inside the save transaction.
   - Lock the edited `Notes` row and its DEV rows consistently.

6. Update UI after accounting decisions.
   - If strict parity: remove or hide employee split fields from this screen.
   - If enhanced behavior: make split behavior explicit in labels and validations, and include it in search/details.
   - Rename page title to `استعاضة العهدة / تمويل الخزينة`.

7. Add verification scripts/tests.
   - Create test cases for operation type 5 and 6.
   - Test payment methods 0, 1, 2, 3.
   - Test branch-scoped lookups.
   - Test edit path deletes/recreates DEV rows correctly.
   - Test old VB6-created rows load in Web without field loss.
   - Test POS Web-created rows remain visible/printable/reportable in old reports.

## 6. Accounting, Balance, And Cashbox Risks

1. Cashbox balance risk.
   - Missing or mismatched `CheckBoxAccount` behavior can allow funding from a cashbox that legacy would block.

2. Custody limit risk.
   - Missing `CheckBoxmaxVaue` behavior can allow custody balances above configured thresholds without the old warning/approval pattern.

3. GL parity risk.
   - POS Web split lines can change account balances versus the old two-line entry.
   - This affects custody account balances, cashbox/bank balances, employee balances, and any reports grouped by `Notes_ID`.

4. Serial duplication/scope risk.
   - `MAX(NoteSerial1) + 1` on `NoteType = 50` is concurrency-sensitive and may not match branch/month legacy coding.

5. Reporting compatibility risk.
   - Old reports may expect `BTCashAccountcode`, `BoxID`/`BankID`, `NoteCashingType`, and exactly two DEV rows.
   - New split fields may be ignored by old reports unless they read DEV lines.

6. Edit/delete side-effect risk.
   - POS Web deletes/recreates DEV lines but does not obviously mirror all old side tables such as cheque-box content and cost center temp rows.

7. Approval/posting risk.
   - If approval settings require `Posted = 1` for this screen, POS Web-created entries may remain unposted from the perspective of legacy reports.

8. Branch relationship risk.
   - POS Web filters boxes, banks, employees by branch, but old data may contain global or loosely assigned accounts. Strict validation could block legitimate historical patterns unless migration rules are agreed.
