# Migration Templates Final Decision - 2026-05-20

## Decision

The Property Migration Toolkit has moved from placeholder migration scripts to executable generic staging-based templates.

## What Is Ready

- Master data generic migration.
- Contracts generic migration with strict/tolerant/hybrid behavior.
- Installments generic migration.
- Opening balance staging.
- Advance payment staging.
- Receipt migration with account safety checks.
- Voucher-linked journal migration with strict accounting safety.
- Issue and termination review queue workflows.
- Runner integration with template execution logging.

## What Is Not Yet Ready

- RSMDB data migration execute.
- RSMDB-specific staging population script.
- Owner payment migration as an approved scenario.
- Full historical GL migration.
- Generic rollback enablement without final delete-scope review.

## Go/No-Go

- Adnan Runner pipeline: proven.
- Generic templates: implementation-ready for clone testing after staging mapping.
- RSMDB: Discovery-only complete; migration is still blocked until mapping review.

## Next Step

Build `RSMDB_StagingMapping_SELECT_TO_STAGING_DRAFT.sql` to populate `PropertyMigrationSource*` tables on an RSMDB clone, then run DryRun and controlled Execute on that clone only.
