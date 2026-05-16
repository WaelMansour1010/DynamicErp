/*
Kishny POS - Closing Reports Baseline Compare
Phase 3 read-only helper for dbo.usp_POS_Report_RunClosing.

SQL Server 2012 compatible.

Purpose:
- Run the current POS closing report procedure for:
  1) finance-closing
  2) finance-closing-discounts
- Compare row counts and available total columns for:
  1) today
  2) last 7 days
  3) current month
- Test both:
  1) one selected branch
  2) all branches using @BranchId = 0

Notes:
- dbo.usp_POS_Report_RunClosing treats all-branch mode as @BranchId <= 0.
- If you need to compare against an old/manual report query, paste that query in
  the marked section near the end and insert matching summary rows into #Summary.
- This script does not create or modify objects.
*/

SET NOCOUNT ON;

DECLARE @Today DATETIME;
DECLARE @Last7From DATETIME;
DECLARE @MonthFrom DATETIME;
DECLARE @SelectedBranchId INT;
DECLARE @UserId INT;

SET @Today = DATEADD(DAY, DATEDIFF(DAY, 0, GETDATE()), 0);
SET @Last7From = DATEADD(DAY, -6, @Today);
SET @MonthFrom = DATEADD(MONTH, DATEDIFF(MONTH, 0, @Today), 0);

SELECT TOP (1)
    @SelectedBranchId = c.BranchID
FROM dbo.TBLClosePos c
WHERE c.OrderDate >= @MonthFrom
  AND c.OrderDate < DATEADD(DAY, 1, @Today)
GROUP BY c.BranchID
ORDER BY COUNT(1) DESC, c.BranchID;

IF @SelectedBranchId IS NULL
BEGIN
    SELECT TOP (1)
        @SelectedBranchId = c.BranchID
    FROM dbo.TBLClosePos c
    WHERE c.BranchID IS NOT NULL
    GROUP BY c.BranchID
    ORDER BY COUNT(1) DESC, c.BranchID;
END;

SELECT TOP (1)
    @UserId = u.UserID
FROM dbo.TblUsers u
ORDER BY u.UserID;

IF @UserId IS NULL
BEGIN
    SET @UserId = 0;
END;

CREATE TABLE #Scenarios
(
    ScenarioName NVARCHAR(50) NOT NULL,
    FromDate DATETIME NOT NULL,
    ToDate DATETIME NOT NULL,
    BranchMode NVARCHAR(30) NOT NULL,
    BranchId INT NOT NULL
);

INSERT INTO #Scenarios (ScenarioName, FromDate, ToDate, BranchMode, BranchId)
VALUES
    (N'Today', @Today, @Today, N'SelectedBranch', ISNULL(@SelectedBranchId, 0)),
    (N'Today', @Today, @Today, N'AllBranches', 0),
    (N'Last7Days', @Last7From, @Today, N'SelectedBranch', ISNULL(@SelectedBranchId, 0)),
    (N'Last7Days', @Last7From, @Today, N'AllBranches', 0),
    (N'CurrentMonth', @MonthFrom, @Today, N'SelectedBranch', ISNULL(@SelectedBranchId, 0)),
    (N'CurrentMonth', @MonthFrom, @Today, N'AllBranches', 0);

CREATE TABLE #Summary
(
    SourceName NVARCHAR(100) NOT NULL,
    ReportKey NVARCHAR(80) NOT NULL,
    ScenarioName NVARCHAR(50) NOT NULL,
    BranchMode NVARCHAR(30) NOT NULL,
    BranchId INT NOT NULL,
    FromDate DATETIME NOT NULL,
    ToDate DATETIME NOT NULL,
    ResultRowCount INT NOT NULL,
    TotalSupply DECIMAL(38, 3) NULL,
    TotalRechargeValue DECIMAL(38, 3) NULL,
    TotalRev DECIMAL(38, 3) NULL,
    TotalVat DECIMAL(38, 3) NULL,
    CashOutTotal DECIMAL(38, 3) NULL,
    BoxBalance DECIMAL(38, 3) NULL,
    NoteValue DECIMAL(38, 3) NULL,
    CountCards DECIMAL(38, 3) NULL,
    CountTransaction DECIMAL(38, 3) NULL,
    TotalReturns DECIMAL(38, 3) NULL,
    DurationMs INT NULL
);

