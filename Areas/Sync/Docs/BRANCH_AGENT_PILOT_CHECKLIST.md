# Branch Agent Controlled Pilot Checklist

Warning: Do not enable ApplyMode. Do not enable batch apply. Do not insert destination invoices. Use central test API/DB only.

## Pilot Identity

- BranchId:
- Branch machine name:
- Pilot operator:
- Technical owner:
- Date/time:

## Connections

- Local POS DB server/database:
- Local POS DB connection method: Windows Integrated Security / other approved method
- Central test API URL:
- Central test DB:
- Token environment variable name:
- Token configured for service account: Yes / No

## Service Account

- Windows service account:
- Has local POS DB read access: Yes / No
- Has outbox folder write access: Yes / No
- Has log folder write access: Yes / No
- Has no central SQL credentials: Yes / No

## Paths

- Agent install folder:
- Outbox folder:
- Log folder:
- Watermark file:

## Safety Settings

- Committed config has `BranchAgent.EnableSend=false`: Yes / No
- Read-only pilot command completed: Yes / No
- Heartbeat-only test completed before payload send: Yes / No
- One-payload send used for payload test: Yes / No
- Batch apply remains blocked: Yes / No
- ApplyMode remains disabled: Yes / No

## Health Command

Command:

```cmd
SyncBranchAgent.exe --console --health
```

Paste result:

```json

```

## Controlled Read-Only Pilot

Command:

```cmd
SyncBranchAgent.exe --console --once
```

Expected:

- scans local DB
- creates local outbox files
- does not send because `EnableSend=false`

Result:

## Controlled Send Test

Heartbeat first:

```cmd
SyncBranchAgent.exe --console --heartbeat-only
```

Expected:

- central test dashboard updates heartbeat only
- no invoice destination insert

One payload second:

```cmd
SyncBranchAgent.exe --console --send-one-payload
```

Expected:

- exactly one local pending payload is sent
- central accepts into `Sync_Outbox`
- no ApplyMode
- no destination invoice insert

Result:

## Sign Off

- Business owner approval:
- Technical owner approval:
- Pilot accepted / rejected:
