# Project Extracts Live Schema Mapping

Scope: مستخلصات المشاريع / Project Extracts. This document is based on live SQL Server inspection performed against `Wael\Sql2019`, representative catalog `Eng`.

## Database Selection

`MyErp` is the current DynamicErp web database, but it contains only `transactionsVatDetails` from the requested table list. Full legacy coverage exists in several local databases; `Eng` was used as the representative Main ERP schema specimen.

## Live Row Counts in `Eng`

| Table | Rows |
| --- | ---: |
| `project_billl` | 3373 |
| `project_bill_details` | 3371 |
| `projects` | 44 |
| `Notes` | 74420 |
| `DOUBLE_ENTREY_VOUCHERS` | 553924 |
| `transactionsVatDetails` | 170 |
| `TblPayPrePayed` | 0 |
| `TblProjePayPrePayed` | 0 |
| `TblCustemers` | 2668 |
| `tblActivitesType` | 1 |
| `TblBranchesData` | 1 |

## `project_billl`

Primary key: `PK_project_billl` on `id`.

Identity: `id` is not identity. VB6 assigns it with `new_id("project_billl", "id", "", True)`.

Important columns:

| Column | Type | Null | Role |
| --- | --- | --- | --- |
| `id` | `int` | no | extract/bill id |
| `bill_date`, `dueDate`, `StartDate`, `StartDateProje`, `PostedDate`, `DateRec`, `RecTime` | date/time types | yes | document, period, posting, and e-invoice dates |
| `project_no`, `project_name` | `nvarchar` | yes | project reference and copied name |
| `End_user_name`, `Sub_user_name`, `End_user_account`, `Sub_user_account` | `nvarchar` | yes | customer/subcontractor display and account fields |
| `bill_to`, `bill_type`, `subContractorId` | `nvarchar/int` | yes | document direction and party |
| `revenue_account`, `AccountUnderImp` | `nvarchar` | yes | revenue / under-implementation account |
| `note_id`, `NoteSerial`, `NoteSerial1` | `int`/`varchar`/`nvarchar` | yes | generated note and serial linkage |
| `total`, `Results`, `NetValue`, `TotalValue`, `TotalBefore` | money/float | yes | total and net values |
| `discount`, `discount1ID`, `discount2ID`, `discount1value`, `discount2value`, `DiscountGMater`, `DiscountAccount`, `Discount3`, `Discount4` | numeric/account fields | yes | discount and deduction handling |
| `advancedPayment`, `PerformanceBond`, `PerforValue`, `BondAmt` | numeric | yes | advance payment and retention/performance handling |
| `FATYou`, `FATValue`, `AccountCodeVat`, `PreVAT` | numeric/account fields | yes | VAT rate/value/account and advance-payment VAT |
| `PreBalaValue`, `PreBalaVAT`, `PreBalaTotal`, `PreBalaPayed`, `PreBalaRemain`, `PreBalaTransPyed`, `PreBalaNet`, `PreBalaVATYu` | numeric | yes | previous balance/prepayment fields |
| `SumVATLine`, `SumValueLine` | numeric | yes | aggregate line VAT/value |
| `Branch_NO`, `UserID`, `Approved`, `Posted` | int/bit | yes | branch, audit, approval/posting state |
| `Currency_id`, `Currency_rate` | int/float | yes | currency |
| `QrCodeData`, `QrCodeDataPath`, `QrCodeImage` | text/image fields | yes | QR/e-invoice artifacts |
| `zatcaStatus`, `ErrorMessageS`, `warrningmessage`, `TableName`, e-invoice fields | mixed | yes | e-invoice integration metadata |

No direct FK was returned from `project_billl` to `projects`, `Notes`, or customers. The VB6 code stores relationships by ids and account codes.

## `project_bill_details`

Primary key: returned through FK/index metadata as detail identity on `id`.

Identity: `id` is identity.

Foreign key: `FK_project_bill_details_project_billl` on `bill_id -> project_billl.id`.

Important columns:

| Column | Type | Role |
| --- | --- | --- |
| `bill_id` | `int` | parent extract id |
| `project_no`, `project_id`, `projectName`, `FullCode` | mixed | project references |
| `item`, `item_id`, `Unit_id`, `item_unit` | mixed | line item/work item |
| `Quantity`, `Price`, `cost`, `exe`, `percentage`, `exedate` | numeric/date | contract/execution quantities and rates |
| `Pre_Quantity`, `Pre_Value`, `Pre_Percent` | numeric | previous quantity/value/percent |
| `Curr_Quantity`, `Curr_value`, `curr_Percent` | numeric | current period execution |
| `tot_quantity`, `tot_value`, `tot_percent` | numeric | cumulative execution |
| `qty`, `total`, `discount`, `net`, `quntExc`, `totEx`, `discountEXE`, `NetExe` | numeric | line totals and executed net |
| `LineDiscountPercent`, `LineDiscount`, `linenetaftermainDiscount`, `linenetaftermainDiscountBeforevat`, `LineVat`, `linenetaftermainDiscountWithvat`, `PerforVLineDiscount`, `LineFinal` | numeric | line allocation of discount, VAT, performance retention |
| `QtyApprov`, `TotalApprov`, `PriceApprov`, `DiscApprov`, `NetApprov` | numeric | approval fields |
| `qtySubContractor`, `costSubContractor` | numeric | subcontractor fields |
| `OLDTotalwithVat`, `CurrenttotalWithvat`, `Totalwitvat`, `oldPerforValue`, `totalPerforValue` | numeric | previous/current VAT and retention totals |
| `AccountCode` | text | line account linkage |

