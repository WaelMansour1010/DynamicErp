# Phase 9 - Rollback / Reset Plan
Date: 2026-05-20
Pilot Clone: $clone
BatchId: $batch

## Reset Levels
| Level | Purpose | Action |
|---|---|---|
| Test artifacts cleanup | Remove web validation receipts/issues/terminations only | Execute Phase9_TestArtifactsCleanup_SANDBOX_ONLY_20260520.sql |
| Migration batch rollback | Remove migrated 283-contract batch and staging | Use existing sandbox rollback procedure for BatchId, after review |
| Full clone reset | Rebuild clone from backup | Restore from Alromaizan_PropertyPilot_Adnan_20260520_to_PilotClone_20260520.bak or recreate clone |

## Cleanup Already Verified
The Phase9 test-artifact cleanup was executed on the clone and verified:

| Check | Result |
|---|---:|
| Remaining Phase9 receipts | 0 |
| Remaining Phase9 issues | 0 |
| Remaining Phase9 terminations | 0 |
| Remaining migrated contracts | 283 |
| Remaining advance staging rows | 14 |

## Batch Rollback Requirements
A full migration rollback must delete by MigrationBatchId only and include:
- PropertyPilotCrossReference
- PropertyPilotOpeningBalanceStaging
- PropertyPilotAdvancePaymentStaging
- migrated PropertyContractBatch
- migrated PropertyContract
- migrated PropertyDetail
- migrated Property
- migrated PropertyRenter
- seeded migrated ChartOfAccount rows from account cross references

## Guard Requirements
Any rollback/reset script must refuse to run unless DB_NAME() contains one of:
- PropertyPilot
- PilotClone
- Sandbox

It must also explicitly block:
- Alromaizan
- Adnan
- RSMDB

## Operational Seed Decision
Operational Seed is kept by default because it is required for clone testing and does not come from Adnan Users/Passwords.
