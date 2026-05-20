# Property Migration Toolkit - Accounting Strategy
Date: 2026-05-20

## Default Strategy
Accounting migration is opt-in and diagnostic-driven. The default for a new customer is:

- Opening Balance as of cutoff.
- Advance payments staged, not blindly posted.
- Historical receipts only if linked to migrated contracts/installments.
- Historical issues only if source type and accounting meaning are proven.
- Journals only if linked to approved migrated vouchers and balanced.
- Owner payments default to Manual Review.
- Unsafe history is Archive Only.

## When To Migrate Receipts
Migrate receipts when all are true:
- Receipt source table is known.
- Receipt type values are verified with data/code.
- Receipt links to contract or installment with no ambiguity.
- Tenant/account mapping exists.
- Cash/bank/payment method can be resolved.
- Related journal lines are balanced or the receipt can be posted through DynamicErp safely.

## When To Migrate Issues
Migrate issues only when:
- Source type is known.
- Debit account and credit cash/bank account are known.
- It is not an owner payment or refund with unclear semantics.
- Same-account debit/credit is impossible or explicitly approved.

## When To Migrate Journals
Migrate journals only when:
- They are linked to approved migrated vouchers.
- Every account maps to target ChartOfAccount.
- Debit equals credit within tolerance.
- No line has null account.
- Migration will not double-post an opening balance.

## Opening Balance vs History
If historical accounting is incomplete or unsafe, use Opening Balance and archive old history. Do not mix opening balance and full history in a way that double-counts tenant balances.

## Advance Payments
Advance payments should be staged first. Posting/allocating them requires a reviewed business decision:
- Credit opening balance.
- Advance payment liability.
- Allocation to future installments.
- Archive-only note.

## Terminations
Old terminations are high risk. Default to archive/manual review unless contract status, remaining balance, deposit/insurance, and journal effect can be reproduced exactly.


## Owner Accounting
Owner accounting is not migrated by default. It becomes eligible only after the owner master, property-owner relationship, owner account, and payment/journal source are proven for the customer.

Migrate owner payable balances only when:
- The source table is confirmed as owner payable/receivable, not rental installments or tenant debt.
- The owner is mapped to `PropertyOwner`.
- The related property or contract is mapped, or the balance is explicitly approved as owner-level opening balance.
- Finance signs off that the balance will not duplicate tenant opening balances or historical journals.

Migrate owner payment vouchers only when:
- The payment is explicitly linked to an owner and a property/contract/payment source.
- Owner account and cash/bank account both resolve to target accounts.
- The generated journal is balanced and contains no null account.
- Same-account debit/credit is blocked unless explicitly approved and documented.

Default handling:
- Owner payments are `ManualReview`.
- SourceType-based owner payments, including `SourceTypeId=13`, are not trusted automatically.
- Unclear owner payables are staged in Review Queue, not posted.
- Suspense for owners requires a visible report and finance approval before Go Live.
