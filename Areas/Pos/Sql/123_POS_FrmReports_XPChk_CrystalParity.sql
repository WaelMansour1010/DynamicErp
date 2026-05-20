SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.usp_POS_FrmReports_XPChk90_93_DailySales', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FrmReports_XPChk90_93_DailySales;
GO
CREATE PROCEDURE dbo.usp_POS_FrmReports_XPChk90_93_DailySales
    @reportKey NVARCHAR(80),
    @fromDate DATETIME,
    @toDate DATETIME,
    @branchId INT,
    @userId INT,
    @canChangeDefaults BIT,
    @storeId INT = NULL,
    @filterUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATE = CONVERT(DATE, @fromDate);
    DECLARE @to DATE = CONVERT(DATE, @toDate);

    ;WITH TargetBranches AS
    (
        SELECT b.branch_id
        FROM dbo.TblBranchesData b
        WHERE (@branchId <= 0 OR b.branch_id = @branchId)
          AND (@canChangeDefaults = 1 OR b.branch_id IN (SELECT ub.BranchID FROM dbo.TblUsersBranches ub WHERE ub.UserID = @userId))
          AND (ISNULL(b.isStoped, 0) = 0 OR ISNULL(b.IsStopedDate, '9999-12-31') > @from)
    ),
    Tx AS
    (
        SELECT
            t.Transaction_ID,
            CONVERT(DATE, t.Transaction_Date) AS Transaction_Date,
            t.BranchId,
            ISNULL(t.Transaction_NetValue, 0) AS Transaction_NetValue,
            ISNULL(t.RechargeValue, 0) AS RechargeValue
        FROM dbo.Transactions t
        INNER JOIN TargetBranches tb ON tb.branch_id = t.BranchId
        WHERE t.Transaction_Type = 21
          AND CONVERT(DATE, t.Transaction_Date) BETWEEN @from AND @to
          AND (@storeId IS NULL OR t.StoreID = @storeId)
          AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
    ),
    DetailAgg AS
    (
        SELECT
            tx.BranchId,
            tx.Transaction_Date,
            COUNT(CASE WHEN td.Item_ID = 1 THEN 1 END) AS Count1,
            COUNT(CASE WHEN td.Item_ID = 2 AND ISNULL(td.Price, 0) <> 0 THEN 1 END) AS Count2,
            COUNT(CASE WHEN td.Item_ID = 3 THEN 1 END) AS Count3,
            COUNT(CASE WHEN td.Item_ID = 4 THEN 1 END) AS Count4,
            COUNT(CASE WHEN td.Item_ID = 5 THEN 1 END) AS Count5,
            COUNT(CASE WHEN td.Item_ID = 6 THEN 1 END) AS Count6,
            COUNT(CASE WHEN td.Item_ID = 7 THEN 1 END) AS Count7,
            COUNT(CASE WHEN td.Item_ID = 8 THEN 1 END) AS Count8,
            COUNT(CASE WHEN td.Item_ID = 10 THEN 1 END) AS Count9,
            COUNT(1) AS CountTotal,
            SUM(ISNULL(td.Vat, 0)) AS VatDet
        FROM Tx tx
        INNER JOIN dbo.Transaction_Details td ON td.Transaction_ID = tx.Transaction_ID
        GROUP BY tx.BranchId, tx.Transaction_Date
    ),
    TxAgg AS
    (
        SELECT
            BranchId,
            Transaction_Date,
            SUM(Transaction_NetValue) AS Transaction_NetValue,
            SUM(RechargeValue) AS RechargeValue
        FROM Tx
        GROUP BY BranchId, Transaction_Date
    ),
    MonthAgg AS
    (
        SELECT
            tx.BranchId,
            tx.Transaction_Date,
            COUNT(td.Transaction_ID) AS CountTotalMonth
        FROM Tx tx
        INNER JOIN dbo.Transaction_Details td ON td.Transaction_ID = tx.Transaction_ID
        GROUP BY tx.BranchId, tx.Transaction_Date
    )
    SELECT
        CAST(ROW_NUMBER() OVER (ORDER BY ta.Transaction_Date, b.branch_Code) AS INT) AS RowNo,
        @reportKey AS ReportKey,
        CASE WHEN @reportKey = N'daily-trans-2' THEN N'DailyReprotTotalCayshny2Sector.rpt' ELSE N'DailyReprotTotalCayshny2.rpt' END AS CrystalReportName,
        ta.Transaction_Date,
        b.branch_Code,
        ISNULL(da.Count1, 0) AS Count1,
        ISNULL(da.Count2, 0) AS Count2,
        ISNULL(da.Count3, 0) AS Count3,
        ISNULL(da.Count4, 0) AS Count4,
        ISNULL(da.Count5, 0) AS Count5,
        ISNULL(da.Count6, 0) AS Count6,
        ISNULL(da.Count7, 0) AS Count7,
        ISNULL(da.Count8, 0) AS Count8,
        ISNULL(da.Count9, 0) AS Count9,
        ISNULL(da.CountTotal, 0) AS CountTotal,
        ISNULL(ma.CountTotalMonth, 0) AS CountTotalMonth,
        ta.Transaction_NetValue,
        ta.RechargeValue,
        ISNULL(da.VatDet, 0) AS VatDet,
        sec.name AS SectionName,
        gov.GovernmentName,
        b.branch_name AS BranchName,
        act.name AS ActivitesTypeName,
        b.branch_id
    FROM TxAgg ta
    INNER JOIN dbo.TblBranchesData b ON b.branch_id = ta.BranchId
    LEFT JOIN DetailAgg da ON da.BranchId = ta.BranchId AND da.Transaction_Date = ta.Transaction_Date
    LEFT JOIN MonthAgg ma ON ma.BranchId = ta.BranchId AND ma.Transaction_Date = ta.Transaction_Date
    LEFT JOIN dbo.TblSection sec ON sec.Id = b.RegionID
    LEFT JOIN dbo.TblCountriesGovernments gov ON gov.GovernmentID = b.GovernmentID
    LEFT JOIN dbo.tblActivitesType act ON act.Id = b.ActivityTypeId
    ORDER BY ta.Transaction_Date, b.branch_Code;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FrmReports_XPChk2_ItemsSalesDetails', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FrmReports_XPChk2_ItemsSalesDetails;
