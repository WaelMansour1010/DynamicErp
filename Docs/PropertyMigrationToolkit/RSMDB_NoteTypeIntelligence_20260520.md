# RSMDB NoteType Intelligence - 2026-05-20

## Results
| NoteType | Suggested Category | Confidence | Evidence |
|---|---:|---:|---|
| 4 | Receipt | 94 | Count 10,365, with ContNo=8,778, with CusID=8,805 |
| 5 | IssueOrPayment | 72 | Count 7,632, weak property/owner evidence |
| 60 | ContractJournalCandidate | 70 | Count 2,426, with installment link 1,858 |
| -1 | TerminationCandidate | 65 | Count 754, mixed CusID/installment evidence |
| 9088 | VATOrInstallmentAdjustmentCandidate | 45 | Count 64, insufficient evidence |

## Decision
Only NoteType=4 is high-confidence. Types 5, 60, -1, and especially 9088 remain review-controlled and must not be migrated as final accounting without more VB6/finance evidence.
