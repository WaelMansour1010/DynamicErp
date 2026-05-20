# Property Owners Accounting Strategy - 2026-05-20

## Owner Accounting Default

Owner accounting is not part of the default safe migration. It is review-gated.

## What Can Be Migrated Safely

- Owner master data.
- Property-to-owner primary link.
- Owner balance staging for review.

## What Is Manual Review By Default

- Owner payable balances from `TblAqrOwin`.
- Owner payment vouchers from `TblOwnerPayment` / `TblNotesOwnerPayment`.
- Any `CashIssueVoucher` scenario that appears to pay an owner.
- Any journal entry linked to owner payments.

## Required Conditions Before Owner Payments

Owner payment migration requires all of:

- Owner linked to `PropertyOwner`.
- Property linked to owner.
- Owner `AccountId` mapped.
- CashBox/BankAccount mapped.
- Payment method mapped through resolver.
- Journal direction verified.
- No `AccountId=NULL`.
- Debit/Credit balanced.
- No duplicate accounting effect from old journals and new vouchers.

## Owner Payables

Owner payable balances can be staged and reconciled, but should not be posted automatically until finance confirms:

- Whether the payable already exists in opening balances.
- Whether paid amounts in `TblAqrOwin.TotalPayed` are already reflected in journals.
- Whether owner payable should be delivered as opening balance, historical archive, or payment schedule.
