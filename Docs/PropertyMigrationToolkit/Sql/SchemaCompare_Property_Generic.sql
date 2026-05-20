/* Generic property schema compare - SELECT ONLY. */
DECLARE @ReferenceDatabaseName sysname = N'$(ReferenceDatabaseName)';
DECLARE @TargetDatabaseName sysname = N'$(TargetDatabaseName)';
DECLARE @sql nvarchar(max);
SET @sql = N'
;WITH RefTables AS (
 SELECT TABLE_SCHEMA, TABLE_NAME FROM ' + QUOTENAME(@ReferenceDatabaseName) + N'.INFORMATION_SCHEMA.TABLES
 WHERE TABLE_TYPE=''BASE TABLE'' AND (TABLE_NAME LIKE ''Property%'' OR TABLE_NAME IN (''CashReceiptVoucherPropertyContractBatch''))
), TargetTables AS (
 SELECT TABLE_SCHEMA, TABLE_NAME FROM ' + QUOTENAME(@TargetDatabaseName) + N'.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE=''BASE TABLE''
)
SELECT r.TABLE_SCHEMA,r.TABLE_NAME,CASE WHEN t.TABLE_NAME IS NULL THEN 0 ELSE 1 END ExistsInTarget
FROM RefTables r LEFT JOIN TargetTables t ON t.TABLE_SCHEMA=r.TABLE_SCHEMA AND t.TABLE_NAME=r.TABLE_NAME
ORDER BY r.TABLE_NAME;';
EXEC sp_executesql @sql;
