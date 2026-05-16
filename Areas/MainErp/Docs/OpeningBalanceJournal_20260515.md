# Opening Balance Journal Mode

Date: 2026-05-15

## Source Authority

Opening-balance journal behavior was traced from:

- `F:\Source Code\SatriahMain\Frm\FrmAccEditJournal1.frm`

`FrmOpeningBalance.frm` is not the authority for this task; it is not the general journal opening-balance screen.

## VB6 Behavior Traced

The VB6 opening-balance journal is the normal journal workflow with a different storage profile:

- Header table: `Notes1`
- Detail table: `DOUBLE_ENTREY_VOUCHERS1`
- Note type: `101`
- Header reference: `Double_Entry_Vouchers_ID`
- Detail reference: `opening_balance_voucher_id`
- Uses account selection/search and debit/credit journal lines.
- Defaults missing line branch to the header branch, or branch `1` in the old fallback path.
- Blocks empty account lines, negative values, debit and credit on the same line, and unbalanced totals.
- Blocks save when totals are not balanced.
- Blocks edit/delete when entries are posted or locked.

## Implemented Design

Opening Balance is not a separate hardcoded screen.

MainErp opening balance opens the same shared journal UI and save/search engine as normal journal entries, with `OpeningBalance` profile selected:

- Route: `/MainErp/JournalEntries/OpeningBalance`
- Search endpoint: `/MainErp/JournalEntries/SearchOpeningBalance`
- Get endpoint: `/MainErp/JournalEntries/GetOpeningBalance/{id}`
- Save endpoint: `/MainErp/JournalEntries/SaveOpeningBalance`

The screen title is `قيد افتتاحي`, and search/list/save operate against the opening-balance profile.

## Safety Rules

Opening balance remains balanced-only, matching VB6.

Validation blocks:

- Missing/invalid date
- Missing/invalid branch
- Missing/invalid account
- Negative debit/credit values
- Debit and credit on one line
- Zero-value account lines
- Fewer than two valid lines
- Unbalanced total debit/credit
- Posted entry update

The UI surfaces validation messages; it does not silently post unsafe entries.

## Remaining Risks

- Exact VB6 `sand_numbering_type` serial parity still needs a dedicated serial-numbering audit before production opening-balance posting is approved.
- Full `LockedInterval`/closed-period parity should be centralized in a period-lock service; Phase 1 blocks posted entries at repository/service level.
- Permanent accounting posting tests were not run without explicit approval. Validation and route behavior were tested; save should be tested against approved rollback or seeded test data before live use.

## QA

- Build: PASS.
- Browser route smoke: PASS for `/MainErp/JournalEntries/OpeningBalance`.
- Console errors: none.
- Opening search: PASS, JSON success from `Notes1`/`DOUBLE_ENTREY_VOUCHERS1`.
- Invalid opening save from UI: PASS, returns `يجب إدخال طرفين على الأقل للقيد.`
- Invalid opening save by HTTP JSON: PASS, returns JSON validation and no raw server error.

## Files Changed

- `Common/JournalEntries/SharedJournalEntryModels.cs`
- `Common/JournalEntries/SharedJournalService.cs`
- `Common/JournalEntries/SharedJournalSqlRepository.cs`
- `Areas/MainErp/Controllers/JournalEntriesController.cs`
- `Areas/MainErp/Views/JournalEntries/Index.cshtml`
- `Views/Shared/JournalEntries/_JournalWorkspace.cshtml`
- `Scripts/JournalEntries/shared-journal-workspace.js`
- `MyERP.csproj`
