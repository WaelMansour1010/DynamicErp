# Payroll Test Posting Mode - 2026-05-14

## Purpose

Protected Test Posting Mode lets the client see real generated accounting rows in a test database while production payroll posting remains disabled.

This is not production posting.

## Allowed Databases

Configured in `Web.config`:

- `PayrollTestPostingAllowedDatabases=Dania`

Generation and cleanup are blocked when the current MainErp database is not allowlisted.

## Password And Confirmation

Configured in `Web.config`:

- `PayrollTestPostingPassword`

The user must type the exact confirmation phrase:

```text
POST TO TEST
```

## Workflow

1. User opens `/MainErp/EmployeePayroll/SalaryRun`.
2. User selects year, month, branch, department, or employee scope.
3. User runs dry-run.
4. UI shows:
   - replay debit;
   - replay credit;
   - balance;
   - notes count;
   - voucher line count;
   - affected accounts;
   - affected branches;
   - affected projects;
   - affected departments;
   - safety warnings.
5. User enters password and `POST TO TEST`.
6. User clicks `Generate Test Posting`.
7. System inserts marked test rows only in the allowlisted database.
8. System writes audit row with `TestPostingBatchId`.
9. User can cleanup by batch id.

## Data Marking

Generated legacy rows are marked in text fields:

```text
[TEST_PAYROLL_POSTING] Batch=<TestPostingBatchId>
```

Tables affected only in test mode:

- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- `MainErpPayrollTestPostingAudit`

## Audit Table

Script:

`Areas/MainErp/Sql/14_PayrollTestPostingMode.sql`

Runtime also creates the table safely if missing.

Audit table:

- `dbo.MainErpPayrollTestPostingAudit`

Stored values:

- batch id;
- user id/name;
- database name;
- year/month;
- branch/department/employee scope;
- inserted notes count;
- inserted voucher line count;
- debit;
- credit;
- balance;
- created at;
- cleanup status;
- cleaned counts.

## Cleanup

Cleanup is batch-scoped and marker-scoped.

It deletes only:

- `DOUBLE_ENTREY_VOUCHERS` rows linked to marked test notes and marked with the same batch;
- `Notes` rows marked with the same batch.

It never deletes unrelated rows.

## Runtime Verification On Dania

Tested period:

- `2026/03`, all branches.

Result:

- Dry-run: PASS.
- Generate: PASS.
- Cleanup: PASS.
- Inserted and cleaned notes: 4.
- Inserted and cleaned voucher lines: 572.
- Remaining marked rows after cleanup: 0.

## Safety Rules

Still blocked in production:

- payroll posting;
- salary payment posting;
- production `Notes` creation;
- production `DOUBLE_ENTREY_VOUCHERS` creation;
- `SendTopost` replacement;
- allocation rebuild.

## Client Explanation

Use this wording:

> Test posting creates real accounting rows only in the current test database. Production posting remains disabled until payroll/accounting parity is formally approved.
