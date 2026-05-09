# Employee Payroll Migration Final Report

Date: 2026-05-09

## Completed

- Reviewed VB6 `FrmEmployees.frm` and `FrmEmpSalary5.frm`.
- Reviewed related payroll tables in `Eng`.
- Added shared employee/payroll repository and DTOs.
- Added employee and payroll screens inside POS and MainErp only.
- Added menu links inside POS and MainErp only.
- Added medical insurance database script.
- Added draft payroll calculation with medical insurance deduction.
- Added payroll journal preview following the observed VB6 account direction.

## Database Objects

New script:

- `Areas/MainErp/Sql/06_EmployeePayroll_MedicalInsurance.sql`

Objects:

- `EmployeeMedicalInsurance`
- `SalaryRunMedicalInsuranceDeduction`
- `usp_EmployeePayroll_GetMedicalInsuranceDeduction`

## Preserved Behavior

- Employee base data remains in `TblEmployee`.
- Salary run draft data remains in `emp_salary`.
- Period key follows VB6 `sgn = year + monthNumber`.
- Advances are read through `QryAllEmpAdvance(month, year)`.
- Medical insurance is treated as a deduction and reduces net salary.

## Controlled Differences

- Web employee deletion is replaced by deactivate/reactivate.
- Payroll accounting posting is preview-only in this pass.
- Full component editor and Crystal reports are not moved.
- Bank export is not moved.

## Test Plan

Run on `Eng`:

1. Apply `Areas/MainErp/Sql/06_EmployeePayroll_MedicalInsurance.sql`.
2. Open `/Pos/EmployeePayroll/Employees` and `/MainErp/EmployeePayroll/Employees`.
3. Search employees and filter by branch/department/status.
4. Add an employee with salary and account fields.
5. Edit employee and deactivate/reactivate.
6. Add medical insurance with fixed monthly amount.
7. Preview salary run for the same month and verify medical deduction.
8. Stop insurance or move period outside its dates and verify deduction is gone.
9. Save salary draft and verify `emp_salary.TotalDiscount`, `total2`, `EmpTotalNet`.
10. Compare the journal preview against VB6 logic for a known period before enabling real posting.

## Follow-Up Needed

- Side-by-side comparison against a known VB6 payroll period with approved accounting entries.
- Decide whether medical insurance should credit a dedicated insurance liability account or the employee receivable account in this customer setup.
- Implement actual `DOUBLE_ENTREY_VOUCHERS` posting only after parity sign-off.
