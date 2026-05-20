# Property Migration Runner Report

- CustomerCode: `RSMDB-DISCOVERY`
- SourceDatabase: `RSMDB`
- TargetCloneDatabase: `Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520`
- BatchId: `64ab9a10-1b7e-4e9a-9a93-98bfc9bcb1eb`
- MigrationMode: `Hybrid`
- ExecutionMode: `DryRun`
- Status: `Completed`
- StartedAt: `2026-05-20T10:54:55.7154403+03:00`
- CompletedAt: `2026-05-20T10:54:56.3364583+03:00`

## Summary

- Contracts: `283`
- Receipts: `753`
- Issues: `0`
- Journals: `754`
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
- `Preflight`: `Passed` 
- `ReadOnlySafetyValidation`: `Started` 
- `ReadOnlySafetyValidation`: `Passed` 
- `ExecutionPlan`: `Started` 
- `ExecutionPlan`: `Passed` 
- `ReadyToTestDelivery`: `Skipped` Skipped by config.

## Messages
- `Runner` `Info`: Mode=Hybrid, Execution=DryRun, BatchId=00000000-0000-0000-0000-000000000000
- `Preflight` `Info`: Generated new BatchId=64ab9a10-1b7e-4e9a-9a93-98bfc9bcb1eb.
- `Preflight` `Info`: Source/target databases exist and target name passed clone safety guard.
- `ReadOnlySafetyValidation` `Info`: Contracts=283, Receipts=753, Journals=754, AccountIdNullLines=0, UnbalancedJournals=0.
- `ExecutionPlan` `Info`: DryRun only. No SQL templates will be executed and no database objects will be changed.
- `ExecutionPlan` `Info`: Planned stages: CoreSetup, Discovery, Diagnostics, MappingValidation, Migration, Reconciliation, ReadyToTestDelivery.
- `ExecutionPlan` `Info`: Selected modules: Accounting=False, Receipts=False, Issues=False, Journals=False, AdvancePayments=True, Terminations=False.
- `ExecutionPlan` `Info`: ControlledPipelineOnly=True.
- `ExecutionPlan` `Info`: SkipStages=Migration,Reconciliation,ReadyToTestDelivery.
- `ExecutionPlan` `Info`: Safety: target guard passed; execute requires BackupVerified=False, ExecutionPlanApproved=False.
- `ReadyToTestDelivery` `Info`: Skipped by config.
