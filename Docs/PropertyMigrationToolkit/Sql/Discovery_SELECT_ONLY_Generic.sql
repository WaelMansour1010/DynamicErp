/* Generic Discovery - SELECT ONLY. Replace variables before running. */
DECLARE @SourceDatabaseName sysname = N'$(SourceDatabaseName)';
DECLARE @CustomerCode nvarchar(50) = N'$(CustomerCode)';

SELECT @CustomerCode AS CustomerCode, @SourceDatabaseName AS SourceDatabaseName;

DECLARE @sql nvarchar(max);
SET @sql = N'
SELECT TABLE_SCHEMA, TABLE_NAME
FROM ' + QUOTENAME(@SourceDatabaseName) + N'.INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = ''BASE TABLE''
  AND (
       TABLE_NAME LIKE ''%Property%'' OR TABLE_NAME LIKE ''%Aqar%'' OR TABLE_NAME LIKE ''%Iqar%''
    OR TABLE_NAME LIKE ''%Contract%'' OR TABLE_NAME LIKE ''%Install%'' OR TABLE_NAME LIKE ''%Note%''
    OR TABLE_NAME LIKE ''%Voucher%'' OR TABLE_NAME LIKE ''%Journal%'' OR TABLE_NAME LIKE ''%DOUBLE%''
    OR TABLE_NAME LIKE ''%Cust%'' OR TABLE_NAME LIKE ''%Tenant%'' OR TABLE_NAME LIKE ''%Renter%''
  )
ORDER BY TABLE_NAME;';
EXEC sp_executesql @sql;

SET @sql = N'
SELECT NoteType, COUNT(*) CountRows, MIN(NoteDate) MinDate, MAX(NoteDate) MaxDate, SUM(ISNULL(Note_Value,0)) SumValue
FROM ' + QUOTENAME(@SourceDatabaseName) + N'.dbo.Notes
GROUP BY NoteType
ORDER BY NoteType;';
EXEC sp_executesql @sql;
