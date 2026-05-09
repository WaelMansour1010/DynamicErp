# MainErp UAT Delivery Test - LC and Project Extracts

Date: 2026-05-09  
Environment: local IIS Express / `http://localhost:63735`  
Database: `MainErp_ConnectionString -> Wael\Sql2019 / Eng`  
Login used: `admin` with configured development master password.

## Scope

This pass tested the two critical MainErp migration screens as delivery/UAT candidates:

- الاعتمادات المستندية / Letters of Credit.
- مستخلصات المشاريع / Project Extracts.

The test covered route access, search, real data loading, create/save/edit where implemented, account creation, voucher creation, opening balance posting, journal links, reports, UI weight/usability, audit evidence, and build validation.

## LC Test Summary

Test routes:

- `/MainErp/LC`
- `/MainErp/LC?searchText=UAT-LC-20260509164035`
- `/MainErp/LC?selectedId=199`
- `/MainErp/LC/New`
- `/MainErp/LC/Edit/199`
- `/MainErp/LC/Report/199`
- `/MainErp/JournalEntries/DetailsByNote/222101`
- `/MainErp/JournalEntries/DetailsByNote/222102`

All tested routes returned HTTP 200 after login and did not show server errors.

## LC Create Test

Created a new LC from the web screen:

- `TblLCID = 199`
- `LCNO = UAT-LC-20260509164035`
- Value: `12345.67`, later edited to `12500.00`
- OpenValue: `321.45`, later edited to `333.33`
- OpenBalance: `1000`, later edited to `1100`
- Branch: `1`
- Bank: `1`
- Currency: `1`
- Account parent fields:
  - LC parent: `a6a2a1`
  - Margin parent: `a1a1a4`
  - Acceptance parent: `a2a1a1`
  - Expense parent: `a6a2a1`

Result:

- Save redirected correctly to `/MainErp/LC?selectedId=199`.
- `TblLC` row was inserted.
- No `Notes` or voucher rows were created during initial save.

## LC Account Creation Test

The LC save with `AutoCreateMissingAccounts=true` generated four child accounts safely:

| Account_Code | Account_Serial | Account_Name | Parent |
| --- | --- | --- | --- |
| `a1a1a4a193` | `110310193` | LC Margin UAT-LC-20260509164035 | `a1a1a4` |
| `a2a1a1a188` | `210310188` | LC Acceptance UAT-LC-20260509164035 | `a2a1a1` |
| `a6a2a1a190` | `620100006` | LC UAT-LC-20260509164035 | `a6a2a1` |
| `a6a2a1a191` | `620100007` | LC Expenses UAT-LC-20260509164035 | `a6a2a1` |

Result:

- Account creation worked.
- Accounts were linked back to `TblLCID = 199`.
- User-facing account display remains based on `Account_Serial - Account_Name`; raw `Account_Code` stays internal.

## LC Edit Test

Edited `TblLCID=199` from `/MainErp/LC/Edit/199`.

Changed:

- Value: `12500.00`
- OpenValue: `333.33`
- OpenBalance: `1100`
- ProjectName: `UAT Project Edited`
- Remarks: `UAT edit from Codex 20260509164141`

Result:

- Save redirected correctly to `/MainErp/LC?selectedId=199`.
- `TblLC` row updated.
- Existing generated accounts were preserved.
- No duplicate accounts were created during edit.

## LC Voucher Posting Test

Executed protected LC actions on the UAT LC:

- `CreateVoucher`
- `CreateOpenExpenseVoucher`
- `CreateOpeningBalanceVoucher`
- `CreateGridVouchers`

Results:

| Action | Result |
| --- | --- |
| Header opening voucher | Created `Notes.NoteID=222101`, `NoteSerial=202603202` |
| Open expense voucher | Created `Notes.NoteID=222102`, `NoteSerial=202603203` |
| Opening balance voucher | Created `opening_balance_voucher_id=6578` in `DOUBLE_ENTREY_VOUCHERS1` |
| Grid voucher action | Returned safely; no grid rows needed posting |

Voucher balance validation:

