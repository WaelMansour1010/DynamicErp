# Project Extracts Accounting Flow

Scope: مستخلصات المشاريع / Project Extracts. Active source: `F:\Source Code\SatriahMain\Frm\New frm\projectsbill.frm`.

## Active Entry Points

| VB6 member | Lines observed | Purpose |
| --- | ---: | --- |
| `Search` / `maaRetrive` | 6120-6247 | Loads `project_billl` and `project_bill_details`. |
| `Accredit_Click` | 6318-6335 | Sends saved extract to approval/posting workflow through `SendTopost`. |
| `ALLButton1_Click` | 6442 onward | Loads advance/prepaid notes into `VSFlexGrid4`. |
| `DeleteBillBuy` | 6657-6668 | Resets `Notes.TotalPayed` for selected advance-payment notes. |
| `saveBillBuy` | 6669-6745 | Saves advance-payment deduction links. |
| `SaveData` | 6821-8034 | Main extract save and accounting posting routine. |
| `Savetemp` | 13050-13205 | Saves detail rows, line VAT/discount allocations, QR/e-invoice artifacts, and calls `saveBillBuy`. |

## SaveData Transaction Sequence

1. `calcnet` runs first.
2. Party selection:
   - `billto.ListIndex=0` means end-user/customer extract.
   - `billto.ListIndex=1` means subcontractor extract.
   - Code later forces `X = TXTEnd_user_id`, so this must be reviewed before migration.
3. `Cn.BeginTrans` starts at line 6853.
4. Existing note lookup opens `Notes` by `NoteType=5000` and `NoteSerial`.
5. New extract:
   - Validates customer id.
   - Assigns `note_id = new_id("Notes", "NoteID", "", True)`.
   - Assigns `txtid = new_id("project_billl", "id", "", True)`.
   - Adds a new `project_billl` row.
6. Edit extract:
   - Deletes `DOUBLE_ENTREY_VOUCHERS` where `Notes_ID = note_id`.
   - Deletes `Notes` where `NoteID = note_id`.
   - Deletes `project_bill_details` where `bill_id = txtid`.
7. Creates a new `Notes` row:
   - `NoteType = 5000`.
   - `NoteDate = XPDtbTrans`.
   - `NoteSerial1` is generated with `Voucher_coding`:
     - type `65` for end-user extract.
     - type `84` for subcontractor extract.
   - `NoteSerial` is generated with `Notes_coding`.
   - `branch_no`, `UserID`, `CusID`, fiscal year/month, and Arabic amount text are written.
8. Populates `project_billl`:
   - Header: dates, project, party names/accounts, branch, note id, manual number, period, due dates.
   - Totals: `total`, `Results`, `NetValue`, `TotalValue`.
   - VAT: `FATYou`, `FATValue`, `AccountCodeVat`, `PreVAT`, `PreBala*`, `SumVATLine`, `SumValueLine`.
   - Deductions: `discount`, `discount1/2`, `DiscountGMater`, `PerformanceBond`, `AdvancedPayment`.
   - Status/audit: `UserID`, `Approved`, `Posted`, currency and e-invoice metadata.
9. Calls `SaveBillMonthly`.
10. Saves the `project_billl` row.
11. Opens empty `DOUBLE_ENTREY_VOUCHERS`, allocates `LngDevID`, and determines `Posted` using `CheckAprroveScreen(Me.Name)`.
12. Creates voucher rows.
13. Calls `Savetemp`.
14. Commits the transaction at line 7994.
15. After commit, may generate/send e-invoice:
   - `savenewelectroncic` when normal invoice, not tax exempt, and `SystemOptions.ApplyEinvoice`.
   - `SENDEINVOICE` when `SystemOptions.IsBluee=True` and normal invoice.
16. Error trap cancels recordset update and rolls back the transaction.

## End-User Extract Posting (`billto.ListIndex=0`)

Main debit row:

- Debits `TxtAccountUnderImp` when `Option8=True`, otherwise customer account `accountdep`.
- Value is `TxtTotalValue` when `SystemOptions.CustCreat4Acc=True`; otherwise `total + TxtFATValue`.
- Links `Notes_ID`, `project_bill_no`, `project_id`, branch, user, period, and posted flag.

Customer account lookups:

- `AdvancedAccount = TblCustemers.Account_CodeHi1`.
- `GuranteeAccount = TblCustemers.Account_CodeAss2`.
- `mAccountMaterial = TblCustemers.Account_CodeHi2`.

Observed posting patterns:

