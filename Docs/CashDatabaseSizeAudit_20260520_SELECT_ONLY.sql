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

PRINT 'SECTION 0 - DATABASE FILES';
SELECT 
    DB_NAME() AS database_name,
    mf.name AS logical_name,
    mf.type_desc,
    mf.physical_name,
    CAST(mf.size/128.0 AS DECIMAL(18,2)) AS size_mb,
    CAST(FILEPROPERTY(mf.name,'SpaceUsed')/128.0 AS DECIMAL(18,2)) AS used_mb,
    CAST((mf.size - FILEPROPERTY(mf.name,'SpaceUsed'))/128.0 AS DECIMAL(18,2)) AS free_mb
FROM sys.database_files mf
ORDER BY mf.type_desc, mf.name;

PRINT 'SECTION 1 - TABLE SIZE TOP 30';
;WITH TableSpace AS (
    SELECT 
        s.name AS schema_name,
        t.name AS table_name,
        SUM(CASE WHEN i.index_id IN (0,1) THEN p.rows ELSE 0 END) AS row_count,
        SUM(a.total_pages) AS total_pages,
        SUM(a.used_pages) AS used_pages,
        SUM(CASE WHEN i.index_id IN (0,1) THEN a.data_pages ELSE 0 END) AS data_pages,
        SUM(CASE WHEN i.index_id > 1 THEN a.used_pages ELSE 0 END) AS index_used_pages
    FROM sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    INNER JOIN sys.indexes i ON i.object_id = t.object_id
    INNER JOIN sys.partitions p ON p.object_id = i.object_id AND p.index_id = i.index_id
    INNER JOIN sys.allocation_units a ON a.container_id = p.partition_id
    WHERE t.is_ms_shipped = 0
    GROUP BY s.name, t.name
)
SELECT TOP (30)
    schema_name,
    table_name,
    row_count,
    CAST(data_pages*8.0/1024 AS DECIMAL(18,2)) AS data_mb,
    CAST(index_used_pages*8.0/1024 AS DECIMAL(18,2)) AS index_mb,
    CAST((used_pages-data_pages-index_used_pages)*8.0/1024 AS DECIMAL(18,2)) AS lob_or_other_mb,
    CAST(used_pages*8.0/1024 AS DECIMAL(18,2)) AS used_mb,
    CAST((total_pages-used_pages)*8.0/1024 AS DECIMAL(18,2)) AS unused_mb,
    CAST(total_pages*8.0/1024 AS DECIMAL(18,2)) AS reserved_mb,
    CASE WHEN data_pages = 0 THEN NULL ELSE CAST(index_used_pages*1.0/data_pages AS DECIMAL(18,2)) END AS index_to_data_ratio
FROM TableSpace
ORDER BY total_pages DESC;

