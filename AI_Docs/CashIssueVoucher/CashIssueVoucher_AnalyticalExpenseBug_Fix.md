# CashIssueVoucher Analytical Expense Bug Fix

## Modified files

- `Views/CashIssueVoucher/AddEdit.cshtml`
- `AI_Docs/CashIssueVoucher/CashIssueVoucher_AnalyticalExpenseBug_Analysis.md`
- `AI_Docs/CashIssueVoucher/CashIssueVoucher_AnalyticalExpenseBug_Fix.md`

No SQL changes were required.

## Fix details

### Property/unit cascade

- Added `loadPropertyUnits(propertyList, selectedUnitValue)`.
- `loadProperties` now loads units immediately after property options are appended.
- `onChangePropertyList` delegates to the same unit loader.
- `initiatePropertyUnitsForDetailsTable` now restores existing selected units after the unit Ajax request completes.
- `sourceTypeChange` now calls `initiatePropertyUnitsForDetailsTable()` when `SourceTypeId == 10`.

This makes the first selected property valid and active, including "عمارة الأربعين", without requiring the user to pick another property first.

### Safe numeric parsing

Added shared helpers in `AddEdit.cshtml`:

- `normalizeNumberText`
- `getElementNumberInfo`
- `safeElementNumber`
- `numbersEqual`

The helpers protect analytical calculations from:

- empty values
- `null` / `undefined`
- comma-grouped numbers
- Arabic/Persian digits
- Arabic decimal separators
- missing inputmask initialization

### Validation improvement

The analytical save validation now:

- parses total and detail prices safely
- prevents `NaN` from entering totals
- compares detail prices against `Value` when `Total price` is disabled or empty for non-VAT accounts
- reports invalid numeric values with analysis row and column
- compares totals with a small decimal tolerance instead of strict floating-point equality

### Layout fix

- Closed the missing price `<td>` in `_IssueAnalysis.cshtml`.
- Added scoped widths for analytical grid columns.
- Forced analytical numeric inputs to LTR direction with right alignment so values like `1000.00` remain visible in RTL layout.

## Test scenario

1. Open `CashIssueVoucher/AddEdit`.
2. Select voucher source type `صرف تحليلي`.
3. Add/select the first analytical row.
4. Enter reason `1223`.
5. Select account `110501 - إيجارات مدفوعة مقدما`.
6. Add a detail row.
7. Select property `عمارة الأربعين` as the first property option.
8. Confirm the unit combo loads immediately.
9. Select a unit.
10. Enter price/value `100`.
11. Save.

Expected result:

- Units appear immediately after selecting the first property.
- No `NaN` appears in total validation.
- No mismatch appears when analytical row total equals detail price sum.
- For accounts without VAT, detail sum is compared with `Value`, not the disabled empty `Total price`.
- Price and value inputs are visible without clipping.
- Voucher saves normally, assuming the remaining required header fields are valid.

## Future notes

- Keep analytical numeric calculations on the shared safe helpers.
- Avoid relying on implicit browser selection to trigger cascaded combo reloads.
- Avoid fixed `setTimeout` waits for Ajax-backed select restoration.
