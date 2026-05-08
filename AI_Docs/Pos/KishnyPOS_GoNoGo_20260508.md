# Kishny POS Go / No-Go - 2026-05-08

## Result
Safe to deploy today from current worktree: **No**.

## Required Manual Steps
- Create a clean release package/branch that contains only approved Kishny POS save/deadlock hardening.
- Exclude MainErp migration, DevStart/RunMode/debug, local Excel uploads, backup files, and risky POS feature work.
- Apply production config from a safe template, not current root `Web.config`.
- Apply SQL only after customer DB backup and test DB validation.

## SQL Required
Yes, for the deadlock/save release:
- `31_POS_GetNextID_FromSequence_Concurrency.sql`
- `47_POS_SaveAttemptLog.sql`
- `46_POS_SaveTransaction_ConcurrencyIndexes.sql`
- `30_POS_SaveTransaction_UnicodeText.sql`
- Optional diagnostic: `39_POS_Deadlock_Diagnostics.sql`

## Config Risk
High.

Reasons:
- Current `Web.config` is local/debug.
- Dev master password is enabled.
- `MainErp_ConnectionString` points to Eng.
- `/RunMode` and DevStart routing is unconditional.
- MainErp area routing is unconditional.

## Deadlock Fix Included
Yes, in the worktree, but mixed with unrelated changes. It should be split before shipping.

## MainErp / Debug Isolated
No, not in the current build tree.

Findings:
- `Areas/MainErp` is included in the project and routes.
- POS payment/cashing additions reference MainErp models/repositories.
- `/RunMode`, root DevStart, and `/MainErp` routes are visible unless blocked or code is changed.

## Build Validation
`MyERP.sln` Release build completed successfully with MSBuild 17.14 / Visual Studio 2022.

Important: this validates compilation only. It built the mixed MainErp/POS tree and therefore does **not** make the current worktree customer-safe.

## POS Smoke Test Result
Read-only local IIS Express route check:
- `/Pos/Login`: 200 OK.
- `/Pos`: 200 after redirect to `/Pos/Login` because no POS session was present.
- `/Pos/PosTransaction/Index`: 200 after redirect to `/Pos/Login` because no POS session was present.
- `/MainErp`: 200 after redirect to `/MainErp/Login`; this confirms MainErp is not isolated/blocked in the current build.
- `/RunMode`: first no-redirect check returned 404 in this local IIS Express run, but route code still registers `RunMode` unconditionally and should be explicitly disabled/blocked for production.

No save POST, print, or SQL-changing smoke test was run. A safe test DB/login is required for that.

## Rollback Readiness
Not ready until backups are taken and old stored procedures are scripted out.

## Final Gate
**NO-GO** until:
1. Clean package is built.
2. Production config is verified.
3. MainErp/debug routes are disabled or excluded.
4. SQL apply plan is tested on non-production DB.
5. POS smoke test passes.
