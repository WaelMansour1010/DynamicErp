/*
Cash Phase 2 - Index Maintenance Draft (DO NOT EXECUTE NOW)
Date: 2026-05-20
SQL Server 2012 compatible

Policy draft:
- REBUILD when page_count >= 1000 and fragmentation >= 30%
- REORGANIZE when page_count >= 1000 and fragmentation between 10% and 29.99%
- Update stats recommended after large maintenance batches
*/
USE [Cash];
GO

/* =========================
   REBUILD Draft (Top target)
   ========================= */
-- ALTER INDEX [IX_Transaction_Details] ON [dbo].[Transaction_Details] REBUILD WITH (SORT_IN_TEMPDB = ON, ONLINE = OFF);

/* ============================
   REORGANIZE Draft (Top target)
   ============================ */
-- ALTER INDEX [IX_POS_Transactions_Card_VisaNumber] ON [dbo].[Transactions] REORGANIZE;
-- ALTER INDEX [IX_POS_Transactions_Search_VisaNumber] ON [dbo].[Transactions] REORGANIZE;
-- ALTER INDEX [IX_POS_KycAvailableCards_Transactions] ON [dbo].[Transactions] REORGANIZE;
-- ALTER INDEX [IX_POS_TransactionDetails_ItemSerial_Transaction] ON [dbo].[Transaction_Details] REORGANIZE;
-- ALTER INDEX [IX_POS_Transactions_Report_ServiceSearch] ON [dbo].[Transactions] REORGANIZE;
-- ALTER INDEX [IX_POS_Notes_Transaction_ID] ON [dbo].[Notes] REORGANIZE;
-- ALTER INDEX [IX_POS_TransactionDetails_StoreSerials_Report] ON [dbo].[Transaction_Details] REORGANIZE;
-- ALTER INDEX [IX_Notes_Header_NoteSerial] ON [dbo].[Notes] REORGANIZE;
-- ALTER INDEX [IX_Transactions__UserID] ON [dbo].[Transactions] REORGANIZE;

/* ==========================================
   Optional: generate dynamic maintenance SQL
   ========================================== */
-- ;WITH frag AS (
--   SELECT
--     s.name AS schema_name,
--     t.name AS table_name,
--     i.name AS index_name,
--     ips.page_count,
--     CAST(ips.avg_fragmentation_in_percent AS DECIMAL(10,2)) AS frag_pct
--   FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
--   JOIN sys.indexes i ON i.object_id=ips.object_id AND i.index_id=ips.index_id
--   JOIN sys.tables t ON t.object_id=i.object_id
--   JOIN sys.schemas s ON s.schema_id=t.schema_id
--   WHERE t.name IN ('DOUBLE_ENTREY_VOUCHERS','Transactions','Transaction_Details','Notes','Payments')
--     AND ips.page_count >= 1000
--     AND i.index_id > 0
-- )
-- SELECT
--   CASE
--     WHEN frag_pct >= 30 THEN 'ALTER INDEX [' + index_name + '] ON [' + schema_name + '].[' + table_name + '] REBUILD WITH (SORT_IN_TEMPDB = ON, ONLINE = OFF);'
--     WHEN frag_pct >= 10 THEN 'ALTER INDEX [' + index_name + '] ON [' + schema_name + '].[' + table_name + '] REORGANIZE;'
--   END AS maintenance_statement
-- FROM frag
-- WHERE frag_pct >= 10
-- ORDER BY frag_pct DESC, page_count DESC;

/* ============================
   Post-maintenance stats draft
   ============================ */
-- UPDATE STATISTICS [dbo].[Transactions] WITH FULLSCAN;
-- UPDATE STATISTICS [dbo].[Transaction_Details] WITH FULLSCAN;
-- UPDATE STATISTICS [dbo].[DOUBLE_ENTREY_VOUCHERS] WITH FULLSCAN;
-- UPDATE STATISTICS [dbo].[Notes] WITH FULLSCAN;
