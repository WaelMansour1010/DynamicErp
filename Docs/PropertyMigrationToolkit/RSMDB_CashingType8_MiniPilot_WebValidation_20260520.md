# RSMDB CashingType=8 Mini Pilot Web Validation - 2026-05-20

## Result

Blocked, not failed.

## Reason

A local iisexpress process was present, but no listening IIS Express port was found for DynamicErp during this run. Therefore browser-level validation could not be completed safely before rollback.

## Completed Instead

Database-level validation was completed successfully:

- Receipts inserted and linked to contracts/renters/installments.
- Journal headers/details inserted.
- Debit/Credit balanced.
- AccountId=NULL = 0.
- Rollback completed cleanly.

## Required Next Web Test

After starting DynamicErp against $clone, repeat:

1. Open a migrated contract from the 32-scope set.
2. Review payments/Paid/Remain.
3. Open migrated receipt if UI supports historical receipt lookup.
4. Review the linked journal.
5. Create a new receipt on the same contract, validate posting, then clean it up.
