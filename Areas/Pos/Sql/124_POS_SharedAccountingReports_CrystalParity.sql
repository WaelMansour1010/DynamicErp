/*
POS SQL update number: 124
Module: POS
Purpose: Shared accounting report stored procedures required by POS AccountingReports screen.
Safe to rerun? Yes
Auto apply?: Yes
Dependencies: dbo.ACCOUNTS,dbo.DOUBLE_ENTREY_VOUCHERS,dbo.TblBranchesData
Date: 2026-05-20
Author/Agent: Codex
*/
IF OBJECT_ID(N'dbo.usp_Shared_AccountingReports_Branches', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_Shared_AccountingReports_Branches;
GO
CREATE PROCEDURE dbo.usp_Shared_AccountingReports_Branches
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        branch_id AS Id,
        COALESCE(NULLIF(branch_name, N''), NULLIF(branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), branch_id)) AS Name
    FROM dbo.TblBranchesData WITH (NOLOCK)
    WHERE branch_id IS NOT NULL
    ORDER BY branch_id;
END;
GO

IF OBJECT_ID(N'dbo.usp_Shared_AccountingReports_AccountTree', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_Shared_AccountingReports_AccountTree;
GO
CREATE PROCEDURE dbo.usp_Shared_AccountingReports_AccountTree
    @parentCode NVARCHAR(50) = NULL,
    @term NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @parentCode = NULLIF(LTRIM(RTRIM(ISNULL(@parentCode, N''))), N'');
    SET @term = NULLIF(LTRIM(RTRIM(ISNULL(@term, N''))), N'');

    SELECT TOP (80)
        a.Account_Code AS AccountCode,
        a.Account_Serial AS AccountSerial,
        a.Account_Name AS AccountName,
        a.Parent_Account_Code AS ParentAccountCode,
        CONVERT(BIT, ISNULL(a.last_account, 0)) AS IsLastAccount,
        CONVERT(BIT, CASE WHEN EXISTS (SELECT 1 FROM dbo.ACCOUNTS c WITH (NOLOCK) WHERE c.Parent_Account_Code = a.Account_Code AND c.Account_Code <> a.Account_Code) THEN 1 ELSE 0 END) AS HasChildren
    FROM dbo.ACCOUNTS a WITH (NOLOCK)
    WHERE NULLIF(LTRIM(RTRIM(ISNULL(a.Account_Code, N''))), N'') IS NOT NULL
      AND (
            (@term IS NOT NULL AND (a.Account_Code LIKE N'%' + @term + N'%' OR a.Account_Serial LIKE N'%' + @term + N'%' OR a.Account_Name LIKE N'%' + @term + N'%'))
            OR (@term IS NULL AND @parentCode IS NULL AND (a.Parent_Account_Code IS NULL OR a.Parent_Account_Code = N'' OR a.Account_Code = a.Parent_Account_Code))
            OR (@term IS NULL AND @parentCode IS NOT NULL AND a.Parent_Account_Code = @parentCode AND a.Account_Code <> @parentCode)
          )
    ORDER BY a.Account_Serial, a.Account_Code;
END;
GO

IF OBJECT_ID(N'dbo.usp_Shared_AccountingReports_Run', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_Shared_AccountingReports_Run;
GO
CREATE PROCEDURE dbo.usp_Shared_AccountingReports_Run
    @reportKey NVARCHAR(100),
    @fromDate DATETIME = NULL,
    @toDate DATETIME = NULL,
    @branchId INT = NULL,
    @accountFrom NVARCHAR(50) = NULL,
    @accountTo NVARCHAR(50) = NULL,
    @accountCodes NVARCHAR(MAX) = NULL,
    @costCenterId INT = NULL,
    @projectId INT = NULL,
    @activityId INT = NULL,
    @regionId INT = NULL,
    @noteType INT = NULL,
    @accountLevel INT = NULL,
    @hideZeroBalance BIT = 1,
    @detailed BIT = 0,
    @userId INT = 0,
    @canChangeDefaults BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = ISNULL(CONVERT(DATE, @fromDate), CONVERT(DATE, GETDATE()));
    DECLARE @to DATETIME = ISNULL(CONVERT(DATE, @toDate), CONVERT(DATE, GETDATE()));
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, @to);
    DECLARE @key NVARCHAR(100) = LOWER(LTRIM(RTRIM(ISNULL(@reportKey, N''))));
    DECLARE @codes NVARCHAR(MAX) = NULLIF(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(@accountCodes, N''))), N'''', N''), N' ', N''), N'');

    IF @key = N'trial-balance' SET @key = N'opt-35-trial-balance-2';
    IF @key = N'income-statement' SET @key = N'opt-3-income-statement-com';
    IF @key = N'account-statement' SET @key = N'opt-0-account-statement';
    IF @key = N'general-ledger-assistant' SET @key = N'opt-1-general-ledger-for';

    CREATE TABLE #SelectedAccounts
    (
        Account_Code NVARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY
    );

    INSERT INTO #SelectedAccounts (Account_Code)
    SELECT a.Account_Code
    FROM dbo.ACCOUNTS a WITH (NOLOCK)
    WHERE (@codes IS NULL OR CHARINDEX(N',' + a.Account_Code + N',', N',' + @codes + N',') > 0)
      AND (@accountFrom IS NULL OR a.Account_Serial >= @accountFrom OR a.Account_Code >= @accountFrom)
      AND (@accountTo IS NULL OR a.Account_Serial <= @accountTo OR a.Account_Code <= @accountTo);

    CREATE TABLE #MovementBase
    (
        Double_Entry_Vouchers_ID INT NULL,
        DEV_ID_Line_No INT NULL,
        Account_Code NVARCHAR(50) COLLATE DATABASE_DEFAULT NULL,
        RecordDate DATETIME NULL,
        NoteSerial DECIMAL(38, 0) NULL,
        NoteSerial1 DECIMAL(38, 0) NULL,
        NoteType INT NULL,
        branch_id INT NULL,
        Description NVARCHAR(4000) COLLATE DATABASE_DEFAULT NULL,
        Debit DECIMAL(38, 6) NULL,
        Credit DECIMAL(38, 6) NULL,
        project_id INT NULL,
        pandid INT NULL,
        operid INT NULL,
        UserName NVARCHAR(255) COLLATE DATABASE_DEFAULT NULL
    );

    INSERT INTO #MovementBase
    (
        Double_Entry_Vouchers_ID,
        DEV_ID_Line_No,
        Account_Code,
        RecordDate,
        NoteSerial,
        NoteSerial1,
        NoteType,
        branch_id,
        Description,
        Debit,
        Credit,
        project_id,
        pandid,
        operid,
        UserName
    )
    SELECT
        v.Double_Entry_Vouchers_ID,
        v.DEV_ID_Line_No,
        v.Account_Code,
        COALESCE(n.NoteDate, v.RecordDate) AS RecordDate,
        n.NoteSerial,
        n.NoteSerial1,
        n.NoteType,
        v.branch_id,
        COALESCE(v.Double_Entry_Vouchers_Description, v.des, n.Remark) AS Description,
        CASE WHEN ISNULL(v.Credit_Or_Debit, 0) = 0 THEN ISNULL(v.Value, 0) ELSE 0 END AS Debit,
        CASE WHEN ISNULL(v.Credit_Or_Debit, 0) = 1 THEN ISNULL(v.Value, 0) ELSE 0 END AS Credit,
        COALESCE(v.project_id, v.projectid),
        v.pandid,
        v.operid,
        u.UserName
    FROM dbo.DOUBLE_ENTREY_VOUCHERS v WITH (NOLOCK)
    LEFT JOIN dbo.Notes n WITH (NOLOCK) ON n.NoteID = v.Notes_ID
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = v.UserID
    WHERE COALESCE(n.NoteDate, v.RecordDate) < @toExclusive
      AND (@branchId IS NULL OR @branchId = 0 OR v.branch_id = @branchId)
      AND (@projectId IS NULL OR @projectId = 0 OR v.project_id = @projectId OR v.projectid = @projectId)
      AND (@noteType IS NULL OR @noteType = 0 OR n.NoteType = @noteType)
      AND (@canChangeDefaults = 1 OR @userId = 0 OR v.UserID = @userId OR n.UserID = @userId)
      AND EXISTS (SELECT 1 FROM #SelectedAccounts sa WHERE sa.Account_Code = v.Account_Code);
    IF @key IN (N'opt-35-trial-balance-2', N'opt-39-trial-balance-levels-2', N'opt-38-trial-balance-for-account-2', N'opt-12-general-ledger-by-trans', N'opt-20-project-account')
    BEGIN
        SELECT
            CASE @key
                WHEN N'opt-39-trial-balance-levels-2' THEN N'TrialBalanceNew1.rpt'
                WHEN N'opt-12-general-ledger-by-trans' THEN N'TrialBalanceNew.rpt (ShowTrialBalanceNew2)'
                WHEN N'opt-20-project-account' THEN N'construction\TrialBalanceNew.rpt'
                ELSE N'TrialBalanceNew.rpt'
            END AS ReportSource,
            a.Account_Serial AS AccountSerial,
            a.Account_Code AS AccountCode,
            a.Account_Name AS AccountName,
            a.Account_NameEng AS AccountNameEng,
            a.Parent_Account_Code AS ParentAccountCode,
            ISNULL(a.[Level], 0) AS AccountLevel,
            ISNULL(a.opening_balance, 0) AS OpeningBalance,
            ISNULL(a.DepitBalance, 0) AS DebitBalance,
            ISNULL(a.CreditBalance, 0) AS CreditBalance,
            ISNULL(a.DepitBalance, 0) AS Debit,
            ISNULL(a.CreditBalance, 0) AS Credit,
            ISNULL(a.Balance, 0) AS ClosingBalance
        FROM dbo.ACCOUNTS a WITH (NOLOCK)
        WHERE EXISTS (SELECT 1 FROM #SelectedAccounts sa WHERE sa.Account_Code = a.Account_Code)
          AND (@key <> N'opt-35-trial-balance-2' OR ISNULL(a.last_account, 0) = 1)
          AND (@key <> N'opt-39-trial-balance-levels-2' OR ISNULL(a.last_account, 0) = 0 OR ISNULL(@accountLevel, 0) > 0)
          AND (@key <> N'opt-38-trial-balance-for-account-2' OR @codes IS NULL OR CHARINDEX(N',' + a.Account_Code + N',', N',' + @codes + N',') > 0 OR a.Parent_Account_Code IN (SELECT Account_Code FROM #SelectedAccounts))
          AND (@key <> N'opt-20-project-account' OR @codes IS NULL OR CHARINDEX(N',' + a.Account_Code + N',', N',' + @codes + N',') > 0)
          AND (ISNULL(@accountLevel, 0) = 0 OR ISNULL(a.[Level], 0) <= @accountLevel OR (LEN(a.Account_Code) - LEN(REPLACE(a.Account_Code, N'a', N''))) <= @accountLevel)
          AND (@hideZeroBalance = 0 OR NOT (ISNULL(a.opening_balance, 0) = 0 AND ISNULL(a.DepitBalance, 0) = 0 AND ISNULL(a.CreditBalance, 0) = 0 AND ISNULL(a.Balance, 0) = 0))
        ORDER BY a.Account_Serial, a.Account_Code;
        RETURN;
    END;

    IF @key IN (N'opt-3-income-statement-com', N'opt-28-income-statement-int', N'opt-41-income-statement-by-level')
    BEGIN
        SELECT
            CASE WHEN @key = N'opt-28-income-statement-int' THEN N'IncomeStatementNew1.rpt' ELSE N'IncomeStatementNew.rpt' END AS ReportSource,
            CASE WHEN ISNULL(a.AccountTab, 0) = 2 THEN N'الإيرادات' WHEN ISNULL(a.AccountTab, 0) = 3 THEN N'المصروفات' ELSE N'قائمة الدخل' END AS SectionName,
            a.Account_Serial AS AccountSerial,
            a.Account_Code AS AccountCode,
            a.Account_Name AS AccountName,
            a.Account_NameEng AS AccountNameEng,
            a.Parent_Account_Code AS ParentAccountCode,
            ISNULL(a.[Level], 0) AS AccountLevel,
            ISNULL(a.opening_balance, 0) AS OpeningBalance,
            ISNULL(a.Balance, 0) AS Balance,
            CASE WHEN ISNULL(a.Balance, 0) > 0 THEN ISNULL(a.Balance, 0) ELSE 0 END AS Debit,
            CASE WHEN ISNULL(a.Balance, 0) < 0 THEN ABS(ISNULL(a.Balance, 0)) ELSE 0 END AS Credit,
            ISNULL(a.Balance, 0) AS NetAmount
        FROM dbo.ACCOUNTS a WITH (NOLOCK)
        WHERE EXISTS (SELECT 1 FROM #SelectedAccounts sa WHERE sa.Account_Code = a.Account_Code)
          AND (ISNULL(a.last_account, 0) <> 1 OR @key = N'opt-28-income-statement-int')
          AND ISNULL(a.AccountTypes, 0) = 2
          AND (ISNULL(@accountLevel, 0) = 0 OR ISNULL(a.[Level], 0) <= @accountLevel OR (LEN(a.Account_Code) - LEN(REPLACE(a.Account_Code, N'a', N''))) <= @accountLevel)
          AND (@hideZeroBalance = 0 OR ISNULL(a.Balance, 0) <> 0 OR ISNULL(a.opening_balance, 0) <> 0)
        ORDER BY a.Account_Serial, a.Account_Code;
        RETURN;
    END;

    IF @key = N'opt-1-general-ledger-for'
    BEGIN
        SELECT
            N'GenrealLedger.rpt' AS ReportSource,
            a.Account_Serial AS AccountSerial,
            a.Account_Code AS AccountCode,
            a.Account_Name AS AccountName,
            a.Account_NameEng AS AccountNameEng,
            a.Parent_Account_Code AS ParentAccountCode,
            ISNULL(a.[Level], 0) AS AccountLevel,
            ISNULL(a.opening_balance, 0) AS OpeningBalance,
            ISNULL(a.Balance, 0) AS Balance,
            ISNULL(a.DepitBalance, 0) AS Debit,
            ISNULL(a.CreditBalance, 0) AS Credit,
            ISNULL(a.Balance, 0) AS ClosingBalance
        FROM dbo.ACCOUNTS a WITH (NOLOCK)
        WHERE EXISTS (SELECT 1 FROM #SelectedAccounts sa WHERE sa.Account_Code = a.Account_Code OR sa.Account_Code = a.Parent_Account_Code)
          AND (@hideZeroBalance = 0 OR NOT (ISNULL(a.opening_balance, 0) = 0 AND ISNULL(a.DepitBalance, 0) = 0 AND ISNULL(a.CreditBalance, 0) = 0 AND ISNULL(a.Balance, 0) = 0))
        ORDER BY a.Account_Serial, a.Account_Code;
        RETURN;
    END;

    IF @key = N'opt-9-cost-center-transactions'
    BEGIN
        SELECT
            N'Transactions_with_cost_center.rpt' AS ReportSource,
            m.NoteDate,
            m.NoteSerial,
            a.Account_Serial AS AccountSerial,
            a.Account_Code AS AccountCode,
            a.Account_Name AS AccountName,
            m.cost_center_id AS CostCenterId,
            m.cost_center AS CostCenterName,
            m.Project__code AS ProjectId,
            m.Project_name AS ProjectName,
            m.Description,
            CASE WHEN ISNULL(m.depit_or_credit, 0) = 0 THEN ISNULL(m.[value], 0) ELSE 0 END AS Debit,
            CASE WHEN ISNULL(m.depit_or_credit, 0) = 1 THEN ISNULL(m.[value], 0) ELSE 0 END AS Credit,
            m.user_id AS UserId
        FROM dbo.marakes_taklefa_temp m WITH (NOLOCK)
        INNER JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = m.account_no
        WHERE ISNULL(m.ok, 0) = 1
          AND ISNULL(m.line_no, 0) <> 0
          AND m.cost_center_id IS NOT NULL
          AND m.NoteDate >= @from AND m.NoteDate < @toExclusive
          AND (@costCenterId IS NULL OR @costCenterId = 0 OR m.cost_center_id = @costCenterId)
          AND EXISTS (SELECT 1 FROM #SelectedAccounts sa WHERE sa.Account_Code = a.Account_Code)
        ORDER BY m.NoteDate, m.NoteSerial, a.Account_Serial;
        RETURN;
    END;

    IF @key IN (N'opt-10-project-transactions-details', N'opt-30-project-transactions-totals', N'opt-42-project-account-new')
    BEGIN
        SELECT
            CASE WHEN @key = N'opt-42-project-account-new' THEN N'GL _with_projectsAcc.rpt' WHEN @key = N'opt-30-project-transactions-totals' THEN N'GL _with_projects1.rpt' ELSE N'GL _with_projects.rpt' END AS ReportSource,
            r.RecordDate,
            r.NoteDate,
            r.NoteSerial,
            r.NoteSerial1,
            r.NoteType,
            r.project_id AS ProjectId,
            r.Project_name AS ProjectName,
            r.pandid AS OperationId,
            r.opr_fullcode AS OperationName,
            r.Account_Serial AS AccountSerial,
            r.Account_Code AS AccountCode,
            r.Account_Name AS AccountName,
            r.DEV_DES AS Description,
            CASE WHEN ISNULL(r.Credit_Or_Debit, 0) = 0 THEN ISNULL(r.DEV_Value, 0) ELSE 0 END AS Debit,
            CASE WHEN ISNULL(r.Credit_Or_Debit, 0) = 1 THEN ISNULL(r.DEV_Value, 0) ELSE 0 END AS Credit,
            r.net AS NetAmount,
            r.UserName
        FROM dbo.RptLedger_sub_projects r WITH (NOLOCK)
        WHERE r.RecordDate >= @from AND r.RecordDate < @toExclusive
          AND (@projectId IS NULL OR @projectId = 0 OR r.project_id = @projectId)
          AND (@noteType IS NULL OR @noteType = 0 OR r.NoteType = @noteType)
          AND EXISTS (SELECT 1 FROM #SelectedAccounts sa WHERE sa.Account_Code = r.Account_Code)
        ORDER BY CASE WHEN @key = N'opt-30-project-transactions-totals' THEN r.Account_Serial ELSE CONVERT(NVARCHAR(30), r.RecordDate, 112) END, r.Account_Serial, r.NoteSerial;
        RETURN;
    END;

    ;WITH Lines AS
    (
        SELECT
            N'Sub-Masster.rpt' AS ReportSource,
            m.RecordDate,
            m.NoteSerial,
            m.NoteSerial1,
            m.NoteType,
            b.branch_name AS BranchName,
            a.Account_Serial AS AccountSerial,
            m.Account_Code AS AccountCode,
            a.Account_Name AS AccountName,
            m.Description,
            m.Debit,
            m.Credit,
            m.project_id AS ProjectId,
            m.pandid AS OperationId,
            m.UserName
        FROM #MovementBase m
        LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = m.Account_Code
        LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = m.branch_id
        WHERE m.RecordDate >= @from AND m.RecordDate < @toExclusive
    )
    SELECT
        ReportSource,
        RecordDate,
        NoteSerial,
        NoteSerial1,
        NoteType,
        BranchName,
        AccountSerial,
        AccountCode,
        AccountName,
        Description,
        Debit,
        Credit,
        SUM(Debit - Credit) OVER (PARTITION BY AccountCode ORDER BY RecordDate, NoteSerial, NoteSerial1 ROWS UNBOUNDED PRECEDING) AS RunningBalance,
        ProjectId,
        OperationId,
        UserName
    FROM Lines
    ORDER BY AccountSerial, AccountCode, RecordDate, NoteSerial;
END;
GO






