# Phase 10A - Accounting Dry Run Results
Date: 2026-05-20
Target Clone: `Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520`
BatchId: `A10AD000-0000-4000-9000-202605200010`

## Execution Result
| Item | Count / Amount |
|---|---:|
| Cash receipts migrated | 753 |
| Receipt detail rows migrated | 811 |
| Receipt amount | 12,719,724.4580 |
| Journal entries migrated | 753 |
| Journal detail lines migrated | 2,360 |
| Journal debit | 12,802,048.4785 |
| Journal credit | 12,802,048.4785 |
| Cash issue candidates excluded | 6 |
| Advance staging retained | 14 rows / 55,592.8900 |

## Stored Procedures Created
- `usp_PropertyPilot_MigrateCashReceipts_Adnan`
- `usp_PropertyPilot_MigrateCashIssues_Adnan`
- `usp_PropertyPilot_MigrateJournalEntries_Adnan`
- `usp_PropertyPilot_MigrateAdvancePayments_Adnan`
- `usp_PropertyPilot_ReconcileAccounting_Adnan`
- `usp_PropertyPilot_RollbackAccountingBatch_Adnan`

## Execution Output
Raw output: `Phase10A_AccountingMigrationExecution_output.txt`.
