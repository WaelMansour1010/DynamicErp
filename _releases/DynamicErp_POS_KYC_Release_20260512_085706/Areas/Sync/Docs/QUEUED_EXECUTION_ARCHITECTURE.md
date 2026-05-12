# Sync Queued Execution Architecture

The browser never executes ApplyMode directly.

Flow:

1. Admin reviews `/sync/apply` and `/sync/AdminOperations`.
2. Admin submits a queued request with one `SyncKey`, reason, approval checkbox, and password confirmation.
3. Server validates:
   - one invoice SyncKey only
   - `ApplySingleSyncKeyOnly=true`
   - `MaxInvoicesPerRun=1`
   - approval reason present
   - password confirmation present
4. Server writes `Sync_AdminOperation` and `Sync_AdminAudit`.
5. Worker listener polls `Sync_AdminOperation`.
6. Current worker skeleton reserves the operation and marks it blocked because the real execution adapter is not enabled.

No batch apply is allowed.

## SignalR / Live Updates

SignalR package is not currently installed in this MVC project. The implemented live status endpoint is:

`/sync/LiveStatus/Snapshot`

It returns read-only dashboard counts and can be replaced by SignalR after package approval.

## Notifications

Notifications are stored in `Sync_AdminNotification` after script approval. Types:

- `Conflict`
- `FailedSync`
- `BlockedPilot`

## Deployment Automation

1. Review `002_Sync_AdminOperations.sql`.
2. Back up the target database.
3. Apply only after explicit approval.
4. Configure roles and app settings.
5. Deploy web UI.
6. Install worker service separately.
7. Keep execution adapter disabled until controlled pilot approval.
