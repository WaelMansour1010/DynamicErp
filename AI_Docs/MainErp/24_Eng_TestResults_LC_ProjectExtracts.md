# Eng Test Results: LC And Project Extracts

## Connection Used

MainErp now looks for `MainErp_ConnectionString` first and falls back to `MyERP_ConnectionString` if it is absent.

Local test connection:

- Server: `Wael\Sql2019`
- Database: `Eng`
- Auth: Integrated Security
- Connection name: `MainErp_ConnectionString`

This connection is used only by MainErp infrastructure.

## Route Checks

IIS Express route checks were performed without an authenticated browser session. The application returned expected authorization redirects, confirming route resolution without exposing anonymous content:

| Route | Result |
| --- | --- |
| `/MainErp/LC` | `302` to `/Login?ReturnUrl=/MainErp/LC` |
| `/MainErp/LC/Details/197` | `302` to `/Login?ReturnUrl=/MainErp/LC/Details/197` |
| `/MainErp/ProjectExtracts` | `302` to `/Login?ReturnUrl=/MainErp/ProjectExtracts` |
| `/MainErp/ProjectExtracts/Details/3449` | `302` to `/Login?ReturnUrl=/MainErp/ProjectExtracts/Details/3449` |
| `/MainErp/AccountingReports/JournalEntries` | `302` to login |
| `/MainErp/AccountingReports/AccountMovement` | `302` to login |
| `/MainErp/SalesReports/SalesSummary` | `302` to login |

## LC Data Test

Tables checked:

- `TblLC`
- `ACCOUNTS`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS1`
- `LCTypes`
- `BanksData`
- `TblBoxesData`
- `currency`
- `TblCountriesData`
- `TblCustemers`
- `TblBranchesData`

Sample ID:

- `TblLCID = 197`

Results:

- `TblLC` row count: `191`
- List query returned latest row `TblLCID = 197`
- Details query returned:
  - `LCNO = IMCC045825`
  - `Value = 500000.0000`
  - `BankName = Current A/C.# 0108095016690018 - ANB BANK`
  - `CurrencyName = ريال سعودي`
  - `VendorName = مصدر لمواد البناء`

Schema warnings:

- None from the tested LC list/details SQL on `Eng`.

## Project Extracts Data Test

Tables checked:

- `project_billl`
- `project_bill_details`
- `projects`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- `TblCustemers`
- `TblBranchesData`
- `transactionsVatDetails`
- `TblPayPrePayed`
- `TblProjePayPrePayed`

Sample ID:

- `project_billl.id = 3449`

Results:

- `project_billl` row count: `3373`
- List query returned latest row `id = 3449`
- Details query returned:
  - `bill_date = 2026-03-10`
  - `NoteSerial = 202603269`
  - `ManualNO = 0005-11`
  - `project_name = College Of Engineering .King Faisal University`
  - `total = 46600.0000`
  - `FATValue = 6990`
  - `NetValue = 46600`
  - `Branch_NO = 1`
  - `note_id = 218296`

Schema warnings:

- None from the tested Project Extract list/details SQL on `Eng`.

## Read-Only Reports Data Smoke

Test period:

- From `2026-01-01`
- To `2026-04-30`

Results:

- Journal entries report rows: `25020`
- Journal entries total debit: `317129131.3350`
- Journal entries total credit: `317129131.9799`
- Account movement sample account: `a2a1a12a1126`
- Account movement sample rows: `5`
- Account movement sample debit: `15020.0000`
- Account movement sample credit: `8925.0000`
- Sales summary invoice count: `604`
- Sales summary before VAT: `5596472.7100`
- Sales summary VAT: `1104967.2220`
- Sales summary total: `6435943.6320`

## Fixes Applied

- MainErp connection factory now supports `MainErp_ConnectionString` with fallback to `MyERP_ConnectionString`.
- Journal-entry direction logic was corrected to match the legacy values found in `Eng`: `0 = debit`, `1 = credit`.

## Remaining Gaps

- Authenticated browser testing should be repeated with a real MainErp-authorized user session.
- Account movement opening balance still needs legacy rule validation.
- Sales summary uses a conservative transaction-type filter and needs final mapping approval.
