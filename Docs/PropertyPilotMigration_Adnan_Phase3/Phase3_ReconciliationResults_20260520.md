# Phase 3 Reconciliation Results - 2026-05-20

## Batch

`B7B0DA8D-1E0E-4A1D-A4AB-AD2026052001`

## Counts Before Rollback

| Entity | Source Expected | Sandbox Actual | Difference |
|---|---:|---:|---:|
| Properties | 26 | 26 | 0 |
| Units | 258 | 258 | 0 |
| Tenants | 256 | 256 | 0 |
| Contracts | 283 | 283 | 0 |
| Contract batches | 1,099 | 1,099 | 0 |
| Accounts | 256 | 256 | 0 |
| Excluded bad-link contracts | 10 | 10 | 0 |

## Financial Reconciliation

| Metric | Source | Sandbox | Difference | Status |
|---|---:|---:|---:|---|
| Total batch value | 33,055,074.9365 | 33,055,074.9365 | 0 | Pass |
| Opening Balance | 1,156,544.6600 | 1,156,544.6600 | 0 | Pass |
| Future gross installments | 19,234,398.7085 | 19,234,398.7085 | 0 | Pass as gross schedule |
| Future paid/advance | 55,592.8900 | 0 allocated | -55,592.8900 | Requires design |
| Future remain | 19,178,805.8185 | Not represented as net remain | N/A | Requires design |

## Account Mapping

| Metric | Value |
|---|---:|
| Active renter accounts required | 256 |
| Accounts seeded in Sandbox | 256 |
| Tenant rows without AccountId | 0 |

## Lookup Mapping Gaps

| Field | Rows With NULL After Fixed Migration | Comment |
|---|---:|---|
| `Property.PropertyTypeId` | 26 | Old Adnan IDs do not match target lookup. |
| `PropertyDetail.PropertyUnitTypeId` | 258 | Requires lookup mapping. |
| `PropertyContract.PropertyUnitTypeId` | 283 | Requires lookup mapping. |

## Reconciliation Decision

Core counts and opening balance passed. The Dry Run is financially promising, but not Go Live ready until future advance handling and lookup mappings are resolved.
