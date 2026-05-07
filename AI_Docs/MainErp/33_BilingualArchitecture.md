# MainErp Bilingual Architecture

Date: 2026-05-07

## Goal

MainErp must support Arabic and English on the same screens, controllers, routes, workflows, reports, and accounting logic. No duplicated Arabic/English views or workflows should be created.

## Resource Strategy

Resources live under:

- `Areas\MainErp\Resources\MainErp.resx`
- `Areas\MainErp\Resources\MainErp.en.resx`
- `Areas\MainErp\Resources\MainErp.ar.resx`

Resource keys are used for:

- menus,
- buttons,
- tabs,
- page titles,
- report titles,
- validation messages,
- dashboard labels,
- grid column labels.

Neutral resource is English fallback. Arabic overrides live in `MainErp.ar.resx`.

## Culture Management

Implemented:

- `MainErpCultureManager`
- `MainErpLocalizationService`
- `MainErpEntityLocalization`
- `LocalizationController`

Language support:

- `ar`
- `en`

Preference storage:

- Session key: `MainErp.Culture`
- Cookie: `MainErp.Culture`

Switch route:

- `/MainErp/Localization/Set?culture=ar|en&returnUrl=...`

The same routes are used for both languages. There is no `/ar/MainErp` or `/en/MainErp`.

## RTL/LTR

The MainErp layout now sets:

- `<html lang="...">`
- `<html dir="rtl|ltr">`
- body class:
  - `main-erp-rtl`
  - `main-erp-ltr`

CSS adds LTR handling for:

- sidebar,
- navigation,
- form labels,
- tables,
- report filters.

## Database Entity Localization

The database already has bilingual columns in many entities. MainErp uses a central fallback rule:

`Localize(ar, en)`

Behavior:

- Arabic mode: Arabic value first, English fallback.
- English mode: English value first, Arabic fallback.

Examples:

- `Account_Name` / `Account_NameEng`
- `CusName` / `CusNamee`
- `Project_name` / `Project_nameE`
- `Item_Name` / `Item_NameE`
- `BankName` / `BankNameE`

## Account Display Standard

Important accounting rule:

- `ACCOUNTS.Account_Code` is an internal posting/join key only.
- Do not show raw `Account_Code` as the primary user-facing value.

Visible account identity:

`Account_Serial + " - " + Localized Account Name`

Implemented for:

- LC account,
- margin account,
- acceptance account,
- expense account,
- project expense account,
- parent accounts.
- LC voucher trace rows,
- journal entry list rows,
- journal entry detail rows,
- journal entries report rows,
- account movement report account header.

If the account is missing:

- Arabic: `الحساب غير موجود`
- English: `Account not found`

## Initial Screens Converted

Applied immediately to:

- MainErp layout/topbar,
- MainErp sidebar,
- MainErp dashboard,
- LC screen key labels/buttons/sections,
- Accounting Reports index,
- Sales Reports index,
- Journal Entries list/details,
- Journal Entries report,
- Account Movement report,
- Sales Summary report.

LC account display now localizes account names using:

- `Account_Name`,
- `Account_NameEng`,
- `Account_Serial`.

## Report Localization Strategy

Reports should use the same resource keys for:

- report titles,
- column names,
- filter names,
- totals,
- warnings.

Entity values should use bilingual database fields. Report definitions should not be duplicated unless required by legal/business format.

## Validation Localization

Validation messages should come from resources, for example:

- `Required field`
- `الحقل مطلوب`
- `Account not found`
- `الحساب غير موجود`

## Future Migration Rule

Every migrated MainErp module should:

- use `MainErpLocalizationService.T(key)` for UI text,
- use `MainErpEntityLocalization.Localize(ar, en)` for database entity names,
- use `MainErpEntityLocalization.AccountDisplay(...)` for accounts,
- keep one controller/view/workflow per screen,
- avoid raw VB6 control names and internal database keys in user-facing UI.
