# Phase 4 Web Validation Checklist - 2026-05-20

## Preconditions

- Sandbox DB exists: `Alromaizan_PropertyPilot_Adnan_20260520`.
- Phase3 fixed migration scripts are approved and re-run after rollback.
- Phase4 operational seed script is executed on Sandbox only.
- Authenticated Sandbox user is available.
- User has Department 44 and active CashBox access.
- Payment methods `CASH-PILOT` and `BANK-PILOT` exist.

## Login

1. Open DynamicErp local URL.
2. Use Debug/DevStart database override to point to `Alromaizan_PropertyPilot_Adnan_20260520`.
3. Login with existing Sandbox admin or approved Sandbox-only pilot admin.
4. Confirm username appears in the shell.
5. Confirm no redirect loop.

## Property Screens

1. Open properties list.
2. Confirm migrated properties appear.
3. Open a migrated property.
4. Confirm units are visible.
5. Confirm no FK/null runtime exception.

## Unit Screens

1. Open units list.
2. Filter/search by migrated unit number.
3. Confirm status is suitable for active contract.
4. Confirm unit links to property.

## Contract Screens

1. Open contracts list.
2. Confirm 283 migrated contracts after re-run.
3. Open one migrated contract.
4. Confirm renter, property, unit, dates, totals.
5. Review payment schedule/batches.
6. Confirm gross future installment schedule matches source.
7. Confirm advance staging/credit treatment is visible or reconciled if implemented.

## Receipt Test

1. Create receipt for one migrated contract in Sandbox only.
2. Select source type `Property Renter` or property contract source as required by UI.
3. Select `CASH-PILOT` payment method.
4. Select active CashBox.
5. Allocate to one unpaid batch.
6. Save.
7. Confirm receipt row created.
8. Confirm batch paid/remain changed as expected.
9. Confirm journal entry is created only if intended by sandbox accounting settings.
10. Confirm no production DB was touched.

## Payment Test

1. Open Cash Issue Voucher if payment scenario is needed.
2. Select `CASH-PILOT` or `BANK-PILOT` issue method.
3. Select cashbox/bank account.
4. Save a minimal Sandbox-only test if business scenario is approved.
5. Review generated journal.

## Termination Test

1. Pick one migrated contract with simple unpaid balance.
2. Open termination screen.
3. Load contract.
4. Confirm unit, renter, balances, insurance, and unpaid amount load.
5. Do not finalize unless test scope explicitly approves writing termination in Sandbox.
6. If finalized in Sandbox, verify unit status and journal behavior.

## Reconciliation After Web Test

1. Compare contract batch paid/remain before and after receipt.
2. Review CashReceiptVoucher and allocation table.
3. Review JournalEntry/JournalEntryDetail.
4. Confirm CashBox/Bank account impact.
5. Confirm no writes to `Adnan` or original `Alromaizan`.
