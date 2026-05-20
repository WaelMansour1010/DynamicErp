# Phase 10A - Accounting Scope Assessment
Date: 2026-05-20
Target Clone: `Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520`
Source: `Adnan` read-only

## Source Tables Reviewed
| Area | Tables |
|---|---|
| Vouchers | `Notes` |
| Journal lines | `DOUBLE_ENTREY_VOUCHERS` |
| Rent installment payments | `ContracttBillInstallmentsDone` |
| Contract installments | `TblContractInstallments` |
| Termination / settlement | `TblFiterWaiver`, `TblFiterWaiverDe`, `TblFiterWaiverDet2` |
| Owner payments | `TblNotesOwnerPayment` |

## Notes Findings
Important `Notes.NoteType` counts in `Adnan`:

| NoteType | Meaning inferred | Count | Sum |
|---:|---|---:|---:|
| -1 | Settlement / termination-like | 885 | 1,383,617.6400 |
| 4 | Receipts | 8,784 | 76,641,565.8423 |
| 5 | Payments | 53 | 447,557.5800 |
| 60 | Contract journal-like | 1,847 | 30,257,405.2190 |
| 9088 | Not present in current grouped results | 0 | 0 |

## Safe Property-Linked Scope Found
| Scope | Count | Amount |
|---|---:|---:|
| Receipts linked to migrated active-contract installments | 753 | 12,719,724.4580 |
| Receipt detail rows linked to migrated batches | 811 | 12,719,724.4580 |
| Receipt journal entries in `DOUBLE_ENTREY_VOUCHERS` | 753 | Debit/Credit 12,802,048.4785 |
| Candidate issue/payment notes linked to property/tenant/property | 6 | 10,538.4200 |
| Owner payment rows linked to migrated properties | 0 | 0 |

## Key Logic
- Receipts are safely linked through `ContracttBillInstallmentsDone.NoteID` -> `TblContractInstallments.id` -> `PropertyPilotCrossReference(EntityType='ContractBatch')`.
- Journals are linked through `DOUBLE_ENTREY_VOUCHERS.Notes_ID = Notes.NoteID`.
- Tenant/account validation uses `ChartOfAccount.Code = DOUBLE_ENTREY_VOUCHERS.Account_Code` after Phase10A account seed.
- Payment notes `NoteType=5` were not contract-linked enough for safe operational migration.
