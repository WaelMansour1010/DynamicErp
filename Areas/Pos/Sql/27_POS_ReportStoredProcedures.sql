IF OBJECT_ID(N'dbo.usp_POS_Report_Run', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Report_Run;
GO

CREATE PROCEDURE dbo.usp_POS_Report_Run
    @reportKey NVARCHAR(80),
    @fromDate DATETIME,
    @toDate DATETIME,
    @branchId INT,
    @userId INT,
    @canChangeDefaults BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = CONVERT(DATE, @fromDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @toDate));

    IF @reportKey IN (N'daily-trans', N'daily-trans-2')
    BEGIN
        SELECT TOP (1000)
            t.Transaction_ID,
            t.NoteSerial1,
            t.Transaction_Date,
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), t.BranchId)) AS BranchName,
            CASE
                WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
                WHEN ISNULL(t.IsPOS, 0) = 1 THEN N'Keshni Card'
                WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
                ELSE N'Cash In'
            END AS ReportType,
            t.CashCustomerName,
            t.CashCustomerPhone,
            ISNULL(t.RechargeValue, 0) AS RechargeValue,
            ISNULL(t.NetValue, 0) AS NetValue,
            ISNULL(t.Vat, 0) AS Vat,
            ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0)) AS TotalValue
        FROM dbo.Transactions t
        LEFT JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND (@branchId <= 0 OR t.BranchId = @branchId)
          AND (@canChangeDefaults = 1 OR t.UserId = @userId)
        ORDER BY t.Transaction_ID DESC;

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
        SELECT TOP (1000)
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), t.BranchId)) AS BranchName,
            CASE
                WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
                WHEN ISNULL(t.IsPOS, 0) = 1 THEN N'Keshni Card'
                WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
                ELSE N'Cash In'
            END AS ReportType,
            COUNT(1) AS TransactionCount,
            SUM(ISNULL(t.RechargeValue, 0)) AS RechargeTotal,
            SUM(ISNULL(t.NetValue, 0)) AS NetValueTotal,
            SUM(ISNULL(t.Vat, 0)) AS VatTotal,
            SUM(ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))) AS TotalValue
        FROM dbo.Transactions t
        LEFT JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND (@branchId <= 0 OR t.BranchId = @branchId)
          AND (@canChangeDefaults = 1 OR t.UserId = @userId)
        GROUP BY
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), t.BranchId)),
            CASE
                WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
                WHEN ISNULL(t.IsPOS, 0) = 1 THEN N'Keshni Card'
                WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
                ELSE N'Cash In'
            END
        ORDER BY BranchName, ReportType;

        RETURN;
    END;

    IF @reportKey = N'finance-closing'
    BEGIN
        SELECT TOP (1000)
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), c.BranchID)) AS BranchName,
            c.OrderDate AS ClosingDate,
            c.NoteID,
            CONVERT(NVARCHAR(50), CAST(c.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
            CONVERT(NVARCHAR(50), CAST(c.NoteSerial1 AS DECIMAL(38,0))) AS NoteSerial1,
            n.NoteDate,
            N'قيد الإغلاق المالي' AS VoucherType,
            ISNULL(c.OpenBalance, 0) AS OpenBalance,
            ISNULL(c.LastBalance, 0) AS LastBalance,
            ISNULL(c.TotalRechargeValue, 0) AS TotalRechargeValue,
            ISNULL(c.TotalRev, 0) AS TotalRev,
            ISNULL(c.TotalVat, 0) AS TotalVat,
            ISNULL(c.CashOutTotal, 0) AS CashOutTotal,
            ISNULL(c.TotalSupply, 0) AS TotalSupply,
            ISNULL(c.BoxBalance, 0) AS BoxBalance,
            ISNULL(n.Note_Value, 0) AS NoteValue,
            u.UserName
        FROM dbo.TBLClosePos c
        LEFT JOIN dbo.Notes n ON n.NoteID = c.NoteID
        LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
        LEFT JOIN dbo.TblUsers u ON u.UserID = c.UserID
        WHERE c.OrderDate >= @from
          AND c.OrderDate < @toExclusive
          AND (@branchId <= 0 OR c.BranchID = @branchId)
          AND (@canChangeDefaults = 1 OR c.UserID = @userId)
        ORDER BY c.OrderDate DESC, c.ID DESC;

        RETURN;
    END;

    IF @reportKey = N'finance-closing-discounts'
    BEGIN
        SELECT TOP (1000)
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), c.BranchID)) AS BranchName,
            c.OrderDate AS ClosingDate,
            c.NoteID,
            CONVERT(NVARCHAR(50), CAST(c.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
            CASE WHEN ISNULL(n.NoteType, 0) = 29807 THEN N'خصومات الإغلاق' ELSE N'قيد الإغلاق المالي' END AS VoucherType,
            ISNULL(c.TotalRev, 0) AS TotalRev,
            ISNULL(c.TotalRev2, 0) AS TotalRev2,
            ISNULL(c.TotalRevVat, 0) AS TotalRevVat,
            ISNULL(c.CashOutDisc, 0) AS CashOutDisc,
            ISNULL(c.TotalSupply, 0) AS TotalSupply,
            ISNULL(n.Note_Value, 0) AS NoteValue,
            u.UserName
        FROM dbo.TBLClosePos c
        LEFT JOIN dbo.Notes n ON n.NoteID = c.NoteID
        LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
        LEFT JOIN dbo.TblUsers u ON u.UserID = c.UserID
        WHERE c.OrderDate >= @from
          AND c.OrderDate < @toExclusive
          AND (@branchId <= 0 OR c.BranchID = @branchId)
          AND (@canChangeDefaults = 1 OR c.UserID = @userId)
        ORDER BY c.OrderDate DESC, c.ID DESC;

        RETURN;
    END;

    IF @reportKey = N'revenues'
    BEGIN
        SELECT TOP (1000)
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), t.BranchId)) AS BranchName,
            CASE
                WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
                WHEN ISNULL(t.IsPOS, 0) = 1 THEN N'Keshni Card'
                WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
                ELSE N'Cash In'
            END AS ReportType,
            COUNT(1) AS TransactionCount,
            SUM(ISNULL(t.NetValue, 0)) AS FeesTotal,
            SUM(ISNULL(t.Vat, 0)) AS VatTotal,
            SUM(ISNULL(t.NetValue, 0) + ISNULL(t.Vat, 0)) AS TotalValue,
            SUM(ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))) AS NetCollection
        FROM dbo.Transactions t
        LEFT JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND (@branchId <= 0 OR t.BranchId = @branchId)
          AND (@canChangeDefaults = 1 OR t.UserId = @userId)
        GROUP BY
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), t.BranchId)),
            CASE
                WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
                WHEN ISNULL(t.IsPOS, 0) = 1 THEN N'Keshni Card'
                WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
                ELSE N'Cash In'
            END
        ORDER BY BranchName, ReportType;

        RETURN;
    END;

    SELECT
        COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), t.BranchId)) AS BranchName,
        CASE
            WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
            WHEN ISNULL(t.IsPOS, 0) = 1 THEN N'Keshni Card'
            WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
            ELSE N'Cash In'
        END AS ReportType,
        COUNT(1) AS TransactionCount,
        SUM(ISNULL(t.RechargeValue, 0)) AS RechargeTotal,
        SUM(ISNULL(t.NetValue, 0)) AS NetValueTotal,
        SUM(ISNULL(t.Vat, 0)) AS VatTotal,
        SUM(ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))) AS TotalValue
    FROM dbo.Transactions t
    LEFT JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
    WHERE t.Transaction_Type = 21
      AND t.Transaction_Date >= @from
      AND t.Transaction_Date < @toExclusive
      AND (@branchId <= 0 OR t.BranchId = @branchId)
      AND (@canChangeDefaults = 1 OR t.UserId = @userId)
    GROUP BY
        COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), t.BranchId)),
        CASE
            WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
            WHEN ISNULL(t.IsPOS, 0) = 1 THEN N'Keshni Card'
            WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
            ELSE N'Cash In'
        END
    ORDER BY ReportType;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_Dashboard_Summary', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Dashboard_Summary;
