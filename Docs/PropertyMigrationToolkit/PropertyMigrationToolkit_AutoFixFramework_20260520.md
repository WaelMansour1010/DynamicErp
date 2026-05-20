# Property Migration Toolkit - AutoFix Framework
Date: 2026-05-20

## Principle
AutoFix never means silent fix. Every fix must create:
- `PropertyMigrationAutoFix`
- `PropertyMigrationWarning`
- `PropertyMigrationReviewQueue` if manual review is needed
- `PropertyMigrationEntityMap` marked `UsedFallback=1`

## Supported AutoFixes
| Source Issue | Fallback | Severity | Review Required |
|---|---|---|---|
| Contract without unit | `MIGRATION_UNKNOWN_UNIT` | Warning | Yes |
| Unit without property | `MIGRATION_UNKNOWN_PROPERTY` | Warning | Yes |
| Missing renter | `MIGRATION_UNKNOWN_RENTER` | Warning | Yes |
| Missing renter account | `MIGRATION_TEMP_RENTER_ACCOUNT` | Warning/Critical for accounting | Yes |
| Unknown property type | `MIGRATION_UNKNOWN_PROPERTY_TYPE` | Info/Warning | Optional |
| Unknown unit type | `MIGRATION_UNKNOWN_UNIT_TYPE` | Info/Warning | Optional |
| Unknown payment method | `MIGRATION_DEFAULT_PAYMENT_METHOD` | Warning | Yes |
| Missing cashbox | `MIGRATION_DEFAULT_CASHBOX` | Warning | Yes |
| Missing bank | `MIGRATION_DEFAULT_BANK` | Warning | Yes |
| Unknown journal account | `MIGRATION_SUSPENSE_ACCOUNT` | Critical-controlled | Yes |

## Implementation Pattern
1. Read config and mode.
2. If strict: exclude/log.
3. If tolerant/hybrid and allowed: create/use fallback.
4. Insert target row with fallback link.
5. Insert AutoFix row.
6. Insert ReviewQueue row.
7. Include in reconciliation.

## No Silent Defaults
Any default without a log is considered a migration defect.
