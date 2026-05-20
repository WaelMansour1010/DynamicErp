# Property Migration Runner - First Controlled Execute

Date: 2026-05-20

## Scope

This was the first controlled `--execute` run for `DynamicErp.PropertyMigration.Runner` against a clone database only.

No production database was used as a target. The source database `Adnan` was only validated/read by name and was not modified.

## Configuration

- Config file: `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Config\adnan-readytotest.execute.json`
- CustomerCode: `ADNAN-PILOT`
- SourceDatabase: `Adnan`
- TargetCloneDatabase: `Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520`
- MigrationMode: `Hybrid`
- CutoffDate: `2026-05-20`
- BatchId: `2d524af4-1426-4f0b-9522-a047c434fdc9`
- Connection password: not stored in config; supplied through `PROPERTY_MIGRATION_SQL_CONNECTION`.

## Safety Confirmation

- Target name contains `PropertyPilot` and `ReadyToTest`.
- Target is not named `Alromaizan`, `MyErp`, `Adnan`, or `RSMDB`.
- `BackupVerified=true` was set for this clone execution.
- `ExecutionPlanApproved=true` was set for this clone execution.
- No SQL was executed against `Adnan`, `RSMDB`, `Alromaizan`, or `MyErp` as a target.

## DryRun Result

DryRun succeeded.

- DryRun report: `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Reports\PropertyMigrationRunnerReport_ADNAN-PILOT_20260520_102659.md`
- Preflight: Passed
- Target safety guard: Passed
- Read-only accounting safety validation: Passed
- Contracts detected: `283`
- Receipts detected: `753`
- Journals detected: `754`
- AccountId NULL lines: `0`
- Unbalanced journals: `0`

## Controlled Execute Result

Execute succeeded on the clone only.

- Execute report: `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Reports\PropertyMigrationRunnerReport_ADNAN-PILOT_20260520_103053.md`
- Final status: `Completed`
- Preflight: Passed
- CoreSetup: Passed
- Discovery: Passed
- Diagnostics: Passed
- MappingValidation: Passed
- Migration: Passed as controlled pipeline only
- Reconciliation: Passed
- ReadyToTestDelivery: Passed

## Important Migration Note

The generic customer migration templates are still placeholders that require customer-specific SELECT/mapping blocks. Therefore this first controlled execute intentionally used:

- `SkipCustomerSpecificMigrationTemplates=true`

This proves the runner can execute the operational pipeline on a clone, create/use toolkit control tables, run diagnostics, run reconciliation, and produce a final report. It does not claim that the generic templates performed a fresh data migration in this run.

## Validation Metrics After Execute

- Contracts: `283`
- Cash receipts: `753`
- Cash issues: `0`
- Journal entries: `754`
- Journal lines: `2363`
- Opening Balance: `1,156,544.6600`
- Future gross after cutoff: `19,234,398.7085`
- Advance payments: `55,592.8900`
- Net remain after advance: `19,178,805.8185`
- AccountId NULL lines: `0`
- Unbalanced journals: `0`
- Test cash receipts: `0`
- Test cash issues: `0`
- Test terminations: `0`
- Runner warnings for this batch: `0`
- Runner errors for this batch: `0`
- Runner autofixes for this batch: `0`
- Open suspense items for this batch: `0`
- Open review items for this batch: `0`
- Excluded records for this batch: `0`

## Issue Found And Fixed During First Execute

The first execute attempt reached CoreSetup and Discovery but failed in `Diagnostics_Generic.sql` because the `Checks` CTE was reused across multiple statements. SQL Server CTE scope is one statement only.

Fix applied:

- Replaced the reusable CTE with `#PropertyMigrationDiagnosticChecks` temp table.
- Re-ran controlled execute successfully on the same clone and BatchId.

## Decision

The runner is now proven beyond DryRun: it can perform a controlled execute pipeline on a safe clone.

It is ready for the next controlled experiment for `RSMDB`, but only as a pipeline/diagnostics runner until RSMDB-specific mapping and migration templates are reviewed and approved.
