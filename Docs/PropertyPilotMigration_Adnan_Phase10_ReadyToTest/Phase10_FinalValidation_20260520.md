# Phase 10 - Final Validation
Date: 2026-05-20
ReadyToTest DB: $db

## Counts
| Metric | Actual | Expected | Status |
|---|---:|---:|---|
| Properties | 26 | 26 | Pass |
| Units | 258 | 258 | Pass |
| Tenants | 256 | 256 | Pass |
| Tenant accounts | 256 | 256 | Pass |
| Contracts | 283 | 283 | Pass |
| Contract batches | 1,099 | 1,099 | Pass |
| Excluded contracts | 10 | 10 | Pass |

## Financials
| Metric | Actual | Expected | Status |
|---|---:|---:|---|
| Opening Balance | 1,156,544.6600 | 1,156,544.6600 | Pass |
| Future Gross | 19,234,398.7085 | 19,234,398.7085 | Pass |
| Advance Payments | 55,592.8900 | 55,592.8900 | Pass |
| Net Remain | 19,178,805.8185 | 19,178,805.8185 | Pass |

## Integrity
| Check | Count | Status |
|---|---:|---|
| Contracts without unit | 0 | Pass |
| Contracts without tenant | 0 | Pass |
| Batches without contract | 0 | Pass |
| Missing seeded accounts | 0 | Pass |
| Contracts with tenant missing account | 0 | Pass |
| Test receipts | 0 | Pass |
| Test issues | 0 | Pass |
| Test terminations | 0 | Pass |
| Test journal entries | 0 | Pass |
| Active journal lines with AccountId=NULL | 0 | Pass |
| Unbalanced active journals | 0 | Pass |
| CashIssue same-account debit/credit pairs | 0 | Pass |

## Operational Readiness
| Check | Count | Status |
|---|---:|---|
| Pilot branch | 1 | Pass |
| Ready cash boxes | 1 | Pass |
| Ready bank accounts | 1 | Pass |
| Receipt methods | 2 | Pass |
| Issue methods | 2 | Pass |
| ErpAdmin user | 1 | Pass |
| ErpAdmin department link | 1 | Pass |
| ErpAdmin cashbox link | 1 | Pass |

Raw output: Phase10_FinalValidation_raw.txt.
