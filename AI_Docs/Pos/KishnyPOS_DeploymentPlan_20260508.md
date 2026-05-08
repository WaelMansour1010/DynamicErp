# Kishny POS Deployment Plan - 2026-05-08

## Decision
Current worktree is **NO-GO for direct deployment**.

Prepare a clean package first. Do not copy `F:\Source Code\DynamicErp` over `C:\WWWSite\cayshny\`.

## Candidate Files to Deploy
Only after split/review:
- `bin/*` from a clean build that excludes MainErp/debug/local-only changes, or an approved POS-only build output.
- `Areas/Pos/Data/PosSqlRepository.cs` compiled changes limited to deadlock retry/save attempt logging.
- `Areas/Pos/Models/PosSystemErrorLogModels.cs` compiled changes if log UI is included.
- `Areas/Pos/Controllers/PosSystemErrorLogController.cs` compiled changes if log UI is included.
- `Areas/Pos/Views/PosSystemErrorLog/Index.cshtml` if log UI is included.
- Existing POS transaction files only if split to approved IPN/save hardening changes.
- SQL scripts listed in `AI_Docs/Pos/KishnyPOS_SQLApplyPlan_20260508.md`.
- Production `Web.config` based on the template, with customer-specific secrets inserted outside source control/docs.

## Files to Exclude
- `Areas/MainErp/**`
- `AI_Docs/MainErp/**`
- `AI_Docs/SharedMigration/**`
- `App_Data/PosExcelImports/**`
- `Excel/**`
- All `*_Backup_202605*` files.
- `MyERP_Backup_20260507_1903.csproj`
- POS Excel import feature files unless approved.
- POS payment/cashing feature files unless approved.
- MainErp SQL scripts.
- Current root `Web.config`.
- Server tuning config containing real credentials in `Areas/Pos/Sql/ServerTuningDeployment/Web.Byte.Production.Tuned.config`.

## Backup Steps
1. Stop or drain customer traffic.
2. Backup `C:\WWWSite\cayshny\` to a timestamped folder.
3. Backup current `C:\WWWSite\cayshny\Web.config`.
4. Backup customer database, or at least affected procedures/tables/index metadata.
5. Export current POS config and app pool settings.

## SQL Steps
1. Run scripts on a restored test DB first.
2. Apply approved scripts in order:
   - `31_POS_GetNextID_FromSequence_Concurrency.sql`
   - `47_POS_SaveAttemptLog.sql`
   - `46_POS_SaveTransaction_ConcurrencyIndexes.sql`
   - `30_POS_SaveTransaction_UnicodeText.sql`
3. Do not apply MainErp SQL.

## Web.config Steps
1. Start from customer current `Web.config`.
2. Apply only required connection/session/binding settings.
3. Set debug/dev flags off.
4. Disable or block `/DevStart`, `/RunMode`, and `/MainErp`.
5. Preserve `machineKey` unless intentionally rotating sessions.

## Smoke Test Steps
- `/Pos/Login`
- `/Pos`
- `/Pos/PosTransaction/Index`
- Load POS context/session restore.
- Safe test save using non-production/test data only.
- Confirm retry logging table exists.
- Confirm log screen loads if included.
- Confirm print route if safe.
- Confirm no MainErp/sidebar/debug menu exposure.
- Confirm `/DevStart`, `/RunMode`, `/MainErp` are blocked/disabled.

## Rollback Steps
1. Restore web folder backup.
2. Restore original `Web.config`.
3. Restore backed-up stored procedures if save behavior regresses.
4. Drop/disable added indexes only if they are linked to regression.
5. Leave `POS_SaveAttemptLog` unless it causes issues; otherwise export then drop.
6. Recycle app pool and rerun login/save smoke test.
