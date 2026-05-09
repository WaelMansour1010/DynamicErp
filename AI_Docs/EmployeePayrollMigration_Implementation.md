# Employee Payroll Migration Implementation

Date: 2026-05-09

## Implemented Files

Shared code:

- `Common/EmployeePayroll/EmployeePayrollModels.cs`
- `Common/EmployeePayroll/EmployeePayrollRepository.cs`

POS:

- `Areas/Pos/Controllers/EmployeePayrollController.cs`
- `Areas/Pos/Views/EmployeePayroll/Employees.cshtml`
- `Areas/Pos/Views/EmployeePayroll/SalaryRun.cshtml`
- `Areas/Pos/Views/EmployeePayroll/_EmployeePayrollEmployees.cshtml`
- `Areas/Pos/Views/EmployeePayroll/_EmployeePayrollSalaryRun.cshtml`
- `Areas/Pos/Content/employee-payroll.css`
- `Areas/Pos/Scripts/employee-payroll.js`
- Menu link in `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`
- Shell routes in `Areas/Pos/Controllers/PosDashboardController.cs`

MainErp:

- `Areas/MainErp/Controllers/EmployeePayrollController.cs`
- `Areas/MainErp/Views/EmployeePayroll/Employees.cshtml`
- `Areas/MainErp/Views/EmployeePayroll/SalaryRun.cshtml`
- Menu links in `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`

SQL:

- `Areas/MainErp/Sql/06_EmployeePayroll_MedicalInsurance.sql`
- `Areas/Pos/Sql/52_POS_EmployeePayroll_MedicalInsurance.sql`

## Employee Screen

Features:

- Search by code/name/mobile.
- Filter by branch, department, active state.
- Add/edit employee core data.
- Activate/deactivate through `TblEmployee.chkStop`.
- Salary/account/contact fields.
- Medical insurance tab.

The screen is reachable only through:

- `/Pos/EmployeePayroll/Employees`
- `/MainErp/EmployeePayroll/Employees`

No public root web controller or main website menu was added.

## Payroll Screen

Features:

- Year/month selection.
- Branch/department/employee filtering.
- Preview rows from `TblEmployee` and existing `emp_salary` drafts.
- Advance deduction loaded from `QryAllEmpAdvance(month, year)` when available.
- Medical insurance automatically calculated for active monthly subscriptions overlapping the payroll month.
- Summary totals: base, additions, deductions, medical insurance, net.
- Journal preview based on the VB6 account philosophy.
- Save draft rows into `emp_salary` when `payed` is not approved.

## Accounting Safety

Actual posting into `DOUBLE_ENTREY_VOUCHERS` is intentionally kept as a documented skeleton/preview, not a write operation. The VB6 code has multiple posting branches for components, projects, departments, advances, vacation and insurance. A direct automated posting without a known reference period would create real accounting risk.

## Medical Insurance

Supported setup:

- amount or percentage
- start/end dates
- monthly flag
- active flag
- notes

Payroll application:

- applies only when active, monthly, and date range overlaps the payroll month
- appears in totals as medical insurance
- is included in total deductions and therefore lowers net salary
- is audited in `SalaryRunMedicalInsuranceDeduction`

## SQL Server Compatibility

The SQL script is SQL Server 2012 compatible and uses `DROP PROCEDURE` then `CREATE PROCEDURE` for the procedure. Tables are created with `IF OBJECT_ID IS NULL` to avoid destructive changes.
