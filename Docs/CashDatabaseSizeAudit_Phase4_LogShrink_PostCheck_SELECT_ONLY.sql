/*
Cash Phase 4 - Log Shrink Post Check (SELECT ONLY)
SQL Server 2012 compatible
*/
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

USE [Cash];

PRINT 'POSTCHECK 1: DB status';
SELECT 
    DB_NAME() AS database_name,
    d.recovery_model_desc,
    d.log_reuse_wait_desc
FROM sys.databases d
WHERE d.name = DB_NAME();

PRINT 'POSTCHECK 2: Current log file physical metrics';
SELECT 
    name AS logical_name,
    physical_name,
    CAST(size/128.0 AS DECIMAL(18,2)) AS size_mb,
    CAST(FILEPROPERTY(name,'SpaceUsed')/128.0 AS DECIMAL(18,2)) AS used_mb,
    CAST((size - FILEPROPERTY(name,'SpaceUsed'))/128.0 AS DECIMAL(18,2)) AS free_mb
FROM sys.database_files
WHERE type_desc = 'LOG';

PRINT 'POSTCHECK 3: SQLPERF log usage';
DBCC SQLPERF(LOGSPACE);

PRINT 'POSTCHECK 4: Log autogrowth settings';
SELECT 
    name AS logical_name,
    growth,
    is_percent_growth,
    CASE WHEN is_percent_growth = 1 THEN CAST(growth AS VARCHAR(20)) + '%' ELSE CAST(growth*8/1024 AS VARCHAR(20)) + ' MB' END AS growth_setting
FROM sys.database_files
WHERE type_desc='LOG';
