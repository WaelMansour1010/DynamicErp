# POS SQL Auto-Update Final Release Gate

Date: 2026-05-09  
Project: DynamicErp  
Module: Kishny POS / Areas\Pos  
Branch reviewed: claude/improve-report-designer-iAq77  
Release package: Areas\Pos\Releases\POS_ProductionRelease_20260509  
Final verdict: READY

## Executive Summary

The POS SQL auto-update release gate is complete for the local/test targets available in this workspace. No POS-scoped automatic updater existed before this gate; the only existing updater-like code found was outside POS under Sync installer tooling. A POS-only, manifest-driven updater was added under Areas\Pos, with script ordering, SHA256 hashing, run/history tables, dry-run mode, safe stop-on-error behavior, POS database probing, and repeat-run idempotency.

The updater was tested against the configured local/test connection strings from Web.config:

- CASH: `KishnyCashConnection`, database `Cash`.
- ENG: `MainErp_ConnectionString`, database `Eng`.

CASH is the POS target and successfully applied all approved POS scripts. A second run on CASH skipped all already-applied scripts and proved idempotency. ENG was correctly detected as not matching the POS probe objects and was skipped safely on both runs.

One production-blocking SQL issue was found and fixed in `45_POS_ExcelImport.sql`: the script created/import validation objects using column names that did not match the application repository schema. The failed attempt was logged safely in CASH, execution stopped at the failed script, and the corrected script then applied successfully.

## Files Reviewed

- Web.config connection strings.
- Areas\Pos\Sql\*.sql.
- Areas\Pos\Data\PosSqlRepository.cs.
- Areas\Pos\Controllers and views used by login/transaction/dashboard smoke tests.
- Areas\Pos\Tools existing POS audit/test helper scripts.
- Areas\Sync\InstallerTools\DynamicERPSyncServerSetup\Program.cs, only to confirm it is not the POS updater.

## SQL Updater Location

New POS updater:

- Areas\Pos\Tools\Invoke-PosSqlAutoUpdate.ps1.
- Areas\Pos\Sql\POS_SQL_AutoUpdate_Manifest.json.

The updater reads only approved POS scripts from the manifest. It does not read legacy `AllScripts.sql` and does not rely on unrelated modules.

## Updater Behavior Verified

- Reads scripts from Areas\Pos\Sql through the manifest.
- Preserves explicit manifest order, including fractional order 45.1.
- Calculates SHA256 per script.
- Creates and writes to `dbo.POS_SqlUpdateRun` and `dbo.POS_SqlUpdateHistory` during Apply.
- Skips scripts that were already successful with the same hash.
- Blocks and reports same ScriptName with different successful hash.
- Splits SQL batches on `GO`.
- Runs each script inside a SQL transaction.
- Stops on fatal SQL error when `-StopOnError` is set.
- Logs the failed script name, error summary, duration, hash, run id, and release number.
- Detects non-POS database by required objects and skips safely unless explicitly forced.

## POS Database Probe

The updater requires these objects before auto-apply:

- dbo.Transactions
- dbo.TblUsers
- dbo.TblBranchesData
- dbo.TBLClosePos

This prevented POS scripts from being applied to ENG, which has generic ERP objects but is not the POS target database.

## Approved Execution Order

| Order | Script | Purpose | Result on CASH |
|---:|---|---|---|
| 25 | 25_POS_TemporaryPermissions.sql | POS user permissions table | Applied |
| 26 | 26_POS_TellerPermission_ECUsers.sql | Teller permission defaults | Applied |
| 27 | 27_POS_ReportStoredProcedures.sql | POS report stored procedures | Applied |
| 28 | 28_POS_Payments_Audit.sql | Payment/cashing audit columns | Applied |
| 29 | 29_POS_UserCategory_BulkPermissions.sql | User category and bulk permissions | Applied |
| 30 | 30_POS_SaveTransaction_UnicodeText.sql | Latest SaveTransaction Unicode text | Applied |
| 31 | 31_POS_GetNextID_FromSequence_Concurrency.sql | Serialized sequence allocator | Applied |
| 32 | 32_POS_WebInvoiceAuditReport.sql | Web invoice audit report | Applied |
| 33 | 33_POS_JournalEntryAuditColumns.sql | Journal entry audit columns | Applied |
| 34 | 34_POS_PerformanceStoredProcedures.sql | POS performance procedures | Applied |
| 37 | 37_POS_SystemErrorLog.sql | POS system error log | Applied |
| 38 | 38_POS_SystemHealthEncodingHotfix.sql | System health Unicode hotfix | Applied |
| 39 | 39_POS_NonWebLoginUsersReport.sql | Non-web login users report | Applied |
| 40 | 40_POS_Dashboard_DailySnapshots.sql | Dashboard snapshots | Applied |
| 41 | 41_POS_PurchaseInvoice.sql | Purchase invoice support | Applied |
| 42 | 42_POS_StockTransfer.sql | Stock transfer support | Applied |
| 45 | 45_POS_ExcelImport.sql | Excel import audit/mapping tables | Fixed, then applied |
| 45.1 | 45_POS_FinancialIntelligenceReports.sql | Financial intelligence procedures | Applied |
| 46 | 46_POS_SaveTransaction_ConcurrencyIndexes.sql | SaveTransaction concurrency indexes | Applied |
| 47 | 47_POS_SaveAttemptLog.sql | POS save attempt log | Applied |
| 48 | 48_POS_SalesRepresentativesPerformanceDashboard.sql | Sales reps performance dashboard | Applied |
| 49 | 49_POS_SalesRepresentativeTargets.sql | Sales rep targets | Applied |
| 50 | 50_POS_ExcelImportCommitAudit.sql | Excel import commit audit | Applied |
| 51 | 51_POS_PaymentCashing_ReadProcedures.sql | Payment/cashing read procedures | Applied |
| 54 | 54_POS_DoubleEntryVoucherSequence_Fix.sql | Voucher sequence repair | Applied |
| 55 | 55_POS_SaveTransaction_Allocator_Hardening.sql | Allocator hardening | Applied |

