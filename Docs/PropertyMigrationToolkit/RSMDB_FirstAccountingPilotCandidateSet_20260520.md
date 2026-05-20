# RSMDB First Accounting Pilot Candidate Set - 2026-05-20

## Recommended First Pilot Scope
Use a very narrow accounting pilot:
- Receipts only: NoteType=4.
- Only receipts already linked or high-confidence linked to contracts/installments.
- Only journals tied to approved receipt candidates.
- Only journals whose source lines are balanced.
- Only journals where every source account has finance-approved mapping.
- Prefer active contract candidates where possible.

## Excluded From First Pilot
- No issue/payment vouchers.
- No owner payments.
- No terminations.
- No NoteType 9088.
- No unbalanced journals.
- No journal with unresolved account mapping.
- No suspense unless explicitly approved and tracked.

## Entry Conditions
1. Finance approves mappings for at least Top 50 accounts, preferably Top 100.
2. Apply approved mappings on clone using the draft script after review.
3. Re-run account impact simulation.
4. Select only ReadyForMigration journals.
5. Run reconciliation before any accounting migration execution.
