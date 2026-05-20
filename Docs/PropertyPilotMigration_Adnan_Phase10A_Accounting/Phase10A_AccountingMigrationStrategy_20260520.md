# Phase 10A - Accounting Migration Strategy
Date: 2026-05-20

## Strategy Chosen
Hybrid controlled accounting migration:

- Migrate historical cash receipts only when they are linked to migrated active-contract installments.
- Migrate the journal entries/details for those migrated receipts only.
- Keep existing Opening Balance and Advance Payments staging from Phase10.
- Exclude cash issues and owner payments from operational migration until manual review.
- Do not migrate all general accounting history.

## Why This Strategy
| Option | Decision | Reason |
|---|---|---|
| A - receipts/issues for 283 contracts only | Partially accepted | Receipts linked by installments are safe; issues are not safely contract-linked |
| B - all receipts/issues for tenants | Rejected | 4,923 tenant receipt notes include older/non-active contexts and would distort active contract testing |
| C - opening balance only + after cutover | Too limited | Does not satisfy realistic receipt history requirement |
| D - hybrid | Selected | Best balance: real receipt history for migrated contracts, no unsafe general ledger history |

## Final Scope Executed
| Item | Result |
|---|---:|
| Cash receipts migrated | 753 |
| Cash receipt batch details migrated | 811 |
| Journal entries migrated | 753 |
| Journal detail lines migrated | 2,360 |
| Cash issue candidates excluded | 6 |
| Advance payment staging retained | 14 rows / 55,592.8900 |

## Non-Migrated Accounting
- General ledger entries not linked to migrated receipt notes.
- Cash issue/payment notes that are tenant/property-related but not safely contract-linked.
- Property owner payments / `SourceTypeId=13`.
- Old full accounting history unrelated to the migrated property contracts.
