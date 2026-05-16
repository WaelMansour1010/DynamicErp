/*
    Kishny POS - DEV_Serial legacy/business requirement audit
    Read-only. SQL Server 2012 compatible.

    Goal:
      Prove whether dbo.DOUBLE_ENTREY_VOUCHERS.DEV_Serial is a strict business key
      or a legacy display/order serial that can be moved out of the hot POS save path.
*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @FromDate DATE = DATEADD(DAY, -30, CONVERT(DATE, GETDATE()));
DECLARE @ToDateExclusive DATE = DATEADD(DAY, 1, CONVERT(DATE, GETDATE()));

PRINT '01. Environment';
SELECT
    DatabaseName = DB_NAME(),
    LoginName = SUSER_SNAME(),
    DatabaseUser = USER_NAME(),
    SqlVersion = @@VERSION,
    FromDate = @FromDate,
    ToDateExclusive = @ToDateExclusive;

PRINT '02. Required objects';
SELECT
    DoubleEntryTableExists = CASE WHEN OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS', N'U') IS NULL THEN 0 ELSE 1 END,
    NotesTableExists = CASE WHEN OBJECT_ID(N'dbo.Notes', N'U') IS NULL THEN 0 ELSE 1 END,
    TransactionsTableExists = CASE WHEN OBJECT_ID(N'dbo.Transactions', N'U') IS NULL THEN 0 ELSE 1 END,
    DevSerialAllocatorExists = CASE WHEN OBJECT_ID(N'dbo.POS_DEVSerialAllocator', N'U') IS NULL THEN 0 ELSE 1 END,
    SaveProcedureExists = CASE WHEN OBJECT_ID(N'dbo.usp_POS_SaveTransaction', N'P') IS NULL THEN 0 ELSE 1 END;

IF OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS', N'U') IS NULL
BEGIN
    RAISERROR('dbo.DOUBLE_ENTREY_VOUCHERS was not found in this database.', 16, 1);
    RETURN;
END;

PRINT '03. DEV_Serial column metadata';
SELECT
    ColumnName = c.name,
    TypeName = t.name,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable,
    c.column_id
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

PRINT '04. Indexes and constraints touching DEV_Serial';
SELECT
    IndexName = i.name,
    i.index_id,
    i.type_desc,
    i.is_primary_key,
    i.is_unique,
    i.is_unique_constraint,
    KeyColumns = STUFF(
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
    ),
    IncludedColumns = STUFF(
        (
            SELECT N', ' + c.name
            FROM sys.index_columns ic
            INNER JOIN sys.columns c
                ON c.object_id = ic.object_id
               AND c.column_id = ic.column_id
            WHERE ic.object_id = i.object_id
              AND ic.index_id = i.index_id
              AND ic.is_included_column = 1
            ORDER BY c.name
            FOR XML PATH(N''), TYPE
        ).value(N'.', N'nvarchar(max)'),
        1,
        2,
        N''
    )
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
  AND EXISTS
  (
      SELECT 1
      FROM sys.index_columns ic
      INNER JOIN sys.columns c
          ON c.object_id = ic.object_id
         AND c.column_id = ic.column_id
      WHERE ic.object_id = i.object_id
        AND ic.index_id = i.index_id
        AND c.name = N'DEV_Serial'
  )
ORDER BY i.is_primary_key DESC, i.is_unique DESC, i.name;

PRINT '05. Overall DEV_Serial quality';
SELECT
    TotalRows = COUNT_BIG(*),
    VoucherHeaders = COUNT(DISTINCT Double_Entry_Vouchers_ID),
    NullOrEmptyDevSerialRows = SUM(CASE WHEN NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'') IS NULL THEN 1 ELSE 0 END),
    DistinctDevSerialValues = COUNT(DISTINCT NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'')),
    MinRecordDate = MIN(RecordDate),
    MaxRecordDate = MAX(RecordDate)
FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK);

PRINT '06. Recent DEV_Serial quality by date';
SELECT
    VoucherDate = CONVERT(DATE, RecordDate),
    RowsCount = COUNT_BIG(*),
    VoucherHeaders = COUNT(DISTINCT Double_Entry_Vouchers_ID),
    EmptyDevSerialRows = SUM(CASE WHEN NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'') IS NULL THEN 1 ELSE 0 END),
    DistinctDevSerialValues = COUNT(DISTINCT NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'')),
    MinVoucherId = MIN(Double_Entry_Vouchers_ID),
    MaxVoucherId = MAX(Double_Entry_Vouchers_ID)
FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
WHERE RecordDate >= @FromDate
  AND RecordDate < @ToDateExclusive
GROUP BY CONVERT(DATE, RecordDate)
ORDER BY VoucherDate DESC;

PRINT '07. Same DEV_Serial used by more than one voucher header on same date';
SELECT TOP (100)
    VoucherDate = CONVERT(DATE, RecordDate),
    DevSerialText = NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N''),
    VoucherHeaders = COUNT(DISTINCT Double_Entry_Vouchers_ID),
    RowsCount = COUNT_BIG(*),
    MinVoucherId = MIN(Double_Entry_Vouchers_ID),
    MaxVoucherId = MAX(Double_Entry_Vouchers_ID)
FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
WHERE RecordDate >= @FromDate
  AND RecordDate < @ToDateExclusive
  AND NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'') IS NOT NULL
GROUP BY CONVERT(DATE, RecordDate), NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'')
HAVING COUNT(DISTINCT Double_Entry_Vouchers_ID) > 1
ORDER BY VoucherDate DESC, VoucherHeaders DESC, DevSerialText;

PRINT '08. One voucher header with multiple DEV_Serial values';
SELECT TOP (100)
    Double_Entry_Vouchers_ID,
    VoucherDate = CONVERT(DATE, MIN(RecordDate)),
    RowsCount = COUNT_BIG(*),
    DistinctDevSerialValues = COUNT(DISTINCT NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'')),
    DevSerialList = STUFF(
        (
            SELECT DISTINCT N', ' + NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), d2.DEV_Serial))), N'')
            FROM dbo.DOUBLE_ENTREY_VOUCHERS d2 WITH (READCOMMITTEDLOCK)
            WHERE d2.Double_Entry_Vouchers_ID = d.Double_Entry_Vouchers_ID
              AND NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), d2.DEV_Serial))), N'') IS NOT NULL
            FOR XML PATH(N''), TYPE
        ).value(N'.', N'nvarchar(max)'),
        1,
        2,
        N''
    )
FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (READCOMMITTEDLOCK)
WHERE RecordDate >= @FromDate
  AND RecordDate < @ToDateExclusive
GROUP BY Double_Entry_Vouchers_ID
HAVING COUNT(DISTINCT NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'')) > 1
ORDER BY VoucherDate DESC, Double_Entry_Vouchers_ID DESC;

PRINT '09. DEV_Serial prefix does not match RecordDate yyyyMMdd0';
SELECT TOP (100)
    Double_Entry_Vouchers_ID,
    DEV_ID_Line_No,
    RecordDate,
    DEV_Serial,
    ExpectedPrefix = CONVERT(CHAR(8), RecordDate, 112) + N'0'
FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
WHERE RecordDate >= @FromDate
  AND RecordDate < @ToDateExclusive
  AND NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'') IS NOT NULL
  AND LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))) NOT LIKE CONVERT(CHAR(8), RecordDate, 112) + N'0%'
ORDER BY RecordDate DESC, Double_Entry_Vouchers_ID DESC, DEV_ID_Line_No;

PRINT '10. Daily serial sequence rough gap check';
;WITH SerialValues AS
(
    SELECT
        VoucherDate = CONVERT(DATE, RecordDate),
        Double_Entry_Vouchers_ID,
        DevSerialText = LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))),
        SerialTailText = SUBSTRING(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), 10, 50)
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
    WHERE RecordDate >= @FromDate
      AND RecordDate < @ToDateExclusive
      AND NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))), N'') IS NOT NULL
      AND LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial))) LIKE CONVERT(CHAR(8), RecordDate, 112) + N'0%'
      AND LEN(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DEV_Serial)))) > 9
),
NumericSerials AS
(
    SELECT DISTINCT
        VoucherDate,
        Double_Entry_Vouchers_ID,
        SerialTail = CONVERT(INT, SerialTailText)
    FROM SerialValues
    WHERE ISNUMERIC(SerialTailText) = 1
)
SELECT
    VoucherDate,
    DistinctVoucherSerials = COUNT_BIG(*),
    MinSerialTail = MIN(SerialTail),
    MaxSerialTail = MAX(SerialTail),
    ApproxGapCount = MAX(SerialTail) - MIN(SerialTail) + 1 - COUNT_BIG(*)
FROM NumericSerials
GROUP BY VoucherDate
ORDER BY VoucherDate DESC;

PRINT '11. POS-related recent rows and DEV_Serial coverage';
SELECT
    SourceGroup =
        CASE
            WHEN n.sanad_source LIKE N'%POS%' OR n.sanad_source LIKE N'%KYC%' OR n.Transaction_ID IS NOT NULL OR d.Transaction_ID IS NOT NULL THEN N'Likely POS/transaction linked'
            WHEN d.Transaction_ID IS NOT NULL THEN N'Transaction linked'
            ELSE N'Other/manual/accounting'
        END,
    RowsCount = COUNT_BIG(*),
    VoucherHeaders = COUNT(DISTINCT d.Double_Entry_Vouchers_ID),
    EmptyDevSerialRows = SUM(CASE WHEN NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), d.DEV_Serial))), N'') IS NULL THEN 1 ELSE 0 END),
    DistinctDevSerialValues = COUNT(DISTINCT NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), d.DEV_Serial))), N''))
FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (READCOMMITTEDLOCK)
LEFT JOIN dbo.Notes n WITH (READCOMMITTEDLOCK)
    ON n.NoteID = d.Notes_ID
WHERE d.RecordDate >= @FromDate
  AND d.RecordDate < @ToDateExclusive
GROUP BY
    CASE
        WHEN n.sanad_source LIKE N'%POS%' OR n.sanad_source LIKE N'%KYC%' OR n.Transaction_ID IS NOT NULL OR d.Transaction_ID IS NOT NULL THEN N'Likely POS/transaction linked'
        WHEN d.Transaction_ID IS NOT NULL THEN N'Transaction linked'
        ELSE N'Other/manual/accounting'
    END;

PRINT '12. SQL modules that mention DEV_Serial';
SELECT
    SchemaName = OBJECT_SCHEMA_NAME(o.object_id),
    ObjectName = o.name,
    o.type_desc,
    MentionsDevSerial = CASE WHEN m.definition LIKE N'%DEV_Serial%' THEN 1 ELSE 0 END,
    HasOrderByNearDevSerial = CASE WHEN m.definition LIKE N'%ORDER BY%DEV_Serial%' OR m.definition LIKE N'%DEV_Serial%ORDER BY%' THEN 1 ELSE 0 END,
    HasWhereNearDevSerial = CASE WHEN m.definition LIKE N'%WHERE%DEV_Serial%' OR m.definition LIKE N'%DEV_Serial%WHERE%' THEN 1 ELSE 0 END
FROM sys.sql_modules m
INNER JOIN sys.objects o ON o.object_id = m.object_id
WHERE m.definition LIKE N'%DEV_Serial%'
ORDER BY o.type_desc, SchemaName, ObjectName;

PRINT '13. Unexpected recent DEV_Serial save-stage rows if POS_SaveAllocationStageLog exists';
IF OBJECT_ID(N'dbo.POS_SaveAllocationStageLog', N'U') IS NOT NULL
BEGIN
    SELECT
        StageName,
        Attempts = COUNT_BIG(*),
        AvgMs = AVG(CONVERT(BIGINT, DurationMs)),
        MaxMs = MAX(DurationMs)
    FROM dbo.POS_SaveAllocationStageLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= @FromDate
      AND StageName = N'DEV_Serial allocation'
    GROUP BY StageName
    ORDER BY StageName;
END;

PRINT '14. Current POS_DEVSerialAllocator rows';
IF OBJECT_ID(N'dbo.POS_DEVSerialAllocator', N'U') IS NOT NULL
BEGIN
    SELECT TOP (100)
        SerialDate,
        LastSerialNo,
        UpdatedAt
    FROM dbo.POS_DEVSerialAllocator WITH (READCOMMITTEDLOCK)
    ORDER BY SerialDate DESC;
END;
