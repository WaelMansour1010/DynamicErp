# RSMDB Review Queue Diagnostics - 2026-05-20

## Scope

Diagnostics were executed after staging load on clone only. The diagnostics populated `PropertyMigrationWarning`, `PropertyMigrationError`, and `PropertyMigrationReviewQueue` for BatchId `1b5d8eb5-dd7e-4b63-8ddb-e7b00aad558b`.

## Totals

| Diagnostic Bucket | Count |
|---|---:|
| Warnings | 9 |
| Critical Errors | 1 |
| Review Queue items | 18,932 |

## Review Queue By Issue Type

| IssueType | Entity | Severity | Count |
|---|---|---|---:|
| `JournalWithoutMappedLines` | Journal | Critical | 8,083 |
| `IssuePaymentManualReview` | Issue | Warning | 7,632 |
| `ReceiptWithoutSafeLink` | Receipt | Warning | 2,282 |
| `TerminationManualReview` | Termination | Warning | 754 |
| `UnitWithoutProperty` | Unit | Warning | 105 |
| `UnclassifiedNoteType9088` | Note | Warning | 64 |
| `ContractWithoutUnit` | Contract | Warning | 4 |
| `ContractWithoutRenter` | Contract | Warning | 4 |
| `OwnerPayableCandidate` | OwnerBalance | Warning | 4 |

## Warning Summary

| Warning | Count / Meaning |
|---|---|
| UnitsWithoutProperty | 105 units need property mapping or fallback. |
| ContractsWithoutUnit | 4 contracts need unit mapping or fallback. |
| ContractsWithoutRenter | 4 contracts need renter mapping or fallback. |
| ContractsWithoutProperty | 4 contracts need property mapping or fallback. |
| ReceiptsWithoutSafeLink | 2,282 receipts need contract/installment linkage review. |
| IssuesManualReview | 7,632 issue/payment records require classification. |
| OwnerPayableCandidates | 4 owner payable candidates require finance review. |
| TerminationsManualReview | 754 termination candidates require NoteType -1 confirmation. |
| JournalsWithoutMappedLines | 8,083 staged journal headers have no mapped target account lines. |

## Critical Error

`JournalsWithoutMappedLines`: RSMDB journal headers were staged but no journal lines mapped to target accounts. Accounting migration is blocked until account mapping is resolved.

## Top 5 Problems

1. Journal headers without mapped account lines: 8,083.
2. Issue/payment records requiring manual review: 7,632.
3. Receipts without safe contract/installment link: 2,282.
4. Termination candidates requiring manual review: 754.
5. Units without property link: 105.

## Decision

RSMDB must not proceed to migration until at least journal account mapping, receipt linkage, payment classification, and NoteType review are completed.
