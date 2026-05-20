# RSMDB Staging Clone Setup - 2026-05-20

## Purpose

Create a safe DynamicErp target clone for RSMDB staging only. This phase does not execute migration templates and does not migrate data into live DynamicErp business tables.

## Clone

| Item | Value |
|---|---|
| Target clone database | `Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520` |
| Source used for clone structure | `Alromaizan` via COPY_ONLY backup/restore |
| Source VB6 database | `RSMDB` read-only |
| BatchId used for staging load | `1b5d8eb5-dd7e-4b63-8ddb-e7b00aad558b` |
| Mode | `Hybrid` |
| CutoffDate | `2026-05-20` |

## Safety Confirmation

- Target name contains `PropertyPilot` and `StagingClone`.
- Target is not `RSMDB`, `Alromaizan`, `MyErp`, or `Adnan`.
- `RSMDB` was used only as read source.
- No generic migration templates were executed.
- No changes were made to `RSMDB` or `Alromaizan` original.

## Setup Executed On Clone Only

- `00_ToolkitCore_ConfigAndXref_Generic.sql`
- `01_SourceStagingTables_Generic.sql`
- `PropertyMigrationToolkit_RSMDBConfig_DRAFT_20260520.sql`
- `RSMDB_StagingMapping_SELECT_TO_STAGING_DRAFT_20260520.sql`

## Toolkit Objects Confirmed

The clone now contains toolkit core/staging objects including:

- `PropertyMigrationBatch`
- `PropertyMigrationConfig`
- `PropertyMigrationEntityMap`
- `PropertyMigrationWarning`
- `PropertyMigrationError`
- `PropertyMigrationReviewQueue`
- `PropertyMigrationReconciliationResult`
- `PropertyMigrationRunLog`
- `PropertyMigrationSource*`
- Owner staging tables: `PropertyMigrationSourceOwner`, `PropertyMigrationSourcePropertyOwner`, `PropertyMigrationSourceOwnerBalance`, `PropertyMigrationSourceOwnerPayment`

## Runner DryRun

Runner DryRun was also executed for safety/preflight validation only.

Report:
`F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Reports\PropertyMigrationRunnerReport_RSMDB-STAGING_20260520_111427.md`
