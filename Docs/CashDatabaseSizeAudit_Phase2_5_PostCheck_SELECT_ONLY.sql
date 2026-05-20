/*
Phase 2.5 - Post Check (SELECT ONLY)
Run after approved execution of duplicate-index drop
*/
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
USE [Cash];

PRINT 'CHECK 1: Remaining vs removed index';
SELECT i.name AS index_name, i.index_id, i.type_desc
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.Notes')
  AND i.name IN ('IX_Notes_Transaction_ID','IX_POS_Notes_Transaction_ID')
ORDER BY i.name;

PRINT 'CHECK 2: Notes table size after change';
;WITH t AS (
    SELECT
        SUM(a.total_pages) AS total_pages,
        SUM(a.used_pages) AS used_pages,
        SUM(a.data_pages) AS data_pages
    FROM sys.tables tb
    JOIN sys.indexes i ON i.object_id=tb.object_id
    JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
    JOIN sys.allocation_units a ON a.container_id=p.partition_id
    WHERE tb.object_id = OBJECT_ID('dbo.Notes')
)
SELECT
    CAST(total_pages*8.0/1024 AS DECIMAL(18,2)) AS reserved_mb,
    CAST(used_pages*8.0/1024 AS DECIMAL(18,2)) AS used_mb,
    CAST(data_pages*8.0/1024 AS DECIMAL(18,2)) AS data_mb,
    CAST((used_pages-data_pages)*8.0/1024 AS DECIMAL(18,2)) AS index_and_other_used_mb
FROM t;

PRINT 'CHECK 3: Index-level size on dbo.Notes';
SELECT
    i.name AS index_name,
    CAST(SUM(a.used_pages)*8.0/1024 AS DECIMAL(18,2)) AS used_mb,
    SUM(a.used_pages) AS used_pages
FROM sys.indexes i
JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
JOIN sys.allocation_units a ON a.container_id=p.partition_id
WHERE i.object_id=OBJECT_ID('dbo.Notes')
GROUP BY i.name
ORDER BY used_mb DESC;

PRINT 'CHECK 4: Estimated impact proxy - modules referencing Notes + Transaction_ID';
SELECT DISTINCT
    o.type_desc AS object_type,
    s.name AS schema_name,
    o.name AS object_name
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id=m.object_id
JOIN sys.schemas s ON s.schema_id=o.schema_id
WHERE m.definition LIKE '%Notes%'
  AND m.definition LIKE '%Transaction_ID%'
ORDER BY o.type_desc, s.name, o.name;

PRINT 'CHECK 5: Missing index DMVs related to dbo.Notes + Transaction_ID';
SELECT
    d.statement AS table_statement,
    d.equality_columns,
    d.inequality_columns,
    d.included_columns,
    s.user_seeks,
    s.user_scans,
    CAST(s.avg_total_user_cost AS DECIMAL(18,2)) AS avg_total_user_cost,
    CAST(s.avg_user_impact AS DECIMAL(18,2)) AS avg_user_impact
FROM sys.dm_db_missing_index_details d
JOIN sys.dm_db_missing_index_groups g ON g.index_handle=d.index_handle
JOIN sys.dm_db_missing_index_group_stats s ON s.group_handle=g.index_group_handle
WHERE d.database_id = DB_ID()
  AND d.statement LIKE '%[Notes]%'
  AND (ISNULL(d.equality_columns,'') LIKE '%Transaction_ID%' OR ISNULL(d.inequality_columns,'') LIKE '%Transaction_ID%' OR ISNULL(d.included_columns,'') LIKE '%Transaction_ID%')
ORDER BY s.user_seeks DESC, s.user_scans DESC;
