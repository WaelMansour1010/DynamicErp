# Shared Journal Final Safety Verification

Date: 2026-05-15

## Scope

This was a verification-only pass. No application code, SQL schema, or routes were changed.

Verified areas:

- POS/Kishny journal regression safety.
- MainErp normal journal safety.
- MainErp opening-balance journal safety.
- Shared UI/date behavior.
- Save target separation by code inspection and controlled save attempt.

## Final Routes

- POS normal journal: `/Pos/JournalEntries/Index`
- MainErp normal journal: `/MainErp/JournalEntries`
- MainErp opening balance: `/MainErp/JournalEntries/OpeningBalance`

Shared UI/script:

- `Views/Shared/JournalEntries/_JournalWorkspace.cshtml`
- `Scripts/JournalEntries/shared-journal-workspace.js`

## POS Regression Safety

Verified:

- POS route opens with HTTP 200 after POS login.
- POS route renders the shared journal workspace.
- POS route loads the shared journal script.
- POS does not redirect to MainErp context.
- POS controller still restores context via `PosLoginController.RestorePosContext`.
- POS still uses `KishnyCashConnection`.
- POS still applies non-admin branch restriction.
- POS still enforces `CanViewJournalEntry`, `CanCreateJournalEntry`, and `CanEditJournalEntry`.
- POS automatic-journal edit still requires general admin password through `IsGeneralAdminPassword`.

Smoke results:

- `/Pos/JournalEntries/Index`: PASS.
- Shared script loaded: PASS.
- Browser console errors: none.
- POS invalid save returned JSON validation: PASS.
- POS filtered search returned JSON success: PASS.

## MainErp Functional Safety

Verified:

- `/MainErp/JournalEntries` opens.
- `/MainErp/JournalEntries/OpeningBalance` opens.
- MainErp normal search returns JSON success.
- MainErp opening-balance search returns JSON success.
- MainErp invalid normal save returns JSON validation.
- MainErp invalid opening-balance save returns JSON validation.
- MainErp permission keys remain separate:
  - Normal: `FrmAccEditJournal`
  - Opening balance: `FrmAccEditJournal1`

Browser smoke:

- Normal title: `إدخال واستعراض القيود`
- Opening-balance title: `قيد افتتاحي`
- Shared workspace root exists.
- Shared script exists.
- Browser console errors: none.

## Save Target Separation

Code inspection confirms the profile targets:

Normal journal:

- Header table: `Notes`
- Detail table: `DOUBLE_ENTREY_VOUCHERS`
- Manual note type: `57`

Opening balance:

- Header table: `Notes1`
- Detail table: `DOUBLE_ENTREY_VOUCHERS1`
- Note type: `101`
- Detail reference: `opening_balance_voucher_id`

The shared repository uses `SharedJournalProfile.ForMode(mode)` to choose table names and note type. Normal and opening-balance endpoints pass different modes and do not share write targets.

## Real Save Test Status

Real save test was attempted only with safe, uniquely named verification data in database `Eng`, using balanced test lines and a cleanup guard.

Result: BLOCKED before insert.

Failure:

```text
Normal save failed: تعذر حفظ القيد Could not find stored procedure 'dbo.GetNextID_FromSequence'.
```

Database verification:

```text
DB = Eng
OBJECT_ID(N'dbo.GetNextID_FromSequence', N'P') = NULL
```

Impact:

- No verification journal entry was created.
- No cleanup rows were needed.
- Permanent financial posting was not performed.
- Because the normal save failed at ID generation, opening-balance real save was not attempted in this pass.

Required before production approval:

- Decide whether `GetNextID_FromSequence` must be created/migrated for `Eng`, or whether the shared repository must use an existing approved ID allocator already present in the production schema.
- Re-run normal and opening-balance save verification after the allocator decision.
- Verify no cross-write after successful saves by checking the inserted `Remark`/`NoteID` in only the expected header/detail tables.

## Accounting Safety

Verified by service/repository inspection and invalid-save tests:

- Missing branch is blocked.
- Missing account lines are blocked.
- Negative debit/credit values are blocked.
- Debit and credit on the same line are blocked.
- Unbalanced totals are blocked.
- Opening balance is balanced-only.
- Posted entries are blocked from update via `IsPosted`.
- MainErp validates account existence.
- MainErp validates branch existence.
- POS preserves its prior zero-value-line tolerance to avoid regression; MainErp/opening-balance uses stricter zero-value-line validation.

Note type behavior by profile:

- Normal profile uses `NoteType = 57`.
- Opening-balance profile uses `NoteType = 101`.

Generated ID/serial status:

- Not fully verified by real save because `dbo.GetNextID_FromSequence` is missing in `Eng`.
- Opening-balance serial generation remains a production approval checklist item because exact VB6 `sand_numbering_type` parity has not been signed off.

## Database Safety

Database used:

- MainErp: `Eng`
- POS: `Cash`

No SQL schema was changed.

No permanent production financial posting was performed.

The only real save attempt failed before insert because the required ID allocator procedure is missing in `Eng`.

## UI Safety

Verified:

- Gregorian date input remains `yyyy-MM-dd`.
- Browser observed date value: `2026-05-15`.
- Arabic RTL route rendering is intact.
- POS visual flow still renders the journal workspace with the same POS CSS wrapper.
- MainErp and POS wrappers load the same shared partial and shared journal script.

## Remaining Production Approval Checklist

Before enabling production posting:

- Resolve `dbo.GetNextID_FromSequence` missing in `Eng`.
- Re-run controlled normal journal save and cleanup.
- Re-run controlled opening-balance save and cleanup.
- Verify normal save writes only:
  - `Notes`
  - `DOUBLE_ENTREY_VOUCHERS`
- Verify opening-balance save writes only:
  - `Notes1`
  - `DOUBLE_ENTREY_VOUCHERS1`
- Verify normal note type is `57`.
- Verify opening-balance note type is `101`.
- Verify generated `NoteID`, `Double_Entry_Vouchers_ID`, `NoteSerial`, and `NoteSerial1` are not duplicated.
- Verify locked/closed-period behavior against the approved period-lock source.
- Approve opening-balance serial policy against VB6 `sand_numbering_type`.

## Rollback And Deploy Notes

Deployment notes:

- Deploy shared common files, area wrappers, and shared script together.
- Do not deploy MainErp write capability as production-approved until the allocator blocker is resolved and the successful save/no-cross-write verification is repeated.
- POS route behavior should be smoke-tested immediately after deploy because POS is the operational source behavior.

Rollback notes:

- Revert wrappers to the prior POS view and prior MainErp route behavior if shared UI/script loading fails.
- No SQL rollback is required from this verification pass because no schema changes were made and the real save test inserted no rows.

## Verification Summary

PASS:

- POS route and context boundary.
- MainErp normal route.
- MainErp opening-balance route.
- Search endpoints with filters.
- Invalid-save validation responses.
- UI date format and browser console safety.
- Code-level save target separation.

BLOCKED:

- Real normal/opening save verification is blocked by missing `dbo.GetNextID_FromSequence` in `Eng`.

