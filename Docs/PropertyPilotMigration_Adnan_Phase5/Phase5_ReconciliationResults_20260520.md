# Phase 5 Reconciliation Results

Date: 2026-05-20  
Database: Alromaizan_PropertyPilot_Adnan_20260520  
BatchId: C5EAD000-5A5E-4AD5-9A55-202605200005

## Before Rollback - Migration Counts

| Entity | Count |
|---|---:|
| Accounts | 256 |
| Tenants | 256 |
| Properties | 26 |
| Units | 258 |
| Contracts | 283 |
| Contract batches/installments | 1,099 |
| Opening balance staging rows | 68 |

## Financial Reconciliation

| Metric | Expected | Actual | Status |
|---|---:|---:|---|
| Opening Balance | 1,156,544.6600 | 1,156,544.6600 | PASS |
| Future installments gross | 19,234,398.7085 | 19,234,398.7085 | PASS |
| Advance payments | 55,592.8900 | 55,592.8900 | PASS |
| Expected net remain | 19,178,805.8185 | 19,178,805.8185 | PASS |
| Active contracts total | 293 | 293 | PASS |
| Valid linked contracts migrated | 283 | 283 | PASS |
| Excluded missing-link contracts | 10 | 10 | PASS |

## Data Quality Checks

| Check | Result |
|---|---:|
| Tenant account nulls | 0 |
| Property type nulls | 18 |
| Unit type nulls | 0 |
| Contract unit type nulls | 0 |
| Original Alromaizan pilot contracts | 0 |
| Original Alromaizan pilot accounts | 0 |

## Notes

The 18 property type nulls are not a migration failure; Adnan source active properties have missing qartypeid. A business decision is required before forcing a default type.
