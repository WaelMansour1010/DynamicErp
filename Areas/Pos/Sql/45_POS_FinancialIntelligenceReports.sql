/*
    POS ط§ظ„ظ…ط¤ط´ط±ط§طھ ط§ظ„ظ…ط§ظ„ظٹط© ط§ظ„ط°ظƒظٹط©
    SQL Server 2012 compatible. Read-only diagnostic stored procedures.

    Account mapping:
    - Prefer passing @ReceivableParentSerial / @CustodyParentSerial from configuration.
    - When mappings are not provided, procedures use conservative account-name heuristics plus NEmpid.
    - No posting/accounting logic is changed.

    Performance:
    - Every procedure requires a date scope and materializes the scoped ledger into temp tables.
    - No indexes are created in this script. Existing ledger indexes should cover RecordDate, branch_id, Account_Code.
*/

IF OBJECT_ID(N'dbo.usp_POS_FI_AccountingHealthDashboard', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_AccountingHealthDashboard;
GO

CREATE PROCEDURE dbo.usp_POS_FI_AccountingHealthDashboard
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL,
    @UserId INT = NULL,
    @ReceivableParentSerial NVARCHAR(50) = NULL,
    @CustodyParentSerial NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    DECLARE @ReceivablePrefix NVARCHAR(50);
    DECLARE @CustodyPrefix NVARCHAR(50);

    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);

    SELECT @ReceivablePrefix = Account_Code FROM dbo.ACCOUNTS WITH (NOLOCK) WHERE @ReceivableParentSerial IS NOT NULL AND CONVERT(NVARCHAR(50), Account_Serial) = @ReceivableParentSerial;
    SELECT @CustodyPrefix = Account_Code FROM dbo.ACCOUNTS WITH (NOLOCK) WHERE @CustodyParentSerial IS NOT NULL AND CONVERT(NVARCHAR(50), Account_Serial) = @CustodyParentSerial;

    SELECT
        d.Double_Entry_Vouchers_ID,
        d.DEV_ID_Line_No,
        d.Account_Code,
        a.Account_Name,
        d.Value,
        d.Credit_Or_Debit,
        CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE -d.Value END AS SignedAmount,
        d.RecordDate,
        d.Notes_ID,
        d.Transaction_ID,
        d.UserID,
        d.branch_id,
        d.NEmpid,
        ISNULL(d.Double_Entry_Vouchers_Description, N'') AS Description,
        CASE WHEN @ReceivablePrefix IS NOT NULL AND d.Account_Code LIKE @ReceivablePrefix + N'%' THEN 1
             WHEN @ReceivablePrefix IS NULL AND d.NEmpid IS NOT NULL AND (a.Account_Name LIKE N'%ط°ظ…ظ…%' OR a.Account_Name LIKE N'%ظ…ظˆط¸ظپ%' OR a.Account_Name LIKE N'%employee%') THEN 1
             ELSE 0 END AS IsReceivable,
        CASE WHEN @CustodyPrefix IS NOT NULL AND d.Account_Code LIKE @CustodyPrefix + N'%' THEN 1
             WHEN @CustodyPrefix IS NULL AND d.NEmpid IS NOT NULL AND (a.Account_Name LIKE N'%ط¹ظ‡ط¯%' OR a.Account_Name LIKE N'%ط³ظ„ظپ%' OR a.Account_Name LIKE N'%custody%' OR a.Account_Name LIKE N'%advance%') THEN 1
             ELSE 0 END AS IsCustody
    INTO #Ledger
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
    WHERE d.RecordDate >= @From
      AND d.RecordDate < @ToExclusive
      AND (@BranchId IS NULL OR d.branch_id = @BranchId)
      AND (@UserId IS NULL OR d.UserID = @UserId);

    SELECT
        JournalCount = COUNT(DISTINCT Double_Entry_Vouchers_ID),
        LedgerLineCount = COUNT(1),
        MovementAmount = ISNULL(SUM(Value), 0),
        NetMovement = ISNULL(SUM(SignedAmount), 0),
        AbnormalJournalCount = ISNULL(SUM(CASE WHEN ABS(Value) >= 100000 OR Value = ROUND(Value, -3) THEN 1 ELSE 0 END), 0),
        NegativeSensitiveAccountCount = (
            SELECT COUNT(1)
            FROM (
                SELECT Account_Code
                FROM #Ledger
                WHERE IsReceivable = 1 OR IsCustody = 1
                GROUP BY Account_Code
                HAVING SUM(SignedAmount) < 0
            ) x
        ),
        ManualLikeJournalCount = ISNULL(SUM(CASE WHEN Transaction_ID IS NULL THEN 1 ELSE 0 END), 0),
        UserCount = COUNT(DISTINCT UserID),
        BranchCount = COUNT(DISTINCT branch_id)
    FROM #Ledger;

    ;WITH JournalAgg AS
    (
        SELECT
            Double_Entry_Vouchers_ID,
            MAX(RecordDate) AS JournalDate,
            MAX(UserID) AS UserID,
            MAX(branch_id) AS BranchId,
            SUM(CASE WHEN Credit_Or_Debit = 0 THEN Value ELSE 0 END) AS Debit,
            SUM(CASE WHEN Credit_Or_Debit = 1 THEN Value ELSE 0 END) AS Credit,
            MAX(CASE WHEN Transaction_ID IS NULL THEN 1 ELSE 0 END) AS WithoutOperationalSource,
            MAX(CASE WHEN IsReceivable = 1 OR IsCustody = 1 THEN 1 ELSE 0 END) AS SensitiveAccount,
            MAX(Value) AS MaxLineValue
        FROM #Ledger
        GROUP BY Double_Entry_Vouchers_ID
    )
    SELECT TOP (25)
        RiskScore =
            CASE WHEN ABS(Debit - Credit) > 0.01 THEN 35 ELSE 0 END
          + CASE WHEN MaxLineValue >= 100000 THEN 25 ELSE 0 END
          + CASE WHEN WithoutOperationalSource = 1 THEN 20 ELSE 0 END
          + CASE WHEN SensitiveAccount = 1 THEN 15 ELSE 0 END
          + CASE WHEN MaxLineValue = ROUND(MaxLineValue, -3) AND MaxLineValue >= 10000 THEN 5 ELSE 0 END,
        AlertTitle = N'ظ‚ظٹط¯ ظٹط­طھط§ط¬ ظ…ط±ط§ط¬ط¹ط©',
        Double_Entry_Vouchers_ID,
        JournalDate,
        Debit,
        Credit,
        Difference = Debit - Credit,
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), j.UserID)),
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), j.BranchId))
    FROM JournalAgg j
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = j.UserID
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = j.BranchId
    WHERE ABS(Debit - Credit) > 0.01
       OR MaxLineValue >= 100000
       OR WithoutOperationalSource = 1
       OR SensitiveAccount = 1
    ORDER BY RiskScore DESC, JournalDate DESC;

    SELECT TOP (25)
        AccountCode = l.Account_Code,
        AccountName = MAX(l.Account_Name),
        CurrentBalance = SUM(l.SignedAmount),
        MaxLineValue = MAX(l.Value),
        MovementCount = COUNT(1),
        FirstMovementDate = MIN(l.RecordDate),
        LastMovementDate = MAX(l.RecordDate),
        RiskScore =
            CASE WHEN SUM(l.SignedAmount) < 0 AND MAX(CASE WHEN l.IsCustody = 1 OR l.IsReceivable = 1 THEN 1 ELSE 0 END) = 1 THEN 35 ELSE 0 END
          + CASE WHEN MAX(l.Value) >= 100000 THEN 25 ELSE 0 END
          + CASE WHEN COUNT(DISTINCT l.UserID) > 3 THEN 15 ELSE 0 END
          + CASE WHEN SUM(CASE WHEN l.Transaction_ID IS NULL THEN 1 ELSE 0 END) > 0 THEN 15 ELSE 0 END
    FROM #Ledger l
    GROUP BY l.Account_Code
    HAVING ABS(SUM(l.SignedAmount)) > 0 OR MAX(l.Value) >= 100000
    ORDER BY RiskScore DESC, ABS(SUM(l.SignedAmount)) DESC;

    SELECT TOP (20)
        AccountCode = l.Account_Code,
        AccountName = MAX(l.Account_Name),
        Balance = SUM(l.SignedAmount),
        AccountType = CASE WHEN MAX(l.IsCustody) = 1 THEN N'ط¹ظ‡ط¯ ظˆط³ظ„ظپ' ELSE N'ط°ظ…ظ… ظ…ظˆط¸ظپظٹظ†' END,
        BranchCount = COUNT(DISTINCT l.branch_id),
        UserCount = COUNT(DISTINCT l.UserID),
        RiskScore = 70 + CASE WHEN ABS(SUM(l.SignedAmount)) >= 50000 THEN 20 ELSE 0 END
    FROM #Ledger l
    WHERE l.IsCustody = 1 OR l.IsReceivable = 1
    GROUP BY l.Account_Code
    HAVING SUM(l.SignedAmount) < 0
    ORDER BY ABS(SUM(l.SignedAmount)) DESC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FI_CashFlowAnalysis', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_CashFlowAnalysis;