GO
CREATE PROCEDURE dbo.usp_POS_FrmReports_XPChk2_ItemsSalesDetails
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

    DECLARE @from DATE = CONVERT(DATE, @fromDate);
    DECLARE @to DATE = CONVERT(DATE, @toDate);

    SELECT TOP (5000)
        CAST(ROW_NUMBER() OVER (ORDER BY t.Transaction_Date, t.Transaction_ID, td.Item_ID) AS INT) AS RowNo,
        N'items-sales-details' AS ReportKey,
        N'sales5.rpt / sales910.rpt' AS CrystalReportName,
        t.Transaction_ID,
        t.Transaction_Serial,
        t.Transaction_Date,
        t.VisaNumber,
        t.RechargeValue,
        tt.TransactionTypeName,
        tt.TransactionEnglishName,
        tt.StockEffect,
        t.Transaction_Type,
        t.PaymentType,
        t.CusID,
        t.StoreID,
        t.UserID,
        t.Emp_ID,
        td.Item_ID,
        td.Quantity,
        td.Price,
        td.UnitId,
        td.OpeningBurcahseValue,
        td.OpeningBurcahseQty,
        td.OpeningSalesQty,
        td.OpeningSalesValue,
        u.UnitName,
        s.StoreName,
        c.CusName,
        c.CusNamee,
        t.PayedValue,
        t.BranchId,
        b.branch_name,
        b.branch_namee,
        b.Tel,
        i.ItemCode,
        i.ItemName,
        i.GroupID,
        g.GroupName,
        td.OpeningReSalesQty,
        td.OpeningReSalesValue,
        ISNULL(td.TotalDiscountPerLine, 0) * ISNULL(t.Currency_rate, 1) AS TotalDiscountPerLine,
        ISNULL(t.Currency_rate, 1) AS Currency_rate,
        CASE
            WHEN td.ItemDiscountType = 1 THEN 0
            WHEN td.ItemDiscountType = 2 THEN ISNULL(td.ItemDiscount, 0) * ISNULL(t.Currency_rate, 1)
            WHEN td.ItemDiscountType = 3 THEN (ISNULL(td.showqty, 0) * ISNULL(td.showPrice, 0)) * (ISNULL(td.ItemDiscount, 0) / 100.0) * ISNULL(t.Currency_rate, 1)
            WHEN td.ItemDiscountType = 4 THEN (ISNULL(td.showqty, 0) * ISNULL(td.showPrice, 0)) * ISNULL(t.Currency_rate, 1)
            ELSE 0
        END AS ItemDiscountValue,
        ISNULL(td.showPrice, td.Price) * ISNULL(t.Currency_rate, 1) AS localprice,
        ISNULL(td.showPrice, td.Price) * ISNULL(t.Currency_rate, 1) * ISNULL(td.showqty, td.Quantity) AS LineNet,
        td.ExpiryDate,
        td.lotNo
    FROM dbo.Transactions t
    INNER JOIN dbo.Transaction_Details td ON t.Transaction_ID = td.Transaction_ID
    INNER JOIN dbo.TblStore s ON t.StoreID = s.StoreID
    INNER JOIN dbo.TblCustemers c ON t.CusID = c.CusID
    INNER JOIN dbo.TransactionTypes tt ON t.Transaction_Type = tt.Transaction_Type
    INNER JOIN dbo.TblItems i ON td.Item_ID = i.ItemID
    INNER JOIN dbo.Groups g ON i.GroupID = g.GroupID
    LEFT JOIN dbo.TblBranchesData b ON t.BranchId = b.branch_id
    LEFT JOIN dbo.TblUnites u ON td.UnitId = u.UnitID
    WHERE t.Transaction_Type IN (21, 9)
      AND CONVERT(DATE, t.Transaction_Date) BETWEEN @from AND @to
      AND (@branchId <= 0 OR t.BranchId = @branchId)
      AND (@canChangeDefaults = 1 OR t.BranchId IN (SELECT ub.BranchID FROM dbo.TblUsersBranches ub WHERE ub.UserID = @userId))
      AND (@storeId IS NULL OR t.StoreID = @storeId)
      AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
    ORDER BY t.Transaction_Date, t.Transaction_ID, td.Item_ID;
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

    DECLARE @key NVARCHAR(80) = LOWER(LTRIM(RTRIM(ISNULL(@reportKey, N''))));
    DECLARE @singleBranchId INT = CASE WHEN @branchId > 0 THEN @branchId ELSE NULL END;

    IF @key IN (N'daily-trans', N'daily-trans-2')
    BEGIN
        EXEC dbo.usp_POS_FrmReports_XPChk90_93_DailySales
            @reportKey = @key,
            @fromDate = @fromDate,
            @toDate = @toDate,
            @branchId = @branchId,
            @userId = @userId,
            @canChangeDefaults = @canChangeDefaults,
            @storeId = @storeId,
            @filterUserId = @filterUserId;
        RETURN;
    END;

    IF @key IN (N'sales-complete', N'sales-complete-2', N'sales-sectors')
    BEGIN
        EXEC dbo.RPT_SalesSummary_Main
            @FromDate = @fromDate,
            @ToDate = @toDate,
            @UserId = @userId,
            @SingleBranchID = @singleBranchId,
            @BranchIDs = NULL,
            @GovernmentIDs = NULL,
            @ActivityIDs = NULL;
        RETURN;
    END;

    IF @key = N'sales-analytical'
    BEGIN
        EXEC dbo.RPT_CloseReportSubDetails
            @FromDate = @fromDate,
            @ToDate = @toDate,
            @UserId = @userId,
            @BranchId = @singleBranchId,
            @BrnchIDes = NULL,
            @BranshesActiv = NULL,
            @BranchGovernment = NULL;
        RETURN;
    END;

    IF @key = N'items-sales-details'
    BEGIN
        EXEC dbo.usp_POS_FrmReports_XPChk2_ItemsSalesDetails
            @fromDate = @fromDate,
            @toDate = @toDate,
            @branchId = @branchId,
            @userId = @userId,
            @canChangeDefaults = @canChangeDefaults,
            @serviceType = @serviceType,
            @storeId = @storeId,
            @filterUserId = @filterUserId;
        RETURN;
    END;

    SELECT CAST(NULL AS NVARCHAR(200)) AS BranchName, CAST(NULL AS NVARCHAR(80)) AS ReportType WHERE 1 = 0;
END;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'IX_POS_FrmReports_Tx_TypeDateBranch')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_FrmReports_Tx_TypeDateBranch
    ON dbo.Transactions (Transaction_Type, Transaction_Date, BranchId)
    INCLUDE (Transaction_ID, StoreID, UserID, Transaction_NetValue, RechargeValue, PayedValue, Currency_rate, Transaction_Serial, PaymentType, CusID, Emp_ID);
END;
GO

IF OBJECT_ID(N'dbo.Transaction_Details', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transaction_Details') AND name = N'IX_POS_FrmReports_Details_TxItem')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_FrmReports_Details_TxItem
    ON dbo.Transaction_Details (Transaction_ID, Item_ID)
    INCLUDE (Price, Vat, Quantity, UnitId, OpeningSalesValue, OpeningReSalesValue, showPrice, showqty, TotalDiscountPerLine);
END;
GO
