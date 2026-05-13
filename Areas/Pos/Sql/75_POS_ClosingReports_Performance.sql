IF OBJECT_ID(N'dbo.TBLClosePos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TBLClosePos') AND name = N'IX_POS_TBLClosePos_Report_Order_Branch_User')
   AND COL_LENGTH(N'dbo.TBLClosePos', N'OrderDate') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'BranchID') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'UserID') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'ID') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'NoteID') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'NoteSerial') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'NoteSerial1') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'OpenBalance') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'LastBalance') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalRechargeValue') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalRev') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalVat') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'CashOutTotal') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalSupply') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'BoxBalance') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'Net') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalSaleDay2Vat') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalRevPOS') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'NetPOS') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'CountCards') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalSaleDay2') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'CountTransaction') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'CashOut') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'CashOutDisc') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'BankBalanceCharge') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalRev2') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalRevvat') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'IsClosed') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TBLClosePos_Report_Order_Branch_User
    ON dbo.TBLClosePos (OrderDate, BranchID, UserID)
    INCLUDE
    (
        ID, NoteID, NoteSerial, NoteSerial1, OpenBalance, LastBalance,
        TotalRechargeValue, TotalRev, TotalVat, CashOutTotal, TotalSupply,
        BoxBalance, Net, TotalSaleDay2Vat, TotalRevPOS, NetPOS, CountCards,
        TotalSaleDay2, CountTransaction, CashOut, CashOutDisc,
        BankBalanceCharge, TotalRev2, TotalRevvat, IsClosed
    );
END;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'IX_POS_Transactions_Report_Returns')
   AND COL_LENGTH(N'dbo.Transactions', N'Transaction_Type') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'Transaction_Date') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'BranchId') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'UserID') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'Transaction_NetValue') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'RechargeValue') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transactions_Report_Returns
    ON dbo.Transactions (Transaction_Type, Transaction_Date, BranchId, UserID)
    INCLUDE (Transaction_NetValue, RechargeValue)
    WHERE Transaction_Type = 9;
END;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'IX_POS_Transactions_Report_ServiceSearch')
   AND COL_LENGTH(N'dbo.Transactions', N'Transaction_Type') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'Transaction_Date') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'BranchId') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'ItemIDService') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'ItemIDService2') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'ItemIDService3') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transactions_Report_ServiceSearch
    ON dbo.Transactions (BranchId, Transaction_Date)
    INCLUDE (ItemIDService, ItemIDService2, ItemIDService3)
    WHERE Transaction_Type = 21;
END;
GO

IF OBJECT_ID(N'dbo.TBLClosePos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TBLClosePos') AND name = N'IX_POS_TBLClosePos_Report_Branch_Order_User')
   AND COL_LENGTH(N'dbo.TBLClosePos', N'BranchID') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'OrderDate') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'UserID') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'ID') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'Net') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalSaleDay2Vat') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalRevPOS') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'NetPOS') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'CountCards') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalSaleDay2') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'CountTransaction') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'CashOutTotal') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'CashOut') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'CashOutDisc') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'BankBalanceCharge') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalRechargeValue') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalRev2') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'TotalRevvat') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'BoxBalance') IS NOT NULL
   AND COL_LENGTH(N'dbo.TBLClosePos', N'IsClosed') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TBLClosePos_Report_Branch_Order_User
    ON dbo.TBLClosePos (BranchID, OrderDate, UserID)
    INCLUDE
    (
        ID, Net, TotalSaleDay2Vat, TotalRevPOS, NetPOS, CountCards,
        TotalSaleDay2, CountTransaction, CashOutTotal, CashOut, CashOutDisc,
        BankBalanceCharge, TotalRechargeValue, TotalRev2, TotalRevvat,
        BoxBalance, IsClosed
    );
