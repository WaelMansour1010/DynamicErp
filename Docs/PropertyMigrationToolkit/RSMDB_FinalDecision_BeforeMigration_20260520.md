# RSMDB Final Decision Before Migration - 2026-05-20

## Decision

RSMDB is not yet approved for migration execute.

## What Is Ready

- Discovery complete enough for mapping design.
- VB6 property/owner/contract/installment relationships identified.
- RSMDB staging mapping draft created.
- Owners are now included in the toolkit staging contract and templates.
- Runner DryRun completed successfully.

## What Blocks Migration Execute

- Active contract rule needs final VB6 confirmation.
- Account mapping to DynamicErp `ChartOfAccount` is not approved.
- Owner payable rows in `TblAqrOwin` need finance review.
- Owner payment scenario is not enabled by default.
- `NoteType=9088` is unclassified.
- Journal grouping/direction needs deeper validation due high potential unbalanced group count.
- Receipts need stronger installment/contract linking for the `1587` weakly linked rows.

## Next Step

Create an RSMDB clone and run only:

1. Core setup.
2. Source staging table creation.
3. `RSMDB_StagingMapping_SELECT_TO_STAGING_DRAFT_20260520.sql` on the clone.
4. Staging diagnostics.
5. Mapping review with finance/implementation.

No final migration templates should run until this review is complete.
