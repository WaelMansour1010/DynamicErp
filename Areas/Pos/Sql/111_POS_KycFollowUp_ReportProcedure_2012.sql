/*
    Kishny POS - KYC follow-up report procedure hotfix.
    SQL Server 2012 compatible: DROP + CREATE, OFFSET/FETCH only.
    This script intentionally avoids Arabic string literals; UI status text is built in C#.
*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'VisaNumber') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'IX_POS_Transactions_KycFollow_VisaNumber')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transactions_KycFollow_VisaNumber
    ON dbo.Transactions(Transaction_Type, VisaNumber)
    INCLUDE (Transaction_ID, Transaction_Date);
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_KycFollowUp_Report', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_KycFollowUp_Report;
GO

CREATE PROCEDURE dbo.usp_POS_KycFollowUp_Report
    @fromDate DATETIME,
    @toDate DATETIME,
    @branchId INT = NULL,
    @search NVARCHAR(255) = N'',
    @invoiceStatus NVARCHAR(20) = N'all',
    @smartFilter NVARCHAR(40) = N'',
    @offset INT = 0,
    @pageSize INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @toExclusive DATETIME;
    DECLARE @searchLike NVARCHAR(260);

    SET @fromDate = CONVERT(DATE, @fromDate);
    SET @toExclusive = DATEADD(DAY, 1, CONVERT(DATE, @toDate));
    SET @search = LTRIM(RTRIM(ISNULL(@search, N'')));
    SET @invoiceStatus = LOWER(LTRIM(RTRIM(ISNULL(@invoiceStatus, N'all'))));
    SET @smartFilter = LOWER(LTRIM(RTRIM(ISNULL(@smartFilter, N''))));
    SET @offset = CASE WHEN ISNULL(@offset, 0) < 0 THEN 0 ELSE @offset END;
    SET @pageSize = CASE WHEN ISNULL(@pageSize, 0) <= 0 THEN 25 WHEN @pageSize > 100000 THEN 100000 ELSE @pageSize END;
    SET @searchLike = CASE WHEN @search = N'' THEN NULL ELSE N'%' + @search + N'%' END;

    CREATE TABLE #KycBase
    (
        CustomerID INT NOT NULL PRIMARY KEY,
        ArabicName NVARCHAR(500) COLLATE DATABASE_DEFAULT NULL,
        EnglishName NVARCHAR(500) COLLATE DATABASE_DEFAULT NULL,
        NationalId NVARCHAR(100) COLLATE DATABASE_DEFAULT NULL,
        Phone NVARCHAR(100) COLLATE DATABASE_DEFAULT NULL,
        TokenCardNumber NVARCHAR(255) COLLATE DATABASE_DEFAULT NULL,
        CardNo NVARCHAR(255) COLLATE DATABASE_DEFAULT NULL,
        CardId NVARCHAR(255) COLLATE DATABASE_DEFAULT NULL,
        BranchID INT NULL,
        BranchName NVARCHAR(255) COLLATE DATABASE_DEFAULT NULL,
        CreatedDate DATETIME NULL,
        LastUpdateDate DATETIME NULL,
        MissingRequiredData INT NOT NULL,
        IncompleteKyc INT NOT NULL
    );

    INSERT INTO #KycBase
    (
        CustomerID, ArabicName, EnglishName, NationalId, Phone, TokenCardNumber, CardNo, CardId,
        BranchID, BranchName, CreatedDate, LastUpdateDate, MissingRequiredData, IncompleteKyc
    )
    SELECT
        c.Id,
        COALESCE(NULLIF(c.name, N''), NULLIF(c.CustName, N''), LTRIM(RTRIM(ISNULL(c.ArabicName0, N'') + N' ' + ISNULL(c.ArabicName1, N'') + N' ' + ISNULL(c.ArabicName2, N'') + N' ' + ISNULL(c.ArabicName3, N'')))) COLLATE DATABASE_DEFAULT,
        COALESCE(NULLIF(c.namee, N''), LTRIM(RTRIM(ISNULL(c.EnglishName0, N'') + N' ' + ISNULL(c.EnglishName1, N'') + N' ' + ISNULL(c.EnglishName2, N'') + N' ' + ISNULL(c.EnglishName3, N'')))) COLLATE DATABASE_DEFAULT,
        NULLIF(LTRIM(RTRIM(ISNULL(c.Tet_NumPoket, N''))), N'') COLLATE DATABASE_DEFAULT,
        COALESCE(NULLIF(c.PhoneNo2, N''), NULLIF(c.PhoneNo, N''), NULLIF(c.tel, N'')) COLLATE DATABASE_DEFAULT,
        COALESCE(NULLIF(c.CardNo, N''), NULLIF(c.CardId, N''), NULLIF(c.card, N'')) COLLATE DATABASE_DEFAULT,
        NULLIF(LTRIM(RTRIM(ISNULL(c.CardNo, N''))), N'') COLLATE DATABASE_DEFAULT,
        NULLIF(LTRIM(RTRIM(ISNULL(c.CardId, N''))), N'') COLLATE DATABASE_DEFAULT,
        c.BranchID,
        COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'Branch ' + CONVERT(NVARCHAR(20), c.BranchID)) COLLATE DATABASE_DEFAULT,
        COALESCE(c.SaveDate, c.RecordDate, c.OrderDate),
        COALESCE(c.RecordDate, c.SaveDate, c.OrderDate),
        CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(c.PhoneNo2, N''))), N'') IS NULL
               OR NULLIF(LTRIM(RTRIM(ISNULL(c.Tet_NumPoket, N''))), N'') IS NULL
               OR COALESCE(NULLIF(c.CardNo, N''), NULLIF(c.CardId, N'')) IS NULL
             THEN 1 ELSE 0 END,
        CASE WHEN c.BirthDate IS NULL
               OR NULLIF(LTRIM(RTRIM(ISNULL(c.Address, N''))), N'') IS NULL
               OR NULLIF(LTRIM(RTRIM(ISNULL(c.name, N''))), N'') IS NULL
             THEN 1 ELSE 0 END
    FROM dbo.TblCusCsh c WITH (NOLOCK)
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = c.BranchID
    WHERE ISNULL(c.EasyCashType, 0) = 0
      AND COALESCE(c.SaveDate, c.RecordDate, c.OrderDate) >= @fromDate
      AND COALESCE(c.SaveDate, c.RecordDate, c.OrderDate) < @toExclusive
      AND (@branchId IS NULL OR c.BranchID = @branchId)
      AND
      (
          @search = N''
          OR c.PhoneNo2 = @search
          OR c.PhoneNo = @search
          OR c.tel = @search
          OR c.Tet_NumPoket = @search
          OR c.CardNo = @search
          OR c.CardId = @search
          OR c.card = @search
          OR ISNULL(c.name, N'') LIKE @searchLike
          OR ISNULL(c.CustName, N'') LIKE @searchLike
          OR ISNULL(c.namee, N'') LIKE @searchLike
      );

    CREATE TABLE #KycTokens
    (
        CustomerID INT NOT NULL,
        Token NVARCHAR(255) COLLATE DATABASE_DEFAULT NOT NULL
    );

    INSERT INTO #KycTokens(CustomerID, Token)
    SELECT CustomerID, CardNo FROM #KycBase WHERE CardNo IS NOT NULL
    UNION
    SELECT CustomerID, CardId FROM #KycBase WHERE CardId IS NOT NULL;

    CREATE INDEX IX_KycTokens_Token ON #KycTokens(Token, CustomerID);

    CREATE TABLE #InvoiceAgg
    (
        CustomerID INT NOT NULL PRIMARY KEY,
        InvoiceCount INT NOT NULL,
        LastInvoiceDate DATETIME NULL
    );

    INSERT INTO #InvoiceAgg(CustomerID, InvoiceCount, LastInvoiceDate)
    SELECT
        kt.CustomerID,
        COUNT(DISTINCT t.Transaction_ID),
        MAX(t.Transaction_Date)
    FROM #KycTokens kt
    INNER JOIN dbo.Transactions t WITH (NOLOCK)
        ON t.Transaction_Type = 21
       AND t.VisaNumber COLLATE DATABASE_DEFAULT = kt.Token COLLATE DATABASE_DEFAULT
    GROUP BY kt.CustomerID;

    CREATE TABLE #AttachmentAgg
    (
        CustomerID INT NOT NULL PRIMARY KEY,
        AttachmentCount INT NOT NULL
    );

    IF OBJECT_ID(N'dbo.Subject_doc', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.Subject_doc', N'subject_no') IS NOT NULL
       AND COL_LENGTH(N'dbo.Subject_doc', N'operation_type') IS NOT NULL
    BEGIN
        INSERT INTO #AttachmentAgg(CustomerID, AttachmentCount)
        SELECT
            k.CustomerID,
            COUNT(1)
        FROM #KycBase k
        INNER JOIN dbo.Subject_doc sd WITH (NOLOCK)
            ON sd.operation_type COLLATE DATABASE_DEFAULT = N'0701201991' COLLATE DATABASE_DEFAULT
           AND ISNULL(sd.IsDeleted, 0) = 0
           AND sd.subject_no COLLATE DATABASE_DEFAULT = k.TokenCardNumber COLLATE DATABASE_DEFAULT
        WHERE k.TokenCardNumber IS NOT NULL
        GROUP BY k.CustomerID;
    END;

    CREATE TABLE #Filtered
    (
        CustomerID INT NOT NULL PRIMARY KEY,
        ArabicName NVARCHAR(500) COLLATE DATABASE_DEFAULT NULL,
        EnglishName NVARCHAR(500) COLLATE DATABASE_DEFAULT NULL,
        NationalId NVARCHAR(100) COLLATE DATABASE_DEFAULT NULL,
        Phone NVARCHAR(100) COLLATE DATABASE_DEFAULT NULL,
        TokenCardNumber NVARCHAR(255) COLLATE DATABASE_DEFAULT NULL,
        BranchName NVARCHAR(255) COLLATE DATABASE_DEFAULT NULL,
        CreatedDate DATETIME NULL,
        LastUpdateDate DATETIME NULL,
        HasInvoice INT NOT NULL,
        InvoiceCount INT NOT NULL,
        LastInvoiceDate DATETIME NULL,
        HasAttachments INT NOT NULL,
        MissingRequiredData INT NOT NULL,
        IncompleteKyc INT NOT NULL
    );

    INSERT INTO #Filtered
    (
        CustomerID, ArabicName, EnglishName, NationalId, Phone, TokenCardNumber, BranchName,
        CreatedDate, LastUpdateDate, HasInvoice, InvoiceCount, LastInvoiceDate, HasAttachments,
        MissingRequiredData, IncompleteKyc
    )
    SELECT
        k.CustomerID,
        k.ArabicName,
        k.EnglishName,
        k.NationalId,
        k.Phone,
        k.TokenCardNumber,
        k.BranchName,
        k.CreatedDate,
        k.LastUpdateDate,
        CASE WHEN ISNULL(i.InvoiceCount, 0) > 0 THEN 1 ELSE 0 END,
        ISNULL(i.InvoiceCount, 0),
        i.LastInvoiceDate,
        CASE WHEN ISNULL(a.AttachmentCount, 0) > 0 THEN 1 ELSE 0 END,
        k.MissingRequiredData,
        k.IncompleteKyc
    FROM #KycBase k
    LEFT JOIN #InvoiceAgg i ON i.CustomerID = k.CustomerID
    LEFT JOIN #AttachmentAgg a ON a.CustomerID = k.CustomerID
    WHERE (@invoiceStatus = N'all'
        OR (@invoiceStatus = N'with' AND ISNULL(i.InvoiceCount, 0) > 0)
        OR (@invoiceStatus = N'without' AND ISNULL(i.InvoiceCount, 0) = 0))
      AND (@smartFilter = N'' OR @smartFilter = N'all'
        OR (@smartFilter = N'missing-required' AND k.MissingRequiredData = 1)
        OR (@smartFilter = N'incomplete' AND k.IncompleteKyc = 1)
        OR (@smartFilter = N'recent' AND k.CreatedDate >= DATEADD(DAY, -7, GETDATE()))
        OR (@smartFilter = N'updated-unused' AND ISNULL(i.InvoiceCount, 0) = 0 AND k.LastUpdateDate IS NOT NULL)
        OR (@smartFilter = N'has-attachments' AND ISNULL(a.AttachmentCount, 0) > 0)
        OR (@smartFilter = N'no-attachments' AND ISNULL(a.AttachmentCount, 0) = 0)
        OR (@smartFilter = N'bank-misr' AND LEN(LTRIM(RTRIM(ISNULL(k.TokenCardNumber, N'')))) = 18)
        OR (@smartFilter = N'bank-ahly' AND LEN(LTRIM(RTRIM(ISNULL(k.TokenCardNumber, N'')))) = 8));

    CREATE INDEX IX_Filtered_Order ON #Filtered(CreatedDate DESC, CustomerID DESC);

    SELECT
        COUNT(1) AS TotalCustomers,
        ISNULL(SUM(CASE WHEN HasInvoice = 1 THEN 1 ELSE 0 END), 0) AS WithInvoices,
        ISNULL(SUM(CASE WHEN HasInvoice = 0 THEN 1 ELSE 0 END), 0) AS WithoutInvoices,
        ISNULL(SUM(CASE WHEN IncompleteKyc = 1 THEN 1 ELSE 0 END), 0) AS IncompleteCustomers,
        ISNULL(SUM(CASE WHEN MissingRequiredData = 1 THEN 1 ELSE 0 END), 0) AS MissingRequiredData,
        ISNULL(SUM(CASE WHEN HasAttachments = 1 THEN 1 ELSE 0 END), 0) AS WithAttachments
    FROM #Filtered;

    SELECT
        f.CustomerID,
        f.ArabicName,
        f.EnglishName,
        f.NationalId,
        f.Phone,
        f.TokenCardNumber,
        f.BranchName,
        f.CreatedDate,
        f.LastUpdateDate,
        f.HasInvoice,
        f.InvoiceCount,
        f.LastInvoiceDate,
        f.HasAttachments,
        N'' AS KycStatus,
        COUNT(1) OVER() AS TotalRows
    FROM #Filtered f
    ORDER BY f.CreatedDate DESC, f.CustomerID DESC
    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
END;
GO
