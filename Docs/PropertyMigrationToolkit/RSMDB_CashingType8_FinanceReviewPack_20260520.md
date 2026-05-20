# RSMDB CashingType=8 Finance Review Pack - 2026-05-20

## Purpose
This review pack is limited to true property receipts:

- `NoteType=4`
- `CashingType=8`
- `ContracttBillInstallmentsDone` exists

It intentionally excludes `CashingType=7` and general account receipts.

## Review Table
Generated in clone:
`dbo.PropertyMigrationCashingType8FinanceReviewAccount`

## Summary

| Metric | Count |
|---|---:|
| Accounts requiring review | 733 |
| Accounts that can unlock linked balanced receipts | present in review table via `CandidateClassImpact` |
| Suggested decision | `NeedsFinanceApproval` |

## Export Query

```sql
SELECT *
FROM dbo.PropertyMigrationCashingType8FinanceReviewAccount
WHERE MigrationBatchId = '1B5D8EB5-DD7E-4B63-8DDB-E7B00AAD558B'
ORDER BY CASE WHEN CandidateClassImpact LIKE N'Can unlock%' THEN 0 ELSE 1 END,
         UsageReceiptCount DESC,
         ISNULL(TotalDebit,0)+ISNULL(TotalCredit,0) DESC;
```

## Required Finance Decisions
For each account:

- Approve target account mapping
- Change target account mapping
- SuspenseApproved, only if finance explicitly approves
- Block
- NeedsMoreInfo

No mapping from this pack has been automatically approved.