GO

CREATE PROCEDURE dbo.usp_POS_FI_CashFlowAnalysis
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);

    SELECT
        d.Double_Entry_Vouchers_ID,
        d.Account_Code,
        a.Account_Name,
        d.Value,
        d.Credit_Or_Debit,
        CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE -d.Value END AS SignedAmount,
        d.RecordDate,
        d.Transaction_ID,
        d.UserID,
        d.branch_id
    INTO #M
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
    WHERE d.RecordDate >= @From
      AND d.RecordDate < @ToExclusive
      AND (@BranchId IS NULL OR d.branch_id = @BranchId)
      AND (a.Account_Name LIKE N'%ط®ط²%' OR a.Account_Name LIKE N'%طµظ†ط¯ظˆظ‚%' OR a.Account_Name LIKE N'%ط¨ظ†ظƒ%' OR a.Account_Name LIKE N'%Cash%' OR a.Account_Name LIKE N'%Bank%');

    SELECT
        MovementDate = CONVERT(DATE, RecordDate),
        CashIn = SUM(CASE WHEN SignedAmount > 0 THEN SignedAmount ELSE 0 END),
        CashOut = SUM(CASE WHEN SignedAmount < 0 THEN ABS(SignedAmount) ELSE 0 END),
        NetMovement = SUM(SignedAmount),
        MovementCount = COUNT(1),
        LargeMovementCount = SUM(CASE WHEN ABS(Value) >= 50000 THEN 1 ELSE 0 END)
    FROM #M
    GROUP BY CONVERT(DATE, RecordDate)
    ORDER BY MovementDate;

    SELECT TOP (30)
        BranchId = m.branch_id,
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id)),
        CashIn = SUM(CASE WHEN m.SignedAmount > 0 THEN m.SignedAmount ELSE 0 END),
        CashOut = SUM(CASE WHEN m.SignedAmount < 0 THEN ABS(m.SignedAmount) ELSE 0 END),
        NetMovement = SUM(m.SignedAmount),
        TreasuryPressureScore =
            CASE WHEN SUM(m.SignedAmount) < 0 THEN 35 ELSE 0 END
          + CASE WHEN SUM(CASE WHEN ABS(m.Value) >= 50000 THEN 1 ELSE 0 END) >= 3 THEN 25 ELSE 0 END
          + CASE WHEN STDEV(CONVERT(FLOAT, m.SignedAmount)) > NULLIF(AVG(ABS(CONVERT(FLOAT, m.SignedAmount))), 0) * 2 THEN 20 ELSE 0 END
    FROM #M m
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = m.branch_id
    GROUP BY m.branch_id, COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id))
    ORDER BY TreasuryPressureScore DESC, ABS(SUM(m.SignedAmount)) DESC;

    SELECT TOP (50)
        RiskScore = CASE WHEN ABS(m.Value) >= 100000 THEN 50 ELSE 0 END + CASE WHEN DATEPART(HOUR, m.RecordDate) NOT BETWEEN 7 AND 23 THEN 20 ELSE 0 END,
        AlertTitle = N'ط­ط±ظƒط© ط®ط²ظٹظ†ط© طھط­طھط§ط¬ ظ…ط±ط§ط¬ط¹ط©',
        m.Double_Entry_Vouchers_ID,
        m.RecordDate,
        m.Account_Code AS AccountCode,
        m.Account_Name AS AccountName,
        m.Value,
        MovementDirection = CASE WHEN m.Credit_Or_Debit = 0 THEN N'ظ…ط¯ظٹظ†' ELSE N'ط¯ط§ط¦ظ†' END,
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), m.UserID)),
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id))
    FROM #M m
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = m.UserID
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = m.branch_id
    WHERE ABS(m.Value) >= 50000 OR DATEPART(HOUR, m.RecordDate) NOT BETWEEN 7 AND 23
    ORDER BY RiskScore DESC, m.RecordDate DESC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FI_SalesCollectionsAnalysis', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_SalesCollectionsAnalysis;
GO

CREATE PROCEDURE dbo.usp_POS_FI_SalesCollectionsAnalysis
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);

    SELECT
        t.Transaction_ID,
        t.Transaction_Date,
        t.BranchId,
        t.UserID,
        t.Emp_ID,
        SalesAmount = ISNULL(t.Transaction_NetValue, ISNULL(t.NetValue, ISNULL(t.PayedValue, 0))),
        CollectionAmount = ISNULL(t.PayedValue, 0),
        BalanceAmount = ISNULL(t.RemainValue, 0),
        ServiceType = CASE WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
                           WHEN ISNULL(t.isRecharg, 0) = 1 OR NULLIF(t.VisaNumber, N'') IS NOT NULL THEN N'Card'
                           WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
                           ELSE N'Cash In' END
    INTO #Sales
    FROM dbo.Transactions t WITH (NOLOCK)
    WHERE t.Transaction_Type = 21
      AND t.Transaction_Date >= @From
      AND t.Transaction_Date < @ToExclusive
      AND (@BranchId IS NULL OR t.BranchId = @BranchId);

    SELECT
        BranchId = s.BranchId,
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), s.BranchId)),
        InvoiceCount = COUNT(1),
        SalesAmount = SUM(s.SalesAmount),
        CollectionAmount = SUM(s.CollectionAmount),
        BalanceAmount = SUM(s.BalanceAmount),
        CollectionEfficiency = CONVERT(DECIMAL(18, 2), SUM(s.CollectionAmount) * 100.0 / NULLIF(SUM(s.SalesAmount), 0)),
        AverageInvoiceValue = CONVERT(DECIMAL(18, 2), SUM(s.SalesAmount) / NULLIF(COUNT(1), 0))
    FROM #Sales s
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = s.BranchId
    GROUP BY s.BranchId, COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), s.BranchId))
    ORDER BY SalesAmount DESC;

    SELECT
        MovementDate = CONVERT(DATE, Transaction_Date),
        SalesAmount = SUM(SalesAmount),
        CollectionAmount = SUM(CollectionAmount),
        BalanceAmount = SUM(BalanceAmount),
        InvoiceCount = COUNT(1)
    FROM #Sales
    GROUP BY CONVERT(DATE, Transaction_Date)
    ORDER BY MovementDate;

    SELECT TOP (30)
        ServiceType,
        InvoiceCount = COUNT(1),
        SalesAmount = SUM(SalesAmount),
        CollectionAmount = SUM(CollectionAmount),
        CollectionEfficiency = CONVERT(DECIMAL(18, 2), SUM(CollectionAmount) * 100.0 / NULLIF(SUM(SalesAmount), 0))
    FROM #Sales
    GROUP BY ServiceType
    ORDER BY SalesAmount DESC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FI_ExpenseAnalysis', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_ExpenseAnalysis;
