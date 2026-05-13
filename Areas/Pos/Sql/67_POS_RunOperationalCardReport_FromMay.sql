/*
    Kishny POS - Run Operational Transactions Report for Card service
    SQL Server 2012 compatible

    Purpose:
    تشغيل نفس تقرير التقارير اليومية للحركات Service Type = Card
    من أول مايو 2026 حتى تاريخ التشغيل.

    Notes:
    - هذا السكريبت Read-only.
    - لو الـ EXEC فشل، سيعرض ErrorMessage الحقيقي.
    - يوجد Query مباشر في الجزء الثاني لنفس منطق التقرير بدون استدعاء SP.
*/

SET NOCOUNT ON;

DECLARE @FromDate DATETIME = '20260501';
DECLARE @ToDate DATETIME = CONVERT(DATE, GETDATE());
DECLARE @BranchId INT = 0;       -- 0 = كل الفروع
DECLARE @UserId INT = 0;         -- 0 عند التشغيل بصلاحية مدير
DECLARE @StoreId INT = NULL;     -- NULL = كل المخازن
DECLARE @FilterUserId INT = NULL;-- NULL = كل المستخدمين
DECLARE @StartedAt DATETIME;
DECLARE @EndedAt DATETIME;

PRINT '=== 1) Run same POS report stored procedure: daily-trans / card / from 2026-05-01 ===';

BEGIN TRY
    SET @StartedAt = GETDATE();

    EXEC dbo.usp_POS_Report_Run
        @reportKey = N'daily-trans',
        @fromDate = @FromDate,
        @toDate = @ToDate,
        @branchId = @BranchId,
        @userId = @UserId,
        @canChangeDefaults = 1,
        @branchFromId = NULL,
        @branchToId = NULL,
        @showEmptyBranches = 0,
        @serviceSearch = NULL,
        @serviceType = N'card',
        @storeId = @StoreId,
        @filterUserId = @FilterUserId;

    SET @EndedAt = GETDATE();
    SELECT
        RunStatus = N'OK',
        StartedAt = @StartedAt,
        EndedAt = @EndedAt,
        DurationMs = DATEDIFF(MILLISECOND, @StartedAt, @EndedAt);
END TRY
BEGIN CATCH
    SELECT
        RunStatus = N'FAILED',
        ErrorNumber = ERROR_NUMBER(),
        ErrorSeverity = ERROR_SEVERITY(),
        ErrorState = ERROR_STATE(),
        ErrorLine = ERROR_LINE(),
        ErrorProcedure = ERROR_PROCEDURE(),
        ErrorMessage = ERROR_MESSAGE();
END CATCH;

PRINT '=== 2) Direct fallback query, same card-service report logic, TOP 500 only ===';

