/*
    POS save-attempt diagnostics screen repair/backfill.
    SQL Server 2012 compatible.
*/

IF OBJECT_ID(N'dbo.POS_SaveAttemptLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SaveAttemptLog
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SaveAttemptLog PRIMARY KEY,
        SaveAttemptId UNIQUEIDENTIFIER NOT NULL,
        EventName NVARCHAR(100) NOT NULL,
        UserID INT NULL,
        EmpID INT NULL,
        BranchId INT NULL,
        TransactionType NVARCHAR(50) NULL,
        RetryAttempt INT NULL,
        SqlErrorNumber INT NULL,
        DelayMs INT NULL,
        DurationMs INT NULL,
        Transaction_ID INT NULL,
        Status NVARCHAR(50) NULL,
        Message NVARCHAR(MAX) NULL,
        RequestSummary NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_SaveAttemptLog_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH(N'dbo.POS_SaveAttemptLog', N'StoreID') IS NULL ALTER TABLE dbo.POS_SaveAttemptLog ADD StoreID INT NULL;
IF COL_LENGTH(N'dbo.POS_SaveAttemptLog', N'BoxID') IS NULL ALTER TABLE dbo.POS_SaveAttemptLog ADD BoxID INT NULL;
IF COL_LENGTH(N'dbo.POS_SaveAttemptLog', N'PaymentType') IS NULL ALTER TABLE dbo.POS_SaveAttemptLog ADD PaymentType INT NULL;
IF COL_LENGTH(N'dbo.POS_SaveAttemptLog', N'IsCashOut') IS NULL ALTER TABLE dbo.POS_SaveAttemptLog ADD IsCashOut BIT NULL;
IF COL_LENGTH(N'dbo.POS_SaveAttemptLog', N'IsWallet') IS NULL ALTER TABLE dbo.POS_SaveAttemptLog ADD IsWallet BIT NULL;
IF COL_LENGTH(N'dbo.POS_SaveAttemptLog', N'ItemIDService') IS NULL ALTER TABLE dbo.POS_SaveAttemptLog ADD ItemIDService INT NULL;
IF COL_LENGTH(N'dbo.POS_SaveAttemptLog', N'ItemIDService2') IS NULL ALTER TABLE dbo.POS_SaveAttemptLog ADD ItemIDService2 INT NULL;
IF COL_LENGTH(N'dbo.POS_SaveAttemptLog', N'RechargeValue') IS NULL ALTER TABLE dbo.POS_SaveAttemptLog ADD RechargeValue DECIMAL(18, 4) NULL;
IF COL_LENGTH(N'dbo.POS_SaveAttemptLog', N'ItemCount') IS NULL ALTER TABLE dbo.POS_SaveAttemptLog ADD ItemCount INT NULL;
IF COL_LENGTH(N'dbo.POS_SaveAttemptLog', N'OperationFingerprint') IS NULL ALTER TABLE dbo.POS_SaveAttemptLog ADD OperationFingerprint NVARCHAR(400) NULL;
GO

IF OBJECT_ID(N'dbo.ufn_POS_SaveAttemptSummaryValue', N'FN') IS NOT NULL
    DROP FUNCTION dbo.ufn_POS_SaveAttemptSummaryValue;
GO

CREATE FUNCTION dbo.ufn_POS_SaveAttemptSummaryValue
(
    @Summary NVARCHAR(MAX),
    @Key NVARCHAR(100)
)
RETURNS NVARCHAR(4000)
AS
BEGIN
    DECLARE @Value NVARCHAR(4000);
    DECLARE @Pattern NVARCHAR(120);
    DECLARE @Start INT;
    DECLARE @End INT;

    IF @Summary IS NULL OR @Key IS NULL OR LEN(@Key) = 0
        RETURN NULL;

    SET @Pattern = @Key + N'=';
    SET @Start = CHARINDEX(@Pattern, @Summary);
    IF @Start = 0
        RETURN NULL;

    SET @Start = @Start + LEN(@Pattern);
    SET @End = CHARINDEX(N';', @Summary, @Start);
    IF @End = 0
        SET @End = LEN(@Summary) + 1;

    SET @Value = NULLIF(LTRIM(RTRIM(SUBSTRING(@Summary, @Start, @End - @Start))), N'');
    RETURN @Value;
END
GO

UPDATE dbo.POS_SaveAttemptLog
SET
    StoreID = COALESCE(StoreID, CASE WHEN ISNUMERIC(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'StoreID')) = 1 THEN CONVERT(INT, dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'StoreID')) ELSE NULL END),
    BoxID = COALESCE(BoxID, CASE WHEN ISNUMERIC(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'BoxID')) = 1 THEN CONVERT(INT, dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'BoxID')) ELSE NULL END),
    PaymentType = COALESCE(PaymentType, CASE WHEN ISNUMERIC(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'PaymentType')) = 1 THEN CONVERT(INT, dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'PaymentType')) ELSE NULL END),
    IsCashOut = COALESCE(IsCashOut, CASE LOWER(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'IsCashOut')) WHEN N'true' THEN 1 WHEN N'false' THEN 0 ELSE NULL END),
    IsWallet = COALESCE(IsWallet, CASE LOWER(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'IsWallet')) WHEN N'true' THEN 1 WHEN N'false' THEN 0 ELSE NULL END),
    ItemIDService = COALESCE(ItemIDService, CASE WHEN ISNUMERIC(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'ItemIDService')) = 1 THEN CONVERT(INT, dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'ItemIDService')) ELSE NULL END),
    ItemIDService2 = COALESCE(ItemIDService2, CASE WHEN ISNUMERIC(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'ItemIDService2')) = 1 THEN CONVERT(INT, dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'ItemIDService2')) ELSE NULL END),
    RechargeValue = COALESCE(RechargeValue, CASE WHEN ISNUMERIC(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'RechargeValue')) = 1 THEN CONVERT(DECIMAL(18, 4), dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'RechargeValue')) ELSE NULL END),
    ItemCount = COALESCE(ItemCount, CASE WHEN ISNUMERIC(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'Items')) = 1 THEN CONVERT(INT, dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'Items')) ELSE NULL END),
    OperationFingerprint = COALESCE(OperationFingerprint,
        LOWER(ISNULL(TransactionType, N'')) +
        N'|b:' + ISNULL(CONVERT(NVARCHAR(20), BranchId), N'') +
        N'|s:' + ISNULL(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'StoreID'), N'') +
        N'|box:' + ISNULL(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'BoxID'), N'') +
        N'|pay:' + ISNULL(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'PaymentType'), N'') +
        N'|svc:' + ISNULL(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'ItemIDService'), N'') +
        N'|svc2:' + ISNULL(dbo.ufn_POS_SaveAttemptSummaryValue(RequestSummary, N'ItemIDService2'), N''))
