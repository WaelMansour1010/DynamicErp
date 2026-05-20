# DynamicErp.PropertyMigration.Runner

Console runner for the Enterprise Property Migration Toolkit.

## Safety Defaults

- DryRun is the default unless `--execute` is supplied.
- Execute mode requires `BackupVerified=true` and `ExecutionPlanApproved=true` in the config.
- Target database name must contain one of: `Clone`, `Sandbox`, `PropertyPilot`, `ReadyToTest`, `PilotClone`, `Migration`.
- The runner blocks direct targets named `Alromaizan`, `MyErp`, `Adnan`, or `RSMDB`.
- Source database is only validated/read; it is never used as the target connection.

## Connection String\n\nUse either `ConnectionString` in the JSON for a local private config, or set `ConnectionStringEnvironmentVariable` and provide the value outside source control. Do not commit real passwords in shared configs.\n\n```powershell\n$env:PROPERTY_MIGRATION_SQL_CONNECTION = "Server=<server>;User ID=<user>;Password=<password>;TrustServerCertificate=True;Encrypt=False;"\n```\n\n## Dry Run

```powershell
dotnet run --project "F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner" -- --config "F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Config\adnan-pilot.sample.json" --dry-run
```

## Execute On Clone Only

Before execute, use a clone target and set these config values intentionally:

```json
"DryRun": false,
"BackupVerified": true,
"ExecutionPlanApproved": true
```

Then run:

```powershell
dotnet run --project "F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner" -- --config "<config.json>" --execute
```

## Current Version Scope

This first practical version provides:

- Config JSON loading.
- Source/target database selection.
- Strict/Tolerant/Hybrid mode selection.
- Clone-only target guard.
- Preflight validation.
- Ordered pipeline orchestration.
- DryRun planning.
- Execute mode support for clone-safe toolkit SQL templates.
- Markdown report output.

Customer-specific source extraction and the final production-grade migration stored procedures remain controlled by the generic SQL templates and customer mapping configs.

