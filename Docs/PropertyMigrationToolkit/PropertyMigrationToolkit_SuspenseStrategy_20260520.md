# Property Migration Toolkit - Suspense Strategy
Date: 2026-05-20

## Purpose
Suspense/Holding accounts allow migration progress without inventing false accounting mappings.

## Accounts
| Account | Purpose |
|---|---|
| `MIGRATION_SUSPENSE_ACCOUNT` | Unknown source account requiring finance review |
| `MIGRATION_HOLDING_ACCOUNT` | Temporary holding for approved transitional balances |
| `MIGRATION_TEMP_RENTER_ACCOUNT` | Tenant account placeholder until real account is mapped |

## When To Use
- Tolerant/Hybrid testing only.
- Missing source account has real financial value but cannot yet be mapped.
- Finance accepts explicit suspense tracking.

## When Not To Use
- Final Strict GoLive unless reviewed and signed off.
- To hide unbalanced journals.
- To force owner payments into production.

## Review Requirement
Every suspense usage must appear in:
- `PropertyMigrationSuspenseMapping`.
- `PropertyMigrationReviewQueue`.
- Reconciliation report.

## GoLive Rule
Open suspense balances block GoLive unless explicitly approved by finance and documented.
