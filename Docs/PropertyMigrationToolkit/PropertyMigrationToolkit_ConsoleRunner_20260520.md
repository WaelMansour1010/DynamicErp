# Property Migration Console Runner

## Purpose

`DynamicErp.PropertyMigration.Runner` is the first practical runner for the Enterprise Property Migration Toolkit. It turns the toolkit from documentation and SQL templates into a controlled pipeline that can be run by a technical support engineer or senior implementer.

The runner does not make GoLive decisions. It prepares and validates clone migrations only.

## Location

- Project: `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner`
- Solution entry: `F:\Source Code\DynamicErp\MyERP.sln`
- Sample config: `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Config\adnan-pilot.sample.json`
- Reports: `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Reports`

## Supported Modes

- `Strict`: no AutoFix; unsafe records are blocked or excluded.
- `Tolerant`: maximizes migration coverage with logged fallbacks and review queue.
- `Hybrid`: default. Master data can use tolerant fallbacks, accounting remains strict.

## Pipeline

1. Preflight
2. Discovery
3. Diagnostics
4. Mapping Validation
5. Migration
6. Reconciliation
7. ReadyToTest Delivery

## Safety Gates

The runner blocks execution when:

- Target database is named like a known source/reference database: `Alromaizan`, `MyErp`, `Adnan`, `RSMDB`.
- Target database name does not contain `Clone`, `Sandbox`, `PropertyPilot`, `ReadyToTest`, `PilotClone`, or `Migration`.
- Execute mode is requested without `BackupVerified=true` and `ExecutionPlanApproved=true`.
- Accounting safety checks find `AccountId=NULL` or unbalanced journals.
- Suspense remains open without finance sign-off for GoLive readiness.

## DryRun

DryRun validates configuration, checks source/target database existence, prints the execution plan, and writes a Markdown report. It does not execute SQL templates and does not change the target clone.

```powershell
dotnet run --project "F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner" -- --config "F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Config\adnan-pilot.sample.json" --dry-run
```

## Execute Mode

Execute mode is clone-only and must be explicitly enabled with both CLI and config approval.

Required config values:

```json
"DryRun": false,
"BackupVerified": true,
"ExecutionPlanApproved": true
```

Command:

```powershell
dotnet run --project "F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner" -- --config "<customer-config.json>" --execute
```

## Current Implementation Scope

Implemented now:

- JSON config loading.
- CLI overrides for source DB, target DB, and mode.
- Strict/Tolerant/Hybrid selection.
- Clone-only guards.
- Source database read-only preflight validation.
- Target clone preflight validation.
- BatchId handling.
- Ordered pipeline orchestration.
- DryRun mode.
- Execute mode wiring for generic SQL templates.
- Markdown run report.

Still pending before non-developer handoff:

- Customer-specific mapping UI/editor.
- Rich dashboard for warnings and review queue.
- Interactive rerun for a single failed/reviewed record.
- Final production-grade Console packaging.
