# Property Migration Runner Report

- CustomerCode: `LOCAL-DEBUG-WEBTEST`
- SourceDatabase: `RSMDB`
- TargetCloneDatabase: `Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520`
- BatchId: `7d870143-0216-4949-813f-43ea428ad048`
- MigrationMode: `Hybrid`
- ExecutionMode: `DryRun`
- Status: `Completed`
- StartedAt: `2026-05-20T14:10:00.5119348+03:00`
- CompletedAt: `2026-05-20T14:10:00.8993411+03:00`

## Summary

- Contracts: `258`
- Receipts: `0`
- Issues: `0`
- Journals: `1`
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
- `Preflight` `Info`: Generated new BatchId=7d870143-0216-4949-813f-43ea428ad048.
- `Preflight` `Info`: Source/target databases exist and target name passed clone safety guard.
- `ReadOnlySafetyValidation` `Info`: Contracts=258, Receipts=0, Journals=1, AccountIdNullLines=0, UnbalancedJournals=0.
- `ExecutionPlan` `Info`: DryRun only. No SQL templates will be executed and no database objects will be changed.
- `ExecutionPlan` `Info`: Planned stages: CoreSetup, Discovery, Diagnostics, MappingValidation, Migration, Reconciliation, ReadyToTestDelivery.
- `ExecutionPlan` `Info`: Selected modules: Accounting=False, Receipts=False, Issues=False, Journals=False, AdvancePayments=True, Terminations=False.
- `ExecutionPlan` `Info`: ControlledPipelineOnly=True.
- `ExecutionPlan` `Info`: SkipStages=Migration,Reconciliation,ReadyToTestDelivery.
- `ExecutionPlan` `Info`: Safety: target guard passed; execute requires BackupVerified=False, ExecutionPlanApproved=False.
- `ReadyToTestDelivery` `Info`: Skipped by config.
