# Migration Templates Audit - 2026-05-20

## Scope

Reviewed SQL templates under `F:\Source Code\DynamicErp\Docs\PropertyMigrationToolkit\Sql` after the first controlled runner execute.

## Before This Phase

The following migration templates were placeholders or intentionally blocking templates:

1. `Migration_DefaultEntitiesSeed_Generic.sql`
2. `Migration_MasterData_Generic.sql`
3. `Migration_Contracts_Generic.sql`
4. `Migration_Installments_Generic.sql`
5. `Migration_OpeningBalance_Generic.sql`
6. `Migration_AdvancePayments_Generic.sql`
7. `Migration_Receipts_Generic.sql`
8. `Migration_Issues_Generic.sql`
9. `Migration_Journals_Generic.sql`
10. `Migration_Terminations_Generic.sql`
11. `Rollback_Generic.sql`

## Converted To Executable Generic Templates

These templates no longer stop with placeholder `RAISERROR` and now operate from the generic source staging contract:

- `01_SourceStagingTables_Generic.sql`
- `Migration_DefaultEntitiesSeed_Generic.sql`
- `Migration_MasterData_Generic.sql`
- `Migration_Contracts_Generic.sql`
- `Migration_Installments_Generic.sql`
- `Migration_OpeningBalance_Generic.sql`
- `Migration_AdvancePayments_Generic.sql`
- `Migration_Receipts_Generic.sql`
- `Migration_Journals_Generic.sql`
- `Migration_Issues_Generic.sql`
- `Migration_Terminations_Generic.sql`

## Still Draft / Not Converted

- `Rollback_Generic.sql` remains a guarded draft because destructive cleanup must be reviewed per target table scope before enabling in a general runner.

## Generic But Requires Customer Mapping

The templates are generic executable templates, but they require customer-specific staging population first. For example, RSMDB needs SELECT mapping from `TblAqar`, `TblUnites`, `TblContract`, `TblContractInstallments`, `Notes`, and `DOUBLE_ENTREY_VOUCHERS` into the `PropertyMigrationSource*` tables.

## Adnan Hardcoding Check

No generic migration template contains Adnan-specific business logic. Mentions of `Adnan`, `RSMDB`, `Alromaizan`, or `MyErp` are safety guards only.

## Parse/Syntax Review

A parse-only review was run against the clone target for the converted scripts. All converted migration scripts parsed successfully. `Migration_DefaultEntitiesSeed_Generic.sql` raised its expected config guard for a non-existing parse customer, not a syntax issue.
