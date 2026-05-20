# RSMDB Receipt Matching - 2026-05-20

## Scope
Receipts were matched using staging/source evidence only. No receipt vouchers were migrated.

## Signals Used
- Notes.ContNo exact contract signal.
- Notes.CusID renter consistency.
- Amount vs installment BatchTotal.
- Date proximity vs installment due date.

## Results
- Total staged receipts: 11,829.
- Already safe/auto-approved from staging: 9,547.
- Previously unsafe receipts: 2,282.
- New high-confidence candidates: 191.
- Medium review candidates: 172.
- Weak match candidates: 332.
- Blocked/no useful candidate: 1,587.

## Decision
Receipt review queue was reduced meaningfully, but only 191 unsafe receipts should be sampled/approved before auto-linking. Blocked receipts remain excluded from migration.