PRINT 'SECTION 2 - INDEX SIZE AND USAGE TOP 100';
;WITH IndexStats AS (
    SELECT 
        t.object_id,
        s.name AS schema_name,
        t.name AS table_name,
        i.name AS index_name,
        i.index_id,
        i.type_desc,
        SUM(a.total_pages) AS total_pages,
        SUM(a.used_pages) AS used_pages,
        SUM(p.rows) AS row_count
    FROM sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    INNER JOIN sys.indexes i ON i.object_id = t.object_id
    INNER JOIN sys.partitions p ON p.object_id = i.object_id AND p.index_id = i.index_id
    INNER JOIN sys.allocation_units a ON a.container_id = p.partition_id
    WHERE t.is_ms_shipped = 0
      AND i.index_id > 0
    GROUP BY t.object_id, s.name, t.name, i.name, i.index_id, i.type_desc
), UsageStats AS (
    SELECT object_id, index_id, user_seeks, user_scans, user_lookups, user_updates
    FROM sys.dm_db_index_usage_stats
    WHERE database_id = DB_ID()
), IndexCols AS (
    SELECT 
        ic.object_id,
        ic.index_id,
        STUFF((
            SELECT ',' + c.name
            FROM sys.index_columns ic2
            INNER JOIN sys.columns c ON c.object_id = ic2.object_id AND c.column_id = ic2.column_id
            WHERE ic2.object_id = ic.object_id
              AND ic2.index_id = ic.index_id
              AND ic2.key_ordinal > 0
            ORDER BY ic2.key_ordinal
            FOR XML PATH(''), TYPE
        ).value('.','NVARCHAR(MAX)'),1,1,'') AS key_columns,
        STUFF((
            SELECT ',' + c.name
            FROM sys.index_columns ic2
            INNER JOIN sys.columns c ON c.object_id = ic2.object_id AND c.column_id = ic2.column_id
            WHERE ic2.object_id = ic.object_id
              AND ic2.index_id = ic.index_id
              AND ic2.is_included_column = 1
            ORDER BY c.column_id
            FOR XML PATH(''), TYPE
        ).value('.','NVARCHAR(MAX)'),1,1,'') AS include_columns
    FROM sys.index_columns ic
    GROUP BY ic.object_id, ic.index_id
), DupGroups AS (
    SELECT object_id, key_columns, ISNULL(include_columns,'') AS include_columns, COUNT(*) AS cnt
    FROM IndexCols
    GROUP BY object_id, key_columns, ISNULL(include_columns,'')
    HAVING COUNT(*) > 1
)
SELECT TOP (100)
    isx.schema_name,
    isx.table_name,
    isx.index_name,
    isx.type_desc,
    CAST(isx.used_pages*8.0/1024 AS DECIMAL(18,2)) AS used_mb,
    isx.used_pages,
    isx.row_count,
    ISNULL(us.user_seeks,0) AS user_seeks,
    ISNULL(us.user_scans,0) AS user_scans,
    ISNULL(us.user_lookups,0) AS user_lookups,
    ISNULL(us.user_updates,0) AS user_updates,
    CASE WHEN ISNULL(us.user_seeks,0)+ISNULL(us.user_scans,0)+ISNULL(us.user_lookups,0) = 0 THEN 1 ELSE 0 END AS is_unused_read,
    CASE WHEN dg.cnt IS NOT NULL THEN 1 ELSE 0 END AS is_duplicate_signature,
    ic.key_columns,
    ic.include_columns
FROM IndexStats isx
LEFT JOIN UsageStats us ON us.object_id = isx.object_id AND us.index_id = isx.index_id
LEFT JOIN IndexCols ic ON ic.object_id = isx.object_id AND ic.index_id = isx.index_id
LEFT JOIN DupGroups dg ON dg.object_id = isx.object_id AND dg.key_columns = ic.key_columns AND dg.include_columns = ISNULL(ic.include_columns,'')
ORDER BY isx.used_pages DESC;

PRINT 'SECTION 3 - TARGET TABLES SIZE';
;WITH TS AS (
    SELECT 
        s.name AS schema_name,
        t.name AS table_name,
        SUM(CASE WHEN i.index_id IN (0,1) THEN p.rows ELSE 0 END) AS row_count,
        SUM(a.total_pages) AS total_pages,
        SUM(a.used_pages) AS used_pages,
        SUM(CASE WHEN i.index_id IN (0,1) THEN a.data_pages ELSE 0 END) AS data_pages,
        SUM(CASE WHEN i.index_id > 1 THEN a.used_pages ELSE 0 END) AS index_used_pages
    FROM sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    INNER JOIN sys.indexes i ON i.object_id = t.object_id
    INNER JOIN sys.partitions p ON p.object_id = i.object_id AND p.index_id = i.index_id
    INNER JOIN sys.allocation_units a ON a.container_id = p.partition_id
    WHERE t.is_ms_shipped = 0
    GROUP BY s.name, t.name
)
SELECT 
    schema_name,
    table_name,
    row_count,
    CAST(data_pages*8.0/1024 AS DECIMAL(18,2)) AS data_mb,
    CAST(index_used_pages*8.0/1024 AS DECIMAL(18,2)) AS index_mb,
    CAST(total_pages*8.0/1024 AS DECIMAL(18,2)) AS reserved_mb
FROM TS
WHERE table_name IN ('Transactions','Transaction_Details','DOUBLE_ENTREY_VOUCHERS','Payments','Notes')
   OR table_name LIKE '%Log%'
   OR table_name LIKE '%Temp%'
   OR table_name LIKE '%Audit%'
   OR table_name LIKE '%Import%'
   OR table_name LIKE '%Stage%'
   OR table_name LIKE '%Staging%'
   OR table_name LIKE '%_bak%'
   OR table_name LIKE '%bak%'
   OR table_name LIKE '%test%'
   OR table_name LIKE '%old%'
