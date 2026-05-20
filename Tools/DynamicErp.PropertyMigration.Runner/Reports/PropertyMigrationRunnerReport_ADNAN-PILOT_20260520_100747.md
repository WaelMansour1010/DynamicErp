# Property Migration Runner Report

- CustomerCode: `ADNAN-PILOT`
- SourceDatabase: `Adnan`
- TargetCloneDatabase: `Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520`
- BatchId: `00000000-0000-0000-0000-000000000001`
- MigrationMode: `Hybrid`
- ExecutionMode: `DryRun`
- Status: `Completed`
- StartedAt: `2026-05-20T10:07:47.1425019+03:00`
- CompletedAt: `2026-05-20T10:07:47.8444774+03:00`

## Summary

- Contracts: `0`
- Receipts: `0`
- Issues: `0`
- Journals: `0`
- Warnings: `0`
- Errors: `0`
- AutoFixes: `0`
- SuspenseItems: `0`
- OpenReviewItems: `0`

## Steps
- `Preflight`: `Started` 
- `Preflight`: `Passed` 
- `ExecutionPlan`: `Started` 
- `ExecutionPlan`: `Passed` 
- `ReadyToTestDelivery`: `Started` 
- `ReadyToTestDelivery`: `Passed` 

## Messages
- `Runner` `Info`: Mode=Hybrid, Execution=DryRun, BatchId=00000000-0000-0000-0000-000000000001
- `Preflight` `Info`: Source/target databases exist and target name passed clone safety guard.
- `ExecutionPlan` `Info`: DryRun only. No SQL templates will be executed and no database objects will be changed.
- `ExecutionPlan` `Info`: Planned stages: CoreSetup, Discovery, Diagnostics, MappingValidation, Migration, Reconciliation, ReadyToTestDelivery.
- `ExecutionPlan` `Info`: Selected modules: Accounting=False, Receipts=False, Issues=False, Journals=False, AdvancePayments=True, Terminations=False.
- `ExecutionPlan` `Info`: Safety: target guard passed; execute requires BackupVerified=False, ExecutionPlanApproved=False.
- `ReadyToTestDelivery` `Info`: DryRun mode: final database summary was not queried. Report contains the planned run only.
