# Kishny POS Feature Release GO / NO-GO - 20260508

Status: GO for controlled customer release preparation. Do not deploy without the manual backups and DB smoke noted below.

## GO Evidence

- Clean package created: `F:\Source Code\DynamicErp\Releases\KishnyPOS_FeatureRelease_20260508`.
- Release build succeeded from the clean worktree.
- Release gate output: GO.
- Production config template disables MainErp, DevStart, RunMode, Dev master password, and payment/cashing exposure.
- Package contains no `Areas/MainErp` content, no `AI_Docs`, no local Excel uploads, no backup files, no `MyERP.dll.config`, and no Firebase JSON credential file.
- SQL package contains only approved POS scripts plus optional diagnostics; no MainErp SQL and no payment/cashing SQL.
- Route smoke passed: POS routes redirect to login; `/MainErp`, `/DevStart`, `/RunMode`, `/Pos/Payments/Index`, and `/Pos/Cashing/Index` return 404.

## Required Manual Steps Before Customer Deploy

1. Back up `C:\WWWSite\cayshny\`.
2. Back up current production `Web.config` and preserve real customer secrets/machineKey.
3. Back up customer database.
4. Review and apply SQL in `Sql\SQL_APPLY_ORDER.md` against Kishny POS DB only.
5. Verify `App_Data\PosExcelImports` exists and IIS app pool identity can write there if Excel import is enabled.
6. Run authenticated smoke on a safe/test DB for save, purchases, transfers, reports, and Excel import commit before production traffic.

## Risk Notes

- Excel import commit and save/deadlock DB-write tests remain pending because no safe test DB session was available in this run.
- `PosSqlRepository` includes internal rollback/delete helpers used by Excel import audit rollback, but no admin delete controller/view was shipped in the package scope.
- MainErp code may still be compiled inside the application DLL due the legacy single-project structure, but MainErp content is absent and routes are disabled by config/area registration.

## Final Decision

GO for package handoff and controlled deployment window after manual backups and safe DB smoke. NO blind deploy.
