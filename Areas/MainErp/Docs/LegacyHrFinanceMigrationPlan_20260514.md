# Legacy HR/Finance VB6 Migration Plan - MainErp Enterprise

Date: 2026-05-14
Workspace: `F:\Source Code\DynamicErp`

## Executive Summary

This migration must keep two legacy systems separated:

- Main Original VB6 source: `F:\Source Code\SatriahMain` - the general ERP/HR/business source of truth.
- Kishny VB6 source: `F:\Source Code\SatriahMain\Cayshny` - source only for cards, POS, Kishny-specific cases, and payroll runtime/replay compatibility.

Controlling constitution:

- Main Original owns employees, departments, jobs, branches, HR hierarchy, payroll administration workflow, medical insurance workflow, advances, vacations, sick leave, employee allocations, changed components as HR/admin workflow, approvals, HR statuses, attendance-related HR rules, insurance-related employee rules, and employee operational structure.
- Kishny must not be used as a Human Resources business reference unless explicitly instructed.
- Kishny may be used for payroll snapshot/replay mechanics (`emp_salary`, `Comp1..Comp40`, AddNewDev, Notes/voucher linkage) only where compatibility requires it.

The target implementation is `Areas/MainErp` with POS reuse only where explicitly required. The goal is not a VB6 form clone. The goal is a modern enterprise experience that preserves the verified legacy workflow, database structure, permissions, and accounting/payroll behavior.

## Strict Source Matrix

| Screen | Source System | Source File Verified | Target Area | Shared Logic | Isolated Logic | Primary Tables | Risk |
| --- | --- | --- | --- | --- | --- | --- | --- |
| FrmBanksData | Kishny VB6 only | `Cayshny\Frm\FrmBanksData.frm` | `Areas/MainErp`, POS reuse where needed | Branches, accounts, currency lookup | Kishny bank setup, opening balance, approval/loan flags | `BanksData`, `TblBranchesData`, `ACCOUNTS`, `currency` | High: accounting setup |
| FrmBoxesData | Kishny VB6 only | `Cayshny\Frm\FrmBoxesData.frm` | `Areas/MainErp`, POS reuse where needed | Branch/account/employee lookup | Kishny cashbox/POS terminal flags | `tblBoxesData`, `TblBranchesData`, `ACCOUNTS`, `TblEmployee` | High: POS cash operations |
| Employees | Main Original VB6 only | Main Original employee/HR forms under `F:\Source Code\SatriahMain` | `Areas/MainErp` | Existing web `EmployeePayroll` baseline can be reused after Main Original mapping | Main Original employee field behavior, HR structure, validations, department/job/branch hierarchy | `TblEmployee`, `TblEmpDepartments`, `TblEmpJobsTypes`, `TblBranchesData` | High: HR master data |
| Payroll / Salary Run | Main Original for payroll administration workflow; Kishny only for runtime/replay mechanics | Main Original HR/payroll admin forms plus Kishny `FrmEmpSalary5.frm` for snapshot/replay mechanics | `Areas/MainErp` | Existing web `EmployeePayroll` salary run baseline can be reused after ownership split | Main Original owns workflow; Kishny explains `emp_salary`, `Comp1..Comp40`, AddNewDev, Notes/vouchers | `mofrad`, `TblEmployee`, `salary_voucher`, `notes`, `DOUBLE_ENTREY_VOUCHERS`, `emp_salary` | Critical: payroll/accounting |
| MOFRAD | Main Original VB6 only | `Frm\MOFRAD.frm` | `Areas/MainErp` | Payroll component dictionary | Main original component behavior | `mofrad`, `tblUsers`, `ACCOUNTS` | High |
| FrmEmpsAdvanceRequest | Main Original VB6 only | `Frm\FrmEmpsAdvanceRequest.frm` | `Areas/MainErp` | Employee, branch, department, job lookups | Main original advance request workflow, approval/posting | `TblEmpAdvanceRequest`, `TblEmployee`, `TblEmpDepartments`, `TblEmpJobsTypes` | Critical |
| FrmVocationEntitlements | Main Original VB6 only | `Frm\FrmVocationEntitlements.frm` | `Areas/MainErp` | Employee/branch/job/dept lookups | Leave entitlement calculation and payout logic | `TblVocationEntitlements`, `TblEmployee` | High |
| FrmRegsterSickleave | Main Original VB6 only | `Frm\FrmRegsterSickleave.frm` | `Areas/MainErp` | Employee/branch lookup | Sick leave registration and overlap/payroll impact | `TblRegsterSickleave`, `TblEmployee` | High |
| FrmChangedComponentData | Main Original VB6 only | `Frm\FrmChangedComponentData.frm` | `Areas/MainErp` | Component and employee lookup | Variable salary component register | `TblChangedComponentRegister`, `TblChangedComponentRegisterDetails`, `mofrad` | Critical |
| FrmChangedComponentData1 | Main Original VB6 only | `Frm\FrmChangedComponentData1.frm` | `Areas/MainErp` | Accounting notes/vouchers | Employee allocation and accounting posting | `TblEmpAllocations`, `TblEmpAllocationsDetails`, `notes`, `DOUBLE_ENTREY_VOUCHERS` | Critical |