WHERE RequestSummary IS NOT NULL
  AND (
      StoreID IS NULL OR BoxID IS NULL OR PaymentType IS NULL OR IsCashOut IS NULL OR IsWallet IS NULL
      OR ItemIDService IS NULL OR ItemIDService2 IS NULL OR RechargeValue IS NULL OR ItemCount IS NULL
      OR OperationFingerprint IS NULL
  );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_DeadlockDiagnostics' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
BEGIN
    CREATE INDEX IX_POS_SaveAttemptLog_DeadlockDiagnostics
    ON dbo.POS_SaveAttemptLog(EventName, CreatedAt DESC, BranchId, StoreID, BoxID, TransactionType)
    INCLUDE (SaveAttemptId, UserID, EmpID, RetryAttempt, DelayMs, DurationMs, Status, SqlErrorNumber, OperationFingerprint);
END
GO

SELECT
    TotalRows = COUNT(1),
    RowsWithDeadlock = SUM(CASE WHEN EventName = N'Save.Retry.Deadlock' OR SqlErrorNumber = 1205 THEN 1 ELSE 0 END),
    RowsWithTimeout = SUM(CASE WHEN SqlErrorNumber = -2 OR Message LIKE N'%timeout%' OR Message LIKE N'%مهلة%' THEN 1 ELSE 0 END),
    RowsWithOperationDetails = SUM(CASE WHEN StoreID IS NOT NULL OR BoxID IS NOT NULL OR ItemIDService IS NOT NULL OR OperationFingerprint IS NOT NULL THEN 1 ELSE 0 END),
    MinCreatedAt = MIN(CreatedAt),
    MaxCreatedAt = MAX(CreatedAt)
FROM dbo.POS_SaveAttemptLog WITH (NOLOCK);
GO
