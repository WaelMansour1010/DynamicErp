# Employee Payroll Migration Analysis

Date: 2026-05-09

## Scope

The requested VB6 references were reviewed from `F:\Source Code\SatriahMain` using Windows default encoding to avoid touching the original Arabic VB6 files.

- Requested `FrmEmployee`: the source tree exposes the practical employee screen as `Frm\FrmEmployees.frm`; VB6 menu code also references `FrmEmployee` as the logical permission/screen name.
- Requested `FrmEmpSalary5`: reviewed from `Frm\FrmEmpSalary5.frm`.

## FrmEmployees Findings

The simple employee maintenance screen uses `TblEmployee` directly with ADODB table access.

Core fields moved:

- `Emp_ID`
- `Emp_Code`
- `Emp_Name`
- `Emp_Salary`

Useful fields also available in the actual database and moved into the web UI:

- `BranchId`
- `DepartmentID`
- `JobTypeID`
- `BignDateWork`
- `chkStop`
- `Account_code`
- `Account_code1`
- `Emp_Phone`
- `Emp_mobile`
- `Emp_Mail`
- `EmpNotes`

Original save behavior:

- New id is `MAX(Emp_ID)+1` style via VB6 `new_id`.
- Duplicate checks are done against `Emp_Code` and `Emp_Name`.
- Save writes directly to `TblEmployee`.
- Delete exists in the old toolbar, but the web implementation uses activate/deactivate instead of hard delete to reduce risk.

## FrmEmpSalary5 Findings

The payroll screen is much larger than the basic employee screen and contains several tabs:

- salary preparation/allocation
- bank export
- salary payment
- payment entries
- project details
- movement details

Important tables and objects found:

- `emp_salary`
- `TblEmployee`
- `TblBranchesData`
- `TblEmpDepartments`
- `TblEmpJobsTypes`
- `EmpSalaryComponent`
- `mofrad`
- `mofrdat`
- `DOUBLE_ENTREY_VOUCHERS`
- `QryAllEmpAdvance(month, year)`

Important payroll columns:

- `sgn` is built as `Year & MonthNumber`.
- `m_year`, `m_month`
- `payed`
- `Emp_Salary`
- `total1`
- `TotalAdvance`
- `TotalDiscount`
- `ToalInsurance`
- `total2`
- `EmpTotalNet`
- `Comp1..Comp40`
- `BranchId`
- `DepartmentID`

Core calculation observed:

- Gross base comes from salary and salary components.
- Advances are loaded from `QryAllEmpAdvance`.
- Deductions include advances, discounts, insurance, vacation/absence-related values, and visible salary components marked as deductions.
- Net salary is effectively gross/additions minus total deductions.

Accounting behavior observed:

- VB6 creates `DOUBLE_ENTREY_VOUCHERS` records through `ModAccounts.AddNewDev`.
- Salary/accrual account is usually `TblEmployee.Account_code1`.
- Employee receivable/advance account is usually `TblEmployee.Account_code`.
- Some component lines use `mofrad.Account_Code` / `Account_code1`.
- Insurance has existing behavior around `ToalInsurance` and insurance account helpers.

## Medical Insurance Decision

No existing table named `EmployeeMedicalInsurance` or `SalaryRunMedicalInsuranceDeduction` was found in `Eng`.

Decision:

- Add a small `EmployeeMedicalInsurance` table for employee-level setup.
- Add `SalaryRunMedicalInsuranceDeduction` as an audit/detail table for monthly deduction values saved from the web.
- Apply the medical insurance deduction under the same deduction philosophy as VB6: it increases total deductions and reduces `EmpTotalNet`.
- Do not invent a new accounting philosophy. The web journal preview credits the employee receivable account, matching the safe deduction side until final VB6 parity sign-off.

## Exclusions

Not moved in this phase:

- Full 40-component payroll maintenance UI.
- Bank export.
- Crystal Reports printing.
- Hard delete of employees.
- Actual posting into `DOUBLE_ENTREY_VOUCHERS`.

Reason: these parts have broad production accounting impact and require side-by-side parity testing with known payroll periods before enabling write posting.
