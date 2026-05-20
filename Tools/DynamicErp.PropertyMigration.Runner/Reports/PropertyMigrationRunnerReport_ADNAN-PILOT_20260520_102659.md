# Property Migration Runner Report

- CustomerCode: `ADNAN-PILOT`
- SourceDatabase: `Adnan`
- TargetCloneDatabase: `Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520`
- BatchId: `2d524af4-1426-4f0b-9522-a047c434fdc9`
- MigrationMode: `Hybrid`
- ExecutionMode: `DryRun`
- Status: `Completed`
- StartedAt: `2026-05-20T10:26:59.5465170+03:00`
- CompletedAt: `2026-05-20T10:26:59.9556429+03:00`

## Summary

- Contracts: `283`
- Receipts: `753`
- Issues: `0`
- Journals: `754`
- Warnings: `0`
- Errors: `0`
- AutoFixes: `0`
- SuspenseItems: `0`
- OpenReviewItems: `0`

## Steps
- `Preflight`: `Started` 
- `Preflight`: `Passed` 
- `ReadOnlySafetyValidation`: `Started` 
- `ReadOnlySafetyValidation`: `Passed` 
- `ExecutionPlan`: `Started` 
- `ExecutionPlan`: `Passed` 
- `ReadyToTestDelivery`: `Started` 
- `ReadyToTestDelivery`: `Passed` 

## Messages
- `Runner` `Info`: Mode=Hybrid, Execution=DryRun, BatchId=2d524af4-1426-4f0b-9522-a047c434fdc9
- `Preflight` `Info`: Source/target databases exist and target name passed clone safety guard.
- `ReadOnlySafetyValidation` `Info`: Contracts=283, Receipts=753, Journals=754, AccountIdNullLines=0, UnbalancedJournals=0.
- `ExecutionPlan` `Info`: DryRun only. No SQL templates will be executed and no database objects will be changed.
- `ExecutionPlan` `Info`: Planned stages: CoreSetup, Discovery, Diagnostics, MappingValidation, Migration, Reconciliation, ReadyToTestDelivery.
- `ExecutionPlan` `Info`: Selected modules: Accounting=True, Receipts=True, Issues=False, Journals=True, AdvancePayments=True, Terminations=False.
- `ExecutionPlan` `Info`: ControlledPipelineOnly=True.
- `ExecutionPlan` `Info`: Safety: target guard passed; execute requires BackupVerified=True, ExecutionPlanApproved=True.
- `ReadyToTestDelivery` `Info`: DryRun mode: final database summary was not queried. Report contains the planned run only.
