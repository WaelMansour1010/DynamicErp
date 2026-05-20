# Phase 6 Dry Run Results

Date: 2026-05-20  
BatchId: `D6EAD000-0000-4000-9000-202605200006`

## Result

PASS.

## Migrated Counts

| Entity | Count |
|---|---:|
| Accounts | 256 |
| Tenants | 256 |
| Properties | 26 |
| Units | 258 |
| Contracts | 283 |
| Contract batches/installments | 1,099 |
| Opening balance rows | 68 |
| Advance payment rows | 14 |

## Financial Reconciliation

| Metric | Value | Status |
|---|---:|---|
| Opening Balance | 1,156,544.6600 | PASS |
| Future gross | 19,234,398.7085 | PASS |
| Advance payments | 55,592.8900 | PASS |
| Net remain | 19,178,805.8185 | PASS |

## Scope Control

- Migrated only the 283 valid active contracts.
- Did not migrate the 10 missing-link contracts.
- Did not migrate full accounting history.
- Did not migrate Adnan users/passwords.
