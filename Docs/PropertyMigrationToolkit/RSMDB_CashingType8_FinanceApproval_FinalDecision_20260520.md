# RSMDB CashingType=8 Finance Approval Final Decision - 2026-05-20

## Decision

RSMDB is not ready for Accounting Pilot Execute yet.

The correct CashingType=8 receipt scope is now known and measurable, but finance mappings have not been approved/applied for this scope.

## Completed

- Created CashingType=8-specific finance approval pack.
- Added scoped approval support using ScopeName = RSMDB_CashingType8 and ApprovalBatchId.
- Ran impact simulation for Top 25, Top 50, Top 100, and Score >= 60.
- Updated Runner with CashingType8FinanceApproval stage.
- Confirmed no migration/posting was executed.

## Key Numbers

| Metric | Value |
|---|---:|
| Finance pack accounts | 733 |
| ApproveAfterFinanceReview | 97 |
| NeedsMoreInfo | 636 |
| Actual ReadyForAccountingPilot | 0 |
| Best simulated ready set | 32 receipts / 966,568.2500 |

## Safety Status

- RSMDB was not modified.
- No production database was modified.
- No receipts were created.
- No journals were created.
- No suspense approval was applied.
- No accounting pilot was executed.

## Next Step

Finance must approve/correct the CashingType=8-specific account mappings. After that, rerun this stage with explicit approval application, rebuild the ready set, then execute a small Accounting Pilot on the clone only.
