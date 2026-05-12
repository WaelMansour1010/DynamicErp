# POS Excel Import - IPN Warning Handling

Date: 2026-05-12

## Scope

This change applies only to POS invoices created from Excel import. Manual POS invoices keep the existing duplicate IPN blocking behavior.

## Behavior

- Duplicate IPN in Excel import no longer blocks invoice creation.
- For cash-in and Kishny card rows, duplicate IPN is converted to a row warning during preflight.
- The imported invoice is saved through the existing POS save flow.
- The warning is stored in the existing `POS_ImportBatchRow.Message` value with the marker `[ExcelImportWarning]`.
- No new table or column was added.

## UI

- Excel preview shows the duplicate IPN as a warning instead of a rejection.
- Sales invoice lists show an `IPN مكرر` badge for Excel invoices imported with warnings.
- Opening the invoice shows the Excel warning message in the review area.
- Sales filters include `فواتير Excel بملاحظات`.

## Files Changed

- `Areas/Pos/Services/PosExcelImportPreflightService.cs`
- `Areas/Pos/Services/PosExcelImportCommitService.cs`
- `Areas/Pos/Data/PosSqlRepository.cs`
- `Areas/Pos/Controllers/PosTransactionController.cs`
- `Areas/Pos/Models/PosSaveTransactionRequest.cs`
- `Areas/Pos/Views/PosTransaction/Index.cshtml`
- `Areas/Pos/Scripts/pos-transaction.js`
- `Areas/Pos/Content/pos-transaction.css`

## Test Cases

1. Import Excel with duplicate IPN in cash-in/card rows: row is warning, not rejected.
2. Commit the file: invoice is created and import row message contains `[ExcelImportWarning]`.
3. Open created invoice: warning message is visible.
4. Sales filter `فواتير Excel بملاحظات` returns only Excel invoices with warning marker.
5. Manual duplicate IPN save remains blocked by existing validation.

## SQL

No SQL script was required. Existing audit table `POS_ImportBatchRow` is reused.

## Period Reimport Guard

The Excel commit guard checks the detected date range of the uploaded sheet against previously imported Excel batches for the same branch. The comparison is range-based:

- NewFrom <= ExistingTo
- NewTo >= ExistingFrom

So a previously imported branch range `2026-05-01` to `2026-05-10` blocks any later Excel import for that branch that touches any date inside that range, even if the new file name or source rows are different. This check runs server-side immediately before saving and under a branch-level application lock.
