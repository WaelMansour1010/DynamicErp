# Database Update Manager

## Overview

Database Update Manager is the MainErp web screen for managing DynamicErp SQL migrations at customer sites. It replaces manual PowerShell execution as the normal update path while keeping the old runner as a developer/fallback helper.

Open:

```text
/MainErp/DatabaseMigration
```

## Permissions

The screen is restricted to MainErp system administrators. The permission constants are:

- `DatabaseMigration.View`
- `DatabaseMigration.Manage`
- `DatabaseMigration.Apply`

Normal users are blocked server-side with HTTP 403 even if they know the URL.

## What The Screen Shows

- Current database name and SQL Server name.
- Environment label from `DatabaseMigrationEnvironment`.
- Last applied migration date.
- Applied, pending, failed, and hash mismatch counts.
- Pending updates, applied history, failures, sources, and execution log.
- Script preview with warnings for dangerous SQL patterns.
- CSV export report.

The screen does not display connection strings or passwords.

## Migration Sources

Sources are configured in `Web.config`:

```xml
<add key="DatabaseMigrationFolders" value="~/Database/Migrations" />
<add key="DatabaseMigrationEnvironment" value="Current" />
```

Only whitelisted folders are scanned. The UI never accepts a script path from the browser.

## Naming Rules

Use a four-digit sequence, module name, and purpose:

```text
0001_POS_CreateTables.sql
0002_POS_SaveTransaction.sql
0003_MainErp_AddReports.sql
```

Unnumbered legacy files are treated as unclassified and are not applied automatically.

## Required Header

Each migration should start with:

```sql
/*
Migration number: 0001
Module: POS
Purpose: Short purpose
Safe to rerun? Yes/No
Dependencies: None
Date: YYYY-MM-DD
Author/Agent: Name
*/
```

## Dry Run

Dry Run reads the files, calculates SHA256 hashes, compares them to `dbo.DatabaseMigrationHistory`, and shows pending scripts and warnings. It does not create metadata tables and does not execute SQL.

## Apply

Apply requires typing `APPLY` and accepting the backup confirmation. It can run selected scripts or all pending classified scripts. Each script is executed server-side and logged in `dbo.DatabaseMigrationHistory`, `dbo.DatabaseMigrationRun`, and `dbo.DatabaseMigrationRunDetail`.

## Preview

Preview is read-only and warns for `DROP TABLE`, `TRUNCATE`, `ALTER COLUMN`, dynamic SQL, `UPDATE` without `WHERE`, and `DELETE` without `WHERE`.

## Failure Handling

Failed scripts are saved with `Success = 0` and a run detail row with `Status = Failed`. The UI shows a safe summary; detailed errors stay in the server log and migration tables.

## Hash Mismatch

If a script name already exists in history with a different hash, the screen marks it as `Hash mismatch` and excludes it from automatic apply. Restore the original script or create a new migration number; do not hide the mismatch by overwriting history.

## Production Checklist

- Run Dry Run first.
- Export the report.
- Confirm a current database backup exists.
- Review dangerous SQL warnings.
- Apply selected scripts when possible.
- Reopen the screen after apply and confirm pending count is zero.