GO

CREATE PROCEDURE dbo.usp_POS_FI_ExpenseAnalysis
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);

    SELECT
        d.Double_Entry_Vouchers_ID,
        d.Account_Code,
        a.Account_Name,
        d.Value,
        d.RecordDate,
        d.Transaction_ID,
        d.UserID,
        d.branch_id,
        ISNULL(d.Double_Entry_Vouchers_Description, N'') AS Description
    INTO #E
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
    WHERE d.RecordDate >= @From
      AND d.RecordDate < @ToExclusive
      AND (@BranchId IS NULL OR d.branch_id = @BranchId)
      AND (a.Account_Name LIKE N'%ظ…طµط±ظˆظپ%' OR a.Account_Name LIKE N'%طھظƒظ„ظپط©%' OR a.Account_Name LIKE N'%Expense%' OR a.Account_Name LIKE N'%Cost%');

    SELECT TOP (50)
        AccountCode = Account_Code,
        AccountName = MAX(Account_Name),
        ExpenseAmount = SUM(ABS(Value)),
        MovementCount = COUNT(1),
        MaxExpense = MAX(ABS(Value)),
        ManualLikeCount = SUM(CASE WHEN Transaction_ID IS NULL THEN 1 ELSE 0 END),
        RiskScore = CASE WHEN MAX(ABS(Value)) >= 50000 THEN 35 ELSE 0 END + CASE WHEN SUM(CASE WHEN Transaction_ID IS NULL THEN 1 ELSE 0 END) > 0 THEN 25 ELSE 0 END
    FROM #E
    GROUP BY Account_Code
    ORDER BY ExpenseAmount DESC;

    SELECT
        MovementDate = CONVERT(DATE, RecordDate),
        ExpenseAmount = SUM(ABS(Value)),
        MovementCount = COUNT(1),
        LargeExpenseCount = SUM(CASE WHEN ABS(Value) >= 50000 THEN 1 ELSE 0 END)
    FROM #E
    GROUP BY CONVERT(DATE, RecordDate)
    ORDER BY MovementDate;

    SELECT TOP (50)
        RiskScore = CASE WHEN ABS(Value) >= 50000 THEN 40 ELSE 0 END + CASE WHEN Transaction_ID IS NULL THEN 20 ELSE 0 END,
        AlertTitle = N'ظ…طµط±ظˆظپ ظٹط­طھط§ط¬ ظ…ط±ط§ط¬ط¹ط©',
        Double_Entry_Vouchers_ID,
        RecordDate,
        AccountCode = Account_Code,
        AccountName = Account_Name,
        Value,
        Description
    FROM #E
    WHERE ABS(Value) >= 50000 OR Transaction_ID IS NULL
    ORDER BY RiskScore DESC, RecordDate DESC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FI_InventoryProfitability', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_InventoryProfitability;
GO

CREATE PROCEDURE dbo.usp_POS_FI_InventoryProfitability
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);

    SELECT
        td.Item_ID,
        i.ItemCode,
        ItemName = COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), td.Item_ID)),
        td.StoreID2,
        StoreName = COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), td.StoreID2)),
        t.BranchId,
        t.Transaction_ID,
        t.Transaction_Date,
        QuantityEffect = ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0),
        SalesValue = CASE WHEN t.Transaction_Type = 21 THEN ISNULL(td.Price, 0) * ISNULL(td.Quantity, 0) ELSE 0 END,
        CostValue = ISNULL(td.CostPrice, 0) * ISNULL(td.Quantity, 0),
        td.ItemSerial
    INTO #M
    FROM dbo.Transaction_Details td WITH (NOLOCK)
    INNER JOIN dbo.Transactions t WITH (NOLOCK) ON t.Transaction_ID = td.Transaction_ID
    LEFT JOIN dbo.TransactionTypes tt WITH (NOLOCK) ON tt.Transaction_Type = t.Transaction_Type
    LEFT JOIN dbo.TblItems i WITH (NOLOCK) ON i.ItemID = td.Item_ID
    LEFT JOIN dbo.TblStore s WITH (NOLOCK) ON s.StoreID = td.StoreID2
    WHERE t.Transaction_Date >= @From
      AND t.Transaction_Date < @ToExclusive
      AND (@BranchId IS NULL OR t.BranchId = @BranchId);

    SELECT TOP (100)
        Item_ID,
        ItemCode = MAX(ItemCode),
        ItemName = MAX(ItemName),
        StoreID = StoreID2,
        StoreName = MAX(StoreName),
        StockBalance = SUM(QuantityEffect),
        SalesValue = SUM(SalesValue),
        CostValue = SUM(CostValue),
        ProfitEstimate = SUM(SalesValue) - SUM(CostValue),
        SerialCount = COUNT(DISTINCT NULLIF(ItemSerial, N'')),
        RiskScore = CASE WHEN SUM(QuantityEffect) < 0 THEN 45 ELSE 0 END + CASE WHEN COUNT(DISTINCT NULLIF(ItemSerial, N'')) > 0 AND SUM(QuantityEffect) < 1 THEN 20 ELSE 0 END
    FROM #M
    GROUP BY Item_ID, StoreID2
    HAVING SUM(QuantityEffect) <> 0 OR SUM(SalesValue) <> 0
    ORDER BY RiskScore DESC, ProfitEstimate DESC;

    SELECT TOP (100)
        BranchId,
        Item_ID,
        ItemName = MAX(ItemName),
        Serial = ItemSerial,
        StoreID = MAX(StoreID2),
        Balance = SUM(QuantityEffect),
        MovementCount = COUNT(1),
        RiskScore = CASE WHEN SUM(QuantityEffect) < 0 THEN 70 WHEN COUNT(DISTINCT Transaction_ID) > 1 AND SUM(QuantityEffect) <> 1 THEN 35 ELSE 0 END
    FROM #M
    WHERE NULLIF(ItemSerial, N'') IS NOT NULL
    GROUP BY BranchId, Item_ID, ItemSerial
    HAVING SUM(QuantityEffect) < 0 OR COUNT(DISTINCT Transaction_ID) > 1
    ORDER BY RiskScore DESC, MovementCount DESC;

    SELECT
        MovementDate = CONVERT(DATE, Transaction_Date),
        SalesValue = SUM(SalesValue),
        CostValue = SUM(CostValue),
        ProfitEstimate = SUM(SalesValue) - SUM(CostValue),
        StockEffectQty = SUM(QuantityEffect)
    FROM #M
    GROUP BY CONVERT(DATE, Transaction_Date)
    ORDER BY MovementDate;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FI_JournalDetails', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_JournalDetails;
