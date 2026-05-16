/*
Kishny POS - Daily Closing Summary Prototype

SQL Server 2012 compatible.

Scope:
- SQL-only prototype for POS closing summary.
- Does not change dbo.usp_POS_Report_RunClosing.
- Does not change UI or application code.
- Does not create SQL Agent jobs.
- Does not backfill history automatically.

Design note:
- This prototype deliberately uses dbo.usp_POS_Report_RunClosing as the source
  of truth inside the rebuild procedure. That keeps business parity while the
  summary-table approach is validated.
- INSERT EXEC is used only inside this prototype rebuild procedure. Future
  versions can replace it with direct set-based logic after parity is proven.
*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'dbo.POS_DailyClosingSummary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DailyClosingSummary
    (
        SummaryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_DailyClosingSummary PRIMARY KEY,
        SummaryDate DATE NOT NULL,
        BranchId INT NOT NULL,
        UserId INT NULL,
        ReportKey NVARCHAR(50) NOT NULL,
        ServiceType NVARCHAR(50) NULL,
        PaymentType INT NULL,
        CashBoxId INT NULL,
        StoreId INT NULL,

        InvoiceCount INT NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_InvoiceCount DEFAULT (0),
        TotalAmount DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_TotalAmount DEFAULT (0),
        DiscountAmount DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_DiscountAmount DEFAULT (0),
        ReturnAmount DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_ReturnAmount DEFAULT (0),
        NetAmount DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_NetAmount DEFAULT (0),
        CashInAmount DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_CashInAmount DEFAULT (0),
        CashOutAmount DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_CashOutAmount DEFAULT (0),
        CardAmount DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_CardAmount DEFAULT (0),
        ViolationAmount DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_ViolationAmount DEFAULT (0),

        OpenBalance DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_OpenBalance DEFAULT (0),
        LastBalance DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_LastBalance DEFAULT (0),
        TotalRechargeValue DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_TotalRechargeValue DEFAULT (0),
        TotalRev DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_TotalRev DEFAULT (0),
        TotalVat DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_TotalVat DEFAULT (0),
        TotalRevVat DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_TotalRevVat DEFAULT (0),
        TotalSupply DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_TotalSupply DEFAULT (0),
        BoxBalance DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_BoxBalance DEFAULT (0),
        NoteValue DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_NoteValue DEFAULT (0),
        CountCards INT NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_CountCards DEFAULT (0),
        CountTransaction INT NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_CountTransaction DEFAULT (0),
        TotalReturns DECIMAL(18,3) NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_TotalReturns DEFAULT (0),
        ReturnsCount INT NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_ReturnsCount DEFAULT (0),
        ClosingStatus NVARCHAR(50) NULL,

        IsDirty BIT NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_IsDirty DEFAULT (0),
        SummaryEngineVersion INT NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_EngineVersion DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_UpdatedAt DEFAULT (GETDATE()),
        SourceMinTransactionId INT NULL,
        SourceMaxTransactionId INT NULL,
        RebuildVersion INT NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_RebuildVersion DEFAULT (1),

        UserIdKey AS (ISNULL(UserId, (-1))) PERSISTED,
        ServiceTypeKey AS (ISNULL(ServiceType, N'')) PERSISTED,
        PaymentTypeKey AS (ISNULL(PaymentType, (-1))) PERSISTED,
        CashBoxIdKey AS (ISNULL(CashBoxId, (-1))) PERSISTED,
        StoreIdKey AS (ISNULL(StoreId, (-1))) PERSISTED
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.POS_DailyClosingSummary')
      AND name = N'UX_POS_DailyClosingSummary_Grain'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_POS_DailyClosingSummary_Grain
    ON dbo.POS_DailyClosingSummary
    (
        SummaryDate,
        BranchId,
        UserIdKey,
        ReportKey,
        ServiceTypeKey,
        PaymentTypeKey,
        CashBoxIdKey,
        StoreIdKey,
        SummaryEngineVersion
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.POS_DailyClosingSummary')
      AND name = N'IX_POS_DailyClosingSummary_Query'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_DailyClosingSummary_Query
    ON dbo.POS_DailyClosingSummary (SummaryDate, ReportKey, BranchId, SummaryEngineVersion)
    INCLUDE
    (
        UserId, TotalAmount, ReturnAmount, NetAmount, CashInAmount, CashOutAmount,
        CardAmount, OpenBalance, LastBalance, TotalRechargeValue, TotalRev, TotalVat,
        TotalRevVat, TotalSupply, BoxBalance, NoteValue, CountCards,
        CountTransaction, TotalReturns, ReturnsCount, ClosingStatus, IsDirty
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_DailyClosingSummary_RebuildLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DailyClosingSummary_RebuildLog
    (
        LogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_DailyClosingSummary_RebuildLog PRIMARY KEY,
        SummaryDate DATE NULL,
        BranchId INT NULL,
        UserId INT NULL,
        StartedAt DATETIME NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_RebuildLog_StartedAt DEFAULT (GETDATE()),
        FinishedAt DATETIME NULL,
        DurationMs INT NULL,
        Status NVARCHAR(30) NOT NULL,
        RowsDeleted INT NULL,
        RowsInserted INT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        SummaryEngineVersion INT NOT NULL CONSTRAINT DF_POS_DailyClosingSummary_RebuildLog_EngineVersion DEFAULT (1)
    );
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_RebuildDailyClosingSummary', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_RebuildDailyClosingSummary;
GO

CREATE PROCEDURE dbo.usp_POS_RebuildDailyClosingSummary
    @SummaryDate DATE,
    @BranchId INT = NULL,
    @UserId INT = NULL,
    @ForceRebuild BIT = 0,
    @SummaryEngineVersion INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @StartedAt DATETIME;
    DECLARE @LogId BIGINT;
    DECLARE @FromDate DATETIME;
    DECLARE @ToExclusive DATETIME;
    DECLARE @CurrentBranchId INT;
    DECLARE @EffectiveUserId INT;
    DECLARE @RowsDeleted INT;
    DECLARE @RowsInserted INT;
    DECLARE @RebuildVersion INT;

    SET @StartedAt = GETDATE();
    SET @RowsDeleted = 0;
    SET @RowsInserted = 0;
    SET @FromDate = CAST(@SummaryDate AS DATETIME);
    SET @ToExclusive = DATEADD(DAY, 1, @FromDate);
    SET @EffectiveUserId = ISNULL(@UserId, 0);

    IF @SummaryDate IS NULL
    BEGIN
        RAISERROR(N'@SummaryDate is required.', 16, 1);
        RETURN;
    END;

    IF OBJECT_ID(N'dbo.usp_POS_Report_RunClosing', N'P') IS NULL
    BEGIN
        RAISERROR(N'dbo.usp_POS_Report_RunClosing is required before rebuilding POS closing summary.', 16, 1);
        RETURN;
    END;

    INSERT INTO dbo.POS_DailyClosingSummary_RebuildLog
    (
        SummaryDate, BranchId, UserId, StartedAt, Status, SummaryEngineVersion
    )
    VALUES
    (
        @SummaryDate, @BranchId, @UserId, @StartedAt, N'Running', @SummaryEngineVersion
    );

    SET @LogId = SCOPE_IDENTITY();

    CREATE TABLE #Branches
    (
        BranchId INT NOT NULL PRIMARY KEY
    );

    IF @BranchId IS NOT NULL AND @BranchId > 0
    BEGIN
        INSERT INTO #Branches (BranchId) VALUES (@BranchId);
    END;
    ELSE
    BEGIN
        INSERT INTO #Branches (BranchId)
        SELECT DISTINCT x.BranchId
        FROM
        (
            SELECT c.BranchID AS BranchId
            FROM dbo.TBLClosePos c
            WHERE c.OrderDate >= @FromDate
              AND c.OrderDate < @ToExclusive
              AND c.BranchID IS NOT NULL

            UNION

            SELECT t.BranchId
            FROM dbo.Transactions t
            WHERE t.Transaction_Date >= @FromDate
              AND t.Transaction_Date < @ToExclusive
              AND t.BranchId IS NOT NULL
              AND t.Transaction_Type IN (21, 9)
        ) x;
    END;

    CREATE TABLE #FinanceClosing
    (
        BranchName NVARCHAR(255) NULL,
        ClosingDate DATETIME NULL,
        NoteID INT NULL,
        NoteSerial NVARCHAR(50) NULL,
        NoteSerial1 NVARCHAR(50) NULL,
        NoteDate DATETIME NULL,
        VoucherType NVARCHAR(100) NULL,
        OpenBalance DECIMAL(38, 6) NULL,
        LastBalance DECIMAL(38, 6) NULL,
        TotalRechargeValue DECIMAL(38, 6) NULL,
        TotalRev DECIMAL(38, 6) NULL,
        TotalVat DECIMAL(38, 6) NULL,
        CashOutTotal DECIMAL(38, 6) NULL,
        TotalSupply DECIMAL(38, 6) NULL,
        BoxBalance DECIMAL(38, 6) NULL,
        NoteValue DECIMAL(38, 6) NULL,
        UserName NVARCHAR(255) NULL
    );

    CREATE TABLE #FinanceClosingDiscounts
    (
        RowNo INT NULL,
        BranchID INT NULL,
        BranchName NVARCHAR(255) NULL,
        TotalSupply DECIMAL(38, 6) NULL,
        CountCards DECIMAL(38, 6) NULL,
        TotalSaleDay2Vat DECIMAL(38, 6) NULL,
        CardValue DECIMAL(38, 6) NULL,
        CountTransaction DECIMAL(38, 6) NULL,
        WalletBalance DECIMAL(38, 6) NULL,
        WalletSupply DECIMAL(38, 6) NULL,
        BankBalanceCharge DECIMAL(38, 6) NULL,
        TotalRechargeValue DECIMAL(38, 6) NULL,
        TotalRev2 DECIMAL(38, 6) NULL,
        TotalRevWithVat DECIMAL(38, 6) NULL,
        ReturnsCount INT NULL,
        TotalReturns DECIMAL(38, 6) NULL,
        NetCashOut DECIMAL(38, 6) NULL,
        BoxValue DECIMAL(38, 6) NULL,
        ClosingStatus NVARCHAR(50) NULL
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @RebuildVersion = ISNULL(MAX(RebuildVersion), 0) + 1
        FROM dbo.POS_DailyClosingSummary WITH (UPDLOCK, HOLDLOCK)
        WHERE SummaryDate = @SummaryDate
          AND (@BranchId IS NULL OR BranchId = @BranchId)
          AND SummaryEngineVersion = @SummaryEngineVersion;

        DELETE s
        FROM dbo.POS_DailyClosingSummary s
        WHERE s.SummaryDate = @SummaryDate
          AND s.SummaryEngineVersion = @SummaryEngineVersion
          AND s.ReportKey IN (N'finance-closing', N'finance-closing-discounts')
          AND (@BranchId IS NULL OR s.BranchId = @BranchId)
          AND
          (
              @UserId IS NULL
              OR s.UserId = @UserId
          );

        SET @RowsDeleted = @@ROWCOUNT;

        DECLARE branch_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT BranchId
            FROM #Branches
            ORDER BY BranchId;

        OPEN branch_cursor;
        FETCH NEXT FROM branch_cursor INTO @CurrentBranchId;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            TRUNCATE TABLE #FinanceClosing;
            TRUNCATE TABLE #FinanceClosingDiscounts;

            INSERT INTO #FinanceClosing
            EXEC dbo.usp_POS_Report_RunClosing
                @reportKey = N'finance-closing',
                @fromDate = @FromDate,
                @toDate = @FromDate,
                @branchId = @CurrentBranchId,
                @userId = @EffectiveUserId,
                @canChangeDefaults = 1,
                @branchFromId = NULL,
                @branchToId = NULL,
                @showEmptyBranches = 0,
                @serviceSearch = NULL,
                @filterUserId = @UserId;

            INSERT INTO #FinanceClosingDiscounts
            EXEC dbo.usp_POS_Report_RunClosing
                @reportKey = N'finance-closing-discounts',
                @fromDate = @FromDate,
                @toDate = @FromDate,
                @branchId = @CurrentBranchId,
                @userId = @EffectiveUserId,
                @canChangeDefaults = 1,
                @branchFromId = NULL,
                @branchToId = NULL,
                @showEmptyBranches = 0,
                @serviceSearch = NULL,
                @filterUserId = @UserId;

            IF EXISTS (SELECT 1 FROM #FinanceClosing)
            BEGIN
                INSERT INTO dbo.POS_DailyClosingSummary
                (
                    SummaryDate, BranchId, UserId, ReportKey, ServiceType, PaymentType, CashBoxId, StoreId,
                    InvoiceCount, TotalAmount, DiscountAmount, ReturnAmount, NetAmount,
                    CashInAmount, CashOutAmount, CardAmount, ViolationAmount,
                    OpenBalance, LastBalance, TotalRechargeValue, TotalRev, TotalVat,
                    TotalRevVat, TotalSupply, BoxBalance, NoteValue, CountCards,
                    CountTransaction, TotalReturns, ReturnsCount, ClosingStatus,
                    IsDirty, SummaryEngineVersion, CreatedAt, UpdatedAt,
                    SourceMinTransactionId, SourceMaxTransactionId, RebuildVersion
                )
                SELECT
                    @SummaryDate,
                    @CurrentBranchId,
                    @UserId,
                    N'finance-closing',
                    N'all',
                    NULL,
                    NULL,
                    NULL,
                    COUNT(1),
                    CAST(SUM(ISNULL(TotalSupply, 0)) AS DECIMAL(18, 3)),
                    0,
                    0,
                    CAST(SUM(ISNULL(NoteValue, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(TotalRechargeValue, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(CashOutTotal, 0)) AS DECIMAL(18, 3)),
                    0,
                    0,
                    CAST(SUM(ISNULL(OpenBalance, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(LastBalance, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(TotalRechargeValue, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(TotalRev, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(TotalVat, 0)) AS DECIMAL(18, 3)),
                    0,
                    CAST(SUM(ISNULL(TotalSupply, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(BoxBalance, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(NoteValue, 0)) AS DECIMAL(18, 3)),
                    0,
                    0,
                    0,
                    0,
                    NULL,
                    0,
                    @SummaryEngineVersion,
                    GETDATE(),
                    GETDATE(),
                    tx.SourceMinTransactionId,
                    tx.SourceMaxTransactionId,
                    @RebuildVersion
                FROM #FinanceClosing fc
                OUTER APPLY
                (
                    SELECT
                        MIN(t.Transaction_ID) AS SourceMinTransactionId,
                        MAX(t.Transaction_ID) AS SourceMaxTransactionId
                    FROM dbo.Transactions t
                    WHERE t.Transaction_Date >= @FromDate
                      AND t.Transaction_Date < @ToExclusive
                      AND t.BranchId = @CurrentBranchId
                      AND t.Transaction_Type IN (21, 9)
                      AND (@UserId IS NULL OR t.UserID = @UserId)
                ) tx
                GROUP BY tx.SourceMinTransactionId, tx.SourceMaxTransactionId;

                SET @RowsInserted = @RowsInserted + @@ROWCOUNT;
            END;

            IF EXISTS (SELECT 1 FROM #FinanceClosingDiscounts)
            BEGIN
                INSERT INTO dbo.POS_DailyClosingSummary
                (
                    SummaryDate, BranchId, UserId, ReportKey, ServiceType, PaymentType, CashBoxId, StoreId,
                    InvoiceCount, TotalAmount, DiscountAmount, ReturnAmount, NetAmount,
                    CashInAmount, CashOutAmount, CardAmount, ViolationAmount,
                    OpenBalance, LastBalance, TotalRechargeValue, TotalRev, TotalVat,
                    TotalRevVat, TotalSupply, BoxBalance, NoteValue, CountCards,
                    CountTransaction, TotalReturns, ReturnsCount, ClosingStatus,
                    IsDirty, SummaryEngineVersion, CreatedAt, UpdatedAt,
                    SourceMinTransactionId, SourceMaxTransactionId, RebuildVersion
                )
                SELECT
                    @SummaryDate,
                    @CurrentBranchId,
                    @UserId,
                    N'finance-closing-discounts',
                    N'all',
                    NULL,
                    NULL,
                    NULL,
                    SUM(ISNULL(ReturnsCount, 0)) + CAST(SUM(ISNULL(CountTransaction, 0)) AS INT),
                    CAST(SUM(ISNULL(TotalSupply, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(TotalRev2, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(TotalReturns, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(TotalSupply, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(TotalRechargeValue, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(NetCashOut, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(CardValue, 0)) AS DECIMAL(18, 3)),
                    0,
                    0,
                    0,
                    CAST(SUM(ISNULL(TotalRechargeValue, 0)) AS DECIMAL(18, 3)),
                    0,
                    0,
                    CAST(SUM(ISNULL(TotalRevWithVat, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(TotalSupply, 0)) AS DECIMAL(18, 3)),
                    CAST(SUM(ISNULL(BoxValue, 0)) AS DECIMAL(18, 3)),
                    0,
                    CAST(SUM(ISNULL(CountCards, 0)) AS INT),
                    CAST(SUM(ISNULL(CountTransaction, 0)) AS INT),
                    CAST(SUM(ISNULL(TotalReturns, 0)) AS DECIMAL(18, 3)),
                    SUM(ISNULL(ReturnsCount, 0)),
                    CASE
                        WHEN MIN(ISNULL(ClosingStatus, N'')) = MAX(ISNULL(ClosingStatus, N'')) THEN MAX(ClosingStatus)
                        ELSE N'Partial'
                    END,
                    0,
                    @SummaryEngineVersion,
                    GETDATE(),
                    GETDATE(),
                    tx.SourceMinTransactionId,
                    tx.SourceMaxTransactionId,
                    @RebuildVersion
                FROM #FinanceClosingDiscounts fd
                OUTER APPLY
                (
                    SELECT
                        MIN(t.Transaction_ID) AS SourceMinTransactionId,
                        MAX(t.Transaction_ID) AS SourceMaxTransactionId
                    FROM dbo.Transactions t
                    WHERE t.Transaction_Date >= @FromDate
                      AND t.Transaction_Date < @ToExclusive
                      AND t.BranchId = @CurrentBranchId
                      AND t.Transaction_Type IN (21, 9)
                      AND (@UserId IS NULL OR t.UserID = @UserId)
                ) tx
                GROUP BY tx.SourceMinTransactionId, tx.SourceMaxTransactionId;

                SET @RowsInserted = @RowsInserted + @@ROWCOUNT;
            END;

            FETCH NEXT FROM branch_cursor INTO @CurrentBranchId;
        END;

        CLOSE branch_cursor;
        DEALLOCATE branch_cursor;

        UPDATE dbo.POS_DailyClosingSummary_RebuildLog
        SET FinishedAt = GETDATE(),
            DurationMs = DATEDIFF(MILLISECOND, @StartedAt, GETDATE()),
            Status = N'Succeeded',
            RowsDeleted = @RowsDeleted,
            RowsInserted = @RowsInserted
        WHERE LogId = @LogId;

        COMMIT TRANSACTION;

        SELECT
            @SummaryDate AS SummaryDate,
            @BranchId AS BranchId,
            @UserId AS UserId,
            @RowsDeleted AS RowsDeleted,
            @RowsInserted AS RowsInserted,
            @RebuildVersion AS RebuildVersion,
            @SummaryEngineVersion AS SummaryEngineVersion;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'branch_cursor') >= 0
        BEGIN
            CLOSE branch_cursor;
        END;

        IF CURSOR_STATUS('local', 'branch_cursor') > -3
        BEGIN
            DEALLOCATE branch_cursor;
        END;

        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        UPDATE dbo.POS_DailyClosingSummary_RebuildLog
        SET FinishedAt = GETDATE(),
            DurationMs = DATEDIFF(MILLISECOND, @StartedAt, GETDATE()),
            Status = N'Failed',
            RowsDeleted = @RowsDeleted,
            RowsInserted = @RowsInserted,
            ErrorMessage = ERROR_MESSAGE()
        WHERE LogId = @LogId;

        DECLARE @ErrorMessage NVARCHAR(4000);
        DECLARE @ErrorSeverity INT;
        DECLARE @ErrorState INT;

        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_GetClosingSummary', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_GetClosingSummary;
GO

CREATE PROCEDURE dbo.usp_POS_GetClosingSummary
    @FromDate DATE,
    @ToDate DATE,
    @BranchId INT = NULL,
    @UserId INT = NULL,
    @ReportKey NVARCHAR(50) = NULL,
    @SummaryEngineVersion INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME;
    DECLARE @ToExclusive DATETIME;

    IF @FromDate IS NULL OR @ToDate IS NULL
    BEGIN
        RAISERROR(N'@FromDate and @ToDate are required.', 16, 1);
        RETURN;
    END;

    SET @From = CAST(@FromDate AS DATETIME);
    SET @ToExclusive = DATEADD(DAY, 1, CAST(@ToDate AS DATETIME));

    IF @ToExclusive < @From
    BEGIN
        DECLARE @Swap DATETIME;
        SET @Swap = @From;
        SET @From = DATEADD(DAY, -1, @ToExclusive);
        SET @ToExclusive = DATEADD(DAY, 1, @Swap);
    END;

    SELECT
        s.ReportKey,
        s.BranchId,
        MIN(s.SummaryDate) AS FromSummaryDate,
        MAX(s.SummaryDate) AS ToSummaryDate,
        COUNT(1) AS SummaryRows,
        SUM(s.InvoiceCount) AS InvoiceCount,
        SUM(s.TotalAmount) AS TotalAmount,
        SUM(s.DiscountAmount) AS DiscountAmount,
        SUM(s.ReturnAmount) AS ReturnAmount,
        SUM(s.NetAmount) AS NetAmount,
        SUM(s.CashInAmount) AS CashInAmount,
        SUM(s.CashOutAmount) AS CashOutAmount,
        SUM(s.CardAmount) AS CardAmount,
        SUM(s.ViolationAmount) AS ViolationAmount,
        SUM(s.OpenBalance) AS OpenBalance,
        SUM(s.LastBalance) AS LastBalance,
        SUM(s.TotalRechargeValue) AS TotalRechargeValue,
        SUM(s.TotalRev) AS TotalRev,
        SUM(s.TotalVat) AS TotalVat,
        SUM(s.TotalRevVat) AS TotalRevVat,
        SUM(s.TotalSupply) AS TotalSupply,
        SUM(s.BoxBalance) AS BoxBalance,
        SUM(s.NoteValue) AS NoteValue,
        SUM(s.CountCards) AS CountCards,
        SUM(s.CountTransaction) AS CountTransaction,
        SUM(s.TotalReturns) AS TotalReturns,
        SUM(s.ReturnsCount) AS ReturnsCount,
        MIN(s.SourceMinTransactionId) AS SourceMinTransactionId,
        MAX(s.SourceMaxTransactionId) AS SourceMaxTransactionId,
        MAX(s.RebuildVersion) AS MaxRebuildVersion,
        MAX(s.UpdatedAt) AS LastUpdatedAt,
        SUM(CASE WHEN s.IsDirty = 1 THEN 1 ELSE 0 END) AS DirtyRows
    FROM dbo.POS_DailyClosingSummary s
    WHERE s.SummaryDate >= @From
      AND s.SummaryDate < @ToExclusive
      AND s.SummaryEngineVersion = @SummaryEngineVersion
      AND (@ReportKey IS NULL OR s.ReportKey = @ReportKey)
      AND (@BranchId IS NULL OR @BranchId <= 0 OR s.BranchId = @BranchId)
      AND (@UserId IS NULL OR s.UserId = @UserId)
    GROUP BY s.ReportKey, s.BranchId
    ORDER BY s.ReportKey, s.BranchId;
END;
GO

/*
Validation examples only. Do not run automatically.

-- Rebuild last 7 days for BranchId = 22:
DECLARE @d DATE;
SET @d = DATEADD(DAY, -6, CAST(GETDATE() AS DATE));
WHILE @d <= CAST(GETDATE() AS DATE)
BEGIN
    EXEC dbo.usp_POS_RebuildDailyClosingSummary
        @SummaryDate = @d,
        @BranchId = 22,
        @UserId = NULL,
        @ForceRebuild = 1,
        @SummaryEngineVersion = 1;

    SET @d = DATEADD(DAY, 1, @d);
END;

-- Query summary for BranchId = 22:
EXEC dbo.usp_POS_GetClosingSummary
    @FromDate = DATEADD(DAY, -6, CAST(GETDATE() AS DATE)),
    @ToDate = CAST(GETDATE() AS DATE),
    @BranchId = 22,
    @ReportKey = NULL,
    @SummaryEngineVersion = 1;

-- Compare raw current report for finance-closing, today:
EXEC dbo.usp_POS_Report_RunClosing
    @reportKey = N'finance-closing',
    @fromDate = CAST(GETDATE() AS DATE),
    @toDate = CAST(GETDATE() AS DATE),
    @branchId = 22,
    @userId = 0,
    @canChangeDefaults = 1;

-- Compare raw current report for finance-closing-discounts, last 7 days:
EXEC dbo.usp_POS_Report_RunClosing
    @reportKey = N'finance-closing-discounts',
    @fromDate = DATEADD(DAY, -6, CAST(GETDATE() AS DATE)),
    @toDate = CAST(GETDATE() AS DATE),
    @branchId = 22,
    @userId = 0,
    @canChangeDefaults = 1;

-- Current month start without EOMONTH for SQL Server 2012:
DECLARE @monthStart DATE;
SET @monthStart = DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE()), 0);

EXEC dbo.usp_POS_Report_RunClosing
    @reportKey = N'finance-closing-discounts',
    @fromDate = @monthStart,
    @toDate = CAST(GETDATE() AS DATE),
    @branchId = 22,
    @userId = 0,
    @canChangeDefaults = 1;
*/
