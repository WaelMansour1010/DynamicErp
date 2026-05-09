/*
    POS DOUBLE_ENTREY_VOUCHERS allocator diagnostics.
    Database target: POS production database.
    Read-only script. SQL Server 2012 compatible.
*/
SET NOCOUNT ON;

DECLARE @SequenceName SYSNAME;
DECLARE @SequenceObject NVARCHAR(300);
DECLARE @MaxDevID BIGINT;

SET @SequenceName = N'seq_DOUBLE_ENTREY_VOUCHERS_Double_Entry_Vouchers_ID';
SET @SequenceObject = N'dbo.' + QUOTENAME(@SequenceName);

SELECT
    DatabaseName = DB_NAME(),
    LoginName = SUSER_SNAME(),
    DatabaseUser = USER_NAME(),
    IsSysAdmin = IS_SRVROLEMEMBER(N'sysadmin'),
    SqlVersion = @@VERSION;

SELECT
    SequenceExists = CASE WHEN OBJECT_ID(@SequenceObject, N'SO') IS NULL THEN 0 ELSE 1 END,
    SequenceName = @SequenceName,
    SequenceObjectId = OBJECT_ID(@SequenceObject, N'SO');

SELECT
    s.name,
    SchemaName = SCHEMA_NAME(s.schema_id),
    s.current_value,
    s.start_value,
    s.increment,
    s.is_cached
FROM sys.sequences AS s
WHERE s.name = @SequenceName
  AND SCHEMA_NAME(s.schema_id) = N'dbo';

SELECT
    ProcExists = CASE WHEN OBJECT_ID(N'dbo.GetNextID_FromSequence', N'P') IS NULL THEN 0 ELSE 1 END,
    HasAppLock = CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetNextID_FromSequence', N'P')) LIKE N'%sp_getapplock%' THEN 1 ELSE 0 END,
    UsesSequence = CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetNextID_FromSequence', N'P')) LIKE N'%NEXT VALUE FOR%' THEN 1 ELSE 0 END,
    CreatesSequence = CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetNextID_FromSequence', N'P')) LIKE N'%CREATE SEQUENCE%' THEN 1 ELSE 0 END,
    AltersSequence = CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetNextID_FromSequence', N'P')) LIKE N'%ALTER SEQUENCE%' THEN 1 ELSE 0 END,
    ExecutesAsOwner = CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetNextID_FromSequence', N'P')) LIKE N'%EXECUTE AS OWNER%' THEN 1 ELSE 0 END;

SELECT @MaxDevID = ISNULL(MAX(CONVERT(BIGINT, Double_Entry_Vouchers_ID)), 0)
FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK);

SELECT
    MaxDoubleEntryVoucherId = @MaxDevID,
    RequiredNextValue = @MaxDevID + 1,
    SequenceCurrentValue = s.current_value,
    SequenceIsBehindOrEqualToMax =
        CASE
            WHEN s.object_id IS NULL THEN NULL
            WHEN CONVERT(BIGINT, s.current_value) <= @MaxDevID THEN 1
            ELSE 0
        END
FROM (SELECT object_id, current_value FROM sys.sequences WHERE name = @SequenceName AND SCHEMA_NAME(schema_id) = N'dbo') AS s;

SELECT
    i.name AS PrimaryKeyName,
    c.name AS ColumnName,
    ic.key_ordinal
FROM sys.indexes AS i
INNER JOIN sys.index_columns AS ic
    ON ic.object_id = i.object_id
   AND ic.index_id = i.index_id
INNER JOIN sys.columns AS c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
  AND i.is_primary_key = 1
ORDER BY ic.key_ordinal;

SELECT TOP (50)
    Double_Entry_Vouchers_ID,
    DEV_ID_Line_No,
    DuplicateRows = COUNT_BIG(*)
FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
GROUP BY Double_Entry_Vouchers_ID, DEV_ID_Line_No
HAVING COUNT_BIG(*) > 1
ORDER BY DuplicateRows DESC, Double_Entry_Vouchers_ID DESC;

SELECT TOP (100)
    d.Double_Entry_Vouchers_ID,
    d.DEV_ID_Line_No,
    d.Transaction_ID,
    d.Notes_ID,
    d.RecordDate,
    d.branch_id,
    d.UserID,
    d.Account_Code,
    d.Value
FROM dbo.DOUBLE_ENTREY_VOUCHERS AS d WITH (READCOMMITTEDLOCK)
WHERE d.Double_Entry_Vouchers_ID >= @MaxDevID - 100
ORDER BY d.Double_Entry_Vouchers_ID DESC, d.DEV_ID_Line_No;

IF OBJECT_ID(N'dbo.POS_SystemErrorLog', N'U') IS NOT NULL
BEGIN
    SELECT
        ActionName,
        Status,
        Severity,
        BranchId,
        OperationType,
        ErrorCount = COUNT(*),
        FirstAt = MIN(CreatedAt),
        LastAt = MAX(CreatedAt)
    FROM dbo.POS_SystemErrorLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= DATEADD(DAY, -2, GETDATE())
      AND
      (
          ActionName LIKE N'%Save%'
          OR ActionName LIKE N'%CalculateCommission%'
          OR ErrorMessage LIKE N'%DOUBLE_ENTREY_VOUCHERS%'
          OR ErrorMessage LIKE N'%GetNextID_FromSequence%'
          OR ErrorMessage LIKE N'%PRIMARY KEY%'
      )
    GROUP BY ActionName, Status, Severity, BranchId, OperationType
    ORDER BY LastAt DESC;
END;

IF OBJECT_ID(N'dbo.POS_SaveAttemptLog', N'U') IS NOT NULL
BEGIN
    SELECT
        EventName,
        Status,
        SqlErrorNumber,
        BranchId,
        TransactionType,
        AttemptCount = COUNT(*),
        FirstAt = MIN(CreatedAt),
        LastAt = MAX(CreatedAt)
    FROM dbo.POS_SaveAttemptLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= DATEADD(DAY, -2, GETDATE())
    GROUP BY EventName, Status, SqlErrorNumber, BranchId, TransactionType
    ORDER BY LastAt DESC;
END;

SELECT TOP (200)
    t.Transaction_ID,
    t.Transaction_Date,
    t.BranchId,
    t.UserID,
    t.RechargeValue,
    t.NetValue,
    t.VAT,
    DevRows = COUNT(d.Double_Entry_Vouchers_ID)
FROM dbo.Transactions AS t WITH (READCOMMITTEDLOCK)
LEFT JOIN dbo.DOUBLE_ENTREY_VOUCHERS AS d WITH (READCOMMITTEDLOCK)
    ON d.Transaction_ID = t.Transaction_ID
WHERE t.Transaction_Date >= DATEADD(DAY, -2, GETDATE())
  AND ISNULL(t.IsPOS, 0) = 1
GROUP BY t.Transaction_ID, t.Transaction_Date, t.BranchId, t.UserID, t.RechargeValue, t.NetValue, t.VAT
HAVING COUNT(d.Double_Entry_Vouchers_ID) = 0
ORDER BY t.Transaction_ID DESC;