GO

CREATE PROCEDURE dbo.usp_POS_FI_JournalDetails
    @DoubleEntryVoucherId INT = NULL,
    @NotesId INT = NULL,
    @TransactionId INT = NULL,
    @AccountCode NVARCHAR(50) = NULL,
    @BranchId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (200)
        d.Double_Entry_Vouchers_ID,
        d.DEV_ID_Line_No,
        d.RecordDate,
        d.Account_Code AS AccountCode,
        a.Account_Name AS AccountName,
        d.Value,
        MovementDirection = CASE WHEN d.Credit_Or_Debit = 0 THEN N'ظ…ط¯ظٹظ†' ELSE N'ط¯ط§ط¦ظ†' END,
        d.Notes_ID,
        d.Transaction_ID,
        d.Double_Entry_Vouchers_Description AS Description,
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), d.UserID)),
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), d.branch_id))
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = d.UserID
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = d.branch_id
    WHERE (@DoubleEntryVoucherId IS NULL OR d.Double_Entry_Vouchers_ID = @DoubleEntryVoucherId)
      AND (@NotesId IS NULL OR d.Notes_ID = @NotesId)
      AND (@TransactionId IS NULL OR d.Transaction_ID = @TransactionId)
      AND (@AccountCode IS NULL OR @AccountCode = N'' OR d.Account_Code = @AccountCode)
      AND (@BranchId IS NULL OR d.branch_id = @BranchId)
    ORDER BY d.RecordDate DESC, d.DEV_ID_Line_No;
END;
GO


IF OBJECT_ID(N'dbo.usp_POS_FI_CfoDashboard', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_CfoDashboard;
GO

CREATE PROCEDURE dbo.usp_POS_FI_CfoDashboard
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL,
    @UserId INT = NULL,
    @ReceivableParentSerial NVARCHAR(50) = NULL,
    @CustodyParentSerial NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);

    SELECT
        d.Double_Entry_Vouchers_ID,
        d.Account_Code,
        a.Account_Name,
        d.Value,
        d.Credit_Or_Debit,
        CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE -d.Value END AS SignedAmount,
        d.RecordDate,
        d.Notes_ID,
        d.Transaction_ID,
        d.UserID,
        d.branch_id,
        ISNULL(d.Double_Entry_Vouchers_Description, N'') AS Description,
        IsRevenue = CASE WHEN a.Account_Name LIKE N'%ط¥ظٹط±ط§ط¯%' OR a.Account_Name LIKE N'%ظ…ط¨ظٹط¹ط§طھ%' OR a.Account_Name LIKE N'%Revenue%' OR a.Account_Name LIKE N'%Sales%' THEN 1 ELSE 0 END,
        IsExpense = CASE WHEN a.Account_Name LIKE N'%ظ…طµط±ظˆظپ%' OR a.Account_Name LIKE N'%طھظƒظ„ظپط©%' OR a.Account_Name LIKE N'%Expense%' OR a.Account_Name LIKE N'%Cost%' THEN 1 ELSE 0 END,
        IsCashBank = CASE WHEN a.Account_Name LIKE N'%ط®ط²%' OR a.Account_Name LIKE N'%طµظ†ط¯ظˆظ‚%' OR a.Account_Name LIKE N'%ط¨ظ†ظƒ%' OR a.Account_Name LIKE N'%Cash%' OR a.Account_Name LIKE N'%Bank%' THEN 1 ELSE 0 END
    INTO #Ledger
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
    WHERE d.RecordDate >= @From
      AND d.RecordDate < @ToExclusive
      AND (@BranchId IS NULL OR d.branch_id = @BranchId)
      AND (@UserId IS NULL OR d.UserID = @UserId);

    SELECT
        [طµط§ظپظٹ ط§ظ„ط­ط±ظƒط©] = ISNULL(SUM(SignedAmount), 0),
        [ظ…ط¨ظٹط¹ط§طھ ط§ظ„ظٹظˆظ…] = ISNULL((SELECT SUM(ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))) FROM dbo.Transactions t WITH (NOLOCK) WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @From AND t.Transaction_Date < @ToExclusive AND (@BranchId IS NULL OR t.BranchId = @BranchId)), 0),
        [ط§ظ„طھط­طµظٹظ„ط§طھ] = ISNULL((SELECT SUM(ISNULL(t.PayedValue, ISNULL(t.Transaction_NetValue, 0))) FROM dbo.Transactions t WITH (NOLOCK) WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @From AND t.Transaction_Date < @ToExclusive AND (@BranchId IS NULL OR t.BranchId = @BranchId)), 0),
        [ط§ظ„ظ…ط´طھط±ظٹط§طھ] = ISNULL((SELECT SUM(ISNULL(t.NetValue, ISNULL(t.Transaction_NetValue, 0))) FROM dbo.Transactions t WITH (NOLOCK) WHERE t.Transaction_Type IN (5, 25) AND t.Transaction_Date >= @From AND t.Transaction_Date < @ToExclusive AND (@BranchId IS NULL OR t.BranchId = @BranchId)), 0),
        [ط§ظ„ظ…طµط±ظˆظپط§طھ] = ISNULL(SUM(CASE WHEN IsExpense = 1 THEN ABS(SignedAmount) ELSE 0 END), 0),
        [ط­ط±ظƒط© ط§ظ„ط®ط²ظٹظ†ط© ظˆط§ظ„ط¨ظ†ظˆظƒ] = ISNULL(SUM(CASE WHEN IsCashBank = 1 THEN SignedAmount ELSE 0 END), 0),
        [ظ†ط³ط¨ط© ط§ظ„ظ‚ظٹظˆط¯ ط§ظ„ظٹط¯ظˆظٹط©] = CONVERT(DECIMAL(18, 2), CASE WHEN COUNT(1) = 0 THEN 0 ELSE SUM(CASE WHEN Transaction_ID IS NULL THEN 1 ELSE 0 END) * 100.0 / COUNT(1) END),
        [طھظ†ط¨ظٹظ‡ط§طھ طھط­طھط§ط¬ ظ…ط±ط§ط¬ط¹ط©] = ISNULL(SUM(CASE WHEN ABS(Value) >= 100000 OR Transaction_ID IS NULL THEN 1 ELSE 0 END), 0)
    FROM #Ledger;

    SELECT
        MovementDate = CONVERT(DATE, RecordDate),
        Inflows = SUM(CASE WHEN SignedAmount > 0 THEN SignedAmount ELSE 0 END),
        Outflows = SUM(CASE WHEN SignedAmount < 0 THEN ABS(SignedAmount) ELSE 0 END),
        NetMovement = SUM(SignedAmount)
    FROM #Ledger
    GROUP BY CONVERT(DATE, RecordDate)
    ORDER BY MovementDate;

    SELECT TOP (20)
        BranchId = l.branch_id,
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), l.branch_id)),
        Sales = ISNULL((SELECT SUM(ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))) FROM dbo.Transactions t WITH (NOLOCK) WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @From AND t.Transaction_Date < @ToExclusive AND t.BranchId = l.branch_id), 0),
        Collections = ISNULL((SELECT SUM(ISNULL(t.PayedValue, 0)) FROM dbo.Transactions t WITH (NOLOCK) WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @From AND t.Transaction_Date < @ToExclusive AND t.BranchId = l.branch_id), 0),
        Expenses = SUM(CASE WHEN l.IsExpense = 1 THEN ABS(l.SignedAmount) ELSE 0 END),
        NetMovement = SUM(l.SignedAmount),
        ProfitabilityIndicator = SUM(CASE WHEN l.IsRevenue = 1 THEN ABS(l.SignedAmount) ELSE 0 END) - SUM(CASE WHEN l.IsExpense = 1 THEN ABS(l.SignedAmount) ELSE 0 END),
        AlertCount = SUM(CASE WHEN ABS(l.Value) >= 100000 OR l.Transaction_ID IS NULL THEN 1 ELSE 0 END)
    FROM #Ledger l
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = l.branch_id
    GROUP BY l.branch_id, COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), l.branch_id))
    ORDER BY ProfitabilityIndicator DESC, NetMovement DESC;

    SELECT TOP (20)
        AccountCode = Account_Code,
        AccountName = MAX(Account_Name),
        ExpenseAmount = SUM(ABS(SignedAmount)),
        MovementCount = COUNT(1)
    FROM #Ledger
    WHERE IsExpense = 1
    GROUP BY Account_Code
    ORDER BY SUM(ABS(SignedAmount)) DESC;

    SELECT TOP (50)
        RiskScore = CASE WHEN ABS(Value) >= 100000 THEN 40 ELSE 0 END + CASE WHEN Transaction_ID IS NULL THEN 30 ELSE 0 END,
        AlertTitle = N'طھظ†ط¨ظٹظ‡ ظٹط­طھط§ط¬ ظ…ط±ط§ط¬ط¹ط©',
        Double_Entry_Vouchers_ID,
        Notes_ID,
        Transaction_ID,
        AccountCode = Account_Code,
        AccountName = Account_Name,
        RecordDate,
        Value,
        Description
    FROM #Ledger
    WHERE ABS(Value) >= 100000 OR Transaction_ID IS NULL
    ORDER BY RiskScore DESC, RecordDate DESC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FI_BranchPerformance', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_BranchPerformance;
