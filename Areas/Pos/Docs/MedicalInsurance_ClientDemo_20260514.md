# Medical Insurance Client Demo - 2026-05-14

## Demo Route

- POS operational view: `/Pos/EmployeePayroll/MedicalInsurance`
- POS reports: `/Pos/EmployeePayroll/MedicalInsuranceReports`
- POS employee lookup: `/Pos/EmployeePayroll/Employees`
- MainErp administration: `/MainErp/EmployeePayroll/MedicalInsurance`

## Demo Data

Default operational database:

- The active Kishny POS database from `KishnyCashConnection`.

Optional demo database:

- `Dania` may be used only for a controlled demo after explicitly setting `PosEmployeePayrollDemoOverrideEnabled=true`.
- `PosEmployeePayrollDatabaseOverride` is empty and ignored by default.
- The POS screen now shows a visible environment/database badge so the presenter and operator know whether the view is live Kishny POS or demo-only.

Scripts:

1. `Areas/MainErp/Sql/06_EmployeePayroll_MedicalInsurance.sql`
2. `Areas/Pos/Sql/84_POS_MedicalInsurance_ProductDemo_Dania.sql`

## Recommended Demo Sequence

1. Open POS Medical Insurance.
2. Show KPI cards:
   - insured employees
   - monthly employee deductions
   - company contribution
   - renewal due
   - overdue installments
3. Search for an employee.
4. Open the employee insurance card visually.
5. Explain provider, plan, family members, overdue flag, and payroll-linked badge.
6. Show the accounting preview:
   - employee deduction
   - company contribution
   - payment to provider
7. Open reports for monthly deductions and subscription coverage.
8. Explain that POS is operational read-only.
9. Open MainErp administration for setup/approval context.
10. Explain protected posting:
    production posting remains blocked until finance approves accounting controls.

## Screenshots Checklist

- POS hero and KPI dashboard.
- Employee insurance cards.
- Accounting preview side panel.
- Renewal/overdue examples.
- POS reports.
- MainErp administration screen.

## Client-Facing Wording

Use:

- “Payroll-linked insurance deduction”
- “Company contribution”
- “Insurance payable”
- “Renewal requires follow-up”
- “Production posting remains financially protected”

Avoid:

- “reconstructed”
- “legacy gap”
- “missing implementation”
- “VB6 branch”

## Remaining Warnings

- Production accounting posting is not enabled.
- Attachment upload and provider network are roadmap items.
- POS is intentionally not a full HR administration surface.
- The demo override is opt-in, disabled by default, and visibly marked when active.

## Selling Message

“This is no longer employee insurance data hidden inside HR. It is a practical medical-insurance operating platform that helps HR, payroll, accounting, and branches understand coverage, deductions, company cost, renewals, and provider payables in one place.”
