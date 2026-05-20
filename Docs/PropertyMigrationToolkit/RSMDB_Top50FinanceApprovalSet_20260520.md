# RSMDB Top 50 Finance Approval Set - 2026-05-20

## Scope
First finance-assisted account approval cycle on clone only.

## Clone
Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520

## BatchId
1b5d8eb5-dd7e-4b63-8ddb-e7b00aad558b

## Approval Set
- Top impact accounts selected: 50.
- Selection basis: accounts with highest impact on the 706 journal candidates, ordered by candidate journal count then amount.
- All selected rows had suggested target accounts and score >= 60.

## Top Examples
| Rank | SourceAccountCode | SourceAccountName | Candidate Journals | Suggested Target | Family |
|---:|---|---|---:|---|---|
| 1 | 1a2a1a2a7a1 | البنك السعودى للاستثمار | 310 | 110102001 | Banks |
| 2 | 2a2a4a5a5 | مصاريف فواتير كهرباء مستحقة | 140 | 410201001 | Expense |
| 3 | 1a2a1a1a3a1 | النقديه بصندوق المركز الرئيسى | 94 | 110101001 | Cash |
| 4 | 3a1a3a3a4 | مصاريف كهرباء | 77 | 410201001 | Expense |
| 5 | 1a2a7a1 | ايجارات مستحقة | 70 | 220601002 | Revenue |

## Output
Rows inserted into PropertyMigrationAccountFinanceApproval with:
- Decision=Approved
- Status=Approved
- ApprovedBy=PropertyMigrationToolkit Pilot Approval

This is clone-only pilot approval, not GoLive approval.
