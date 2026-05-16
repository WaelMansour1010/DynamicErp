/*
    POS performance stored procedures.
    SQL Server 2012 compatible. This file belongs to DynamicErp POS only.
    Do not mirror these changes into the old SatriahMain AllScripts.sql.
*/

IF OBJECT_ID(N'dbo.usp_POS_KycBank_Export', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_KycBank_Export;
GO

CREATE PROCEDURE dbo.usp_POS_KycBank_Export
    @cardLength INT,
    @fromDate DATETIME,
    @toDate DATETIME,
    @branchId INT = NULL,
    @canChangeDefaults BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = CONVERT(DATE, @fromDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @toDate));

    -- Matches VB6 FrmCustCash.btnExport(0/1) -> ExcelBank(18/8) bank template columns.
    ;WITH KycCustomers AS
    (
        SELECT
            c.*,
            KycNationalId = LTRIM(RTRIM(ISNULL(c.Tet_NumPoket, N''))),
            KycCardNo = LTRIM(RTRIM(ISNULL(c.CardNo, N''))),
            KycCardId = LTRIM(RTRIM(ISNULL(c.CardId, N'')))
        FROM dbo.TblCusCsh c
        INNER JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
        LEFT JOIN dbo.TblUsers u ON c.UserID = u.UserID
        LEFT JOIN dbo.TblEmployee e ON u.Empid = e.Emp_ID
        WHERE ISNULL(c.EasyCashType, 0) = 0
    )
    SELECT
        Token = LTRIM(RTRIM(ISNULL(c.CardId, N''))),
        EmbossingName = LEFT(UPPER(LTRIM(RTRIM(ISNULL(c.EnglishName0, N'') + N' ' + ISNULL(c.EnglishName1, N'') + N' ' + ISNULL(c.EnglishName3, N'')))), 25),
        ExtensionName = LEFT(UPPER(LTRIM(RTRIM(ISNULL(c.EnglishName0, N'') + N' ' + ISNULL(c.EnglishName1, N'') + N' ' + ISNULL(c.EnglishName3, N'')))), 25),
        MagstripeName = LEFT(UPPER(LTRIM(RTRIM(ISNULL(c.EnglishName0, N'') + N' ' + ISNULL(c.EnglishName1, N'') + N' ' + ISNULL(c.EnglishName3, N'')))), 25),
        NationalId = LTRIM(RTRIM(ISNULL(c.Tet_NumPoket, N''))),
        Address1 = LEFT(ISNULL(c.EnglishName5, N''), 35),
        Address2 = LEFT(ISNULL(c.EnglishName6, N''), 35),
        Address3 = LEFT(ISNULL(c.EnglishName7, N''), 35),
        SmsFlag = N'1',
        MobileNumber = N'002' + LTRIM(RTRIM(ISNULL(c.PhoneNo2, N''))),
        BirthDate = CASE WHEN c.BirthDate IS NULL THEN N'' ELSE CONVERT(NVARCHAR(10), c.BirthDate, 103) END,
        FullEnglishName = UPPER(LTRIM(RTRIM(ISNULL(c.EnglishName0, N'') + N' ' + ISNULL(c.EnglishName1, N'') + N' ' + ISNULL(c.EnglishName2, N'') + N' ' + ISNULL(c.EnglishName3, N'')))),
        FullArabicName = UPPER(LTRIM(RTRIM(ISNULL(c.ArabicName0, N'') + N' ' + ISNULL(c.ArabicName1, N'') + N' ' + ISNULL(c.ArabicName2, N'') + N' ' + ISNULL(c.ArabicName3, N'')))),
        OperationBranchCode = LTRIM(RTRIM(ISNULL(ob.branch_Code, N''))),
        OperationBranchName = COALESCE(NULLIF(ob.branch_name, N''), NULLIF(ob.branch_namee, N''), CONVERT(NVARCHAR(50), op.BranchId))
    FROM KycCustomers c
    CROSS APPLY
    (
        SELECT TOP 1 t2.BranchId
        FROM dbo.Transactions t2
        WHERE NULLIF(LTRIM(RTRIM(ISNULL(t2.VisaNumber, N''))), N'') IS NOT NULL
          AND LEN(LTRIM(RTRIM(ISNULL(t2.VisaNumber, N'')))) = @cardLength
          AND t2.Transaction_Date >= @from
          AND t2.Transaction_Date < @toExclusive
          -- VB6 checks CardNo. POS KYC import stores the same bank token in CardNo/CardId, so accept either.
          AND LTRIM(RTRIM(ISNULL(t2.VisaNumber, N''))) IN (c.KycCardNo, c.KycCardId)
        ORDER BY t2.Transaction_Date DESC, t2.Transaction_ID DESC
    ) op
    LEFT JOIN dbo.TblBranchesData ob ON ob.branch_id = op.BranchId
    WHERE 1 = 1
      AND EXISTS
      (
          SELECT 1
          FROM dbo.Transactions t1
          WHERE NULLIF(LTRIM(RTRIM(ISNULL(t1.VisaNumber, N''))), N'') IS NOT NULL
            AND LEN(LTRIM(RTRIM(ISNULL(t1.VisaNumber, N'')))) = @cardLength
            AND t1.Transaction_Date >= @from
            AND t1.Transaction_Date < @toExclusive
            AND LTRIM(RTRIM(ISNULL(CONVERT(NVARCHAR(50), CONVERT(DECIMAL(38, 0), t1.Tet_NumPoket)), N''))) = c.KycNationalId
      )
    ORDER BY c.Id DESC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_Accounting_Report', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Accounting_Report;