## Verified VB6 Findings

### FrmBanksData - Kishny

Observed logic:

- Loads one bank by `BankID` from `BanksData` and joins `TblBranchesData`.
- `Form_Load` loads currency with `select id,code from currency`.
- List loading is branch-aware: `SELECT * From BanksData where BranchId = Current_branch` when branch isolation is active, otherwise all banks.
- Important fields from query: `BankID`, `BankName`, `BankNamee`, `Remarks`, `Account_Code`, `Account_Code1`, `Account_Code2`, `Account_code3`, `ParetnAccount`, `parent_account`, `report_no`, `BranchId`, `Commision`, `opening_balance_voucher_id`, `OpenBalanceDate`, `OpenBalanceType`, `OpenBalance`, `account_no`, `IBan`, `Branch_NO`, `Tel`, `Address`, `Email`, `Currency_ID`, `chkapprov`, `chkLoan`.

Enterprise redesign:

- Financial Administration dashboard for banks.
- Left searchable bank list, summary header, status/accounting cards, branch/account/currency panels.
- Last movement and usage indicators must be derived from `notes`/`DOUBLE_ENTREY_VOUCHERS` only after verified query mapping.
- Editing must preserve legacy table fields and avoid schema changes.

### FrmBoxesData - Kishny

Observed logic:

- Loads list from `tblBoxesData`, branch-aware using `Current_branch`.
- POS field exists: `IsTerminalPOS`.
- Other important fields: `BoxID`, `BoxName`, `BoxNameE`, `Comments`, `Account_Code`, `Account_Code1`, `Account_Code2`, `ParentAccount`, `parent_account`, `Type`, `empid`, `BranchId`, `ChequeBox`, `opening_balance_voucher_id`, `OpenBalanceDate`, `OpenBalanceType`, `OpenBalance`, `DriverId`, `BTtype`, `boxValue`, `Priod`, `PriodDMY`, `IsWallet` in Kishny.

Enterprise redesign:

- Cashbox/POS terminal administration screen.
- Cards for active cashbox state, terminal status, linked branch/account, employee/custodian, opening balance, limits.
- POS reuse should be read from the same repository/service to avoid duplicate logic.

### Employees - Main Original

Source ownership correction:

- Employee behavior must be traced from Main Original VB6 under `F:\Source Code\SatriahMain`.
- Kishny employee forms are not HR business authority unless explicitly instructed.
- Any Kishny employee/runtime data usage must be documented as payroll compatibility only.

Observed dependency:

- `TblEmployee` has many HR, payroll, identity, insurance, document, branch, department, job, salary, project, and account columns.
- Existing web code already has `EmployeePayrollController` and `Common\EmployeePayroll\EmployeePayrollRepository`, so migration should improve and align it rather than fork it.

Enterprise redesign:

