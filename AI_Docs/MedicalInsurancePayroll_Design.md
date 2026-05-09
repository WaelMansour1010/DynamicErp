# Medical Insurance Payroll Design

## Scope

The medical insurance module is implemented only inside:

- `Areas/Pos`
- `Areas/MainErp`

No public/root web controller or menu was added for this module.

## VB6 Payroll Fit

`FrmEmpSalary5.frm` calculates salary rows by period using `m_year`, `m_month`, and `sgn`, then deducts advances, saved discounts, and insurance-related amounts before calculating net salary. The web module keeps that philosophy:

- Employee share of medical insurance is an additional payroll deduction.
- Company share is information/audit cost and is not deducted from employee net salary.
- Journal preview adds only the employee deduction under the same deduction direction used for employee-linked deductions.
- Company cost is not posted automatically until a safe company expense/liability account mapping is confirmed.

## Tables

New/extended SQL Server 2012-compatible objects are in:

- `Areas/MainErp/Sql/06_EmployeePayroll_MedicalInsurance.sql`

Objects:

- `MedicalInsuranceProviders`
- `MedicalInsurancePlans`
- `EmployeeMedicalInsurance` extended with plan, monthly cost, employee share, company share, calculated employee deduction, calculated company cost, update metadata.
- `PayrollMedicalInsuranceDeduction`
- `SalaryRunMedicalInsuranceDeduction` retained as compatibility audit table and extended with company/monthly cost.
- `usp_EmployeePayroll_GetMedicalInsuranceDeduction`

## Calculation Rules

`MonthlyCost` is the plan/subscription monthly cost.

Employee share:

- `Amount`: `EmployeeDeduction = EmployeeShareValue`
- `Percent`: `EmployeeDeduction = MonthlyCost * EmployeeShareValue / 100`

Company share:

- `Amount`: `CompanyCost = CompanyShareValue`
- `Percent`: `CompanyCost = MonthlyCost * CompanyShareValue / 100`
- `AutoBalance`: `CompanyCost = MonthlyCost - EmployeeDeduction`

Guards:

- Negative shares are clamped to zero in runtime calculation.
- Employee deduction is capped at monthly cost.
- If employee + company exceeds monthly cost, company cost is reduced to the remaining balance.

## Employee Link

The employee screen has a medical insurance tab:

- Plan
- Start/end date
- Active/stopped
- Monthly cost
- Employee share
- Company share
- Calculated employee monthly deduction
- Calculated company monthly cost
- Subscription history

History is stopped/deactivated rather than hard-deleted.

## Payroll Link

Payroll preview checks active monthly employee subscriptions where the payroll month overlaps the subscription start/end date. It shows:

- Medical insurance plan name
- Monthly cost
- Employee deduction
- Company cost
- Net salary

Net salary uses employee deduction only.
