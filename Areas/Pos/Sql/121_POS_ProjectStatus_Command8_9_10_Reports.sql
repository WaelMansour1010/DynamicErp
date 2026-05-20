SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.usp_POS_ProjectStatus_Report_Run', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_ProjectStatus_Report_Run;
GO

CREATE PROCEDURE dbo.usp_POS_ProjectStatus_Report_Run
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
    @filterUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = CONVERT(DATE, @fromDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @toDate));
    DECLARE @serviceTerm NVARCHAR(100) = NULLIF(LTRIM(RTRIM(ISNULL(@serviceSearch, N''))), N'');
    DECLARE @operationType NVARCHAR(30) = LOWER(NULLIF(LTRIM(RTRIM(ISNULL(@serviceType, N''))), N''));

    IF @toExclusive < @from
    BEGIN
        DECLARE @swap DATETIME = @from;
        SET @from = DATEADD(DAY, -1, @toExclusive);
        SET @toExclusive = DATEADD(DAY, 1, @swap);
    END;

    IF @operationType NOT IN (N'cash-in', N'cash-out', N'card', N'violations')
        SET @operationType = NULL;

    ;WITH BranchScope AS
    (
        SELECT
            b.branch_id AS BranchID,
            LTRIM(RTRIM(COALESCE(NULLIF(b.branch_Code, N'') + N' ', N'') + COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), b.branch_id)))) AS BranchName,
            b.branch_Code AS BranchCode,
            b.branch_name AS BranchArabicName
        FROM dbo.TblBranchesData b
        WHERE b.branch_id IS NOT NULL
          AND (@branchId <= 0 OR b.branch_id = @branchId)
          AND (@branchId > 0 OR @branchFromId IS NULL OR b.branch_id >= @branchFromId)
          AND (@branchId > 0 OR @branchToId IS NULL OR b.branch_id <= @branchToId)
          AND (ISNULL(b.isStoped, 0) = 0 OR ISNULL(b.IsStopedDate, '99991231') > @from)
          AND (
                @branchId > 0
                OR @canChangeDefaults = 1
                OR EXISTS
                (
                    SELECT 1
                    FROM dbo.TblUsersBranches ub
                    WHERE ub.UserID = @userId
                      AND ub.BranchID = b.branch_id
                )
              )
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
                    AND st.Transaction_Type = 21
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
    TxBase AS
    (
        SELECT
            t.Transaction_ID,
            t.BranchId,
            t.StoreID,
            t.UserID,
            t.Transaction_Date,
            ISNULL(t.RechargeValue, 0) AS RechargeValue,
            ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0)) AS TransactionNetValue,
            ISNULL(t.NetValue, 0) AS NetValue,
            ISNULL(t.Vat, 0) AS Vat,
            ISNULL(t.Cost, 0) AS Cost,
            ISNULL(t.CashBack, 0) AS CashBack,
            ISNULL(t.IsWallet, 0) AS IsWallet,
            ISNULL(t.HaveGuarantee, 0) AS HaveGuarantee,
            ISNULL(t.OtherItems, 0) AS OtherItems,
            ISNULL(t.InstallmentService, 0) AS InstallmentService,
            ISNULL(t.IsReturn, 0) AS IsReturn,
            ISNULL(t.IsCashOut, 0) AS IsCashOut,
            ISNULL(t.IsPOS, 0) AS IsPOS,
            ISNULL(t.TrafficViolations, 0) AS TrafficViolations,
            NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') AS VisaNumber,
            CASE
                WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                WHEN ISNULL(t.IsCashOut, 0) = 1 OR ISNULL(t.IsWallet, 0) = 1 THEN N'cash-out'
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1 THEN N'card'
                ELSE N'cash-in'
            END AS OperationType
        FROM dbo.Transactions t
        INNER JOIN BranchScope b ON b.BranchID = t.BranchId
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND (@canChangeDefaults = 1 OR t.UserID = @userId)
          AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
          AND (@storeId IS NULL OR t.StoreID = @storeId)
          AND
          (
              @serviceTerm IS NULL
              OR CONVERT(NVARCHAR(20), ISNULL(t.ItemIDService, 0)) = @serviceTerm
              OR CONVERT(NVARCHAR(20), ISNULL(t.ItemIDService2, 0)) = @serviceTerm
              OR CONVERT(NVARCHAR(20), ISNULL(t.ItemIDService3, 0)) = @serviceTerm
          )
    ),
    TxFiltered AS
    (
        SELECT *
        FROM TxBase
        WHERE @operationType IS NULL OR OperationType = @operationType
    ),
    TxAgg AS
    (
        SELECT
            t.BranchId,
            SUM(CASE WHEN t.IsWallet = 0 AND t.HaveGuarantee = 0 AND t.InstallmentService = 0 AND t.RechargeValue <> 0 THEN t.RechargeValue ELSE 0 END) AS TotalRechargeValue,
            COUNT(CASE WHEN t.IsWallet = 0 AND t.HaveGuarantee = 0 AND t.InstallmentService = 0 AND t.RechargeValue <> 0 THEN 1 END) AS CountTransactionIn,
            SUM(CASE WHEN t.IsWallet = 1 AND t.HaveGuarantee = 0 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN t.RechargeValue ELSE 0 END) AS CashOutTotal,
            SUM(CASE WHEN t.IsWallet = 1 AND t.HaveGuarantee = 0 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN t.TransactionNetValue ELSE 0 END) AS CashOut,
            COUNT(CASE WHEN t.IsWallet = 1 AND t.HaveGuarantee = 0 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN 1 END) AS CountTransactionOut,
            SUM(CASE WHEN t.IsWallet = 1 AND t.HaveGuarantee = 0 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN
                    CASE
                        WHEN t.Transaction_Date >= '20250701' THEN t.Cost - t.CashBack
                        WHEN t.Transaction_Date > '20240825' THEN (t.RechargeValue + t.TransactionNetValue) * 0.008
                        ELSE (t.RechargeValue + t.TransactionNetValue) * 0.01
                    END
                ELSE 0 END) AS CashOutDisc,
            SUM(CASE WHEN t.IsWallet = 0 AND t.HaveGuarantee = 0 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN t.TransactionNetValue ELSE 0 END) AS TotalRevSS,
            SUM(CASE WHEN t.HaveGuarantee = 1 AND t.RechargeValue <> 0 THEN t.RechargeValue ELSE 0 END) AS NetPOS,
            SUM(CASE WHEN t.HaveGuarantee = 1 AND t.OtherItems = 0 AND t.InstallmentService = 0 AND t.TransactionNetValue <> 0 THEN t.TransactionNetValue ELSE 0 END) AS TotalRevPOS,
            COUNT(CASE WHEN t.HaveGuarantee = 1 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN 1 END) AS CountTransactionPOS,
            SUM(CASE WHEN t.OtherItems = 1 AND t.TransactionNetValue <> 0 THEN t.TransactionNetValue ELSE 0 END) AS TawqeefiTotal,
            COUNT(CASE WHEN t.OtherItems = 1 THEN 1 END) AS CountTawqeefi,
            SUM(CASE WHEN t.InstallmentService = 1 THEN t.TransactionNetValue ELSE 0 END) AS TotalInstallmentRevVat,
            SUM(CASE WHEN t.InstallmentService = 1 THEN t.RechargeValue + ISNULL(t.NetValue, 0) ELSE 0 END) AS InstallmentTotal,
            COUNT(CASE WHEN t.InstallmentService = 1 AND t.RechargeValue <> 0 THEN 1 END) AS CountInstallment
        FROM TxFiltered t
        GROUP BY t.BranchId
    ),
    DetailAgg AS
    (
        SELECT
            t.BranchId,
            SUM(CASE WHEN i.ItemType = 0 AND ISNULL(i.HaveSerial, 0) = 1 AND t.IsWallet = 0 AND t.HaveGuarantee = 0 AND t.IsReturn = 0 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN ISNULL(td.Price, 0) ELSE 0 END) AS TotalSaleDay2,
            SUM(CASE WHEN i.ItemType = 0 AND ISNULL(i.HaveSerial, 0) = 1 AND t.IsWallet = 0 AND t.HaveGuarantee = 0 AND t.IsReturn = 0 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN ISNULL(td.Vat, 0) ELSE 0 END) AS TotalSaleDay2Vat,
            COUNT(CASE WHEN i.ItemType = 0 AND ISNULL(i.HaveSerial, 0) = 1 AND t.IsWallet = 0 AND t.HaveGuarantee = 0 AND t.IsReturn = 0 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN 1 END) AS CountCards,
            SUM(CASE WHEN ISNULL(i.HaveSerial, 0) = 0 AND t.IsWallet = 0 AND t.HaveGuarantee = 0 AND t.IsReturn = 0 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN ISNULL(td.Price, 0) ELSE 0 END) AS TotalRev2,
            SUM(CASE WHEN ISNULL(i.HaveSerial, 0) = 0 AND t.IsWallet = 0 AND t.HaveGuarantee = 0 AND t.IsReturn = 0 AND t.OtherItems = 0 AND t.InstallmentService = 0 THEN ISNULL(td.Vat, 0) ELSE 0 END) AS TotalRevVat,
            COUNT(CASE WHEN i.ItemType = 1 AND ISNULL(td.Price, 0) <> 0 AND t.IsWallet = 0 AND t.HaveGuarantee = 0 AND t.IsReturn = 0 THEN 1 END) AS CountTransaction
        FROM TxFiltered t
        INNER JOIN dbo.Transaction_Details td ON td.Transaction_ID = t.Transaction_ID
        INNER JOIN dbo.TblItems i ON i.ItemID = td.Item_ID
        GROUP BY t.BranchId
    ),
    ReturnsAgg AS
    (
        SELECT
            r.BranchId,
            COUNT(1) AS ReturnsCount,
            SUM(ISNULL(r.Transaction_NetValue, 0) + ISNULL(r.RechargeValue, 0)) AS TotalReturns
        FROM dbo.Transactions r
        INNER JOIN dbo.Notes n ON n.NoteID = ISNULL(r.NoteID3, 0)
        INNER JOIN BranchScope b ON b.BranchID = r.BranchId
        WHERE r.Transaction_Type = 9
          AND n.NoteDate >= @from
          AND n.NoteDate < @toExclusive
          AND n.branch_no = r.BranchId
          AND (@canChangeDefaults = 1 OR r.UserID = @userId)
          AND (@filterUserId IS NULL OR r.UserID = @filterUserId)
        GROUP BY r.BranchId
    ),
    CloseAgg AS
    (
        SELECT
            c.BranchID,
            SUM(ISNULL(c.BoxBalance, 0)) AS BoxValue,
            MAX(CONVERT(INT, ISNULL(c.IsClosed, 0))) AS IsClosed
        FROM dbo.TBLClosePos c
        INNER JOIN BranchScope b ON b.BranchID = c.BranchID
        WHERE c.OrderDate >= @from
          AND c.OrderDate < @toExclusive
          AND (@canChangeDefaults = 1 OR c.UserID = @userId)
          AND (@filterUserId IS NULL OR c.UserID = @filterUserId)
        GROUP BY c.BranchID
    ),
    BankAgg AS
    (
        SELECT
            d.branch_id AS BranchID,
            SUM(ISNULL(d.Value, 0)) AS BankBalanceCharge
        FROM dbo.DOUBLE_ENTREY_VOUCHERS d
        INNER JOIN BranchScope b ON b.BranchID = d.branch_id
        WHERE d.Credit_Or_Debit = 0
          AND d.RecordDate >= @from
          AND d.RecordDate < @toExclusive
          AND d.Account_Code = N'a3a1a1a1a6'
          AND d.Posted IS NULL
        GROUP BY d.branch_id
    ),
    ClosedNotes AS
    (
        SELECT n.branch_no AS BranchID, 1 AS IsClosedByNote
        FROM dbo.Notes n
        INNER JOIN BranchScope b ON b.BranchID = n.branch_no
        WHERE n.NoteType = 29806
          AND n.NoteDate >= @from
          AND n.NoteDate < @toExclusive
        GROUP BY n.branch_no
    ),
    Rollup AS
    (
        SELECT
            b.BranchID,
            b.BranchName,
            b.BranchCode,
            ISNULL(tx.TotalRechargeValue, 0) AS TotalRechargeValue,
            ISNULL(tx.CountTransactionIn, 0) AS CountTransactionIn,
            ISNULL(tx.CashOutTotal, 0) AS CashOutTotal,
            ISNULL(tx.CashOut, 0) AS CashOut,
            ISNULL(tx.CountTransactionOut, 0) AS CountTransactionOut,
            ISNULL(tx.CashOutDisc, 0) AS CashOutDisc,
            ISNULL(tx.TotalRevSS, 0) AS TotalRevSS,
            ISNULL(tx.NetPOS, 0) AS NetPOS,
            ISNULL(tx.TotalRevPOS, 0) AS TotalRevPOS,
            ISNULL(tx.CountTransactionPOS, 0) AS CountTransactionPOS,
            ISNULL(tx.TawqeefiTotal, 0) AS TawqeefiTotal,
            ISNULL(tx.CountTawqeefi, 0) AS CountTawqeefi,
            ISNULL(tx.TotalInstallmentRevVat, 0) AS TotalInstallmentRevVat,
            ISNULL(tx.InstallmentTotal, 0) AS InstallmentTotal,
            ISNULL(tx.CountInstallment, 0) AS CountInstallment,
            ISNULL(da.TotalSaleDay2, 0) AS TotalSaleDay2,
            ISNULL(da.TotalSaleDay2Vat, 0) AS TotalSaleDay2Vat,
            ISNULL(da.CountCards, 0) AS CountCards,
            ISNULL(da.TotalRev2, 0) AS TotalRev2,
            ISNULL(da.TotalRevVat, 0) AS TotalRevVat,
            ISNULL(da.CountTransaction, 0) AS CountTransaction,
            ISNULL(ret.ReturnsCount, 0) AS ReturnsCount,
            ISNULL(ret.TotalReturns, 0) AS TotalReturns,
            ISNULL(cl.BoxValue, 0) AS BoxValue,
            ISNULL(cl.IsClosed, 0) AS IsClosed,
            ISNULL(cn.IsClosedByNote, 0) AS IsClosedByNote,
            ISNULL(ba.BankBalanceCharge, 0) AS BankBalanceCharge
        FROM BranchScope b
        LEFT JOIN TxAgg tx ON tx.BranchId = b.BranchID
        LEFT JOIN DetailAgg da ON da.BranchId = b.BranchID
        LEFT JOIN ReturnsAgg ret ON ret.BranchId = b.BranchID
        LEFT JOIN CloseAgg cl ON cl.BranchID = b.BranchID
        LEFT JOIN ClosedNotes cn ON cn.BranchID = b.BranchID
        LEFT JOIN BankAgg ba ON ba.BranchID = b.BranchID
    )
    SELECT
        ROW_NUMBER() OVER (ORDER BY r.BranchName, r.BranchID) AS RowNo,
        r.BranchID,
        r.BranchName,
        CAST(CASE WHEN @reportKey = N'finance-closing'
             THEN r.TotalRechargeValue + r.TotalRevPOS + r.NetPOS + (r.TotalRevSS - r.TotalSaleDay2 - r.TotalSaleDay2Vat) + r.CashOut - r.CashOutDisc
             ELSE r.TotalRechargeValue + (r.TotalRevSS - r.TotalSaleDay2 - r.TotalSaleDay2Vat) + r.CashOut - r.CashOutDisc
        END AS DECIMAL(18, 3)) AS TotalSupply,
        CAST(r.CountCards AS DECIMAL(18, 0)) AS CountCards,
        CAST(r.TotalSaleDay2Vat AS DECIMAL(18, 3)) AS TotalSaleDay2Vat,
        CAST(r.TotalSaleDay2 + r.TotalSaleDay2Vat AS DECIMAL(18, 3)) AS CardValue,
        CAST(r.CountTransactionIn + r.CountTransactionOut + CASE WHEN @reportKey = N'finance-closing' THEN r.CountTransactionPOS ELSE 0 END AS DECIMAL(18, 0)) AS CountTransaction,
        CAST(r.CashOutTotal + r.CashOut AS DECIMAL(18, 3)) AS WalletBalance,
        CAST(r.CashOutTotal + r.CashOut - r.CashOutDisc AS DECIMAL(18, 3)) AS WalletSupply,
        CAST(r.BankBalanceCharge AS DECIMAL(18, 3)) AS BankBalanceCharge,
        CAST(r.TotalRechargeValue AS DECIMAL(18, 3)) AS TotalRechargeValue,
        CAST(r.TotalRev2 AS DECIMAL(18, 3)) AS TotalRev2,
        CAST(r.TotalRevVat AS DECIMAL(18, 3)) AS TotalRevVat,
        CAST(r.TotalRev2 + r.TotalRevVat AS DECIMAL(18, 3)) AS TotalRevWithVat,
        CAST(r.ReturnsCount AS DECIMAL(18, 0)) AS ReturnsCount,
        CAST(r.TotalReturns AS DECIMAL(18, 3)) AS TotalReturns,
        CAST(r.CashOutTotal AS DECIMAL(18, 3)) AS NetCashOut,
        CAST(r.BoxValue AS DECIMAL(18, 3)) AS BoxValue,
        CASE WHEN r.IsClosed = 1 OR r.IsClosedByNote = 1 THEN N'تم الإغلاق' ELSE N'غير مغلق' END AS ClosingStatus,
        CAST(r.TotalSaleDay2 AS DECIMAL(18, 3)) AS TotalSaleDay2,
        CAST(r.CountTransactionIn AS DECIMAL(18, 0)) AS CountTransactionIn,
        CAST(r.CountTransactionOut AS DECIMAL(18, 0)) AS CountTransactionOut,
        CAST(r.CashOut AS DECIMAL(18, 3)) AS WalletNet,
        CAST(r.TotalRevPOS + r.NetPOS AS DECIMAL(18, 3)) AS PosTotal,
        CAST(r.TawqeefiTotal AS DECIMAL(18, 3)) AS TawqeefiTotal,
        CAST(r.TotalInstallmentRevVat AS DECIMAL(18, 3)) AS TotalInstallmentRevVat,
        CAST(r.InstallmentTotal AS DECIMAL(18, 3)) AS InstallmentTotal,
        CAST(r.CountInstallment AS DECIMAL(18, 0)) AS CountInstallment,
        CAST(r.CountTransactionPOS AS DECIMAL(18, 0)) AS CountTransactionPOS,
        CAST(0 AS DECIMAL(18, 3)) AS NetPosFinalProfit,
        CAST(0 AS DECIMAL(18, 3)) AS Bank9995,
        CAST(0 AS DECIMAL(18, 3)) AS Bank8846
    FROM Rollup r
    WHERE @showEmptyBranches = 1
       OR r.TotalRechargeValue <> 0
       OR r.TotalRevSS <> 0
       OR r.NetPOS <> 0
       OR r.TotalRevPOS <> 0
       OR r.CashOutTotal <> 0
       OR r.CashOut <> 0
       OR r.CashOutDisc <> 0
       OR r.TotalSaleDay2 <> 0
       OR r.TotalSaleDay2Vat <> 0
       OR r.TotalRev2 <> 0
       OR r.TotalRevVat <> 0
       OR r.TotalReturns <> 0
       OR r.BoxValue <> 0
       OR r.BankBalanceCharge <> 0
    ORDER BY r.BranchName, r.BranchID
    OPTION (RECOMPILE);
END;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'IX_POS_ProjectStatus_Tx_Date_Branch_User')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_ProjectStatus_Tx_Date_Branch_User
    ON dbo.Transactions (Transaction_Type, Transaction_Date, BranchId, UserID)
    INCLUDE (StoreID, RechargeValue, Transaction_NetValue, PayedValue, NetValue, Vat, IsWallet, HaveGuarantee, OtherItems, InstallmentService, IsReturn, IsCashOut, IsPOS, TrafficViolations, VisaNumber, Cost, CashBack, ItemIDService, ItemIDService2, ItemIDService3, NoteID3);
END;
GO

IF OBJECT_ID(N'dbo.Transaction_Details', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transaction_Details') AND name = N'IX_POS_ProjectStatus_Details_Tx')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_ProjectStatus_Details_Tx
    ON dbo.Transaction_Details (Transaction_ID, Item_ID)
    INCLUDE (Price, Vat);
END;
GO

IF OBJECT_ID(N'dbo.Notes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Notes') AND name = N'IX_POS_ProjectStatus_Notes_Type_Date_Branch')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_ProjectStatus_Notes_Type_Date_Branch
    ON dbo.Notes (NoteType, NoteDate, branch_no)
    INCLUDE (NoteID);
END;
GO

