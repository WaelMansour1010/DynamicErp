# Property Migration Runner Report

- CustomerCode: `LOCAL-DEBUG-WEBTEST`
- SourceDatabase: `RSMDB`
- TargetCloneDatabase: `Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520`
- BatchId: `8d82d528-780b-4995-903a-67eee8e79ca5`
- MigrationMode: `Hybrid`
- ExecutionMode: `DryRun`
- Status: `Failed`
- StartedAt: `2026-05-20T14:09:42.7808768+03:00`
- CompletedAt: `2026-05-20T14:09:43.3722929+03:00`

## Summary

- Contracts: `0`
- Receipts: `0`
- Issues: `0`
- Journals: `0`
- JournalLines: `0`
- AccountIdNullLines: `0`
- UnbalancedJournals: `0`
- Warnings: `0`
- Errors: `0`
- AutoFixes: `0`
- SuspenseItems: `0`
- OpenReviewItems: `0`
- ExcludedRecords: `0`
- ReconciliationResults: `0`

## Steps
- `Preflight`: `Started` 
- `Preflight`: `Failed` Source database not found: RSMDB

## Messages
- `Runner` `Info`: Mode=Hybrid, Execution=DryRun, BatchId=00000000-0000-0000-0000-000000000000
- `Preflight` `Info`: Generated new BatchId=8d82d528-780b-4995-903a-67eee8e79ca5.
- `Preflight` `Error`: Source database not found: RSMDB
- `Runner` `Fatal`: Source database not found: RSMDB
