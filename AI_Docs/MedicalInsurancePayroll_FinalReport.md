# Medical Insurance Payroll Final Report

## Completed

- Added a full medical insurance module for POS and MainErp only.
- Added providers and plans setup.
- Added employee subscriptions with start/end date, active flag, monthly recurring flag, employee share, company share, and calculated monthly amounts.
- Added employee insurance history without deleting previous subscriptions.
- Integrated employee medical deduction into salary run preview and draft save.
- Saved payroll medical deduction details with a unique employee/year/month row to prevent duplicate deductions.
- Added medical insurance reports for subscriptions and saved payroll deductions.
- Updated POS and MainErp menus only.
- Applied the SQL script to `Eng`.
- Applied and tested the SQL/module flow on `Cash` for the Kishny scenario.
- Updated project file and documentation.

## Calculation Result

For a monthly cost of `1000`:

- Employee fixed amount `200` gives employee deduction `200`, company cost `800`.
- Employee percent `25%` gives employee deduction `250`, company cost `750` when company share is `AutoBalance`.

Only employee deduction reduces net salary.

## Payroll And Journal

The payroll save keeps the old salary-run shape by adding employee medical deduction into `emp_salary.TotalDiscount`. The journal preview adds medical insurance as an employee deduction line. Company cost is not posted automatically yet; it is persisted in insurance audit/report tables.

## Tests On Eng

Executed:

- SQL migration applied successfully.
- Verified tables and new columns exist.
- Transactional rollback test created a provider, two plans, an employee subscription, executed `usp_EmployeePayroll_GetMedicalInsuranceDeduction`, inserted/upserted one payroll medical deduction row, and rolled back.
- Verified fixed share and percent share calculations.
- Verified unique payroll medical row remains one row within the transaction.
- `MSBuild` succeeded after implementation; existing project warnings remain unrelated.

## Full Cycle Test On Cash

Executed on 2026-05-09 and documented in `AI_Docs/EmployeePayrollFullCycle_TestReport_20260509.md`:

- Created provider `ProviderId = 3`.
- Created plan `PlanId = 3`, monthly cost `1000`, employee share `200`, company share `800`.
- Created employee `Emp_ID = 333`, code `T0509171715`.
- Created the six VB6-style employee linked accounts automatically.
- Saved July 2026 payroll draft with `TotalDiscount = 200` and `EmpTotalNet = 5000`.
- Re-saved/recalculated the same payroll period; deduction stayed `200`, proving no duplicate insurance deduction.
- Stopped insurance and previewed August 2026; medical deduction became `0`.

## Deferred

- Actual company medical-cost accounting posting is deferred until an explicit safe account mapping and VB6 parity decision are confirmed.
- Payroll approval/final posting remains guarded by the existing payroll migration stance; draft save and journal preview are implemented.

## URLs

- `/Pos/EmployeePayroll/MedicalInsurance`
- `/Pos/EmployeePayroll/MedicalInsuranceReports`
- `/MainErp/EmployeePayroll/MedicalInsurance`
- `/MainErp/EmployeePayroll/MedicalInsuranceReports`
