# Phase 5 Rollback Results

Date: 2026-05-20  
BatchId: C5EAD000-5A5E-4AD5-9A55-202605200005

## Result

Rollback is safe for the Phase5 migration batch.

## Rollback Scope

Rolled back:

- Cross reference rows for Phase5 batch
- Account mapping rows for Phase5 batch
- Opening balance staging rows for Phase5 batch
- Advance payment staging rows for Phase5 batch
- Seeded renter accounts for Phase5 migration
- Migrated tenants/properties/units/contracts/contract batches tagged with PropertyPilot Adnan

Retained intentionally:

- Pilot Branch ADNAN-PILOT
- Pilot receipt/payment methods CASH-PILOT, BANK-PILOT
- ErpAdmin operational links to Department/CashBox
- Adnan lookup mappings for property/unit types

## After Rollback Verification

| Metric | Value |
|---|---:|
| CrossReference | 0 |
| AccountMapping | 0 |
| OpeningBalanceStaging | 0 |
| AdvancePaymentStaging | 0 |
| PilotAccounts | 0 |
| PilotTenants | 0 |
| PilotProperties | 0 |
| PilotUnits | 0 |
| PilotContracts | 0 |
| PilotBatches | 0 |
| OperationalPilotBranch retained | 1 |
| Receipt methods retained | 2 |
| Issue methods retained | 2 |
| Lookup mappings retained | 15 |

## Migration Batch Status

RolledBack
"@ | Set-Content -Path (Join-Path F:\Source Code\DynamicErp\Docs\PropertyPilotMigration_Adnan_Phase5 'Phase5_RollbackResults_20260520.md') -Encoding UTF8

@"
# Property Pilot Migration Adnan - Phase 5 Dry Run Report

Date: 2026-05-20  
Database: Alromaizan_PropertyPilot_Adnan_20260520  
Source: Adnan read-only  
Original production/reference: Alromaizan untouched

## Executive Summary

Phase5 database dry run succeeded financially and structurally inside Sandbox. Operational Seed was applied, lookup mapping was corrected, 283 valid active contracts migrated, opening balance matched exactly, advance payments reconciled correctly, and rollback cleaned all batch data safely.

Web validation is still blocked by an application authentication/authorization error before login: ERPAuthorizeAttribute.Log throws NullReferenceException on /Account/Login for unauthenticated requests.

## What Was Executed

1. Safety review for Sandbox-only scripts.
2. Operational Seed inside Sandbox only.
3. Corrected PropertyType and PropertyUnitType mapping.
4. Phase5 Batch creation: C5EAD000-5A5E-4AD5-9A55-202605200005.
5. Account seeding for active tenants only.
6. Migration of valid 283 active contracts and related entities.
7. Advance payment staging.
8. Full reconciliation.
9. Web validation attempt.
10. Rollback test.

## Migration Results

| Entity | Count |
|---|---:|
| Accounts | 256 |
| Tenants | 256 |
| Properties | 26 |
| Units | 258 |
| Contracts | 283 |
| Contract installments | 1,099 |
| Opening balance rows | 68 |
| Advance payment rows | 14 |

## Financial Results

| Metric | Value | Status |
|---|---:|---|
| Opening Balance | 1,156,544.6600 | PASS |
| Future installments gross | 19,234,398.7085 | PASS |
| Advance payments | 55,592.8900 | PASS |
| Expected net remain | 19,178,805.8185 | PASS |

## Key Risks Remaining

| Risk | Severity | Status |
|---|---|---|
| Login/auth filter runtime error blocks web validation | Critical | Needs Phase6 fix |
| Receipt/payment/termination not tested through UI | Critical | Blocked by auth issue |
| Accounting journal correctness not validated | Critical | Blocked by receipt/termination tests |
| 18 properties have missing source qartypeid | Medium | Needs business decision/default or leave nullable |
| 10 active contracts have missing links | Medium | Continue excluded/archive-only unless manually fixed |

## Decision

Do not move to a real limited pilot yet. Database migration tooling is ready for repeatable Sandbox dry runs, but operational go-live confidence requires Phase6 focused on safe web authentication validation and receipt/termination accounting tests.
