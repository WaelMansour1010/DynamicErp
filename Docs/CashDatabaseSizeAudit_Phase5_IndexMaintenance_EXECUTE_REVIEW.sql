/*
Cash Phase 5 - Large Index Maintenance Execute Review (DO NOT RUN BEFORE APPROVAL)
SQL Server 2012 compatible
Scope: DOUBLE_ENTREY_VOUCHERS, Transactions, Transaction_Details, Notes
Rules:
- No DROP INDEX
- No data modification
- Maintenance in batches / quiet window
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

USE [Cash];

PRINT 'STEP 1: Preview fragmentation & recommendation';
SELECT 
 t.name AS table_name,
 i.name AS index_name,
 ips.page_count,
 CAST(ips.avg_fragmentation_in_percent AS DECIMAL(10,2)) AS frag_pct,
 CAST(SUM(a.used_pages)*8.0/1024 AS DECIMAL(18,2)) AS index_used_mb,
 CASE WHEN ips.page_count < 1000 THEN 'NO_ACTION'
      WHEN ips.avg_fragmentation_in_percent >= 30 THEN 'REBUILD'
      WHEN ips.avg_fragmentation_in_percent >= 10 THEN 'REORGANIZE'
      ELSE 'NO_ACTION' END AS recommendation
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
JOIN sys.indexes i ON i.object_id=ips.object_id AND i.index_id=ips.index_id
JOIN sys.tables t ON t.object_id=i.object_id
JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
JOIN sys.allocation_units a ON a.container_id=p.partition_id
WHERE t.name IN ('DOUBLE_ENTREY_VOUCHERS','Transactions','Transaction_Details','Notes')
  AND i.index_id > 0
GROUP BY t.name,i.name,ips.page_count,ips.avg_fragmentation_in_percent
ORDER BY frag_pct DESC, index_used_mb DESC;

PRINT 'STEP 2: REORGANIZE batch (10%-29.99%, page_count>=1000)';
-- ALTER INDEX [IX_POS_Transactions_Card_VisaNumber] ON [dbo].[Transactions] REORGANIZE;
-- ALTER INDEX [IX_POS_Transactions_Search_VisaNumber] ON [dbo].[Transactions] REORGANIZE;
-- ALTER INDEX [IX_POS_KycAvailableCards_Transactions] ON [dbo].[Transactions] REORGANIZE;
-- ALTER INDEX [IX_POS_TransactionDetails_ItemSerial_Transaction] ON [dbo].[Transaction_Details] REORGANIZE;
-- ALTER INDEX [IX_POS_Transactions_Report_ServiceSearch] ON [dbo].[Transactions] REORGANIZE;
-- ALTER INDEX [IX_POS_TransactionDetails_StoreSerials_Report] ON [dbo].[Transaction_Details] REORGANIZE;
-- ALTER INDEX [IX_Notes_Header_NoteSerial] ON [dbo].[Notes] REORGANIZE;
-- ALTER INDEX [IX_Transactions__UserID] ON [dbo].[Transactions] REORGANIZE;

PRINT 'STEP 3: REBUILD batch (>=30%, page_count>=1000)';
-- ALTER INDEX [IX_Transaction_Details] ON [dbo].[Transaction_Details]
-- REBUILD WITH (SORT_IN_TEMPDB = ON, ONLINE = OFF);

PRINT 'STEP 4: Update statistics after maintenance';
-- UPDATE STATISTICS [dbo].[Transactions] WITH FULLSCAN;
-- UPDATE STATISTICS [dbo].[Transaction_Details] WITH FULLSCAN;
-- UPDATE STATISTICS [dbo].[DOUBLE_ENTREY_VOUCHERS] WITH FULLSCAN;
-- UPDATE STATISTICS [dbo].[Notes] WITH FULLSCAN;

PRINT 'STEP 5: Post-maintenance validation (queries)';
-- Re-run fragmentation select
-- Validate top report/search procedures timing
