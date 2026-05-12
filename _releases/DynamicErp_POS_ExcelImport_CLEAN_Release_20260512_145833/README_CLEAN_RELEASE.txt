DynamicErp POS Excel Import CLEAN Release
Generated: 2026-05-12 14:58:34
Built from clean worktree: F:\Source Code\DynamicErp_POS_CleanBuild_20260512

IMPORTANT DEPLOYMENT RULE:
- Copy the contents of DEPLOY only to the client web application root.
- SOURCE_PATCH is for developer review only. Do not deploy SOURCE_PATCH.

Why this package is different from the broken one:
- MyERP.dll was rebuilt from a clean git worktree.
- Only POS Excel Import/POS sales files were applied.
- MyERP.csproj includes only one POS addition: Areas\Pos\Services\PosKycExcelParser.cs.
- No MainErp customer migration files are included in the build.

Scope:
- Excel branch detection from workbook/file branch hints, including default Sheet 1 handling from existing parser flow.
- KYC Excel section inside Excel Import screen with preview and save/activation step.
- Duplicate IPN in Excel import saves with warning instead of rejection.
- Sales screen shows and filters Excel invoices with warnings.
- Server-side overlap guard rejects any Excel import for the same branch when the new sheet date range overlaps a previously imported Excel batch.

Deploy contents:
- bin\MyERP.dll
- Areas\Pos\Views\ExcelImport\Index.cshtml
- Areas\Pos\Views\ExcelImport\Preview.cshtml
- Areas\Pos\Views\PosTransaction\Index.cshtml
- Areas\Pos\Scripts\pos-transaction.js
- Areas\Pos\Content\pos-transaction.css
- Areas\Pos\AI_Docs\*.md
- Areas\Pos\Sql\45_POS_ExcelImport.sql
- Areas\Pos\Sql\50_POS_ExcelImportCommitAudit.sql

Database:
- No new SQL script was introduced by this clean release.
- Existing POS audit scripts 45 and 50 are included for fresh/repair deployments only.
- Do not run any non-POS SQL for this release.

Validation:
- node --check Areas\Pos\Scripts\pos-transaction.js succeeded.
- MSBuild Debug Any CPU succeeded from the clean worktree.
- MyERP.dll SHA256: BADB0C1A688FD818D0E68DED6A459E5A993A1F2818E37ACD24950FCB9AFE75EE

Smoke test after deploy:
1. Restart IIS application pool.
2. Open /Pos/PosTransaction/Index.
3. Open /Pos/ExcelImport/Index.
4. Preview an Excel sheet.
5. Confirm overlapping same-branch Excel period is rejected before save.
6. Confirm duplicate IPN appears as warning and can be filtered in sales screen.
