/*
    POS Dashboard Daily KPI Snapshots
    Scope: daily snapshots only.
    SQL Server 2012 compatible.

    Apply after:
      27_POS_ReportStoredProcedures.sql
      34_POS_PerformanceStoredProcedures.sql

    Notes:
    - Weekly/monthly/yearly snapshots are intentionally not implemented here.
    - This script does not add indexes to live POS transaction tables.
    - Rerunning generation for the same day/filter replaces the previous snapshot safely.
*/

IF OBJECT_ID(N'dbo.POS_DashboardSnapshotHeader', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DashboardSnapshotHeader
    (
        SnapshotId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_DashboardSnapshotHeader PRIMARY KEY,
        PeriodType NVARCHAR(20) NOT NULL,
        PeriodStart DATE NOT NULL,
        PeriodEnd DATE NOT NULL,
        BranchId INT NULL,
        OperationType NVARCHAR(30) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotHeader_OperationType DEFAULT (N''),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotHeader_Status DEFAULT (N'Running'),
        GeneratedAt DATETIME NULL,
        GeneratedByUserId INT NULL,
        Message NVARCHAR(500) NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_POS_DashboardSnapshotHeader_Filter' AND object_id = OBJECT_ID(N'dbo.POS_DashboardSnapshotHeader'))
BEGIN
    CREATE UNIQUE INDEX UX_POS_DashboardSnapshotHeader_Filter
    ON dbo.POS_DashboardSnapshotHeader(PeriodType, PeriodStart, PeriodEnd, BranchId, OperationType);
END;
GO

IF OBJECT_ID(N'dbo.POS_DashboardSnapshotKpi', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DashboardSnapshotKpi
    (
        SnapshotId INT NOT NULL,
        ScopeName NVARCHAR(20) NOT NULL,
        TransactionCount INT NOT NULL CONSTRAINT DF_POS_DashboardSnapshotKpi_TransactionCount DEFAULT (0),
        SalesTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotKpi_SalesTotal DEFAULT (0),
        FeesTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotKpi_FeesTotal DEFAULT (0),
        VatTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotKpi_VatTotal DEFAULT (0),
        NetCollection DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotKpi_NetCollection DEFAULT (0),
        ActivatedKycCards INT NOT NULL CONSTRAINT DF_POS_DashboardSnapshotKpi_ActivatedKycCards DEFAULT (0),
        CancelledOrReturnedCount INT NOT NULL CONSTRAINT DF_POS_DashboardSnapshotKpi_CancelledOrReturnedCount DEFAULT (0),
        CONSTRAINT PK_POS_DashboardSnapshotKpi PRIMARY KEY (SnapshotId, ScopeName)
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_DashboardSnapshotBranch', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DashboardSnapshotBranch
    (
        SnapshotId INT NOT NULL,
        SectionName NVARCHAR(20) NOT NULL,
        RankNo INT NOT NULL,
        BranchId INT NULL,
        BranchName NVARCHAR(200) NULL,
        TransactionCount INT NOT NULL CONSTRAINT DF_POS_DashboardSnapshotBranch_TransactionCount DEFAULT (0),
        TotalValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotBranch_TotalValue DEFAULT (0),
        FeesTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotBranch_FeesTotal DEFAULT (0),
        CONSTRAINT PK_POS_DashboardSnapshotBranch PRIMARY KEY (SnapshotId, SectionName, RankNo)
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_DashboardSnapshotService', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DashboardSnapshotService
    (
        SnapshotId INT NOT NULL,
        RankNo INT NOT NULL,
        Item_ID INT NULL,
        ItemName NVARCHAR(200) NULL,
        SaleCount INT NOT NULL CONSTRAINT DF_POS_DashboardSnapshotService_SaleCount DEFAULT (0),
        TotalValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotService_TotalValue DEFAULT (0),
        FeesTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotService_FeesTotal DEFAULT (0),
        CONSTRAINT PK_POS_DashboardSnapshotService PRIMARY KEY (SnapshotId, RankNo)
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_DashboardSnapshotOperation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DashboardSnapshotOperation
    (
        SnapshotId INT NOT NULL,
        OperationType NVARCHAR(30) NOT NULL,
        TransactionCount INT NOT NULL CONSTRAINT DF_POS_DashboardSnapshotOperation_TransactionCount DEFAULT (0),
        RechargeTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotOperation_RechargeTotal DEFAULT (0),
        FeesTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotOperation_FeesTotal DEFAULT (0),
        VatTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotOperation_VatTotal DEFAULT (0),
        NetCollection DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotOperation_NetCollection DEFAULT (0),
        CONSTRAINT PK_POS_DashboardSnapshotOperation PRIMARY KEY (SnapshotId, OperationType)
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_DashboardSnapshotTrend', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DashboardSnapshotTrend
    (
        SnapshotId INT NOT NULL,
        DayValue VARCHAR(10) NOT NULL,
        TransactionCount INT NOT NULL CONSTRAINT DF_POS_DashboardSnapshotTrend_TransactionCount DEFAULT (0),
        NetCollection DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotTrend_NetCollection DEFAULT (0),
        CONSTRAINT PK_POS_DashboardSnapshotTrend PRIMARY KEY (SnapshotId, DayValue)
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_DashboardSnapshotSeller', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DashboardSnapshotSeller
    (
        SnapshotId INT NOT NULL,
        RankNo INT NOT NULL,
        SellerId INT NULL,
        SellerName NVARCHAR(200) NULL,
        TransactionCount INT NOT NULL CONSTRAINT DF_POS_DashboardSnapshotSeller_TransactionCount DEFAULT (0),
        NetValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotSeller_NetValue DEFAULT (0),
        CONSTRAINT PK_POS_DashboardSnapshotSeller PRIMARY KEY (SnapshotId, RankNo)
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_DashboardSnapshotSmartMetric', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DashboardSnapshotSmartMetric
    (
        SnapshotId INT NOT NULL,
        MetricType NVARCHAR(20) NOT NULL,
        EntityId INT NULL,
        EntityName NVARCHAR(200) NULL,
        CurrentCount INT NOT NULL CONSTRAINT DF_POS_DashboardSnapshotSmartMetric_CurrentCount DEFAULT (0),
        CurrentValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotSmartMetric_CurrentValue DEFAULT (0),
        CurrentFees DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotSmartMetric_CurrentFees DEFAULT (0),
        PreviousCount INT NOT NULL CONSTRAINT DF_POS_DashboardSnapshotSmartMetric_PreviousCount DEFAULT (0),
        PreviousValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotSmartMetric_PreviousValue DEFAULT (0),
        PreviousFees DECIMAL(18,2) NOT NULL CONSTRAINT DF_POS_DashboardSnapshotSmartMetric_PreviousFees DEFAULT (0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_DashboardSnapshotSmartMetric' AND object_id = OBJECT_ID(N'dbo.POS_DashboardSnapshotSmartMetric'))
BEGIN
    CREATE INDEX IX_POS_DashboardSnapshotSmartMetric
    ON dbo.POS_DashboardSnapshotSmartMetric(SnapshotId, MetricType);
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_DashboardSnapshot_GenerateDaily', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_DashboardSnapshot_GenerateDaily;
GO

CREATE PROCEDURE dbo.usp_POS_DashboardSnapshot_GenerateDaily
    @snapshotDate DATETIME,
    @branchId INT = NULL,
    @operationType NVARCHAR(30) = N'',
    @generatedByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @snapshotId INT;
    DECLARE @from DATETIME = CONVERT(DATE, @snapshotDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @snapshotDate));
    DECLARE @previousFrom DATETIME = DATEADD(DAY, -1, CONVERT(DATE, @snapshotDate));
    DECLARE @previousToExclusive DATETIME = CONVERT(DATE, @snapshotDate);
    SET @operationType = LTRIM(RTRIM(ISNULL(@operationType, N'')));

    BEGIN TRY
        IF OBJECT_ID('tempdb..#OldDashboardSnapshots') IS NOT NULL DROP TABLE #OldDashboardSnapshots;

        SELECT h.SnapshotId
        INTO #OldDashboardSnapshots
        FROM dbo.POS_DashboardSnapshotHeader h
        WHERE h.PeriodType = N'daily'
          AND h.PeriodStart = CONVERT(DATE, @snapshotDate)
          AND h.PeriodEnd = CONVERT(DATE, @snapshotDate)
          AND ISNULL(h.BranchId, -1) = ISNULL(@branchId, -1)
          AND h.OperationType = @operationType;

        DELETE d FROM dbo.POS_DashboardSnapshotSmartMetric d INNER JOIN #OldDashboardSnapshots o ON o.SnapshotId = d.SnapshotId;
        DELETE d FROM dbo.POS_DashboardSnapshotSeller d INNER JOIN #OldDashboardSnapshots o ON o.SnapshotId = d.SnapshotId;
        DELETE d FROM dbo.POS_DashboardSnapshotTrend d INNER JOIN #OldDashboardSnapshots o ON o.SnapshotId = d.SnapshotId;
        DELETE d FROM dbo.POS_DashboardSnapshotOperation d INNER JOIN #OldDashboardSnapshots o ON o.SnapshotId = d.SnapshotId;
        DELETE d FROM dbo.POS_DashboardSnapshotService d INNER JOIN #OldDashboardSnapshots o ON o.SnapshotId = d.SnapshotId;
        DELETE d FROM dbo.POS_DashboardSnapshotBranch d INNER JOIN #OldDashboardSnapshots o ON o.SnapshotId = d.SnapshotId;
        DELETE d FROM dbo.POS_DashboardSnapshotKpi d INNER JOIN #OldDashboardSnapshots o ON o.SnapshotId = d.SnapshotId;
        DELETE h FROM dbo.POS_DashboardSnapshotHeader h INNER JOIN #OldDashboardSnapshots o ON o.SnapshotId = h.SnapshotId;

        INSERT INTO dbo.POS_DashboardSnapshotHeader
        (
            PeriodType, PeriodStart, PeriodEnd, BranchId, OperationType,
            Status, GeneratedAt, GeneratedByUserId, Message
        )
        VALUES
        (
            N'daily', CONVERT(DATE, @snapshotDate), CONVERT(DATE, @snapshotDate), @branchId, @operationType,
            N'Running', GETDATE(), @generatedByUserId, N'جاري تجهيز مؤشرات اليوم'
        );

        SET @snapshotId = SCOPE_IDENTITY();

        IF OBJECT_ID('tempdb..#PosTransactions') IS NOT NULL DROP TABLE #PosTransactions;
        IF OBJECT_ID('tempdb..#PreviousPosTransactions') IS NOT NULL DROP TABLE #PreviousPosTransactions;

        SELECT
            t.Transaction_ID,
            t.UserID,
            t.BranchId,
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(50), t.BranchId)) AS BranchName,
            CASE
                WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card'
                WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
                ELSE N'cash-in'
            END AS OperationType,
            ISNULL(t.RechargeValue, 0) AS RechargeValue,
            ISNULL(t.NetValue, 0) AS FeesValue,
            ISNULL(t.Vat, 0) AS VatValue,
            ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0)) AS NetCollection
        INTO #PosTransactions
        FROM dbo.Transactions t
        INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND ISNULL(b.IsStoped, 0) = 0
          AND (@branchId IS NULL OR t.BranchId = @branchId)
          AND
          (
              @operationType = N''
              OR
              CASE
                  WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                  WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card'
                  WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
                  ELSE N'cash-in'
              END = @operationType
          );

        SELECT
            t.Transaction_ID,
            t.UserID,
            t.BranchId,
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(50), t.BranchId)) AS BranchName,
            CASE
                WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card'
                WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
                ELSE N'cash-in'
            END AS OperationType,
            ISNULL(t.RechargeValue, 0) AS RechargeValue,
            ISNULL(t.NetValue, 0) AS FeesValue,
            ISNULL(t.Vat, 0) AS VatValue,
            ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0)) AS NetCollection
        INTO #PreviousPosTransactions
        FROM dbo.Transactions t
        INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @previousFrom
          AND t.Transaction_Date < @previousToExclusive
          AND ISNULL(b.IsStoped, 0) = 0
          AND (@branchId IS NULL OR t.BranchId = @branchId)
          AND
          (
              @operationType = N''
              OR
              CASE
                  WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                  WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card'
                  WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
                  ELSE N'cash-in'
              END = @operationType
          );

        INSERT INTO dbo.POS_DashboardSnapshotKpi
        (
            SnapshotId, ScopeName, TransactionCount, SalesTotal, FeesTotal, VatTotal,
            NetCollection, ActivatedKycCards, CancelledOrReturnedCount
        )
        SELECT
            @snapshotId,
            N'Current',
            COUNT(1),
            ISNULL(SUM(CASE WHEN OperationType = N'card' THEN NetCollection ELSE RechargeValue END), 0),
            ISNULL(SUM(FeesValue), 0),
            ISNULL(SUM(VatValue), 0),
            ISNULL(SUM(NetCollection), 0),
            ISNULL((SELECT COUNT(DISTINCT c.Id)
                    FROM dbo.TblCusCsh c
                    WHERE ISNULL(c.EasyCashType, 0) = 0
                      AND (@operationType = N'' OR @operationType = N'card')
                      AND (@branchId IS NULL OR c.BranchID = @branchId)
                      AND EXISTS
                      (
                          SELECT 1
                          FROM dbo.Transactions kt
                          INNER JOIN dbo.TblBranchesData kb ON kb.branch_id = kt.BranchId
                          WHERE kt.Transaction_Type = 21
                            AND kt.Transaction_Date >= @from
                            AND kt.Transaction_Date < @toExclusive
                            AND ISNULL(kb.IsStoped, 0) = 0
                            AND (@branchId IS NULL OR kt.BranchId = @branchId)
                            AND (ISNULL(kt.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(kt.VisaNumber, N''))), N'') IS NOT NULL)
                            AND NULLIF(LTRIM(RTRIM(ISNULL(kt.VisaNumber, N''))), N'') IS NOT NULL
                            AND
                            (
                                LTRIM(RTRIM(ISNULL(kt.VisaNumber, N''))) = LTRIM(RTRIM(ISNULL(c.CardNo, N'')))
                                OR LTRIM(RTRIM(ISNULL(kt.VisaNumber, N''))) = LTRIM(RTRIM(ISNULL(c.CardId, N'')))
                            )
                      )), 0),
            ISNULL((SELECT COUNT(1)
                    FROM dbo.Transactions r
                    INNER JOIN dbo.TblBranchesData rb ON rb.branch_id = r.BranchId
                    WHERE r.Transaction_Type = 9
                      AND r.Transaction_Date >= @from
                      AND r.Transaction_Date < @toExclusive
                      AND ISNULL(rb.IsStoped, 0) = 0
                      AND (@branchId IS NULL OR r.BranchId = @branchId)), 0)
        FROM #PosTransactions;

        INSERT INTO dbo.POS_DashboardSnapshotKpi
        (
            SnapshotId, ScopeName, TransactionCount, SalesTotal, FeesTotal, VatTotal,
            NetCollection, ActivatedKycCards, CancelledOrReturnedCount
        )
        SELECT
            @snapshotId,
            N'Previous',
            COUNT(1),
            ISNULL(SUM(CASE WHEN OperationType = N'card' THEN NetCollection ELSE RechargeValue END), 0),
            ISNULL(SUM(FeesValue), 0),
            ISNULL(SUM(VatValue), 0),
            ISNULL(SUM(NetCollection), 0),
            ISNULL((SELECT COUNT(DISTINCT c.Id)
                    FROM dbo.TblCusCsh c
                    WHERE ISNULL(c.EasyCashType, 0) = 0
                      AND (@operationType = N'' OR @operationType = N'card')
                      AND (@branchId IS NULL OR c.BranchID = @branchId)
                      AND EXISTS
                      (
                          SELECT 1
                          FROM dbo.Transactions kt
                          INNER JOIN dbo.TblBranchesData kb ON kb.branch_id = kt.BranchId
                          WHERE kt.Transaction_Type = 21
                            AND kt.Transaction_Date >= @previousFrom
                            AND kt.Transaction_Date < @previousToExclusive
                            AND ISNULL(kb.IsStoped, 0) = 0
                            AND (@branchId IS NULL OR kt.BranchId = @branchId)
                            AND (ISNULL(kt.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(kt.VisaNumber, N''))), N'') IS NOT NULL)
                            AND NULLIF(LTRIM(RTRIM(ISNULL(kt.VisaNumber, N''))), N'') IS NOT NULL
                            AND
                            (
                                LTRIM(RTRIM(ISNULL(kt.VisaNumber, N''))) = LTRIM(RTRIM(ISNULL(c.CardNo, N'')))
                                OR LTRIM(RTRIM(ISNULL(kt.VisaNumber, N''))) = LTRIM(RTRIM(ISNULL(c.CardId, N'')))
                            )
                      )), 0),
            ISNULL((SELECT COUNT(1)
                    FROM dbo.Transactions r
                    INNER JOIN dbo.TblBranchesData rb ON rb.branch_id = r.BranchId
                    WHERE r.Transaction_Type = 9
                      AND r.Transaction_Date >= @previousFrom
                      AND r.Transaction_Date < @previousToExclusive
                      AND ISNULL(rb.IsStoped, 0) = 0
                      AND (@branchId IS NULL OR r.BranchId = @branchId)), 0)
        FROM #PreviousPosTransactions;

        INSERT INTO dbo.POS_DashboardSnapshotBranch(SnapshotId, SectionName, RankNo, BranchId, BranchName, TransactionCount, TotalValue, FeesTotal)
        SELECT @snapshotId, N'Top', ROW_NUMBER() OVER (ORDER BY SUM(NetCollection) DESC, COUNT(1) DESC), BranchId, BranchName, COUNT(1), ISNULL(SUM(NetCollection), 0), ISNULL(SUM(FeesValue), 0)
        FROM #PosTransactions
        GROUP BY BranchId, BranchName
        HAVING COUNT(1) > 0 AND SUM(NetCollection) > 0
        ORDER BY SUM(NetCollection) DESC, COUNT(1) DESC;

        DELETE FROM dbo.POS_DashboardSnapshotBranch WHERE SnapshotId = @snapshotId AND SectionName = N'Top' AND RankNo > 5;

        INSERT INTO dbo.POS_DashboardSnapshotBranch(SnapshotId, SectionName, RankNo, BranchId, BranchName, TransactionCount, TotalValue, FeesTotal)
        SELECT @snapshotId, N'Worst', ROW_NUMBER() OVER (ORDER BY SUM(NetCollection) ASC, COUNT(1) ASC), BranchId, BranchName, COUNT(1), ISNULL(SUM(NetCollection), 0), ISNULL(SUM(FeesValue), 0)
        FROM #PosTransactions
        GROUP BY BranchId, BranchName
        HAVING COUNT(1) > 0 AND SUM(NetCollection) > 0
        ORDER BY SUM(NetCollection) ASC, COUNT(1) ASC;

        DELETE FROM dbo.POS_DashboardSnapshotBranch WHERE SnapshotId = @snapshotId AND SectionName = N'Worst' AND RankNo > 5;

        INSERT INTO dbo.POS_DashboardSnapshotService(SnapshotId, RankNo, Item_ID, ItemName, SaleCount, TotalValue, FeesTotal)
        SELECT TOP (10)
            @snapshotId,
            ROW_NUMBER() OVER (ORDER BY COUNT(1) DESC, SUM(ISNULL(d.TotalPrice, ISNULL(d.Price, 0) + ISNULL(d.Vat, 0))) DESC),
            d.Item_ID,
            COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID)),
            COUNT(1),
            ISNULL(SUM(ISNULL(d.TotalPrice, ISNULL(d.Price, 0) + ISNULL(d.Vat, 0))), 0),
            ISNULL(SUM(ISNULL(d.showPrice, ISNULL(d.Price, 0))), 0)
        FROM dbo.Transaction_Details d
        INNER JOIN dbo.Transactions t ON t.Transaction_ID = d.Transaction_ID
        INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
        LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND ISNULL(b.IsStoped, 0) = 0
          AND (@branchId IS NULL OR t.BranchId = @branchId)
          AND (@operationType = N'' OR CASE WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations' WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card' WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out' ELSE N'cash-in' END = @operationType)
        GROUP BY d.Item_ID, COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID))
        ORDER BY COUNT(1) DESC, SUM(ISNULL(d.TotalPrice, ISNULL(d.Price, 0) + ISNULL(d.Vat, 0))) DESC;

        INSERT INTO dbo.POS_DashboardSnapshotOperation(SnapshotId, OperationType, TransactionCount, RechargeTotal, FeesTotal, VatTotal, NetCollection)
        SELECT @snapshotId, OperationType, COUNT(1), ISNULL(SUM(RechargeValue), 0), ISNULL(SUM(FeesValue), 0), ISNULL(SUM(VatValue), 0), ISNULL(SUM(NetCollection), 0)
        FROM #PosTransactions
        GROUP BY OperationType;

        INSERT INTO dbo.POS_DashboardSnapshotTrend(SnapshotId, DayValue, TransactionCount, NetCollection)
        SELECT @snapshotId, CONVERT(VARCHAR(10), @from, 120), COUNT(1), ISNULL(SUM(NetCollection), 0)
        FROM #PosTransactions;

        INSERT INTO dbo.POS_DashboardSnapshotSeller(SnapshotId, RankNo, SellerId, SellerName, TransactionCount, NetValue)
        SELECT TOP (5)
            @snapshotId,
            ROW_NUMBER() OVER (ORDER BY SUM(p.NetCollection) DESC, COUNT(1) DESC),
            p.UserID,
            COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), p.UserID)),
            COUNT(1),
            ISNULL(SUM(p.NetCollection), 0)
        FROM #PosTransactions p
        INNER JOIN dbo.TblUsers u ON u.UserID = p.UserID
        LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
        WHERE p.UserID IS NOT NULL
          AND ISNULL(u.isDeactivated, 0) = 0
        GROUP BY p.UserID, COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), p.UserID))
        ORDER BY SUM(p.NetCollection) DESC, COUNT(1) DESC;

        INSERT INTO dbo.POS_DashboardSnapshotSmartMetric(SnapshotId, MetricType, EntityId, EntityName, CurrentCount, CurrentValue, CurrentFees, PreviousCount, PreviousValue, PreviousFees)
        SELECT @snapshotId, N'Branch', COALESCE(c.BranchId, p.BranchId), COALESCE(c.BranchName, p.BranchName),
               ISNULL(c.CurrentCount, 0), ISNULL(c.CurrentValue, 0), ISNULL(c.CurrentFees, 0),
               ISNULL(p.PreviousCount, 0), ISNULL(p.PreviousValue, 0), ISNULL(p.PreviousFees, 0)
        FROM
        (
            SELECT BranchId, BranchName, COUNT(1) CurrentCount, SUM(NetCollection) CurrentValue, SUM(FeesValue) CurrentFees
            FROM #PosTransactions GROUP BY BranchId, BranchName
        ) c
        FULL OUTER JOIN
        (
            SELECT BranchId, BranchName, COUNT(1) PreviousCount, SUM(NetCollection) PreviousValue, SUM(FeesValue) PreviousFees
            FROM #PreviousPosTransactions GROUP BY BranchId, BranchName
        ) p ON p.BranchId = c.BranchId
        WHERE ISNULL(c.CurrentCount, 0) > 0 OR ISNULL(p.PreviousCount, 0) > 0;

        INSERT INTO dbo.POS_DashboardSnapshotSmartMetric(SnapshotId, MetricType, EntityId, EntityName, CurrentCount, CurrentValue, CurrentFees, PreviousCount, PreviousValue, PreviousFees)
        SELECT @snapshotId, N'Service', COALESCE(c.Item_ID, p.Item_ID), COALESCE(c.ItemName, p.ItemName),
               ISNULL(c.CurrentCount, 0), ISNULL(c.CurrentValue, 0), ISNULL(c.CurrentFees, 0),
               ISNULL(p.PreviousCount, 0), ISNULL(p.PreviousValue, 0), ISNULL(p.PreviousFees, 0)
        FROM
        (
            SELECT d.Item_ID, COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID)) ItemName,
                   COUNT(1) CurrentCount, SUM(ISNULL(d.TotalPrice, ISNULL(d.Price, 0) + ISNULL(d.Vat, 0))) CurrentValue, SUM(ISNULL(d.showPrice, ISNULL(d.Price, 0))) CurrentFees
            FROM dbo.Transaction_Details d
            INNER JOIN dbo.Transactions t ON t.Transaction_ID = d.Transaction_ID
            INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
            LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
            WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @from AND t.Transaction_Date < @toExclusive AND ISNULL(b.IsStoped, 0) = 0
              AND (@branchId IS NULL OR t.BranchId = @branchId)
              AND (@operationType = N'' OR CASE WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations' WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card' WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out' ELSE N'cash-in' END = @operationType)
            GROUP BY d.Item_ID, COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID))
        ) c
        FULL OUTER JOIN
        (
            SELECT d.Item_ID, COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID)) ItemName,
                   COUNT(1) PreviousCount, SUM(ISNULL(d.TotalPrice, ISNULL(d.Price, 0) + ISNULL(d.Vat, 0))) PreviousValue, SUM(ISNULL(d.showPrice, ISNULL(d.Price, 0))) PreviousFees
            FROM dbo.Transaction_Details d
            INNER JOIN dbo.Transactions t ON t.Transaction_ID = d.Transaction_ID
            INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
            LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
            WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @previousFrom AND t.Transaction_Date < @previousToExclusive AND ISNULL(b.IsStoped, 0) = 0
              AND (@branchId IS NULL OR t.BranchId = @branchId)
              AND (@operationType = N'' OR CASE WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations' WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card' WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out' ELSE N'cash-in' END = @operationType)
            GROUP BY d.Item_ID, COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID))
        ) p ON p.Item_ID = c.Item_ID
        WHERE ISNULL(c.CurrentCount, 0) > 0 OR ISNULL(p.PreviousCount, 0) > 0;

        INSERT INTO dbo.POS_DashboardSnapshotSmartMetric(SnapshotId, MetricType, EntityId, EntityName, CurrentCount, CurrentValue, CurrentFees, PreviousCount, PreviousValue, PreviousFees)
        SELECT @snapshotId, N'Seller', COALESCE(c.UserID, p.UserID), COALESCE(c.SellerName, p.SellerName),
               ISNULL(c.CurrentCount, 0), ISNULL(c.CurrentValue, 0), ISNULL(c.CurrentFees, 0),
               ISNULL(p.PreviousCount, 0), ISNULL(p.PreviousValue, 0), ISNULL(p.PreviousFees, 0)
        FROM
        (
            SELECT pt.UserID, COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), pt.UserID)) SellerName,
                   COUNT(1) CurrentCount, SUM(pt.NetCollection) CurrentValue, SUM(pt.FeesValue) CurrentFees
            FROM #PosTransactions pt
            INNER JOIN dbo.TblUsers u ON u.UserID = pt.UserID
            LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
            WHERE pt.UserID IS NOT NULL AND ISNULL(u.isDeactivated, 0) = 0
            GROUP BY pt.UserID, COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), pt.UserID))
        ) c
        FULL OUTER JOIN
        (
            SELECT pt.UserID, COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), pt.UserID)) SellerName,
                   COUNT(1) PreviousCount, SUM(pt.NetCollection) PreviousValue, SUM(pt.FeesValue) PreviousFees
            FROM #PreviousPosTransactions pt
            INNER JOIN dbo.TblUsers u ON u.UserID = pt.UserID
            LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
            WHERE pt.UserID IS NOT NULL AND ISNULL(u.isDeactivated, 0) = 0
            GROUP BY pt.UserID, COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), pt.UserID))
        ) p ON p.UserID = c.UserID
        WHERE ISNULL(c.CurrentCount, 0) > 0 OR ISNULL(p.PreviousCount, 0) > 0;

        UPDATE dbo.POS_DashboardSnapshotHeader
        SET Status = N'Completed',
            GeneratedAt = GETDATE(),
            Message = N'تم تجهيز مؤشرات اليوم بنجاح'
        WHERE SnapshotId = @snapshotId;
    END TRY
    BEGIN CATCH
        IF @snapshotId IS NOT NULL
        BEGIN
            UPDATE dbo.POS_DashboardSnapshotHeader
            SET Status = N'Failed',
                GeneratedAt = GETDATE(),
                Message = ERROR_MESSAGE()
            WHERE SnapshotId = @snapshotId;
        END;

        DECLARE @error NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@error, 16, 1);
    END CATCH;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_DashboardSnapshot_ReadDaily', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_DashboardSnapshot_ReadDaily;
