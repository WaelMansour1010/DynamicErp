# RSMDB CashingType=8 First Pilot Scope - 2026-05-20

## Recommended Pilot Scope

Do not start with Top 25/50 by raw priority because they unlock only 4 receipts. The safer and more useful pilot scope is:

- NoteType=4 only
- CashingType=8 only
- ContracttBillInstallmentsDone allocation required
- Contract/Installment/Renter link required
- Balanced journal required
- No unknown direction
- No AccountId=NULL
- Finance-approved accounts only
- No Issues
- No Owner Payments
- No 9088
- No Terminations
- No unapproved Suspense

## Proposed Execution After Approval

Preferred first execution candidate after explicit finance sign-off:

| Scenario | Receipts | Journals | Lines | Value |
|---|---:|---:|---:|---:|
| Score >= 60 approved mappings | 32 | 32 | 64 | 966,568.2500 |

## Why Not Top 25/50

Top 25/50 unlock only 4 receipts because many high-priority accounts do not have a safe suggested target yet. Running such a tiny pilot is technically possible after approval, but it does not meaningfully validate the RSMDB accounting conversion.

## Next Finance Action

Finance should approve or correct the 97 ApproveAfterFinanceReview accounts and manually map the highest-impact NeedsMoreInfo tenant/customer accounts before execute.
