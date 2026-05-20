# Property Pilot Migration Adnan - Phase 3 Dry Run Report - 2026-05-20

## Executive Summary

Phase 3 executed the first real Dry Run against a separate Sandbox database only:

`Alromaizan_PropertyPilot_Adnan_20260520`

Source database `Adnan` was used read-only. Original `Alromaizan` was not modified.

The Dry Run successfully migrated the 283 structurally valid strict-active contracts into the Sandbox, then executed a batch rollback successfully.

## Sandbox Creation

| Item | Result |
|---|---|
| Sandbox DB created | Yes |
| Sandbox DB name | `Alromaizan_PropertyPilot_Adnan_20260520` |
| Creation method | `COPY_ONLY BACKUP` from `Alromaizan`, then `RESTORE DATABASE` to separate MDF/LDF files |
| Original `Alromaizan` modified | No |
| `Adnan` modified | No |

## Script Review Result

| Check | Result |
|---|---|
| DML/DDL scripts have DB name guards | Passed |
| Guard blocks `Alromaizan` | Passed |
| Guard requires `PropertyPilot` or `Sandbox` in DB name | Passed |
| SELECT-only scripts contain no DML/DDL | Passed |
| SQL Server 2012 compatibility | Mostly passed; Phase3 fixes required for collation and CTE scope |
| Cross Reference layer exists | Passed |

## Phase3 Fixes Required Before Successful Run

| Issue | Impact | Fix Applied |
|---|---|---|
| Collation conflicts between `Adnan` and Sandbox | Procedure deployment failed | Added explicit `COLLATE DATABASE_DEFAULT` in Phase3 fixed scripts |
| `SELECT DISTINCT *` over legacy tables with non-comparable columns | Procedure deployment failed | Replaced with explicit column lists |
| CTE reuse outside statement scope | Tenant procedure failed | Replaced entity migration with temp-table based fixed script |
| Missing target lookup mappings for `PropertyTypeId` and `PropertyUnitTypeId` | FK failure | Set unmapped lookup IDs to `NULL` temporarily and preserved legacy values in source references |
| Seed account procedure hung, while equivalent set-based script worked | Unsafe to continue with procedure | Replaced account seeding step with fixed sandbox-only set-based script |

Fixed scripts were saved under:

`Phase3_FixedScripts`

## Migration Results Before Rollback

| Entity | Migrated Count |
|---|---:|
| Accounts seeded | 256 |
| Tenants | 256 |
| Properties | 26 |
| Units | 258 |
| Contracts | 283 |
| Contract batches/installments | 1,099 |
| Excluded bad-link contracts | 10 |

## Financial Results Before Rollback

| Metric | Expected | Actual | Status |
|---|---:|---:|---|
| Opening Balance | 1,156,544.66 | 1,156,544.6600 | Matched |
| Future gross batches | 19,234,398.7085 | 19,234,398.7085 | Matched as gross batch total |
| Future paid/advance amount | 55,592.89 | Not migrated as allocation | Requires decision |
| Future remaining expected | 19,178,805.8185 | Not represented as net remain in target batches | Requires enhancement |
| Total batch value | 33,055,074.9365 | 33,055,074.9365 | Matched |

## Important Business Finding

The migrated future batches match the gross future installment schedule, but they do not yet represent the paid-in-advance portion of `55,592.89` against future installments.

Before Go Live, one of these treatments must be approved:

1. Create opening/prepaid credit allocation against future batches.
2. Import minimal safe receipt allocations for future-paid amounts only.
3. Keep gross future batches and add separate advance balance on renter account.

## Lookup Mapping Finding

The first run proved that old property/unit type IDs from `Adnan` do not safely match DynamicErp lookup IDs.

Temporary Phase3 approach:

- `Property.PropertyTypeId = NULL`
- `PropertyDetail.PropertyUnitTypeId = NULL`
- `PropertyContract.PropertyUnitTypeId = NULL`

This allowed FK-safe migration, but it is not enough for a production-quality pilot. Lookup mapping must be added before the next Dry Run if screens/reports depend on those types.

## Web Validation Summary

IIS Express started successfully and root request returned HTTP 200.

However, property module URLs returned HTTP 302 redirects to authentication/login. Because no authenticated web session was available in this run, full screen validation could not be completed.

Test result:

| Test | Result |
|---|---|
| IIS Express starts | Passed |
| Local root request | HTTP 200 |
| Sandbox DB override via DevStart | Attempted |
| Property screens | Blocked by auth redirect HTTP 302 |
| Receipt creation | Not executed due auth block |
| Termination test | Not executed due auth block |

## Rollback Result

Rollback was executed using `MigrationBatchId`:

`B7B0DA8D-1E0E-4A1D-A4AB-AD2026052001`

| Check After Rollback | Result |
|---|---:|
| Sandbox Cross Reference rows for batch | 0 |
| Sandbox tagged accounts | 0 |
| Sandbox tagged tenants | 0 |
| Sandbox tagged properties | 0 |
| Sandbox tagged units | 0 |
| Sandbox tagged contracts | 0 |
| Sandbox tagged batches | 0 |
| Original `Alromaizan` tagged accounts | 0 |
| Original `Alromaizan` property contracts | 0 |

Rollback is safe for the tested batch pattern.

## Final Phase 3 Decision

The first Sandbox Dry Run succeeded for data movement and rollback, but it is not yet ready for business UAT because:

1. Web validation was blocked by authentication.
2. Lookup mappings for property/unit types are missing.
3. Future paid/advance amount `55,592.89` needs accounting treatment.
4. Draft procedures need to be replaced with the Phase3 fixed scripts before re-use.

Recommendation:

Proceed to a second Sandbox Dry Run after applying Phase3 fixes permanently and preparing an authenticated web validation path.
