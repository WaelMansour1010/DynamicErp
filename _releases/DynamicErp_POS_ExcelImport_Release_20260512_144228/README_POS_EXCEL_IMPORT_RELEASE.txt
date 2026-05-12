DynamicErp POS Excel Import Release
Generated: 2026-05-12 14:42:28

Scope:
- POS Excel Import branch detection from workbook/file hints.
- KYC Excel preview and second-step save/activation from Excel Import screen.
- Excel invoice commit through existing POS save flow only.
- Duplicate IPN in Excel imports saves with visible warning, not rejection.
- Sales screen filter for Excel invoices with warnings.
- Server-side guard rejects reimporting overlapping Excel date ranges for the same branch.

Deploy contents:
- bin\MyERP.dll
- Areas\Pos\Views\ExcelImport\Index.cshtml
- Areas\Pos\Views\ExcelImport\Preview.cshtml
- Areas\Pos\Views\PosTransaction\Index.cshtml
- Areas\Pos\Scripts\pos-transaction.js
- Areas\Pos\Content\pos-transaction.css
- Areas\Pos\AI_Docs\*.md relevant to this release
- Areas\Pos\Sql\45_POS_ExcelImport.sql and 50_POS_ExcelImportCommitAudit.sql for audit table compatibility/reference

SourcePatch:
- Contains the source files changed for review/merge traceability.

Database:
- No new SQL script was required for the latest changes.
- Existing POS audit tables are reused.
- POS SQL remains under Areas\Pos\Sql only.

Validation:
- MSBuild Debug Any CPU succeeded.
- node --check Areas\Pos\Scripts\pos-transaction.js succeeded.
- AllScripts.sql was not changed.

Deployment notes:
1. Backup current site files and database.
2. Copy deploy contents over the web application root.
3. Ensure POS SQL audit scripts 45 and 50 already exist/applied if this is a fresh environment.
4. Restart the application pool.
5. Test Excel import preview, commit, KYC commit, duplicate IPN warning, and overlapping period rejection.
