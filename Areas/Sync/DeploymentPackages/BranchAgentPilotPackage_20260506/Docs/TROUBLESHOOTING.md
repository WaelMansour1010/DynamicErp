# Branch Sync Agent Troubleshooting

## Safety First

- Do not enable ApplyMode from the branch machine.
- Do not store central SQL credentials on the branch machine.
- Keep `BranchAgent.EnableSend=false` until the central test API, token, and checklist are approved.
- Use `--heartbeat-only` before sending any payload.
- Use `--send-one-payload` only for one queued payload in a central test environment.

## Health Command

```cmd
SyncBranchAgent.exe --console --health
```

Expected safe default:

- `SendEnabled=false`
- `DryRunSend=true`
- local pending/failed counts visible
- no token value printed

## Common Issues

### Local DB Connection Fails

Check:

- service account has read access to the local POS DB
- `BranchAgent.LocalDbConnectionName` points to the correct connection string
- SQL Server service is running

### No Payloads Created

Check:

- `BranchAgent.BranchId`
- watermark file path
- source `Transactions` rows exist above `LastTransactionId`
- optional `BranchAgent.InvoiceCandidateQuery`

### Payloads Stay Pending

This is expected when `EnableSend=false` or the central API is offline.

Check:

- `NextAttemptAtUtc`
- local log folder
- central API URL
- HMAC token environment variable

### HTTP 403 From Central API

Likely causes:

- bad token
- missing `X-Signature`
- timestamp outside replay window
- wrong BranchId

The central dashboard records auth failure counts without exposing the token.

### Service Does Not Start

Check:

- the service account can read the executable folder
- the service account can write outbox/log folders
- Windows Event Viewer
- local agent logs

## Recovery

Pending payload files are durable. Restarting the service does not duplicate central rows because the central API is idempotent by `SyncKey` and `PayloadHash`.
