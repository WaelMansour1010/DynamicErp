# Branch Agent Pilot Deployment Package

## Package Contents

- `SyncBranchAgent.exe`
- `SyncBranchAgent.exe.config`
- `BranchSyncAgent.config.template`
- install/uninstall/start/stop/restart scripts
- config generation script
- `README.md`
- `TROUBLESHOOTING.md`
- `BRANCH_AGENT_PILOT_CHECKLIST.md`

## Safe Defaults

The committed package configuration keeps:

- `BranchAgent.EnableSend=false`
- `BranchAgent.DryRunSend=true`
- `BranchAgent.RequireHttps=true`

This means the first pilot mode is read-only collection:

```cmd
SyncBranchAgent.exe --console --once
```

## Controlled Central Test Send

Only after the checklist is completed against a central test API/DB:

1. Set the token as an environment variable for the service account.
2. Use a test-only generated config with `EnableSend=true`.
3. Send heartbeat first:

```cmd
SyncBranchAgent.exe --console --heartbeat-only
```

4. Send one queued payload only:

```cmd
SyncBranchAgent.exe --console --send-one-payload
```

The branch agent never runs ApplyMode. Central acceptance means only `Sync_Outbox`/monitoring rows are written.

## Local Pilot Validation On 2026-05-06

Package path:

```text
F:\Source Code\DynamicErp\Areas\Sync\DeploymentPackages\BranchAgentPilotPackage_20260506
```

Read-only pilot command:

```cmd
SyncBranchAgent.exe --console --once
SyncBranchAgent.exe --console --health
```

Read-only health result:

```json
{"BranchId":10,"MachineName":"WAEL","AgentVersion":"0.0.0.0","ConfigVersion":"1.0","PayloadSchemaVersion":"1.0","LastScanUtc":"\/Date(1778089437683)\/","LastSendUtc":null,"LastHeartbeatUtc":null,"PendingLocalOutboxCount":1,"FailedLocalOutboxCount":0,"SendEnabled":false,"DryRunSend":true,"CentralConnectivityOk":false,"CentralConnectivityMessage":"Send disabled; central connectivity was not tested."}
```

Controlled send test used a temporary local/test config only:

```cmd
SyncBranchAgent.exe --console --heartbeat-only
SyncBranchAgent.exe --console --send-one-payload
SyncBranchAgent.exe --console --health
```

Controlled send health result:

```json
{"BranchId":10,"MachineName":"WAEL","AgentVersion":"0.0.0.0","ConfigVersion":"1.0","PayloadSchemaVersion":"1.0","LastScanUtc":"\/Date(1778089437683)\/","LastSendUtc":"\/Date(1778089438645)\/","LastHeartbeatUtc":"\/Date(1778089438436)\/","PendingLocalOutboxCount":0,"FailedLocalOutboxCount":0,"SendEnabled":true,"DryRunSend":false,"CentralConnectivityOk":true,"CentralConnectivityMessage":"Central API reachable."}
```

Central test DB result:

- `Sync_Outbox` accepted `10:Invoice:29327` with status `Pending`.
- `Sync_BranchHeartbeat` updated branch version and last payload metadata.
- `Sync_BranchUpload` recorded `Accepted`.
- `dbo.Transactions` is not present in `SyncAdminTest`; no destination invoice rows were inserted.
- `ApplyMode` was not executed.

Screenshot:

```text
F:\Source Code\DynamicErp\Areas\Sync\Docs\Screenshots\branch-agent-pilot-dashboard.png
```

## Installation

Dry run:

```powershell
.\Scripts\Install-BranchSyncAgent.ps1 -BinaryPath "C:\Satriah\SyncBranchAgent\SyncBranchAgent.exe" -ServiceAccount "NT AUTHORITY\LocalService" -DryRun
```

Install:

```powershell
.\Scripts\Install-BranchSyncAgent.ps1 -BinaryPath "C:\Satriah\SyncBranchAgent\SyncBranchAgent.exe" -ServiceAccount "NT AUTHORITY\LocalService"
```

No real password should be committed. Configure domain service account passwords through approved operations procedures only.
