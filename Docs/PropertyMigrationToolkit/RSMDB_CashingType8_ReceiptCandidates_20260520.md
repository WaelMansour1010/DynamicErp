# RSMDB CashingType=8 Receipt Candidates - 2026-05-20

## Scope
This phase rebuilt the RSMDB accounting pilot candidate set from true property contract receipts only:

- `Notes.NoteType = 4`
- `Notes.CashingType = 8` (`من عقد` / From Contract)
- `ContracttBillInstallmentsDone` rows exist
- No `CashingType=7` receipts included
- No migration, no posting, no receipt creation, no journal creation

## Source Counts

| Metric | Count / Value |
|---|---:|
| Total `NoteType=4 + CashingType=8` receipts | 8,778 |
| Total value of all `CashingType=8` receipts | 212,423,848.6510 |
| Receipts with `ContracttBillInstallmentsDone` | 8,083 |
| `ContracttBillInstallmentsDone` rows | 9,547 |
| Receipts with source journal lines | 8,778 |

## Candidate Table
The candidate set was written to clone-only table:
`dbo.PropertyMigrationCashingType8ReceiptCandidate`

Batch:
`1B5D8EB5-DD7E-4B63-8DDB-E7B00AAD558B`

## Classification Summary

| Classification | Receipts | Receipt Amount | Journal Lines | Contracts | Renters |
|---|---:|---:|---:|---:|---:|
| NeedsFinanceApproval | 505 | 14,115,520.2900 | 1,010 | 229 | 227 |
| NeedsLinkReview | 7,578 | 184,248,146.5350 | 15,166 | 2,356 | 650 |
| ReadyForAccountingPilot | 0 | 0.0000 | 0 | 0 | 0 |

## Interpretation
The new scope is operationally correct, unlike the previous 58 `CashingType=7` candidates. However, the first ready set is still blocked by account finance approvals.
