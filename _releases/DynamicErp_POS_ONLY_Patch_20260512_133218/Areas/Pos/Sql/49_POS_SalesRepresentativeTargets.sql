/*
    POS Sales Representative Targets
    SQL Server 2012 compatible.
*/

IF OBJECT_ID(N'dbo.POS_SalesRepresentativeTargets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SalesRepresentativeTargets
    (
        TargetId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SalesRepresentativeTargets PRIMARY KEY,
        UserID INT NULL,
        BranchId INT NULL,
        FromDate DATETIME NOT NULL,
        ToDate DATETIME NOT NULL,
        MonthlyRechargeTarget DECIMAL(18, 2) NOT NULL CONSTRAINT DF_POS_SalesTargets_Recharge DEFAULT (0),
        MonthlyCardTarget INT NOT NULL CONSTRAINT DF_POS_SalesTargets_Cards DEFAULT (0),
        WorkingDaysInMonth INT NOT NULL CONSTRAINT DF_POS_SalesTargets_WorkingDays DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_POS_SalesTargets_IsActive DEFAULT (1),
        CreatedByUserID INT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_SalesTargets_CreatedAt DEFAULT (GETDATE()),
        UpdatedByUserID INT NULL,
        UpdatedAt DATETIME NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SalesTargets_ActiveScope' AND object_id = OBJECT_ID(N'dbo.POS_SalesRepresentativeTargets'))
BEGIN
    CREATE INDEX IX_POS_SalesTargets_ActiveScope
    ON dbo.POS_SalesRepresentativeTargets (IsActive, UserID, BranchId, FromDate, ToDate);
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_SalesTargets_List', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SalesTargets_List;
GO

CREATE PROCEDURE dbo.usp_POS_SalesTargets_List
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @To DATETIME = CONVERT(DATE, ISNULL(@ToDate, @From));
    IF @To < @From
    BEGIN
        DECLARE @Swap DATETIME = @From;
        SET @From = @To;
        SET @To = @Swap;
    END;

    SELECT TOP (500)
        t.TargetId,
        t.UserID,
        RepresentativeName = CASE
            WHEN t.UserID IS NULL THEN N'كل المناديب'
            ELSE COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), N'User ' + CONVERT(NVARCHAR(20), t.UserID))
        END,
        t.BranchId,
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), t.BranchId)),
        t.FromDate,
        t.ToDate,
        t.MonthlyRechargeTarget,
        t.MonthlyCardTarget,
        t.WorkingDaysInMonth,
        DailyRechargeTarget = CONVERT(DECIMAL(18, 2), CASE WHEN t.WorkingDaysInMonth > 0 THEN t.MonthlyRechargeTarget / t.WorkingDaysInMonth ELSE 0 END),
        DailyCardTarget = CONVERT(DECIMAL(18, 2), CASE WHEN t.WorkingDaysInMonth > 0 THEN CONVERT(DECIMAL(18, 2), t.MonthlyCardTarget) / t.WorkingDaysInMonth ELSE 0 END),
        t.CreatedAt,
        CreatedByName = COALESCE(NULLIF(ce.Emp_Name, N''), NULLIF(cu.UserName, N''), N'')
    FROM dbo.POS_SalesRepresentativeTargets t WITH (NOLOCK)
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = t.UserID
    LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = u.Empid
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = t.BranchId
    LEFT JOIN dbo.TblUsers cu WITH (NOLOCK) ON cu.UserID = t.CreatedByUserID
    LEFT JOIN dbo.TblEmployee ce WITH (NOLOCK) ON ce.Emp_ID = cu.Empid
    WHERE t.IsActive = 1
      AND t.FromDate <= @To
      AND t.ToDate >= @From
      AND (@BranchId IS NULL OR t.BranchId IS NULL OR t.BranchId = @BranchId)
      AND (@UserId IS NULL OR t.UserID IS NULL OR t.UserID = @UserId)
    ORDER BY t.FromDate DESC, CASE WHEN t.UserID IS NULL THEN 1 ELSE 0 END, t.TargetId DESC;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_SalesTargets_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SalesTargets_Save;
GO

