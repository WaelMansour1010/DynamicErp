/*
    Kishny POS - DEV_Serial diagnostics for DOUBLE_ENTREY_VOUCHERS
    Read-only. SQL Server 2012 compatible.
*/

SET NOCOUNT ON;

DECLARE @FromDate DATE = DATEADD(DAY, -2, CONVERT(DATE, GETDATE()));
DECLARE @ToDate DATE = DATEADD(DAY, 1, CONVERT(DATE, GETDATE()));

SELECT
    DatabaseName = DB_NAME(),
    LoginName = SUSER_SNAME(),
    DatabaseUser = USER_NAME(),
    SqlVersion = @@VERSION;

SELECT
    DoubleEntryTableExists = CASE WHEN OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS', N'U') IS NULL THEN 0 ELSE 1 END,
    AllocatorTableExists = CASE WHEN OBJECT_ID(N'dbo.POS_DEVSerialAllocator', N'U') IS NULL THEN 0 ELSE 1 END,
    SaveProcedureExists = CASE WHEN OBJECT_ID(N'dbo.usp_POS_SaveTransaction', N'P') IS NULL THEN 0 ELSE 1 END;

SELECT
    ColumnName = c.name,
    TypeName = t.name,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
  AND c.name IN
  (
      N'Double_Entry_Vouchers_ID',
      N'DEV_ID_Line_No',
      N'DEV_Serial',
      N'RecordDate',
      N'Notes_ID',
      N'Transaction_ID',
      N'branch_id',
      N'UserID'
  )
ORDER BY c.column_id;

SELECT
    IndexName = i.name,
    i.is_primary_key,
    i.is_unique,
    Columns = STUFF(
        (
            SELECT N', ' + c.name
            FROM sys.index_columns ic
            INNER JOIN sys.columns c
                ON c.object_id = ic.object_id
               AND c.column_id = ic.column_id
            WHERE ic.object_id = i.object_id
              AND ic.index_id = i.index_id
              AND ic.is_included_column = 0
            ORDER BY ic.key_ordinal
            FOR XML PATH(N''), TYPE
        ).value(N'.', N'nvarchar(max)'),
        1,
        2,
        N''
    )
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
ORDER BY i.is_primary_key DESC, i.is_unique DESC, i.name;

IF OBJECT_ID(N'dbo.POS_DEVSerialAllocator', N'U') IS NOT NULL
BEGIN
    SELECT TOP (30)
        SerialDate,
        LastSerialNo,
        UpdatedAt
    FROM dbo.POS_DEVSerialAllocator WITH (READCOMMITTEDLOCK)
    ORDER BY SerialDate DESC;
END;

SELECT
    VoucherDate = CONVERT(DATE, RecordDate),
    VoucherHeaders = COUNT(DISTINCT Double_Entry_Vouchers_ID),
    RowsCount = COUNT_BIG(*),
    EmptyDevSerialRows = SUM(CASE WHEN NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'') IS NULL THEN 1 ELSE 0 END),
    DistinctDevSerials = COUNT(DISTINCT NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'')),
    MinDoubleEntryVoucherId = MIN(Double_Entry_Vouchers_ID),
    MaxDoubleEntryVoucherId = MAX(Double_Entry_Vouchers_ID)
FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
WHERE RecordDate >= @FromDate
  AND RecordDate < @ToDate
GROUP BY CONVERT(DATE, RecordDate)
ORDER BY VoucherDate DESC;

SELECT TOP (100)
    VoucherDate = CONVERT(DATE, RecordDate),
    DevSerialText = NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N''),
    VoucherHeaders = COUNT(DISTINCT Double_Entry_Vouchers_ID),
    RowsCount = COUNT_BIG(*),
    MinVoucherId = MIN(Double_Entry_Vouchers_ID),
    MaxVoucherId = MAX(Double_Entry_Vouchers_ID)
FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
WHERE RecordDate >= @FromDate
  AND RecordDate < @ToDate
  AND NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'') IS NOT NULL
GROUP BY CONVERT(DATE, RecordDate), NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'')
HAVING COUNT(DISTINCT Double_Entry_Vouchers_ID) > 1
ORDER BY VoucherDate DESC, VoucherHeaders DESC;

SELECT TOP (100)
    d.Double_Entry_Vouchers_ID,
    d.DEV_ID_Line_No,
    d.DEV_Serial,
    d.RecordDate,
    d.Notes_ID,
    d.Transaction_ID,
    d.branch_id,
    d.UserID,
    d.Account_Code,
    d.Value
FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (READCOMMITTEDLOCK)
WHERE d.RecordDate >= @FromDate
  AND d.RecordDate < @ToDate
ORDER BY d.Double_Entry_Vouchers_ID DESC, d.DEV_ID_Line_No;

IF OBJECT_ID(N'dbo.POS_SaveAttemptLog', N'U') IS NOT NULL
BEGIN
    SELECT TOP (100)
        CreatedAt,
        EventName,
        Status,
        SqlErrorNumber,
        BranchId,
        TransactionType,
        DurationMs,
        DelayMs,
        Message,
        RequestSummary
    FROM dbo.POS_SaveAttemptLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= @FromDate
      AND
      (
          Message LIKE N'%DEV_Serial%'
          OR Message LIKE N'%DOUBLE_ENTREY_VOUCHERS%'
          OR Message LIKE N'%sp_getapplock%'
          OR SqlErrorNumber IN (1205, 50000)
      )
    ORDER BY CreatedAt DESC;
END;

