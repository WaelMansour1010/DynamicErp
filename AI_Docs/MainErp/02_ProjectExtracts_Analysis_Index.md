# Project Extracts Analysis Index

## Prior Analysis Found

- `F:\Source Code\SatriahMain\AI_Docs\Screens\ProjectsBill_Analysis_For_DotNet_Migration.md`
- `F:\Source Code\SatriahMain\AI_Docs\ZATCA_Upload_Web_Project_Analysis.md`

## Main Legacy VB6 Files Found

- `F:\Source Code\SatriahMain\Frm\New frm\projectsbill.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\projectsbill_search.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\ProjectsBillAlarm1.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\ProjectsBillAlarm1X.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\ProjectsBillselect.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\projects\projects.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\projects\FrmProjectMonthBill.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\FrmProjectSearch.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\frmProjectsReports.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\frmSubcontractorContractl.frm`

`Account.vbp` references the active `New frm` project bill, project, report, and contractor forms.

## Legacy Tables And Accounting Objects Mentioned

- `project_billl`
- `project_bill_details`
- `projects`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- `TblCustemers`
- `TblBranchesData`
- `tblActivitesType`
- `TblPayPrePayed`
- `TblProjePayPrePayed`
- `transactionsVatDetails`

## Verified Behavior Summary

- `projectsbill.frm` is the project progress invoice/extract screen.
- Header data is stored in `project_billl`.
- Detail lines are stored in `project_bill_details`.
- Accounting note linkage uses `Notes.NoteID` and `project_billl.note_id`.
- Accounting postings are written to `DOUBLE_ENTREY_VOUCHERS`.
- The screen handles progress, previous quantities, VAT, retentions/performance deductions, advance payment deductions, and ZATCA/e-invoice fields.

## AllScripts.sql Status

Searched `F:\Source Code\SatriahMain\Main Script\AllScripts.sql` for `project_billl`, `project_bill_details`, `projects`, `Notes`, and `DOUBLE_ENTREY_VOUCHERS`. The script contains accounting/reporting procedures and views referencing `projects`, `Notes`, and double-entry voucher tables, but no direct `project_billl` or `project_bill_details` table definition was found in the current file.

## Gaps Before Implementation

- Confirm live schema for `project_billl` and `project_bill_details`.
- Confirm active source version of `projectsbill.frm` and related search/report forms.
- Map each deduction and retention field to accounting entries.
- Confirm report inventory and Crystal report migration path.
- Confirm ZATCA fields that are required in the Main ERP migration and what belongs outside phase 1.