GO

CREATE PROCEDURE dbo.usp_POS_Dashboard_Summary
    @fromDate DATETIME,
    @toDate DATETIME,
    @previousFromDate DATETIME = NULL,
    @previousToDate DATETIME = NULL,
    @branchId INT = NULL,
    @operationType NVARCHAR(30) = N''
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = CONVERT(DATE, @fromDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @toDate));
    DECLARE @previousFrom DATETIME = CONVERT(DATE, ISNULL(@previousFromDate, DATEADD(DAY, -DATEDIFF(DAY, @from, @toExclusive), @from)));
    DECLARE @previousToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@previousToDate, DATEADD(DAY, -1, @from))));
    SET @operationType = LTRIM(RTRIM(ISNULL(@operationType, N'')));

    IF OBJECT_ID('tempdb..#PosTransactions') IS NOT NULL DROP TABLE #PosTransactions;
    IF OBJECT_ID('tempdb..#PreviousPosTransactions') IS NOT NULL DROP TABLE #PreviousPosTransactions;

    SELECT
        t.Transaction_ID,
        t.UserID,
        t.BranchId,
        COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)) AS BranchName,
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
        t.BranchId,
        COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)) AS BranchName,
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

    SELECT
        COUNT(1) AS TransactionCount,
        SUM(CASE WHEN OperationType = N'card' THEN NetCollection ELSE RechargeValue END) AS SalesTotal,
        SUM(FeesValue) AS FeesTotal,
        SUM(VatValue) AS VatTotal,
        SUM(NetCollection) AS NetCollection,
        -- KYC activation has no standalone activation flag in TblCusCsh.
        -- Dashboard "Activated KYC Cards" therefore counts saved Keshni KYC records
        -- that were actually used by a POS card invoice in the selected period.
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
                  )), 0) AS ActivatedKycCards,
        ISNULL((SELECT COUNT(1)
                FROM dbo.Transactions r
                INNER JOIN dbo.TblBranchesData rb ON rb.branch_id = r.BranchId
                WHERE r.Transaction_Type = 9
                  AND r.Transaction_Date >= @from
                  AND r.Transaction_Date < @toExclusive
                  AND ISNULL(rb.IsStoped, 0) = 0
                  AND (@branchId IS NULL OR r.BranchId = @branchId)), 0) AS CancelledOrReturnedCount
    FROM #PosTransactions;

    SELECT
        COUNT(1) AS TransactionCount,
        SUM(CASE WHEN OperationType = N'card' THEN NetCollection ELSE RechargeValue END) AS SalesTotal,
        SUM(FeesValue) AS FeesTotal,
        SUM(VatValue) AS VatTotal,
        SUM(NetCollection) AS NetCollection,
        -- Same real activation definition for the previous comparison period.
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
                  )), 0) AS ActivatedKycCards,
        ISNULL((SELECT COUNT(1)
                FROM dbo.Transactions r
                INNER JOIN dbo.TblBranchesData rb ON rb.branch_id = r.BranchId
                WHERE r.Transaction_Type = 9
                  AND r.Transaction_Date >= @previousFrom
                  AND r.Transaction_Date < @previousToExclusive
                  AND ISNULL(rb.IsStoped, 0) = 0
                  AND (@branchId IS NULL OR r.BranchId = @branchId)), 0) AS CancelledOrReturnedCount
    FROM #PreviousPosTransactions;

    SELECT TOP (5)
        BranchId,
        BranchName,
        COUNT(1) AS TransactionCount,
        SUM(NetCollection) AS TotalValue,
        SUM(FeesValue) AS FeesTotal
    FROM #PosTransactions
    GROUP BY BranchId, BranchName
    HAVING COUNT(1) > 0 AND SUM(NetCollection) > 0
    ORDER BY SUM(NetCollection) DESC, COUNT(1) DESC;

    SELECT TOP (5)
        BranchId,
        BranchName,
        COUNT(1) AS TransactionCount,
        SUM(NetCollection) AS TotalValue,
        SUM(FeesValue) AS FeesTotal
    FROM #PosTransactions
    GROUP BY BranchId, BranchName
    HAVING COUNT(1) > 0 AND SUM(NetCollection) > 0
    ORDER BY SUM(NetCollection) ASC, COUNT(1) ASC;

    SELECT TOP (10)
        d.Item_ID,
        COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID)) AS ItemName,
        COUNT(1) AS SaleCount,
        SUM(ISNULL(d.TotalPrice, ISNULL(d.Price, 0) + ISNULL(d.Vat, 0))) AS TotalValue,
        SUM(ISNULL(d.showPrice, ISNULL(d.Price, 0))) AS FeesTotal
    FROM dbo.Transaction_Details d
    INNER JOIN dbo.Transactions t ON t.Transaction_ID = d.Transaction_ID
    INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
    LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
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
      )
    GROUP BY d.Item_ID, COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID))
    ORDER BY COUNT(1) DESC, SUM(ISNULL(d.TotalPrice, ISNULL(d.Price, 0) + ISNULL(d.Vat, 0))) DESC;

    SELECT
        OperationType,
        COUNT(1) AS TransactionCount,
        SUM(RechargeValue) AS RechargeTotal,
        SUM(FeesValue) AS FeesTotal,
        SUM(VatValue) AS VatTotal,
        SUM(NetCollection) AS NetCollection
    FROM #PosTransactions
    GROUP BY OperationType
    ORDER BY OperationType;

    SELECT
        CONVERT(VARCHAR(10), t.Transaction_Date, 120) AS Day,
        COUNT(1) AS TransactionCount,
        SUM(ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))) AS NetCollection
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
      )
    GROUP BY CONVERT(VARCHAR(10), t.Transaction_Date, 120)
    ORDER BY Day;

    SELECT TOP (5)
        ROW_NUMBER() OVER (ORDER BY SUM(p.NetCollection) DESC, COUNT(1) DESC, p.UserID ASC) AS RankNo,
        p.UserID AS SellerId,
        COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), p.UserID)) AS SellerName,
        COUNT(1) AS TransactionCount,
        SUM(p.NetCollection) AS NetValue
    FROM #PosTransactions p
    INNER JOIN dbo.TblUsers u ON u.UserID = p.UserID
    LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
    WHERE p.UserID IS NOT NULL
      AND ISNULL(u.isDeactivated, 0) = 0
    GROUP BY p.UserID, COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), p.UserID))
    HAVING COUNT(1) > 0 AND SUM(p.NetCollection) > 0
    ORDER BY SUM(p.NetCollection) DESC, COUNT(1) DESC, p.UserID ASC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_Report_StoreSerials', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Report_StoreSerials;
