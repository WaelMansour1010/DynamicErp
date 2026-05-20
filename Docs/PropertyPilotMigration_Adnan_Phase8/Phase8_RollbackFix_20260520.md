# Phase 8 - Rollback Fix
Date: 2026-05-20

## Issue From Phase7
`usp_PropertyPilot_RollbackBatch_Adnan` did not clean `PropertyPilotAdvancePaymentStaging`, leaving staging rows after rollback.

## Phase8 Fix
Sandbox procedure was updated to include:

```sql
IF OBJECT_ID(N'dbo.PropertyPilotAdvancePaymentStaging', N'U') IS NOT NULL
    DELETE FROM dbo.PropertyPilotAdvancePaymentStaging WHERE MigrationBatchId=@MigrationBatchId;
```

## Test
Created a Sandbox-only dummy batch:
`F8EAD000-0000-4000-9000-202605200008`

Inserted one row in `PropertyPilotAdvancePaymentStaging`, executed rollback, and verified:
- Before rollback: 1 row
- After rollback: 0 rows
- Batch status: RolledBack

## Additional Technical Note
A SQL Server internal query planner error appeared when a rollback batch had no chart-account cross references and the procedure still compiled the `ChartOfAccount` delete path. The Sandbox procedure was adjusted to run the ChartOfAccount delete via guarded dynamic SQL only when account cross references exist. This avoids the empty-batch planner failure and preserves account cleanup for real migration batches.

## Production Guidance
Treat this as Draft for customer clone rollout. Apply only to Sandbox/clone after review; never directly to `Alromaizan` production.
