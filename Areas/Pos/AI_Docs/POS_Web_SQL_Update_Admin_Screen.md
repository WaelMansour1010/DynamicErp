# POS Web SQL Update Admin Screen

Date: 2026-05-10  
Project: DynamicErp  
Area: Areas\Pos  
Screen: `/Pos/PosSqlUpdates/Index`  
Final verdict: READY FOR CONTROLLED CUSTOMER USE

## Purpose

The POS SQL update flow is now available from inside the web application for POS administrators. The existing PowerShell updater remains available as a fallback/developer tool, but customer operation no longer depends on manually running PowerShell.

## Implemented Files

- `Areas\Pos\Controllers\PosSqlUpdatesController.cs`
- `Areas\Pos\Services\PosSqlAutoUpdateService.cs`
- `Areas\Pos\Models\PosSqlUpdateModels.cs`
- `Areas\Pos\Views\PosSqlUpdates\Index.cshtml`
- `Areas\Pos\Sql\POS_SQL_AutoUpdate_Manifest.json`
- `Areas\Pos\Tools\Invoke-PosSqlAutoUpdate.ps1`
- `Areas\Pos\Views\PosDashboard\_Sidebar.cshtml`
- `Areas\Pos\Controllers\PosDashboardController.cs`
- `MyERP.csproj`
- `Areas\Pos\Sql\45_POS_ExcelImport.sql`

## Screen Capabilities

- Shows current database/server/module status.
- Reads the approved manifest from `Areas\Pos\Sql\POS_SQL_AutoUpdate_Manifest.json`.
- Lists scripts in execution order.
- Shows script order, script name, purpose, status, applied date, applied by, hash, and last failed error summary.
- Shows manual scripts as Manual and never applies them automatically.
- Supports DryRun from the web screen.
- Supports Apply Pending Updates from the web screen.
- Requires a backup confirmation checkbox before Apply.
- Uses AntiForgeryToken for POST actions.
- Blocks Apply on non-POS database probe failure.
- Blocks Apply on hash mismatch.
- Runs each script independently inside a SQL transaction.
- Stops on the first failed script.
- Logs web Apply runs into `dbo.POS_SqlUpdateRun`.
- Logs script results into `dbo.POS_SqlUpdateHistory`.
- Records web user id, username, client IP, and date in the run row.
- Provides a download log link for run details.
- Does not expose connection strings or passwords in the UI.
- Does not allow arbitrary SQL upload or user-supplied script paths.

## Security

Access is limited to POS admin users:

- `UserType == 0`, or
- `PosUserContext.IsFullAccess == true`

Non-admin POS users receive HTTP 403 Access Denied.

Apply is protected by:

- POS admin session requirement.
- AntiForgeryToken.
- Backup confirmation checkbox.
- manifest-only script discovery.
- whitelist path resolution under `Areas\Pos\Sql`.
- POS probe objects:
  - `dbo.Transactions`
  - `dbo.TblUsers`
  - `dbo.TblBranchesData`
  - `dbo.TBLClosePos`

## SQL Compatibility

The web updater uses `System.Data.SqlClient`, SQL Server transactions, and SQL Server 2012-compatible DDL for history/run tables. Batch splitting supports `GO`.

## Testing Performed

Build:

- Command: `MSBuild.exe MyERP.csproj /p:Configuration=Release /p:Platform=AnyCPU /v:minimal`
- Result: succeeded.
- Existing unrelated warnings remain.

Web DryRun on CASH:

- Logged in as POS admin `admin`.
- Opened `/Pos/PosSqlUpdates/Index`.
- Posted DryRun successfully.
- Result: screen rendered and detected current script status.
- CASH currently reports hash mismatch because it had previously been updated with a package/test hash. This proves the web screen detects and blocks changed already-applied scripts.
- Evidence: `Areas\Pos\AI_Docs\web_sql_updates_cash_dryrun.html`

Web Apply on disposable restored POS database:

- Created disposable restored database: `POSWebSqlUpdate_20260510`.
- Temporarily pointed `KishnyCashConnection` to this disposable database for the web test.
- Removed only updater history tables from the disposable database to force a fresh update path.
- Logged in as POS admin `admin`.
- Ran Apply from the web screen with backup checkbox checked.
- First attempt found and safely stopped on a real source SQL defect in `45_POS_ExcelImport.sql`.
- Fixed `45_POS_ExcelImport.sql` to match `Areas\Pos\Data\PosSqlRepository.cs`.
- Reran Apply from the web screen.
- Result: remaining 10 scripts applied successfully after the first 16 already-successful scripts.
- Reran Apply again from the web screen.
- Result: 0 applied, 26 skipped.
- Run rows recorded user `admin` and client IP `::1`.
- Evidence:
  - `Areas\Pos\AI_Docs\web_sql_updates_disposable_apply_after_fix.html`
  - `Areas\Pos\AI_Docs\web_sql_updates_disposable_rerun_after_fix.html`
  - `Areas\Pos\AI_Docs\web_sql_updates_disposable_db_state.txt`

Hash mismatch from web:

- Temporarily modified an already-applied script.
- Posted DryRun from the web screen.
- Result: `اختلاف Hash` was displayed and execution was blocked.
- Script file was restored immediately after the test.
- Evidence: `Areas\Pos\AI_Docs\web_sql_updates_hash_mismatch.html`

Non-admin access:

- Logged in as non-admin POS user `EC002`.
- Opened `/Pos/PosSqlUpdates/Index`.
- Result: HTTP 403 Forbidden.

Non-POS database block:

- Attempted to point the web app to `Eng`.
- POS login did not establish a usable POS session on that database, so the screen could not be opened.
- The service itself also blocks Apply when required POS probe objects are missing.

PowerShell fallback:

- Ran `Areas\Pos\Tools\Invoke-PosSqlAutoUpdate.ps1` against the disposable database after web Apply.
- Result: `Pending=0`, `Skipped=26`, `HashMismatch=0`.
- Evidence: `Areas\Pos\AI_Docs\web_sql_updates_powershell_fallback_dryrun.log`

Browser/UI verification:

- In-app browser opened `/Pos/PosSqlUpdates/Index`.
- DOM verified the Arabic screen title, DryRun button, backup checkbox, Apply button, and download-log link.
- Screenshot capture was attempted but the browser capture API timed out; saved HTML evidence is available in AI_Docs.

## Operational Use

Customer deployment order:

1. Deploy web application and POS SQL files.
2. Confirm production POS connection string.
3. Take and verify a full SQL backup.
4. Login as POS admin.
5. Open `/Pos/PosSqlUpdates/Index`.
6. Click `فحص بدون تنفيذ`.
7. Confirm no hash mismatch and review pending scripts.
8. Check the backup confirmation checkbox.
9. Click `تطبيق التحديثات المنتظرة`.
10. Download and archive the run log.
11. Run DryRun again and verify no pending scripts remain.
12. Smoke test POS login, defaults, transaction screen, reports/print/payment/KYC paths.

## Remaining Risks

- Production backup remains mandatory and outside the application.
- Manual scripts remain DBA-reviewed and excluded from automatic execution.
- If a hash mismatch appears in production, do not apply. Stop and compare the script file with the version previously deployed.
- If Apply fails, the failing script transaction rolls back, execution stops, and the log must be reviewed before retry.

## Final Verdict

READY.

The customer can now review and apply approved POS SQL updates from inside the web application using an admin-only Arabic UI. PowerShell remains available only as a fallback.
