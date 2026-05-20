# Phase 9 - Accounting Validation
Date: 2026-05-20
Pilot Clone: $clone

## Journal Summary
| Scenario | Document | JournalEntry | Debit | Credit | Diff | Null Account Lines | Status |
|---|---:|---:|---:|---:|---:|---:|---|
| Cash receipt partial | 40 | 2928 | 125.0000 | 125.0000 | 0.0000 | 0 | Pass |
| Bank receipt full | 41 | 2929 | 41,400.0000 | 41,400.0000 | 0.0000 | 0 | Pass |
| Cash direct expense issue | 24 | 2930 | 77.0000 | 77.0000 | 0.0000 | 0 | Pass |
| Bank direct expense issue | 25 | 2931 | 88.0000 | 88.0000 | 0.0000 | 0 | Pass |
| Contract termination | 4 | 2932 | 311,928.9800 | 311,928.9800 | 0.0000 | 0 | Pass |

## Account Direction
| Scenario | Debit | Credit | Status |
|---|---|---|---|
| Cash receipt | CashBox account 629 | Renter account 764 | Pass |
| Bank receipt | Bank account 631 | Renter account 764 | Pass |
| Cash direct expense issue | Direct expense account 805 | CashBox account 629 | Pass |
| Bank direct expense issue | Direct expense account 805 | Bank account 631 | Pass |
| Termination | Renter account 764 | Rent revenue account 740 | Pass |

## Specific Risk Checks
| Check | Result |
|---|---:|
| AccountId=NULL in Phase9 journals | 0 |
| Unbalanced Phase9 journals | 0 |
| Same debit/credit account in cash issue | 0 |
| Global active journal detail rows with AccountId=NULL after cleanup | 0 |
| Global unbalanced active journals after cleanup | 0 |

## Receipt Batch Effect
| Receipt | Batch | Paid | Remain | Delivered |
|---:|---:|---:|---:|---|
| 40 | 6119 | 125.0000 | 41,275.0000 | False |
| 41 | 6120 | 41,400.0000 | 0.0000 | True |

## Cleanup
The test vouchers and termination were removed after validation using the guarded cleanup script. Raw accounting evidence remains in Phase9_AccountingValidation_raw.txt.
