# Property Owners Accounting Strategy - 2026-05-20

## Principle

Owner accounting is financially sensitive and must remain strict even when the rest of property master data uses Hybrid or Tolerant mode.

## What Can Be Migrated Safely

| Item | Default Decision |
|---|---|
| Owner master data | Allowed after mapping validation. |
| Property-owner relationship | Allowed when property and owner are both mapped. |
| Owner balances | Stage/review first; migrate only with finance sign-off. |
| Owner payment vouchers | Manual Review by default. |
| Owner journals | Migrate only when linked to approved owner payments and fully balanced. |

## When To Migrate Owner Payables

Owner payable balances may be migrated only when:

- Source table is confirmed as owner payable/receivable.
- Owner maps to `PropertyOwner`.
- Related property/contract maps to target data or finance approves owner-level balance.
- Balance does not duplicate tenant opening balance or historical journal migration.

## When To Migrate Owner Payments

Owner payments may be migrated only when:

- Voucher is explicitly linked to owner.
- Voucher source type is proven for that customer.
- Debit account and cash/bank credit account are known.
- Journal is balanced and has no null accounts.
- Same-account debit/credit is blocked unless specifically approved.

## SourceTypeId=13

`CashIssueVoucher.SourceTypeId = 13` appears in MyErp and likely represents property owner payment behavior, but the toolkit must not rely on this value alone. It is a candidate mapping requiring code/data validation per customer.

## Suspense Use

Suspense/holding accounts for owners are allowed only with explicit finance approval and must remain visible in Review Queue and final reconciliation. They must not be silently cleared or hidden.

## Go Live Blockers

- Open owner payment review items without finance sign-off.
- Owner payment journals with null accounts.
- Owner payment journals that do not balance.
- Owner payable balances not reconciled to owner reports.
- Any use of owner suspense not signed off.