- General discounts/deductions debit `DcDiscountAccount` or branch default account depending on settings.
- Performance/retention value (`TxtPerforValue`) posts to guarantee/performance accounts.
- `txtPerformanceBond` posts a credit to `GuranteeAccount`; a debit row is commented out in the inspected block.
- Material deduction (`txtDiscountGMater`) debits `accountdep` and credits `mAccountMaterial`.
- Advance payment (`advancedPayment`) debits `AdvancedAccount`; additional customer/advance rows are inserted manually in the surrounding block.
- VAT (`TxtFATValue`) credits `AccountVat`.
- Advance-payment VAT (`TxtPreVAT`) debits `AccountVat` and credits the customer/under-implementation account when `SuppCreat4Acc=False`.

## Subcontractor Extract Posting (`billto.ListIndex<>0`)

Observed branch begins around line 7640.

Posting patterns:

- Main work value debits `expanses_account` or `TxtAccountUnderImp` depending on `UnderImp` option.
- Credits subcontractor/customer account `accountdep`.
- VAT (`TxtFATValue`) debits `AccountVat`.
- Work guarantee/retention (`Discount1`) debits `accountdep` and credits `GuranteeAccount`.
- Performance bond debits `accountdep` and credits `GuranteeAccount`.
- Material deduction debits `accountdep` and credits `mAccountMaterial`.
- General deductions (`txtDiscountG`) debit `accountdep` and credit `DcDiscountAccount`.
- Advance payment debits `accountdep` and credits `AdvancedAccount`.
- Advance VAT (`TxtPreVAT`) credits `AccountVat`.
- Additional VAT adjustment can credit `accountdep` by `TxtFATValue`.

## Detail Save and Line Calculations

`Savetemp`:

1. Saves `TBLProjectBillHistory` from `GrdBondHistory`.
2. Deletes existing `project_bill_details` for the bill.
3. Inserts a row per non-empty grid item.
4. Persists project/item/account references, quantities, previous/current/cumulative values, approval fields, subcontractor quantity/cost, and previous/current VAT totals.
5. Allocates overall discount and performance retention by line:
   - `LineDiscountPercent = NetExe / Results`.
   - `LineDiscount = txtDiscountG * LineDiscountPercent`.
   - `PerforVLineDiscount = TxtPerforValue * LineDiscountPercent`.
   - `linenetaftermainDiscountBeforevat = NetExe - LineDiscount`.
   - `LineVat = linenetaftermainDiscountBeforevat * TxtFATYou / 100`.
   - `LineFinal = linenetaftermainDiscountWithvat - PerforVLineDiscount`.
6. Calls `UpdatePre_QuantityCont` for contract-based previous quantities when applicable.
7. Calls `updateNotesValueAndNobytext(note_id)`.
8. Calls `saveBillBuy`.
9. Saves QR code data with either `SaveQRCode` or `SaveQRCode6` depending on `SystemOptions.SuppCreat4Acc`.

## Advance Payment Deduction Flow

`ALLButton1_Click` loads candidate notes where `Notes.NCashingType = 3`, joins `TblCustemers` and `TblBranchesData`, and reads existing `TblProjePayPrePayed` / `TblPayPrePayed` rows for edits.

`saveBillBuy`:

1. On edit, deletes old `TblPayPrePayed` by `NoteID1 = txtid`.
2. Deletes old `TblProjePayPrePayed` by `NoteID = txtid`.
3. Inserts selected `TblPayPrePayed` rows:
   - `NoteID1 = project_billl.id`.
   - Source note in `NoteID`.
   - Copies note value, VAT line, value line, branch, paid/trans-paid/net/remaining values.
4. Updates source `Notes.TotalPayed` to `1` when net value becomes zero, otherwise `Null`.
5. Inserts `TblProjePayPrePayed` rows:
   - `NoteID = project_billl.id`.
   - `Transaction_ID = source Notes.NoteID`.
   - Stores source note value and applied payment.

## Approval, Edit, Cancellation

- Approval submission is `Accredit_Click`, which calls `SendTopost Me.Name, "project_billl", "id", ...`.
- Edit is destructive/rebuild-style for accounting rows and details: old vouchers, note, and details are deleted then recreated.
- Runtime error rollback is implemented with `Cn.RollbackTrans`.
- A complete business cancellation/reversal path was not found in the inspected `projectsbill.frm` block. Before implementation, search `SendTopost`, approval forms, and any delete/reverse handlers for `project_billl`.

## Migration Risks

- `project_billl.id` and `Notes.NoteID` are manual ids, not identity values.
- The save process mixes header persistence, accounting posting, detail save, advance-payment application, QR generation, and e-invoice hooks in one transaction plus post-commit side effects.
- `billto` party logic must be carefully revalidated because the code sets `X = TXTEnd_user_id` after earlier party selection.
- There are multiple posting branches controlled by `SystemOptions` and UI options. Do not implement posting until these options are mapped from production settings.
