# Legacy HR/Finance Production Hardening

Date: 2026-05-14  
Database: `Dania`  
Server: existing MainErp SQL Server instance `Wael\Sql2019`

## Status

The migration is now in production hardening, not feature migration. Banks/boxes and HR finance read workflows remain usable, but payroll posting is still blocked until full VB6 parity is proven.

## Changes Completed

### Account Lookup Scalability

- Removed the static account payload from the financial administration index model.
- Added `FinancialAdministration/AccountLookup`.
- Added debounced async lookup in `financial-administration.js`.
- Added keyboard support: up/down, enter, escape.
- Result limit is capped at 50 server-side, default 20.
- Tested against `Dania` profile where `ACCOUNTS` has 16,018 rows.

### Permission Hardening

- Added `Areas/MainErp/Sql/11_LegacyHrFinance_Dania_Permission_QA_Users.sql`.
- Script creates stable QA users:
  - `QA_HRFIN_VIEW`
  - `QA_HRFIN_ADD`
  - `QA_HRFIN_EDIT`
  - `QA_HRFIN_DENIED`
- Script inserts explicit `ScreenJuncUser` rows for:
  - Main Original source: employees, HR workflows, payroll administration workflow, medical insurance workflow, `MOFRAD`, `FrmEmpsAdvanceRequest`, `FrmVocationEntitlements`, `FrmRegsterSickleave`, `FrmChangedComponentData`, `FrmChangedComponentData1`
  - Kishny source: cards, POS/Kishny-specific cases, and payroll runtime/replay mechanics only (`FrmEmpSalary5`, `emp_salary`, `Comp1..Comp40`, AddNewDev, Notes/voucher linkage)
- Executed on `Dania`; 40 permission rows were created/updated.
- Employee save now requires add/edit permission on `FrmEmployee`.
- Employee active-state change now requires edit permission on `FrmEmployee`.
- Salary run save now requires add/edit permission on `FrmEmpSalary5`.

### Payroll Parity Evidence

- Added `Areas/MainErp/Sql/12_LegacyHrFinance_Dania_PayrollParity_Check.sql`.
- The script is read-only and SQL Server 2012 compatible.
- Period checked: `emp_salary.sgn = 20264`.

Result:

| Metric | Value |
| --- | ---: |
| Employees compared | 436 |
| Matched employees | 0 |
| Mismatched employees | 436 |
| Legacy net total | 1,578,491.00 |
| Current web-basis net total | -53,163.00 |
| Total net difference | 1,631,654.00 |
| `notes` rows in database | 80,331 |
| `DOUBLE_ENTREY_VOUCHERS` rows in database | 266,557 |

Conclusion: payroll posting must remain blocked. Current web calculation cannot be approved because historical `emp_salary` behavior is not reproduced from the employee master salary fields.

## Remaining Production Blocks

- Full employee-by-employee VB6 payroll output comparison is still required.
- `notes` parity is not complete.
- `DOUBLE_ENTREY_VOUCHERS` parity is not complete.
- `SendTopost` parity is not complete.
- Allocation rebuild parity is not complete.
- Payroll posting parity is not complete.

## QA Checklist

| Check | Result | Notes |
| --- | --- | --- |
| Account lookup avoids full 16k account payload | PASS | Static `Model.Accounts` payload removed from financial administration page. |
| Async account search endpoint exists | PASS | `FinancialAdministration/AccountLookup`. |
| Permission QA users seeded on `Dania` | PASS | Four users created/updated with explicit screen permissions. |
| Employee save direct endpoint requires save permission | PASS | Controller now checks `FrmEmployee` add/edit. |
| Salary save direct endpoint requires save permission | PASS | Controller now checks `FrmEmpSalary5` add/edit. |
| Payroll parity for `20264` | FAIL | 436/436 mismatched; posting remains blocked. |
| Accounting posting parity | BLOCKED | Requires VB6 notes/voucher comparison. |

## Customer Delivery Rule

Read-only payroll/accounting workflows stay protected until the parity scripts show matching values and the VB6 posting outputs are verified for the same period, users, branches, components, allocations, notes, and `DOUBLE_ENTREY_VOUCHERS`.