## Manual / Excluded Scripts

These files are packaged with a `MANUAL_` prefix and are intentionally not auto-applied:

- 35_POS_SystemHealthPermissions.sql: requires sysadmin/master login and placeholder replacement.
- 36_POS_PerformanceIndexRollback.sql: rollback-only helper.
- 39_POS_Deadlock_Diagnostics.sql: read-only diagnostic.
- 52_POS_EmployeePayroll_MedicalInsurance.sql: pointer to MainErp canonical script.
- 53_POS_DoubleEntryVoucherSequence_Diagnostics.sql: read-only diagnostic.
- 56_POS_RechargeValue_Validation.sql: read-only diagnostic.

## What Was Missing / Fixed

Missing before this gate:

- No POS-local automatic SQL updater.
- No approved POS script manifest.
- No POS SQL history/run logging for customer deployment.
- No automated protection against applying POS SQL to ENG/MainErp.
- No customer-ready POS SQL release package.

Fixed during this gate:

- Added manifest-driven POS updater.
- Added POS-only database probe.
- Added dry-run/apply/report behavior.
- Added success/failure run history.
- Added hash mismatch detection.
- Added release packaging and deployment checklist.
- Corrected `45_POS_ExcelImport.sql` to match the application repository schema and SQL Server 2012 syntax.

## CASH Test Results

Database state was documented before update in:

- Areas\Pos\Releases\POS_ProductionRelease_20260509\TestDbState\Cash_pre_update_state.txt.

Tests run:

- DryRun before apply: Pending=26, Skipped=0, HashMismatch=0.
- Apply first run: stopped safely at `45_POS_ExcelImport.sql` due invalid FK column reference.
- Apply after SQL fix: remaining 10 scripts applied successfully.
- Apply rerun: Pending=0, Skipped=26, HashMismatch=0.
- Final DryRun: Pending=0, Skipped=26, HashMismatch=0.

Final CASH verification:

- `dbo.POS_SqlUpdateHistory`: 26 successful rows, 1 failed historical row from the caught Excel import defect.
- Last updater run: Completed, TotalScripts=0, AppliedCount=0, SkippedCount=26, FailedCount=0.
- `dbo.usp_POS_SaveTransaction`: exists and was updated on 2026-05-09; definition length verified at 49088 characters.

Objects verified after update:

- dbo.GetNextID_FromSequence
- dbo.POS_DashboardSnapshotHeader
- dbo.POS_DashboardSnapshotSmartMetric
- dbo.POS_ImportBatch
- dbo.POS_ImportBatchRow
- dbo.POS_ImportValidationResult
- dbo.POS_SalesRepresentativeTargets
- dbo.POS_SaveAttemptLog
- dbo.POS_SystemErrorLog
- dbo.POS_UserPermissions
- dbo.TBLClosePos
- dbo.Transactions
- dbo.usp_POS_Payments_Search
- dbo.usp_POS_Report_NonWebLoginUsers
- dbo.usp_POS_Report_Run
- dbo.usp_POS_SalesRepresentativesPerformanceDashboard
- dbo.usp_POS_SalesTargets_Save
- dbo.usp_POS_SavePurchaseInvoice
- dbo.usp_POS_SaveStockTransfer
- dbo.usp_POS_SaveTransaction
- dbo.usp_POS_SystemHealth_Database

Indexes verified after update:

