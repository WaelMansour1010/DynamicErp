# Branch Sync Agent Service

## Purpose

`SyncBranchAgent` is a lightweight Windows Service/Worker intended to run on each POS branch machine.

It is branch-side only:

- scans the local POS database
- detects candidate invoice rows using a watermark
- creates local outbox payload files
- retries safely while offline
- sends payloads to the central server over HTTPS API when enabled
- sends heartbeat messages
- writes local rolling logs

It does not replace the central Sync Admin UI. It does not apply invoices, approve operations, write central accounting data, or run any ApplyMode path.

## Safety Model

- No direct central DB connection is required.
- No central DB password should exist on the branch machine.
- Central communication is HTTPS API only.
- API token is read from an environment variable, not committed config.
- Outbox is local and durable; offline failures keep payloads pending.
- Batch apply is not implemented.
- Branch agent is collector/sender only; central server controls approval and apply.

## Local Configuration

`App.config` contains only a local POS database connection and safe agent settings. Prefer Windows Integrated Security:

```xml
<add name="BranchLocalDb"
     connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=BranchPosTest;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
     providerName="System.Data.SqlClient" />
```

Central API token:

```cmd
setx SATRIAH_BRANCH_SYNC_TOKEN_10 "token-from-central-admin" /M
```

Do not commit real tokens or SQL passwords.

## Important Settings

- `BranchAgent.BranchId`: branch identifier used in `SyncKey`
- `BranchAgent.PollSeconds`: scan interval
- `BranchAgent.BatchSize`: max invoice payloads per scan/send cycle
- `BranchAgent.EnableSend`: `false` by default; when false the agent scans and queues locally only
- `BranchAgent.DryRunSend`: when true and sending is enabled, logs what would be sent without calling the central API
- `BranchAgent.CentralApiBaseUrl`: HTTPS central API base URL
- `BranchAgent.ApiTokenEnvironmentVariable`: environment variable containing the bearer token
- `BranchAgent.OutboxPath`: durable local pending/sent/failed payload files
- `BranchAgent.WatermarkPath`: local watermark JSON file
- `BranchAgent.LogPath`: local log directory
- `BranchAgent.InvoiceCandidateQuery`: optional customer-specific invoice candidate SQL
- `BranchAgent.ConfigVersion`: branch config version included in heartbeat
- `BranchAgent.PayloadSchemaVersion`: payload contract version included in payloads and heartbeat
- `BranchAgent.MaxRetryCount`: failed sends before quarantine
- `BranchAgent.MaxRetryDelaySeconds`: cap for exponential backoff

## Default Candidate Query

The default scanner reads from `dbo.Transactions` using:

- `Transaction_ID`
- `branch_no`
- `NoteSerial`
- `NoteSerial1`
- `NoteId`
- `Transaction_Date`
- `Total`
- `Paid`
- `Remain`

Customers with different schema/filters should set `BranchAgent.InvoiceCandidateQuery`. The query must return `Transaction_ID` or `TransactionId`, and it receives:

- `@BranchId`
- `@LastTransactionId`
- `@BatchSize`

## Console Test

Run one local scan without sending:

```cmd
SyncBranchAgent.exe --console --once
```

Health diagnostics:

```cmd
SyncBranchAgent.exe --console --health
```

The health command prints JSON with last scan/send/heartbeat times, local pending/failed counts, configured versions, and central API connectivity status. It never prints the API token.

Expected result:

- reads local POS database
- creates payloads under `%ProgramData%\Satriah\BranchSyncAgent\outbox\pending`
- updates `%ProgramData%\Satriah\BranchSyncAgent\watermark.json`
- writes logs under `%ProgramData%\Satriah\BranchSyncAgent\logs`
- does not contact central server unless `EnableSend=true`

For a safe send rehearsal, use a test-only config with:

- `BranchAgent.EnableSend=true`
- `BranchAgent.DryRunSend=true`
- local/test central API URL

For a test send to central API, set `DryRunSend=false` only in a local/test environment.

## Windows Service Install

Use the scripts in `Scripts` after publishing/copying the compiled folder:

```powershell
.\Scripts\Install-BranchSyncAgent.ps1 -BinaryPath "C:\Satriah\SyncBranchAgent\SyncBranchAgent.exe" -ServiceAccount "NT AUTHORITY\LocalService" -DryRun
.\Scripts\Install-BranchSyncAgent.ps1 -BinaryPath "C:\Satriah\SyncBranchAgent\SyncBranchAgent.exe" -ServiceAccount "NT AUTHORITY\LocalService"
.\Scripts\Start-BranchSyncAgent.ps1
.\Scripts\Restart-BranchSyncAgent.ps1
.\Scripts\Stop-BranchSyncAgent.ps1
.\Scripts\Uninstall-BranchSyncAgent.ps1 -DryRun
```

The install script sets Windows Service recovery to restart the service after failure. For domain/service accounts, set the password securely outside source control using approved operations procedures.

Legacy `sc.exe` equivalent:

```cmd
sc create SatriahBranchSyncAgent binPath= "C:\Satriah\SyncBranchAgent\SyncBranchAgent.exe" start= delayed-auto
sc description SatriahBranchSyncAgent "Satriah POS branch sync collector and sender"
sc failure SatriahBranchSyncAgent reset= 86400 actions= restart/60000/restart/120000/restart/300000
sc start SatriahBranchSyncAgent
```

Run the service under a dedicated Windows account with:

- read access to local POS database
- write access to `%ProgramData%\Satriah\BranchSyncAgent`
- no central SQL permissions

## Central API Contract Draft

Outbox endpoint:

```http
POST /sync/api/branch/outbox
Authorization: Bearer <token>
Content-Type: application/json
```

Heartbeat endpoint:

```http
POST /sync/api/branch/heartbeat
Authorization: Bearer <token>
Content-Type: application/json
```

Outbox endpoint:

```http
POST /sync/api/branch/outbox
Authorization: Bearer <token>
X-Branch-Id: 10
X-Branch-Timestamp: 2026-05-06T19:23:54.0000000Z
X-Payload-Hash: <sha256 hex>
X-Signature: <base64 hmac>
Content-Type: application/json
```

Central server should persist received payloads into central intake/queue tables and keep Apply approval under the existing Sync Admin controls.

## Recovery And Retry

If the network is offline, payloads stay in local `pending`. Failed sends update `TryCount`, `LastAttemptAtUtc`, `NextAttemptAtUtc`, and retry with exponential backoff up to `BranchAgent.MaxRetryDelaySeconds`. After `BranchAgent.MaxRetryCount`, the payload moves to `failed` quarantine for operator review. A payload moves to `sent` only after central API acceptance, so duplicate sends are safe and idempotent by `SyncKey` + `PayloadHash`.

## Security

- Token values are read from environment variables and are never written to config or logs.
- Branch requests must include `X-Branch-Timestamp`, `X-Payload-Hash`, and `X-Signature`.
- Central API rejects unsigned payloads by default.
- Central replay window is 15 minutes.
- HTTPS is required when `BranchAgent.RequireHttps=true`.
- Branch machines must not store central SQL credentials.
