# Property Migration Runner Report

- CustomerCode: `ADNAN-PILOT`
- SourceDatabase: `Adnan`
- TargetCloneDatabase: `Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520`
- BatchId: `2d524af4-1426-4f0b-9522-a047c434fdc9`
- MigrationMode: `Hybrid`
- ExecutionMode: `Execute`
- Status: `Failed`
- StartedAt: `2026-05-20T10:27:09.5938082+03:00`
- CompletedAt: `2026-05-20T10:27:10.1209963+03:00`

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
- `CoreSetup`: `Started` 
- `CoreSetup`: `Passed` 
- `Discovery`: `Started` 
- `Discovery`: `Passed` 
- `Diagnostics`: `Started` 
- `Diagnostics`: `Failed` Invalid object name 'Checks'.

## Messages
- `Runner` `Info`: Mode=Hybrid, Execution=Execute, BatchId=2d524af4-1426-4f0b-9522-a047c434fdc9
- `Preflight` `Info`: Source/target databases exist and target name passed clone safety guard.
- `00_ToolkitCore_ConfigAndXref_Generic.sql` `Info`: Executed target clone template.
- `CoreSetup` `Info`: Core config and batch rows are ready on the target clone.
- `Discovery_SELECT_ONLY_Generic.sql` `Info`: Executed SELECT/diagnostic template.
- `Diagnostics` `Error`: Invalid object name 'Checks'.
- `Runner` `Fatal`: Invalid object name 'Checks'.