GO

CREATE PROCEDURE dbo.usp_POS_Accounting_Report
    @reportKey NVARCHAR(80),
    @fromDate DATETIME,
    @toDate DATETIME,
    @branchId INT = 0,
    @accountFrom NVARCHAR(50) = NULL,
    @accountTo NVARCHAR(50) = NULL,
    @accountCodes NVARCHAR(MAX) = NULL,
    @costCenterId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = CONVERT(DATE, @fromDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @toDate));
    SET @reportKey = LOWER(LTRIM(RTRIM(ISNULL(@reportKey, N''))));

    IF @reportKey = N'trial-balance'
    BEGIN
        ;WITH Movements AS
        (
            SELECT
                d.Account_Code,
                SUM(CASE WHEN d.RecordDate < @from THEN CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE -d.Value END ELSE 0 END) AS BeforeBalance,
                SUM(CASE WHEN d.RecordDate >= @from AND d.RecordDate < @toExclusive AND d.Credit_Or_Debit = 0 THEN d.Value ELSE 0 END) AS Debit,
                SUM(CASE WHEN d.RecordDate >= @from AND d.RecordDate < @toExclusive AND d.Credit_Or_Debit = 1 THEN d.Value ELSE 0 END) AS Credit
            FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
            WHERE (@branchId = 0 OR d.branch_id = @branchId)
              AND (@accountCodes IS NULL OR @accountCodes = N'' OR CHARINDEX(N',' + d.Account_Code + N',', N',' + @accountCodes + N',') > 0)
              AND (@accountFrom IS NULL OR d.Account_Code >= @accountFrom)
              AND (@accountTo IS NULL OR d.Account_Code <= @accountTo)
              AND (@costCenterId IS NULL OR d.project_id = @costCenterId)
            GROUP BY d.Account_Code
        )
        SELECT
            a.Account_Serial AS AccountSerial,
            a.Account_Code AS AccountCode,
            a.Account_Name AS AccountName,
            ISNULL(a.opening_balance, 0) + ISNULL(m.BeforeBalance, 0) AS OpeningBalance,
            ISNULL(m.Debit, 0) AS Debit,
            ISNULL(m.Credit, 0) AS Credit,
            ISNULL(a.opening_balance, 0) + ISNULL(m.BeforeBalance, 0) + ISNULL(m.Debit, 0) - ISNULL(m.Credit, 0) AS ClosingBalance
        FROM dbo.ACCOUNTS a WITH (NOLOCK)
        LEFT JOIN Movements m ON m.Account_Code = a.Account_Code
        WHERE ISNULL(a.last_account, 0) = 1
          AND (@accountCodes IS NULL OR @accountCodes = N'' OR CHARINDEX(N',' + a.Account_Code + N',', N',' + @accountCodes + N',') > 0)
          AND (@accountFrom IS NULL OR a.Account_Code >= @accountFrom)
          AND (@accountTo IS NULL OR a.Account_Code <= @accountTo)
          AND (ISNULL(a.opening_balance, 0) <> 0 OR ISNULL(m.BeforeBalance, 0) <> 0 OR ISNULL(m.Debit, 0) <> 0 OR ISNULL(m.Credit, 0) <> 0)
        ORDER BY a.Account_Serial, a.Account_Code;
        RETURN;
    END;

    IF @reportKey = N'income-statement'
    BEGIN
        SELECT
            CASE WHEN a.AccountTab = 2 THEN N'الإيرادات' WHEN a.AccountTab = 3 THEN N'المصروفات' ELSE N'أخرى' END AS SectionName,
            a.Account_Serial AS AccountSerial,
            a.Account_Code AS AccountCode,
            a.Account_Name AS AccountName,
            SUM(CASE WHEN d.Credit_Or_Debit = 1 THEN d.Value ELSE 0 END) AS Credit,
            SUM(CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE 0 END) AS Debit,
            CASE
                WHEN a.AccountTab = 2 THEN SUM(CASE WHEN d.Credit_Or_Debit = 1 THEN d.Value ELSE -d.Value END)
                WHEN a.AccountTab = 3 THEN SUM(CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE -d.Value END)
                ELSE SUM(CASE WHEN d.Credit_Or_Debit = 1 THEN d.Value ELSE -d.Value END)
            END AS NetAmount
        FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
        INNER JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
        WHERE d.RecordDate >= @from
          AND d.RecordDate < @toExclusive
          AND (@branchId = 0 OR d.branch_id = @branchId)
          AND (@accountCodes IS NULL OR @accountCodes = N'' OR CHARINDEX(N',' + d.Account_Code + N',', N',' + @accountCodes + N',') > 0)
          AND (@accountFrom IS NULL OR d.Account_Code >= @accountFrom)
          AND (@accountTo IS NULL OR d.Account_Code <= @accountTo)
          AND (@costCenterId IS NULL OR d.project_id = @costCenterId)
          AND a.AccountTab IN (2, 3)
        GROUP BY a.AccountTab, a.Account_Serial, a.Account_Code, a.Account_Name
        HAVING SUM(d.Value) <> 0
        ORDER BY a.AccountTab, a.Account_Serial, a.Account_Code;
        RETURN;
    END;

    ;WITH Lines AS
    (
        SELECT
            a.Account_Serial AS AccountSerial,
            d.Account_Code AS AccountCode,
            a.Account_Name AS AccountName,
            d.RecordDate,
            CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
            CONVERT(NVARCHAR(50), CAST(n.NoteSerial1 AS DECIMAL(38,0))) AS NoteSerial1,
            b.branch_name AS BranchName,
            d.Double_Entry_Vouchers_Description AS Description,
            CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE 0 END AS Debit,
            CASE WHEN d.Credit_Or_Debit = 1 THEN d.Value ELSE 0 END AS Credit
        FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
        LEFT JOIN dbo.Notes n WITH (NOLOCK) ON n.NoteID = d.Notes_ID
        LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
        LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = d.branch_id
        WHERE d.RecordDate >= @from
          AND d.RecordDate < @toExclusive
          AND (@branchId = 0 OR d.branch_id = @branchId)
          AND (@accountCodes IS NULL OR @accountCodes = N'' OR CHARINDEX(N',' + d.Account_Code + N',', N',' + @accountCodes + N',') > 0)
          AND (@accountFrom IS NULL OR d.Account_Code >= @accountFrom)
          AND (@accountTo IS NULL OR d.Account_Code <= @accountTo)
          AND (@costCenterId IS NULL OR d.project_id = @costCenterId)
    )
    SELECT
        RecordDate,
        NoteSerial,
        NoteSerial1,
        BranchName,
        AccountSerial,
        AccountCode,
        AccountName,
        Description,
        Debit,
        Credit,
        SUM(Debit - Credit) OVER (PARTITION BY AccountCode ORDER BY RecordDate, NoteSerial ROWS UNBOUNDED PRECEDING) AS RunningBalance
    FROM Lines
    ORDER BY AccountSerial, AccountCode, RecordDate, NoteSerial;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_Journal_Search', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Journal_Search;