GO

CREATE PROCEDURE dbo.usp_POS_DashboardSnapshot_ReadDaily
    @snapshotDate DATETIME,
    @branchId INT = NULL,
    @operationType NVARCHAR(30) = N'',
    @includeSmartMetrics BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @snapshotId INT;
    SET @operationType = LTRIM(RTRIM(ISNULL(@operationType, N'')));

    SELECT TOP (1)
        @snapshotId = SnapshotId
    FROM dbo.POS_DashboardSnapshotHeader
    WHERE PeriodType = N'daily'
      AND PeriodStart = CONVERT(DATE, @snapshotDate)
      AND PeriodEnd = CONVERT(DATE, @snapshotDate)
      AND ISNULL(BranchId, -1) = ISNULL(@branchId, -1)
      AND OperationType = @operationType
    ORDER BY SnapshotId DESC;

    IF @snapshotId IS NULL
    BEGIN
        SELECT N'Missing' AS Status, CAST(NULL AS DATETIME) AS GeneratedAt, N'لم يتم تجهيز مؤشرات هذه الفترة بعد' AS Message;
        RETURN;
    END;

    SELECT Status, GeneratedAt, Message
    FROM dbo.POS_DashboardSnapshotHeader
    WHERE SnapshotId = @snapshotId;

    SELECT TransactionCount, SalesTotal, FeesTotal, VatTotal, NetCollection, ActivatedKycCards, CancelledOrReturnedCount
    FROM dbo.POS_DashboardSnapshotKpi
    WHERE SnapshotId = @snapshotId AND ScopeName = N'Current';

    SELECT TransactionCount, SalesTotal, FeesTotal, VatTotal, NetCollection, ActivatedKycCards, CancelledOrReturnedCount
    FROM dbo.POS_DashboardSnapshotKpi
    WHERE SnapshotId = @snapshotId AND ScopeName = N'Previous';

    SELECT BranchId, BranchName, TransactionCount, TotalValue, FeesTotal
    FROM dbo.POS_DashboardSnapshotBranch
    WHERE SnapshotId = @snapshotId AND SectionName = N'Top'
    ORDER BY RankNo;

    SELECT BranchId, BranchName, TransactionCount, TotalValue, FeesTotal
    FROM dbo.POS_DashboardSnapshotBranch
    WHERE SnapshotId = @snapshotId AND SectionName = N'Worst'
    ORDER BY RankNo;

    SELECT Item_ID, ItemName, SaleCount, TotalValue, FeesTotal
    FROM dbo.POS_DashboardSnapshotService
    WHERE SnapshotId = @snapshotId
    ORDER BY RankNo;

    SELECT OperationType, TransactionCount, RechargeTotal, FeesTotal, VatTotal, NetCollection
    FROM dbo.POS_DashboardSnapshotOperation
    WHERE SnapshotId = @snapshotId
    ORDER BY OperationType;

    SELECT DayValue AS [Day], TransactionCount, NetCollection
    FROM dbo.POS_DashboardSnapshotTrend
    WHERE SnapshotId = @snapshotId
    ORDER BY DayValue;

    SELECT RankNo, SellerId, SellerName, TransactionCount, NetValue
    FROM dbo.POS_DashboardSnapshotSeller
    WHERE SnapshotId = @snapshotId
    ORDER BY RankNo;

    IF @includeSmartMetrics = 1
    BEGIN
        SELECT EntityId, EntityName, CurrentCount, CurrentValue, CurrentFees, PreviousCount, PreviousValue, PreviousFees
        FROM dbo.POS_DashboardSnapshotSmartMetric
        WHERE SnapshotId = @snapshotId AND MetricType = N'Branch'
        ORDER BY CurrentValue DESC;

        SELECT EntityId, EntityName, CurrentCount, CurrentValue, CurrentFees, PreviousCount, PreviousValue, PreviousFees
        FROM dbo.POS_DashboardSnapshotSmartMetric
        WHERE SnapshotId = @snapshotId AND MetricType = N'Service'
        ORDER BY CurrentFees DESC;

        SELECT EntityId, EntityName, CurrentCount, CurrentValue, CurrentFees, PreviousCount, PreviousValue, PreviousFees
        FROM dbo.POS_DashboardSnapshotSmartMetric
        WHERE SnapshotId = @snapshotId AND MetricType = N'Seller'
        ORDER BY CurrentValue DESC;
    END;
END;
GO
