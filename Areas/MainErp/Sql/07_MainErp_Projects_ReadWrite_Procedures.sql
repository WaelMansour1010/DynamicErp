/*
MainErp Projects migration support.
SQL Server 2012 compatible. No schema changes are made by this script.
The web implementation currently uses parameterized repository SQL directly;
these procedures document the supported read/write contract for DBAs who prefer
stored procedure deployment.
*/

IF OBJECT_ID(N'dbo.MainErp_Projects_Search', N'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_Projects_Search;
GO
CREATE PROCEDURE dbo.MainErp_Projects_Search
    @SearchText nvarchar(200) = NULL,
    @StatusId int = NULL,
    @BranchId int = NULL,
    @StartRow int = 1,
    @EndRow int = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SearchLike nvarchar(230);
    SET @SearchLike = CASE WHEN @SearchText IS NULL OR LTRIM(RTRIM(@SearchText)) = N'' THEN NULL ELSE N'%' + @SearchText + N'%' END;

    WITH ProjectRows AS (
        SELECT
            ROW_NUMBER() OVER (ORDER BY p.id DESC) AS RowNo,
            COUNT(1) OVER() AS TotalCount,
            p.id, p.Fullcode, p.Project_name, p.Project_nameE,
            cust.CusName AS CustomerName,
            st.name AS StatusName,
            br.branch_name AS BranchName,
            p.project_cost, p.net, p.cost_after_discount, p.StartDate, p.EndDate
        FROM dbo.projects p
        LEFT JOIN dbo.TblCustemers cust ON cust.CusID = CASE WHEN ISNUMERIC(p.End_user_id) = 1 THEN CONVERT(int, p.End_user_id) END
        LEFT JOIN dbo.project_status st ON st.id = CASE WHEN ISNUMERIC(p.Project_status) = 1 THEN CONVERT(int, p.Project_status) END
        LEFT JOIN dbo.TblBranchesData br ON br.branch_id = p.branch_no
        WHERE (@SearchLike IS NULL
               OR p.Fullcode LIKE @SearchLike
               OR p.Code LIKE @SearchLike
               OR p.Project_name LIKE @SearchLike
               OR p.Project_nameE LIKE @SearchLike
               OR cust.CusName LIKE @SearchLike)
          AND (@StatusId IS NULL OR CASE WHEN ISNUMERIC(p.Project_status) = 1 THEN CONVERT(int, p.Project_status) END = @StatusId)
          AND (@BranchId IS NULL OR p.branch_no = @BranchId)
    )
    SELECT *
    FROM ProjectRows
    WHERE RowNo BETWEEN @StartRow AND @EndRow
    ORDER BY RowNo;
END;
GO

IF OBJECT_ID(N'dbo.MainErp_ProjectExtract_CreateMinimal', N'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_ProjectExtract_CreateMinimal;
GO
CREATE PROCEDURE dbo.MainErp_ProjectExtract_CreateMinimal
    @ProjectId int,
    @BillDate datetime,
    @ManualNo nvarchar(4000) = NULL,
    @Total money = 0,
    @VatValue float = 0,
    @NetValue float = 0,
    @BranchNo int = NULL,
    @UserId int = NULL,
    @Remarks nvarchar(4000) = NULL,
    @NewId int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProjectName nvarchar(4000);
    DECLARE @StartDate datetime;
    DECLARE @NoteSerial int;

    SELECT
        @ProjectName = Project_name,
        @StartDate = StartDate,
        @BranchNo = ISNULL(@BranchNo, branch_no)
    FROM dbo.projects
    WHERE id = @ProjectId;

    IF @ProjectName IS NULL
    BEGIN
        RAISERROR(N'المشروع المحدد غير موجود.', 16, 1);
        RETURN;
    END;

    SELECT @NewId = ISNULL(MAX(id), 0) + 1 FROM dbo.project_billl WITH (UPDLOCK, HOLDLOCK);
    SELECT @NoteSerial = ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial) = 1 THEN CONVERT(int, NoteSerial) END), 0) + 1 FROM dbo.project_billl WITH (UPDLOCK, HOLDLOCK);

    INSERT INTO dbo.project_billl
    (
        id, bill_date, project_no, project_name, total, FATValue, NetValue,
        Branch_NO, ManualNO, NoteSerial, Results, UserID, Remarks, StartDateProje
    )
    VALUES
    (
        @NewId, @BillDate, CONVERT(nvarchar(50), @ProjectId), @ProjectName, @Total, @VatValue,
        CASE WHEN ISNULL(@NetValue, 0) = 0 THEN ISNULL(@Total, 0) + ISNULL(@VatValue, 0) ELSE @NetValue END,
        @BranchNo, @ManualNo, CONVERT(varchar(500), @NoteSerial), 0, @UserId, @Remarks, @StartDate
    );
END;
GO
