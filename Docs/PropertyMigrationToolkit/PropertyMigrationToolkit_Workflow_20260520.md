# Property Migration Toolkit - Workflow
Date: 2026-05-20

## Wizard / Pipeline Steps
| Step | Name | Output |
|---:|---|---|
| 0 | Connection Validation | Source/target status, safe target confirmation |
| 1 | Discovery | Tables, note types, candidate links |
| 2 | Diagnostics | Issues, critical blockers, fallback candidates |
| 3 | Mapping Review | Entity/account/payment mappings |
| 4 | Migration | Batch data, maps, warnings, autofixes |
| 5 | Reconciliation | Counts/totals/balances/journal checks |
| 6 | Warnings / Review Queue | Open review items and suggested fixes |
| 7 | ReadyToTest Delivery | Clean DB, report, user guide, decision |

## Runner Commands Concept
```text
property-migration validate --config customer.json
property-migration discover --config customer.json
property-migration diagnose --config customer.json
property-migration migrate --config customer.json --mode Hybrid --dry-run false
property-migration reconcile --batch <BatchId>
property-migration report --batch <BatchId>
property-migration rollback --batch <BatchId> --confirm
```

## Retry Model
- Each stage writes logs.
- Migration is BatchId-scoped.
- EntityMap prevents duplicates.
- Failed records enter ReviewQueue or ExcludedRecords.
- Rerun can target one stage or record group.
