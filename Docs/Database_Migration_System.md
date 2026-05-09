# DynamicErp Database Migration System

## Goal

DynamicErp now has a lightweight migration system for SQL Server 2012 without EF Migrations. It gives every database a clear record of applied scripts, prevents accidental re-runs, detects edited scripts, and supports dry-run reporting before changes are applied.

## Files

- `Database/Create_DatabaseMigrationHistory.sql`: creates `dbo.DatabaseMigrationHistory`.
- `Database/Migrations/{Module}`: canonical migration source.
- `Tools/DatabaseMigrationRunner/DatabaseMigrationRunner.ps1`: runner tool.
- `Tools/DatabaseMigrationRunner/README.md`: command examples.

## History Table

The runner records each attempt in `dbo.DatabaseMigrationHistory`:

- `MigrationId`
- `ScriptName`
- `ScriptPath`
- `ScriptHash`
- `ModuleName`
- `AppliedOn`
- `AppliedBy`
- `MachineName`
- `DatabaseName`
- `DurationMs`
- `Success`
- `ErrorMessage`
- `BatchNo`
- `ReleaseNo`

Successful applications are unique by `ScriptName + ScriptHash`, so an already applied script is skipped. If the same `ScriptName` appears with a different hash, the runner reports a hash mismatch and blocks `Apply`.

## Migration Naming

Use a four-digit prefix and module name:

```text
0001_POS_CreateTables.sql
0002_POS_SaveTransaction.sql
0003_MainErp_AddReports.sql
```

The runner sorts by the leading number, then file name, then path.

## Header Required

Every migration must start with:

```sql
/*
Migration number: 0001
Module: POS
Purpose: Short purpose
Safe to rerun? Yes/No
Dependencies: 0000_Something.sql or None
Date: YYYY-MM-DD
Author/Agent: Name
*/
```

## Recommended Structure

Use `Database/Migrations` as the canonical source:

```text
Database/
  Migrations/
    POS/
    MainErp/
    Shared/
    Reports/
    Sync/
```

Keep old folders as historical source until the conversion is complete:

- `Areas/Pos/Sql`
- `Areas/MainErp/Sql`
- `Areas/Reports/Sql`
- `Areas/Sync/Sql`
- `Scripts`

Release package SQL under `Releases/*/Sql` should be treated as packaged copies, not canonical migration sources.

## Runner Modes

- `DryRun`: compares files to history and prints pending/skipped/mismatch lists. Does not create or change database objects.
- `ReportOnly`: same safe reporting behavior; use for audit pipelines.
- `Apply`: creates the history table if needed, applies only pending scripts, logs success/failure, and stops on hash mismatch.

## Example Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\DatabaseMigrationRunner\DatabaseMigrationRunner.ps1 `
  -Server ".\SQL2019" `
  -Database "MyErp_Test" `
  -Mode DryRun
```

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\DatabaseMigrationRunner\DatabaseMigrationRunner.ps1 `
  -Server ".\SQL2019" `
  -Database "MyErp_Test" `
  -Mode Apply `
  -StopOnError `
  -ReleaseNo "2026.05.09"
```

## SQL Rules

Stored procedures:

- Use `DROP PROCEDURE` followed by `CREATE PROCEDURE`.
- Put `GO` between drop and create.
- Avoid `CREATE OR ALTER`, because SQL Server 2012 does not support it.

Table/column changes:

- Use guards such as `OBJECT_ID`, `COL_LENGTH`, or `sys.columns`.
- Do not add a column without checking if it already exists.

Data fixes:

- Must be bounded and documented.
- Do not use broad `UPDATE` or `DELETE` without a clear `WHERE`.
- Prefer idempotent predicates that make a second run harmless.

## Production Rule

Production updates must be a two-step process:

1. Run `DryRun` or `ReportOnly` and attach the report to the release.
2. Run `Apply` only after the pending scripts and hash warnings are reviewed.

## Required Test Checklist

Run against a test database only:

- `DryRun` lists pending scripts.
- `Apply` applies new scripts.
- Re-running `Apply` skips the same scripts.
- Editing an applied script produces a hash mismatch.
- A failing script is logged with `Success = 0` and `ErrorMessage`.
- Final output includes pending, applied, skipped, failed, and mismatch sections.

## Local Verification Performed

Verified on local test database `DynamicErp_MigrationRunner_Test` on `.\SQL2019`:

- `DryRun` listed 3 pending example migrations and applied nothing.
- `Apply` applied the 3 example migrations and recorded them in `dbo.DatabaseMigrationHistory`.
- A second `Apply` skipped the same 3 scripts.
- A temporary hash change in the test history produced a hash mismatch warning and exit code `2`; the original hash was restored.
- A temporary failing migration outside the repository logged `Success = 0` and the divide-by-zero error message.
- `ReportOnly` showed 3 skipped scripts and no pending work after the tests.
