/*
    Kishny POS - Sales invoice and KYC search performance.
    SQL Server 2012 compatible.
    Invoice side-list reads use NOLOCK because this endpoint is display-only and
    must not deadlock with dbo.usp_POS_SaveTransaction during teller traffic.
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
   AND COL_LENGTH(N'dbo.Transactions', N'NoteSerial1') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'IX_POS_Transactions_Search_NoteSerial1')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transactions_Search_NoteSerial1
    ON dbo.Transactions(NoteSerial1, Transaction_Date, Transaction_ID)
    INCLUDE (BranchId, UserID, CashCustomerName, CashCustomerPhone, Phone2, VisaNumber, IPN, ManualNO, PayedValue, RechargeValue, VAT, NetValue, TrafficViolations, IsCashOut, IsCancelled)
    WHERE Transaction_Type = 21 AND NoteSerial1 IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'VisaNumber') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'IX_POS_Transactions_Search_VisaNumber')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transactions_Search_VisaNumber
    ON dbo.Transactions(VisaNumber, Transaction_Date, Transaction_ID)
    INCLUDE (BranchId, UserID, NoteSerial1, CashCustomerName, CashCustomerPhone, Phone2, IPN, ManualNO, PayedValue, RechargeValue, VAT, NetValue, TrafficViolations, IsCashOut, IsCancelled)
    WHERE Transaction_Type = 21 AND VisaNumber IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Transactions', N'ManualNO') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'IX_POS_Transactions_Search_ManualNO')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transactions_Search_ManualNO
    ON dbo.Transactions(ManualNO, Transaction_Date, Transaction_ID)
    INCLUDE (BranchId, UserID, NoteSerial1, CashCustomerName, CashCustomerPhone, Phone2, VisaNumber, IPN, PayedValue, RechargeValue, VAT, NetValue, TrafficViolations, IsCashOut, IsCancelled)
    WHERE Transaction_Type = 21 AND ManualNO IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.TblCusCsh', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblCusCsh', N'PhoneNo2') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TblCusCsh') AND name = N'IX_POS_TblCusCsh_KycSearch_PhoneNo2')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TblCusCsh_KycSearch_PhoneNo2
    ON dbo.TblCusCsh(EasyCashType, PhoneNo2, BranchID, Id);
END;
GO

IF OBJECT_ID(N'dbo.TblCusCsh', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblCusCsh', N'CardNo') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TblCusCsh') AND name = N'IX_POS_TblCusCsh_KycSearch_CardNo')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TblCusCsh_KycSearch_CardNo
    ON dbo.TblCusCsh(EasyCashType, CardNo, BranchID, Id);
END;
GO

IF OBJECT_ID(N'dbo.TblCusCsh', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblCusCsh', N'CardId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TblCusCsh') AND name = N'IX_POS_TblCusCsh_KycSearch_CardId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TblCusCsh_KycSearch_CardId
    ON dbo.TblCusCsh(EasyCashType, CardId, BranchID, Id);
END;
GO

IF OBJECT_ID(N'dbo.TblCusCsh', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblCusCsh', N'Tet_NumPoket') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TblCusCsh') AND name = N'IX_POS_TblCusCsh_KycSearch_National')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TblCusCsh_KycSearch_National
    ON dbo.TblCusCsh(EasyCashType, Tet_NumPoket, BranchID, Id);
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_SalesInvoices_Search', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SalesInvoices_Search;
GO

CREATE PROCEDURE dbo.usp_POS_SalesInvoices_Search
    @userId INT,
    @branchId INT = NULL,
    @filterBranchId INT = NULL,
    @canSeeAllBranches BIT = 0,
    @canSeeAllUsers BIT = 0,
    @fromDate DATETIME,
    @toDate DATETIME,
    @term NVARCHAR(100) = NULL,
    @operationType NVARCHAR(30) = NULL,
    @excelOnly BIT = 0,
    @excelWithWarnings BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @from DATETIME = CONVERT(DATE, @fromDate);
    DECLARE @toExclusive DATETIME = DATEADD(DAY, 1, CONVERT(DATE, @toDate));
    DECLARE @search NVARCHAR(100) = NULLIF(LTRIM(RTRIM(ISNULL(@term, N''))), N'');
    DECLARE @like NVARCHAR(110) = CASE WHEN @search IS NULL THEN NULL ELSE N'%' + @search + N'%' END;

    SET @operationType = LOWER(LTRIM(RTRIM(ISNULL(@operationType, N''))));
    IF @operationType NOT IN (N'cash-in', N'cash-out', N'card', N'violations')
        SET @operationType = N'';

    IF @toExclusive < @from
    BEGIN
        DECLARE @swap DATETIME = @from;
        SET @from = DATEADD(DAY, -1, @toExclusive);
        SET @toExclusive = DATEADD(DAY, 1, @swap);
    END;

    IF @search IS NOT NULL
    BEGIN
        CREATE TABLE #CandidateTransactions
        (
            Transaction_ID INT NOT NULL PRIMARY KEY
        );

        INSERT INTO #CandidateTransactions (Transaction_ID)
        SELECT TOP (75) t.Transaction_ID
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND t.NoteSerial1 = @search
          AND (@filterBranchId IS NULL OR t.BranchId = @filterBranchId)
          AND (@canSeeAllBranches = 1 OR ((@branchId IS NULL OR t.BranchId = @branchId) AND (@canSeeAllUsers = 1 OR t.UserID = @userId)))
        ORDER BY t.Transaction_ID DESC;

        INSERT INTO #CandidateTransactions (Transaction_ID)
        SELECT TOP (75) t.Transaction_ID
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND t.VisaNumber = @search
          AND (@filterBranchId IS NULL OR t.BranchId = @filterBranchId)
          AND (@canSeeAllBranches = 1 OR ((@branchId IS NULL OR t.BranchId = @branchId) AND (@canSeeAllUsers = 1 OR t.UserID = @userId)))
          AND NOT EXISTS (SELECT 1 FROM #CandidateTransactions c WHERE c.Transaction_ID = t.Transaction_ID)
        ORDER BY t.Transaction_ID DESC;

        INSERT INTO #CandidateTransactions (Transaction_ID)
        SELECT TOP (75) t.Transaction_ID
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND t.ManualNO = @search
          AND (@filterBranchId IS NULL OR t.BranchId = @filterBranchId)
          AND (@canSeeAllBranches = 1 OR ((@branchId IS NULL OR t.BranchId = @branchId) AND (@canSeeAllUsers = 1 OR t.UserID = @userId)))
          AND NOT EXISTS (SELECT 1 FROM #CandidateTransactions c WHERE c.Transaction_ID = t.Transaction_ID)
        ORDER BY t.Transaction_ID DESC;

        IF (SELECT COUNT(1) FROM #CandidateTransactions) = 0 OR @search LIKE N'%[^0-9]%'
        BEGIN
            INSERT INTO #CandidateTransactions (Transaction_ID)
            SELECT TOP (75) t.Transaction_ID
            FROM dbo.Transactions t WITH (NOLOCK)
            WHERE t.Transaction_Type = 21
              AND t.Transaction_Date >= @from
              AND t.Transaction_Date < @toExclusive
              AND
              (
                  t.IPN LIKE @search + N'%'
                  OR t.CashCustomerPhone LIKE @search + N'%'
                  OR t.Phone2 LIKE @search + N'%'
                  OR ISNULL(t.CashCustomerName, N'') LIKE @like
                  OR (ISNUMERIC(@search) = 1 AND CONVERT(NVARCHAR(50), t.Transaction_ID) = @search)
              )
              AND (@filterBranchId IS NULL OR t.BranchId = @filterBranchId)
              AND (@canSeeAllBranches = 1 OR ((@branchId IS NULL OR t.BranchId = @branchId) AND (@canSeeAllUsers = 1 OR t.UserID = @userId)))
              AND NOT EXISTS (SELECT 1 FROM #CandidateTransactions c WHERE c.Transaction_ID = t.Transaction_ID)
            ORDER BY t.Transaction_ID DESC;
        END;

        IF (SELECT COUNT(1) FROM #CandidateTransactions) = 0
        BEGIN
            INSERT INTO #CandidateTransactions (Transaction_ID)
            SELECT TOP (50) t.Transaction_ID
            FROM dbo.Transactions t WITH (NOLOCK)
            WHERE t.Transaction_Type = 21
              AND t.Transaction_Date >= @from
              AND t.Transaction_Date < @toExclusive
              AND (@filterBranchId IS NULL OR t.BranchId = @filterBranchId)
              AND (@canSeeAllBranches = 1 OR ((@branchId IS NULL OR t.BranchId = @branchId) AND (@canSeeAllUsers = 1 OR t.UserID = @userId)))
              AND EXISTS
              (
                  SELECT 1
                   FROM dbo.Transaction_Details d WITH (NOLOCK)
                  WHERE d.Transaction_ID = t.Transaction_ID
                    AND d.ItemSerial LIKE @search + N'%'
              )
              AND NOT EXISTS (SELECT 1 FROM #CandidateTransactions c WHERE c.Transaction_ID = t.Transaction_ID)
            ORDER BY t.Transaction_ID DESC;
        END;

        ;WITH Base AS
        (
            SELECT TOP (50)
                t.Transaction_ID,
                t.NoteSerial1,
                t.Transaction_Date,
                t.BranchId,
                t.UserID,
                t.CashCustomerName,
                t.CashCustomerPhone,
                t.Phone2,
                t.VisaNumber,
                t.IPN,
                t.ManualNO,
                t.PayedValue,
                t.RechargeValue,
                t.VAT,
                t.NetValue,
                t.TrafficViolations,
                t.IsCashOut,
                t.IsCancelled
            FROM #CandidateTransactions c
            INNER JOIN dbo.Transactions t WITH (NOLOCK) ON t.Transaction_ID = c.Transaction_ID
            WHERE
              (
                  @operationType = N''
                  OR (@operationType = N'cash-in' AND ISNULL(t.TrafficViolations, 0) = 0 AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NULL AND ISNULL(t.IsCashOut, 0) = 0)
                  OR (@operationType = N'cash-out' AND ISNULL(t.IsCashOut, 0) = 1)
                  OR (@operationType = N'card' AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL)
                  OR (@operationType = N'violations' AND ISNULL(t.TrafficViolations, 0) = 1)
              )
              AND
              (
                  @excelOnly = 0
                  OR EXISTS
                  (
                      SELECT 1
                       FROM dbo.POS_ImportBatchRow er WITH (NOLOCK)
                      WHERE er.TransactionId = t.Transaction_ID
                        AND er.Status = N'Imported'
                  )
              )
              AND
              (
                  @excelWithWarnings = 0
                  OR EXISTS
                  (
                      SELECT 1
                       FROM dbo.POS_ImportBatchRow wr WITH (NOLOCK)
                      WHERE wr.TransactionId = t.Transaction_ID
                        AND wr.Status = N'Imported'
                        AND CHARINDEX(N'[ExcelImportWarning]', ISNULL(wr.Message, N'')) > 0
                  )
              )
            ORDER BY t.Transaction_ID DESC
        )
        SELECT
            b.Transaction_ID,
            b.NoteSerial1,
            CONVERT(VARCHAR(10), b.Transaction_Date, 120) AS TransactionDate,
            CONVERT(VARCHAR(5), b.Transaction_Date, 108) AS TransactionTime,
            b.CashCustomerName,
            b.CashCustomerPhone,
            CAST(ISNULL(b.PayedValue, 0) AS DECIMAL(18, 2)) AS PayedValue,
            CAST(CASE
                WHEN ISNULL(b.TrafficViolations, 0) = 1 THEN ISNULL(b.PayedValue, 0)
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(b.VisaNumber, N''))), N'') IS NOT NULL THEN ISNULL(b.PayedValue, 0)
                ELSE ISNULL(b.RechargeValue, 0) + ISNULL(b.VAT, 0) + ISNULL(b.NetValue, 0)
            END AS DECIMAL(18, 2)) AS NetValue,
            CASE
                WHEN ISNULL(b.TrafficViolations, 0) = 1 THEN N'مخالفات'
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(b.VisaNumber, N''))), N'') IS NOT NULL THEN N'كارت كيشني'
                WHEN ISNULL(b.IsCashOut, 0) = 1 THEN N'كاش أوت'
                ELSE N'كاش إن'
            END AS ServiceType,
            CASE WHEN excelRow.RowId IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsExcelImported,
            excelRow.BatchId AS ExcelImportBatchId,
            CAST(CASE WHEN CHARINDEX(N'[ExcelImportWarning]', ISNULL(excelRow.Message, N'')) > 0 THEN 1 ELSE 0 END AS BIT) AS HasExcelImportWarning,
            REPLACE(ISNULL(excelRow.Message, N''), N'[ExcelImportWarning] ', N'') AS ExcelImportWarningMessage,
            CAST(ISNULL(b.IsCancelled, 0) AS BIT) AS IsCancelled
        FROM Base b
        OUTER APPLY
        (
            SELECT TOP (1) r.RowId, r.BatchId, r.Message
            FROM dbo.POS_ImportBatchRow r WITH (NOLOCK)
            WHERE r.TransactionId = b.Transaction_ID
              AND r.Status = N'Imported'
            ORDER BY r.RowId DESC
        ) excelRow
        ORDER BY b.Transaction_ID DESC
        OPTION (RECOMPILE);

        RETURN;
    END;

    ;WITH Base AS
    (
        SELECT TOP (50)
            t.Transaction_ID,
            t.NoteSerial1,
            t.Transaction_Date,
            t.BranchId,
            t.UserID,
            t.CashCustomerName,
            t.CashCustomerPhone,
            t.Phone2,
            t.VisaNumber,
            t.IPN,
            t.ManualNO,
            t.PayedValue,
            t.RechargeValue,
            t.VAT,
            t.NetValue,
            t.TrafficViolations,
            t.IsCashOut,
            t.IsCancelled
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @from
          AND t.Transaction_Date < @toExclusive
          AND (@filterBranchId IS NULL OR t.BranchId = @filterBranchId)
          AND
          (
              @canSeeAllBranches = 1
              OR
              (
                  (@branchId IS NULL OR t.BranchId = @branchId)
                  AND (@canSeeAllUsers = 1 OR t.UserID = @userId)
              )
          )
          AND
          (
              @operationType = N''
              OR (@operationType = N'cash-in' AND ISNULL(t.TrafficViolations, 0) = 0 AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NULL AND ISNULL(t.IsCashOut, 0) = 0)
              OR (@operationType = N'cash-out' AND ISNULL(t.IsCashOut, 0) = 1)
              OR (@operationType = N'card' AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL)
              OR (@operationType = N'violations' AND ISNULL(t.TrafficViolations, 0) = 1)
          )
          AND
          (
              @search IS NULL
              OR CONVERT(NVARCHAR(50), t.Transaction_ID) = @search
              OR ISNULL(t.NoteSerial1, N'') = @search
              OR ISNULL(t.VisaNumber, N'') = @search
              OR ISNULL(t.ManualNO, N'') = @search
              OR ISNULL(t.IPN, N'') = @search
              OR ISNULL(t.CashCustomerPhone, N'') = @search
              OR ISNULL(t.Phone2, N'') = @search
              OR ISNULL(t.NoteSerial1, N'') LIKE @like
              OR ISNULL(t.VisaNumber, N'') LIKE @like
              OR ISNULL(t.ManualNO, N'') LIKE @like
              OR ISNULL(t.IPN, N'') LIKE @like
              OR ISNULL(t.CashCustomerPhone, N'') LIKE @like
              OR ISNULL(t.Phone2, N'') LIKE @like
              OR ISNULL(t.CashCustomerName, N'') LIKE @like
              OR EXISTS
              (
                  SELECT 1
                   FROM dbo.Transaction_Details d WITH (NOLOCK)
                  WHERE d.Transaction_ID = t.Transaction_ID
                    AND ISNULL(d.ItemSerial, N'') LIKE @like
              )
          )
          AND
          (
              @excelOnly = 0
              OR EXISTS
              (
                  SELECT 1
                   FROM dbo.POS_ImportBatchRow er WITH (NOLOCK)
                  WHERE er.TransactionId = t.Transaction_ID
                    AND er.Status = N'Imported'
              )
          )
          AND
          (
              @excelWithWarnings = 0
              OR EXISTS
              (
                  SELECT 1
                   FROM dbo.POS_ImportBatchRow wr WITH (NOLOCK)
                  WHERE wr.TransactionId = t.Transaction_ID
                    AND wr.Status = N'Imported'
                    AND CHARINDEX(N'[ExcelImportWarning]', ISNULL(wr.Message, N'')) > 0
              )
          )
        ORDER BY t.Transaction_ID DESC
    )
    SELECT
        b.Transaction_ID,
        b.NoteSerial1,
        CONVERT(VARCHAR(10), b.Transaction_Date, 120) AS TransactionDate,
        CONVERT(VARCHAR(5), b.Transaction_Date, 108) AS TransactionTime,
        b.CashCustomerName,
        b.CashCustomerPhone,
        CAST(ISNULL(b.PayedValue, 0) AS DECIMAL(18, 2)) AS PayedValue,
        CAST(CASE
            WHEN ISNULL(b.TrafficViolations, 0) = 1 THEN ISNULL(b.PayedValue, 0)
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(b.VisaNumber, N''))), N'') IS NOT NULL THEN ISNULL(b.PayedValue, 0)
            ELSE ISNULL(b.RechargeValue, 0) + ISNULL(b.VAT, 0) + ISNULL(b.NetValue, 0)
        END AS DECIMAL(18, 2)) AS NetValue,
        CASE
            WHEN ISNULL(b.TrafficViolations, 0) = 1 THEN N'مخالفات'
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(b.VisaNumber, N''))), N'') IS NOT NULL THEN N'كارت كيشني'
            WHEN ISNULL(b.IsCashOut, 0) = 1 THEN N'كاش أوت'
            ELSE N'كاش إن'
        END AS ServiceType,
        CASE WHEN excelRow.RowId IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsExcelImported,
        excelRow.BatchId AS ExcelImportBatchId,
        CAST(CASE WHEN CHARINDEX(N'[ExcelImportWarning]', ISNULL(excelRow.Message, N'')) > 0 THEN 1 ELSE 0 END AS BIT) AS HasExcelImportWarning,
        REPLACE(ISNULL(excelRow.Message, N''), N'[ExcelImportWarning] ', N'') AS ExcelImportWarningMessage,
        CAST(ISNULL(b.IsCancelled, 0) AS BIT) AS IsCancelled
    FROM Base b
    OUTER APPLY
    (
        SELECT TOP (1) r.RowId, r.BatchId, r.Message
        FROM dbo.POS_ImportBatchRow r WITH (NOLOCK)
        WHERE r.TransactionId = b.Transaction_ID
          AND r.Status = N'Imported'
        ORDER BY r.RowId DESC
    ) excelRow
    ORDER BY b.Transaction_ID DESC
    OPTION (RECOMPILE);
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_KycCustomers_Search', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_KycCustomers_Search;
GO

CREATE PROCEDURE dbo.usp_POS_KycCustomers_Search
    @term NVARCHAR(255),
    @branchId INT = NULL,
    @canChangeDefaults BIT = 0,
    @unusedOnly BIT = 0,
    @maxRows INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @search NVARCHAR(255) = LTRIM(RTRIM(ISNULL(@term, N'')));
    DECLARE @like NVARCHAR(270) = N'%' + @search + N'%';
    DECLARE @isBroadNameSearch BIT = CASE WHEN LEN(@search) >= 3 AND @search LIKE N'%[^0-9]%' THEN 1 ELSE 0 END;

    IF @maxRows IS NULL OR @maxRows <= 0 SET @maxRows = 20;
    IF @maxRows > 50 SET @maxRows = 50;

    ;WITH Matches AS
    (
        SELECT TOP (@maxRows)
            c.Id,
            c.name,
            c.namee,
            c.ArabicName0,
            c.ArabicName1,
            c.ArabicName2,
            c.ArabicName3,
            c.EnglishName0,
            c.EnglishName1,
            c.EnglishName2,
            c.EnglishName3,
            c.EnglishName5,
            c.EnglishName6,
            c.EnglishName7,
            COALESCE(NULLIF(c.name, N''), NULLIF(c.CustName, N'')) AS CustomerName,
            COALESCE(NULLIF(c.PhoneNo2, N''), NULLIF(c.PhoneNo, N''), NULLIF(c.tel, N'')) AS Phone,
            c.PhoneNo2,
            c.CardId,
            c.CardNo,
            c.card,
            c.CardSource,
            c.tel,
            c.Tet_NumPoket,
            c.Address,
            c.MailAdress,
            c.Nationality,
            c.BirthDate,
            c.CardDate,
            c.CardEndDate,
            c.OrderDate,
            c.EasyCashType,
            c.BranchID,
            COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)) AS BranchName,
            COALESCE(c.SaveDate, c.RecordDate, c.OrderDate) AS CreatedDate
        FROM dbo.TblCusCsh c
        LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
        WHERE ISNULL(c.EasyCashType, 0) = 0
          AND (@branchId IS NULL OR @canChangeDefaults = 1 OR c.BranchID = @branchId)
          AND
          (
              c.PhoneNo2 = @search
              OR c.PhoneNo = @search
              OR c.tel = @search
              OR c.CardNo = @search
              OR c.CardId = @search
              OR c.Tet_NumPoket = @search
              OR c.PhoneNo2 LIKE @search + N'%'
              OR c.PhoneNo LIKE @search + N'%'
              OR c.tel LIKE @search + N'%'
              OR c.CardNo LIKE @search + N'%'
              OR c.CardId LIKE @search + N'%'
              OR c.Tet_NumPoket LIKE @search + N'%'
              OR (@isBroadNameSearch = 1 AND (c.name LIKE @like OR c.CustName LIKE @like OR c.namee LIKE @like OR c.ArabicName0 LIKE @like OR c.ArabicName1 LIKE @like OR c.ArabicName2 LIKE @like OR c.ArabicName3 LIKE @like OR c.EnglishName0 LIKE @like OR c.EnglishName1 LIKE @like OR c.EnglishName2 LIKE @like OR c.EnglishName3 LIKE @like))
          )
          AND
          (
              @unusedOnly = 0
              OR NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.Transactions t
                  WHERE t.Transaction_Type = 21
                    AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL
                    AND (t.VisaNumber = c.CardNo OR t.VisaNumber = c.CardId)
              )
          )
        ORDER BY c.Id DESC
    )
    SELECT *
    FROM Matches
    ORDER BY Id DESC
    OPTION (RECOMPILE);
END;
GO
