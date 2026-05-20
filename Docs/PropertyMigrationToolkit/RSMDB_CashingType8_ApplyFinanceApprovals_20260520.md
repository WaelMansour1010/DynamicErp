# RSMDB CashingType=8 Apply Finance Approvals - 2026-05-20

## Scope

- Source: RSMDB read-only
- Target clone: Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520
- Operational BatchId: 1B5D8EB5-DD7E-4B63-8DDB-E7B00AAD558B
- ApprovalBatchId: 5234254A-A689-4502-A4F6-0172F74D66DB
- ScopeName: RSMDB_CashingType8
- Applied rule: ConfidenceScore >= 60 and SuggestedTargetAccountId IS NOT NULL
- Suspense usage: none

## Results

| Metric | Value |
|---|---:|
| Finance approvals applied | 97 |
| Resolution rows available for scoped accounts | 97 |
| AppliedBy | MigrationPilot |
| Notes | Score>=60 Mini Pilot Approval |
| RSMDB modified | No |
| Production modified | No |

## Safety

The apply script validates that every approved account has a real target ChartOfAccount row before writing approval/resolution rows. No receipts, journals, issues, owner payments, terminations, or 9088 records were migrated by this step.