GO

CREATE PROCEDURE dbo.usp_POS_FI_BranchPerformance
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);

    SELECT
        t.Transaction_ID,
        t.BranchId,
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)),
        SalesAmount = ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0)),
        CollectionAmount = ISNULL(t.PayedValue, 0),
        t.Transaction_Date
    INTO #Sales
    FROM dbo.Transactions t WITH (NOLOCK)
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = t.BranchId
    WHERE t.Transaction_Type = 21
      AND t.Transaction_Date >= @From
      AND t.Transaction_Date < @ToExclusive
      AND (@BranchId IS NULL OR t.BranchId = @BranchId);

    SELECT
        BranchId = d.branch_id,
        Expenses = SUM(ABS(CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE -d.Value END))
    INTO #Expenses
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
    WHERE d.RecordDate >= @From
      AND d.RecordDate < @ToExclusive
      AND (@BranchId IS NULL OR d.branch_id = @BranchId)
      AND (a.Account_Name LIKE N'%ظ…طµط±ظˆظپ%' OR a.Account_Name LIKE N'%طھظƒظ„ظپط©%' OR a.Account_Name LIKE N'%Expense%' OR a.Account_Name LIKE N'%Cost%')
    GROUP BY d.branch_id;

    SELECT
        BranchId = d.branch_id,
        AbnormalJournalCount = COUNT(DISTINCT d.Double_Entry_Vouchers_ID)
    INTO #Alerts
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    WHERE d.RecordDate >= @From
      AND d.RecordDate < @ToExclusive
      AND (@BranchId IS NULL OR d.branch_id = @BranchId)
      AND (ABS(d.Value) >= 100000 OR d.Transaction_ID IS NULL)
    GROUP BY d.branch_id;

    SELECT
        Ranking = ROW_NUMBER() OVER (ORDER BY SUM(s.SalesAmount) - ISNULL(MAX(e.Expenses), 0) DESC),
        s.BranchId,
        s.BranchName,
        BranchSales = SUM(s.SalesAmount),
        Collections = SUM(s.CollectionAmount),
        Expenses = ISNULL(MAX(e.Expenses), 0),
        NetMovement = SUM(s.CollectionAmount) - ISNULL(MAX(e.Expenses), 0),
        ProfitabilityIndicator = SUM(s.SalesAmount) - ISNULL(MAX(e.Expenses), 0),
        AverageInvoiceValue = CONVERT(DECIMAL(18, 2), SUM(s.SalesAmount) / NULLIF(COUNT(1), 0)),
        AbnormalJournalCount = ISNULL(MAX(a.AbnormalJournalCount), 0)
    FROM #Sales s
    LEFT JOIN #Expenses e ON e.BranchId = s.BranchId
    LEFT JOIN #Alerts a ON a.BranchId = s.BranchId
    GROUP BY s.BranchId, s.BranchName
    ORDER BY ProfitabilityIndicator DESC;

    SELECT TOP (5) * FROM (
        SELECT BranchId, BranchName, BranchSales = SUM(SalesAmount), Collections = SUM(CollectionAmount)
        FROM #Sales GROUP BY BranchId, BranchName
    ) x ORDER BY BranchSales DESC;

    SELECT TOP (5) * FROM (
        SELECT s.BranchId, s.BranchName, ProfitabilityIndicator = SUM(s.SalesAmount) - ISNULL(MAX(e.Expenses), 0), AbnormalJournalCount = ISNULL(MAX(a.AbnormalJournalCount), 0)
        FROM #Sales s
        LEFT JOIN #Expenses e ON e.BranchId = s.BranchId
        LEFT JOIN #Alerts a ON a.BranchId = s.BranchId
        GROUP BY s.BranchId, s.BranchName
    ) x ORDER BY ProfitabilityIndicator ASC, AbnormalJournalCount DESC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FI_EmployeeReceivableDiagnostics', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_EmployeeReceivableDiagnostics;
GO