CREATE TABLE #FinanceClosing
(
    BranchName NVARCHAR(255) NULL,
    ClosingDate DATETIME NULL,
    NoteID INT NULL,
    NoteSerial NVARCHAR(50) NULL,
    NoteSerial1 NVARCHAR(50) NULL,
    NoteDate DATETIME NULL,
    VoucherType NVARCHAR(100) NULL,
    OpenBalance DECIMAL(38, 6) NULL,
    LastBalance DECIMAL(38, 6) NULL,
    TotalRechargeValue DECIMAL(38, 6) NULL,
    TotalRev DECIMAL(38, 6) NULL,
    TotalVat DECIMAL(38, 6) NULL,
    CashOutTotal DECIMAL(38, 6) NULL,
    TotalSupply DECIMAL(38, 6) NULL,
    BoxBalance DECIMAL(38, 6) NULL,
    NoteValue DECIMAL(38, 6) NULL,
    UserName NVARCHAR(255) NULL
);

CREATE TABLE #FinanceClosingDiscounts
(
    RowNo INT NULL,
    BranchID INT NULL,
    BranchName NVARCHAR(255) NULL,
    TotalSupply DECIMAL(38, 6) NULL,
    CountCards DECIMAL(38, 6) NULL,
    TotalSaleDay2Vat DECIMAL(38, 6) NULL,
    CardValue DECIMAL(38, 6) NULL,
    CountTransaction DECIMAL(38, 6) NULL,
    WalletBalance DECIMAL(38, 6) NULL,
    WalletSupply DECIMAL(38, 6) NULL,
    BankBalanceCharge DECIMAL(38, 6) NULL,
    TotalRechargeValue DECIMAL(38, 6) NULL,
    TotalRev2 DECIMAL(38, 6) NULL,
    TotalRevWithVat DECIMAL(38, 6) NULL,
    ReturnsCount INT NULL,
    TotalReturns DECIMAL(38, 6) NULL,
    NetCashOut DECIMAL(38, 6) NULL,
    BoxValue DECIMAL(38, 6) NULL,
    ClosingStatus NVARCHAR(50) NULL
);

DECLARE
    @ScenarioName NVARCHAR(50),
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchMode NVARCHAR(30),
    @BranchId INT,
    @StartedAt DATETIME,
    @DurationMs INT;

DECLARE scenario_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT ScenarioName, FromDate, ToDate, BranchMode, BranchId
    FROM #Scenarios
    ORDER BY
        CASE ScenarioName WHEN N'Today' THEN 1 WHEN N'Last7Days' THEN 2 ELSE 3 END,
        CASE BranchMode WHEN N'SelectedBranch' THEN 1 ELSE 2 END;

OPEN scenario_cursor;
FETCH NEXT FROM scenario_cursor INTO @ScenarioName, @FromDate, @ToDate, @BranchMode, @BranchId;

