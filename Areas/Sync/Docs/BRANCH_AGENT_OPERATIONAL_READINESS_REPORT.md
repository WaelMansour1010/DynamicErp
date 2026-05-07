# Branch Agent Operational Readiness Report

Date: 2026-05-06

## Scope

This phase hardened the branch-side collector/sender and central branch monitoring only.

No ApplyMode was enabled. No destination invoice insert path was added. No batch apply was added. No production database was touched.

## Local Test Environment

- Central admin DB: `Wael\Sql2019 / SyncAdminTest`
- Branch test DB: `Wael\Sql2019 / BranchPosTest`
- Test BranchId: `10`
- Test SyncKey: `10:Invoice:29327`
- Test API token environment variable: `SATRIAH_BRANCH_SYNC_TOKEN_10`
- IIS Express URL: `http://localhost:8096`

## Files Added

- `SyncBranchAgent/Config/BranchSyncAgent.config.template`
- `SyncBranchAgent/Scripts/Install-BranchSyncAgent.ps1`
- `SyncBranchAgent/Scripts/Uninstall-BranchSyncAgent.ps1`
- `SyncBranchAgent/Scripts/Start-BranchSyncAgent.ps1`
- `SyncBranchAgent/Scripts/Stop-BranchSyncAgent.ps1`
- `SyncBranchAgent/Scripts/Restart-BranchSyncAgent.ps1`
- `SyncBranchAgent/Scripts/New-BranchSyncAgentConfig.ps1`
- `SyncBranchAgent/Models/HealthSnapshot.cs`
- `Areas/Sync/Sql/004_Sync_BranchAgentHardening.sql`
- `Areas/Sync/Docs/Screenshots/branch-monitoring-dashboard.png`

## Key Hardening Implemented

- Installer/service scripts with dry-run support.
- Config template and config generation script.
- Health diagnostics command: `SyncBranchAgent.exe --console --health`.
- Local retry/backoff with `NextAttemptAtUtc`.
- Failed payload quarantine after `MaxRetryCount`.
- Agent/config/payload schema versioning.
- Signed central API ping endpoint.
- Central API rejects unsigned payloads by default.
- Timestamp replay window remains 15 minutes.
- Auth failures are recorded into central monitoring tables.
- Dashboard branch monitoring shows online/offline, versions, pending, local failed, rejected, and auth failure counts.

## Local Test Results

### SQL Apply

Applied only to local/test admin DB:

```powershell
sqlcmd -S "Wael\Sql2019" -d SyncAdminTest -E -b -i ".\Areas\Sync\Sql\004_Sync_BranchAgentHardening.sql"
```

Result: success.

### Installer Dry Run

```powershell
.\SyncBranchAgent\Scripts\Install-BranchSyncAgent.ps1 -BinaryPath .\SyncBranchAgent\bin\Debug\SyncBranchAgent.exe -ServiceAccount "NT AUTHORITY\LocalService" -DryRun
.\SyncBranchAgent\Scripts\Uninstall-BranchSyncAgent.ps1 -DryRun
```

Result: dry-run only; no Windows Service was created or removed.

### Health Output

```json
{
  "BranchId": 10,
  "MachineName": "WAEL",
  "AgentVersion": "0.0.0.0",
  "ConfigVersion": "1.0",
  "PayloadSchemaVersion": "1.0",
  "PendingLocalOutboxCount": 0,
  "FailedLocalOutboxCount": 0,
  "SendEnabled": true,
  "DryRunSend": false,
  "CentralConnectivityOk": true,
  "CentralConnectivityMessage": "Central API reachable."
}
```

### Offline Retry

Central URL was pointed to an unavailable local endpoint. Result:

- Payload remained in local `pending`.
- `TryCount=1`.
- `NextAttemptAtUtc` was populated.
- No central insert occurred.

### Duplicate Resend

Same signed payload resent to central API.

Result:

```json
{"Accepted":true,"Status":"DuplicateAccepted","Message":"Duplicate payload already exists.","SyncKey":"10:Invoice:29327"}
```

### Security Tests

- Bad token/signature: rejected with HTTP 403.
- Old timestamp: rejected with HTTP 403.
- Auth failures recorded in `Sync_Error` with `EntityType='BranchAuth'`.
- Token value was never printed by scripts or agent logs.

### Central DB Counts

```text
Sync_Outbox: 1
Sync_BranchHeartbeat: 1
Sync_BranchUpload: 6
Sync_Error BranchAuth rows: 6
Destination invoice table in SyncAdminTest: not present
```

## Central UI Screenshot

`Areas/Sync/Docs/Screenshots/branch-monitoring-dashboard.png`

## Safety Confirmation

- ApplyMode was not run.
- No destination invoice tables were inserted into.
- No browser/PowerShell execution path was added.
- Branch agent still has no central DB credentials.
- `EnableSend=false` remains the committed default.
- Batch apply remains blocked.