| Voucher source | Rows | Debit | Credit | Difference |
| --- | ---: | ---: | ---: | ---: |
| `Notes_ID=222101` | 2 | 625.00 | 625.00 | 0.00 |
| `Notes_ID=222102` | 3 | 333.33 | 333.33 | 0.00 |
| Opening balance group `6578` | 2 | 1100.00 | 1100.00 | 0.00 |

Journal entry links:

- `/MainErp/JournalEntries/DetailsByNote/222101` opened successfully.
- `/MainErp/JournalEntries/DetailsByNote/222102` opened successfully.

Audit evidence:

- `LC.PostHeader`
- `LC.PostOpenExpense`
- `LC.PostOpeningBalance`
- `LC.PostGridVouchers`

were written to `MainErp_AuditLog` for `EntityKey=199`.

## LC UI and Usability Test

Before this pass, `/MainErp/LC/Edit/199` rendered about `9.29 MB` because every account dropdown loaded up to 3000 account options repeatedly.

Fix applied:

- Account lookup loading now loads a bounded working set plus all currently selected account codes.
- Existing grid/account selections remain available.
- Page size after the fix: about `874 KB`.
- Option count after the fix: about `8,228`.

Remaining UX improvement:

- A future AJAX account search is recommended for very large charts of accounts.
- The current bounded lookup is acceptable for this delivery pass and is much safer than the previous 9 MB page.

## Project Extract Test Summary

Test routes:

- `/MainErp/ProjectExtracts`
- `/MainErp/ProjectExtracts?searchText=202604491`
- `/MainErp/ProjectExtracts/Details/3499`
- `/MainErp/ProjectExtracts/Report/3499`

All tested routes returned HTTP 200 after login and did not show server errors.

## Project Extract Sample

Sample tested:

- `project_billl.id = 3499`
- `NoteID = 222097`
- `NoteSerial = 202604491`
- `ManualNO = 2611001462`
- Project: `College Of Engineering .King Faisal University`

Header values:

- Total: `1500.00`
- VAT: `225.00`
- Net: `1500.00`

Detail grid:

- `project_bill_details` rows loaded: `1`
- Item: `Construction Of College Of Engineering Alfaisal University`
- FullCode: `j1021`
- VAT: `225.00`
- LineFinal: `1725.00`
- AccountCode internal: `a5a1a1a9a379`

Advance payments:

- `TblPayPrePayed`: `0`
- `TblProjePayPrePayed`: `0`
- The screen shows a real empty state instead of a placeholder.

Voucher validation:

| Rows | Debit | Credit | Difference |
| ---: | ---: | ---: | ---: |
| 4 | 1725.00 | 1725.00 | 0.00 |

Result:

- Project Extract details are now a real read-only operational view.
- Real detail lines, VAT, final totals, and linked accounting rows are visible.
- Account display uses `Account_Serial - Account_Name` where the account can be resolved.

## Project Extract Gaps

The following are not implemented yet and therefore could not be passed as UAT-ready:

- New Project Extract creation.
- Project Extract edit/save.
- Project Extract posting/rebuild.
- Project Extract delete/cancel workflow.
- Project Extract account creation.

These gaps are expected based on the current controller: `ProjectExtractsController` only exposes `Index`, `Details`, and `Report`.

## Safety Validation

- No `Areas\Pos` files were intentionally modified in this UAT pass.
- No `AllScripts.sql` changes were made.
- No MainErp SQL migration scripts were added in this pass.
- LC tests did write to the `Eng` test database as requested:
  - `TblLC`
  - `ACCOUNTS`
  - `Notes`
  - `DOUBLE_ENTREY_VOUCHERS`
  - `DOUBLE_ENTREY_VOUCHERS1`
  - `MainErp_AuditLog`
- Project Extract tests were read-only.

## Build

`MyERP.sln` was built successfully in Debug / Any CPU after the LC lookup performance fix.

## Delivery Verdict

LC is functionally testable for create/save/edit/account generation/basic posting/opening balance/audit/report/journal trace on the Eng test database.

Project Extracts are strong as read-only migrated operational screens, but write workflows are not implemented yet and must be treated as the next major migration phase.

