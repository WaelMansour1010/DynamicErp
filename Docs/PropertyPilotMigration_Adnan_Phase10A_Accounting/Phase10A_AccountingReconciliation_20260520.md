# Phase 10A - Accounting Reconciliation
Date: 2026-05-20

## Receipt Reconciliation
| Metric | Source | Clone | Status |
|---|---:|---:|---|
| Cash receipt count | 753 | 753 | Pass |
| Receipt paid total | 12,719,724.4580 | 12,719,724.4580 | Pass |
| Receipt detail rows | 811 | 811 | Pass |

## Journal Reconciliation
| Metric | Value | Status |
|---|---:|---|
| Journal entries | 753 | Pass |
| Journal detail lines | 2,360 | Pass |
| Total debit | 12,802,048.4785 | Pass |
| Total credit | 12,802,048.4785 | Pass |
| Unbalanced journals | 0 | Pass |
| `AccountId=NULL` lines | 0 | Pass |

## Renter Balance Reconciliation
| Metric | Value |
|---|---:|
| Contracts compared | 250 |
| Matched | 250 |
| Differences | 0 |
| Source renter net total | -12,717,224.4580 |
| Clone renter net total | -12,717,224.4580 |

## Exclusions
| Type | Count | Reason |
|---|---:|---|
| Cash issue/payment candidates | 6 | Not safely contract-linked; refund/insurance/payment semantics require manual review |

## Advance Payments
| Metric | Value |
|---|---:|
| Advance staging rows | 14 |
| Advance amount | 55,592.8900 |
| Phase10A extra posting | 0 |

Raw files:
- `Phase10A_AccountingReconciliation_raw.txt`
- `Phase10A_RenterBalanceReconciliation_raw.txt`
- `Phase10A_FinalAccountingValidation_raw.txt`