GO

CREATE PROCEDURE dbo.usp_POS_Journal_Search
    @fromDate DATETIME = NULL,
    @toDate DATETIME = NULL,
    @branchId INT = NULL,
    @voucherNo NVARCHAR(100) = N'',
    @description NVARCHAR(200) = N'',
    @accountCode NVARCHAR(50) = N'',
    @accountCodes NVARCHAR(MAX) = NULL,
    @userId INT,
    @canChangeDefaults BIT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (200)
        n.NoteID,
        CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
        CONVERT(NVARCHAR(50), CAST(n.NoteSerial1 AS DECIMAL(38,0))) AS NoteSerial1,
        n.NoteDate,
        n.branch_no AS BranchId,
        COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), n.branch_no)) AS BranchName,
        ISNULL(n.Remark, N'') AS Description,
        CASE WHEN ISNULL(n.NoteType, 0) = 57 THEN 1 ELSE 0 END AS IsManual,
        SUM(CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE 0 END) AS DebitTotal,
        SUM(CASE WHEN d.Credit_Or_Debit = 1 THEN d.Value ELSE 0 END) AS CreditTotal
    FROM dbo.Notes n WITH (NOLOCK)
    INNER JOIN dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK) ON d.Notes_ID = n.NoteID
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = n.branch_no
    WHERE (@fromDate IS NULL OR n.NoteDate >= @fromDate)
      AND (@toDate IS NULL OR n.NoteDate < DATEADD(DAY, 1, @toDate))
      AND (@branchId IS NULL OR n.branch_no = @branchId)
      AND (@voucherNo = N'' OR CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) LIKE N'%' + @voucherNo + N'%' OR CONVERT(NVARCHAR(50), CAST(n.NoteSerial1 AS DECIMAL(38,0))) LIKE N'%' + @voucherNo + N'%')
      AND (@description = N'' OR ISNULL(n.Remark, N'') LIKE N'%' + @description + N'%' OR d.Double_Entry_Vouchers_Description LIKE N'%' + @description + N'%')
      AND (@accountCode = N'' OR d.Account_Code = @accountCode)
      AND (@accountCodes IS NULL OR @accountCodes = N'' OR CHARINDEX(N',' + d.Account_Code + N',', N',' + @accountCodes + N',') > 0)
      AND (@canChangeDefaults = 1 OR n.UserID = @userId OR d.UserID = @userId)
    GROUP BY n.NoteID, n.NoteSerial, n.NoteSerial1, n.NoteDate, n.branch_no, b.branch_name, b.branch_namee, n.Remark, n.NoteType
    ORDER BY n.NoteDate DESC, n.NoteID DESC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_Dashboard_SmartInsights', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Dashboard_SmartInsights;
