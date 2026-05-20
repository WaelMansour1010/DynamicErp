# PropertyMigrationToolkit Final Packaging Guide - 2026-05-20

## Packaging Goal

The toolkit should be copied and used as a coherent internal migration package, not as isolated SQL snippets.

## Current Package Roots

- Docs and SQL: `F:\Source Code\DynamicErp\Docs\PropertyMigrationToolkit`
- Runner: `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner`
- Runner configs: `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Config`
- Runner reports: `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Reports`

## Logical Folder Structure

The following folders now exist for packaged deliverables:

- `Docs`
- `Sql`
- `Configs`
- `Samples`
- `Runner`
- `Reports`
- `Templates`
- `Safety`
- `Intelligence`
- `Accounting`
- `Operational`
- `Rollback`
- `Archive`

Historical files remain in the root to preserve paths referenced by reports/configs. Future packaging can copy stable files into these folders without breaking existing references.

## Customer Package Contents

Each customer package should include:

- Customer config JSON
- Discovery report
- Diagnostics report
- Mapping/finance approval report
- Execute report
- Reconciliation report
- Rollback report
- ReadyToTest delivery guide
- Known exclusions and review queue export

## Sample Customers

- Adnan: active-contract ReadyToTest sample.
- RSMDB: intelligence-heavy accounting pilot sample.

## Do Not Package As Production Tool Without

- UI or approved runner release process
- formal finance sign-off workflow
- final GoLive checklist
- controlled backup/restore automation
- automated web validation coverage
