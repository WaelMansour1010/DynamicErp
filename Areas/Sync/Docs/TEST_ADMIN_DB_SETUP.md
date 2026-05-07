# Sync Admin Test DB Setup

## Scope

This validation used a local/test admin database only:

- SQL Server: `Wael\Sql2019`
- Database: `SyncAdminTest`
- Web connection: `SyncAdminConnection` with Windows Integrated Security
- Verified identity: `WAEL\Wael`
- No production database was used.
- No invoice, payment, stock, journal, or legacy transfer tables were modified.
- ApplyMode was not executed.

## Schema Applied

Script applied to `SyncAdminTest` only:

```powershell
sqlcmd -S "Wael\Sql2019" -E -d SyncAdminTest -i "F:\Source Code\DynamicErp\Areas\Sync\Sql\002_Sync_AdminOperations.sql" -b
```

No SQL password is required in committed configuration for this local/test setup.

Verified tables:

- `Sync_AdminOperation`
- `Sync_AdminAudit`
- `Sync_AdminApproval`
- `Sync_AdminNotification`
- `Sync_AdminRolePermission`

## Test Rows

Fresh queued request:

- `AdminOperationId`: `2`
- `SyncKey`: `10:Invoice:29327`
- `ProfileName`: `POSOnly`
- `Status`: `Blocked`
- `Result`: `Blocked`
- `ApplySingleSyncKeyOnly`: `1`
- `MaxInvoicesPerRun`: `1`
- `WorkerName`: `LocalTestWorker`
- `LastError`: `Worker listener reserved the request; execution adapter is not enabled.`

Approval row:

- `Status`: `ApprovedForQueue`
- `RequestedBy`: `local-sync-admin`
- `ApprovedBy`: `local-sync-admin`

Audit rows:

- `PrepareApplySingle` / `Queued`
- `WorkerPoll` / `Blocked`

Notification samples:

- `Conflict`
- `Failed`
- `BlockedPilot`

## Validation Results

Server-side validation blocked:

- missing `SyncKey`
- batch attempt
- missing reason
- missing password
- `MaxInvoicesPerRun > 1`
- missing approval checkbox

Worker skeleton result:

- picked one `PendingWorker` operation
- marked it `Blocked`
- wrote worker audit
- did not execute ApplyMode
- did not call any execution adapter

UI/security result:

- `/sync/AdminOperations` returned `200` and showed the blocked operation
- `/sync/Notifications` returned `200` and showed notification samples
- `/sync/AdminAudit?dangerousOnly=true` returned `200` and showed queued/worker audit
- unauthenticated POST `/sync/AdminOperations/Queue` redirected to login
- POST `/sync/apply/requestapply` returned `403`

## Screenshots

- `Areas/Sync/Docs/Screenshots/TestAdminDb/admin-operations.png`
- `Areas/Sync/Docs/Screenshots/TestAdminDb/notifications.png`
- `Areas/Sync/Docs/Screenshots/TestAdminDb/audit.png`

## Safety Confirmation

Apply execution remains blocked. There is still no direct browser ApplyMode path, no batch apply, no PowerShell execution from browser, and no production DB change in this phase.

## Credential Policy

- Do not commit real credentials.
- Prefer Windows Integrated Security using a least-privilege IIS or Windows Service identity.
- If SQL authentication is required for a deployment, use an environment-specific transform, deployment secret injection, or encrypted `connectionStrings` section. Do not commit plaintext passwords.
