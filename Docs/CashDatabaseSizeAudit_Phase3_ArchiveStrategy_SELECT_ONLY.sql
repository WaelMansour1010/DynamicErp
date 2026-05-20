/*
Cash Phase 3 - Archive Strategy Diagnostics (SELECT ONLY)
Date: 2026-05-20
Compatibility: SQL Server 2012+
No DELETE / No INSERT / No CREATE DATABASE
*/
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

USE [Cash];

PRINT 'A) TARGET TABLE SIZE OVERVIEW';
SELECT t.name AS table_name,
       SUM(p.rows) AS approx_rows,
       CAST(SUM(CASE WHEN i.index_id IN (0,1) THEN a.data_pages ELSE 0 END)*8.0/1024 AS DECIMAL(18,2)) AS data_mb,
       CAST(SUM(CASE WHEN i.index_id > 1 THEN a.used_pages ELSE 0 END)*8.0/1024 AS DECIMAL(18,2)) AS index_mb,
       CAST(SUM(a.used_pages)*8.0/1024 AS DECIMAL(18,2)) AS used_mb
FROM sys.tables t
JOIN sys.indexes i ON i.object_id=t.object_id
JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
JOIN sys.allocation_units a ON a.container_id=p.partition_id
WHERE t.name IN ('DOUBLE_ENTREY_VOUCHERS','Transactions','Transaction_Details','Notes')
GROUP BY t.name
ORDER BY used_mb DESC;

PRINT 'B) DATE COLUMNS ON TARGET TABLES';
SELECT t.name AS table_name, c.name AS date_column
FROM sys.tables t
JOIN sys.columns c ON c.object_id=t.object_id
WHERE t.name IN ('DOUBLE_ENTREY_VOUCHERS','Transactions','Transaction_Details','Notes')
  AND c.system_type_id IN (40,42,43,58,61)
ORDER BY t.name,c.column_id;

PRINT 'C1) TRANSACTIONS BY YEAR-MONTH';
SELECT YEAR(Transaction_Date) AS [year], MONTH(Transaction_Date) AS [month], COUNT_BIG(*) AS row_count
FROM dbo.Transactions
WHERE Transaction_Date IS NOT NULL
GROUP BY YEAR(Transaction_Date), MONTH(Transaction_Date)
ORDER BY [year],[month];

PRINT 'C2) TRANSACTION_DETAILS BY PARENT TRANSACTION YEAR-MONTH';
SELECT YEAR(t.Transaction_Date) AS [year], MONTH(t.Transaction_Date) AS [month], COUNT_BIG(*) AS row_count
FROM dbo.Transaction_Details td
JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
WHERE t.Transaction_Date IS NOT NULL
GROUP BY YEAR(t.Transaction_Date), MONTH(t.Transaction_Date)
ORDER BY [year],[month];

PRINT 'C3) DOUBLE_ENTREY_VOUCHERS BY YEAR-MONTH (RecordDate)';
SELECT YEAR(RecordDate) AS [year], MONTH(RecordDate) AS [month], COUNT_BIG(*) AS row_count
FROM dbo.DOUBLE_ENTREY_VOUCHERS
WHERE RecordDate IS NOT NULL
GROUP BY YEAR(RecordDate), MONTH(RecordDate)
ORDER BY [year],[month];

PRINT 'C4) NOTES BY YEAR-MONTH (NoteDate)';
SELECT YEAR(NoteDate) AS [year], MONTH(NoteDate) AS [month], COUNT_BIG(*) AS row_count
FROM dbo.Notes
WHERE NoteDate IS NOT NULL
GROUP BY YEAR(NoteDate), MONTH(NoteDate)
ORDER BY [year],[month];

PRINT 'D) CUTOFF ELIGIBILITY (BEFORE 2023 / BEFORE 2024)';
SELECT 'Transactions' AS table_name,
SUM(CASE WHEN Transaction_Date < '2023-01-01' THEN 1 ELSE 0 END) AS rows_before_2023,
SUM(CASE WHEN Transaction_Date < '2024-01-01' THEN 1 ELSE 0 END) AS rows_before_2024,
COUNT_BIG(*) AS total_rows
FROM dbo.Transactions
UNION ALL
SELECT 'Transaction_Details_by_parent_date',
SUM(CASE WHEN t.Transaction_Date < '2023-01-01' THEN 1 ELSE 0 END),
SUM(CASE WHEN t.Transaction_Date < '2024-01-01' THEN 1 ELSE 0 END),
COUNT_BIG(*)
FROM dbo.Transaction_Details td
JOIN dbo.Transactions t ON t.Transaction_ID=td.Transaction_ID
UNION ALL
SELECT 'DOUBLE_ENTREY_VOUCHERS',
SUM(CASE WHEN RecordDate < '2023-01-01' THEN 1 ELSE 0 END),
SUM(CASE WHEN RecordDate < '2024-01-01' THEN 1 ELSE 0 END),
COUNT_BIG(*)
FROM dbo.DOUBLE_ENTREY_VOUCHERS
UNION ALL
SELECT 'Notes',
SUM(CASE WHEN NoteDate < '2023-01-01' THEN 1 ELSE 0 END),
SUM(CASE WHEN NoteDate < '2024-01-01' THEN 1 ELSE 0 END),
COUNT_BIG(*)
FROM dbo.Notes;

