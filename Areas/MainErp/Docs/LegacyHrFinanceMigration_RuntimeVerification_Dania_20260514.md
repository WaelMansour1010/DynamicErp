# Legacy HR/Finance Runtime Verification - Dania

Date: 2026-05-14  
Database: `Dania`  
Server/environment: existing MainErp configured SQL Server instance `Wael\Sql2019`  
Target: `Areas/MainErp`

## Purpose

This report covers the second verification phase requested after the first closure pass. It validates the migrated Legacy HR/Finance screens against a realistic, populated MainErp database instead of empty route checks.

## Runtime Fixes Made During Verification

| Finding | Fix |
| --- | --- |
| `EmployeePayrollController` resolved `MainErp_ConnectionString` directly, so DevStart database override did not apply. Salary/employee APIs could silently test against `Toger` while other screens used `Dania`. | Updated `EmployeePayrollController.ResolveConnectionString` to apply `MainErpDebugDatabaseOverride` for `MainErp_ConnectionString`. |
| `Dania` does not contain the newer medical insurance tables used by the web salary module. Lookup and salary preview could fail with `Invalid object name 'dbo.MedicalInsuranceProviders'`. | Made medical insurance tables optional in `EmployeePayrollRepository`: lookups return empty medical arrays and salary preview/save runs with zero medical impact when tables are absent. |

## Dania Data Profile

| Table | Count |
| --- | ---: |
| `BanksData` | 12 before QA seed |
| `tblBoxesData` | 77 before QA seed |
| `TblEmployee` | 879 before QA seed |
| `mofrad` | 40 before QA seed |
| `TblEmpAdvanceRequest` | 558 |
| `TblVocationEntitlements` | 415 |
| `TblRegsterSickleave` | 1 |
| `TblChangedComponentRegister` | 476 |
| `TblChangedComponentRegisterDetails` | 8,300 |
| `TblEmpAllocations` | 192 |
| `TblEmpAllocationsDetails` | 14,871 |
| `notes` | 80,331 |
| `DOUBLE_ENTREY_VOUCHERS` | 266,557 |
| `TblBranchesData` | 7 |
| `ACCOUNTS` | 16,018 |
| `currency` | 6 |
| `tblUsers` | 117 |
| `ScreenJuncUser` | 65,094 |

## QA Seed

Added repeatable QA seed script:

- `Areas/MainErp/Sql/10_LegacyHrFinance_Dania_QA_Seed.sql`

The script is SQL Server 2012 compatible and creates clearly marked rows:

| Entity | Seed |
| --- | --- |
| Employee | `Emp_Code = QA-MIG-HR-001` |
| Bank | `QA_MIG_BANK_DANIA` |
| Box | `QA_MIG_BOX_DANIA` |
| Payroll component | `QA_MIG_COMPONENT_DANIA` |
| Salary period | `emp_salary.sgn = 209911` for the QA employee |

The script was executed on `Dania`; verification confirmed one row for each QA entity.

## Runtime Workflow Tests

All tests used local MainErp runtime with DevStart database override set to `Dania`.

| Workflow | Result | Evidence |
| --- | --- | --- |
| Banks add | PASS | Web endpoint returned success and created QA bank row. |
| Banks edit | PASS | Re-tested with `BankId`; update returned same bank id. |
| Banks duplicate prevention | PASS | Duplicate name returned HTTP 400. |
| Banks search | PASS | Search result contained QA bank. |
| Boxes add | PASS | Web endpoint returned success and created QA box row. |
| Boxes edit | PASS | Re-tested with `BoxId`; update returned same box id. |
| Payroll component add | PASS | `MOFRAD` endpoint created QA component row. |
| Payroll component edit | PASS | Edit returned same component id. |
| Payroll component duplicate prevention | PASS | Duplicate name returned HTTP 400. |
| Employee profile search | PASS | Search found `QA-MIG-HR-001`. |
| Employee profile load | PASS | `Get?id=QA employee` returned employee name. |
| Salary run preview | PASS | QA employee preview returned 1 row, net 3,500, and 2 journal preview lines. |
| Salary run save draft | PASS | Inserted 1 `emp_salary` draft row in transient test, then cleanup removed it. |
| Advance workflow reads | PASS | `/LegacyHrFinance/Advances` opened with real `Dania` rows. |
| Leave entitlement reads | PASS | `/LegacyHrFinance/LeaveEntitlements` opened with real `Dania` rows. |
| Sick leave reads | PASS | `/LegacyHrFinance/SickLeaves` opened with real `Dania` row. |
| Compensation adjustment reads | PASS | `/LegacyHrFinance/CompensationAdjustments` opened with real aggregate rows. |
| Employee allocation reads | PASS | `/LegacyHrFinance/EmployeeAllocations` opened with real aggregate rows. |
| Arabic encoding | PASS | Rendered MainErp pages checked for obvious mojibake markers; no broken Arabic found in tested screens. |
| Null reference/runtime errors | PASS after fixes | Medical table absence no longer breaks payroll lookups or salary preview. |
| POS/MainErp cross dependency | PASS after fixes | SalaryRun now uses MainErp partial/assets and MainErp DB override. |
| Multi-user duplicate behavior | PASS | Two concurrent bank-add requests with the same name produced one success, one HTTP 400, and one persisted row. |

## Accounting Impact Validation