CREATE PROCEDURE dbo.usp_POS_FI_EmployeeReceivableDiagnostics
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL,
    @EmployeeId INT = NULL,
    @AccountCode NVARCHAR(50) = NULL,
    @ReceivableParentSerial NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    DECLARE @ReceivablePrefix NVARCHAR(50);

    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);
    SELECT @ReceivablePrefix = Account_Code FROM dbo.ACCOUNTS WITH (NOLOCK) WHERE @ReceivableParentSerial IS NOT NULL AND CONVERT(NVARCHAR(50), Account_Serial) = @ReceivableParentSerial;

    SELECT
        d.Double_Entry_Vouchers_ID,
        d.DEV_ID_Line_No,
        d.Account_Code,
        a.Account_Name,
        d.Value,
        d.Credit_Or_Debit,
        CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE -d.Value END AS SignedAmount,
        d.RecordDate,
        d.Notes_ID,
        d.Transaction_ID,
        d.UserID,
        d.branch_id,
        d.NEmpid,
        ISNULL(d.Double_Entry_Vouchers_Description, N'') AS Description
    INTO #M
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
    WHERE d.RecordDate >= @From
      AND d.RecordDate < @ToExclusive
      AND (@BranchId IS NULL OR d.branch_id = @BranchId)
      AND (@EmployeeId IS NULL OR d.NEmpid = @EmployeeId)
      AND (@AccountCode IS NULL OR @AccountCode = N'' OR d.Account_Code = @AccountCode)
      AND (
            (@ReceivablePrefix IS NOT NULL AND d.Account_Code LIKE @ReceivablePrefix + N'%')
            OR (@ReceivablePrefix IS NULL AND d.NEmpid IS NOT NULL AND (a.Account_Name LIKE N'%ط°ظ…ظ…%' OR a.Account_Name LIKE N'%ظ…ظˆط¸ظپ%' OR a.Account_Name LIKE N'%employee%'))
          );

    ;WITH Daily AS
    (
        SELECT Account_Code, NEmpid, MovementDate = CONVERT(DATE, RecordDate), DailyMovement = SUM(SignedAmount), DailyMax = MAX(Value)
        FROM #M
        GROUP BY Account_Code, NEmpid, CONVERT(DATE, RecordDate)
    ),
    Running AS
    (
        SELECT
            Account_Code,
            NEmpid,
            MovementDate,
            DailyMovement,
            DailyMax,
            RunningBalance = SUM(DailyMovement) OVER (PARTITION BY Account_Code, NEmpid ORDER BY MovementDate ROWS UNBOUNDED PRECEDING),
            Baseline = AVG(ABS(DailyMovement)) OVER (PARTITION BY Account_Code, NEmpid)
        FROM Daily
    )
    SELECT TOP (100)
        AccountCode = r.Account_Code,
        AccountName = MAX(m.Account_Name),
        EmployeeId = r.NEmpid,
        EmployeeName = MAX(e.Emp_Name),
        CurrentBalance = MAX(CASE WHEN r.MovementDate = mx.LastDate THEN r.RunningBalance ELSE NULL END),
        HighestBalance = MAX(r.RunningBalance),
        FirstAbnormalDate = MIN(CASE WHEN ABS(r.DailyMovement) > NULLIF(r.Baseline, 0) * 3 OR ABS(r.RunningBalance) > NULLIF(r.Baseline, 0) * 5 THEN r.MovementDate ELSE NULL END),
        MovementCount = COUNT(1),
        ManualLikeCount = SUM(CASE WHEN m.Transaction_ID IS NULL THEN 1 ELSE 0 END),
        RiskScore = CASE WHEN MAX(r.RunningBalance) >= 50000 THEN 25 ELSE 0 END
                  + CASE WHEN MIN(CASE WHEN ABS(r.DailyMovement) > NULLIF(r.Baseline, 0) * 3 THEN 1 ELSE NULL END) = 1 THEN 25 ELSE 0 END
                  + CASE WHEN SUM(CASE WHEN m.Transaction_ID IS NULL THEN 1 ELSE 0 END) > 0 THEN 20 ELSE 0 END
                  + CASE WHEN MIN(r.RunningBalance) < 0 AND MAX(r.RunningBalance) > 0 THEN 15 ELSE 0 END
                  + CASE WHEN DATEDIFF(DAY, MIN(r.MovementDate), MAX(r.MovementDate)) >= 30 AND MAX(r.RunningBalance) <> 0 THEN 15 ELSE 0 END
    FROM Running r
    INNER JOIN (SELECT Account_Code, NEmpid, MAX(MovementDate) AS LastDate FROM Running GROUP BY Account_Code, NEmpid) mx
        ON mx.Account_Code = r.Account_Code AND ISNULL(mx.NEmpid, -1) = ISNULL(r.NEmpid, -1)
    INNER JOIN #M m ON m.Account_Code = r.Account_Code AND ISNULL(m.NEmpid, -1) = ISNULL(r.NEmpid, -1)
    LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = r.NEmpid
    GROUP BY r.Account_Code, r.NEmpid
    ORDER BY RiskScore DESC, ABS(MAX(r.RunningBalance)) DESC;

    SELECT TOP (50)
        m.Account_Code AS AccountCode,
        m.Double_Entry_Vouchers_ID,
        m.RecordDate,
        m.Value,
        MovementDirection = CASE WHEN m.Credit_Or_Debit = 0 THEN N'ظ…ط¯ظٹظ†' ELSE N'ط¯ط§ط¦ظ†' END,
        m.Description,
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), m.UserID)),
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id)),
        SourceType = CASE WHEN m.Transaction_ID IS NULL THEN N'ظ‚ظٹط¯ ظٹط¯ظˆظٹ ط£ظˆ ط¨ط¯ظˆظ† ظ…طµط¯ط± طھط´ط؛ظٹظ„ظٹ' ELSE N'ط­ط±ظƒط© طھط´ط؛ظٹظ„ظٹط© ظ…ط±طھط¨ط·ط©' END
    FROM #M m
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = m.UserID
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = m.branch_id
    ORDER BY m.Value DESC, m.RecordDate DESC;

    SELECT
        AccountCode = Account_Code,
        EmployeeId = NEmpid,
        MovementDate = CONVERT(DATE, RecordDate),
        Debit = SUM(CASE WHEN Credit_Or_Debit = 0 THEN Value ELSE 0 END),
        Credit = SUM(CASE WHEN Credit_Or_Debit = 1 THEN Value ELSE 0 END),
        NetMovement = SUM(SignedAmount)
    FROM #M
    GROUP BY Account_Code, NEmpid, CONVERT(DATE, RecordDate)
    ORDER BY MovementDate;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FI_CustodyDiagnostics', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_CustodyDiagnostics;
GO