BEGIN TRY
    SET @StartedAt = GETDATE();

    ;WITH BaseTransactions AS
    (
        SELECT TOP (500)
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
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N'') IS NOT NULL
                  AND NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N'') NOT LIKE N'%[^0-9]%'
                    THEN CONVERT(INT, NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N''))
                ELSE NULL
            END AS IssueTransactionID
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE t.Transaction_Type = 21
          AND ISNULL(t.IsCancelled, 0) = 0
          AND t.Transaction_Date >= @FromDate
          AND t.Transaction_Date < DATEADD(DAY, 1, @ToDate)
          AND (@BranchId <= 0 OR t.BranchId = @BranchId)
          AND (@StoreId IS NULL OR t.StoreID = @StoreId)
          AND (@FilterUserId IS NULL OR t.UserID = @FilterUserId)
          AND (NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1)
        ORDER BY t.Transaction_ID DESC
    )
    SELECT
        bt.NoteSerial1 AS InvoiceNumber,
        bt.CashCustomerName AS CustomerName,
        bt.Transaction_Date AS InvoiceDate,
        bt.CashCustomerPhone AS CustomerPhone,
        bt.CardNumber,
        CAST(NULL AS DECIMAL(18, 2)) AS RechargeAmount,
        COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), bt.BranchId)) AS Branch,
        COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), N'مخزن ' + CONVERT(NVARCHAR(20), bt.StoreID)) AS Store,
        u.UserName AS Cashier,
        N'Card' AS ServiceType,
        CASE
            WHEN bt.CardNumber IS NULL THEN N'Problematic Card'
            WHEN ISNULL(card.DuplicateInvoiceCount, 0) > 1 THEN N'Problematic Card'
            WHEN ISNULL(card.HasKycCustomer, 0) = 0 THEN N'Problematic Card'
            WHEN ISNULL(card.IssueQty, 0) <= 0 THEN N'Problematic Card'
            WHEN ISNULL(card.StockBefore, 0) <= 0 THEN N'Insufficient Balance'
            WHEN ISNULL(card.StockBefore, 0) - ISNULL(card.IssueQty, 0) < 0 THEN N'Negative'
            ELSE N'Normal'
        END AS CardIssueStatus,
        CASE
            WHEN bt.CardNumber IS NULL THEN N'كارت به مشكلة'
            WHEN ISNULL(card.DuplicateInvoiceCount, 0) > 1 THEN N'كارت مكرر'
            WHEN ISNULL(card.HasKycCustomer, 0) = 0 THEN N'بيانات KYC غير موجودة'
            WHEN ISNULL(card.IssueQty, 0) <= 0 THEN N'إذن صرف غير موجود'
            WHEN ISNULL(card.StockBefore, 0) <= 0 THEN N'رصيد غير كاف'
            WHEN ISNULL(card.StockBefore, 0) - ISNULL(card.IssueQty, 0) < 0 THEN N'رصيد سالب'
            ELSE N'طبيعي'
        END AS CardIssueStatusAr,
        CAST(ISNULL(card.StockBefore, 0) AS DECIMAL(18, 4)) AS CardStockBefore,
        CAST(ISNULL(card.StockBefore, 0) - ISNULL(card.IssueQty, 0) AS DECIMAL(18, 4)) AS CardStockAfter,
        bt.Transaction_ID,
        bt.NetValue,
        bt.Vat,
        bt.TotalValue
    FROM BaseTransactions bt
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK)
        ON b.branch_id = bt.BranchId
    LEFT JOIN dbo.TblStore s WITH (NOLOCK)
        ON s.StoreID = bt.StoreID
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK)
        ON u.UserID = bt.UserID
    OUTER APPLY
    (
        SELECT
            ISNULL((
                SELECT SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0))
                FROM dbo.Transaction_Details td WITH (NOLOCK)
                INNER JOIN dbo.Transactions mt WITH (NOLOCK)
                    ON mt.Transaction_ID = td.Transaction_ID
                INNER JOIN dbo.TransactionTypes tt WITH (NOLOCK)
                    ON tt.Transaction_Type = mt.Transaction_Type
                WHERE LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = bt.CardNumber
                  AND ISNULL(tt.StockEffect, 0) <> 0
                  AND mt.Transaction_ID < bt.Transaction_ID
            ), 0) AS StockBefore,
            ISNULL((
                SELECT SUM(ISNULL(td.Quantity, 0))
                FROM dbo.Transaction_Details td WITH (NOLOCK)
                WHERE td.Transaction_ID = bt.IssueTransactionID
                  AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = bt.CardNumber
            ), 0) AS IssueQty,
            (
                SELECT COUNT(1)
                FROM dbo.Transactions dup WITH (NOLOCK)
                WHERE dup.Transaction_Type = 21
                  AND ISNULL(dup.IsCancelled, 0) = 0
                  AND NULLIF(LTRIM(RTRIM(ISNULL(dup.VisaNumber, N''))), N'') = bt.CardNumber
            ) AS DuplicateInvoiceCount,
            CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.TblCusCsh c WITH (NOLOCK)
                WHERE ISNULL(c.EasyCashType, 0) = 0
                  AND (LTRIM(RTRIM(ISNULL(c.CardNo, N''))) = bt.CardNumber OR LTRIM(RTRIM(ISNULL(c.CardId, N''))) = bt.CardNumber)
            ) THEN 1 ELSE 0 END AS HasKycCustomer
    ) card
    ORDER BY bt.Transaction_ID DESC;

    SET @EndedAt = GETDATE();
    SELECT
        DirectQueryStatus = N'OK',
        StartedAt = @StartedAt,
        EndedAt = @EndedAt,
        DurationMs = DATEDIFF(MILLISECOND, @StartedAt, @EndedAt);
END TRY
BEGIN CATCH
    SELECT
        DirectQueryStatus = N'FAILED',
        ErrorNumber = ERROR_NUMBER(),
        ErrorSeverity = ERROR_SEVERITY(),
        ErrorState = ERROR_STATE(),
        ErrorLine = ERROR_LINE(),
        ErrorProcedure = ERROR_PROCEDURE(),
        ErrorMessage = ERROR_MESSAGE();
END CATCH;
