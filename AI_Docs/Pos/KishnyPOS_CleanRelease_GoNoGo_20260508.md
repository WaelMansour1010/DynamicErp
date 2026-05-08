# Kishny POS Clean Release GO / NO-GO - 2026-05-08

## Result
Status: **GO for clean package / NO automatic deployment**.

This release package is safe to use as the customer update candidate for Kishny POS deadlock/save hardening only. It still requires the normal customer deployment backups, SQL apply, app pool recycle, and final production smoke test.

## Baseline
- Baseline: git `HEAD` commit `80b490c`.
- Release branch/worktree: `release/kishny-pos-deadlock-20260508` at `F:\Source Code\DynamicErp_release_kishny_pos_deadlock_20260508`.
- Customer deployed folder baseline was not available locally because `C:\WWWSite\cayshny\` does not exist on this machine.

## Package
- Final package: `F:\Source Code\DynamicErp\Releases\KishnyPOS_DeadlockFix_20260508`
- Package gate: **GO**
- Build stamp: `BUILD_RELEASE_OK.txt`

## Build
- `MyERP.sln` Release build: **passed**
- Existing compiler warnings remain, but no build errors.
- Dependency note: the release worktree uses a local junction to the existing `packages` folder for old NuGet package restore layout.

## SQL Package
Included only approved POS SQL:
- `00_BACKUP_BEFORE_APPLY.sql`
- `31_POS_GetNextID_FromSequence_Concurrency.sql`
- `47_POS_SaveAttemptLog.sql`
- `46_POS_SaveTransaction_ConcurrencyIndexes.sql`
- `30_POS_SaveTransaction_UnicodeText.sql`
- `39_POS_Deadlock_Diagnostics.sql` optional/manual
- `SQL_APPLY_ORDER.md`
- `SQL_ROLLBACK.md`

No MainErp SQL is included.

## Config
- Production-ready template created at `Config\Web.config.production-ready`.
- Real secrets are not stored.
- Debug/dev flags are off in the template:
  - `EnableMainErpMigration=false`
  - `EnableDevStart=false`
  - `EnableRunModeSelector=false`
  - `EnableDevMasterPassword=false`
  - `DebugKYC=false`
  - `debug=false`

## Route Isolation
Code now uses config-controlled route exposure:
- `/Pos` allowed when `EnableKishnyPos` is not explicitly false.
- `/MainErp` blocked unless `EnableMainErpMigration=true`.
- `/DevStart` blocked unless `EnableDevStart=true`.
- `/RunMode` blocked unless `EnableRunModeSelector=true` or `EnableDevStart=true`.

Read-only local smoke test:
- `/Pos/Login`: 200
- `/Pos`: 200 redirected to `/Pos/Login` without session
- `/Pos/PosTransaction/Index`: 200 redirected to `/Pos/Login` without session
- `/MainErp`: 404
- `/DevStart`: 404
- `/RunMode`: 404

## Package Exclusions Verified
- No `Areas/MainErp` content folder in package.
- No `AI_Docs` in package.
- No `Excel` folder or `.xlsx` test files in package.
- No `App_Data/PosExcelImports` in package.
- No POS ExcelImport/Payments/Cashing view folders in package.
- No backup files are intentionally included. `00_BACKUP_BEFORE_APPLY.sql` is the approved SQL backup helper.

## Not Performed
- No SQL was applied automatically.
- No production deployment was performed.
- No save transaction POST was run because no safe test DB/customer staging login was provided in this environment.

## Final Gate
Clean package: **GO**.

Customer deployment: **GO only after**:
1. Customer web folder backup.
2. Customer `Web.config` backup.
3. Customer database backup.
4. SQL apply in documented order.
5. Production/staging smoke test including a safe save transaction if approved.
6. Confirm `/MainErp`, `/DevStart`, and `/RunMode` remain blocked after deployment.