GO

CREATE PROCEDURE dbo.usp_POS_Dashboard_SmartInsights
    @fromDate DATETIME,
    @toDate DATETIME,
    @previousFromDate DATETIME,
    @previousToDate DATETIME,
    @branchId INT = NULL,
    @operationType NVARCHAR(30) = N''
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = CONVERT(DATE, @fromDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @toDate));
    DECLARE @previousFrom DATETIME = CONVERT(DATE, @previousFromDate);
    DECLARE @previousToExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @previousToDate));
    SET @operationType = LTRIM(RTRIM(ISNULL(@operationType, N'')));

    ;WITH CurrentBranch AS
    (
        SELECT t.BranchId AS EntityId, COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)) AS EntityName,
               COUNT(1) AS CurrentCount, SUM(ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))) AS CurrentValue, SUM(ISNULL(t.NetValue, 0)) AS CurrentFees
        FROM dbo.Transactions t INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
        WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @from AND t.Transaction_Date < @toExclusive AND ISNULL(b.IsStoped, 0) = 0
          AND (@branchId IS NULL OR t.BranchId = @branchId)
          AND (@operationType = N'' OR CASE WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations' WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card' WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out' ELSE N'cash-in' END = @operationType)
        GROUP BY t.BranchId, COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId))
    ),
    PreviousBranch AS
    (
        SELECT t.BranchId AS EntityId, COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)) AS EntityName,
               COUNT(1) AS PreviousCount, SUM(ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))) AS PreviousValue, SUM(ISNULL(t.NetValue, 0)) AS PreviousFees
        FROM dbo.Transactions t INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
        WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @previousFrom AND t.Transaction_Date < @previousToExclusive AND ISNULL(b.IsStoped, 0) = 0
          AND (@branchId IS NULL OR t.BranchId = @branchId)
          AND (@operationType = N'' OR CASE WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations' WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card' WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out' ELSE N'cash-in' END = @operationType)
        GROUP BY t.BranchId, COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId))
    )
    SELECT COALESCE(c.EntityId, p.EntityId) AS EntityId, COALESCE(c.EntityName, p.EntityName) AS EntityName,
           ISNULL(c.CurrentCount, 0) AS CurrentCount, ISNULL(c.CurrentValue, 0) AS CurrentValue, ISNULL(c.CurrentFees, 0) AS CurrentFees,
           ISNULL(p.PreviousCount, 0) AS PreviousCount, ISNULL(p.PreviousValue, 0) AS PreviousValue, ISNULL(p.PreviousFees, 0) AS PreviousFees
    FROM CurrentBranch c FULL OUTER JOIN PreviousBranch p ON p.EntityId = c.EntityId
    WHERE ISNULL(c.CurrentCount, 0) > 0 OR ISNULL(p.PreviousCount, 0) > 0;

    ;WITH CurrentService AS
    (
        SELECT d.Item_ID AS EntityId, COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID)) AS EntityName,
               COUNT(1) AS CurrentCount, SUM(ISNULL(d.TotalPrice, ISNULL(d.Price, 0) + ISNULL(d.Vat, 0))) AS CurrentValue, SUM(ISNULL(d.showPrice, ISNULL(d.Price, 0))) AS CurrentFees
        FROM dbo.Transaction_Details d INNER JOIN dbo.Transactions t ON t.Transaction_ID = d.Transaction_ID INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
        WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @from AND t.Transaction_Date < @toExclusive AND ISNULL(b.IsStoped, 0) = 0
          AND (@branchId IS NULL OR t.BranchId = @branchId)
          AND (@operationType = N'' OR CASE WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations' WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card' WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out' ELSE N'cash-in' END = @operationType)
        GROUP BY d.Item_ID, COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID))
    ),
    PreviousService AS
    (
        SELECT d.Item_ID AS EntityId, COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID)) AS EntityName,
               COUNT(1) AS PreviousCount, SUM(ISNULL(d.TotalPrice, ISNULL(d.Price, 0) + ISNULL(d.Vat, 0))) AS PreviousValue, SUM(ISNULL(d.showPrice, ISNULL(d.Price, 0))) AS PreviousFees
        FROM dbo.Transaction_Details d INNER JOIN dbo.Transactions t ON t.Transaction_ID = d.Transaction_ID INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
        WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @previousFrom AND t.Transaction_Date < @previousToExclusive AND ISNULL(b.IsStoped, 0) = 0
          AND (@branchId IS NULL OR t.BranchId = @branchId)
          AND (@operationType = N'' OR CASE WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations' WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card' WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out' ELSE N'cash-in' END = @operationType)
        GROUP BY d.Item_ID, COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), CONVERT(NVARCHAR(50), d.Item_ID))
    )
    SELECT COALESCE(c.EntityId, p.EntityId) AS EntityId, COALESCE(c.EntityName, p.EntityName) AS EntityName,
           ISNULL(c.CurrentCount, 0) AS CurrentCount, ISNULL(c.CurrentValue, 0) AS CurrentValue, ISNULL(c.CurrentFees, 0) AS CurrentFees,
           ISNULL(p.PreviousCount, 0) AS PreviousCount, ISNULL(p.PreviousValue, 0) AS PreviousValue, ISNULL(p.PreviousFees, 0) AS PreviousFees
    FROM CurrentService c FULL OUTER JOIN PreviousService p ON p.EntityId = c.EntityId
    WHERE ISNULL(c.CurrentCount, 0) > 0 OR ISNULL(p.PreviousCount, 0) > 0;

    ;WITH CurrentSeller AS
    (
        SELECT t.UserID AS EntityId, COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), t.UserID)) AS EntityName,
               COUNT(1) AS CurrentCount, SUM(ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))) AS CurrentValue, SUM(ISNULL(t.NetValue, 0)) AS CurrentFees
        FROM dbo.Transactions t INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId INNER JOIN dbo.TblUsers u ON u.UserID = t.UserID LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
        WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @from AND t.Transaction_Date < @toExclusive AND t.UserID IS NOT NULL
          AND ISNULL(b.IsStoped, 0) = 0 AND ISNULL(u.isDeactivated, 0) = 0
          AND (@branchId IS NULL OR t.BranchId = @branchId)
          AND (@operationType = N'' OR CASE WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations' WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card' WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out' ELSE N'cash-in' END = @operationType)
        GROUP BY t.UserID, COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), t.UserID))
    ),
    PreviousSeller AS
    (
        SELECT t.UserID AS EntityId, COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), t.UserID)) AS EntityName,
               COUNT(1) AS PreviousCount, SUM(ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))) AS PreviousValue, SUM(ISNULL(t.NetValue, 0)) AS PreviousFees
        FROM dbo.Transactions t INNER JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId INNER JOIN dbo.TblUsers u ON u.UserID = t.UserID LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
        WHERE t.Transaction_Type = 21 AND t.Transaction_Date >= @previousFrom AND t.Transaction_Date < @previousToExclusive AND t.UserID IS NOT NULL
          AND ISNULL(b.IsStoped, 0) = 0 AND ISNULL(u.isDeactivated, 0) = 0
          AND (@branchId IS NULL OR t.BranchId = @branchId)
          AND (@operationType = N'' OR CASE WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations' WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card' WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out' ELSE N'cash-in' END = @operationType)
        GROUP BY t.UserID, COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), CONVERT(NVARCHAR(50), t.UserID))
    )
    SELECT COALESCE(c.EntityId, p.EntityId) AS EntityId, COALESCE(c.EntityName, p.EntityName) AS EntityName,
           ISNULL(c.CurrentCount, 0) AS CurrentCount, ISNULL(c.CurrentValue, 0) AS CurrentValue, ISNULL(c.CurrentFees, 0) AS CurrentFees,
           ISNULL(p.PreviousCount, 0) AS PreviousCount, ISNULL(p.PreviousValue, 0) AS PreviousValue, ISNULL(p.PreviousFees, 0) AS PreviousFees
    FROM CurrentSeller c FULL OUTER JOIN PreviousSeller p ON p.EntityId = c.EntityId
    WHERE ISNULL(c.CurrentCount, 0) > 0 OR ISNULL(p.PreviousCount, 0) > 0;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_SystemHealth_Database', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SystemHealth_Database;
