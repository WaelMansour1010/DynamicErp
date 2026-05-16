# Medical Insurance Executive Dashboard - 2026-05-14

## Dashboard Widgets

- Insured employees.
- Uninsured employees.
- Renewal due.
- Expired/suspended policies.
- Overdue installments.
- Employee contribution total.
- Company contribution total.
- Provider payable total.
- Branch insurance cost.
- Department insurance cost.
- Alerts center.

## Data Source

The dashboard reads the shared EmployeePayroll medical-insurance repository.

POS EmployeePayroll now uses the active Kishny POS operational database by default:

- `KishnyCashConnection`
- active POS login/session context
- active branch/store/operator context where available

Demo override is disabled by default and requires both keys:

- `PosEmployeePayrollDemoOverrideEnabled=true`
- `PosEmployeePayrollDatabaseOverride=<demo database>`

If the flag is not explicitly enabled, the override value is ignored. This prevents Dania demo data from leaking into real Kishny operation.

## Dashboard Behavior

- Uses server-side aggregation for branch/department cost.
- Shows top cost branches and departments as visual bars.
- Shows coverage percentage in a donut widget.
- Shows protected financial preview instead of production posting.

## QA Result

- Endpoint returns `SchemaReady=true`.
- Endpoint returns membership numbers, dependents, alerts, branch costs, and department costs.
- Browser route loads without JavaScript console errors.
- Build passed.

## Remaining Roadmap

- Trend comparison by month.
- Provider-level payable aging.
- Exportable executive PDF.
- Drill-down report for each KPI.
