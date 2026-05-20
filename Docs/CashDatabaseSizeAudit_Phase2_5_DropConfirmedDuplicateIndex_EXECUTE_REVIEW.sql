/*
Phase 2.5 - Execute Review Script
Target DB: Cash
Action: Drop ONE confirmed duplicate index only (if exists)
IMPORTANT: Review before execution
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
USE [Cash];

PRINT 'STEP 1: PRE-CHECK - Compare both index definitions on dbo.Notes';

;WITH idx AS (
    SELECT i.object_id,i.index_id,i.name AS index_name,i.type_desc,i.is_unique,i.fill_factor,i.has_filter,i.filter_definition,ds.name AS data_space_name,ds.type_desc AS data_space_type
    FROM sys.indexes i
    JOIN sys.tables t ON t.object_id=i.object_id
    JOIN sys.data_spaces ds ON ds.data_space_id=i.data_space_id
    WHERE t.name='Notes' AND i.name IN ('IX_Notes_Transaction_ID','IX_POS_Notes_Transaction_ID')
), cols AS (
    SELECT i.object_id,i.index_id,
      STUFF((SELECT ','+c.name FROM sys.index_columns ic2 JOIN sys.columns c ON c.object_id=ic2.object_id AND c.column_id=ic2.column_id WHERE ic2.object_id=i.object_id AND ic2.index_id=i.index_id AND ic2.key_ordinal>0 ORDER BY ic2.key_ordinal FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'') AS key_columns,
      ISNULL(STUFF((SELECT ','+c.name FROM sys.index_columns ic2 JOIN sys.columns c ON c.object_id=ic2.object_id AND c.column_id=ic2.column_id WHERE ic2.object_id=i.object_id AND ic2.index_id=i.index_id AND ic2.is_included_column=1 ORDER BY c.column_id FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,''),'') AS include_columns
    FROM idx i
), sz AS (
    SELECT i.object_id,i.index_id, CAST(SUM(a.used_pages)*8.0/1024 AS DECIMAL(18,2)) AS used_mb, SUM(a.used_pages) AS used_pages, MAX(p.data_compression_desc) AS data_compression_desc
    FROM idx i
    JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
    JOIN sys.allocation_units a ON a.container_id=p.partition_id
    GROUP BY i.object_id,i.index_id
)
SELECT 
    i.index_name,i.type_desc,i.is_unique,i.fill_factor,i.has_filter,i.filter_definition,
    i.data_space_name,i.data_space_type,
    s.data_compression_desc,
    c.key_columns,c.include_columns,
    s.used_mb,s.used_pages,
    ISNULL(us.user_seeks,0) AS user_seeks,
    ISNULL(us.user_scans,0) AS user_scans,
    ISNULL(us.user_lookups,0) AS user_lookups,
    ISNULL(us.user_updates,0) AS user_updates,
    us.last_user_seek,us.last_user_scan,us.last_user_lookup,us.last_user_update
FROM idx i
JOIN cols c ON c.object_id=i.object_id AND c.index_id=i.index_id
JOIN sz s ON s.object_id=i.object_id AND s.index_id=i.index_id
LEFT JOIN sys.dm_db_index_usage_stats us ON us.database_id=DB_ID() AND us.object_id=i.object_id AND us.index_id=i.index_id
ORDER BY i.index_name;

PRINT 'STEP 2: DROP confirmed duplicate index ONLY (IX_POS_Notes_Transaction_ID)';
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID('dbo.Notes')
      AND i.name = 'IX_POS_Notes_Transaction_ID'
)
BEGIN
    DROP INDEX [IX_POS_Notes_Transaction_ID] ON [dbo].[Notes];
    PRINT 'Dropped: dbo.Notes.IX_POS_Notes_Transaction_ID';
END
ELSE
BEGIN
    PRINT 'Index IX_POS_Notes_Transaction_ID not found; nothing dropped.';
END

PRINT 'STEP 3: POST-CHECK - verify remaining/removed index status';
SELECT
    i.name AS index_name,
    i.index_id,
    i.type_desc
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.Notes')
  AND i.name IN ('IX_Notes_Transaction_ID','IX_POS_Notes_Transaction_ID')
ORDER BY i.name;
