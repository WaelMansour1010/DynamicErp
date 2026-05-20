# Property Migration Toolkit - Runbook
Date: 2026-05-20

## Stage 0 - Preflight
1. Confirm source DB is online and read-only for migration work.
2. Create or restore a target clone. Never run first migration on production.
3. Confirm DB name includes `PropertyPilot`, `ReadyToTest`, `PilotClone`, `Sandbox`, or another approved safe marker.
4. Run schema compare against `MyErp` or the approved reference.
5. Apply reviewed missing-column scripts if needed on clone only.
6. Create toolkit core tables using `Sql\00_ToolkitCore_ConfigAndXref_Generic.sql`.
7. Insert customer config and mark `IsApproved=1` only after review.

## Stage 1 - Discovery
1. Run `Discovery_SELECT_ONLY_Generic.sql`.
2. Identify property, unit, tenant, contract, installment, receipt, issue, journal, termination tables.
3. Build a note-type matrix from actual data and VB6 code where available.
4. Capture candidate joins from voucher -> installment -> contract -> tenant.

## Stage 2 - Mapping
1. Fill customer-specific mapping notes.
2. Validate required accounts by account code.
3. Decide operational seed mode: pilot branch/cashbox/bank or mapped existing setup.
4. Decide accounting strategy: opening-only, receipts-only, hybrid, or archive-only.

## Stage 3 - Diagnostics
Run diagnostics before migration:
- Contracts without unit.
- Contracts without tenant.
- Installments without contract.
- Receipts without contract/installment link.
- Missing accounts.
- Unbalanced source journals.
- Payment methods/cashboxes/banks without accounts.
- Advance payments and terminations.

Stop if critical counts are not reviewed.

## Stage 4 - Migration
Recommended order:
1. Core config / cross reference tables.
2. Operational seed.
3. Accounts.
4. Properties.
5. Units.
6. Tenants.
7. Contracts.
8. Installments.
9. Opening balances.
10. Advance payments staging.
11. Receipts only if safe.
12. Issues only if approved.
13. Journals only when linked to approved migrated vouchers.
14. Terminations only after separate validation.

## Stage 5 - Reconciliation
Run `Reconciliation_Generic.sql` and customer-specific reconciliations.
Required pass criteria:
- No `AccountId=NULL`.
- No unbalanced journals.
- No migrated contract without unit/renter.
- Counts and totals match approved scope.
- Exclusions are documented.

## Stage 6 - Web Validation
Test:
- Login.
- Property list/details.
- Unit list/details.
- Contract list/details.
- Batches/paid/remain.
- Historical receipt if migrated.
- New receipt.
- Termination.
- Reports.

Clean all test vouchers/terminations before delivery unless the customer explicitly wants them retained.

## Stage 7 - ReadyToTest Delivery
1. Confirm migrated data remains.
2. Confirm no test artifacts remain.
3. Prepare delivery report.
4. Prepare user testing guide.
5. Prepare rollback/reset script but do not run it.
6. State ReadyToTest / Not Ready / Needs Review.

## Stop Conditions
Stop immediately if:
- Target is production by name or connection.
- Source DB would be modified.
- Missing accounts remain.
- Journal imbalance exists.
- Payment/owner semantics are unclear.
- Contract/unit/renter links are incomplete outside approved exclusions.
