# Real Screen Mapping - Project Extracts / projectsbill.frm

## Source Of Truth

- Active VB6 form: `F:\Source Code\SatriahMain\Frm\New frm\projectsbill.frm`
- Active project reference: `F:\Source Code\SatriahMain\Account.vbp`, line `242`: `Form=Frm\New frm\projectsbill.frm`
- Search form: `Frm\New frm\projectsbill_search.frm`
- Report hubs: `frmProjectsReports.frm`, `ProjectsBillAlarm1.frm`, project-related forms under `Frm\New frm\projects`
- This mapping supersedes generic project invoice layouts. The web target is a modernized `projectsbill.frm`.

## Real VB6 Screen Shape

`projectsbill.frm` is not a simple invoice. It is a project progress/extract screen with:

- dense header fields;
- project/customer/subcontractor selection;
- progress item grid `Fg_Journal`;
- previous/current/cumulative quantity and value logic;
- deductions/retentions/material discounts;
- advance payment allocation grid `VSFlexGrid4`;
- bond/history grids;
- approval workflow;
- accounting note and double-entry posting;
- QR/e-invoice metadata.

The UI should preserve operational density and fast entry. A generic SaaS card layout is not appropriate.

## Header Controls And Field Mapping

| VB6 control | Role | Table/meaning |
| --- | --- | --- |
| `txtid` | project bill id | `project_billl.id` |
| `note_id` | accounting note id | `project_billl.note_id`, `Notes.NoteID` |
| `TxtNoteSerial` | accounting note serial | `Notes.NoteSerial` / display |
| `TxtNoteSerial1` | project invoice serial | `project_billl.NoteSerial1` |
| `txtManualNo` | manual number | `project_billl.ManualNo` |
| `XPDtbTrans` | bill date | `project_billl.bill_date` |
| `Dcbranch` | branch | `project_billl.Branch_NO` |
| `DataCombo2` | project selector | `project_billl.project_no` / `projects.id` |
| `txtprojectname` | project name | `project_billl.project_name` |
| `billto` | party type | end user/customer vs subcontractor |
| `DcbosubContractor` | subcontractor selector | `subContractorId` |
| `DcAccount1`, `DcAccount2`, `txtendaccount`, `txtsubaccount` | party accounts | end/sub account behavior |
| `TXTOrDer_no`, `TXTOrDer_no2` | project/order/contract references | source lines and subcontractor contract flow |
| `CBoBasedON` | source basis | controls project lines retrieval path |
| `Option7`, `Option6`, `Option8` | actual/estimated/under-implementation | changes posting account/value behavior |
| `TxtAccountUnderImp` | under implementation account | debit/credit branch in posting |
| `TxtRemarks` | remarks | header remarks |

## Totals, VAT, Deduction Controls

| VB6 control | Meaning |
| --- | --- |
| `total` | gross/base total |
| `Results` | result after line execution calculations |
| `TxtNetValue` | net value before VAT-related finalization |
| `TxtFATYou` | VAT percent |
| `TxtFATValue` | VAT value |
| `TxtTotalValue` | total including VAT/effects |
| `txtDiscount` | discount |
| `txtDiscount1`, `txtDiscount2`, `txtDiscount3`, `txtDiscount4` | separate deduction/discount channels |
| `txtDiscountG` | general deduction allocated across lines |
| `txtDiscountGMater` | material deduction |
| `TxtPerforValue` | performance/retention value |
| `txtPerformanceBond` | performance bond |
| `advancedPayment` | advance payment deduction |
| `TxtPreVAT` | VAT related to advance payment |
| `AccountVat` | VAT account |
| `DcDiscountAccount`, `txtDiscountAccountCode` | discount account |
| `chkTaxExempt` | tax-exempt option |
| `txt_Currency_rate`, `DcCurrency` | currency/rate |

## Real Grid Mapping

| VB6 grid | Purpose | Must preserve |
| --- | --- | --- |
| `Fg_Journal` | main project line grid | `item`, `cost`, `exe`, `Quantity`, `Price`, `Pre_Quantity`, `Curr_Quantity`, `tot_quantity`, approval fields, `NetExe`, VAT totals |
| `VSFlexGrid4` | advance/prepaid notes | source `Notes`, paid/trans-paid/net/remaining, links to `TblPayPrePayed` and `TblProjePayPrePayed` |
| `GrdBondHistory` | bond/history rows | saved to `TBLProjectBillHistory` |
| `GRID2` | approval/status display | approval workflow rows |
| `VSFlexGrid1`, `VSFlexGrid2`, `VSFlexGrid3` | supporting operational grids | employees/items/expenses/source data depending on options |

The current MainErp page does not yet load `project_bill_details`; that is the biggest gap to close next.

## Real Buttons And Workflow

