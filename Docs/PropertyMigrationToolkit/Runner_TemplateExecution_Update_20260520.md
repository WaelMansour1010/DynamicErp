# Runner Template Execution Update - 2026-05-20

## Changes

The Console Runner was updated so it is no longer limited to pipeline-only controlled executes.

## Implemented

- `CoreSetup` now executes both:
  - `00_ToolkitCore_ConfigAndXref_Generic.sql`
  - `01_SourceStagingTables_Generic.sql`
- Added `SkipStages` config support.
- `SkipCustomerSpecificMigrationTemplates` remains available only as a deprecated pipeline-only escape hatch.
- Default value for `SkipCustomerSpecificMigrationTemplates` is now `false`.
- Each SQL template execution writes `Started`, `Completed`, or `Failed` rows to `PropertyMigrationRunLog` when the log table exists.
- Runner messages include `RowsAffected` from executed SQL batches where SQL Server reports it.

## Safety Gates Preserved

- Execute requires clone-safe target name.
- Execute requires `BackupVerified=true` and `ExecutionPlanApproved=true`.
- Source DB is not used as a target connection.
- Accounting safety gate remains after accounting migration templates.
- DryRun still does not execute SQL templates.

## RSMDB Runner Readiness

Runner can now run the generic template sequence against an RSMDB clone after the RSMDB staging mapping script is reviewed. Until then, RSMDB should only use Discovery/DryRun/Diagnostics.
