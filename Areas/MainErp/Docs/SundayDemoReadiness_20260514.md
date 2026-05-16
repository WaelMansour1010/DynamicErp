# Sunday Demo Readiness - HR + Medical Insurance

Date: 2026-05-14  
Target database: Dania on the configured MainErp SQL Server environment  
Scope: Executive/admin demo readiness only. Payroll posting, SendTopost replacement, allocation rebuild, Notes creation, and DOUBLE_ENTREY_VOUCHERS creation remain intentionally protected.

## Demo Routes

- `/MainErp/EmployeePayroll/Employees`
- `/MainErp/EmployeePayroll/SalaryRun`
- `/MainErp/EmployeePayroll/MedicalInsurance`
- `/MainErp/EmployeePayroll/MedicalInsuranceReports`
- `/MainErp/LegacyHrFinance/Components`
- `/MainErp/LegacyHrFinance/PayrollParity`

## Demo Users

- Primary presenter: existing MainErp admin user with EmployeePayroll access.
- Permission demo users: use the previously prepared QA permission users from `Areas/MainErp/Sql/11_LegacyHrFinance_Dania_Permission_QA_Users.sql` when route/action edge-case validation is required.
- Do not use a payroll posting-capable role during the executive demo. The demo message should emphasize financial protection, not disabled capability.

## Demo Data Selection

Dania currently contains realistic payroll and HR data suitable for demo selection:

- Employees: 880 rows in `TblEmployee`.
- Payroll snapshots: 10,908 rows in `emp_salary`.
- Medical insurance runtime rows: 38 rows in `TBLInsurancesJoin`.
- Recommended payroll preview period: year `2026`, month `3`, because it has 452 snapshot rows and realistic totals.
- Avoid the year `2099` demo/test row unless specifically explaining data-quality filtering.

No destructive seed was applied for this demo pass. The recommended demo approach is to filter to clean branches/departments and use real Dania employees so joins, paging, salary snapshots, and medical insurance state are demonstrated against live data.

## Demo Sequence

1. Open Employee Management.
2. Show the executive KPI row: employee count, insurance visibility, review cases, and branch spread.
3. Search by employee name/code and open the profile summary.
4. Open the employee editor and show personal/job/salary/insurance tabs.
5. Show insurance state chips: insured, pending, inactive/excluded, payroll-linked.
6. Navigate to Medical Insurance and show providers/plans/reports.
7. Open Salary Run for `2026 / 3`.
8. Run preview and highlight totals, component breakdown, insurance impact, and advance/deduction columns.
9. Explain `LegacySnapshot` vs `Reconstructed` badges.
10. Click parity diagnostics and accounting replay.
11. Expand one component explainability drawer and show source, precedence, proration, insurance impact, and rule path.
12. Close with the financial safety banner: posting is protected until accounting parity is complete.

## Protected Workflows

The following remain intentionally blocked for reconstructed rows:

- Payroll posting.
- Salary payment posting.
- SendTopost replacement.
- Allocation rebuild.
- Notes creation.
- DOUBLE_ENTREY_VOUCHERS creation.

Recommended executive wording:

> The new platform can preview and explain payroll safely, but it will not create accounting impact until component parity and accounting replay match the legacy VB6 behavior. This is financial protection by design.

## What Changed For Demo Readiness

- Employee screen was polished as an enterprise HR command center.
- Employee list now has demo-friendly loading and empty states.
- Employee profile summary now highlights branch, department, salary, and medical insurance state.
- Salary Run screen now has executive KPIs, compatibility badges, safety messaging, parity diagnostics, replay diagnostics, and explainability access.
- Protected payroll save action now shows professional safety messaging instead of attempting unsafe posting.
- Explainability drawer is exposed as a product feature, not hidden technical output.

## Screenshots Checklist

- Employee dashboard with KPI cards.
- Employee search results with insurance state chips.
- Employee profile summary and medical insurance tab.
- Medical insurance provider/plan overview.
- Salary preview with totals.
- LegacySnapshot and Reconstructed badges.
- Parity diagnostics panel.
- Accounting replay panel.
- Explainability drawer.
- Protected posting safety banner.

## QA Checklist

- Employee route opens: Pending browser verification after local IIS/IIS Express session.
- SalaryRun route opens: Pending browser verification after local IIS/IIS Express session.
- JavaScript syntax check: Passed with `node --check`.
- Arabic source encoding: Verified with UTF-8 file read.
- Dania data availability: Passed, real employee/payroll/insurance rows found.
- Payroll posting remains blocked: Passed in UI; save action now presents safety message only.
- Reconstructed rows remain protected: Preserved by existing compatibility safety rules.
- No POS visual dependency introduced: Passed by using MainErp-scoped views and `employee-payroll.css`.

## Known Limitations For Sunday

- Payroll/accounting parity is still under compatibility expansion. This must be described as an active financial protection feature.
- Insurance semantics are snapshot/state-driven from legacy runtime data. The demo should show explainability, not claim final posting readiness.
- Permission QA should be repeated with non-admin QA users before customer-facing sign-off.
- Full browser console verification depends on launching the local MainErp runtime under the configured IIS/IIS Express profile.