- Employee profile center: search/list, profile summary, job status, branch/department/job, salary, insurance, leave, advances, absences, documents, timeline, quick actions.
- Large employee lookups must be server-side/search based.

### Payroll / Salary Run - Main Original Workflow + Kishny Runtime Compatibility

Discovered candidates:

- Main Original payroll administration workflow is the business authority.
- Kishny `FrmEmpSalary.frm`, `FrmEmpSalary5.frm`, `FrmEmpSalary6.frm`, `FrmEmpSalaryo.frm`: runtime/snapshot/replay mechanics only.
- `FrmEmpSalary3A.frm`: fixed/specific component allocations using `TblSpecificFixed`, `TblSpecificFixedDeti`.
- `FrmEmpSalary4A.frm`: employee operation/location movement logic using `opr_employee_details`, not the primary salary run.

Enterprise redesign:

- Payroll batch processing module with stepper: period, scope, preview, exceptions, approval, posting/export.
- Salary breakdown by employee and component.
- Warnings for missing accounts, missing departments, locked periods, duplicate salary vouchers.

### MOFRAD - Main Original

Observed logic:

- Opens `mofrad` table directly.
- Loads users from `select UserID,UserName From tblUsers`.
- Updates account mapping with `update MOFRAD set Account_code = ...`.
- Fields include type flags: `Absence`, `Late`, `Punch`, `Discount`, `OverTime`, `AddOrDiscount`, `Unit`, `FixedOrChanged`, `ViewComp`, `ZmamAccount`, `Aloc1`, `Aloc2`, `InCrease`, `AdvPaymentdAccount`, `ADVView`, `acc`, `Insurances`, `INSMofrad`, `Reward`, `MofrdAbcen`, `MofrdDiscount`, `Salary`, `AllowIntrod`, `showinMosirVac`, `showMofradAll`, `culc30orRminder`.

Enterprise redesign:

- Payroll component catalog with classification chips, accounting accounts, calculation behavior, visibility toggles, and usage summary.

### FrmEmpsAdvanceRequest - Main Original

Observed logic:

- Uses `TblEmpAdvanceRequest`.
- Checks approval status: `select AdvanceID from TblEmpAdvanceRequest where AdvanceID = ... and AccAproved = 1`.
- Calls posting workflow: `SendTopost Me.Name, "TblEmpAdvanceRequest", "AdvanceID", ...`.
- Important fields: `AdvanceID`, `Branch_NO`, `Emp_id`, `AdvanceValue`, `PaymentCounts`, `FirstDate`, `UserID`, `AdvanceDate`, `DeparmentID`, `gradeID`, `JobTypeID`, `basicSalary`, `discount`, `DiscountDES`, `EmpDue`, `Contractvalid`, `oldAdvance`, `Posted`, `PostedDate`, `NoteSerial`, `Approved`, `Transaction_ID`, `FirstMonthPayment`, `FirstYearPayment`, `AutoDiscount`, `ManagerID`, `jobID_approve`, `ok`, `notok`, `reason`, `DiffVal`, `MethodDeci`, `Balance`, `AccAproved`, `DBIssueDate`.

Enterprise redesign:

- Employee Advance Workflow with request state, repayment schedule, HR notes, finance notes, approval timeline, and payroll deduction preview.

### FrmVocationEntitlements - Main Original

Observed schema:

- `TblVocationEntitlements` stores employee vacation entitlement calculation and payout values.
- Important fields: `RecordDate`, `DateSta`, `EmpID`, `BranchID`, `JobID`, `DeptID`, `BignDate`, `LastVocatinDate`, `ContDay`, `LastDayVoc`, `TotalDay`, `NoDay`, `NoMonth`, `NoYear`, `DaySalary`, `Salary`, `DayIncrease`, `Increase`, `SalaryVocation`, `Other`, `Advance`, `ValueTickt`, `Booked`, `Delivery`.

Enterprise redesign:

- Leave Balance Management with balance cards, earned/used/carried/projected values, warning indicators, employee profile panel.

### FrmRegsterSickleave - Main Original