CREATE PROCEDURE dbo.usp_POS_FI_CustodyDiagnostics
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL,
    @EmployeeId INT = NULL,
    @AccountCode NVARCHAR(50) = NULL,
    @CustodyParentSerial NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    DECLARE @CustodyPrefix NVARCHAR(50);

    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);
    SELECT @CustodyPrefix = Account_Code FROM dbo.ACCOUNTS WITH (NOLOCK) WHERE @CustodyParentSerial IS NOT NULL AND CONVERT(NVARCHAR(50), Account_Serial) = @CustodyParentSerial;

    SELECT
        d.Double_Entry_Vouchers_ID,
        d.DEV_ID_Line_No,
        d.Account_Code,
        a.Account_Name,
        d.Value,
        d.Credit_Or_Debit,
        CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE -d.Value END AS SignedAmount,
        d.RecordDate,
        d.Transaction_ID,
        d.UserID,
        d.branch_id,
        d.NEmpid,
        ISNULL(d.Double_Entry_Vouchers_Description, N'') AS Description
    INTO #M
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
    WHERE d.RecordDate >= @From
      AND d.RecordDate < @ToExclusive
      AND (@BranchId IS NULL OR d.branch_id = @BranchId)
      AND (@EmployeeId IS NULL OR d.NEmpid = @EmployeeId)
      AND (@AccountCode IS NULL OR @AccountCode = N'' OR d.Account_Code = @AccountCode)
      AND (
            (@CustodyPrefix IS NOT NULL AND d.Account_Code LIKE @CustodyPrefix + N'%')
            OR (@CustodyPrefix IS NULL AND d.NEmpid IS NOT NULL AND (a.Account_Name LIKE N'%ط¹ظ‡ط¯%' OR a.Account_Name LIKE N'%ط³ظ„ظپ%' OR a.Account_Name LIKE N'%custody%' OR a.Account_Name LIKE N'%advance%'))
          );

    SELECT TOP (100)
        AccountCode = m.Account_Code,
        AccountName = MAX(m.Account_Name),
        EmployeeId = m.NEmpid,
        EmployeeName = MAX(e.Emp_Name),
        CurrentBalance = SUM(m.SignedAmount),
        NegativeBalance = CASE WHEN SUM(m.SignedAmount) < 0 THEN 1 ELSE 0 END,
        AgingDays = DATEDIFF(DAY, MIN(m.RecordDate), MAX(m.RecordDate)),
        RepeatedSettlementCount = SUM(CASE WHEN m.Credit_Or_Debit = 1 AND m.Value = ROUND(m.Value, -2) THEN 1 ELSE 0 END),
        ManualLikeCount = SUM(CASE WHEN m.Transaction_ID IS NULL THEN 1 ELSE 0 END),
        RiskScore = CASE WHEN SUM(m.SignedAmount) < 0 THEN 35 ELSE 0 END
                  + CASE WHEN DATEDIFF(DAY, MIN(m.RecordDate), MAX(m.RecordDate)) >= 30 AND SUM(m.SignedAmount) <> 0 THEN 20 ELSE 0 END
                  + CASE WHEN SUM(CASE WHEN m.Credit_Or_Debit = 1 AND m.Value = ROUND(m.Value, -2) THEN 1 ELSE 0 END) >= 3 THEN 20 ELSE 0 END
                  + CASE WHEN SUM(CASE WHEN m.Transaction_ID IS NULL THEN 1 ELSE 0 END) > 0 THEN 15 ELSE 0 END
                  + CASE WHEN MAX(m.Value) >= 50000 THEN 10 ELSE 0 END
    FROM #M m
    LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = m.NEmpid
    GROUP BY m.Account_Code, m.NEmpid
    ORDER BY RiskScore DESC, ABS(SUM(m.SignedAmount)) DESC;

    SELECT TOP (50)
        m.Account_Code AS AccountCode,
        m.Double_Entry_Vouchers_ID,
        m.RecordDate,
        m.Value,
        MovementDirection = CASE WHEN m.Credit_Or_Debit = 0 THEN N'طھظ…ظˆظٹظ„' ELSE N'طھط³ظˆظٹط©' END,
        m.Description,
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), m.UserID)),
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id)),
        SourceType = CASE WHEN m.Transaction_ID IS NULL THEN N'ظ‚ظٹط¯ ظٹط¯ظˆظٹ ط£ظˆ ط¨ط¯ظˆظ† ظ…طµط¯ط± طھط´ط؛ظٹظ„ظٹ' ELSE N'ط­ط±ظƒط© طھط´ط؛ظٹظ„ظٹط© ظ…ط±طھط¨ط·ط©' END
    FROM #M m
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = m.UserID
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = m.branch_id
    ORDER BY m.Value DESC, m.RecordDate DESC;

    SELECT
        AccountCode = Account_Code,
        EmployeeId = NEmpid,
        MovementDate = CONVERT(DATE, RecordDate),
        Funding = SUM(CASE WHEN Credit_Or_Debit = 0 THEN Value ELSE 0 END),
        Settlement = SUM(CASE WHEN Credit_Or_Debit = 1 THEN Value ELSE 0 END),
        NetMovement = SUM(SignedAmount)
    FROM #M
    GROUP BY Account_Code, NEmpid, CONVERT(DATE, RecordDate)
    ORDER BY MovementDate;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FI_AbnormalJournalDetection', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_AbnormalJournalDetection;
GO

CREATE PROCEDURE dbo.usp_POS_FI_AbnormalJournalDetection
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL,
    @UserId INT = NULL,
    @ReceivableParentSerial NVARCHAR(50) = NULL,
    @CustodyParentSerial NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);

    SELECT
        d.Double_Entry_Vouchers_ID,
        d.Account_Code,
        a.Account_Name,
        d.Value,
        d.Credit_Or_Debit,
        d.RecordDate,
        d.Transaction_ID,
        d.UserID,
        d.branch_id,
        ISNULL(d.Double_Entry_Vouchers_Description, N'') AS Description
    INTO #M
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
    WHERE d.RecordDate >= @From
      AND d.RecordDate < @ToExclusive
      AND (@BranchId IS NULL OR d.branch_id = @BranchId)
      AND (@UserId IS NULL OR d.UserID = @UserId);

    ;WITH AmountStats AS
    (
        SELECT AvgValue = AVG(CONVERT(FLOAT, Value)), StdValue = STDEV(CONVERT(FLOAT, Value))
        FROM #M
    ),
    JournalAgg AS
    (
        SELECT
            m.Double_Entry_Vouchers_ID,
            JournalDate = MAX(m.RecordDate),
            Debit = SUM(CASE WHEN m.Credit_Or_Debit = 0 THEN m.Value ELSE 0 END),
            Credit = SUM(CASE WHEN m.Credit_Or_Debit = 1 THEN m.Value ELSE 0 END),
            MaxLineValue = MAX(m.Value),
            LineCount = COUNT(1),
            UserID = MAX(m.UserID),
            BranchId = MAX(m.branch_id),
            WithoutSource = MAX(CASE WHEN m.Transaction_ID IS NULL THEN 1 ELSE 0 END),
            RoundedLine = MAX(CASE WHEN m.Value >= 10000 AND m.Value = ROUND(m.Value, -3) THEN 1 ELSE 0 END),
            SensitiveLine = MAX(CASE WHEN m.Account_Name LIKE N'%طµظ†ط¯ظˆظ‚%' OR m.Account_Name LIKE N'%ط®ط²%' OR m.Account_Name LIKE N'%ط¹ظ‡ط¯%' OR m.Account_Name LIKE N'%ط³ظ„ظپ%' OR m.Account_Name LIKE N'%ط°ظ…ظ…%' THEN 1 ELSE 0 END),
            AccountsFingerprint = MIN(m.Account_Code) + N'|' + MAX(m.Account_Code)
        FROM #M m
        GROUP BY m.Double_Entry_Vouchers_ID
    ),
    Dup AS
    (
        SELECT AccountsFingerprint, Debit, Credit, DuplicateCount = COUNT(1)
        FROM JournalAgg
        GROUP BY AccountsFingerprint, Debit, Credit
        HAVING COUNT(1) > 1
    )
    SELECT TOP (200)
        RiskScore =
            CASE WHEN ABS(j.Debit - j.Credit) > 0.01 THEN 35 ELSE 0 END
          + CASE WHEN s.StdValue IS NOT NULL AND j.MaxLineValue > (s.AvgValue + (3 * s.StdValue)) THEN 25 ELSE 0 END
          + CASE WHEN d.DuplicateCount IS NOT NULL THEN 20 ELSE 0 END
          + CASE WHEN j.RoundedLine = 1 THEN 10 ELSE 0 END
          + CASE WHEN j.SensitiveLine = 1 AND j.WithoutSource = 1 THEN 20 ELSE 0 END
          + CASE WHEN DATEPART(HOUR, j.JournalDate) NOT BETWEEN 7 AND 23 THEN 10 ELSE 0 END,
        j.Double_Entry_Vouchers_ID,
        j.JournalDate,
        j.Debit,
        j.Credit,
        Difference = j.Debit - j.Credit,
        j.MaxLineValue,
        IsDuplicateLike = CASE WHEN d.DuplicateCount IS NULL THEN 0 ELSE 1 END,
        j.RoundedLine,
        j.SensitiveLine,
        j.WithoutSource,
        OutsideBusinessHours = CASE WHEN DATEPART(HOUR, j.JournalDate) NOT BETWEEN 7 AND 23 THEN 1 ELSE 0 END,
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), j.UserID)),
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), j.BranchId))
    FROM JournalAgg j
    CROSS JOIN AmountStats s
    LEFT JOIN Dup d ON d.AccountsFingerprint = j.AccountsFingerprint AND d.Debit = j.Debit AND d.Credit = j.Credit
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = j.UserID
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = j.BranchId
    WHERE ABS(j.Debit - j.Credit) > 0.01
       OR (s.StdValue IS NOT NULL AND j.MaxLineValue > (s.AvgValue + (3 * s.StdValue)))
       OR d.DuplicateCount IS NOT NULL
       OR j.RoundedLine = 1
       OR (j.SensitiveLine = 1 AND j.WithoutSource = 1)
       OR DATEPART(HOUR, j.JournalDate) NOT BETWEEN 7 AND 23
    ORDER BY RiskScore DESC, j.JournalDate DESC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_FI_RootCauseAnalyzer', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_FI_RootCauseAnalyzer;