GO

CREATE PROCEDURE dbo.usp_POS_Report_StoreSerials
    @storeId INT = NULL,
    @serialSearch NVARCHAR(255) = N'',
    @branchId INT,
    @userId INT,
    @canChangeDefaults BIT
AS
BEGIN
    SET NOCOUNT ON;

    SET @serialSearch = LTRIM(RTRIM(ISNULL(@serialSearch, N'')));

    SELECT TOP (1000)
        COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), N'مخزن ' + CONVERT(NVARCHAR(20), td.StoreID2)) AS StoreName,
        i.ItemCode,
        i.ItemName,
        td.ItemSerial,
        SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS StockBalance,
        MAX(t.Transaction_ID) AS LastTransactionId,
        MAX(t.Transaction_Date) AS LastTransactionDate,
        CASE WHEN SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0 THEN N'متاح' ELSE N'غير متاح' END AS SerialStatus
    FROM dbo.Transaction_Details td
    INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
    LEFT JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = t.Transaction_Type
    LEFT JOIN dbo.TblItems i ON i.ItemID = td.Item_ID
    LEFT JOIN dbo.TblStore s ON s.StoreID = td.StoreID2
    WHERE ISNULL(td.ItemSerial, N'') <> N''
      AND (@storeId IS NULL OR td.StoreID2 = @storeId)
      AND (@branchId <= 0 OR t.BranchId = @branchId)
      AND (@canChangeDefaults = 1 OR t.UserId = @userId)
      AND
      (
          @serialSearch = N''
          OR td.ItemSerial LIKE N'%' + @serialSearch + N'%'
          OR i.ItemName LIKE N'%' + @serialSearch + N'%'
          OR i.ItemCode LIKE N'%' + @serialSearch + N'%'
      )
    GROUP BY
        COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), N'مخزن ' + CONVERT(NVARCHAR(20), td.StoreID2)),
        i.ItemCode,
        i.ItemName,
        td.ItemSerial
    HAVING SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0
    ORDER BY StoreName, i.ItemName, td.ItemSerial;
END;
GO
