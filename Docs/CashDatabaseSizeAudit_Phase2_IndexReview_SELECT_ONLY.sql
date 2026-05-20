/*
Cash Database Size Audit - Phase 2 (SELECT ONLY)
Date: 2026-05-20
Scope: Index review for BIG tables in Cash
Compatibility: SQL Server 2012+
IMPORTANT: READ-ONLY. Do not modify objects.
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

PRINT 'SECTION A - TARGET TABLE PRESENCE';
SELECT s.name AS schema_name, t.name AS table_name
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.name IN ('DOUBLE_ENTREY_VOUCHERS','Transactions','Transaction_Details','Notes','Payments')
ORDER BY t.name;

PRINT 'SECTION B - PAYMENT-LIKE TABLES (if Payments not found)';
SELECT s.name AS schema_name, t.name AS table_name
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id=t.schema_id
WHERE t.name LIKE '%Payment%'
ORDER BY t.name;

PRINT 'SECTION C - INDEX INVENTORY + SIZE + USAGE (TARGET TABLES)';
;WITH idx AS (
    SELECT
        s.name AS schema_name,
        t.name AS table_name,
        i.object_id,
        i.index_id,
        i.name AS index_name,
        i.type_desc,
        i.is_primary_key,
        i.is_unique,
        i.is_unique_constraint,
        i.has_filter,
        i.filter_definition,
        SUM(a.used_pages) AS used_pages,
        SUM(a.total_pages) AS total_pages
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id=t.schema_id
    JOIN sys.indexes i ON i.object_id=t.object_id AND i.index_id>0
    JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
    JOIN sys.allocation_units a ON a.container_id=p.partition_id
    WHERE t.name IN ('DOUBLE_ENTREY_VOUCHERS','Transactions','Transaction_Details','Notes','Payments')
    GROUP BY s.name,t.name,i.object_id,i.index_id,i.name,i.type_desc,i.is_primary_key,i.is_unique,i.is_unique_constraint,i.has_filter,i.filter_definition
), cols AS (
    SELECT
        ic.object_id,
        ic.index_id,
        STUFF((
            SELECT ',' + c.name
            FROM sys.index_columns ic2
            JOIN sys.columns c ON c.object_id=ic2.object_id AND c.column_id=ic2.column_id
            WHERE ic2.object_id=ic.object_id AND ic2.index_id=ic.index_id AND ic2.key_ordinal>0
            ORDER BY ic2.key_ordinal
            FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,'') AS key_cols,
        STUFF((
            SELECT ',' + c.name
            FROM sys.index_columns ic2
            JOIN sys.columns c ON c.object_id=ic2.object_id AND c.column_id=ic2.column_id
            WHERE ic2.object_id=ic.object_id AND ic2.index_id=ic.index_id AND ic2.is_included_column=1
            ORDER BY c.column_id
            FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,'') AS include_cols
    FROM sys.index_columns ic
    GROUP BY ic.object_id, ic.index_id
)
SELECT
    idx.schema_name,
    idx.table_name,
    idx.index_name,
    idx.type_desc,
    CAST(idx.used_pages*8.0/1024 AS DECIMAL(18,2)) AS used_mb,
    CAST(idx.total_pages*8.0/1024 AS DECIMAL(18,2)) AS reserved_mb,
    idx.is_primary_key,
    idx.is_unique,
    idx.is_unique_constraint,
    idx.has_filter,
    idx.filter_definition,
    ISNULL(us.user_seeks,0) AS user_seeks,
    ISNULL(us.user_scans,0) AS user_scans,
    ISNULL(us.user_lookups,0) AS user_lookups,
    ISNULL(us.user_updates,0) AS user_updates,
    us.last_user_seek,
    us.last_user_scan,
    us.last_user_lookup,
    us.last_user_update,
    cols.key_cols,
    cols.include_cols
FROM idx
LEFT JOIN sys.dm_db_index_usage_stats us ON us.database_id=DB_ID() AND us.object_id=idx.object_id AND us.index_id=idx.index_id
LEFT JOIN cols ON cols.object_id=idx.object_id AND cols.index_id=idx.index_id
ORDER BY idx.table_name, idx.used_pages DESC;

PRINT 'SECTION D - EXACT DUPLICATE INDEX SIGNATURES';
;WITH sig AS (
    SELECT
        t.name AS table_name,
        i.object_id,
        i.index_id,
        i.name AS index_name,
        i.has_filter,
        ISNULL(i.filter_definition,'') AS filter_definition,
        STUFF((SELECT ',' + c.name
               FROM sys.index_columns ic2
               JOIN sys.columns c ON c.object_id=ic2.object_id AND c.column_id=ic2.column_id
               WHERE ic2.object_id=i.object_id AND ic2.index_id=i.index_id AND ic2.key_ordinal>0
               ORDER BY ic2.key_ordinal FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,'') AS key_cols,
        ISNULL(STUFF((SELECT ',' + c.name
                      FROM sys.index_columns ic2
                      JOIN sys.columns c ON c.object_id=ic2.object_id AND c.column_id=ic2.column_id
                      WHERE ic2.object_id=i.object_id AND ic2.index_id=i.index_id AND ic2.is_included_column=1
                      ORDER BY c.column_id FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,''),'') AS include_cols
    FROM sys.tables t
    JOIN sys.indexes i ON i.object_id=t.object_id AND i.index_id>0
    WHERE t.name IN ('DOUBLE_ENTREY_VOUCHERS','Transactions','Transaction_Details','Notes','Payments')
)
SELECT
    s1.table_name,
    s1.index_name AS index_1,
    s2.index_name AS index_2,
    s1.key_cols,
    s1.include_cols,
    s1.has_filter,
    s1.filter_definition
FROM sig s1
JOIN sig s2 ON s1.object_id=s2.object_id AND s1.index_id<s2.index_id
WHERE s1.key_cols=s2.key_cols
  AND s1.include_cols=s2.include_cols
  AND s1.has_filter=s2.has_filter
  AND s1.filter_definition=s2.filter_definition
ORDER BY s1.table_name, s1.index_name;

PRINT 'SECTION E - OVERLAPPING INDEXES (PREFIX MATCH)';
;WITH x AS (
    SELECT
        t.name AS table_name,
        i.object_id,
        i.index_id,
        i.name AS index_name,
        i.is_primary_key,
        i.has_filter,
        SUM(a.used_pages) AS used_pages,
        STUFF((SELECT ',' + c.name
               FROM sys.index_columns ic2
               JOIN sys.columns c ON c.object_id=ic2.object_id AND c.column_id=ic2.column_id
               WHERE ic2.object_id=i.object_id AND ic2.index_id=i.index_id AND ic2.key_ordinal>0
               ORDER BY ic2.key_ordinal FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,'') AS key_cols,
        ISNULL(STUFF((SELECT ',' + c.name
                      FROM sys.index_columns ic2
                      JOIN sys.columns c ON c.object_id=ic2.object_id AND c.column_id=ic2.column_id
                      WHERE ic2.object_id=i.object_id AND ic2.index_id=i.index_id AND ic2.is_included_column=1
                      ORDER BY c.column_id FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,''),'') AS include_cols
    FROM sys.tables t
    JOIN sys.indexes i ON i.object_id=t.object_id AND i.index_id>0
    JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
    JOIN sys.allocation_units a ON a.container_id=p.partition_id
    WHERE t.name IN ('DOUBLE_ENTREY_VOUCHERS','Transactions','Transaction_Details','Notes','Payments')
    GROUP BY t.name, i.object_id, i.index_id, i.name, i.is_primary_key, i.has_filter
)
SELECT
    a.table_name,
    a.index_name AS wider_index,
    b.index_name AS narrower_index,
    CAST(a.used_pages*8.0/1024 AS DECIMAL(18,2)) AS wider_mb,
    CAST(b.used_pages*8.0/1024 AS DECIMAL(18,2)) AS narrower_mb,
    a.key_cols AS wider_keys,
    b.key_cols AS narrower_keys,
    a.include_cols AS wider_includes,
    b.include_cols AS narrower_includes
FROM x a
JOIN x b ON a.object_id=b.object_id AND a.index_id<>b.index_id
WHERE a.is_primary_key=0 AND b.is_primary_key=0
  AND a.has_filter=0 AND b.has_filter=0
  AND (a.key_cols=b.key_cols OR a.key_cols LIKE b.key_cols + ',%')
ORDER BY a.table_name, narrower_mb DESC, wider_mb DESC;

PRINT 'SECTION F - FRAGMENTATION (TARGET TABLES)';
SELECT
    t.name AS table_name,
    i.name AS index_name,
    ips.page_count,
    CAST(ips.avg_fragmentation_in_percent AS DECIMAL(10,2)) AS frag_pct,
    CASE
        WHEN ips.page_count < 1000 THEN 'NO_ACTION'
        WHEN ips.avg_fragmentation_in_percent >= 30 THEN 'REBUILD'
        WHEN ips.avg_fragmentation_in_percent >= 10 THEN 'REORGANIZE'
        ELSE 'NO_ACTION'
    END AS recommendation
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
JOIN sys.indexes i ON i.object_id=ips.object_id AND i.index_id=ips.index_id
JOIN sys.tables t ON t.object_id=i.object_id
WHERE t.name IN ('DOUBLE_ENTREY_VOUCHERS','Transactions','Transaction_Details','Notes','Payments')
  AND i.index_id > 0
ORDER BY frag_pct DESC, page_count DESC;

PRINT 'SECTION G - MODULES (SP/FN/VIEW) DEPENDENT ON TARGET TABLES';
SELECT DISTINCT
    o.type_desc AS object_type,
    s.name AS schema_name,
    o.name AS object_name
FROM sys.sql_expression_dependencies d
JOIN sys.objects o ON o.object_id=d.referencing_id
JOIN sys.schemas s ON s.schema_id=o.schema_id
WHERE d.referenced_id IN (
    OBJECT_ID('dbo.DOUBLE_ENTREY_VOUCHERS'),
    OBJECT_ID('dbo.Transactions'),
    OBJECT_ID('dbo.Transaction_Details'),
    OBJECT_ID('dbo.Notes'),
    OBJECT_ID('dbo.Payments')
)
AND o.type IN ('P','V','FN','IF','TF')
ORDER BY o.type_desc, s.name, o.name;

PRINT 'SECTION H - MODULES CONTAINING INDEX HINT TOKENS';
SELECT
    o.type_desc,
    s.name AS schema_name,
    o.name AS object_name
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id=m.object_id
JOIN sys.schemas s ON s.schema_id=o.schema_id
WHERE (m.definition LIKE '%DOUBLE_ENTREY_VOUCHERS%' OR m.definition LIKE '%Transactions%' OR m.definition LIKE '%Transaction_Details%' OR m.definition LIKE '%Notes%' OR m.definition LIKE '%Payments%')
  AND m.definition LIKE '%INDEX(%'
ORDER BY o.type_desc, s.name, o.name;
