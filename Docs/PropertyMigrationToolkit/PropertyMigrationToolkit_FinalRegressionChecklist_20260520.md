# PropertyMigrationToolkit Final Regression Checklist - 2026-05-20

Run this checklist after any toolkit change.

## Runner

- Build runner: 0 errors.
- DryRun against sample config completes.
- Execute is blocked when BackupVerified=false.
- Execute is blocked when ExecutionPlanApproved=false.
- Execute is blocked when BatchId is empty.
- Execute is blocked for production/live-looking target names.

## Discovery and Diagnostics

- Discovery reads source metadata without modifying source.
- Diagnostics writes only to clone toolkit tables.
- ReviewQueue remains populated for unresolved issues.

## Intelligence

- Receipt allocation discovery still identifies RSMDB `ContracttBillInstallmentsDone`.
- CashingType=7 is not treated as property-contract receipt.
- CashingType=8 candidate builder returns expected RSMDB baseline.
- Account intelligence does not auto-approve weak/blocked accounts.

## Migration

- Master data entity maps are created.
- Receipts require contract/installment/renter links.
- Journals require approved accounts and balance.
- No AccountId=NULL.
- No unbalanced journals.
- No duplicate entity maps.

## Accounting

- Debit equals Credit per migrated journal.
- Receipt totals equal allocation totals for pilot scope.
- Suspense usage is reported and requires sign-off.
- Owner payments remain excluded unless explicitly approved.

## Rollback

- Batch rollback removes only the current batch.
- Operational seed is retained unless explicitly scoped for cleanup.
- No orphan receipt, journal, or detail rows remain.

## Web

- Login works on clone.
- Property/contract screens open.
- Migrated payments display correctly.
- New receipt after migration posts correctly.
- No duplicate posting after edit/retry.
