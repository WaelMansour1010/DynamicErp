/*
Cash Phase 6 - Backup/Temp/Old/Log/Staging Tables Audit (SELECT ONLY)
SQL Server 2012 compatible
No data changes.
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

;WITH TargetTables AS (
    SELECT
        t.object_id,
        s.name AS schema_name,
        t.name AS table_name,
        t.create_date,
        t.modify_date
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id=t.schema_id
    WHERE LOWER(t.name) LIKE '%[_]bak%'
       OR LOWER(t.name) LIKE '%backup%'
       OR LOWER(t.name) LIKE '%old%'
       OR LOWER(t.name) LIKE '%temp%'
       OR LOWER(t.name) LIKE '%tmp%'
       OR LOWER(t.name) LIKE '%log%'
       OR LOWER(t.name) LIKE '%audit%'
       OR LOWER(t.name) LIKE '%staging%'
       OR LOWER(t.name) LIKE '%import%'
       OR LOWER(t.name) LIKE '%test%'
), SpaceAgg AS (
    SELECT
        tt.object_id,
        SUM(CASE WHEN i.index_id IN (0,1) THEN p.rows ELSE 0 END) AS row_count,
        SUM(CASE WHEN i.index_id IN (0,1) THEN a.data_pages ELSE 0 END) AS data_pages,
        SUM(CASE WHEN i.index_id > 1 THEN a.used_pages ELSE 0 END) AS index_used_pages,
        SUM(a.used_pages) AS used_pages
    FROM TargetTables tt
    JOIN sys.indexes i ON i.object_id=tt.object_id
    JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
    JOIN sys.allocation_units a ON a.container_id=p.partition_id
    GROUP BY tt.object_id
), FkAgg AS (
    SELECT
        tt.object_id,
        SUM(CASE WHEN fk.parent_object_id = tt.object_id THEN 1 ELSE 0 END) AS fk_as_child_count,
        SUM(CASE WHEN fk.referenced_object_id = tt.object_id THEN 1 ELSE 0 END) AS fk_as_parent_count
    FROM TargetTables tt
    LEFT JOIN sys.foreign_keys fk
        ON fk.parent_object_id = tt.object_id
        OR fk.referenced_object_id = tt.object_id
    GROUP BY tt.object_id
), RefAgg AS (
    SELECT
        tt.object_id,
        COUNT(DISTINCT CASE WHEN o.type='P' THEN o.object_id END) AS dependent_procedure_count,
        COUNT(DISTINCT CASE WHEN o.type='V' THEN o.object_id END) AS dependent_view_count,
        COUNT(DISTINCT CASE WHEN o.type IN ('FN','IF','TF') THEN o.object_id END) AS dependent_function_count,
        COUNT(DISTINCT CASE WHEN o.type='TR' THEN o.object_id END) AS dependent_trigger_count
    FROM TargetTables tt
    LEFT JOIN sys.sql_expression_dependencies d ON d.referenced_id = tt.object_id
    LEFT JOIN sys.objects o ON o.object_id = d.referencing_id
    GROUP BY tt.object_id
)
SELECT
    tt.schema_name,
    tt.table_name,
    ISNULL(sa.row_count,0) AS row_count,
    CAST(ISNULL(sa.data_pages,0)*8.0/1024 AS DECIMAL(18,2)) AS data_mb,
    CAST(ISNULL(sa.index_used_pages,0)*8.0/1024 AS DECIMAL(18,2)) AS index_mb,
    CAST(ISNULL(sa.used_pages,0)*8.0/1024 AS DECIMAL(18,2)) AS total_used_mb,
    tt.create_date,
    tt.modify_date,
    ISNULL(fk.fk_as_child_count,0) AS fk_as_child_count,
    ISNULL(fk.fk_as_parent_count,0) AS fk_as_parent_count,
    ISNULL(rf.dependent_procedure_count,0) AS dependent_procedure_count,
    ISNULL(rf.dependent_view_count,0) AS dependent_view_count,
    ISNULL(rf.dependent_function_count,0) AS dependent_function_count,
    ISNULL(rf.dependent_trigger_count,0) AS dependent_trigger_count
FROM TargetTables tt
LEFT JOIN SpaceAgg sa ON sa.object_id=tt.object_id
LEFT JOIN FkAgg fk ON fk.object_id=tt.object_id
LEFT JOIN RefAgg rf ON rf.object_id=tt.object_id
ORDER BY total_used_mb DESC, tt.schema_name, tt.table_name;

-- Optional detailed dependency list
SELECT
    s.name AS table_schema,
    t.name AS table_name,
    o.type_desc AS referencing_object_type,
    OBJECT_SCHEMA_NAME(o.object_id) AS referencing_schema,
    o.name AS referencing_object_name
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id=t.schema_id
LEFT JOIN sys.sql_expression_dependencies d ON d.referenced_id=t.object_id
LEFT JOIN sys.objects o ON o.object_id=d.referencing_id
WHERE LOWER(t.name) LIKE '%[_]bak%'
   OR LOWER(t.name) LIKE '%backup%'
   OR LOWER(t.name) LIKE '%old%'
   OR LOWER(t.name) LIKE '%temp%'
   OR LOWER(t.name) LIKE '%tmp%'
   OR LOWER(t.name) LIKE '%log%'
   OR LOWER(t.name) LIKE '%audit%'
   OR LOWER(t.name) LIKE '%staging%'
   OR LOWER(t.name) LIKE '%import%'
   OR LOWER(t.name) LIKE '%test%'
ORDER BY table_name, referencing_object_type, referencing_schema, referencing_object_name;
