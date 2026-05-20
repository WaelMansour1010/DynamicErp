/*
Cash Phase 3 - Archive Move Draft (NOT EXECUTABLE)
Date: 2026-05-20
Purpose: Design-only draft for safe historical archiving.
IMPORTANT: This file is intentionally non-executable guidance.
No DELETE / No INSERT execution in current phase.
*/

/*
=============================
0) PREPARATION (DESIGN ONLY)
=============================
- Proposed archive DB name: Cash_Archive
- Proposed cutoff example: < 2024-01-01 (after business sign-off)
- Keep same schema for archive tables:
  dbo.Transactions
  dbo.Transaction_Details
  dbo.DOUBLE_ENTREY_VOUCHERS
  dbo.Notes
- Add metadata columns in archive copy (optional): ArchivedAt, ArchiveBatchId
- Enable verification tables/logs for row-count reconciliation.
*/

/*
==========================================
1) RELATION-AWARE ARCHIVE ORDER (DESIGN)
==========================================
Suggested extraction keys (from live Cash):
- Candidate Transactions: Transaction_Date < @CutoffDate
- Candidate Transaction_Details: by Transaction_ID in candidate Transactions
- Candidate Notes: NoteDate < @CutoffDate OR Transaction_ID in candidate Transactions
- Candidate DOUBLE_ENTREY_VOUCHERS:
  RecordDate < @CutoffDate
  OR Transaction_ID in candidate Transactions
  OR Notes_ID in candidate Notes

This ensures accounting/document chains are moved together.
*/

/*
====================================================
2) BATCH COPY PATTERN (PSEUDO, DO NOT EXECUTE HERE)
====================================================
-- BEGIN TRY
--   BEGIN TRAN;
--
--   -- Step A: stage candidate keys into temp tables
--   -- SELECT Transaction_ID INTO #ArchiveTransactionKeys FROM dbo.Transactions WHERE Transaction_Date < @CutoffDate;
--   -- SELECT NoteID INTO #ArchiveNoteKeys FROM dbo.Notes WHERE NoteDate < @CutoffDate OR Transaction_ID IN (SELECT Transaction_ID FROM #ArchiveTransactionKeys);
--
--   -- Step B: copy parent/child in safe order to Cash_Archive
--   -- INSERT INTO Cash_Archive.dbo.Transactions (...) SELECT ... FROM dbo.Transactions WHERE Transaction_ID IN (...);
--   -- INSERT INTO Cash_Archive.dbo.Transaction_Details (...) SELECT ... FROM dbo.Transaction_Details WHERE Transaction_ID IN (...);
--   -- INSERT INTO Cash_Archive.dbo.Notes (...) SELECT ... FROM dbo.Notes WHERE NoteID IN (...);
--   -- INSERT INTO Cash_Archive.dbo.DOUBLE_ENTREY_VOUCHERS (...) SELECT ... FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE ...;
--
--   -- Step C: verification checks (must match)
--   -- SELECT COUNT(*) live_vs_archive ...
--   -- checksum/hash spot checks ...
--
--   COMMIT;
-- END TRY
-- BEGIN CATCH
--   IF @@TRANCOUNT > 0 ROLLBACK;
--   THROW;
-- END CATCH;
*/

/*
==============================================
3) READ STRATEGY WITHOUT BREAKING REPORTS
==============================================
Options:
- Option A: keep live reports as-is + add historical report endpoints against Cash_Archive.
- Option B: create UNION ALL views for historical + live reads (careful with performance).
- Option C: synonyms/reporting layer routing by date range.

Rule:
- No change to voucher/note numbering logic in live DB.
- Serial generators continue only on live current-year data.
*/

/*
==================================================
4) SAFETY GATES BEFORE ANY REAL ARCHIVE EXECUTION
==================================================
- Gate 1: Business sign-off for cutoff date and legal retention.
- Gate 2: Reconciliation pass (row counts + financial totals per month).
- Gate 3: Report regression test (top critical SP/Views).
- Gate 4: Rollback plan documented and tested.
*/

/*
======================================
5) POST-ARCHIVE (FUTURE PHASE, DESIGN)
======================================
- Rebuild/report stats windows after large data movement.
- Validate query plans on top report procedures.
- Optional controlled shrink consideration only after stability period.
*/
