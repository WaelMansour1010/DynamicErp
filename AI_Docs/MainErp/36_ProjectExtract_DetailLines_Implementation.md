# Project Extract Detail Lines Implementation

Date: 2026-05-07

## Scope

Fixed `/MainErp/ProjectExtracts/Details/{id}` so the details page is no longer a header-only placeholder. It now loads the real operational data for project extracts from the Eng legacy schema through `MainErp_ConnectionString`.

This phase is read-only only. No save, edit, post, delete, or database write behavior was added.

## Source Of Truth

Legacy screen:

- `F:\Source Code\SatriahMain\Frm\New frm\projectsbill.frm`

Relevant VB6 mappings:

- `Fg_Journal` loads from `project_bill_details` around the VB6 query:
  - `SELECT item_id,id, project_no, item, cost, exe, percentage, exedate, bill_id,item_unit, Unit_id, Quantity, Price, Pre_Quantity, Pre_Value, Pre_Percent, Curr_Quantity, Curr_value, curr_Percent, tot_quantity, tot_value, tot_percent FROM dbo.project_bill_details`
- `VSFlexGrid4` loads linked advance/prepaid rows from:
  - `TblProjePayPrePayed`
  - `TblPayPrePayed`
- Accounting rows are linked through:
  - `DOUBLE_ENTREY_VOUCHERS`

## Tables Read

- `project_billl`
- `project_bill_details`
- `TblPayPrePayed`
- `TblProjePayPrePayed`
- `DOUBLE_ENTREY_VOUCHERS`
- `Notes`
- `projects`
- `TblCustemers`
- `TblBranchesData`
- `ACCOUNTS`

## Detail Fields Loaded

From `project_bill_details` where `bill_id = project_billl.id`:

- `id`
- `item`
- `FullCode`
- `item_unit`
- `Quantity`
- `Price`
- `cost`
- `Pre_Quantity`
- `Pre_Value`
- `Curr_Quantity`
- `Curr_value`
- `tot_quantity`
- `tot_value`
- `curr_Percent`
- `tot_percent`
- `LineDiscount`
- `linenetaftermainDiscountBeforevat`
- `LineVat`
- `linenetaftermainDiscountWithvat`
- `PerforVLineDiscount`
- `LineFinal`
- `AccountCode`

## Advance Payment Linkage

The details page now reads:

- `TblPayPrePayed` where `NoteID1 = project_billl.id`
- `TblProjePayPrePayed` where `NoteID = project_billl.id`

Displayed fields include:

- source table
- `NoteID`
- `Transaction_ID`
- `NoteSerial1` / note serial
- `NoteDate`
- `Note_Value`
- `PayedValue`
- `TransPayedValue`
- `RemainingValue`
- `NetValue`
- `VAT`
- `TypeTrans`
- `NCashingType`
- branch
- linked account when present

If no rows exist, the page shows:

`لا توجد دفعات مقدمة مرتبطة بهذا المستخلص`

## Voucher Linkage

The journal section reads `DOUBLE_ENTREY_VOUCHERS` where:

- `Notes_ID = project_billl.note_id`
- or `project_bill_no = project_billl.id`
- or `bill_id = project_billl.id`

Displayed fields:

- voucher id
- line number
- note id
- note serial
- record date
- account display
- debit
- credit
- description
- branch
- project

Each voucher line with `NoteId` links to:

- `/MainErp/JournalEntries/DetailsByNote/{noteId}`

## Account Display Rule

Raw account codes are kept internal only. User-facing accounts are displayed as:

`Account_Serial - Account_Name`

Applied to:

- `revenue_account`
- `AccountUnderImp`
- `AccountCodeVat`
- `End_user_account`
- `Sub_user_account`
- detail `AccountCode`
- voucher line `Account_Code`
- advance payment account code when present

If the account does not join to `ACCOUNTS`, the page shows:

`الحساب غير موجود`

## Validation Against Eng

Sample requested by user:

- `project_billl.note_id = 222097`
- corresponding `project_billl.id = 3499`

Observed:

- `project_bill_details` rows: `1`
- `TblPayPrePayed` rows for `NoteID1 = 3499`: `0`
- `TblProjePayPrePayed` rows for `NoteID = 3499`: `0`
- `DOUBLE_ENTREY_VOUCHERS` linked rows: `4`

Sample detail line:

- item: `Construction Of College Of Engineering Alfaisal University`
- full code: `j1021`
- VAT: `225`
- final: `1725`
- account display: `5101070380 - Hire Of Plant , Machinery & Vehicls (PMV) External`

## Files Modified

- `F:\Source Code\DynamicErp\Areas\MainErp\ViewModels\ProjectExtracts\ProjectExtractsIndexViewModel.cs`
- `F:\Source Code\DynamicErp\Areas\MainErp\Repositories\ProjectExtracts\ProjectExtractReadRepository.cs`
- `F:\Source Code\DynamicErp\Areas\MainErp\Views\ProjectExtracts\Details.cshtml`

## Safety

- No database writes.
- No stored procedures created or modified.
- No `AllScripts.sql` change.
- No `Areas\Pos` change in this fix.
- Build succeeded.

## Remaining Unmapped Areas

- Exact save/post lifecycle from `projectsbill.frm`.
- Destructive voucher rebuild behavior.
- Full approval/posting statuses.
- Report templates.
- Full advance payment cancellation/edit behavior.
- Any hidden business conditions around `ALLButton1_Click` and `saveBillBuy`.
