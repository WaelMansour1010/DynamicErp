# Current Priority

Date: 2026-05-15

Phase: Stabilization + Unification.

This roadmap is audit-only output. No application code, SQL, routes, or screens were changed.

## Priority 1: Journal Entry Unification

Current truth:

- POS has the working operational journal entry screen at `/Pos/JournalEntries/Index`.
- MainErp has a journal screen at `/MainErp/JournalEntries`, but it is read-only and does not provide the full create/save/edit workflow.
- There is no shared journal-entry core yet.

Next implementation goal:

- Create one shared journal engine.
- Keep separate MainErp and POS wrappers.
- Preserve POS behavior.
- Enable MainErp journal entry without routing into POS.

Required before coding:

- Confirm target tables and note-type rules for MainErp.
- Define MainErp journal permissions.
- Decide which POS validation/posting parts can move safely into shared code.

## Priority 2: Opening Balance Journal

Current truth:

- No professional shared `قيد افتتاحي` journal mode exists yet.
- Opening balance logic is fragmented across account opening balance fields, LC opening vouchers, bank/box opening fields, reports, and planned import models.
- LC opening balance logic uses opening note type `101` and posts to `Notes1` / `DOUBLE_ENTREY_VOUCHERS1`.
- POS manual journal uses note type `57` and posts to `Notes` / `DOUBLE_ENTREY_VOUCHERS`.

Next implementation goal:

- Trace Main Original VB6 opening-balance logic from `F:\Source Code\SatriahMain`.
- Implement opening balance as `OpeningBalance` mode in the shared journal engine.
- Add MainErp menu item `قيد افتتاحي`.
- Add MainErp route such as `/MainErp/JournalEntries/OpeningBalance`.
- Keep POS opening balance hidden or read-only unless explicitly approved.

Do not do yet:

- Do not copy Kishny/POS opening behavior as MainErp source authority.
- Do not create a duplicate hardcoded opening balance form.
- Do not allow unsafe unbalanced posting silently.

## Priority 3: Freeze Placeholder Risk

Modules needing clear status before users rely on them:

- MainErp Purchases.
- MainErp Stock Transfers.
- Opening Balance Journal.
- MainErp Journal Entry write workflow.

Action:

- Keep placeholder or disabled workflows visibly under review until they pass source parity, permission, validation, database, and browser smoke gates.

## Priority 4: Shared Boundaries

Good shared patterns already present:

- `Common/StoreData`
- `Common/Users`
- `Common/EmployeePayroll`
- `Common/DiscountNotifications`

Next shared candidates:

- Journal entry engine.
- Account tree/search component.
- Voucher validation/posting primitives.
- Permission descriptor map for shared modules.

Rule:

- Shared core is allowed.
- Shared route is not required and is often wrong.
- MainErp and POS should keep separate routes, layouts, permissions, and database/session context.

## Priority 5: Permission Hardening

Immediate permission map needed for:

- Journal vouchers.
- Opening balance journal.
- Purchases.
- Stock transfers.
- POS payments/custody.
- System manager/admin screens.

Acceptance rule:

- Every dangerous button must have matching server-side enforcement.
- Menu hiding is not enough.

## Priority 6: QA Gate For Next Coding Phase

Before delivering code changes in the next phase:

- Build passes.
- MainErp journal route opens.
- POS journal route still opens.
- MainErp opening balance route opens if implemented.
- Menus show correct Arabic labels.
- Account lookup works.
- Save validation blocks unsafe journal data.
- Database tests use `Eng` unless a module requires a documented different sample database.
- Browser smoke passes with no raw server errors and no console errors.

## Immediate Recommendation

Start the next implementation phase with Journal Entry unification, because it directly blocks the reported issue: the Kishny/POS journal screen works, while MainErp journal does not operate as the same entry screen.

Then implement `قيد افتتاحي` as a controlled mode of that same journal engine after tracing Main Original VB6 opening-balance rules.
