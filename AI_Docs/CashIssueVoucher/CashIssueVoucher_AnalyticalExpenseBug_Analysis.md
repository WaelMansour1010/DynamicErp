# CashIssueVoucher Analytical Expense Bug Analysis

## Scope

- Project: DynamicErp Web main project.
- Screen: `CashIssueVoucher/AddEdit`.
- Voucher type: analytical issue voucher, `SourceTypeId == 10`.
- Affected grid: lower analytical expense details grid inside `_IssueAnalysis`.

## Files inspected

- `Views/CashIssueVoucher/AddEdit.cshtml`
- `Views/CashIssueVoucher/_IssueAnalysis.cshtml`
- `Controllers/AccountSettings/CashIssueVoucherController.cs`
- Related EF models: `CashIssueVoucher`, `IssueAnalysis`, `IssueAnalysisDetail`, `PropertyDetail`.

## Root cause

### Property units were not loaded for the first selected property

New analytical detail rows are created in `addIssueAnalysisDetailRow`. The property combo is then filled by `loadProperties`.

Before the fix, `loadProperties` appended property options only:

```js
$(propList).empty().append(items).select();
```

The browser selected the first property implicitly, but no `change` event was fired and no explicit unit reload was called. Because of that, if "عمارة الأربعين" was the first option, it looked selected but its units were still empty. Selecting another property and then returning to it fired the change handler, so the units appeared.

This made "عمارة الأربعين" look special because it was the first valid property in the list, not because its data was missing.

### Existing rows used a delayed reload

Existing analytical details used `initiatePropertyUnitsForDetailsTable`, which triggered property change and waited 5 seconds before restoring the selected unit. This was timing-sensitive and could fail depending on Ajax speed.

### NaN in total validation

The save path calculated analytical totals with direct calls like:

```js
parseFloat(td.find('.Total').inputmask('unmaskedvalue'))
parseFloat($tdDetail.find('.price-input').inputmask('unmaskedvalue'))
```

Some inputs are rendered as plain `number` fields first, and some may not have inputmask initialized yet. Empty values, undefined inputmask values, formatted values, or Arabic numeric text can therefore produce `NaN`.

Once `totalPrice` became `NaN`, the validation message became:

```text
The total price (NaN) does not match the sum of all prices (100) for reason (1223)
```

### Total price was not the right comparison field for non-VAT accounts

Some analytical accounts do not include VAT. For these rows, the `Total` input is disabled and the user-entered amount is stored in `Value`. The validation still compared details against `Total`, so a valid row with `Value = 1000` and details sum `1000` could fail as `Total price (0.00) does not match ... (1000.00)`.

The comparison must use `Total` when it is active, and fall back to `Value` when `Total` is disabled or empty.

### Detail price layout was unstable

The existing detail-row markup in `_IssueAnalysis.cshtml` had an unclosed price `<td>` in rendered rows. That can shift the delete cell into the price column and visually clip the amount. The analytical grid also had narrow numeric cells, which made prices appear partially hidden in RTL layout.

## Before fix

- First property selected implicitly after loading property options.
- Unit combo stayed empty until the user selected another property and returned.
- Direct `parseFloat` on empty or undefined values could produce `NaN`.
- Total mismatch message did not identify the row/column causing an invalid numeric value.
- Non-VAT rows could compare detail prices against disabled/empty `Total` instead of `Value`.
- Price cells could render too narrow or misaligned.

## After fix

- Property units load immediately after properties are appended, even for the first selected property.
- Existing row unit reload is deterministic and based on the Ajax completion callback, not a fixed timeout.
- Analytical numeric parsing uses one safe helper:
  - null/undefined/empty => 0
  - removes commas
  - supports Arabic/Persian digits
  - supports Arabic decimal separator
  - logs console warnings for invalid numeric values
- Save validation reports the analytical row and column when an invalid numeric value is found.
- Detail price sum validation now compares against `Value` when `Total` is disabled or empty.
- The analytical detail price cell markup is valid, and numeric cells have a safer width and LTR number alignment.