GO

CREATE PROCEDURE dbo.usp_POS_FI_RootCauseAnalyzer
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL,
    @AccountCode NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, ISNULL(@ToDate, @From)));
    IF @ToExclusive <= @From SET @ToExclusive = DATEADD(DAY, 1, @From);

    SELECT
        d.Double_Entry_Vouchers_ID,
        d.Account_Code,
        a.Account_Name,
        d.Value,
        d.Credit_Or_Debit,
        CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE -d.Value END AS SignedAmount,
        d.RecordDate,
        d.Transaction_ID,
        d.UserID,
        d.branch_id,
        d.NEmpid,
        ISNULL(d.Double_Entry_Vouchers_Description, N'') AS Description
    INTO #M
    FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
    WHERE d.RecordDate >= @From
      AND d.RecordDate < @ToExclusive
      AND d.Account_Code = @AccountCode
      AND (@BranchId IS NULL OR d.branch_id = @BranchId);

    ;WITH Daily AS
    (
        SELECT MovementDate = CONVERT(DATE, RecordDate), Debit = SUM(CASE WHEN Credit_Or_Debit = 0 THEN Value ELSE 0 END), Credit = SUM(CASE WHEN Credit_Or_Debit = 1 THEN Value ELSE 0 END), NetMovement = SUM(SignedAmount)
        FROM #M
        GROUP BY CONVERT(DATE, RecordDate)
    ),
    Running AS
    (
        SELECT
            MovementDate,
            Debit,
            Credit,
            NetMovement,
            RunningBalance = SUM(NetMovement) OVER (ORDER BY MovementDate ROWS UNBOUNDED PRECEDING),
            Baseline = AVG(ABS(NetMovement)) OVER ()
        FROM Daily
    )
    SELECT
        MovementDate,
        Debit,
        Credit,
        NetMovement,
        RunningBalance,
        IsAnomaly = CASE WHEN ABS(NetMovement) > NULLIF(Baseline, 0) * 3 THEN 1 ELSE 0 END
    FROM Running
    ORDER BY MovementDate;

    SELECT TOP (50)
        m.Double_Entry_Vouchers_ID,
        m.RecordDate,
        m.Value,
        MovementDirection = CASE WHEN m.Credit_Or_Debit = 0 THEN N'ظ…ط¯ظٹظ†' ELSE N'ط¯ط§ط¦ظ†' END,
        m.Description,
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), m.UserID)),
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id)),
        SourceType = CASE WHEN m.Transaction_ID IS NULL THEN N'ظ‚ظٹط¯ ظٹط¯ظˆظٹ ط£ظˆ ط¨ط¯ظˆظ† ظ…طµط¯ط± طھط´ط؛ظٹظ„ظٹ' ELSE N'ط­ط±ظƒط© طھط´ط؛ظٹظ„ظٹط© ظ…ط±طھط¨ط·ط©' END,
        m.Transaction_ID
    FROM #M m
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = m.UserID
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = m.branch_id
    ORDER BY m.Value DESC, m.RecordDate DESC;

    SELECT TOP (20)
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), m.UserID)),
        MovementCount = COUNT(1),
        TotalMovement = SUM(m.Value),
        MaxMovement = MAX(m.Value)
    FROM #M m
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = m.UserID
    GROUP BY COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), m.UserID))
    ORDER BY TotalMovement DESC;

    SELECT TOP (20)
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id)),
        MovementCount = COUNT(1),
        TotalMovement = SUM(m.Value),
        MaxMovement = MAX(m.Value)
    FROM #M m
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = m.branch_id
    GROUP BY COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id))
    ORDER BY TotalMovement DESC;

    ;WITH Daily AS
    (
        SELECT MovementDate = CONVERT(DATE, RecordDate), NetMovement = SUM(SignedAmount)
        FROM #M
        GROUP BY CONVERT(DATE, RecordDate)
    ),
    Scored AS
    (
        SELECT MovementDate, NetMovement, Baseline = AVG(ABS(NetMovement)) OVER ()
        FROM Daily
    )
    SELECT TOP (1)
        AccountCode = @AccountCode,
        AccountName = MAX(Account_Name),
        AnomalyStartDate = (SELECT MIN(MovementDate) FROM Scored WHERE ABS(NetMovement) > NULLIF(Baseline, 0) * 3),
        ExplanationText = N'ظٹط¸ظ‡ط± ط£ظ† ط¨ط¯ط§ظٹط© ط§ظ„ط§ظ†ط­ط±ط§ظپ ظƒط§ظ†طھ ظپظٹ '
            + ISNULL(CONVERT(NVARCHAR(10), (SELECT MIN(MovementDate) FROM Scored WHERE ABS(NetMovement) > NULLIF(Baseline, 0) * 3), 120), N'ظ„ط§ ظٹظˆط¬ط¯ طھط§ط±ظٹط® ظˆط§ط¶ط­ ط¶ظ…ظ† ط§ظ„ظپطھط±ط©')
            + N' ظ†طھظٹط¬ط© ط­ط±ظƒط§طھ ظ…ط¤ط«ط±ط© ط¹ظ„ظ‰ ظ‡ط°ط§ ط§ظ„ط­ط³ط§ط¨. ط±ط§ط¬ط¹ ط£ظƒط¨ط± ط§ظ„ظ‚ظٹظˆط¯ ظˆط§ظ„ظ…ط³طھط®ط¯ظ…ظٹظ† ظˆط§ظ„ظپط±ظˆط¹ ظˆظ†ظˆط¹ ط§ظ„ظ…طµط¯ط± ظپظٹ ط§ظ„ط¬ط¯ط§ظˆظ„ ط§ظ„طھط§ظ„ظٹط©.'
    FROM #M;
END;
GO
