# Cash Database Size Audit - Phase 5 Index Maintenance Review (2026-05-20)

## Scope
- Review only, no execution.
- Target tables:
  - `DOUBLE_ENTREY_VOUCHERS`
  - `Transactions`
  - `Transaction_Details`
  - `Notes`
- No `DROP INDEX`, no data changes, no shrink.

## Largest Indexes (impact focus)
- `DOUBLE_ENTREY_VOUCHERS.PK_DOUBLE_ENTREY_VOUCHERS` ~4672.50 MB
- `Transaction_Details.IX_Transaction_Details` ~3772.05 MB
- `Notes.PK_Notes` ~3725.63 MB
- `Transactions.PK_Transactions` ~3344.56 MB
- Very large NCIs on `DOUBLE_ENTREY_VOUCHERS` up to ~2273.55 MB and ~1832.53 MB.

## Fragmentation Decisions (current snapshot)
### Rebuild Candidates (high fragmentation + large page_count)
- `Transaction_Details.IX_Transaction_Details`
  - frag ~54.09%
  - page_count ~479,371
  - action: **REBUILD** (maintenance window)

### Reorganize Candidates (10% to <30%, page_count>=1000)
- `Transactions.IX_POS_Transactions_Card_VisaNumber` (~26.74%)
- `Transactions.IX_POS_Transactions_Search_VisaNumber` (~23.49%)
- `Transactions.IX_POS_KycAvailableCards_Transactions` (~16.21%)
- `Transaction_Details.IX_POS_TransactionDetails_ItemSerial_Transaction` (~15.96%)
- `Transactions.IX_POS_Transactions_Report_ServiceSearch` (~13.06%)
- `Transaction_Details.IX_POS_TransactionDetails_StoreSerials_Report` (~11.14%)
- `Notes.IX_Notes_Header_NoteSerial` (~10.64%)
- `Transactions.IX_Transactions__UserID` (~10.42%)

### No Action Now
- Indexes with page_count < 1000
- Fragmentation < 10%

## Safe Execution Order
1. Reorganize medium-fragmented indexes first (lower impact).
2. Rebuild the severe one (`IX_Transaction_Details`) in a quiet window.
3. Update statistics on the four target tables.
4. Re-check fragmentation and validate critical report/search procedure runtimes.
5. Run in small batches, not one large maintenance blast.

## Rollback / Recovery Notes
- `REORGANIZE` is online-ish and low-risk but not reversible as a single transaction across full batch.
- `REBUILD` does not have a simple rollback after commit except:
  1. Rebuild again (if needed), or
  2. Restore from backup, or
  3. Recreate from scripted definitions (for structural rollback scenarios).
- Mandatory before execution:
  - fresh full backup,
  - tested restore point,
  - scripted index definitions saved,
  - runtime monitoring and stop criteria.

## Files
- Execute review script:
  - `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_Phase5_IndexMaintenance_EXECUTE_REVIEW.sql`
