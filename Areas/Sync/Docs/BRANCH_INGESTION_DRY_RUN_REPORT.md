# Branch Ingestion API and Dry-Run Report

## Scope

Local/test only.

- Central test DB: `Wael\Sql2019 / SyncAdminTest`
- Branch test DB: `Wael\Sql2019 / BranchPosTest`
- BranchId: `10`
- Test SyncKey: `10:Invoice:29327`
- Test token environment variable: `SATRIAH_BRANCH_SYNC_TOKEN_10`
- Token value used only for local test: `local-test-token-not-secret`

No production database was touched.

## Central API Endpoints

- `POST /sync/api/branch/heartbeat`
- `POST /sync/api/branch/outbox`
- `POST /sync/api/branch/outbox/{syncKey}/ack`

The API validates:

- branch token
- branch id header/body match
- replay timestamp window
- optional HMAC signature
- invoice payload schema
- SHA-256 `PayloadHash`
- duplicate `SyncKey`

The API writes only central intake tables:

- `Sync_Outbox`
- `Sync_Log`
- `Sync_Error`
- `Sync_BranchHeartbeat`
- `Sync_BranchUpload`

It does not insert into destination invoice, payment, stock, journal, or accounting tables.

## SQL Applied

Applied to `SyncAdminTest` only:

```cmd
sqlcmd -S "Wael\Sql2019" -E -d SyncAdminTest -i "F:\Source Code\DynamicErp\Areas\Sync\Sql\003_Sync_BranchIngestion.sql" -b
```

Created/verified:

- `Sync_Outbox`
- `Sync_Log`
- `Sync_Error`
- `Sync_BranchHeartbeat`
- `Sync_BranchUpload`

## Branch Dry Run

Generated test config used:

- local DB: `BranchPosTest`
- central URL: `http://localhost:8096`
- `EnableSend=false`
- `DryRunSend=true`
- `RequireHttps=false` for localhost test only
- test output path: `SyncBranchAgent/TestRun`

Command:

```cmd
SyncBranchAgent.exe --console --once
```

Result:

- one local invoice candidate read from `BranchPosTest.dbo.Transactions`
- local outbox file created: `TestRun/outbox/pending/10_Invoice_29327.json`
- watermark written
- log written
- no central API call made

## Test Send

Generated test config changed to:

- `EnableSend=true`
- `DryRunSend=false`
- `RequireHttps=false` for localhost test only

Command:

```cmd
SyncBranchAgent.exe --console --once
```

Result:

- pending payload was signed and posted to `/sync/api/branch/outbox`
- heartbeat was signed and posted to `/sync/api/branch/heartbeat`
- local outbox moved to `TestRun/outbox/sent/10_Invoice_29327.json`

Central result:

| Table | Result |
| --- | --- |
| `Sync_Outbox` | 1 row, `Status=Pending`, `EntityKey=10:Invoice:29327` |
| `Sync_BranchHeartbeat` | 1 row, `BranchId=10`, `PendingOutboxCount=0`, `LastPayloadSyncKey=10:Invoice:29327` |
| `Sync_BranchUpload` | 1 accepted row |
| `Sync_Log` | payload pending log + heartbeat log |
| `Sync_Error` | 0 rows for the test SyncKey |

Duplicate same-payload POST:

- returned HTTP `200`
- status `DuplicateAccepted`
- did not create duplicate `Sync_Outbox`

Destination invoice table check in `SyncAdminTest`:

- `dbo.Transactions` does not exist
- no destination invoice insert occurred

## Central UI Result

Verified:

- `/sync` shows branch heartbeat/recent uploads
- `/sync/queue?SyncKey=10%3AInvoice%3A29327` shows the pending payload
- `/sync/diagnostics?syncKey=10%3AInvoice%3A29327` shows logs/checks
- `/sync/logs?syncKey=10%3AInvoice%3A29327` shows the ingestion log
- `POST /sync/apply/requestapply` still returns HTTP `403`

## Safety Confirmation

- No ApplyMode execution.
- No destination inserts.
- No batch apply.
- Branch agent has no central DB credentials.
- `EnableSend=false` remains the committed default.
- No production DB touched.
