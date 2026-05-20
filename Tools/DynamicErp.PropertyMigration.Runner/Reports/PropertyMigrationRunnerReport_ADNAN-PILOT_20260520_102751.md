# Property Migration Runner Report

- CustomerCode: `ADNAN-PILOT`
- SourceDatabase: `Adnan`
- TargetCloneDatabase: `Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520`
- BatchId: `2d524af4-1426-4f0b-9522-a047c434fdc9`
- MigrationMode: `Hybrid`
- ExecutionMode: `Execute`
- Status: `Completed`
- StartedAt: `2026-05-20T10:27:50.9366726+03:00`
- CompletedAt: `2026-05-20T10:27:51.3610543+03:00`

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
- `CoreSetup`: `Started` 
- `CoreSetup`: `Passed` 
- `Discovery`: `Started` 
- `Discovery`: `Passed` 
- `Diagnostics`: `Started` 
- `Diagnostics`: `Passed` 
- `MappingValidation`: `Started` 
- `MappingValidation`: `Passed` 
- `Migration`: `Started` 
- `Migration`: `Passed` 
- `Reconciliation`: `Started` 
- `Reconciliation`: `Passed` 
- `ReadyToTestDelivery`: `Started` 
- `ReadyToTestDelivery`: `Passed` 

## Messages
- `Runner` `Info`: Mode=Hybrid, Execution=Execute, BatchId=2d524af4-1426-4f0b-9522-a047c434fdc9
- `Preflight` `Info`: Source/target databases exist and target name passed clone safety guard.
- `00_ToolkitCore_ConfigAndXref_Generic.sql` `Info`: Executed target clone template.
- `CoreSetup` `Info`: Core config and batch rows are ready on the target clone.
- `Discovery_SELECT_ONLY_Generic.sql` `Info`: Executed SELECT/diagnostic template.
- `Diagnostics_Generic.sql` `Info`: Executed target clone template.
- `MappingValidation` `Info`: Warnings=0, AutoFixes=0, OpenReview=0.
- `Migration` `Info`: ControlledPipelineOnly: customer-specific migration templates are skipped because generic templates are placeholders.
- `Migration` `Info`: Core setup, diagnostics, reconciliation, and final summary still execute against the target clone.
- `Reconciliation_Generic.sql` `Info`: Executed SELECT/diagnostic template.
- `ReadyToTestDelivery` `Info`: Summary collected. Final report will be written locally; no GoLive action is performed.
