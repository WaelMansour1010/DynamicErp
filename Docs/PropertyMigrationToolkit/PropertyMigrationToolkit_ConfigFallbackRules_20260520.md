# Property Migration Toolkit - Config Fallback Rules
Date: 2026-05-20

## Fallback Flags
| Flag | Effect |
|---|---|
| `AllowUnknownUnits` | Use `MIGRATION_UNKNOWN_UNIT` |
| `AllowUnknownProperties` | Use `MIGRATION_UNKNOWN_PROPERTY` |
| `AllowUnknownRenters` | Use `MIGRATION_UNKNOWN_RENTER` |
| `AllowSuspenseAccounts` | Use suspense for unknown accounts with review |
| `AllowFallbackPaymentMethods` | Use default payment method with review |
| `AllowDefaultCashBox` | Use configured default cashbox |
| `AllowDefaultBank` | Use configured default bank account |
| `AllowTemporaryRenterAccounts` | Use temp renter account |
| `ExcludeUnsafeOwnerPayments` | Push unsafe owner payments to exclusions/review |
| `AutoCreateMissingAccounts` | Seed missing non-critical accounts |
| `AutoCreateMissingLookups` | Seed unknown lookup values |

## Mode Defaults
| Flag | Strict | Tolerant | Hybrid |
|---|---|---|---|
| Unknown units/properties | Off | On | On |
| Temporary renter accounts | Off | On | On for master data |
| Suspense accounts | Off | Optional | Optional, accounting review required |
| Fallback payment methods | Off | On | On for shell, strict for posting |
| Historical issues | Off | Optional | Off until approved |

## Approval
Fallback rules must be reviewed per customer before migration.
