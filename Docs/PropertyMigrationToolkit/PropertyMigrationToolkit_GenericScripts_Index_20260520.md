# Property Migration Toolkit - Generic Scripts Index
Date: 2026-05-20

## SQL Folder
`F:\Source Code\DynamicErp\Docs\PropertyMigrationToolkit\Sql`

| Script | Purpose |
|---|---|
| `00_ToolkitCore_ConfigAndXref_Generic.sql` | Creates enterprise config, batch, map, warning/error, autofix, fallback, review queue, suspense, exclusion, reconciliation, and run log tables |
| `Discovery_SELECT_ONLY_Generic.sql` | Discover candidate legacy tables and note types |
| `SchemaCompare_Property_Generic.sql` | Compare property schema between reference and target |
| `MappingTemplate_Generic.sql` | Mapping checklist template |
| `Diagnostics_Generic.sql` | Mode-aware diagnostics and warning/error logging |
| `Migration_DefaultEntitiesSeed_Generic.sql` | Reviewed fallback/default entity registry template |
| `Migration_MasterData_Generic.sql` | Mode-aware master data/fallback pattern |
| `Migration_Contracts_Generic.sql` | Contract migration placeholder |
| `Migration_Installments_Generic.sql` | Installment migration placeholder |
| `Migration_OpeningBalance_Generic.sql` | Opening balance staging placeholder |
| `Migration_AdvancePayments_Generic.sql` | Advance payment staging placeholder |
| `Migration_Receipts_Generic.sql` | Accounting-safe receipt migration template |
| `Migration_Issues_Generic.sql` | Issue/payment migration template; default manual review |
| `Migration_Journals_Generic.sql` | Strict journal migration template |
| `Migration_Terminations_Generic.sql` | Termination migration placeholder; default disabled |
| `Reconciliation_Generic.sql` | Counts, exceptions, suspense, and journal risk checks |
| `Rollback_Generic.sql` | Rollback template with confirmation gate |

## Enterprise Additions
- `MigrationMode`: Strict, Tolerant, Hybrid.
- AutoFix logging.
- ReviewQueue workflow.
- Suspense/Holding accounting strategy.
- Config-driven fallback flags.
- Runner-ready audit trail.

## Template Status
These are still templates, not one-click production scripts. Customer-specific source SELECTs and mappings must be generated from Discovery/Diagnostics.
