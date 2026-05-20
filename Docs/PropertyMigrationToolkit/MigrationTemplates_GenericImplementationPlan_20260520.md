# Generic Migration Templates Implementation Plan - 2026-05-20

## Implemented Now

A generic staging-based implementation was added. The migration no longer depends on Adnan table names.

## Execution Order

1. `00_ToolkitCore_ConfigAndXref_Generic.sql`
2. `01_SourceStagingTables_Generic.sql`
3. Customer-specific staging population script, not implemented for RSMDB yet.
4. `Migration_DefaultEntitiesSeed_Generic.sql`
5. `Migration_MasterData_Generic.sql`
6. `Migration_Contracts_Generic.sql`
7. `Migration_Installments_Generic.sql`
8. `Migration_OpeningBalance_Generic.sql`
9. `Migration_AdvancePayments_Generic.sql`
10. `Migration_Receipts_Generic.sql`
11. `Migration_Issues_Generic.sql`, manual-review by default.
12. `Migration_Journals_Generic.sql`, voucher-linked only.
13. `Migration_Terminations_Generic.sql`, manual-review by default.
14. `Reconciliation_Generic.sql`

## Safe Implementation Decisions

- Generic templates consume normalized staging tables.
- Customer-specific VB6 table logic is isolated in mapping scripts.
- Contracts can use placeholders in `Tolerant/Hybrid` only if fallback IDs are configured.
- Receipts require AccountId before insert.
- Journals require voucher linkage, valid account IDs, and balanced debit/credit.
- Issues and terminations are review-gated, not silently migrated.

## Remaining Work

- Build customer-specific RSMDB mapping scripts into the staging contract.
- Add richer reconciliation rows into `PropertyMigrationReconciliationResult`.
- Convert `Rollback_Generic.sql` from draft to approved clone-only cleanup after table scope review.
