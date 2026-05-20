# PropertyMigrationToolkit Final Architecture - 2026-05-20

## Purpose

PropertyMigrationToolkit is now an internal migration platform for moving legacy VB6 property-management clients into DynamicErp through controlled clone-first pipelines.

It is not a blind production migrator. It is a staged engine that discovers, classifies, maps, migrates, reconciles, validates, and rolls back property data under strict safety gates.

## Main Components

| Component | Status | Notes |
|---|---|---|
| Console Runner | Stable internal use | Config-driven, guarded execution, reports, stage logging |
| Generic SQL Templates | Mostly executable | Master data/contracts/installments/receipts/journals/rollback exist; some customer-specific flows remain staged/draft |
| Intelligence Layer | Working for RSMDB | Receipt allocation discovery, account intelligence, finance pack, CashingType=8 candidate building |
| Finance Approval Workflow | Working | Scoped approval through `PropertyMigrationAccountFinanceApproval` and account resolution |
| Review Queue | Available | Used for warnings/manual review; must not be bypassed |
| Cross Reference / EntityMap | Required | Every migrated entity must map source to target with BatchId |
| Accounting Safety | Required | AccountId=NULL and unbalanced journals are hard blockers |
| Rollback | Working for pilots | Batch-scoped rollback validated for Adnan and RSMDB mini pilot |

## Pipeline

1. Preflight
2. Core/Staging setup
3. Discovery
4. Diagnostics
5. Intelligence and matching
6. Finance approval
7. Operational migration
8. Accounting pilot/migration
9. Reconciliation
10. Web validation
11. ReadyToTest delivery
12. Rollback or closure

## Modes

- Strict: safest, excludes questionable records.
- Tolerant: uses fallback/defaults for non-accounting master data with full logging.
- Hybrid: default. Master data can be tolerant; accounting remains strict.

## Customer Samples

| Customer | Status |
|---|---|
| Adnan | ReadyToTest pipeline proven; active-contract migration and accounting receipt scope validated |
| RSMDB | Staging, intelligence, CashingType=8 receipt discovery, finance approval, mini accounting pilot, rollback validated |

## Final Architecture Decision

The toolkit is production-grade for internal clone-based migration operations. It is not approved for fully autonomous GoLive without finance sign-off, reconciliation, web validation, and project approval.
