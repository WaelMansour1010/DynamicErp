# Working Read-Only Reports

## Implemented Routes

- `/MainErp/AccountingReports/JournalEntries`
- `/MainErp/AccountingReports/AccountMovement`
- `/MainErp/SalesReports/SalesSummary`

All reports are read-only. No save, edit, delete, posting, stored procedure creation, or SQL object changes were added.

## Report 1: القيود اليومية خلال فترة

Source tables:

- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- `ACCOUNTS`
- `TblBranchesData`
- `TblUsers`

Filters:

- `FromDate`
- `ToDate`
- `BranchId`
- `AccountCode`
- `NoteType`

Direction rule:

- `Credit_Or_Debit = 0` means debit.
- `Credit_Or_Debit = 1` means credit.

Displayed fields:

- date from `COALESCE(Notes.NoteDate, DOUBLE_ENTREY_VOUCHERS.RecordDate)`
- note serial
- note type
- account code/name
- debit
- credit
- description
- branch
- user

## Report 2: حركة حساب خلال فترة

Source tables:

- `DOUBLE_ENTREY_VOUCHERS`
- `Notes`
- `ACCOUNTS`

Filters:

- `FromDate`
- `ToDate`
- `AccountCode` required
- `BranchId`

Current limitation:

- Opening balance is not calculated yet because legacy opening-balance rules require validation.
- The displayed running value is the running movement within the selected period only.

## Report 3: ملخص المبيعات خلال فترة

Source tables:

- `Transactions`
- `TblBranchesData`
- `TblUsers`
- `TblCustemers`

Filters:

- `FromDate`
- `ToDate`
- `BranchId`
- `UserId`
- `CustomerId`

Conservative filter:

- `Transactions.Transaction_Type IN (22, 29)`

Limitation:

- The final ERP-wide sales type mapping still needs approval from legacy analysis.
- The report displays a visible warning explaining that the current sales-type filter is conservative and requires final validation.

## Exclusions

The implementation intentionally excludes specialized operational reports and behavior. It does not call POS controllers, POS repositories, POS stored procedures, POS session restoration, or POS permissions.

## Schema Warning Behavior

Each report repository catches `SqlException` and returns a visible warning instead of crashing when the configured database lacks the required legacy schema.

## Next Report Candidates

- Trial balance.
- Account statement with validated opening balance.
- General ledger assistant.
- Sales report by customer.
- Sales report by branch with approved transaction-type mapping.
