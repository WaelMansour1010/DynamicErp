# Kishny POS SQL Auto-Update Deployment Checklist

## Package Contents

- `Sql/`: approved POS SQL auto-update manifest and scripts.
- `Tools/Invoke-PosSqlAutoUpdate.ps1`: standalone idempotent POS SQL updater.
- `Docs/`: deployment and verification notes.

## Required Inputs From Customer Server

- Production SQL Server name.
- Production POS database name.
- Approved SQL authentication method or integrated security account.
- Verified backup location and restore procedure.

Do not edit SQL scripts on the customer server. If a script must change, stop deployment and rebuild the package.

## Deployment Order

1. Stop POS users or schedule a maintenance window.
2. Take a full production database backup.
3. Verify that the backup can be restored.
4. Open PowerShell from this package root.
5. Run DryRun with a customer connection string:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Invoke-PosSqlAutoUpdate.ps1 -ConnectionString "Data Source=<SERVER>;Initial Catalog=<DATABASE>;Integrated Security=True;MultipleActiveResultSets=True" -Mode DryRun
```

6. Review the output. Continue only when there are no hash mismatch warnings.
7. Run Apply:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Invoke-PosSqlAutoUpdate.ps1 -ConnectionString "Data Source=<SERVER>;Initial Catalog=<DATABASE>;Integrated Security=True;MultipleActiveResultSets=True" -Mode Apply -StopOnError -ReleaseNo POS_ProductionRelease_20260509
```

8. Run DryRun again and confirm:

- `Pending=0`
- `HashMismatch=0`
- all approved scripts are `SkippedAlreadyApplied`

9. Start the web application.
10. Verify POS login, default loading, transaction screen, print/report screens, payment/cashing screens, KYC/card screens, and permissions.
11. Save the updater log with the deployment record.

## Manual Scripts

Files prefixed with `MANUAL_` are intentionally excluded from automatic execution. They are diagnostics, rollback helpers, or server-level permission scripts that require explicit DBA review.

## Web Configuration Notes

- Configure the production POS connection string on the customer server.
- Do not deploy developer server names, database names, or credentials.
- For web farms, align `machineKey` and session/cookie settings across nodes.
- Keep debug/development switches disabled.
- Recommended POS connection options after DBA approval: `Pooling=True;Max Pool Size=200;Connect Timeout=30`.

## Stop Conditions

Stop deployment immediately if:

- DryRun shows `HashMismatch`.
- Apply reports a failed script.
- The updater says the database does not match POS probe objects.
- A manual script appears required and no DBA has approved it.

Rollback must be DBA-led from the verified pre-deployment backup.
