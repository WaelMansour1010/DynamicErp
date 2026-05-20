# Phase 5 Rollback Results

Date: 2026-05-20  
BatchId: `C5EAD000-5A5E-4AD5-9A55-202605200005`

## Result

Rollback is safe for the Phase5 migration batch.

## Rollback Scope

Rolled back:

- Cross reference rows for Phase5 batch
- Account mapping rows for Phase5 batch
- Opening balance staging rows for Phase5 batch
- Advance payment staging rows for Phase5 batch
- Seeded renter accounts for Phase5 migration
- Migrated tenants/properties/units/contracts/contract batches tagged with `PropertyPilot Adnan`

Retained intentionally:

- Pilot Branch `ADNAN-PILOT`
- Pilot receipt/payment methods `CASH-PILOT`, `BANK-PILOT`
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

`RolledBack`