GO

IF OBJECT_ID(N'dbo.usp_POS_Payments_Search', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Payments_Search;
GO

CREATE PROCEDURE dbo.usp_POS_Payments_Search
    @searchText NVARCHAR(100) = N'',
    @fromDate DATETIME = NULL,
    @toDate DATETIME = NULL,
    @branchId INT = NULL,
    @empId INT = NULL,
    @userId INT,
    @contextBranchId INT,
    @canChangeDefaults BIT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (100)
        n.NoteID,
        CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
        CONVERT(NVARCHAR(50), CAST(n.NoteSerial1 AS DECIMAL(38,0))) AS NoteSerial1,
        n.NoteDate,
        ISNULL(n.branch_no, 0) AS BranchId,
        COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), n.branch_no)) AS BranchName,
        ISNULL(n.CashingType, 0) AS CashingType,
        CASE WHEN ISNULL(n.CashingType, 0) = 5 THEN N'استعاضة عهدة' WHEN ISNULL(n.CashingType, 0) = 6 THEN N'تمويل خزينة' ELSE CONVERT(NVARCHAR(20), n.CashingType) END AS CashingTypeName,
        ISNULL(n.BTCashAccountcode, N'') AS NameAccountCode,
        ISNULL(n.person, N'') AS NameText,
        ISNULL(n.NoteCashingType, 0) AS PaymentMethod,
        CAST(ISNULL(n.Note_Value, 0) AS DECIMAL(18,2)) AS Value,
        n.Emp_ID AS EmpId,
        n.UserID AS CreatedUserId,
        u.UserName AS CreatedUserName,
        n.LastModifiedByUserId,
        mu.UserName AS LastModifiedByUserName,
        n.LastModifiedDate
    FROM dbo.Notes n
    LEFT JOIN dbo.TblBranchesData b ON b.branch_id = n.branch_no
    LEFT JOIN dbo.TblUsers u ON u.UserID = n.UserID
    LEFT JOIN dbo.TblUsers mu ON mu.UserID = n.LastModifiedByUserId
    WHERE n.NoteType = 50
      AND (@fromDate IS NULL OR n.NoteDate >= @fromDate)
      AND (@toDate IS NULL OR n.NoteDate < DATEADD(DAY, 1, @toDate))
      AND (@branchId IS NULL OR n.branch_no = @branchId)
      AND (@empId IS NULL OR n.Emp_ID = @empId)
      AND (@canChangeDefaults = 1 OR n.UserID = @userId OR n.branch_no = @contextBranchId)
      AND
      (
          @searchText = N''
          OR CONVERT(NVARCHAR(50), n.NoteID) = @searchText
          OR CONVERT(NVARCHAR(50), CAST(n.NoteSerial1 AS DECIMAL(38,0))) LIKE N'%' + @searchText + N'%'
          OR CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) LIKE N'%' + @searchText + N'%'
          OR ISNULL(n.person, N'') LIKE N'%' + @searchText + N'%'
          OR ISNULL(n.Remark, N'') LIKE N'%' + @searchText + N'%'
          OR ISNULL(n.ChqueNum, N'') LIKE N'%' + @searchText + N'%'
          OR ISNULL(n.BTCashAccountcode, N'') LIKE N'%' + @searchText + N'%'
      )
    ORDER BY n.NoteDate DESC, n.NoteID DESC;
