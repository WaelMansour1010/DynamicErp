/*
    POS KYC/Bank export VB6 parity.
    SQL Server 2012 compatible.

    Matches legacy VB6:
    FrmCustCash.btnExport(0) -> ExcelBank(18)
    FrmCustCash.btnExport(1) -> ExcelBank(8)
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

    IF @cardLength NOT IN (8, 18)
        SET @cardLength = 18;

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