Observed schema:

- `TblRegsterSickleave` stores sick leave registration.
- Important fields: `ID`, `UserID`, `BranchID`, `EmpID`, `SickID`, `Remarks`, `RecordDate`, `RecordDateH`, `FrmDate`, `FrmDateH`, `ToDate`, `ToDateH`, `LastNoDay`.

Enterprise redesign:

- Sick Leave Workflow with employee context, period overlap detection, medical attachment area if storage exists, HR approval state, payroll impact warning.

### FrmChangedComponentData - Main Original

Observed logic:

- Checks whether component is an absence component through `MOFRAD.MofrdAbcen`.
- Uses `TblChangedComponentRegister` and `TblChangedComponentRegisterDetails`.
- Joins component (`mofrad`), branch, and employee details.
- Important fields: `ChangedComponentid`, `RecordDate`, `year`, `month`, `ComponentID`, `All_Or_SelectedEmployee`, `Actualyear`, `Actualmonth`, `BranchId`, `LocationID`, `KsmID`, `Reason`, `Flag`, selector flags, and details value/hour/day/minute/salary.

Enterprise redesign:

- Compensation Adjustment System with header scope, selected employees, component, before/after impact, reason, audit, approval readiness.

### FrmChangedComponentData1 - Main Original

Observed logic:

- Uses `TblEmpAllocations`, `TblEmpAllocationsDetails`, `notes`, `DOUBLE_ENTREY_VOUCHERS`.
- Validates required type/year/month/employees.
- Checks department accounting completeness: `Account_code1`, `Account_code2`, `Account_code3`.
- Deletes/recreates allocation details and linked notes/vouchers in legacy flow.

Enterprise redesign:

- Employee allocation/accounting adjustment workflow with posting preview, accounting validation, protected rebuild actions, and audit timeline.

## Live Database Snapshot

Inspected connections:

- Kishny source DB: `KishnyCashConnection`, database `Cash`
- MainErp target DB: `MainErp_ConnectionString`, database `TogerTest2026`

Important counts:

| Table | Kishny Cash Count | MainErp Count | Note |
| --- | ---: | ---: | --- |
| `BanksData` | 12 | 0 | Same core columns; MainErp has additional LC account fields |
| `tblBoxesData` | 363 | 0 | Kishny has `IsWallet`, `IsTerminalPOS`; MainErp target currently lacks these columns |
| `TblEmployee` | 331 | 0 | Target empty but schema exists |
| `mofrad` | 40 | 40 | Component dictionary exists in both |
| `TblEmpAdvanceRequest` | 1 | 0 | Workflow table exists |
| `TblVocationEntitlements` | 0 | 0 | Table exists |
| `TblRegsterSickleave` | 0 | 0 | Table exists |
| `TblChangedComponentRegister` | 2990 | 0 | Main target empty |
| `TblChangedComponentRegisterDetails` | 2991 | 0 | Main target empty |
| `notes` | 1,336,415 | 0 | Huge in Kishny; must never full-load |
| `DOUBLE_ENTREY_VOUCHERS` | 7,227,939 | 0 | Huge in Kishny; must never full-load |

## Architecture Proposal

Use existing MainErp patterns:

- Controllers under `Areas/MainErp/Controllers`
- ViewModels under `Areas/MainErp/ViewModels`
- Services under `Areas/MainErp/Services`
- Repositories under `Areas/MainErp/Repositories`
- CSS/JS under `Areas/MainErp/Content` and `Areas/MainErp/Scripts`
- SQL scripts under `Areas/MainErp/Sql`
- Docs under `Areas/MainErp/Docs`

Recommended modules:

- `FinancialAdministrationController`
- `HrWorkflowsController`
- `PayrollComponentsController`
- `CompensationAdjustmentsController`

Recommended repository boundaries:

- Financial admin: banks and boxes with shared lookup/search APIs.
- Employee profile and salary run: extend existing `EmployeePayrollController` and repository after full Kishny mapping.
- Main original HR workflows: separate service/repository to avoid mixing Kishny source behavior.

