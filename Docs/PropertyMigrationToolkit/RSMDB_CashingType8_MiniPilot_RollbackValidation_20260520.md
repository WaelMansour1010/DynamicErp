# RSMDB CashingType=8 Mini Pilot Rollback Validation - 2026-05-20

## Rollback Scope

- Pilot BatchId: CD05CD47-10F5-4CC1-9467-CC496A694797
- Operational staging/entity maps retained
- Finance approvals retained
- Only Mini Pilot receipts/journals/links/maps removed

## Rollback Results

| Check | Remaining Count |
|---|---:|
| Pilot receipts | 0 |
| Pilot journals | 0 |
| Pilot journal lines | 0 |
| Pilot entity maps | 0 |
| Documents matching RSMDB-C8-% | 0 |
| Journals with MigrationSource=RSMDB_CashingType8_MiniPilot | 0 |

## Result

Rollback passed. No orphan pilot accounting data remains.
