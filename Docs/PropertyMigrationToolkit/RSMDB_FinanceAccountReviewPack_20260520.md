# RSMDB Finance Account Review Pack - 2026-05-20

## Objective
Convert as many RSMDB account mappings as possible from FinanceReview / Blocked to explicit finance-approved mappings before any accounting migration pilot.

## Current Scope
- Clone: Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520
- BatchId: 1b5d8eb5-dd7e-4b63-8ddb-e7b00aad558b
- No migration executed.
- No source database modified.

## Priority Accounts
- Accounts affecting the 706 finance-review journal candidates: 313.
- These are the top-priority accounts for finance review.

## Top 10 Priority Accounts
| Rank | Source Account | Name | Candidate Journals | Value | Suggested Target | Family | Confidence |
|---:|---|---|---:|---:|---|---|---|
| 1 | 1a2a1a2a7a1 | البنك السعودى للاستثمار | 310 | 24,125,693.0300 | 110102001 | Banks | NeedsFinanceReview |
| 2 | 2a2a4a5a5 | مصاريف فواتير كهرباء مستحقة | 140 | 322,450.4700 | 410201001 | Expense | NeedsFinanceReview |
| 3 | 1a2a1a1a3a1 | النقديه بصندوق المركز الرئيسى | 94 | 1,388,626.4500 | 110101001 | Cash | NeedsFinanceReview |
| 4 | 3a1a3a3a4 | مصاريف كهرباء | 77 | 145,772.9200 | 410201001 | Expense | NeedsFinanceReview |
| 5 | 1a2a7a1 | ايجارات مستحقة | 70 | 30,987,611.1800 | 220601002 | Revenue | NeedsFinanceReview |
| 6 | 2a1a2a1a1 | أ. ناصر المطوع | 49 | 1,434,726.8800 | 110201001 | Receivables | NeedsFinanceReview |
| 7 | 4a1a1a1a1 | ايراد ايجار | 34 | 31,458,993.6100 | 31041001 | Revenue | NeedsFinanceReview |
| 8 | 3a1a5a1a1 | رواتب أساسية | 32 | 2,462,042.3500 | 110201001 | Receivables | NeedsFinanceReview |
| 9 | 1a2a5a10 | مصروف صيانة مقدم | 29 | 392,886.0600 | 410201001 | Expense | NeedsFinanceReview |
| 10 | 1a2a1a2a6a1 | البنك العربى الوطنى | 25 | 16,590,695.4800 | 110102001 | Banks | NeedsFinanceReview |

## Export SQL
Use:
RSMDB_FinanceAccountReviewPack_20260520.sql

It returns Excel-ready result sets for:
- Top 100 by journal usage.
- Top 100 by amount.
- Accounts affecting 706 candidates.
- Blocked accounts.
- Suspense candidates.
- High-confidence mappings.
- Manual review mappings.
- Impact simulation.
