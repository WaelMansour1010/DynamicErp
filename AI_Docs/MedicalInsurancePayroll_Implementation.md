# Medical Insurance Payroll Implementation

## Implemented Files

Core:

- `Common/EmployeePayroll/EmployeePayrollModels.cs`
- `Common/EmployeePayroll/EmployeePayrollRepository.cs`

POS:

- `Areas/Pos/Controllers/EmployeePayrollController.cs`
- `Areas/Pos/Views/EmployeePayroll/MedicalInsurance.cshtml`
- `Areas/Pos/Views/EmployeePayroll/MedicalInsuranceReports.cshtml`
- `Areas/Pos/Views/EmployeePayroll/_MedicalInsuranceSettings.cshtml`
- `Areas/Pos/Views/EmployeePayroll/_MedicalInsuranceReports.cshtml`
- `Areas/Pos/Views/EmployeePayroll/_EmployeePayrollEmployees.cshtml`
- `Areas/Pos/Views/EmployeePayroll/_EmployeePayrollSalaryRun.cshtml`
- `Areas/Pos/Scripts/employee-payroll.js`
- `Areas/Pos/Content/employee-payroll.css`
- `Areas/Pos/Controllers/PosDashboardController.cs`
- `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`

MainErp:

- `Areas/MainErp/Controllers/EmployeePayrollController.cs`
- `Areas/MainErp/Views/EmployeePayroll/MedicalInsurance.cshtml`
- `Areas/MainErp/Views/EmployeePayroll/MedicalInsuranceReports.cshtml`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`

SQL:

- `Areas/MainErp/Sql/06_EmployeePayroll_MedicalInsurance.sql`
- `Areas/Pos/Sql/52_POS_EmployeePayroll_MedicalInsurance.sql` references the canonical script.

Project:

- `MyERP.csproj`

## Features

Settings:

- Provider list/save.
- Plan list/save.
- Default monthly cost.
- Employee share type/value.
- Company share type/value, including `AutoBalance`.
- Optional account fields for later accounting activation.

Employee:

- Medical insurance tab added.
- Save current subscription.
- Stop/update existing subscription.
- Show subscription history.
- Plan defaults can be loaded into the employee subscription.

Payroll:

- Preview includes medical plan, monthly cost, employee deduction, company cost.
- Summary includes total employee medical deduction and total company medical cost.
- Save payroll draft updates `emp_salary.TotalDiscount` with employee deduction only.
- Medical deduction audit is upserted by employee/year/month, preventing duplicate deduction rows.

Reports:

- Active/subscription report.
- Saved payroll deduction report.
- Employee deduction totals.
- Company cost totals.

## Accounting

The employee deduction appears in the payroll journal preview as a credit in the same practical direction used for employee-linked deductions. Company cost is saved and reported but not posted because the VB6 salary screen does not provide an unambiguous medical company-cost posting account. Optional plan account fields were added for a later controlled activation.

## Security And Routing

All endpoints are under:

- `/Pos/EmployeePayroll/...`
- `/MainErp/EmployeePayroll/...`

POS shell entries are under `PosDashboard` only. No root public module route or root menu entry was added.

## SQL Compatibility

The script uses SQL Server 2012-compatible syntax and uses `IF OBJECT_ID` / `COL_LENGTH` guards plus `DROP PROCEDURE` then `CREATE PROCEDURE`.