PRINT 'E) RELATIONSHIP/INTEGRITY CHECKS (ORPHANS)';
SELECT 
 (SELECT COUNT_BIG(*) FROM dbo.Transaction_Details td LEFT JOIN dbo.Transactions t ON t.Transaction_ID=td.Transaction_ID WHERE t.Transaction_ID IS NULL) AS td_orphan_vs_transactions,
 (SELECT COUNT_BIG(*) FROM dbo.DOUBLE_ENTREY_VOUCHERS d LEFT JOIN dbo.Transactions t ON t.Transaction_ID=d.Transaction_ID WHERE d.Transaction_ID IS NOT NULL AND t.Transaction_ID IS NULL) AS dev_orphan_vs_transactions,
 (SELECT COUNT_BIG(*) FROM dbo.DOUBLE_ENTREY_VOUCHERS d LEFT JOIN dbo.Notes n ON n.NoteID=d.Notes_ID WHERE d.Notes_ID IS NOT NULL AND n.NoteID IS NULL) AS dev_orphan_vs_notes,
 (SELECT COUNT_BIG(*) FROM dbo.Notes n LEFT JOIN dbo.Transactions t ON t.Transaction_ID=n.Transaction_ID WHERE n.Transaction_ID IS NOT NULL AND t.Transaction_ID IS NULL) AS notes_orphan_vs_transactions;

PRINT 'F) FK MAP TOUCHING TARGET TABLES';
SELECT fk.name AS fk_name,
       OBJECT_NAME(fk.parent_object_id) AS child_table,
       pc.name AS child_column,
       OBJECT_NAME(fk.referenced_object_id) AS parent_table,
       rc.name AS parent_column
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id
JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id
JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id
WHERE fk.parent_object_id IN (OBJECT_ID('dbo.Transactions'),OBJECT_ID('dbo.Transaction_Details'),OBJECT_ID('dbo.DOUBLE_ENTREY_VOUCHERS'),OBJECT_ID('dbo.Notes'))
   OR fk.referenced_object_id IN (OBJECT_ID('dbo.Transactions'),OBJECT_ID('dbo.Transaction_Details'),OBJECT_ID('dbo.DOUBLE_ENTREY_VOUCHERS'),OBJECT_ID('dbo.Notes'))
ORDER BY child_table,parent_table;

PRINT 'G) OBJECTS REFERENCING TARGET TABLES (SP/FN/VIEW/TRIGGER)';
SELECT o.type_desc AS object_type, s.name AS schema_name, o.name AS object_name
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id=m.object_id
JOIN sys.schemas s ON s.schema_id=o.schema_id
WHERE (m.definition LIKE '%DOUBLE_ENTREY_VOUCHERS%' OR m.definition LIKE '%Transactions%' OR m.definition LIKE '%Transaction_Details%' OR m.definition LIKE '%Notes%')
ORDER BY o.type_desc, s.name, o.name;

PRINT 'H) ESTIMATED DATA MB ELIGIBLE BEFORE 2024 (PROPORTIONAL ESTIMATE)';
;WITH sizes AS (
 SELECT t.name table_name, CAST(SUM(CASE WHEN i.index_id IN (0,1) THEN a.data_pages ELSE 0 END)*8.0/1024 AS DECIMAL(18,2)) AS data_mb
 FROM sys.tables t
 JOIN sys.indexes i ON i.object_id=t.object_id
 JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
 JOIN sys.allocation_units a ON a.container_id=p.partition_id
 WHERE t.name IN ('Transactions','Transaction_Details','DOUBLE_ENTREY_VOUCHERS','Notes')
 GROUP BY t.name
), counts AS (
 SELECT 'Transactions' table_name, COUNT_BIG(*) total_rows, SUM(CASE WHEN Transaction_Date < '2024-01-01' THEN 1 ELSE 0 END) b2024 FROM dbo.Transactions
 UNION ALL
 SELECT 'Transaction_Details', COUNT_BIG(*), SUM(CASE WHEN t.Transaction_Date < '2024-01-01' THEN 1 ELSE 0 END) FROM dbo.Transaction_Details td JOIN dbo.Transactions t ON t.Transaction_ID=td.Transaction_ID
 UNION ALL
 SELECT 'DOUBLE_ENTREY_VOUCHERS', COUNT_BIG(*), SUM(CASE WHEN RecordDate < '2024-01-01' THEN 1 ELSE 0 END) FROM dbo.DOUBLE_ENTREY_VOUCHERS
 UNION ALL
 SELECT 'Notes', COUNT_BIG(*), SUM(CASE WHEN NoteDate < '2024-01-01' THEN 1 ELSE 0 END) FROM dbo.Notes
)
SELECT c.table_name,c.total_rows,c.b2024 AS rows_before_2024,
CAST((c.b2024*100.0)/NULLIF(c.total_rows,0) AS DECIMAL(10,2)) AS pct_before_2024,
s.data_mb,
CAST(s.data_mb * (c.b2024*1.0/NULLIF(c.total_rows,0)) AS DECIMAL(18,2)) AS est_data_mb_before_2024
FROM counts c JOIN sizes s ON s.table_name=c.table_name
ORDER BY est_data_mb_before_2024 DESC;