END;
GO

CREATE PROCEDURE dbo.usp_POS_SystemHealth_Database
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @hasViewServerState BIT;
    DECLARE @statusMessage NVARCHAR(400);
    SET @hasViewServerState = CASE WHEN HAS_PERMS_BY_NAME(NULL, NULL, 'VIEW SERVER STATE') = 1 THEN 1 ELSE 0 END;
    SET @statusMessage = N'';

    IF @hasViewServerState = 1
    BEGIN
        SELECT TOP (5)
            r.session_id,
            r.total_elapsed_time,
            r.command,
            ISNULL(OBJECT_NAME(st.objectid, st.dbid), N'') AS ProcedureName,
            ISNULL(r.wait_type, N'') AS WaitType
        FROM sys.dm_exec_requests r
        CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) st
        WHERE r.session_id <> @@SPID
          AND (r.database_id = DB_ID() OR r.database_id = 0)
        ORDER BY r.total_elapsed_time DESC;

        SELECT TOP (10)
            r.session_id,
            r.blocking_session_id,
            ISNULL(r.wait_type, N'') AS WaitType,
            r.wait_time,
            r.total_elapsed_time
        FROM sys.dm_exec_requests r
        WHERE r.blocking_session_id <> 0
        ORDER BY r.wait_time DESC;

        SELECT ISNULL((
            SELECT TOP (1) cntr_value
            FROM sys.dm_os_performance_counters
            WHERE counter_name = N'Number of Deadlocks/sec'
              AND (instance_name = N'_Total' OR instance_name = DB_NAME())
            ORDER BY CASE WHEN instance_name = N'_Total' THEN 0 ELSE 1 END
        ), 0) AS DeadlockCounter;
    END
    ELSE
    BEGIN
        SET @statusMessage = N'لا توجد صلاحية كافية لقراءة مؤشرات الخادم. يتطلب هذا الجزء صلاحية VIEW SERVER STATE.';

        SELECT TOP (0)
            CAST(0 AS INT) AS session_id,
            CAST(0 AS INT) AS total_elapsed_time,
            CAST(N'' AS NVARCHAR(60)) AS command,
            CAST(N'' AS NVARCHAR(256)) AS ProcedureName,
            CAST(N'' AS NVARCHAR(120)) AS WaitType;

        SELECT TOP (0)
            CAST(0 AS INT) AS session_id,
            CAST(0 AS INT) AS blocking_session_id,
            CAST(N'' AS NVARCHAR(120)) AS WaitType,
            CAST(0 AS INT) AS wait_time,
            CAST(0 AS INT) AS total_elapsed_time;

        SELECT CAST(0 AS BIGINT) AS DeadlockCounter;
    END

    SELECT COUNT(1) AS TransactionsPerMinute
    FROM dbo.Transactions
    WHERE Transaction_Type = 21
      AND Transaction_Date >= DATEADD(MINUTE, -1, GETDATE());

    SELECT @statusMessage AS StatusMessage;
END;
GO

/*
    Index validation note - 2026-05-04:
    The candidate reporting indexes were tested on local Cash with a 120-worker mixed load
    (40 saves, 40 reports, 40 dashboard workers for 10 minutes).
    Result: invoice save latency increased noticeably after adding the indexes.
    Therefore this script intentionally creates stored procedures only.
    Do not add reporting indexes to production until a separate read-heavy benchmark proves
    the benefit is greater than the write overhead.
*/