| Workflow | `notes` before | `notes` after | `DOUBLE_ENTREY_VOUCHERS` before | `DOUBLE_ENTREY_VOUCHERS` after | Result |
| --- | ---: | ---: | ---: | ---: | --- |
| Bank/box/component/salary draft test batch | 80,331 | 80,331 | 266,557 | 266,557 | No automatic accounting posting occurred. |

Interpretation:

- Bank and box maintenance currently preserves master-data account references only. It does not create opening-balance `notes` / `DOUBLE_ENTREY_VOUCHERS`.
- Salary run save currently saves payroll draft rows in `emp_salary`; it does not post accounting vouchers.
- This is safer than posting incomplete entries, but it is not full VB6 accounting parity yet.

## Payroll Impact Validation

| Test | Result |
| --- | --- |
| QA employee salary preview for seeded period `209911` | 1 row, net salary 3,500, 2 journal preview lines |
| Transient salary save for `209912` | inserted 1 draft row in `emp_salary`, then cleanup removed it |
| Medical insurance impact | Gracefully zero on `Dania` because medical insurance tables are not installed |

Important parity gap:

- Existing `Dania` has many `emp_salary` rows with totals, while current web preview primarily reads salary/allowance values from `TblEmployee`. Many `Dania` employee master salary fields are empty, so existing production salary totals may not be reproduced from the web calculation without mapping the full VB6 salary source rules.

## Permission Edge Cases

Verified:

- Admin `UserType = 0` can open and save the migrated screens.
- Unauthenticated access redirects to MainErp login.
- Non-admin permission testing needs one stable named QA user with explicit `ScreenJuncUser` rows for these new legacy screen names. The current `Dania` matrix contains many legacy names, but not all newly mapped names such as `FrmBanksData`, `FrmBoxesData`, and `MOFRAD` in a consistent non-admin test profile.

Required before customer sign-off:

- Add/confirm `ScreenJuncUser` rows for each migrated screen name.
- Test view-only, add-only, edit-only, and denied profiles.

## Schema Mismatches Found

### Kishny Source vs Dania

| Area | Mismatch |
| --- | --- |
| Boxes | Kishny source includes POS-specific `IsTerminalPOS`; `Dania.tblBoxesData` has `IsWallet` but no `IsTerminalPOS`. |
| Banks | `Dania.BanksData` has LC-related account fields (`PAcceptAccount_Code`, `PLCAccount_Code`, `PMarginAccount_Code`) beyond the base Kishny bank form mapping. |
| Medical payroll | Current web payroll has optional medical insurance tables; `Dania` does not contain `MedicalInsuranceProviders`, `MedicalInsurancePlans`, `EmployeeMedicalInsurance`, `PayrollMedicalInsuranceDeduction`, or `SalaryRunMedicalInsuranceDeduction`. |

### Main Original Source vs Dania

| Area | Mismatch / Risk |
| --- | --- |
| Payroll totals | `Dania.emp_salary` contains real period totals while many `TblEmployee.Emp_Salary` master values are empty; full payroll parity needs the VB6 salary run component/allocation sources, not just employee master salary. |
| Advances | `TblEmpAdvanceRequest` has 558 rows, but posting parity depends on legacy `SendTopost` rules and approval flags. |
| Allocations | `TblEmpAllocations` and details exist with large real data. Write parity is blocked because VB6 deletes/recreates allocation details and linked `notes`/`DOUBLE_ENTREY_VOUCHERS`. |
| Sick leave | Table exists and reads work, but write parity requires overlap and payroll-impact rules. |

## Read-Only Workflows: Missing Parity Rules

Do not enable write/post yet for these workflows:

| Screen | Missing parity rules |
| --- | --- |
| `FrmEmpsAdvanceRequest` | `SendTopost` accounting behavior, `Approved` vs `AccAproved`, `Posted`, `NoteSerial`, deduction schedule, and manager/finance approval transitions. |
| `FrmVocationEntitlements` | Leave entitlement formula, payout calculation, booked/delivery state, advance offsets, ticket value, and posting target accounts. |
| `FrmRegsterSickleave` | Date overlap detection, sick type rules, payroll deduction/paid-days impact, HR approval, and medical attachment storage rules. |
| `FrmChangedComponentData` | Absence-component detection via `MOFRAD.MofrdAbcen`, selected-employee expansion, period locking, detail rebuild rules, and posting/audit impact. |
| `FrmChangedComponentData1` | Department account validation, allocation type behavior, `notes`/`DOUBLE_ENTREY_VOUCHERS` delete/recreate flow, and rollback/audit behavior. |

## UX Workflow Verification

Observed:

- Financial admin add/edit is quick: list -> row click -> editor -> save.
- Search/filter is server-side and tested on real Dania data.
- Salary run preview for one employee completed quickly after optional medical fallback.
- Batch salary preview remains dependent on the same query and should be tested with full employee scope during off-hours because `Dania` has 879 employees.

Recommended next UX improvements before sign-off:

- Add explicit keyboard shortcuts for save/cancel/search.
- Add pagination controls visible at bottom of legacy workflow lists.
- Add inline account lookup search instead of TOP 500 datalist for very large account trees.
- Add export/print only after parity of read models is approved.

## Final Status

The module is now verified against `Dania` for real reads, add/edit/save of enabled workflows, lookup loading, duplicate prevention, salary preview/save draft, and no accidental POS dependency for SalaryRun.

It is not yet approved for enabling write/post on read-only workflows because accounting parity for VB6 posting flows remains unverified by design.

