# RSMDB Staging Load Results - 2026-05-20

## Execution Scope

Staging load only. No `Migration_*_Generic.sql` templates were executed.

| Item | Value |
|---|---|
| Clone | `Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520` |
| Source | `RSMDB` read-only |
| BatchId | `1b5d8eb5-dd7e-4b63-8ddb-e7b00aad558b` |
| Mapping script | `RSMDB_StagingMapping_SELECT_TO_STAGING_DRAFT_20260520.sql` |

## Staging Counts

| Staging Entity | Count |
|---|---:|
| Properties | 16 |
| Owners | 4 |
| Property-owner links | 16 |
| Units | 629 |
| Renters | 725 |
| Contracts | 2,813 |
| Active contract candidates | 262 |
| Installments | 7,478 |
| Receipts | 11,829 |
| Issues / payments review-only | 7,632 |
| Journal headers | 8,083 |
| Journal lines mapped to target accounts | 0 |
| Owner balance candidates | 4 |
| Termination candidates | 754 |

## Important Notes

- Owner staging was corrected to avoid duplicate owners from repeated `TblAqar.ownerid` references; final owner count is 4.
- Receipts were staged from `Notes` Type 4. Safe/unsafe linkage is separated in diagnostics.
- Issues/payments are staged as review-only; they are not approved for migration.
- Journals were staged as headers only because no matching target account lines resolved yet. This blocks accounting migration until account mapping is fixed.
- Owner balances from `TblAqrOwin` are staged for finance review only.

## Decision

Staging load succeeded, but it is not migration-ready because accounting and payment mappings require review.
