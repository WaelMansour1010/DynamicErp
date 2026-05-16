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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_DeadlockDiagnostics' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
BEGIN
    CREATE INDEX IX_POS_SaveAttemptLog_DeadlockDiagnostics
    ON dbo.POS_SaveAttemptLog(EventName, CreatedAt DESC, BranchId, StoreID, BoxID, TransactionType)
    INCLUDE (SaveAttemptId, UserID, EmpID, RetryAttempt, DelayMs, DurationMs, Status, SqlErrorNumber, OperationFingerprint);
END
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

IF OBJECT_ID(N'dbo.usp_POS_SaveAttemptDeadlockDiagnostics', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SaveAttemptDeadlockDiagnostics;
GO

CREATE PROCEDURE dbo.usp_POS_SaveAttemptDeadlockDiagnostics
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @BranchId INT = NULL,
    @Top INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @ToDate IS NULL SET @ToDate = GETDATE();
    IF @FromDate IS NULL SET @FromDate = DATEADD(DAY, -1, @ToDate);
    IF @Top IS NULL OR @Top <= 0 SET @Top = 20;
    IF @Top > 200 SET @Top = 200;

    ;WITH ScopeEvents AS
    (
        SELECT *
        FROM dbo.POS_SaveAttemptLog WITH (NOLOCK)
        WHERE CreatedAt >= @FromDate
          AND CreatedAt <= @ToDate
          AND (@BranchId IS NULL OR BranchId = @BranchId)
    ),
    Attempts AS
    (
        SELECT
            SaveAttemptId,
            MAX(BranchId) AS BranchId,
            MAX(StoreID) AS StoreID,
            MAX(BoxID) AS BoxID,
            MAX(UserID) AS UserID,
            MAX(TransactionType) AS TransactionType,
            MAX(PaymentType) AS PaymentType,
            MAX(ItemIDService) AS ItemIDService,
            MAX(ItemIDService2) AS ItemIDService2,
            MAX(DurationMs) AS DurationMs,
            MAX(DelayMs) AS MaxDelayMs,
            MAX(CASE WHEN EventName = N'Save.Retry.Deadlock' THEN 1 ELSE 0 END) AS HadDeadlock,
            MAX(CASE WHEN Status = N'RetriedSuccess' THEN 1 ELSE 0 END) AS RetriedSuccess,
            MAX(CASE WHEN Status = N'RetriedFailed' THEN 1 ELSE 0 END) AS RetriedFailed,
            MAX(CASE WHEN Status IN (N'Failed', N'RetriedFailed') THEN 1 ELSE 0 END) AS Failed
        FROM ScopeEvents
        GROUP BY SaveAttemptId
    )
    SELECT
        TotalAttempts = COUNT(1),
        DeadlockAttempts = SUM(HadDeadlock),
        DeadlockRatePercent = CAST(CASE WHEN COUNT(1) = 0 THEN 0 ELSE (SUM(HadDeadlock) * 100.0 / COUNT(1)) END AS DECIMAL(18, 2)),
        RetriedSuccess = SUM(RetriedSuccess),
        RetriedFailed = SUM(RetriedFailed),
        Failed = SUM(Failed),
        AvgDurationMs = CAST(AVG(CAST(ISNULL(DurationMs, 0) AS DECIMAL(18, 2))) AS DECIMAL(18, 2)),
        MaxDurationMs = MAX(DurationMs),
        MaxDelayMs = MAX(MaxDelayMs)
    FROM Attempts;

    ;WITH Attempts AS
    (
        SELECT SaveAttemptId, BranchId, StoreID, BoxID, UserID, TransactionType, PaymentType, IsCashOut, IsWallet, ItemIDService, ItemIDService2,
               MAX(DurationMs) AS DurationMs,
               MAX(CASE WHEN EventName = N'Save.Retry.Deadlock' THEN 1 ELSE 0 END) AS HadDeadlock,
               MAX(CASE WHEN Status = N'RetriedFailed' THEN 1 ELSE 0 END) AS RetriedFailed
        FROM dbo.POS_SaveAttemptLog WITH (NOLOCK)
        WHERE CreatedAt >= @FromDate AND CreatedAt <= @ToDate AND (@BranchId IS NULL OR BranchId = @BranchId)
        GROUP BY SaveAttemptId, BranchId, StoreID, BoxID, UserID, TransactionType, PaymentType, IsCashOut, IsWallet, ItemIDService, ItemIDService2
    )
    SELECT TOP (@Top)
        BranchId,
        StoreID,
        BoxID,
        TransactionType,
        PaymentType,
        IsCashOut,
        IsWallet,
        ItemIDService,
        ItemIDService2,
        Attempts = COUNT(1),
        Deadlocks = SUM(HadDeadlock),
        RetriedFailed = SUM(RetriedFailed),
        DeadlockRatePercent = CAST(CASE WHEN COUNT(1) = 0 THEN 0 ELSE (SUM(HadDeadlock) * 100.0 / COUNT(1)) END AS DECIMAL(18, 2)),
        AvgDurationMs = CAST(AVG(CAST(ISNULL(DurationMs, 0) AS DECIMAL(18, 2))) AS DECIMAL(18, 2)),
        MaxDurationMs = MAX(DurationMs)
    FROM Attempts
    GROUP BY BranchId, StoreID, BoxID, TransactionType, PaymentType, IsCashOut, IsWallet, ItemIDService, ItemIDService2
    HAVING SUM(HadDeadlock) > 0
    ORDER BY Deadlocks DESC, RetriedFailed DESC, MaxDurationMs DESC;

    SELECT TOP (@Top)
        HourBucket = DATEADD(HOUR, DATEDIFF(HOUR, 0, CreatedAt), 0),
        DeadlockEvents = COUNT(1),
        DistinctAttempts = COUNT(DISTINCT SaveAttemptId),
        AvgDelayMs = CAST(AVG(CAST(ISNULL(DelayMs, 0) AS DECIMAL(18, 2))) AS DECIMAL(18, 2)),
        MaxDelayMs = MAX(DelayMs)
    FROM dbo.POS_SaveAttemptLog WITH (NOLOCK)
    WHERE CreatedAt >= @FromDate
      AND CreatedAt <= @ToDate
      AND EventName = N'Save.Retry.Deadlock'
      AND (@BranchId IS NULL OR BranchId = @BranchId)
    GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, CreatedAt), 0)
    ORDER BY DeadlockEvents DESC, HourBucket DESC;

    SELECT TOP (@Top)
        SaveAttemptId,
        CreatedAt,
        EventName,
        UserID,
        EmpID,
        BranchId,
        StoreID,
        BoxID,
        TransactionType,
        PaymentType,
        ItemIDService,
        ItemIDService2,
        RetryAttempt,
        SqlErrorNumber,
        DelayMs,
        DurationMs,
        Status,
        Message,
        RequestSummary
    FROM dbo.POS_SaveAttemptLog WITH (NOLOCK)
    WHERE CreatedAt >= @FromDate
      AND CreatedAt <= @ToDate
      AND (EventName = N'Save.Retry.Deadlock' OR Status IN (N'RetriedFailed', N'Failed'))
      AND (@BranchId IS NULL OR BranchId = @BranchId)
    ORDER BY CreatedAt DESC;
END
GO
