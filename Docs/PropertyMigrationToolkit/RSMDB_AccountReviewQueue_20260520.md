# RSMDB Account Review Queue - 2026-05-20

## Account Review Queue Results
| Status | Issue Type | Count |
|---|---|---:|
| Blocked | BlockedAccountMapping | 1,125 |
| FinanceReview | FinanceReviewAccountMapping | 470 |
| HighConfidenceReview | HighConfidenceAccountMapping | 4 |

## Journal Impact
- Before account intelligence, balanced source journals with missing target accounts: 2,704.
- After account intelligence, journal candidates with all line accounts at least finance-review mapped: 706.
- Remaining balanced journals blocked by account mapping: 1,998.

## Decision
The account review queue provides a finance-friendly worklist. No journal should be migrated until its account mappings are approved.
