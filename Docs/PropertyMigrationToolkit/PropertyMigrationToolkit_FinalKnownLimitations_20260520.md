# PropertyMigrationToolkit Final Known Limitations - 2026-05-20

## Still Draft / Review-Only

| Area | Status |
|---|---|
| RSMDB full accounting migration | Not approved; mini pilot only |
| RSMDB owner payments | Manual review only |
| RSMDB NoteType 9088 | Not classified for migration |
| RSMDB terminations | Not approved |
| Generic full production migration | Conditional; requires signed plan |
| Some customer-specific scripts marked DRAFT | Keep as review-only until promoted |
| Web automation validation | Manual/blocked unless app is running |

## Customer-Specific Logic Remaining

- RSMDB `CashingType=8` and `ContracttBillInstallmentsDone` receipt allocation logic.
- Adnan active-contract mapping and opening-balance decisions.
- Finance-approved account mappings per customer.

## Permanent Manual/Finance Review Areas

- Account mappings with confidence below approval threshold.
- Suspense/Holding accounts.
- Owner payments.
- Unclassified NoteTypes.
- Historical journals not linked to approved receipts/issues/contracts.

## Biggest Limitation

The toolkit can reduce review work dramatically, but accounting migration still depends on finance-approved account mapping and evidence-based voucher linking. It must not become a fully autonomous financial posting tool.
