# Property Migration Toolkit - Default Entities
Date: 2026-05-20

## Default Entities
| Code | Entity | Creation Method |
|---|---|---|
| `MIGRATION_UNKNOWN_PROPERTY` | Property | Seed script/config |
| `MIGRATION_UNKNOWN_UNIT` | Unit | Seed script/config |
| `MIGRATION_UNKNOWN_RENTER` | Renter | Seed script/config |
| `MIGRATION_UNKNOWN_PROPERTY_TYPE` | PropertyType | Seed script/config |
| `MIGRATION_UNKNOWN_UNIT_TYPE` | UnitType | Seed script/config |
| `MIGRATION_DEFAULT_CASHBOX` | CashBox | Operational seed |
| `MIGRATION_DEFAULT_BANK` | Bank/BankAccount | Operational seed |
| `MIGRATION_DEFAULT_PAYMENT_METHOD` | Receipt/Issue method | Operational seed |
| `MIGRATION_SUSPENSE_ACCOUNT` | ChartOfAccount | Finance-approved seed |
| `MIGRATION_HOLDING_ACCOUNT` | ChartOfAccount | Finance-approved seed |

## Recommendation
Create defaults through a reviewed seed script per clone, then register them in `PropertyMigrationFallback` and config IDs.

## Naming Rule
Names must visibly include `MIGRATION_` so users understand these are temporary/review records.

## Reporting Rule
Any entity linked to a default must be counted in ReadyToTest and GoLive reports.