END;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'IX_POS_Transactions_Report_Returns_Branch')
   AND COL_LENGTH(N'dbo.Transactions', N'BranchId') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'Transaction_Date') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'UserID') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'Transaction_Type') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'Transaction_NetValue') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'RechargeValue') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transactions_Report_Returns_Branch
    ON dbo.Transactions (BranchId, Transaction_Date, UserID)
    INCLUDE (Transaction_NetValue, RechargeValue)
    WHERE Transaction_Type = 9;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_Report_RunClosing', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Report_RunClosing;
GO

CREATE PROCEDURE dbo.usp_POS_Report_RunClosing
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
    @filterUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = CONVERT(DATE, @fromDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @toDate));
    DECLARE @serviceTerm NVARCHAR(100) = NULLIF(LTRIM(RTRIM(ISNULL(@serviceSearch, N''))), N'');

    IF @toExclusive < @from
    BEGIN
        DECLARE @swap DATETIME = @from;
        SET @from = DATEADD(DAY, -1, @toExclusive);
        SET @toExclusive = DATEADD(DAY, 1, @swap);
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
          AND (@branchId > 0 OR @branchFromId IS NULL OR c.BranchID >= @branchFromId)
          AND (@branchId > 0 OR @branchToId IS NULL OR c.BranchID <= @branchToId)
          AND (@canChangeDefaults = 1 OR c.UserID = @userId)
          AND (@filterUserId IS NULL OR c.UserID = @filterUserId)
        ORDER BY c.OrderDate DESC, c.ID DESC
        OPTION (RECOMPILE);

        RETURN;
    END;

    IF @reportKey = N'finance-closing-discounts'
    BEGIN
        -- The shared BranchScope path keeps better plans on Cash for single-branch discount summaries.
        IF 0 = 1 AND @branchId > 0 AND @serviceTerm IS NULL AND @showEmptyBranches = 0
        BEGIN
            ;WITH BranchInfo AS
            (
                SELECT
                    b.branch_id AS BranchID,
                    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), b.branch_id)) AS BranchName
                FROM dbo.TblBranchesData b
                WHERE b.branch_id = @branchId
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
                INNER JOIN BranchInfo b ON b.BranchID = c.BranchID
                WHERE c.OrderDate >= @from
                  AND c.OrderDate < @toExclusive
                  AND (@canChangeDefaults = 1 OR c.UserID = @userId)
                  AND (@filterUserId IS NULL OR c.UserID = @filterUserId)
            ),
            ReturnsByBranch AS
            (
                SELECT
                    t.BranchId AS BranchID,
                    COUNT(1) AS ReturnsCount,
                    SUM(ISNULL(t.Transaction_NetValue, 0) + ISNULL(t.RechargeValue, 0)) AS TotalReturns
                FROM dbo.Transactions t
                WHERE t.Transaction_Type = 9
                  AND t.BranchId = @branchId
                  AND t.Transaction_Date >= @from
                  AND t.Transaction_Date < @toExclusive
                  AND (@canChangeDefaults = 1 OR t.UserID = @userId)
                  AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
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
                ROW_NUMBER() OVER (ORDER BY r.BranchName, r.BranchID) AS RowNo,
                r.BranchName,
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
                    WHEN r.MinClosed = 1 AND r.MaxClosed = 1 THEN N'مغلق'
                    WHEN r.MaxClosed = 1 THEN N'إغلاق جزئي'
                    ELSE N'غير مغلق'
                END AS ClosingStatus
            FROM BranchRollup r
            WHERE
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
            ORDER BY r.BranchName, r.BranchID;

            RETURN;
        END;

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
              AND (@filterUserId IS NULL OR c.UserID = @filterUserId)
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
              AND (@canChangeDefaults = 1 OR t.UserID = @userId)
              AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
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
                WHEN r.BranchID IS NULL THEN N''
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
        ORDER BY s.BranchName, s.BranchID
        OPTION (RECOMPILE);

        RETURN;
    END;

    SELECT
        CAST(NULL AS NVARCHAR(200)) AS BranchName,
        CAST(NULL AS DATETIME) AS ClosingDate
    WHERE 1 = 0;
END;
GO
