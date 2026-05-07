# Data Sync Pilot Operating Mode

## Critical Safety

- ApplyMode remains disabled.
- Batch apply remains disabled.
- Branch agent does not insert destination invoices.
- Branch agent does not store central DB credentials.
- Branch committed default is `EnableSend=false`.

## Phase 1 - Local Read-Only Branch Scan

On the branch machine:

```cmd
SyncBranchAgent.exe --console --health
SyncBranchAgent.exe --console --once
SyncBranchAgent.exe --console --health
```

Expected:

- local POS DB is scanned
- local outbox file is created
- no payload is sent because `EnableSend=false`
- central dashboard is unchanged

## Phase 2 - Heartbeat Only

Use a pilot/test config with `EnableSend=true` only after approval.

```cmd
SyncBranchAgent.exe --console --heartbeat-only
```

Expected:

- server dashboard shows branch online
- no payload is sent
- no destination invoice insert

## Phase 3 - One Payload Only

```cmd
SyncBranchAgent.exe --console --send-one-payload
```

Expected:

- exactly one pending local payload is sent
- central server inserts/updates `Sync_Outbox` only
- queue item remains pending for review
- no ApplyMode
- no destination invoice insert

## Phase 4 - Manual Review

Use central UI:

- `/sync/queue`
- `/sync/diagnostics`
- `/sync/logs`
- `/sync/pilot`

Review payload hash, SyncKey, branch, duplicate/conflict status, and branch heartbeat before any future apply approval.
