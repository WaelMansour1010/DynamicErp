IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'dbo.Transactions', N'U')
         AND name = N'IX_POS_Transactions_OperationalSales_Report'
   )
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transactions_OperationalSales_Report
    ON dbo.Transactions (Transaction_Type, IsCancelled, Transaction_Date, Transaction_ID)
    INCLUDE
    (
        BranchId,
        StoreID,
        UserID,
        NoteSerial1,
        CashCustomerName,
        CashCustomerPhone,
        VisaNumber,
        RechargeValue,
        NetValue,
        Vat,
        Transaction_NetValue,
        PayedValue,
        TrafficViolations,
        IsPOS,
        IsCashOut
    );
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_Report_RunOperationalSales', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Report_RunOperationalSales;
GO

CREATE PROCEDURE dbo.usp_POS_Report_RunOperationalSales
    @reportKey NVARCHAR(80),
    @fromDate DATETIME,
    @toDate DATETIME,
    @branchId INT,
    @userId INT,
    @canChangeDefaults BIT,
    @serviceType NVARCHAR(30) = NULL,
    @storeId INT = NULL,
    @filterUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = CONVERT(DATE, @fromDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @toDate));
    DECLARE @operationType NVARCHAR(30) = LOWER(NULLIF(LTRIM(RTRIM(ISNULL(@serviceType, N''))), N''));

    IF @toExclusive < @from
    BEGIN
        DECLARE @swap DATETIME = @from;
        SET @from = DATEADD(DAY, -1, @toExclusive);
        SET @toExclusive = DATEADD(DAY, 1, @swap);
    END;

    IF @operationType NOT IN (N'cash-in', N'cash-out', N'card', N'violations')
        SET @operationType = NULL;

    IF @reportKey IN (N'daily-trans', N'daily-trans-2')
    BEGIN
        ;WITH BaseTransactions AS
        (
            SELECT
                t.Transaction_ID,
                t.NoteSerial1,
                t.Transaction_Date,
                t.BranchId,
                t.StoreID,
                t.UserID,
                t.CashCustomerName,
                t.CashCustomerPhone,
                NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') AS CardNumber,
                ISNULL(t.RechargeValue, 0) AS RechargeValue,
                ISNULL(t.NetValue, 0) AS NetValue,
                ISNULL(t.Vat, 0) AS Vat,
                ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0)) AS TotalValue,
                CASE
                    WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1 THEN N'card'
                    WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
                    ELSE N'cash-in'
                END AS OperationType,
                CASE
                    WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1 THEN N'Card'
                    WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
                    ELSE N'Cash In'
                END AS ServiceType
            FROM dbo.Transactions t
            WHERE t.Transaction_Type = 21
              AND (t.IsCancelled = 0 OR t.IsCancelled IS NULL)
              AND t.Transaction_Date >= @from
              AND t.Transaction_Date < @toExclusive
              AND (@branchId <= 0 OR t.BranchId = @branchId)
              AND (@canChangeDefaults = 1 OR t.UserId = @userId)
              AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
              AND (@storeId IS NULL OR t.StoreID = @storeId)
        )
        SELECT TOP (1000)
            bt.NoteSerial1 AS InvoiceNumber,
            bt.CashCustomerName AS CustomerName,
            bt.Transaction_Date AS InvoiceDate,
            bt.CashCustomerPhone AS CustomerPhone,
            bt.CardNumber,
            CASE WHEN bt.OperationType IN (N'cash-in', N'cash-out') THEN CAST(bt.RechargeValue AS DECIMAL(18, 2)) ELSE CAST(NULL AS DECIMAL(18, 2)) END AS RechargeAmount,
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'Branch ' + CONVERT(NVARCHAR(20), bt.BranchId)) AS Branch,
            COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), N'Store ' + CONVERT(NVARCHAR(20), bt.StoreID)) AS Store,
            u.UserName AS Cashier,
            bt.ServiceType,
            bt.Transaction_ID,
            bt.NetValue,
            bt.Vat,
            bt.TotalValue
        FROM BaseTransactions bt
        LEFT JOIN dbo.TblBranchesData b ON b.branch_id = bt.BranchId
        LEFT JOIN dbo.TblStore s ON s.StoreID = bt.StoreID
        LEFT JOIN dbo.TblUsers u ON u.UserID = bt.UserID
        WHERE (@operationType IS NULL OR bt.OperationType = @operationType)
        ORDER BY bt.Transaction_ID DESC;

        RETURN;
    END;

    IF @reportKey = N'revenues'
    BEGIN
        ;WITH BaseTransactions AS
        (
            SELECT
                t.BranchId,
                t.UserID,
                t.StoreID,
                ISNULL(t.NetValue, 0) AS NetValue,
                ISNULL(t.Vat, 0) AS Vat,
                ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0)) AS TotalValue,
                CASE
                    WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1 THEN N'card'
                    WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
                    ELSE N'cash-in'
                END AS OperationType,
                CASE
                    WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
                    WHEN ISNULL(t.IsPOS, 0) = 1 THEN N'Keshni Card'
                    WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
                    ELSE N'Cash In'
                END AS ReportType
            FROM dbo.Transactions t
            WHERE t.Transaction_Type = 21
              AND (t.IsCancelled = 0 OR t.IsCancelled IS NULL)
              AND t.Transaction_Date >= @from
              AND t.Transaction_Date < @toExclusive
              AND (@branchId <= 0 OR t.BranchId = @branchId)
              AND (@canChangeDefaults = 1 OR t.UserId = @userId)
              AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
              AND (@storeId IS NULL OR t.StoreID = @storeId)
        )
        SELECT TOP (1000)
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), bt.BranchId)) AS BranchName,
            bt.ReportType,
            COUNT(1) AS TransactionCount,
            SUM(bt.NetValue) AS FeesTotal,
            SUM(bt.Vat) AS VatTotal,
            SUM(bt.NetValue + bt.Vat) AS TotalValue,
            SUM(bt.TotalValue) AS NetCollection
        FROM BaseTransactions bt
        LEFT JOIN dbo.TblBranchesData b ON b.branch_id = bt.BranchId
        WHERE (@operationType IS NULL OR bt.OperationType = @operationType)
        GROUP BY
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), bt.BranchId)),
            bt.ReportType
        ORDER BY BranchName, bt.ReportType;

        RETURN;
    END;

    IF @reportKey IN
    (
        N'sales-complete',
        N'sales-complete-2',
        N'sales-governorates',
        N'sales-departments',
        N'sales-sectors',
        N'sales-analytical',
        N'general-sales'
    )
    BEGIN
        ;WITH BaseTransactions AS
        (
            SELECT
                t.BranchId,
                t.UserID,
                t.StoreID,
                ISNULL(t.RechargeValue, 0) AS RechargeValue,
                ISNULL(t.NetValue, 0) AS NetValue,
                ISNULL(t.Vat, 0) AS Vat,
                ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0)) AS TotalValue,
                CASE
                    WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1 THEN N'card'
                    WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
                    ELSE N'cash-in'
                END AS OperationType,
                CASE
                    WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
                    WHEN ISNULL(t.IsPOS, 0) = 1 THEN N'Keshni Card'
                    WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
                    ELSE N'Cash In'
                END AS ReportType
            FROM dbo.Transactions t
            WHERE t.Transaction_Type = 21
              AND (t.IsCancelled = 0 OR t.IsCancelled IS NULL)
              AND t.Transaction_Date >= @from
              AND t.Transaction_Date < @toExclusive
              AND (@branchId <= 0 OR t.BranchId = @branchId)
              AND (@canChangeDefaults = 1 OR t.UserId = @userId)
              AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
              AND (@storeId IS NULL OR t.StoreID = @storeId)
        )
        SELECT TOP (1000)
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), bt.BranchId)) AS BranchName,
            bt.ReportType,
            COUNT(1) AS TransactionCount,
            SUM(bt.RechargeValue) AS RechargeTotal,
            SUM(bt.NetValue) AS NetValueTotal,
            SUM(bt.Vat) AS VatTotal,
            SUM(bt.TotalValue) AS TotalValue
        FROM BaseTransactions bt
        LEFT JOIN dbo.TblBranchesData b ON b.branch_id = bt.BranchId
        WHERE (@operationType IS NULL OR bt.OperationType = @operationType)
        GROUP BY
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), bt.BranchId)),
            bt.ReportType
        ORDER BY BranchName, bt.ReportType;

        RETURN;
    END;

    SELECT
        CAST(NULL AS NVARCHAR(200)) AS BranchName,
        CAST(NULL AS NVARCHAR(80)) AS ReportType
    WHERE 1 = 0;
END;
GO
