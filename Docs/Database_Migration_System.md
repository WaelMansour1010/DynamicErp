# DynamicErp Database Migration System

## Goal

DynamicErp has a lightweight migration system for SQL Server 2012 without EF Migrations. The primary customer workflow is the MainErp web screen: **Database Update Manager**. It gives every database a clear record of applied scripts, prevents accidental re-runs, detects edited scripts, and supports dry-run reporting before changes are applied.

The PowerShell runner remains available for developers, release preparation, and emergency fallback. It is not the routine customer update path.

## Files

- `Database/Create_DatabaseMigrationHistory.sql`: creates `dbo.DatabaseMigrationHistory`, `dbo.DatabaseMigrationRun`, and `dbo.DatabaseMigrationRunDetail`.
- `Database/Migrations/{Module}`: canonical migration source.
- `Areas/MainErp/Controllers/DatabaseMigrationController.cs`: web entry point.
- `Areas/MainErp/Services/DatabaseMigration/DatabaseMigrationService.cs`: shared discovery, hashing, validation, dry-run, apply, and reporting logic.
- `Areas/MainErp/Views/DatabaseMigration`: web UI.
- `Tools/DatabaseMigrationRunner/DatabaseMigrationRunner.ps1`: developer/fallback runner tool.
- `Tools/DatabaseMigrationRunner/README.md`: command examples.

## History And Run Tables

`dbo.DatabaseMigrationHistory` records each script attempt with script name, path, SHA256 hash, module, applied user, machine, database, duration, success, error, batch, and release.

Successful applications are unique by `ScriptName + ScriptHash`, so an already applied script is skipped. If the same `ScriptName` appears with a different hash, the web screen and runner report a hash mismatch and block apply.

Each web apply operation is also logged as a batch in `dbo.DatabaseMigrationRun` and per-script details in `dbo.DatabaseMigrationRunDetail`.

## Web Update Manager

Open from MainErp as a system administrator:

```text
/MainErp/DatabaseMigration
```

The screen shows server/database status, summary cards, pending updates, applied history, failures, whitelisted sources, execution log, preview warnings, dry run, apply selected/all, and CSV export. Access is server-side protected and limited to MainErp admin users. The screen never displays the connection string or password.

## Web Configuration

```xml
<add key="DatabaseMigrationFolders" value="~/Database/Migrations" />
<add key="DatabaseMigrationEnvironment" value="Current" />
```

Only files under configured folders are discoverable. User input cannot provide an arbitrary script path.

## Migration Naming

Use a four-digit prefix and module name:

```text
0001_POS_CreateTables.sql
0002_POS_SaveTransaction.sql
0003_MainErp_AddReports.sql
```

The system sorts by the leading number, then file name, then path. Unnumbered legacy files are shown as unclassified and are not applied automatically.

## Header Required

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

## Web Modes

- `DryRun`: compares files to history and displays pending/mismatch lists. Does not create or change database objects.
- `ApplySelected`: applies selected pending scripts after typing `APPLY` and confirming backup.
- `ApplyAll`: applies all pending classified scripts after confirmation.
- `PreviewScript`: read-only script display with dangerous SQL warnings.
- `ExportReport`: CSV summary for release evidence.

## SQL Rules

Stored procedures must use `DROP PROCEDURE` followed by `CREATE PROCEDURE` and avoid `CREATE OR ALTER`. Table changes must be idempotent with `OBJECT_ID`, `COL_LENGTH`, or `sys.columns`. Data fixes must be bounded and avoid broad `UPDATE` or `DELETE` without `WHERE`.

## Production Rule

1. Run Dry Run from the web screen and export the report.
2. Confirm a database backup exists.
3. Run ApplySelected or ApplyAll only after pending scripts and hash warnings are reviewed.

## Local Verification Performed

Verified against local test database `DynamicErp_MigrationWeb_Test` on `.` SQL2019: admin access, normal-user block, dry run, preview, apply selected, apply all, repeat apply skip, hash mismatch detection, failed script logging, and CSV export without password text.