ORDER BY reserved_mb DESC;

PRINT 'SECTION 4A - TRANSACTIONS YEAR MONTH DISTRIBUTION';
IF OBJECT_ID('dbo.Transactions') IS NOT NULL
BEGIN
    DECLARE @trxDateCol SYSNAME;
    SELECT TOP 1 @trxDateCol = c.name
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID('dbo.Transactions')
      AND c.system_type_id IN (40,42,43,58,61)
    ORDER BY CASE WHEN c.name IN ('Date','TransactionDate','CreatedDate','DocDate') THEN 0 ELSE 1 END, c.column_id;

    IF @trxDateCol IS NOT NULL
    BEGIN
        DECLARE @sqlTrx NVARCHAR(MAX);
        SET @sqlTrx = N'SELECT YEAR(' + QUOTENAME(@trxDateCol) + N') AS [year], MONTH(' + QUOTENAME(@trxDateCol) + N') AS [month], COUNT_BIG(*) AS row_count FROM dbo.Transactions WHERE ' + QUOTENAME(@trxDateCol) + N' IS NOT NULL GROUP BY YEAR(' + QUOTENAME(@trxDateCol) + N'), MONTH(' + QUOTENAME(@trxDateCol) + N') ORDER BY [year], [month];';
        EXEC sp_executesql @sqlTrx;
    END
    ELSE
    BEGIN
        SELECT 'No date/datetime column found in dbo.Transactions' AS info;
    END
END
ELSE
BEGIN
    SELECT 'dbo.Transactions table not found' AS info;
END

PRINT 'SECTION 4B - TRANSACTION_DETAILS VIA TRANSACTIONS YEAR MONTH DISTRIBUTION';
IF OBJECT_ID('dbo.Transaction_Details') IS NOT NULL AND OBJECT_ID('dbo.Transactions') IS NOT NULL
BEGIN
    DECLARE @fkcol SYSNAME, @pkcol SYSNAME, @trxDateCol2 SYSNAME;

    SELECT TOP 1
        @fkcol = pc.name,
        @pkcol = rc.name
    FROM sys.foreign_key_columns fkc
    INNER JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
    INNER JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
    WHERE fkc.parent_object_id = OBJECT_ID('dbo.Transaction_Details')
      AND fkc.referenced_object_id = OBJECT_ID('dbo.Transactions');

    SELECT TOP 1 @trxDateCol2 = c.name
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID('dbo.Transactions')
      AND c.system_type_id IN (40,42,43,58,61)
    ORDER BY CASE WHEN c.name IN ('Date','TransactionDate','CreatedDate','DocDate') THEN 0 ELSE 1 END, c.column_id;

    IF @fkcol IS NOT NULL AND @pkcol IS NOT NULL AND @trxDateCol2 IS NOT NULL
    BEGIN
        DECLARE @sqlTd NVARCHAR(MAX);
        SET @sqlTd = N'SELECT YEAR(t.' + QUOTENAME(@trxDateCol2) + N') AS [year], MONTH(t.' + QUOTENAME(@trxDateCol2) + N') AS [month], COUNT_BIG(*) AS detail_row_count FROM dbo.Transaction_Details td INNER JOIN dbo.Transactions t ON t.' + QUOTENAME(@pkcol) + N' = td.' + QUOTENAME(@fkcol) + N' WHERE t.' + QUOTENAME(@trxDateCol2) + N' IS NOT NULL GROUP BY YEAR(t.' + QUOTENAME(@trxDateCol2) + N'), MONTH(t.' + QUOTENAME(@trxDateCol2) + N') ORDER BY [year], [month];';
        EXEC sp_executesql @sqlTd;
    END
    ELSE
    BEGIN
        SELECT 'Could not infer FK join or date column for Transaction_Details -> Transactions' AS info;
    END
END
ELSE
BEGIN
    SELECT 'Required tables not found' AS info;
END