WHILE @@FETCH_STATUS = 0
BEGIN
    TRUNCATE TABLE #FinanceClosing;
    SET @StartedAt = GETDATE();

    INSERT INTO #FinanceClosing
    EXEC dbo.usp_POS_Report_RunClosing
        @reportKey = N'finance-closing',
        @fromDate = @FromDate,
        @toDate = @ToDate,
        @branchId = @BranchId,
        @userId = @UserId,
        @canChangeDefaults = 1,
        @branchFromId = NULL,
        @branchToId = NULL,
        @showEmptyBranches = 0,
        @serviceSearch = NULL,
        @filterUserId = NULL;

    SET @DurationMs = DATEDIFF(MILLISECOND, @StartedAt, GETDATE());

    INSERT INTO #Summary
    (
        SourceName, ReportKey, ScenarioName, BranchMode, BranchId, FromDate, ToDate,
        ResultRowCount, TotalSupply, TotalRechargeValue, TotalRev, TotalVat, CashOutTotal,
        BoxBalance, NoteValue, CountCards, CountTransaction, TotalReturns, DurationMs
    )
    SELECT
        N'CurrentProcedure',
        N'finance-closing',
        @ScenarioName,
        @BranchMode,
        @BranchId,
        @FromDate,
        @ToDate,
        COUNT(1),
        SUM(ISNULL(TotalSupply, 0)),
        SUM(ISNULL(TotalRechargeValue, 0)),
        SUM(ISNULL(TotalRev, 0)),
        SUM(ISNULL(TotalVat, 0)),
        SUM(ISNULL(CashOutTotal, 0)),
        SUM(ISNULL(BoxBalance, 0)),
        SUM(ISNULL(NoteValue, 0)),
        NULL,
        NULL,
        NULL,
        @DurationMs
    FROM #FinanceClosing;

    TRUNCATE TABLE #FinanceClosingDiscounts;
    SET @StartedAt = GETDATE();

    INSERT INTO #FinanceClosingDiscounts
    EXEC dbo.usp_POS_Report_RunClosing
        @reportKey = N'finance-closing-discounts',
        @fromDate = @FromDate,
        @toDate = @ToDate,
        @branchId = @BranchId,
        @userId = @UserId,
        @canChangeDefaults = 1,
        @branchFromId = NULL,
        @branchToId = NULL,
        @showEmptyBranches = 0,
        @serviceSearch = NULL,
        @filterUserId = NULL;

    SET @DurationMs = DATEDIFF(MILLISECOND, @StartedAt, GETDATE());

    INSERT INTO #Summary
    (
        SourceName, ReportKey, ScenarioName, BranchMode, BranchId, FromDate, ToDate,
        ResultRowCount, TotalSupply, TotalRechargeValue, TotalRev, TotalVat, CashOutTotal,
        BoxBalance, NoteValue, CountCards, CountTransaction, TotalReturns, DurationMs
    )
    SELECT
        N'CurrentProcedure',
        N'finance-closing-discounts',
        @ScenarioName,
        @BranchMode,
        @BranchId,
        @FromDate,
        @ToDate,
        COUNT(1),
        SUM(ISNULL(TotalSupply, 0)),
        SUM(ISNULL(TotalRechargeValue, 0)),
        NULL,
        SUM(ISNULL(TotalRevWithVat, 0)),
        SUM(ISNULL(NetCashOut, 0)),
        SUM(ISNULL(BoxValue, 0)),
        NULL,
        SUM(ISNULL(CountCards, 0)),
        SUM(ISNULL(CountTransaction, 0)),
        SUM(ISNULL(TotalReturns, 0)),
        @DurationMs
    FROM #FinanceClosingDiscounts;

    FETCH NEXT FROM scenario_cursor INTO @ScenarioName, @FromDate, @ToDate, @BranchMode, @BranchId;
END;

CLOSE scenario_cursor;
DEALLOCATE scenario_cursor;

/*
Optional old/manual comparison section:

1. Paste the old query for finance-closing or finance-closing-discounts here.
2. Use the same @ScenarioName / @FromDate / @ToDate / @BranchMode / @BranchId values.
3. Insert summary rows into #Summary with SourceName = N'OldManualQuery'.
4. Compare the output from the final grouped SELECT below.

Example skeleton:

INSERT INTO #Summary (...)
SELECT
    N'OldManualQuery',
    N'finance-closing',
    @ScenarioName,
    @BranchMode,
    @BranchId,
    @FromDate,
    @ToDate,
    COUNT(1),
    SUM(...),
    ...
FROM (... old query ...) q;
*/

SELECT
    SourceName,
    ReportKey,
    ScenarioName,
    BranchMode,
    BranchId,
    CONVERT(VARCHAR(10), FromDate, 120) AS FromDate,
    CONVERT(VARCHAR(10), ToDate, 120) AS ToDate,
    ResultRowCount,
    TotalSupply,
    TotalRechargeValue,
    TotalRev,
    TotalVat,
    CashOutTotal,
    BoxBalance,
    NoteValue,
    CountCards,
    CountTransaction,
    TotalReturns,
    DurationMs
FROM #Summary
ORDER BY
    ReportKey,
    CASE ScenarioName WHEN N'Today' THEN 1 WHEN N'Last7Days' THEN 2 ELSE 3 END,
    CASE BranchMode WHEN N'SelectedBranch' THEN 1 ELSE 2 END,
    SourceName;

DROP TABLE #FinanceClosing;
DROP TABLE #FinanceClosingDiscounts;
DROP TABLE #Summary;
DROP TABLE #Scenarios;
