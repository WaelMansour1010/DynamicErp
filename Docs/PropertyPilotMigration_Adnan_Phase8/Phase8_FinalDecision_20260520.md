# Phase 8 - Final Decision
Date: 2026-05-20

## Decision
CashIssueVoucher is no longer blocked by the Phase7 critical risk, provided the pilot clone receives:
1. Phase8 code validation.
2. Verified Department direct expense account setup.
3. Rollback procedure cleanup for `PropertyPilotAdvancePaymentStaging`.

## What Is Now Safe
- Direct expense cash payment, when `Department.DirectExpensesAccountId` is a real expense/control account distinct from the CashBox account.
- Direct expense bank payment, when bank account is linked and debit/credit are distinct.
- Same-account vouchers are blocked before posting.

## What Remains Controlled
- Property-owner payment (`SourceTypeId=13`) was not business-tested because the Adnan pilot did not include a safe owner-payment case.
- Vendor/employee/shareholder/prepaid/issue-analysis scenarios have validation protection, but were not part of the Adnan property pilot path.

## Pilot Recommendation
Allow CashIssueVoucher in the limited pilot only for reviewed scenarios. For the Adnan property pilot, it is acceptable to enable direct expense cash/bank tests on the clone after verifying account setup. Keep property-owner payments disabled or manual-review only until a real scenario is tested.

## Go/No-Go
- Receipts: Go
- Termination: Go
- Direct expense payment: Conditional Go after config check
- Property-owner/property payment: Hold/manual review
