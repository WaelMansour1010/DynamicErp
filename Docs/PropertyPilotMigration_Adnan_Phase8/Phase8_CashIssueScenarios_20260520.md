# Phase 8 - CashIssueVoucher Scenarios
Date: 2026-05-20

| Scenario | SourceTypeId | Expected Debit | Expected Credit | Pilot Needed? | Phase8 Status |
|---|---:|---|---|---|---|
| Cash direct expense | 4 | Department.DirectExpensesAccountId | CashBox.AccountId | Optional, not core migration | Safe after department account fix + same-account validation |
| Bank direct expense | 4 | Department.DirectExpensesAccountId | BankAccount payment/account | Optional, not core migration | Safe after department account fix + same-account validation |
| Vendor payment | 2 | Department.VendorsAccountId | CashBox/Bank/Account | Not required for property migration pilot | Validation protects NULL/same account; not business-tested |
| Employee payment | 3 | Employee.AccountId | CashBox/Bank/Account | Not required | Validation protects NULL/same account; not business-tested |
| Other account payment | 6 | Voucher.AccountId | CashBox/Bank/Account | Not required | Deliberate same-account test was blocked |
| Shareholder payment | 7 | ShareHolder.AccountId | CashBox/Bank/Account | Not required | Validation protects NULL/same account; not business-tested |
| Issue analysis | 10 | IssueAnalysis.AccountId rows | CashBox/Bank/Account | Not required | Added validation for missing analysis account and same payment account |
| Prepaid expense | 12 | PrepaidExpenseDetail.PrePaymentAccountId | CashBox/Bank/Account | Not required | Validation protects NULL/same account; not business-tested |
| Property owner payment | 13 | Department.OwnerAccountId | CashBox/Bank/Account | Possibly future property operation | Not tested because migrated Adnan pilot did not include safe property-owner payment data |

## Pilot Recommendation
For the Adnan property pilot, CashIssueVoucher should remain controlled and limited. Receipts and termination are safe; payments may be allowed only after the customer clone has verified Department and source-account setup.
