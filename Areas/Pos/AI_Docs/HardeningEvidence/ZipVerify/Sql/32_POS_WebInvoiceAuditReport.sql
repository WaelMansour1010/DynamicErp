IF OBJECT_ID(N'dbo.usp_POS_Report_WebInvoices', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Report_WebInvoices;
GO

CREATE PROCEDURE dbo.usp_POS_Report_WebInvoices
    @fromDate DATETIME,
    @toDate DATETIME,
    @branchId INT = 0,
    @userId INT = 0,
    @canChangeDefaults BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME;
    DECLARE @toExclusive DATETIME;

    SET @from = DATEADD(DAY, DATEDIFF(DAY, 0, ISNULL(@fromDate, GETDATE())), 0);
    SET @toExclusive = DATEADD(DAY, 1, DATEADD(DAY, DATEDIFF(DAY, 0, ISNULL(@toDate, @from)), 0));

    ;WITH WebInvoices AS
    (
        SELECT
            t.Transaction_ID,
            t.NoteSerial1,
            t.Transaction_Date,
            t.UserID,
            t.Emp_ID,
            t.BranchId,
            ISNULL(t.NetValue, 0) AS NetValue,
            ISNULL(t.PayedValue, 0) AS PayedValue
        FROM dbo.Transactions AS t
        WHERE t.Transaction_Type = 21
          AND ISNULL(t.NoID, '') = 'WEB_POS'
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND (ISNULL(@branchId, 0) <= 0 OR t.BranchId = @branchId)
    )
    SELECT
        [رقم المستخدم] = wi.UserID,
        [اسم المستخدم] = ISNULL(u.UserName, N''),
        [رقم الموظف] = wi.Emp_ID,
        [اسم الموظف] = ISNULL(e.Emp_Name, N''),
        [عدد فواتير الويب] = COUNT(1),
        [إجمالي الصافي] = CONVERT(DECIMAL(18, 2), SUM(wi.NetValue)),
        [إجمالي المدفوع] = CONVERT(DECIMAL(18, 2), SUM(wi.PayedValue)),
        [أول تاريخ] = CONVERT(NVARCHAR(10), MIN(wi.Transaction_Date), 103),
        [آخر تاريخ] = CONVERT(NVARCHAR(10), MAX(wi.Transaction_Date), 103),
        [الفروع] =
            STUFF((
                SELECT DISTINCT N'، ' + COALESCE(NULLIF(b2.branch_name, N''), NULLIF(b2.branch_namee, N''), CONVERT(NVARCHAR(50), wi2.BranchId))
                FROM WebInvoices AS wi2
                LEFT JOIN dbo.TblBranchesData AS b2 ON b2.branch_id = wi2.BranchId
                WHERE ISNULL(wi2.UserID, -1) = ISNULL(wi.UserID, -1)
                FOR XML PATH(''), TYPE
            ).value('.', 'NVARCHAR(MAX)'), 1, 2, N''),
        [مصدر التسجيل] = N'ويب'
    FROM WebInvoices AS wi
    LEFT JOIN dbo.TblUsers AS u ON u.UserID = wi.UserID
    LEFT JOIN dbo.TblEmployee AS e ON e.Emp_ID = wi.Emp_ID
    GROUP BY
        wi.UserID,
        u.UserName,
        wi.Emp_ID,
        e.Emp_Name
    ORDER BY
        COUNT(1) DESC,
        wi.UserID;
END;
GO