PRINT 'SECTION 4C - DOUBLE_ENTREY_VOUCHERS YEAR MONTH DISTRIBUTION';
IF OBJECT_ID('dbo.DOUBLE_ENTREY_VOUCHERS') IS NOT NULL
BEGIN
    DECLARE @devDateCol SYSNAME;
    SELECT TOP 1 @devDateCol = c.name
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID('dbo.DOUBLE_ENTREY_VOUCHERS')
      AND c.system_type_id IN (40,42,43,58,61)
    ORDER BY CASE WHEN c.name IN ('Date','VoucherDate','CreatedDate','DocDate') THEN 0 ELSE 1 END, c.column_id;

    IF @devDateCol IS NOT NULL
    BEGIN
        DECLARE @sqlDev NVARCHAR(MAX);
        SET @sqlDev = N'SELECT YEAR(' + QUOTENAME(@devDateCol) + N') AS [year], MONTH(' + QUOTENAME(@devDateCol) + N') AS [month], COUNT_BIG(*) AS row_count FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE ' + QUOTENAME(@devDateCol) + N' IS NOT NULL GROUP BY YEAR(' + QUOTENAME(@devDateCol) + N'), MONTH(' + QUOTENAME(@devDateCol) + N') ORDER BY [year], [month];';
        EXEC sp_executesql @sqlDev;
    END
    ELSE
    BEGIN
        SELECT 'No date/datetime column found in dbo.DOUBLE_ENTREY_VOUCHERS' AS info;
    END
END
ELSE
BEGIN
    SELECT 'dbo.DOUBLE_ENTREY_VOUCHERS table not found' AS info;
END

PRINT 'SECTION 5 - LOG DIAGNOSTICS';
SELECT 
    d.name AS database_name,
    d.recovery_model_desc,
    d.log_reuse_wait_desc,
    CAST(ls.total_log_size_in_bytes/1048576.0 AS DECIMAL(18,2)) AS total_log_size_mb,
    CAST(ls.used_log_space_in_bytes/1048576.0 AS DECIMAL(18,2)) AS used_log_space_mb,
    CAST(ls.used_log_space_in_percent AS DECIMAL(10,2)) AS used_log_space_in_percent
FROM sys.databases d
CROSS APPLY sys.dm_db_log_space_usage ls
WHERE d.database_id = DB_ID();

PRINT 'SECTION 6 - SPACE RESERVED USED UNUSED PER TABLE TOP 100';
;WITH SpacePerTable AS (
    SELECT 
        s.name AS schema_name,
        t.name AS table_name,
        SUM(a.total_pages) AS total_pages,
        SUM(a.used_pages) AS used_pages,
        SUM(a.data_pages) AS data_pages
    FROM sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    INNER JOIN sys.indexes i ON i.object_id = t.object_id
    INNER JOIN sys.partitions p ON p.object_id = i.object_id AND p.index_id = i.index_id
    INNER JOIN sys.allocation_units a ON a.container_id = p.partition_id
    WHERE t.is_ms_shipped = 0
    GROUP BY s.name, t.name
)
SELECT TOP (100)
    schema_name,
    table_name,
    CAST(total_pages*8.0/1024 AS DECIMAL(18,2)) AS reserved_mb,
    CAST(used_pages*8.0/1024 AS DECIMAL(18,2)) AS used_mb,
    CAST((total_pages-used_pages)*8.0/1024 AS DECIMAL(18,2)) AS unused_mb,
    CAST(data_pages*8.0/1024 AS DECIMAL(18,2)) AS data_mb,
    CAST((used_pages-data_pages)*8.0/1024 AS DECIMAL(18,2)) AS non_data_used_mb
FROM SpacePerTable
ORDER BY total_pages DESC;

PRINT 'SECTION 7 - TOP FRAGMENTED INDEXES (LARGE ONLY)';
SELECT TOP (100)
    s.name AS schema_name,
    t.name AS table_name,
    i.name AS index_name,
    ips.index_type_desc,
    ips.page_count,
    CAST(ips.avg_fragmentation_in_percent AS DECIMAL(10,2)) AS avg_fragmentation_in_percent,
    CASE 
        WHEN ips.page_count < 1000 THEN 'NO ACTION (small index)'
        WHEN ips.avg_fragmentation_in_percent >= 30 THEN 'REBUILD (recommended in maintenance window)'
        WHEN ips.avg_fragmentation_in_percent >= 10 THEN 'REORGANIZE (recommended)'
        ELSE 'NO ACTION'
    END AS recommendation
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON i.object_id = ips.object_id AND i.index_id = ips.index_id
INNER JOIN sys.tables t ON t.object_id = i.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
  AND ips.page_count >= 1000
  AND i.index_id > 0
ORDER BY ips.avg_fragmentation_in_percent DESC, ips.page_count DESC;

