# DynamicErp Database Migration Runner

Lightweight SQL Server migration runner for DynamicErp. It uses plain `.sql` files, SHA-256 hashes, and `dbo.DatabaseMigrationHistory`. It does not use EF Migrations or heavy external tools.

## Usage

Dry run:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\DatabaseMigrationRunner\DatabaseMigrationRunner.ps1 `
  -Server ".\SQL2019" `
  -Database "MyErp_Test" `
  -Mode DryRun
```

Apply:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\DatabaseMigrationRunner\DatabaseMigrationRunner.ps1 `
  -Server ".\SQL2019" `
  -Database "MyErp_Test" `
  -Mode Apply `
  -StopOnError `
  -ReleaseNo "2026.05.09"
```

Filter by module:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\DatabaseMigrationRunner\DatabaseMigrationRunner.ps1 `
  -ConnectionString "Data Source=.\SQL2019;Initial Catalog=MyErp_Test;Integrated Security=True" `
  -Mode ReportOnly `
  -ModuleName POS
```

Use custom migration folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\DatabaseMigrationRunner\DatabaseMigrationRunner.ps1 `
  -Server ".\SQL2019" `
  -Database "MyErp_Test" `
  -MigrationPath ".\Database\Migrations" `
  -Mode DryRun
```

## Rules

- Run `DryRun` first for every database.
- Apply only to a test database before production.
- Never edit a migration after it was applied. Create a new numbered migration instead.
- A same `ScriptName` with a different hash is blocked as a hash mismatch.
- Stored procedures must use SQL Server 2012 compatible `DROP PROCEDURE` then `CREATE PROCEDURE`.
- Table changes must be idempotent with `IF NOT EXISTS`, `COL_LENGTH`, or `OBJECT_ID` guards.
- Data fixes must be bounded and documented with explicit `WHERE` clauses.

