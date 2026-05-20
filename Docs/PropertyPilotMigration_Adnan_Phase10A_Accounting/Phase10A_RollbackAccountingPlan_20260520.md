# Phase 10A - Rollback Accounting Plan
Date: 2026-05-20
BatchId: `A10AD000-0000-4000-9000-202605200010`

## Rollback Procedure
Created procedure:

`usp_PropertyPilot_RollbackAccountingBatch_Adnan`

Required confirmation:

```sql
EXEC dbo.usp_PropertyPilot_RollbackAccountingBatch_Adnan
    @MigrationBatchId = 'A10AD000-0000-4000-9000-202605200010',
    @Confirm = N'YES_ROLLBACK_ACCOUNTING';
```

## Rollback Scope
The rollback removes only Phase10A accounting batch artifacts:

- JournalEntryDetail rows for Phase10A journal entries.
- JournalEntry rows migrated in Phase10A.
- CashReceiptVoucherPropertyContractBatch rows for Phase10A receipts.
- CashReceiptVoucher rows migrated in Phase10A.
- Phase10A seeded accounting accounts, only when marked by Phase10A notes.
- Phase10A cross references.
- Phase10A validation issues.

## Safety
- Blocks `Adnan`, `Alromaizan`, and `RSMDB`.
- Requires DB name to include `PropertyPilot`, `ReadyToTest`, `PilotClone`, or `Sandbox`.
- Requires explicit confirmation string.

Rollback was not executed after Phase10A because the user requested keeping ReadyToTest data for testing.
