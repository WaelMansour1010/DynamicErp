# POS Excel Import - Branch Hint And Sheet 1 Fix

Date: 2026-05-12

## Scope

Limited hardening for the existing Kishny POS Excel import parser/preflight flow.

## Files Reviewed

- `Areas/Pos/Services/PosExcelImportParser.cs`
- `Areas/Pos/Services/PosExcelImportPreflightService.cs`
- `Areas/Pos/Models/PosExcelImportModels.cs`

## Files Changed

- `Areas/Pos/Services/PosExcelImportParser.cs`
- `Areas/Pos/Services/PosExcelImportPreflightService.cs`
- `Areas/Pos/Models/PosExcelImportModels.cs`

## Behavior Added

1. The parser now scans the first header area of every workbook sheet for branch hints written inside the Excel file.
   - Supports branch code cells like `ec083`.
   - Supports Arabic branch name cells like `مرور منوف`.
   - Supports combined row hints like `ec083 مرور منوف`.

2. Branch preflight now prefers workbook branch hints over the external file name.
   - Explicit workbook branch codes like `EC003` have first priority over adjacent Arabic name text.
   - This prevents false ambiguous matches when a full branch label contains shared words such as court/governorate names.
   - If the workbook hint resolves to one branch, that branch is used even if the file name differs.
   - If workbook hints resolve to multiple branches, import is rejected with candidates listed.
   - If workbook hints exist but cannot be resolved, import is rejected instead of silently falling back to the filename.
   - If no workbook branch hint exists, the previous filename-based detection remains the fallback.

3. Workbooks with a single/default worksheet name are no longer skipped just because the sheet is named `Sheet 1`.
   - `Sheet 1`, `Sheet1`, `Sheet01`, `ورقة1`, and `ورقه1` are accepted for inspection.
   - Any sheet that already has the expected operational headers is accepted regardless of its name.
   - Non-operational sheets still fail the existing header validation and are not imported.

## Validation Notes

- No save/accounting/serial/Voucher_coding logic was changed.
- No SQL script was required.
- POS SQL location rule remains unchanged.
- `AllScripts.sql` was not modified.

## Test Cases

1. Excel file contains `ec083` and `مرور منوف` inside the sheet while filename differs:
   - Expected: branch resolves from the workbook cells.

2. Excel file contains only filename branch hint and no workbook branch cells:
   - Expected: previous filename-based detection is used.

2a. Excel file contains `EC003` in the top branch-code cell and a broader Arabic branch name in the adjacent cell:
   - Expected: `EC003` resolves as the branch deterministically and the Arabic text does not create a false multi-branch rejection.

3. Workbook has one sheet named `Sheet 1` with the normal operational headers:
   - Expected: rows are parsed normally.

4. Workbook branch hints match multiple branches:
   - Expected: preflight rejects the import and lists candidate branches.

5. Workbook branch hint does not match any branch:
   - Expected: preflight rejects the import and does not save invoices.
