/*
  Kishny POS deadlock/save hardening backup helper.
  Run before applying SQL scripts. Save result output and generated definitions with the deployment record.
*/
SET NOCOUNT ON;

PRINT 'Existing procedure definitions';
SELECT
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    OBJECT_NAME(object_id) AS ObjectName,
    definition
FROM sys.sql_modules
WHERE object_id IN
(
    OBJECT_ID(N'dbo.GetNextID_FromSequence', N'P'),
    OBJECT_ID(N'dbo.usp_POS_SaveTransaction', N'P')
);

PRINT 'Relevant existing indexes';
SELECT
    OBJECT_SCHEMA_NAME(i.object_id) AS SchemaName,
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc,
    i.is_unique,
    i.is_primary_key
FROM sys.indexes AS i
WHERE i.object_id IN
(
    OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS', N'U'),
    OBJECT_ID(N'dbo.Notes', N'U'),
    OBJECT_ID(N'dbo.Transaction_Details', N'U'),
    OBJECT_ID(N'dbo.TblSalesPayment', N'U')
)
ORDER BY TableName, IndexName;

PRINT 'POS_SaveAttemptLog existence';
SELECT
    CASE WHEN OBJECT_ID(N'dbo.POS_SaveAttemptLog', N'U') IS NULL THEN 0 ELSE 1 END AS POS_SaveAttemptLogExists;

PRINT 'Recommended: take a full database backup before applying release SQL.';
