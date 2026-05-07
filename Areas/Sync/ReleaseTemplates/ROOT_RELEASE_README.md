# Data Sync Pilot Release

This release contains two separate deliverables for a controlled customer pilot:

1. Server/Central package
2. Branch/Client Agent package

## Safety Defaults

- ApplyMode is disabled.
- Batch apply is disabled.
- No destination invoices are inserted automatically.
- No production credentials are included.
- No plaintext passwords are included.
- Branch Agent default is `EnableSend=false`.
- Branch Agent token is read from an environment variable only.

## Package Structure

```text
DataSyncPilotRelease_YYYYMMDD/
  Server/
    Files/
    Sql/
    Config/
    Docs/
  BranchAgent/
    Binaries/
    Scripts/
    Config/
    Docs/
  Checklists/
  README.md
```

## First Pilot Flow

1. Deploy server package to central test ERP.
2. Apply SQL manually in approved order.
3. Configure `SyncAdminConnection` without plaintext passwords.
4. Configure branch token environment variables.
5. Install branch agent with safe defaults.
6. Run branch `--health`.
7. Run branch `--once` with `EnableSend=false`.
8. Enable test send config only for controlled test.
9. Run `--heartbeat-only`.
10. Run `--send-one-payload`.
11. Review `/sync/queue` and `/sync/diagnostics`.

No ApplyMode step is included in this pilot package.