CREATE PROCEDURE dbo.usp_POS_SalesTargets_Save
    @ApplyMode NVARCHAR(20),
    @UserIds NVARCHAR(MAX) = NULL,
    @BranchId INT = NULL,
    @FromDate DATETIME,
    @ToDate DATETIME,
    @MonthlyRechargeTarget DECIMAL(18, 2),
    @MonthlyCardTarget INT,
    @WorkingDaysInMonth INT,
    @CreatedByUserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @To DATETIME = CONVERT(DATE, ISNULL(@ToDate, @From));
    IF @To < @From
    BEGIN
        DECLARE @Swap DATETIME = @From;
        SET @From = @To;
        SET @To = @Swap;
    END;

    SET @ApplyMode = LOWER(LTRIM(RTRIM(ISNULL(@ApplyMode, N''))));
    IF @WorkingDaysInMonth <= 0 OR @WorkingDaysInMonth > 31
        SET @WorkingDaysInMonth = DAY(DATEADD(DAY, -1, DATEADD(MONTH, 1, DATEADD(DAY, 1 - DAY(@From), @From))));

    IF @ApplyMode = N'all'
    BEGIN
        UPDATE dbo.POS_SalesRepresentativeTargets
        SET IsActive = 0,
            UpdatedByUserID = @CreatedByUserID,
            UpdatedAt = GETDATE()
        WHERE IsActive = 1
          AND UserID IS NULL
          AND ((@BranchId IS NULL AND BranchId IS NULL) OR (@BranchId IS NOT NULL AND (BranchId = @BranchId OR BranchId IS NULL)))
          AND FromDate <= @To
          AND ToDate >= @From;

        INSERT INTO dbo.POS_SalesRepresentativeTargets
        (
            UserID, BranchId, FromDate, ToDate, MonthlyRechargeTarget, MonthlyCardTarget,
            WorkingDaysInMonth, IsActive, CreatedByUserID, CreatedAt
        )
        VALUES
        (
            NULL, @BranchId, @From, @To, ISNULL(@MonthlyRechargeTarget, 0), ISNULL(@MonthlyCardTarget, 0),
            @WorkingDaysInMonth, 1, @CreatedByUserID, GETDATE()
        );

        RETURN;
    END;

    DECLARE @SelectedUsers TABLE (UserID INT NOT NULL PRIMARY KEY);
    SET @UserIds = LTRIM(RTRIM(ISNULL(@UserIds, N'')));

    IF @UserIds <> N''
    BEGIN
        DECLARE @Xml XML;
        SET @Xml = CAST(N'<x><i>' + REPLACE(REPLACE(@UserIds, N' ', N''), N',', N'</i><i>') + N'</i></x>' AS XML);

        INSERT INTO @SelectedUsers (UserID)
        SELECT DISTINCT CONVERT(INT, x.i.value(N'.', N'NVARCHAR(20)'))
        FROM @Xml.nodes(N'/x/i') AS x(i)
        WHERE ISNUMERIC(x.i.value(N'.', N'NVARCHAR(20)')) = 1
          AND CONVERT(INT, x.i.value(N'.', N'NVARCHAR(20)')) > 0;
    END;

    UPDATE t
    SET IsActive = 0,
        UpdatedByUserID = @CreatedByUserID,
        UpdatedAt = GETDATE()
    FROM dbo.POS_SalesRepresentativeTargets t
    INNER JOIN @SelectedUsers su ON su.UserID = t.UserID
    WHERE t.IsActive = 1
      AND ((@BranchId IS NULL AND t.BranchId IS NULL) OR (@BranchId IS NOT NULL AND (t.BranchId = @BranchId OR t.BranchId IS NULL)))
      AND t.FromDate <= @To
      AND t.ToDate >= @From;

    INSERT INTO dbo.POS_SalesRepresentativeTargets
    (
        UserID, BranchId, FromDate, ToDate, MonthlyRechargeTarget, MonthlyCardTarget,
        WorkingDaysInMonth, IsActive, CreatedByUserID, CreatedAt
    )
    SELECT
        su.UserID, @BranchId, @From, @To, ISNULL(@MonthlyRechargeTarget, 0), ISNULL(@MonthlyCardTarget, 0),
        @WorkingDaysInMonth, 1, @CreatedByUserID, GETDATE()
    FROM @SelectedUsers su;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_SalesTargets_Deactivate', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SalesTargets_Deactivate;
GO

CREATE PROCEDURE dbo.usp_POS_SalesTargets_Deactivate
    @TargetId INT,
    @UpdatedByUserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.POS_SalesRepresentativeTargets
    SET IsActive = 0,
        UpdatedByUserID = @UpdatedByUserID,
        UpdatedAt = GETDATE()
    WHERE TargetId = @TargetId;
END;
GO
