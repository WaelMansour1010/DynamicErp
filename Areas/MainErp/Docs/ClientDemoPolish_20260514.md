# Client Demo Polish - 2026-05-14

## Scope

Phase 15 focused on client-facing polish for the HR/payroll demo surface without changing production posting safety.

Screens touched in this pass:

- MainErp Salary Run / payroll preview.
- Payroll explainability wording.
- Accounting replay presentation.
- Protected Test Posting panel.

Existing Phase 13/14 polish remains in place for Employees, Medical Insurance, Project Extracts, LC, Banks, Boxes, and POS operational visibility.

## UI Improvements

- Added a dedicated `Protected Test Posting` section to Salary Run.
- Added clear KPI cards for:
  - current database;
  - allowlist status;
  - notes count;
  - voucher line count;
  - replay balance.
- Added visible safety copy:
  - `Generate Test Posting`, not `Post Payroll`;
  - production posting remains disabled;
  - test rows are real accounting rows only in allowlisted test databases.
- Added account/branch/project/department impact lists for finance-friendly preview.
- Added client-friendly wording for replay mismatch categories:
  - `Reconstructed` is displayed as `Calculated preview`.
  - `historically inconsistent behavior` is displayed as `Historical posting pattern requires finance review`.
  - `missing VB6 branch` is displayed as `Legacy source did not produce matching posted entries for this case`.

## Safety Confirmations

- Production posting remains blocked.
- `SendTopost`, salary payment posting, allocation rebuild, production `Notes`, and production `DOUBLE_ENTREY_VOUCHERS` creation remain blocked.
- Test posting requires:
  - allowlisted database;
  - `PayrollTestPostingPassword`;
  - exact confirmation phrase `POST TO TEST`;
  - marked rows with `[TEST_PAYROLL_POSTING]`;
  - generated `TestPostingBatchId`;
  - audit row;
  - cleanup by batch id only.

## Runtime Results

Target database: `Dania`.

| Check | Result | Notes |
| --- | --- | --- |
| Build | PASS | `MyERP -> bin\MyERP.dll`; only existing warnings. |
| JavaScript syntax | PASS | `node --check Areas/MainErp/Scripts/employee-payroll.js`. |
| Salary Run route | PASS | Browser opened `/MainErp/EmployeePayroll/SalaryRun`; no console errors. |
| Protected Test Posting panel | PASS | Browser DOM contains panel and `Generate Test Posting`. |
| Dry-run on Dania | PASS | `2026/03`: 4 notes, 572 voucher lines, debit 1,675,536.4375, credit 1,675,536.1075, balance 0.3300. |
| Test generation on Dania | PASS | Batch `8b432f82-c226-4e87-9368-4bd506a95ec7`; 4 notes, 572 voucher lines. |
| Cleanup by batch | PASS | Same batch cleaned 4 notes and 572 voucher lines; remaining marked rows = 0. |

## Remaining Warnings

- Replay balance for `2026/03` is off by 0.33. This is acceptable for test visibility but remains a finance-review item before any production posting design.
- `Dania` does not contain the newer MainErp medical-insurance tables; fallback behavior remains intentional.
- The configured demo password in `Web.config` is for local demo only and must be changed per environment.

## Screenshots Checklist

- Salary Run top dashboard.
- Payroll preview grid with `LegacySnapshot` and `Calculated preview` badges.
- Accounting replay cards.
- Protected Test Posting dry-run summary.
- Test generation success showing batch id.
- Cleanup success showing cleaned counts.