- UX_POS_DashboardSnapshotHeader_Filter
- IX_POS_DashboardSnapshotSmartMetric
- IX_POS_ImportBatch_SourceFileHash
- IX_POS_ImportBatchRow_Source
- IX_POS_SalesTargets_ActiveScope
- IX_POS_SaveAttemptLog_SaveAttemptId
- IX_POS_SystemErrorLog_CreatedAt

## ENG Test Results

Database state was documented before update in:

- Areas\Pos\Releases\POS_ProductionRelease_20260509\TestDbState\Eng_pre_update_state.txt.

Tests run:

- DryRun: database did not match POS probe objects; no scripts applied.
- Apply: database did not match POS probe objects; no scripts applied.
- Apply rerun: database did not match POS probe objects; no scripts applied.

Final ENG verification:

- POS updater history tables were not created.
- `dbo.TBLClosePos` does not exist.
- `dbo.usp_POS_SaveTransaction` does not exist.

This is the expected safe behavior for a non-POS target database.

## Web / POS Smoke Tests

Build:

- `MSBuild.exe MyERP.csproj /p:Configuration=Release /p:Platform=AnyCPU /v:minimal`
- Result: succeeded; existing project warnings remain but no POS updater build break was introduced.

IIS Express smoke:

- `/Pos/PosLogin/Index`: HTTP 200.
- `/Pos/PosTransaction/Index` while unauthenticated: HTTP 200 login page.
- `/Pos/PosDashboard/Index` while unauthenticated: HTTP 200 login page.
- Login using local test CASH admin credentials succeeded and redirected to `/Pos`.
- `/Pos/PosTransaction/Index` after login: HTTP 200 and transaction page markers present.

Existing safe read-only POS performance audit:

- Ran `Areas\Pos\Tools\Invoke-PosPerformanceAudit.ps1`.
- Output captured under Areas\Pos\Tools\audit-output and copied into release TestLogs.

Existing POS save harness:

- `Areas\Pos\Tools\Invoke-PosSalesConcurrencyTest.ps1` was inspected but not executed because it writes invoice data and has a guard requiring explicit non-test confirmation for database name `Cash`.

## Error Found and Fix Applied

Error:

- `45_POS_ExcelImport.sql` attempted to create a foreign key using `BatchRowId` on `dbo.POS_ImportBatchRow`.
- The application repository and live schema use `RowId`, not `BatchRowId`.

Impact:

- The updater correctly stopped on the failed script, logged the failure, and did not silently continue.

Fix:

- Updated `45_POS_ExcelImport.sql` to match `Areas\Pos\Data\PosSqlRepository.cs`.
- Preserved SQL Server 2012-compatible DDL.
- Kept object creation idempotent.
- Aligned indexes with repository lookup patterns.

## Production Deployment Instructions

Use the prepared package:

- Areas\Pos\Releases\POS_ProductionRelease_20260509.

Deployment sequence:

1. Confirm customer Web.config has the production `KishnyCashConnection`.
2. Do not deploy local server names, local databases, or developer credentials.
3. Take a full SQL Server database backup and verify the restore path.
4. Run DryRun from the release package.
5. Review pending scripts, manual scripts, and any warnings.
6. Run Apply with `-StopOnError`.
7. Run DryRun again and confirm Pending=0 and HashMismatch=0.
8. Open POS login and transaction page.
9. Confirm `dbo.POS_SqlUpdateHistory` contains successful rows.
10. Keep all updater logs with the customer deployment record.

Example DryRun:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Invoke-PosSqlAutoUpdate.ps1 -WebConfigPath "C:\Path\To\Web.config" -ConnectionName KishnyCashConnection -Mode DryRun
```

Example Apply:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Invoke-PosSqlAutoUpdate.ps1 -WebConfigPath "C:\Path\To\Web.config" -ConnectionName KishnyCashConnection -Mode Apply -StopOnError -ReleaseNo POS_ProductionRelease_20260509
```

## Rollback / Backup Recommendation

- A full database backup is mandatory immediately before production Apply.
- Keep the generated updater log file with the deployment record.
- Do not run rollback or diagnostic scripts automatically.
- Manual rollback must be DBA-led and based on the customer backup plus the exact failed script/log entry.
- If a script fails, stop deployment, preserve the log, do not retry with edited SQL until the defect is reviewed.

## Remaining Risks

- Server-level permission script `35_POS_SystemHealthPermissions.sql` remains manual by design.
- The write-heavy POS sales concurrency harness was not run on `Cash` to avoid creating test invoices in a shared local database.
- Production execution still depends on correct customer connection string, SQL login permissions, and a verified backup.

## Final Verdict

READY.

The POS SQL auto-update package is suitable for customer production deployment after the normal production backup/configuration gate. The updater applies required POS SQL in order, records success/failure, prevents repeat execution, detects hash mismatch, skips non-POS databases safely, and has been validated on the available CASH and ENG local/test targets.
