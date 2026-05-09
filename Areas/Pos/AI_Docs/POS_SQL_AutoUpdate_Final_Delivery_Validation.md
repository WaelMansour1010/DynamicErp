# POS SQL Auto-Update Final Delivery Validation

Date: 2026-05-10  
Project: DynamicErp  
Module: Kishny POS / Areas\Pos  
Validation phase: Final hardening before customer delivery  
Decision: GO

## Executive Summary

The POS SQL auto-update package passed final delivery hardening. The final deliverable package is standalone, runs from its own folder, applies the approved POS SQL scripts in manifest order, records history, prevents duplicate successful execution, stops safely on fatal SQL errors, rolls back the failing script transaction, recovers on rerun, and rejects changed scripts by hash mismatch.

Final package:

- Areas\Pos\Releases\POS_ProductionRelease_20260509
- Areas\Pos\Releases\POS_ProductionRelease_20260509.zip
- ZIP SHA256: `73E65EB7BBCACE71BAFDAECFFE02F48CBA1618228D459E9BDC1FF87560F56CF0`

## Worktree / Scope

Initial hardening status showed unrelated Reports-area changes already present in the worktree. This validation stayed scoped to POS release artifacts, POS SQL package contents, and POS hardening evidence.

## Fresh Restored Copy Test

No pre-update SQL backup was available in `msdb`, and the only other POS-looking database was not a valid POS database copy. To satisfy the requirement to avoid retesting the already-updated database directly, a disposable restored database copy was created from a copy-only backup and the updater history tables were removed only in that disposable copy.

Final exact-package validation target:

- `POSHardeningFinal_20260510`

Validation results from the final package folder:

- DryRun: 26 pending, 0 skipped, 0 hash mismatch.
- Apply: 26 scripts applied successfully.
- Rerun Apply: 0 applied, 26 skipped, 0 failed.
- History rows: 26 successful, 0 failed.
- Run rows:
  - Run 1: Completed, TotalScripts=26, AppliedCount=26, SkippedCount=0, FailedCount=0.
  - Run 2: Completed, TotalScripts=0, AppliedCount=0, SkippedCount=26, FailedCount=0.

Evidence:

- HardeningEvidence\final_package_dryrun.log
- HardeningEvidence\final_package_apply.log
- HardeningEvidence\final_package_rerun.log
- HardeningEvidence\final_package_db_state.txt

## Interrupted Execution / Crash Simulation

A separate disposable restored database was used:

- `POSHardeningCrash_20260509`

A hardening-only temporary package inserted an intentional failure script after script 30. The script created `dbo.POS_HardeningRollbackProbe` and then raised a SQL error.

Observed result:

- Updater stopped at the failing script.
- Run status became `Failed`.
- Six previous scripts were recorded as successful.
- One failed history row was recorded for the intentional failure.
- `dbo.POS_HardeningRollbackProbe` did not exist afterward, proving the failing script transaction rolled back.

Recovery result:

- Running the clean original package on the same database skipped the six successful scripts and applied the remaining twenty.
- Final history: 26 successful rows, 1 failed historical hardening row.
- Final rollback probe object remained absent.

Evidence:

- HardeningEvidence\crash_sim_apply.log
- HardeningEvidence\crash_sim_db_state.txt
- HardeningEvidence\crash_recovery_apply.log
- HardeningEvidence\crash_recovery_db_state.txt

## Hash Mismatch Protection

A temporary package copy intentionally modified an already-applied script:

- `25_POS_TemporaryPermissions.sql`

DryRun against an already-updated disposable database produced:

- Pending=0
- Skipped=25
- HashMismatch=1
- The updater threw: `Hash mismatch detected. Refusing to apply changed scripts.`

No Apply was allowed.

Evidence:

- HardeningEvidence\hash_mismatch_dryrun.log
- HardeningEvidence\hash_mismatch_dryrun.console.txt

## Manual Script Exclusion

The production package contains six manual scripts, all prefixed with `MANUAL_`:

- MANUAL_35_POS_SystemHealthPermissions.sql
- MANUAL_36_POS_PerformanceIndexRollback.sql
- MANUAL_39_POS_Deadlock_Diagnostics.sql
- MANUAL_52_POS_EmployeePayroll_MedicalInsurance.sql
- MANUAL_53_POS_DoubleEntryVoucherSequence_Diagnostics.sql
- MANUAL_56_POS_RechargeValue_Validation.sql

Manifest verification:

- Auto-apply scripts: 26.
- Manual scripts documented: 6.
- Manual scripts included in auto-apply list: 0.
- Missing manual files from package: 0.

## Standalone Package Verification

The updater was executed from:

- Areas\Pos\Releases\POS_ProductionRelease_20260509

It used only:

- `.\Tools\Invoke-PosSqlAutoUpdate.ps1`
- `.\Sql\POS_SQL_AutoUpdate_Manifest.json`
- `.\Sql\*.sql`
- an explicit customer-style connection string parameter

The package was reduced to customer-deliverable contents only:

- Docs
- Sql
- Tools

Test logs and local database state snapshots were removed from the customer package and kept only under AI_Docs hardening evidence.

## Local Reference / Credential Scan

The production package was scanned for these forbidden local references:

- developer server name
- SQL instance name used by local testing
- local SQL password
- local absolute source path
- local Web.config connection names
- local test database names in deployment/configuration context
- hardening database names

Result: no matches in the production package.

Note: SQL domain terms such as cash-in, cash-out, cashing, and cashbox remain in stored procedure code because they are business concepts, not local database identifiers.

## Final Package Contents

The final package contains:

- `Docs\DEPLOYMENT_CHECKLIST.md`
- `Tools\Invoke-PosSqlAutoUpdate.ps1`
- 26 approved auto-apply SQL scripts
- 6 documented `MANUAL_` scripts
- `Sql\POS_SQL_AutoUpdate_Manifest.json`

## Exact Customer Deployment Order

1. Schedule POS maintenance window and stop POS users.
2. Copy `POS_ProductionRelease_20260509.zip` to the customer server.
3. Extract it to a deployment folder.
4. Confirm the customer POS connection string/server/database values with the DBA.
5. Take a full SQL Server backup of the production POS database.
6. Verify the backup restore path before applying updates.
7. Open PowerShell from the extracted package root.
8. Run DryRun:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Invoke-PosSqlAutoUpdate.ps1 -ConnectionString "Data Source=<SERVER>;Initial Catalog=<DATABASE>;Integrated Security=True;MultipleActiveResultSets=True" -Mode DryRun
```

9. Continue only if DryRun has no hash mismatch and the pending list matches the expected release.
10. Run Apply:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Invoke-PosSqlAutoUpdate.ps1 -ConnectionString "Data Source=<SERVER>;Initial Catalog=<DATABASE>;Integrated Security=True;MultipleActiveResultSets=True" -Mode Apply -StopOnError -ReleaseNo POS_ProductionRelease_20260509
```

11. Run DryRun again and confirm:

- `Pending=0`
- `HashMismatch=0`
- all approved scripts are skipped as already applied

12. Start the web application.
13. Verify POS login/default loading.
14. Verify transaction page opens.
15. Verify print/report, payment/cashing, KYC/card, permissions, and dashboard pages as applicable.
16. Save updater logs and DBA backup reference with the deployment record.

## Stop / Rollback Conditions

Stop deployment immediately if:

- DryRun reports hash mismatch.
- Apply reports any failed script.
- The updater says the database does not match POS probe objects.
- A required manual script has not been reviewed by a DBA.
- POS smoke tests fail after Apply.

Rollback must be DBA-led from the verified pre-deployment backup.

## Remaining Risks

- The customer DBA must provide and verify the production backup.
- Server-level permission script remains manual by design.
- The package updates SQL only; web application binaries/configuration must still be deployed according to the normal DynamicErp deployment process.
- The local hardening test used restored copies from the current POS database because no older pre-update backup was available.

## Final Decision

GO.

The POS SQL auto-update package is ready for customer delivery, provided the customer deployment follows the backup, DryRun, Apply, rerun-DryRun, and smoke-test order documented above.
