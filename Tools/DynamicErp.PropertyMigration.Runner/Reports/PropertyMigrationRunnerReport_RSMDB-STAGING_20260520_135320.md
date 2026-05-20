# Property Migration Runner Report

- CustomerCode: `RSMDB-STAGING`
- SourceDatabase: `RSMDB`
- TargetCloneDatabase: `Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520`
- BatchId: `f496f258-05c6-4f84-b6ad-9686b12b8685`
- MigrationMode: `Hybrid`
- ExecutionMode: `DryRun`
- Status: `Completed`
- StartedAt: `2026-05-20T13:53:20.3277086+03:00`
- CompletedAt: `2026-05-20T13:53:20.7141399+03:00`

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
- `Preflight` `Info`: Generated new BatchId=f496f258-05c6-4f84-b6ad-9686b12b8685.
- `Preflight` `Info`: Source/target databases exist and target name passed clone safety guard.
- `ReadOnlySafetyValidation` `Info`: Contracts=258, Receipts=0, Journals=1, AccountIdNullLines=0, UnbalancedJournals=0.
- `ExecutionPlan` `Info`: DryRun only. No SQL templates will be executed and no database objects will be changed.
- `ExecutionPlan` `Info`: Planned stages: CoreSetup, Discovery, Diagnostics, MappingValidation, Migration, Reconciliation, ReadyToTestDelivery.
- `ExecutionPlan` `Info`: Intelligence stages enabled: IntelligenceDiscovery, JournalResolution, ReceiptMatching, OwnerPaymentClassification, ConfidenceScoring, ReviewReduction.
- `ExecutionPlan` `Info`: Account intelligence stages enabled: AccountDiscovery, AccountMatching, AccountConfidenceScoring, AccountFamilyDetection, AccountReviewQueue.
- `ExecutionPlan` `Info`: Receipt allocation discovery stage enabled: VB6/schema/data evidence enrichment only; no receipt or journal migration.
- `ExecutionPlan` `Info`: CashingType8ReceiptCandidateBuild stage enabled: builds property receipt pilot candidates and finance review pack only.
- `ExecutionPlan` `Info`: CashingType8FinanceApproval stage enabled: creates scoped finance review pack and approval impact simulation only; no accounting migration.
- `ExecutionPlan` `Info`: Selected modules: Accounting=False, Receipts=False, Issues=False, Journals=False, AdvancePayments=True, Terminations=False.
- `ExecutionPlan` `Info`: ControlledPipelineOnly=True.
- `ExecutionPlan` `Info`: SkipStages=Migration,Reconciliation,ReadyToTestDelivery.
- `ExecutionPlan` `Info`: Safety: target guard passed; execute requires BackupVerified=False, ExecutionPlanApproved=False.
- `ReadyToTestDelivery` `Info`: Skipped by config.
