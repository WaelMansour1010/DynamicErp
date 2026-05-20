# PropertyMigrationToolkit Final Runner Guide - 2026-05-20

## Runner Location

`F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner`

## Build

```powershell
dotnet build "F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\DynamicErp.PropertyMigration.Runner.csproj" /nologo
```

Last validation: build succeeded with 0 warnings and 0 errors.

## Connection

Do not store passwords in JSON configs. Use:

```powershell
$env:PROPERTY_MIGRATION_SQL_CONNECTION = "Server=SERVER;User ID=USER;Password=PASSWORD;TrustServerCertificate=True;Encrypt=False;"
```

## DryRun Example

```powershell
dotnet run --project "F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\DynamicErp.PropertyMigration.Runner.csproj" -- --config "F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Config\rsmdb-stagingclone.dryrun.json" --dry-run
```

## Execute Example

Execute is allowed only against clone/sandbox targets with:

- `BackupVerified=true`
- `ExecutionPlanApproved=true`
- explicit non-empty `BatchId`
- safe target database name

```powershell
dotnet run --project "F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\DynamicErp.PropertyMigration.Runner.csproj" -- --config "path\customer.execute.json" --execute
```

## Supported Runner Stages

- Preflight
- CoreSetup
- Discovery
- Diagnostics
- MappingValidation
- IntelligenceDiscovery
- AccountDiscovery
- FinanceApproval
- OperationalLinkResolution
- ReceiptAllocationDiscovery
- CashingType8ReceiptCandidateBuild
- CashingType8FinanceApproval
- ApplyFinanceApprovals
- MiniAccountingPilotExecute
- MiniAccountingPilotRollback
- AccountingPilotExecute
- AccountingRollback
- Migration
- Reconciliation
- ReadyToTestDelivery

## Reports

Reports are written under:

`F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Reports`

The report includes status, timings, steps, errors, warnings, counts, and final summary.

## Exit Codes

- 0: completed without runner errors
- 1: failed or completed with errors
- 2: missing required command arguments
