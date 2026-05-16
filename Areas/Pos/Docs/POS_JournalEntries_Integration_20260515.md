# POS Journal Entries Integration

Date: 2026-05-15

## Result

POS/Kishny remains the operational source behavior for normal journal entries, but it is now wired through the shared journal service and shared UI foundation.

The POS route and JSON endpoint names are unchanged:

- `/Pos/JournalEntries/Index`
- `/Pos/JournalEntries/Search`
- `/Pos/JournalEntries/Get/{id}`
- `/Pos/JournalEntries/Save`
- `/Pos/JournalEntries/Accounts`
- `/Pos/JournalEntries/AccountTree`

## POS Boundary Preserved

POS still keeps:

- POS login/session restoration.
- POS layout/CSS wrapper.
- POS route URLs.
- POS permission decisions.
- Non-admin branch restriction.
- General admin password behavior for automatic-entry edits.
- Normal journal profile only.

POS does not expose opening-balance creation in this phase.

## Shared Parts Used

- Shared service: `Common/JournalEntries/SharedJournalService.cs`
- Shared SQL engine: `Common/JournalEntries/SharedJournalSqlRepository.cs`
- Shared UI partial: `Views/Shared/JournalEntries/_JournalWorkspace.cshtml`
- Shared JS: `Scripts/JournalEntries/shared-journal-workspace.js`

POS wrapper:

- `Areas/Pos/Views/JournalEntries/Index.cshtml`

POS controller:

- `Areas/Pos/Controllers/JournalEntriesController.cs`

## Normal Journal Profile

- Header table: `Notes`
- Detail table: `DOUBLE_ENTREY_VOUCHERS`
- Manual note type: `57`

POS validation remains aligned with the prior working behavior. To avoid a regression, POS keeps the prior zero-value-line tolerance while MainErp/opening-balance uses the stricter validation profile.

## QA

- Browser smoke: PASS for `/Pos/JournalEntries/Index`.
- Shared script loaded: PASS.
- Console errors: none.
- POS invalid save: PASS, JSON validation returned.
- POS account lookup/tree: PASS.
- POS opening balance: intentionally not exposed.

## Files Changed

- `Areas/Pos/Controllers/JournalEntriesController.cs`
- `Areas/Pos/Views/JournalEntries/Index.cshtml`
- `Common/JournalEntries/SharedJournalService.cs`
- `Common/JournalEntries/SharedJournalSqlRepository.cs`
- `Views/Shared/JournalEntries/_JournalWorkspace.cshtml`
- `Scripts/JournalEntries/shared-journal-workspace.js`
- `MyERP.csproj`
