# Kishny POS Approved Feature Release v2 GO/NO-GO - 2026-05-08

Status: GO for controlled deployment preparation. Do not deploy automatically.

## Build
- Release build: PASSED.
- Output assembly: `Package\bin\MyERP.dll`.

## Bin packaging gate
- Full bin folder included: NO.
- Included bin files: `MyERP.dll` only.
- DevExpress DLLs included: NO.
- License/runtime/dependency DLLs included: NO.
- Extra DLLs included: NO.

## Scope gate
Included approved Kishny POS scope:
- Deadlock/save hardening and friendly busy retry UX.
- Sales invoice/search changes.
- Required field highlight/focus and machine withdraw realtime amount calculation fix in `pos-transaction.js` payload.
- Journal Entries.
- استعاضة العهدة through POS Payments view only.
- Accounting, Kishny, smart, and operational reports.
- System monitoring/error/save-attempt screens.

Excluded:
- Areas/MainErp and MainErp docs.
- DevStart, RunMode, DevMasterPassword exposure.
- Excel import views/scripts/uploads/SQL.
- Cashing views/SQL.
- Purchase/stock transfer views/scripts from older scope.
- Backup files and local spreadsheets.

## Config gate
- `debug=false` in production template.
- `EnableKishnyPos=true`.
- `EnableMainErpMigration=false`.
- `EnableDevStart=false`.
- `EnableRunModeSelector=false`.
- `EnableDevMasterPassword=false`.
- `EnablePosExcelImport=false`.
- `EnablePosPaymentsCashing=false`.
- Customer `machineKey`, binding redirects, and existing dependency setup must be preserved manually.

## SQL gate
- POS-only SQL package created with apply order and rollback notes.
- MainErp SQL excluded.
- Excel import SQL excluded.
- Payment/Cashing read-procedure SQL excluded.
- `28_POS_Payments_Audit.sql` included only for approved استعاضة العهدة/custody replenishment.

## Validation summary
- Package bin contains only `MyERP.dll`.
- No DevExpress DLLs found.
- No MainErp, ExcelImport, Cashing, local Excel upload, backup, pdb, or map payload found.
- Package manifest created: `PACKAGE_MANIFEST.txt`.

## Manual deployment gates
1. Backup `C:\WWWSite\cayshny\`.
2. Backup current customer `Web.config`.
3. Backup customer database.
4. Backup current customer `bin\MyERP.dll`.
5. Merge config settings manually; do not overwrite Web.config blindly.
6. Apply SQL only if not already applied and only after DB backup.
7. Recycle app pool and run smoke tests.

Decision: GO for safe v2 package deployment preparation, with manual backups and smoke tests required before production cutover.
