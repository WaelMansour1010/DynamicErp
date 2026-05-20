# Phase 8 - CashIssueVoucher Audit
Date: 2026-05-20
Scope: CashIssueVoucher only. DynamicErp main web, not POS.
Sandbox: Alromaizan_PropertyPilot_Adnan_20260520

## Executive Finding
Phase 7 warning was valid. `CashIssueVoucher_Insert/Update` can create a balanced but economically wrong journal if the configured source debit account equals the payment credit account.

## Root Cause
For `SourceTypeId=4` (Direct Expenses / مصروفات مباشرة), the stored procedures do not debit `DirectExpenses.AccountId`. They debit:

`Department.DirectExpensesAccountId`

In Sandbox:
- `Department 44.DirectExpensesAccountId = 629`
- `CashBox 1022.AccountId = 629`

Therefore a cash direct-expense issue produced:
- Debit 629 cashbox
- Credit 629 cashbox

This is balanced and non-null, but wrong.

## Stored Procedure Rules Observed
| SourceTypeId | Source Name | Debit Account Source |
|---:|---|---|
| 1 | Customer | Department.CustomersAccountId |
| 2 | Vendor | Department.VendorsAccountId |
| 3 | Employee | Employee.AccountId |
| 4 | Direct Expenses | Department.DirectExpensesAccountId |
| 5 | Taxes | Department.DebitValueAddedTaxesAccountId |
| 6 | Other | CashIssueVoucher.AccountId |
| 7 | Shareholder | ShareHolder.AccountId |
| 8 | Technician in lookup, but SP uses DirectExpenses | DirectExpenses.AccountId |
| 9 | Salary | Department.DueSalariesAccountId |
| 10 | Issue Analysis | IssueAnalysis.AccountId rows + Department VAT account |
| 12 | Prepaid Expense | PrepaidExpenseDetail.PrePaymentAccountId |
| 13 | Owner | Department.OwnerAccountId |

## Payment Credit Rules Observed
| Payment Method | Credit Account Source |
|---|---|
| Cash | CashBox.AccountId |
| Bank/Cheque | BankAccount.AccountId in SP; code validates BankAccountPaymentId or AccountId |
| Account | CashIssueVoucher.ChartOfAccountId |

## Main Risk
A journal can be balanced and have no NULL accounts while still netting zero effect if debit and credit use the same account. This is especially dangerous for direct expenses when department setup points to a cash/bank account.