## `projects`

Primary key: `PK_projects` on `id`.

Identity: `id` is not identity.

Important fields: `Project_name`, `Project_nameE`, `Fullcode`, `project_code`, `End_user_id`, `sub_contractor_id`, `branch_no`, `branche_ID`, `UserID`, `StartDate`, `Pstate`, `End_user_Account`, `sub_contractor_Account`, `expanses_account`, `REVENUE_account`, `Project_account`, `Material_account`, `Salary_account`, `AccountUnderImp`, `AcountGood`, `opening_balance_voucher_id`, opening-balance fields, `NoteSerial`, `NoteId`, project totals and balances.

## `DOUBLE_ENTREY_VOUCHERS`

Primary key: composite `PK_DOUBLE_ENTREY_VOUCHERS` on `Double_Entry_Vouchers_ID`, `DEV_ID_Line_No`.

Foreign keys: `Account_Code -> ACCOUNTS.Account_Code`, `Notes_ID -> Notes.NoteID`.

Important columns: `Double_Entry_Vouchers_ID`, `DEV_ID_Line_No`, `Account_Code`, `Value`, `Credit_Or_Debit`, `Double_Entry_Vouchers_Description`, `Double_Entry_Vouchers_Descriptione`, `RecordDate`, `Notes_ID`, `Transaction_ID`, `AdvanceID`, `UserID`, `Posted`, `PostedDate`, `Account_Interval_ID`, `project_bill_no`, `project_id`, `bill_id`, `branch_id`, `Vat`, `Vatyo`, `AccountCode2`, `FlgVat`, `TotalValue`, `VATYou`, `DueDate`, `IsHiddenInv`, `IsExpens`.

Debit/credit convention from VB6: `Credit_Or_Debit=0` means debit, `1` means credit.

## `TblPayPrePayed`

Primary key: `PK_TblPayPrePayed` on `ID`.

Identity: `ID` is identity.

Fields: `NoteID`, `NoteID1`, `Note_Value`, `NoteSerial1`, `PayedValue`, `too`, `NoteDate`, `TransPayedValue`, `RemainingValue`, `NetValue`, `branch_no`, `VAT`, `Valu`, `VATLine`, `ValueLine`, `TypeTrans`, `Account_code`, `NCashingType`.

Purpose in VB6: stores selected advance/prepaid note deductions for an extract (`NoteID1 = project_billl.id`).

## `TblProjePayPrePayed`

Primary key: `PK_TblProjePayPrePayed` on `ID`.

Identity: `ID` is identity.

Fields: `NoteID`, `Transaction_ID`, `Note_Value`, `PayedValue`, `TypeTrans`, `NCashingType`.

Purpose in VB6: stores project-extract to source-note payment application rows (`NoteID = project_billl.id`, `Transaction_ID = source Notes.NoteID`).

## `transactionsVatDetails`

Primary key: `PK_transactionsVatDetails` on `ID`.

Identity: `ID` is identity.

Fields: `SingedXML`, `EncodedInvoice`, `InvoiceHash`, `UUID`, `QRCode`, `PIH`, `SingedXMLFileName`, `QrCodeData`, `QrCodeDataPath`, `QrCodeImage`, `IsDeleted`, `Transaction_ID`, `Doctype`, `TableName`, `uuidCounter`.

Purpose: e-invoice/ZATCA VAT artifact storage. It must not be written until invoice-generation behavior is explicitly designed.

## `TblCustemers`

Primary key: `PK_TblCustemers` on `CusID`.

Identity: `CusID` is not identity.

Important linkage fields: `CusName`, `CusNamee`, `Type`, `OpenBalance`, `Account_Code`, `Account_Code_As_Client`, `Account_Code_As_Supplier`, `Account_CodeHi1`, `Account_CodeHi2`, `Account_CodeAss1`, `Account_CodeAss2`, `Account_VAT`, `parent_account`, `opening_balance_voucher_id`, `BranchId`, `VATNO`, `CurrncyID`, address/e-invoice fields.

## `tblActivitesType` and `TblBranchesData`

`tblActivitesType`: primary key `id`; holds company/activity and VAT/e-invoice metadata.

`TblBranchesData`: primary key `branch_id`; holds branch code/name/account fields, VAT/e-invoice company data, `StoreId`, `ApplyEinvoice`, `ApplyEinvoiceWithBranch`.

## Migration Notes

The target production database must be confirmed before implementation. The web DB `MyErp` cannot be used to infer Main ERP project extract schema.
