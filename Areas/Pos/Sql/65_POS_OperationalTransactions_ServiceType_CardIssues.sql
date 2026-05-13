IF OBJECT_ID(N'dbo.usp_POS_Report_Run', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Report_Run;
GO

CREATE PROCEDURE dbo.usp_POS_Report_Run
    @reportKey NVARCHAR(80),
    @fromDate DATETIME,
    @toDate DATETIME,
    @branchId INT,
    @userId INT,
    @canChangeDefaults BIT,
    @branchFromId INT = NULL,
    @branchToId INT = NULL,
    @showEmptyBranches BIT = 0,
    @serviceSearch NVARCHAR(100) = NULL,
    @serviceType NVARCHAR(30) = NULL,
    @storeId INT = NULL,
    @filterUserId INT = NULL,
    @includeCardIssueCheck BIT = 0,
    @cardIssueMode NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = CONVERT(DATE, @fromDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @toDate));
    DECLARE @serviceTerm NVARCHAR(100) = NULLIF(LTRIM(RTRIM(ISNULL(@serviceSearch, N''))), N'');
    DECLARE @operationType NVARCHAR(30) = LOWER(NULLIF(LTRIM(RTRIM(ISNULL(@serviceType, N''))), N''));
    DECLARE @issueMode NVARCHAR(20) = LOWER(NULLIF(LTRIM(RTRIM(ISNULL(@cardIssueMode, N''))), N''));

    IF @operationType NOT IN (N'cash-in', N'cash-out', N'card', N'violations')
        SET @operationType = NULL;

    IF @issueMode NOT IN (N'none', N'summary', N'full')
        SET @issueMode = CASE WHEN ISNULL(@includeCardIssueCheck, 0) = 1 THEN N'full' ELSE N'none' END;

    IF @reportKey IN (N'daily-trans', N'daily-trans-2')
    BEGIN
        IF @issueMode = N'none'
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
                  AND ISNULL(t.IsCancelled, 0) = 0
                  AND t.Transaction_Date >= @from
                  AND t.Transaction_Date < @toExclusive
                  AND (@branchId <= 0 OR t.BranchId = @branchId)
                  AND (@canChangeDefaults = 1 OR t.UserId = @userId)
                  AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
                  AND (@storeId IS NULL OR t.StoreID = @storeId)
            ),
            FilteredTransactions AS
            (
                SELECT *
                FROM BaseTransactions
                WHERE (@operationType IS NULL OR OperationType = @operationType)
            )
            SELECT
                ft.NoteSerial1 AS InvoiceNumber,
                ft.CashCustomerName AS CustomerName,
                ft.Transaction_Date AS InvoiceDate,
                ft.CashCustomerPhone AS CustomerPhone,
                ft.CardNumber,
                CASE WHEN ft.OperationType IN (N'cash-in', N'cash-out') THEN CAST(ft.RechargeValue AS DECIMAL(18, 2)) ELSE CAST(NULL AS DECIMAL(18, 2)) END AS RechargeAmount,
                COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'Branch ' + CONVERT(NVARCHAR(20), ft.BranchId)) AS Branch,
                COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), N'Store ' + CONVERT(NVARCHAR(20), ft.StoreID)) AS Store,
                u.UserName AS Cashier,
                ft.ServiceType,
                ft.Transaction_ID,
                ft.NetValue,
                ft.Vat,
                ft.TotalValue
            FROM FilteredTransactions ft
            LEFT JOIN dbo.TblBranchesData b ON b.branch_id = ft.BranchId
            LEFT JOIN dbo.TblStore s ON s.StoreID = ft.StoreID
            LEFT JOIN dbo.TblUsers u ON u.UserID = ft.UserID
            ORDER BY ft.Transaction_ID DESC;

            RETURN;
        END;

        IF @issueMode = N'summary'
        BEGIN
            CREATE TABLE #CardTx
            (
                Transaction_ID INT NOT NULL PRIMARY KEY,
                BranchId INT NULL,
                StoreID INT NULL,
                UserID INT NULL,
                CardNumber NVARCHAR(255) NULL,
                IssueTransactionID INT NULL
            );

            INSERT INTO #CardTx (Transaction_ID, BranchId, StoreID, UserID, CardNumber, IssueTransactionID)
            SELECT
                t.Transaction_ID,
                t.BranchId,
                t.StoreID,
                t.UserID,
                NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') AS CardNumber,
                CASE
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N'') IS NOT NULL
                      AND NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N'') NOT LIKE N'%[^0-9]%'
                        THEN CONVERT(INT, NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N''))
                    ELSE NULL
                END AS IssueTransactionID
            FROM dbo.Transactions t
            WHERE t.Transaction_Type = 21
              AND ISNULL(t.IsCancelled, 0) = 0
              AND t.Transaction_Date >= @from
              AND t.Transaction_Date < @toExclusive
              AND (@branchId <= 0 OR t.BranchId = @branchId)
              AND (@canChangeDefaults = 1 OR t.UserId = @userId)
              AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
              AND (@storeId IS NULL OR t.StoreID = @storeId)
              AND (@operationType IS NULL OR @operationType = N'card')
              AND ISNULL(t.TrafficViolations, 0) <> 1
              AND (NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1);

            CREATE NONCLUSTERED INDEX IX_CardTx_Card ON #CardTx(CardNumber);
            CREATE NONCLUSTERED INDEX IX_CardTx_Issue ON #CardTx(IssueTransactionID);

            CREATE TABLE #Tokens
            (
                Token NVARCHAR(255) NOT NULL PRIMARY KEY
            );

            INSERT INTO #Tokens (Token)
            SELECT DISTINCT CardNumber
            FROM #CardTx
            WHERE CardNumber IS NOT NULL;

            CREATE TABLE #Issue
            (
                Token NVARCHAR(255) NOT NULL PRIMARY KEY,
                IssueQty DECIMAL(18, 4) NOT NULL
            );

            INSERT INTO #Issue (Token, IssueQty)
            SELECT
                LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) AS Token,
                SUM(ISNULL(td.Quantity, 0)) AS IssueQty
            FROM dbo.Transaction_Details td
            INNER JOIN #CardTx ct ON ct.IssueTransactionID = td.Transaction_ID
            WHERE LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) <> N''
            GROUP BY LTRIM(RTRIM(ISNULL(td.ItemSerial, N'')));

            CREATE TABLE #Stock
            (
                Token NVARCHAR(255) NOT NULL PRIMARY KEY,
                StockQty DECIMAL(18, 4) NOT NULL
            );

            INSERT INTO #Stock (Token, StockQty)
            SELECT
                tok.Token,
                SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS StockQty
            FROM #Tokens tok
            INNER JOIN dbo.Transaction_Details td ON LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = tok.Token
            INNER JOIN dbo.Transactions mt ON mt.Transaction_ID = td.Transaction_ID
            INNER JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = mt.Transaction_Type
            WHERE ISNULL(tt.StockEffect, 0) <> 0
              AND mt.Transaction_Date < @toExclusive
            GROUP BY tok.Token;

            CREATE TABLE #Dup
            (
                Token NVARCHAR(255) NOT NULL PRIMARY KEY,
                InvoiceCount INT NOT NULL
            );

            INSERT INTO #Dup (Token, InvoiceCount)
            SELECT CardNumber, COUNT(1)
            FROM #CardTx
            WHERE CardNumber IS NOT NULL
            GROUP BY CardNumber;

            CREATE TABLE #Kyc
            (
                Token NVARCHAR(255) NOT NULL PRIMARY KEY
            );

            INSERT INTO #Kyc (Token)
            SELECT DISTINCT tok.Token
            FROM #Tokens tok
            INNER JOIN dbo.TblCusCsh c
                ON LTRIM(RTRIM(ISNULL(c.CardNo, N''))) = tok.Token
                OR LTRIM(RTRIM(ISNULL(c.CardId, N''))) = tok.Token
            WHERE ISNULL(c.EasyCashType, 0) = 0;

            ;WITH Classified AS
            (
                SELECT
                    ct.BranchId,
                    ct.StoreID,
                    CASE WHEN ct.CardNumber IS NULL THEN 1 ELSE 0 END AS MissingToken,
                    CASE WHEN ct.CardNumber IS NOT NULL AND ISNULL(d.InvoiceCount, 0) > 1 THEN 1 ELSE 0 END AS DuplicateCard,
                    CASE WHEN ct.CardNumber IS NOT NULL AND k.Token IS NULL THEN 1 ELSE 0 END AS MissingKyc,
                    CASE WHEN ct.CardNumber IS NOT NULL AND ISNULL(i.IssueQty, 0) <= 0 THEN 1 ELSE 0 END AS MissingIssueVoucher,
                    CASE WHEN ct.CardNumber IS NOT NULL AND ISNULL(st.StockQty, 0) < ISNULL(i.IssueQty, 0) THEN 1 ELSE 0 END AS PotentialNegativeStock
                FROM #CardTx ct
                LEFT JOIN #Issue i ON i.Token = ct.CardNumber
                LEFT JOIN #Stock st ON st.Token = ct.CardNumber
                LEFT JOIN #Dup d ON d.Token = ct.CardNumber
                LEFT JOIN #Kyc k ON k.Token = ct.CardNumber
            ),
            Rollup AS
            (
                SELECT
                    c.BranchId,
                    c.StoreID,
                    COUNT(1) AS TotalCards,
                    SUM(CASE WHEN c.MissingToken = 1 OR c.DuplicateCard = 1 OR c.MissingKyc = 1 OR c.MissingIssueVoucher = 1 OR c.PotentialNegativeStock = 1 THEN 1 ELSE 0 END) AS ProblematicCards,
                    SUM(c.PotentialNegativeStock) AS PotentialNegativeStockCards,
                    SUM(c.MissingIssueVoucher) AS MissingIssueVoucherCards,
                    SUM(c.MissingKyc) AS MissingKycCards,
                    SUM(c.DuplicateCard) AS DuplicateCardInvoices
                FROM Classified c
                GROUP BY c.BranchId, c.StoreID
            )
            SELECT
                COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'Branch ' + CONVERT(NVARCHAR(20), r.BranchId)) AS Branch,
                COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), N'Store ' + CONVERT(NVARCHAR(20), r.StoreID)) AS Store,
                r.TotalCards,
                r.ProblematicCards,
                r.PotentialNegativeStockCards,
                r.MissingIssueVoucherCards,
                r.MissingKycCards,
                r.DuplicateCardInvoices,
                r.TotalCards - r.ProblematicCards AS NormalCards,
                CAST(CASE WHEN r.TotalCards = 0 THEN 0 ELSE r.ProblematicCards * 100.0 / r.TotalCards END AS DECIMAL(18, 2)) AS ProblemRatio
            FROM Rollup r
            LEFT JOIN dbo.TblBranchesData b ON b.branch_id = r.BranchId
            LEFT JOIN dbo.TblStore s ON s.StoreID = r.StoreID
            WHERE r.ProblematicCards > 0
            ORDER BY r.ProblematicCards DESC, Store, Branch;

            RETURN;
        END;

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
                END AS ServiceType,
                CASE
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N'') IS NOT NULL
                      AND NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N'') NOT LIKE N'%[^0-9]%'
                        THEN CONVERT(INT, NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N''))
                    ELSE NULL
                END AS IssueTransactionID
            FROM dbo.Transactions t
            WHERE t.Transaction_Type = 21
              AND ISNULL(t.IsCancelled, 0) = 0
              AND t.Transaction_Date >= @from
              AND t.Transaction_Date < @toExclusive
              AND (@branchId <= 0 OR t.BranchId = @branchId)
              AND (@canChangeDefaults = 1 OR t.UserId = @userId)
              AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
              AND (@storeId IS NULL OR t.StoreID = @storeId)
        ),
        FilteredTransactions AS
        (
            SELECT *
            FROM BaseTransactions
            WHERE (@operationType IS NULL OR OperationType = @operationType)
        )
        SELECT
            ft.NoteSerial1 AS InvoiceNumber,
            ft.CashCustomerName AS CustomerName,
            ft.Transaction_Date AS InvoiceDate,
            ft.CashCustomerPhone AS CustomerPhone,
            ft.CardNumber,
            CASE WHEN ft.OperationType IN (N'cash-in', N'cash-out') THEN CAST(ft.RechargeValue AS DECIMAL(18, 2)) ELSE CAST(NULL AS DECIMAL(18, 2)) END AS RechargeAmount,
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), ft.BranchId)) AS Branch,
            COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), N'مخزن ' + CONVERT(NVARCHAR(20), ft.StoreID)) AS Store,
            u.UserName AS Cashier,
            ft.ServiceType,
            CASE
                WHEN ft.OperationType <> N'card' THEN N''
                WHEN ft.CardNumber IS NULL THEN N'Problematic Card'
                WHEN ISNULL(card.DuplicateInvoiceCount, 0) > 1 THEN N'Problematic Card'
                WHEN ISNULL(card.HasKycCustomer, 0) = 0 THEN N'Problematic Card'
                WHEN ISNULL(card.IssueQty, 0) <= 0 THEN N'Problematic Card'
                WHEN ISNULL(card.StockBefore, 0) <= 0 THEN N'Insufficient Balance'
                WHEN ISNULL(card.StockBefore, 0) - ISNULL(card.IssueQty, 0) < 0 THEN N'Negative'
                ELSE N'Normal'
            END AS CardIssueStatus,
            CASE
                WHEN ft.OperationType <> N'card' THEN N''
                WHEN ft.CardNumber IS NULL THEN N'كارت به مشكلة'
                WHEN ISNULL(card.DuplicateInvoiceCount, 0) > 1 THEN N'كارت مكرر'
                WHEN ISNULL(card.HasKycCustomer, 0) = 0 THEN N'بيانات KYC غير موجودة'
                WHEN ISNULL(card.IssueQty, 0) <= 0 THEN N'إذن صرف غير موجود'
                WHEN ISNULL(card.StockBefore, 0) <= 0 THEN N'رصيد غير كاف'
                WHEN ISNULL(card.StockBefore, 0) - ISNULL(card.IssueQty, 0) < 0 THEN N'رصيد سالب'
                ELSE N'طبيعي'
            END AS CardIssueStatusAr,
            CASE WHEN ft.OperationType = N'card' THEN CAST(ISNULL(card.StockBefore, 0) AS DECIMAL(18, 4)) ELSE CAST(NULL AS DECIMAL(18, 4)) END AS CardStockBefore,
            CASE WHEN ft.OperationType = N'card' THEN CAST(ISNULL(card.StockBefore, 0) - ISNULL(card.IssueQty, 0) AS DECIMAL(18, 4)) ELSE CAST(NULL AS DECIMAL(18, 4)) END AS CardStockAfter,
            ft.Transaction_ID,
            ft.NetValue,
            ft.Vat,
            ft.TotalValue
        FROM FilteredTransactions ft
        LEFT JOIN dbo.TblBranchesData b ON b.branch_id = ft.BranchId
        LEFT JOIN dbo.TblStore s ON s.StoreID = ft.StoreID
        LEFT JOIN dbo.TblUsers u ON u.UserID = ft.UserID
        OUTER APPLY
        (
            SELECT
                ISNULL((
                    SELECT SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0))
                    FROM dbo.Transaction_Details td
                    INNER JOIN dbo.Transactions mt ON mt.Transaction_ID = td.Transaction_ID
                    INNER JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = mt.Transaction_Type
                    WHERE ft.OperationType = N'card'
                      AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = ft.CardNumber
                      AND ISNULL(tt.StockEffect, 0) <> 0
                      AND mt.Transaction_ID < ft.Transaction_ID
                ), 0) AS StockBefore,
                ISNULL((
                    SELECT SUM(ISNULL(td.Quantity, 0))
                    FROM dbo.Transaction_Details td
                    WHERE ft.OperationType = N'card'
                      AND td.Transaction_ID = ft.IssueTransactionID
                      AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = ft.CardNumber
                ), 0) AS IssueQty,
                (
                    SELECT COUNT(1)
                    FROM dbo.Transactions dup
                    WHERE ft.OperationType = N'card'
                      AND dup.Transaction_Type = 21
                      AND ISNULL(dup.IsCancelled, 0) = 0
                      AND NULLIF(LTRIM(RTRIM(ISNULL(dup.VisaNumber, N''))), N'') = ft.CardNumber
                ) AS DuplicateInvoiceCount,
                CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.TblCusCsh c
                    WHERE ft.OperationType = N'card'
                      AND ISNULL(c.EasyCashType, 0) = 0
                      AND (LTRIM(RTRIM(ISNULL(c.CardNo, N''))) = ft.CardNumber OR LTRIM(RTRIM(ISNULL(c.CardId, N''))) = ft.CardNumber)
                ) THEN 1 ELSE 0 END AS HasKycCustomer
        ) card
        ORDER BY ft.Transaction_ID DESC;

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
        ;WITH BranchScope AS
        (
            SELECT
                b.branch_id AS BranchID,
                COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), b.branch_id)) AS BranchName
            FROM dbo.TblBranchesData b
            WHERE b.branch_id IS NOT NULL
              AND (@branchId <= 0 OR b.branch_id = @branchId)
              AND (@branchId > 0 OR @branchFromId IS NULL OR b.branch_id >= @branchFromId)
              AND (@branchId > 0 OR @branchToId IS NULL OR b.branch_id <= @branchToId)
              AND
              (
                  @serviceTerm IS NULL
                  OR EXISTS
                  (
                      SELECT 1
                      FROM dbo.Transactions st
                      LEFT JOIN dbo.TblItems si1 ON si1.ItemID = st.ItemIDService
                      LEFT JOIN dbo.TblItems si2 ON si2.ItemID = st.ItemIDService2
                      LEFT JOIN dbo.TblItems si3 ON si3.ItemID = st.ItemIDService3
                      WHERE st.BranchId = b.branch_id
                        AND st.Transaction_Date >= @from
                        AND st.Transaction_Date < @toExclusive
                        AND
                        (
                            CONVERT(NVARCHAR(20), ISNULL(st.ItemIDService, 0)) = @serviceTerm
                            OR CONVERT(NVARCHAR(20), ISNULL(st.ItemIDService2, 0)) = @serviceTerm
                            OR CONVERT(NVARCHAR(20), ISNULL(st.ItemIDService3, 0)) = @serviceTerm
                            OR ISNULL(si1.ItemName, N'') LIKE N'%' + @serviceTerm + N'%'
                            OR ISNULL(si2.ItemName, N'') LIKE N'%' + @serviceTerm + N'%'
                            OR ISNULL(si3.ItemName, N'') LIKE N'%' + @serviceTerm + N'%'
                        )
                  )
              )
        ),
        CloseRows AS
        (
            SELECT
                c.BranchID,
                b.BranchName,
                ISNULL(c.Net, 0) AS Net,
                ISNULL(c.TotalSaleDay2Vat, 0) AS TotalSaleDay2Vat,
                ISNULL(c.TotalRevPOS, 0) AS TotalRevPOS,
                ISNULL(c.NetPOS, 0) AS NetPOS,
                ISNULL(c.CountCards, 0) AS CountCards,
                ISNULL(c.TotalSaleDay2, 0) AS CardValue,
                ISNULL(c.CountTransaction, 0) AS CountTransaction,
                ISNULL(c.CashOutTotal, 0) AS CashOutTotal,
                ISNULL(c.CashOut, 0) AS CashOut,
                ISNULL(c.CashOutDisc, 0) AS CashOutDisc,
                ISNULL(c.BankBalanceCharge, 0) AS BankBalanceCharge,
                ISNULL(c.TotalRechargeValue, 0) AS TotalRechargeValue,
                ISNULL(c.TotalRev2, 0) AS TotalRev2,
                ISNULL(c.TotalRevvat, 0) AS TotalRevvat,
                ISNULL(c.BoxBalance, 0) AS BoxValue,
                ISNULL(c.IsClosed, 0) AS IsClosed
            FROM dbo.TBLClosePos c
            INNER JOIN BranchScope b ON b.BranchID = c.BranchID
            WHERE c.OrderDate >= @from
              AND c.OrderDate < @toExclusive
              AND (@canChangeDefaults = 1 OR c.UserID = @userId)
        ),
        ReturnsByBranch AS
        (
            SELECT
                t.BranchId AS BranchID,
                COUNT(1) AS ReturnsCount,
                SUM(ISNULL(t.Transaction_NetValue, 0) + ISNULL(t.RechargeValue, 0)) AS TotalReturns
            FROM dbo.Transactions t
            INNER JOIN BranchScope b ON b.BranchID = t.BranchId
            WHERE t.Transaction_Type = 9
              AND t.Transaction_Date >= @from
              AND t.Transaction_Date < @toExclusive
              AND
              (
                  @canChangeDefaults = 1
                  OR t.UserID = @userId
              )
            GROUP BY t.BranchId
        ),
        BranchRollup AS
        (
            SELECT
                c.BranchID,
                c.BranchName,
                SUM(c.Net) AS Net,
                SUM(c.TotalSaleDay2Vat) AS TotalSaleDay2Vat,
                SUM(c.TotalRevPOS) AS TotalRevPOS,
                SUM(c.NetPOS) AS NetPOS,
                MAX(ISNULL(r.TotalReturns, 0)) AS TotalReturns,
                SUM(c.CountCards) AS CountCards,
                SUM(c.CardValue) AS CardValue,
                SUM(c.CountTransaction) AS CountTransaction,
                SUM(c.CashOutTotal) AS CashOutTotal,
                SUM(c.CashOut) AS CashOut,
                SUM(c.CashOutDisc) AS CashOutDisc,
                SUM(c.BankBalanceCharge) AS BankBalanceCharge,
                SUM(c.TotalRechargeValue) AS TotalRechargeValue,
                SUM(c.TotalRev2) AS TotalRev2,
                SUM(c.TotalRevvat) AS TotalRevvat,
                MAX(ISNULL(r.ReturnsCount, 0)) AS ReturnsCount,
                SUM(c.BoxValue) AS BoxValue,
                MIN(CONVERT(INT, c.IsClosed)) AS MinClosed,
                MAX(CONVERT(INT, c.IsClosed)) AS MaxClosed
            FROM CloseRows c
            LEFT JOIN ReturnsByBranch r ON r.BranchID = c.BranchID
            GROUP BY c.BranchID, c.BranchName
        )
        SELECT
            ROW_NUMBER() OVER (ORDER BY s.BranchName, s.BranchID) AS RowNo,
            s.BranchName,
            CAST((ISNULL(r.Net, 0) + ISNULL(r.TotalSaleDay2Vat, 0) + ISNULL(r.TotalRevPOS, 0) + ISNULL(r.NetPOS, 0) - ISNULL(r.TotalReturns, 0)) AS DECIMAL(18, 3)) AS TotalSupply,
            CAST(ISNULL(r.CountCards, 0) AS DECIMAL(18, 0)) AS CountCards,
            CAST(ISNULL(r.CardValue, 0) AS DECIMAL(18, 3)) AS CardValue,
            CAST(ISNULL(r.CountTransaction, 0) AS DECIMAL(18, 0)) AS CountTransaction,
            CAST((ISNULL(r.CashOutTotal, 0) + ISNULL(r.CashOut, 0)) AS DECIMAL(18, 3)) AS WalletBalance,
            CAST((ISNULL(r.CashOutTotal, 0) + (ISNULL(r.CashOut, 0) - ISNULL(r.CashOutDisc, 0))) AS DECIMAL(18, 3)) AS WalletSupply,
            CAST(ISNULL(r.BankBalanceCharge, 0) AS DECIMAL(18, 3)) AS BankBalanceCharge,
            CAST(ISNULL(r.TotalRechargeValue, 0) AS DECIMAL(18, 3)) AS TotalRechargeValue,
            CAST(ISNULL(r.TotalRev2, 0) AS DECIMAL(18, 3)) AS TotalRev2,
            CAST((ISNULL(r.TotalRev2, 0) + ISNULL(r.TotalRevvat, 0)) AS DECIMAL(18, 3)) AS TotalRevWithVat,
            ISNULL(r.ReturnsCount, 0) AS ReturnsCount,
            CAST(ISNULL(r.TotalReturns, 0) AS DECIMAL(18, 3)) AS TotalReturns,
            CAST((ISNULL(r.CashOut, 0) + ISNULL(r.CashOutTotal, 0) - ISNULL(r.CashOutDisc, 0)) AS DECIMAL(18, 3)) AS NetCashOut,
            CAST(ISNULL(r.BoxValue, 0) AS DECIMAL(18, 3)) AS BoxValue,
            CASE
                WHEN r.BranchID IS NULL
                  OR
                  (
                      ISNULL(r.Net, 0) = 0
                      AND ISNULL(r.TotalSaleDay2Vat, 0) = 0
                      AND ISNULL(r.TotalRevPOS, 0) = 0
                      AND ISNULL(r.NetPOS, 0) = 0
                      AND ISNULL(r.TotalReturns, 0) = 0
                      AND ISNULL(r.CountCards, 0) = 0
                      AND ISNULL(r.CardValue, 0) = 0
                      AND ISNULL(r.CountTransaction, 0) = 0
                      AND ISNULL(r.CashOutTotal, 0) = 0
                      AND ISNULL(r.CashOut, 0) = 0
                      AND ISNULL(r.CashOutDisc, 0) = 0
                      AND ISNULL(r.BankBalanceCharge, 0) = 0
                      AND ISNULL(r.TotalRechargeValue, 0) = 0
                      AND ISNULL(r.TotalRev2, 0) = 0
                      AND ISNULL(r.TotalRevvat, 0) = 0
                      AND ISNULL(r.ReturnsCount, 0) = 0
                      AND ISNULL(r.BoxValue, 0) = 0
                  ) THEN N''
                WHEN r.MinClosed = 1 AND r.MaxClosed = 1 THEN N'مغلق'
                WHEN r.MaxClosed = 1 THEN N'إغلاق جزئي'
                ELSE N'غير مغلق'
            END AS ClosingStatus
        FROM BranchScope s
        LEFT JOIN BranchRollup r ON r.BranchID = s.BranchID
        WHERE
            @showEmptyBranches = 1
            OR
            (
                r.BranchID IS NOT NULL
                AND
                (
                    ISNULL(r.Net, 0) <> 0
                    OR ISNULL(r.TotalSaleDay2Vat, 0) <> 0
                    OR ISNULL(r.TotalRevPOS, 0) <> 0
                    OR ISNULL(r.NetPOS, 0) <> 0
                    OR ISNULL(r.TotalReturns, 0) <> 0
                    OR ISNULL(r.CountCards, 0) <> 0
                    OR ISNULL(r.CardValue, 0) <> 0
                    OR ISNULL(r.CountTransaction, 0) <> 0
                    OR ISNULL(r.CashOutTotal, 0) <> 0
                    OR ISNULL(r.CashOut, 0) <> 0
                    OR ISNULL(r.CashOutDisc, 0) <> 0
                    OR ISNULL(r.BankBalanceCharge, 0) <> 0
                    OR ISNULL(r.TotalRechargeValue, 0) <> 0
                    OR ISNULL(r.TotalRev2, 0) <> 0
                    OR ISNULL(r.TotalRevvat, 0) <> 0
                    OR ISNULL(r.ReturnsCount, 0) <> 0
                    OR ISNULL(r.BoxValue, 0) <> 0
                )
            )
        ORDER BY s.BranchName, s.BranchID;

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
