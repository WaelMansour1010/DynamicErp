# POS Performance Deployment Runbook

This folder contains the final approved POS performance package. It intentionally excludes experimental indexes that made invoice save slower during the 120-worker mixed load test on the local `Cash` test database.

## Files

| File | Purpose |
|---|---|
| `01_Apply_Final_Pos_Performance_Procedures.sql` | Final stored procedures only. No index creation. |
| `02_Rollback_Experimental_Indexes.sql` | Safely drops experimental `IX_POS_*` indexes if they were applied before. |
| `03_Optional_SQL_Server_Memory_Settings.sql` | Optional SQL Server memory cap script. |
| `04_WebConfig_ConnectionString_Recommendation.txt` | Recommended connection string tuning. |
| `05_Test_Commands.ps1` | Audit and Mixed 120-user verification commands. |

## What To Run First

1. Take a full database backup.
2. Confirm the target database is the POS/Cash database, not the old SatriahMain script target.
3. Run `02_Rollback_Experimental_Indexes.sql` first if any experimental `IX_POS_*` indexes were previously deployed.
4. Run `01_Apply_Final_Pos_Performance_Procedures.sql`.
5. Apply the Web.config connection string recommendation during the deployment window if approved.
6. Optionally run `03_Optional_SQL_Server_Memory_Settings.sql` only after confirming RAM and server role.

## What Not To Run

- Do not deploy any untested reporting indexes from earlier experiments.
- Do not change `MAXDOP` or `cost threshold for parallelism` from this package.
- Do not mirror this package into `F:\Source Code\SatriahMain\Main Script\AllScripts.sql`.
- Do not run the Mixed load command on production without explicit approval because it creates invoices.

## Required Backup Step

Before any SQL execution:

```sql
BACKUP DATABASE [Cash]
TO DISK = N'D:\SqlBackups\Cash_before_pos_performance_yyyymmdd.bak'
WITH INIT, COMPRESSION;
```

Adjust the backup path to the server backup standard.

## Maintenance Window Steps

1. Stop or reduce POS traffic if possible.
2. Back up the database.
3. Run `02_Rollback_Experimental_Indexes.sql`.
4. Run `01_Apply_Final_Pos_Performance_Procedures.sql`.
5. Update Web.config connection string:

```text
Max Pool Size=200;Connect Timeout=30;Pooling=True;
```

6. Recycle the IIS application pool.
7. Ask POS users to logout/login once after deployment if session/cookie changes were also included.

## Verification Steps

1. Open POS sales screen.
2. Save a new invoice.
3. Open reports page and confirm no heavy report runs until pressing the report/search button.
4. Run the read-only audit command from `05_Test_Commands.ps1`.
5. On a test copy, run the Mixed 120-user command.
6. Confirm:
   - Failure count is `0` or every failure has a clear known reason.
   - No connection pool timeout errors.
   - No SQL deadlocks.
   - No duplicate invoice numbers or `Transaction_ID`.
   - Invoice save latency does not regress against the approved baseline.

## Approved Baseline From Local Cash Test

Mixed 120 workers for 10 minutes:

| State | Save Count | Failures | Avg Save | Max Save | P95 Save |
|---|---:|---:|---:|---:|---:|
| Before experimental indexes | 7,554 | 0 | 2,170 ms | 4,104 ms | 2,701 ms |
| After experimental indexes | 5,914 | 0 | 3,047 ms | 5,387 ms | 3,510 ms |

Decision: keep the stored procedures, do not deploy the experimental indexes.

## Rollback Steps

If deployment causes unexpected behavior:

1. Run `02_Rollback_Experimental_Indexes.sql` to remove any candidate indexes.
2. Restore the previous Web.config connection string if needed.
3. Stored procedure rollback should use the database backup or the previous approved procedure script, because this package uses `DROP` then `CREATE` for SQL Server 2012 compatibility.

## Expected Result

- Reporting/dashboard/accounting heavy reads use stored procedures instead of large inline application queries.
- No experimental indexes are deployed.
- Connection pool headroom is explicit with `Max Pool Size=200`.
- SQL Server memory is capped only if the optional memory script is approved.
- Save performance is not harmed by unvalidated indexes.