| VB6 member/control | Behavior |
| --- | --- |
| `Cmd_Click(Index)` | standard toolbar workflow; calls new/save/edit/delete/search behavior depending on index |
| `SaveData` | main save/post routine |
| `Savetemp` | saves details, line allocations, QR, advance-payment links |
| `Accredit_Click` | sends document to approval with `SendTopost Me.Name, "project_billl", "id", ...` |
| `ALLButton1_Click` | loads advance payment notes into `VSFlexGrid4` |
| `Command10_Click` | cancels/clears advance payment allocation |
| `ALLButton2_Click` / `ALLButton3_Click` | supporting grid row actions |
| `Fg_Journal_*` events | calculate/edit project lines |
| `VSFlexGrid4_*` events | advance payment selection/allocation |
| `TXTOrDer_no2_KeyUp` | F3 subcontractor contract search |

The web version must keep `الإرسال للاعتماد`, advance-payment selection, and dense grid editing as recognizable first-class workflows.

## Save Flow To Preserve

`SaveData` starts around line `6821` in `projectsbill.frm`.

1. Run `calcnet`.
2. Determine party through `billto`.
3. Start `Cn.BeginTrans`.
4. New document:
   - validate customer/project;
   - allocate `note_id = new_id("Notes","NoteID","",True)`;
   - allocate `txtid = new_id("project_billl","id","",True)`;
   - generate `TxtNoteSerial1` with `Voucher_coding`;
   - generate `TxtNoteSerial` with `Notes_coding`.
5. Edit document:
   - delete old `DOUBLE_ENTREY_VOUCHERS` by `Notes_ID`;
   - delete old `Notes`;
   - delete old `project_bill_details`.
6. Create `Notes` with `NoteType = 5000`.
7. Populate `project_billl` header/totals/VAT/deductions/status/e-invoice fields.
8. Call `SaveBillMonthly`.
9. Create `DOUBLE_ENTREY_VOUCHERS` rows based on end-user/subcontractor branch and options.
10. Call `Savetemp`.
11. Commit transaction.
12. After commit, optionally generate/send e-invoice through `savenewelectroncic` / `SENDEINVOICE`.

## Detail Save Flow

`Savetemp` starts around line `13050`.

1. Save `TBLProjectBillHistory` from `GrdBondHistory`.
2. Delete old `project_bill_details`.
3. Insert one detail row per populated `Fg_Journal` row.
4. Persist:
   - project/item/account fields;
   - source quantities and prices;
   - previous/current/cumulative quantities and values;
   - approval fields;
   - subcontractor quantity/cost;
   - old/current/total VAT fields.
5. Calculate line allocation:
   - `LineDiscountPercent = NetExe / Results`
   - `LineDiscount = txtDiscountG * LineDiscountPercent`
   - `PerforVLineDiscount = TxtPerforValue * LineDiscountPercent`
   - `linenetaftermainDiscountBeforevat = NetExe - LineDiscount`
   - `LineVat = linenetaftermainDiscountBeforevat * TxtFATYou / 100`
6. Call `UpdatePre_QuantityCont` when contract-based previous quantities apply.
7. Call `updateNotesValueAndNobytext(note_id)`.
8. Call `saveBillBuy`.
9. Save QR data through `SaveQRCode` or `SaveQRCode6`.

## Approval Flow

`Accredit_Click`:

- blocks when `txtid = 0`;
- calls `SendTopost`;
- changes caption to `تم الارسال للاعتماد`;
- reloads the document.

`fillapprovData` loads `ApprovalData` into `GRID2` and displays whether the document is fully approved or currently requires approval.

## Current MainErp Status After Correction

Real migrated:

- Project Extract list reads actual `project_billl`.
- Details read actual header values and core accounting fields.
- Details page is now shaped as a modernized `projectsbill.frm` screen with workflow buttons visible but disabled.

Still placeholder:

- `Fg_Journal` line grid not loaded;
- advance payment grid not loaded;
- approval data grid not loaded;
- save/edit/delete not implemented;
- `Notes` and `DOUBLE_ENTREY_VOUCHERS` writes disabled;
- `Savetemp`, `saveBillBuy`, QR/e-invoice side effects not implemented;
- no Project Reports migration yet.

## Next Implementation Order

1. Load `project_bill_details` read-only under the details page with exact `Fg_Journal` columns.
2. Load advance/prepaid allocations from `TblPayPrePayed` / `TblProjePayPrePayed`.
3. Load approval status from `ApprovalData`.
4. Port `ReLineGrid`, `calcnet`, and critical change handlers into deterministic server/client calculations.
5. Implement draft save that writes header and details only.
6. Implement accounting preview matching `SaveData`.
7. Enable real posting only after regression comparison against VB6 sample invoices.
