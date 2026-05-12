# POS Server Tuning Package - Byte

This folder contains the ready-to-run production tuning package for the POS/Keshni deployment using database `Byte`.

## Files

- `Web.Byte.Production.Tuned.config`
  - Full Web.config copy prepared for database `Byte`.
  - Uses fixed `machineKey`.
  - Sets `debug="false"`.
  - Adds connection pooling settings:
    `Max Pool Size=200;Min Pool Size=10;Connect Timeout=30;Pooling=True`

- `01_SQL_Server_64GB_Recommended_Settings.sql`
  - Sets SQL memory for a 64GB server where IIS and SQL Server are on the same machine.
  - Recommended:
    - Min SQL memory: `8192 MB`
    - Max SQL memory: `49152 MB`
  - Enables `optimize for ad hoc workloads`.
  - Does not change `MAXDOP` or `cost threshold`.

- `02_Grant_SystemHealth_ViewServerState.sql`
  - Grants `VIEW SERVER STATE` to `cayshny_pos_app`.
  - Required for full System Health Dashboard SQL Server metrics.

- `03_Check_SQL_Server_Tuning_Status.sql`
  - Read-only verification for memory/settings.

- `04_Apply_IIS_AppPool_Tuning.ps1`
  - Sets IIS App Pool:
    - `AlwaysRunning`
    - no idle timeout
    - no periodic recycle
    - queue length 5000

- `05_Deploy_Tuned_WebConfig.ps1`
  - Backs up the current production `Web.config`.
  - Deploys `Web.Byte.Production.Tuned.config`.

## Execution Order

1. Take a backup of the current production `Web.config`.
2. Run SQL memory settings as sysadmin:

```sql
:r 01_SQL_Server_64GB_Recommended_Settings.sql
```

3. Grant System Health permission as sysadmin:

```sql
:r 02_Grant_SystemHealth_ViewServerState.sql
```

4. Apply IIS tuning from an elevated PowerShell:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\04_Apply_IIS_AppPool_Tuning.ps1 -AppPoolName "cayshny"
```

5. Deploy tuned Web.config:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\05_Deploy_Tuned_WebConfig.ps1 -SiteRoot "C:\WWWSite\cayshny"
```

6. Recycle App Pool manually once after deployment.
7. POS users must Logout/Login once to regenerate `POSCTX`.

## Verification

Run:

```sql
:r 03_Check_SQL_Server_Tuning_Status.sql
```

Then open:

- POS Sales screen
- System Health Dashboard
- Reports screen

Expected:

- No `POS session context is missing`.
- System Health no longer shows `VIEW SERVER STATE` warning after grant.
- Response time should improve after app warm-up and SQL cache warming.

## Notes

- Do not apply experimental indexes from previous tests here.
- Do not change `MAXDOP` or `cost threshold` until wait stats justify it.
- If SQL Server is on a dedicated DB-only server, max memory can be reviewed upward, usually around `55296 MB`.

