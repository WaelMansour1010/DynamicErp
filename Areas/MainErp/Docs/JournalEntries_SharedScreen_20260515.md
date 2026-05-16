# Journal Entries Shared Screen

Date: 2026-05-15

## Result

Journal Entries are now treated as a mandatory shared module.

The working POS/Kishny journal implementation was used as the source behavior. POS and MainErp now render the same shared journal UI foundation and delegate to the same shared journal service/repository layer, while keeping separate area routes, login/session context, permissions, and database connection factories.

## Routes

- POS normal journal: `/Pos/JournalEntries/Index`
- MainErp normal journal: `/MainErp/JournalEntries`
- MainErp opening balance journal: `/MainErp/JournalEntries/OpeningBalance`

No cross-area route leakage was introduced. The wrappers pass area-specific endpoint URLs into the shared UI model.

## Shared Architecture

- Shared view model/profile contracts: `Common/JournalEntries/SharedJournalEntryModels.cs`
- Shared service and validation layer: `Common/JournalEntries/SharedJournalService.cs`
- Shared SQL engine: `Common/JournalEntries/SharedJournalSqlRepository.cs`
- Shared UI foundation: `Views/Shared/JournalEntries/_JournalWorkspace.cshtml`
- Shared UI script: `Scripts/JournalEntries/shared-journal-workspace.js`

Thin wrappers:

- POS wrapper: `Areas/Pos/Views/JournalEntries/Index.cshtml`
- MainErp wrapper: `Areas/MainErp/Views/JournalEntries/Index.cshtml`

Controllers:

- POS: `Areas/Pos/Controllers/JournalEntriesController.cs`
- MainErp: `Areas/MainErp/Controllers/JournalEntriesController.cs`

## Profiles

Normal:

- Header table: `Notes`
- Detail table: `DOUBLE_ENTREY_VOUCHERS`
- Manual note type: `57`

OpeningBalance:

- Header table: `Notes1`
- Detail table: `DOUBLE_ENTREY_VOUCHERS1`
- Note type: `101`
- Detail reference: `opening_balance_voucher_id`

## Permissions And Context

POS preserves:

- POS session restoration through `PosLoginController.RestorePosContext`
- POS branch restriction for non-admin users
- POS view/create/edit permission gates
- General admin password requirement for automatic journal edits

MainErp uses:

- Normal screen permission key: `FrmAccEditJournal`
- Opening balance permission key: `FrmAccEditJournal1`
- MainErp login/session context
- MainErp database connection factory

## QA

- Build: PASS, `MSBuild MyERP.csproj /p:Configuration=Debug /p:Platform=AnyCPU`.
- Browser smoke: PASS for `/Pos/JournalEntries/Index`, `/MainErp/JournalEntries`, and `/MainErp/JournalEntries/OpeningBalance`.
- Browser console: no errors on the three journal routes.
- MainErp search: JSON success from normal and opening-balance search endpoints.
- Account lookup/tree: PASS for POS and MainErp endpoints.
- Invalid save: returns JSON validation instead of raw server errors.
- Date rendering fix: shared date inputs now use invariant Gregorian `yyyy-MM-dd`.

## Files Changed

- `Common/JournalEntries/SharedJournalEntryModels.cs`
- `Common/JournalEntries/SharedJournalService.cs`
- `Common/JournalEntries/SharedJournalSqlRepository.cs`
- `Areas/Pos/Controllers/JournalEntriesController.cs`
- `Areas/MainErp/Controllers/JournalEntriesController.cs`
- `Areas/Pos/Views/JournalEntries/Index.cshtml`
- `Areas/MainErp/Views/JournalEntries/Index.cshtml`
- `Views/Shared/JournalEntries/_JournalWorkspace.cshtml`
- `Scripts/JournalEntries/shared-journal-workspace.js`
- `MyERP.csproj`
