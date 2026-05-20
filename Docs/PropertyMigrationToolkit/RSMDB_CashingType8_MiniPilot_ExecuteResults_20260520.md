# RSMDB CashingType=8 Mini Pilot Execute Results - 2026-05-20

## Execute Scope

- Target clone: Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520
- Operational BatchId: 1B5D8EB5-DD7E-4B63-8DDB-E7B00AAD558B
- Pilot BatchId: CD05CD47-10F5-4CC1-9467-CC496A694797
- Scope: CashingType=8 / NoteType=4 receipts only
- Excluded: Issues, Owner Payments, Terminations, 9088, Suspense, weak/manual/blocked records

## Execution Results

| Metric | Value |
|---|---:|
| Receipts migrated | 32 |
| Journals migrated | 32 |
| Journal lines migrated | 64 |
| Debit total | 966,568.2500 |
| Credit total | 966,568.2500 |
| Receipt to installment link rows | 51 |

## Objects Created During Pilot

- CashReceiptVoucher
- CashReceiptVoucherPropertyContractBatch
- JournalEntry
- JournalEntryDetail
- PropertyMigrationEntityMap rows for Receipt/Journal

## Note

These objects were created only for the Mini Pilot and were subsequently rolled back by PilotBatchId.
