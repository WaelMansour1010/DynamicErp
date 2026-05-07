/*
    POS Financial Intelligence Reports
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
             WHEN @ReceivablePrefix IS NULL AND d.NEmpid IS NOT NULL AND (a.Account_Name LIKE N'%ذمم%' OR a.Account_Name LIKE N'%موظف%' OR a.Account_Name LIKE N'%employee%') THEN 1
             ELSE 0 END AS IsReceivable,
        CASE WHEN @CustodyPrefix IS NOT NULL AND d.Account_Code LIKE @CustodyPrefix + N'%' THEN 1
             WHEN @CustodyPrefix IS NULL AND d.NEmpid IS NOT NULL AND (a.Account_Name LIKE N'%عهد%' OR a.Account_Name LIKE N'%سلف%' OR a.Account_Name LIKE N'%custody%' OR a.Account_Name LIKE N'%advance%') THEN 1
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
        AlertTitle = N'قيد يحتاج مراجعة',
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
        AccountType = CASE WHEN MAX(l.IsCustody) = 1 THEN N'Custody/Advance' ELSE N'Employee receivable' END,
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
            OR (@ReceivablePrefix IS NULL AND d.NEmpid IS NOT NULL AND (a.Account_Name LIKE N'%ذمم%' OR a.Account_Name LIKE N'%موظف%' OR a.Account_Name LIKE N'%employee%'))
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
        MovementDirection = CASE WHEN m.Credit_Or_Debit = 0 THEN N'Debit' ELSE N'Credit' END,
        m.Description,
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), m.UserID)),
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id)),
        SourceType = CASE WHEN m.Transaction_ID IS NULL THEN N'Manual/No operational source' ELSE N'Operational transaction' END
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
            OR (@CustodyPrefix IS NULL AND d.NEmpid IS NOT NULL AND (a.Account_Name LIKE N'%عهد%' OR a.Account_Name LIKE N'%سلف%' OR a.Account_Name LIKE N'%custody%' OR a.Account_Name LIKE N'%advance%'))
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
        MovementDirection = CASE WHEN m.Credit_Or_Debit = 0 THEN N'Funding' ELSE N'Settlement' END,
        m.Description,
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), m.UserID)),
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id)),
        SourceType = CASE WHEN m.Transaction_ID IS NULL THEN N'Manual/No operational source' ELSE N'Operational transaction' END
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
            SensitiveLine = MAX(CASE WHEN m.Account_Name LIKE N'%صندوق%' OR m.Account_Name LIKE N'%خز%' OR m.Account_Name LIKE N'%عهد%' OR m.Account_Name LIKE N'%سلف%' OR m.Account_Name LIKE N'%ذمم%' THEN 1 ELSE 0 END),
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
        MovementDirection = CASE WHEN m.Credit_Or_Debit = 0 THEN N'Debit' ELSE N'Credit' END,
        m.Description,
        UserName = COALESCE(NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), m.UserID)),
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), m.branch_id)),
        SourceType = CASE WHEN m.Transaction_ID IS NULL THEN N'Manual/No operational source' ELSE N'Operational transaction' END,
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
        ExplanationText = N'The abnormal change appears to start on '
            + ISNULL(CONVERT(NVARCHAR(10), (SELECT MIN(MovementDate) FROM Scored WHERE ABS(NetMovement) > NULLIF(Baseline, 0) * 3), 120), N'no clear date in this range')
            + N' due to high-impact ledger movements on this account. Review the largest journal entries, users, branches, and source type below.'
    FROM #M;
END;
GO
