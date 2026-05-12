IF OBJECT_ID(N'dbo.usp_POS_Report_NonWebLoginUsers', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Report_NonWebLoginUsers;
GO

CREATE PROCEDURE dbo.usp_POS_Report_NonWebLoginUsers
    @fromDate DATETIME,
    @toDate DATETIME,
    @branchId INT = 0,
    @filterUserId INT = NULL,
    @loginSource NVARCHAR(80) = N'',
    @userId INT = 0,
    @canChangeDefaults BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME;
    DECLARE @toExclusive DATETIME;
    DECLARE @sourceFilter NVARCHAR(80);

    SET @from = DATEADD(DAY, DATEDIFF(DAY, 0, ISNULL(@fromDate, GETDATE())), 0);
    SET @toExclusive = DATEADD(DAY, 1, DATEADD(DAY, DATEDIFF(DAY, 0, ISNULL(@toDate, @from)), 0));
    SET @sourceFilter = LTRIM(RTRIM(ISNULL(@loginSource, N'')));

    ;WITH NonWebActivity AS
    (
        SELECT
            t.Transaction_ID,
            t.Transaction_Date,
            t.UserID,
            t.Emp_ID,
            t.BranchId,
            LoginSource = NULLIF(LTRIM(RTRIM(ISNULL(t.NoID, N''))), N'')
        FROM dbo.Transactions AS t
        WHERE t.Transaction_Type = 21
          AND ISNULL(t.NoID, N'') <> N'WEB_POS'
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND (ISNULL(@branchId, 0) <= 0 OR t.BranchId = @branchId)
          AND (@filterUserId IS NULL OR t.UserID = @filterUserId)
          AND (@sourceFilter = N'' OR ISNULL(t.NoID, N'') LIKE N'%' + @sourceFilter + N'%')
    )
    SELECT
        UserId = nwa.UserID,
        UserName = ISNULL(u.UserName, N''),
        EmployeeName = ISNULL(e.Emp_Name, N''),
        BranchId = nwa.BranchId,
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), nwa.BranchId)),
        LastLoginDate = MAX(nwa.Transaction_Date),
        LoginSource = ISNULL(nwa.LoginSource, N'Legacy/Desktop'),
        LoginType = N'Non-Web',
        IpAddress = N'',
        MachineName = N'',
        AppName = CASE WHEN ISNULL(nwa.LoginSource, N'') = N'' THEN N'VB6 / RemoteApp / Legacy' ELSE nwa.LoginSource END,
        ClientType = N'Non-Web',
        LoginCount = COUNT(1)
    FROM NonWebActivity AS nwa
    LEFT JOIN dbo.TblUsers AS u ON u.UserID = nwa.UserID
    LEFT JOIN dbo.TblEmployee AS e ON e.Emp_ID = nwa.Emp_ID
    LEFT JOIN dbo.TblBranchesData AS b ON b.branch_id = nwa.BranchId
    GROUP BY
        nwa.UserID,
        u.UserName,
        e.Emp_Name,
        nwa.BranchId,
        b.branch_name,
        b.branch_namee,
        nwa.LoginSource
    ORDER BY
        MAX(nwa.Transaction_Date) DESC,
        COUNT(1) DESC,
        nwa.UserID;
END;
GO
