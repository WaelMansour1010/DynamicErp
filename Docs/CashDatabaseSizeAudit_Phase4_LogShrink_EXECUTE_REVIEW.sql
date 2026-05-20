/*
Cash Phase 4 - Log Shrink Execute Review (DO NOT RUN BEFORE APPROVAL)
SQL Server 2012 compatible
Target: Cash log file only
No SHRINKDATABASE. No recovery model change.
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

USE [Cash];

PRINT 'PHASE 4 - STEP 1: Current DB + log status';
SELECT 
    DB_NAME() AS database_name,
    d.recovery_model_desc,
    d.log_reuse_wait_desc
FROM sys.databases d
WHERE d.name = DB_NAME();

SELECT 
    name AS logical_name,
    physical_name,
    CAST(size/128.0 AS DECIMAL(18,2)) AS size_mb,
    CAST(FILEPROPERTY(name,'SpaceUsed')/128.0 AS DECIMAL(18,2)) AS used_mb,
    CAST((size - FILEPROPERTY(name,'SpaceUsed'))/128.0 AS DECIMAL(18,2)) AS free_mb
FROM sys.database_files
WHERE type_desc = 'LOG';

PRINT 'PHASE 4 - STEP 2: DBCC SQLPERF(LOGSPACE) BEFORE';
DBCC SQLPERF(LOGSPACE);

PRINT 'PHASE 4 - STEP 3: CHECKPOINT to flush reusable log records';
CHECKPOINT;

PRINT 'PHASE 4 - STEP 4: Resolve log logical name and shrink to SAFE target (1024MB)';
DECLARE @LogLogicalName SYSNAME;
SELECT TOP (1) @LogLogicalName = name
FROM sys.database_files
WHERE type_desc = 'LOG'
ORDER BY file_id;

PRINT 'Target log logical name: ' + ISNULL(@LogLogicalName, '<NULL>');

IF @LogLogicalName IS NOT NULL
BEGIN
    -- Safe target: 1024MB (conservative). Do not use tiny size like 1MB.
    DBCC SHRINKFILE (@LogLogicalName, 1024);
END
ELSE
BEGIN
    PRINT 'No LOG file found. Shrink skipped.';
END

PRINT 'PHASE 4 - STEP 5: DBCC SQLPERF(LOGSPACE) AFTER';
DBCC SQLPERF(LOGSPACE);

PRINT 'PHASE 4 - STEP 6: Final log file size snapshot';
SELECT 
    name AS logical_name,
    physical_name,
    CAST(size/128.0 AS DECIMAL(18,2)) AS size_mb,
    CAST(FILEPROPERTY(name,'SpaceUsed')/128.0 AS DECIMAL(18,2)) AS used_mb,
    CAST((size - FILEPROPERTY(name,'SpaceUsed'))/128.0 AS DECIMAL(18,2)) AS free_mb
FROM sys.database_files
WHERE type_desc = 'LOG';