## Permissions

Use existing `LegacyScreenPermissionService`.

Screen mappings:

- `FrmBanksData`
- `FrmBoxesData`
- Main Original employee screen permission mapping to be confirmed (`FrmEmployee`/equivalent)
- `FrmEmpSalary5` or confirmed salary run form after comparison
- `MOFRAD`
- `FrmEmpsAdvanceRequest`
- `FrmVocationEntitlements`
- `FrmRegsterSickleave`
- `FrmChangedComponentData`
- `FrmChangedComponentData1`

All write actions must check add/edit/delete permissions and log user id where schema supports it.

## Performance Rules

- `notes` and `DOUBLE_ENTREY_VOUCHERS` must be queried by key/date/account with indexes if needed.
- Employee and account lookups must be searchable and paged.
- Bank and box screens can load summary counts but should page lists.
- Salary run preview must be scoped by period/branch/department and cannot load every employee blindly in production data.

## Implementation Phases

### Phase 1 - Analysis and Read-Only Enterprise Shell

- Create this migration plan.
- Build read-only financial administration views for banks and boxes.
- Use server-side paging/search.
- Add summary cards and relation panels.
- No schema changes.

### Phase 2 - Banks/Boxes Save Workflow

- Implement add/edit with duplicate checks by name/code/account where legacy logic confirms.
- Preserve branch/account/currency/opening balance fields.
- For POS-only flags like `IsTerminalPOS`, document whether target schema needs columns before adding them.

### Phase 3 - Employee Profile Modernization

- Trace Main Original employee/HR forms and confirm the permission screen key.
- Extend existing `EmployeePayroll` screen to cover missing fields, profile/timeline, leave/advance/absence summaries.

### Phase 4 - Payroll Batch Processing

- Use Main Original for payroll administration workflow and Kishny salary forms only for runtime snapshot/replay mechanics.
- Add batch state, approval readiness, component breakdown, exception warnings, export/print.

### Phase 5 - Main Original HR Workflows

- Implement MOFRAD component catalog.
- Implement advance request workflow.
- Implement leave entitlements.
- Implement sick leave.
- Implement compensation adjustments and allocation posting preview.

### Phase 6 - Accounting and Audit Hardening

- Verify note/voucher creation against legacy.
- Add audit logging.
- Add protected rebuild operations for legacy delete/reinsert behaviors.

### Phase 7 - QA and Deployment

- Build.
- Test against `MainErp_ConnectionString`.
- Test read-only against Kishny where source validation is needed.
- Document SQL and deployment notes.

## Risks

- Some VB6 captions are non-Unicode/mojibake in source files; field meaning must be confirmed from control names, SQL, and runtime behavior, not captions alone.
- Existing MainErp employee/payroll pages use a shared POS partial for salary run; this should be replaced with MainErp-native partials during modernization.
- MainErp target DB currently has empty master tables for banks, boxes, employees, and salary transactions; functional tests may require seeded or copied data.
- Kishny `tblBoxesData` has POS-specific columns not present in MainErp target (`IsWallet`, `IsTerminalPOS`). Adding them requires a documented schema update.
- Legacy forms sometimes delete/reinsert details and accounting rows. Web implementation must protect this with transaction, audit, and confirmation.
- Large legacy tables require strict indexed queries and paging.

## Quick Wins

- Build a read-only Bank/Cashbox Administration dashboard first.
- Add account/branch/currency lookup APIs once and reuse them.
- Add component catalog for `mofrad` as a clean read-only/manageable screen.
- Replace MainErp salary run POS partial with a native MainErp partial after salary-form comparison.

## Initial Technical Debt Found

- MainErp `EmployeePayroll/SalaryRun.cshtml` currently references POS view/CSS/JS partials.
- Some existing Arabic strings in controllers/views are mojibake, likely due historical encoding.
- Current employee/payroll implementation exists but source mapping to Kishny VB6 is not documented.
- MainErp target database has schemas but little/no operational data, so QA needs a controlled seed plan.
