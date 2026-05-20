# RSMDB Receipt Allocation Discovery Results - 2026-05-20

## Execution Scope
Executed discovery/enrichment only on clone:
`Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520`

No migration was executed. No receipts or journals were created.

## Key Findings

1. A valid operational receipt-to-installment evidence table exists: `ContracttBillInstallmentsDone`.
2. That table links normal property receipts through `NoteID` and installment id.
3. Current 58 finance-approved candidates have no rows in that table.
4. Current 58 candidates are all `CashingType=7` (`From Account`), while VB6 confirms property contract receipts are `CashingType=8` (`From Contract`).
5. `ReciveDetails` exists for 49 of the 58, but does not contain contract/installment relationship evidence.

## Result After Discovery

| Classification | Count |
|---|---:|
| AutoApprovedLink | 0 |
| HighConfidence | 0 |
| MediumReview | 4 |
| WeakMatch | 21 |
| Blocked | 33 |

## Operational Decision
The current 58 cannot be used for the first accounting pilot because they do not satisfy the rule:
`Receipt must have Contract + Installment + Renter link`.

## Better Candidate Direction
The first RSMDB accounting pilot should be rebuilt around:
- `Notes.NoteType = 4`
- `Notes.CashingType = 8`
- direct `Notes.ContNo`, `Notes.CusID`, `Notes.akarid`, `Notes.UnitNo`
- matching `ContracttBillInstallmentsDone.NoteID`
- finance-approved account mappings
