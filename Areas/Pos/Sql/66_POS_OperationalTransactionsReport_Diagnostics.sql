/*
    Kishny POS - Operational Transactions Report diagnostics
    Date: 2026-05-13

    Purpose:
    Read-only diagnostics for "تعذر تشغيل التقرير" on PosReports daily transactions
    after adding Service Type and card issue tracking.

    Run on customer MyErp database in SSMS/sqlcmd.
    SQL Server 2012 compatible.

    What it checks:
    1. Current database and required tables/procedure.
    2. dbo.usp_POS_Report_Run parameters expected by the new DLL.
    3. Required columns used by the report query.
    4. Data volume for recent POS transactions.
    5. Service type distribution using the actual Kishny mapping.
    6. Card rows with missing linked issue voucher/KYC/stock-balance problems.
    7. Existing indexes relevant to the report.
    8. A small TOP sample query with timings, to separate SQL errors from timeout.
*/

SET NOCOUNT ON;

DECLARE @DaysBack INT = 7;
DECLARE @FromDate DATETIME = DATEADD(DAY, -@DaysBack, CONVERT(DATE, GETDATE()));
DECLARE @ToDate DATETIME = DATEADD(DAY, 1, CONVERT(DATE, GETDATE()));
DECLARE @BranchId INT = 0;
DECLARE @StoreId INT = NULL;
DECLARE @UserId INT = 0;
DECLARE @ServiceType NVARCHAR(30) = N'card'; -- cash-in, cash-out, card, violations, or NULL
DECLARE @SmokeToDate DATETIME = GETDATE();
DECLARE @StartedAt DATETIME;
DECLARE @EndedAt DATETIME;

PRINT '=== 1) Environment ===';
SELECT
    DB_NAME() AS CurrentDatabase,
    @@SERVERNAME AS ServerName,
    CONVERT(NVARCHAR(30), GETDATE(), 120) AS ServerDateTime,
    @FromDate AS DiagnosticFromDate,
    @ToDate AS DiagnosticToExclusive,
    @ServiceType AS DiagnosticServiceType;

PRINT '=== 2) Required objects ===';
SELECT
    ObjectName,
    ExistsFlag = CASE WHEN OBJECT_ID(ObjectName, ObjectType) IS NULL THEN 0 ELSE 1 END,
    ObjectType
FROM
(
    SELECT N'dbo.usp_POS_Report_Run' AS ObjectName, N'P' AS ObjectType
    UNION ALL SELECT N'dbo.Transactions', N'U'
    UNION ALL SELECT N'dbo.Transaction_Details', N'U'
    UNION ALL SELECT N'dbo.TransactionTypes', N'U'
    UNION ALL SELECT N'dbo.TblBranchesData', N'U'
    UNION ALL SELECT N'dbo.TblStore', N'U'
    UNION ALL SELECT N'dbo.TblUsers', N'U'
    UNION ALL SELECT N'dbo.TblCusCsh', N'U'
) o;

PRINT '=== 3) usp_POS_Report_Run parameters: new DLL requires @serviceType, @storeId, @filterUserId ===';
SELECT
    p.parameter_id,
    p.name,
    TYPE_NAME(p.user_type_id) AS TypeName,
    p.max_length,
    p.has_default_value,
    p.is_output
FROM sys.parameters p
WHERE p.object_id = OBJECT_ID(N'dbo.usp_POS_Report_Run', N'P')
ORDER BY p.parameter_id;

PRINT 'Missing parameters check';
SELECT MissingParameter = v.ParameterName
FROM
(
    SELECT N'@reportKey' AS ParameterName
    UNION ALL SELECT N'@fromDate'
    UNION ALL SELECT N'@toDate'
    UNION ALL SELECT N'@branchId'
    UNION ALL SELECT N'@userId'
    UNION ALL SELECT N'@canChangeDefaults'
    UNION ALL SELECT N'@branchFromId'
    UNION ALL SELECT N'@branchToId'
    UNION ALL SELECT N'@showEmptyBranches'
    UNION ALL SELECT N'@serviceSearch'
    UNION ALL SELECT N'@serviceType'
    UNION ALL SELECT N'@storeId'
    UNION ALL SELECT N'@filterUserId'
) v
WHERE NOT EXISTS
(
    SELECT 1
    FROM sys.parameters p
    WHERE p.object_id = OBJECT_ID(N'dbo.usp_POS_Report_Run', N'P')
      AND p.name = v.ParameterName
);

PRINT '=== 4) Required columns ===';
SELECT
    TableName,
    ColumnName,
    ExistsFlag = CASE WHEN COL_LENGTH(TableName, ColumnName) IS NULL THEN 0 ELSE 1 END
FROM
(
    SELECT N'dbo.Transactions' AS TableName, N'Transaction_ID' AS ColumnName
    UNION ALL SELECT N'dbo.Transactions', N'Transaction_Date'
    UNION ALL SELECT N'dbo.Transactions', N'Transaction_Type'
    UNION ALL SELECT N'dbo.Transactions', N'BranchId'
    UNION ALL SELECT N'dbo.Transactions', N'StoreID'
    UNION ALL SELECT N'dbo.Transactions', N'UserID'
    UNION ALL SELECT N'dbo.Transactions', N'IsCashOut'
    UNION ALL SELECT N'dbo.Transactions', N'IsPOS'
    UNION ALL SELECT N'dbo.Transactions', N'TrafficViolations'
    UNION ALL SELECT N'dbo.Transactions', N'isRecharg'
    UNION ALL SELECT N'dbo.Transactions', N'VisaNumber'
    UNION ALL SELECT N'dbo.Transactions', N'NOTS'
    UNION ALL SELECT N'dbo.Transactions', N'IsCancelled'
    UNION ALL SELECT N'dbo.Transactions', N'NoteSerial1'
    UNION ALL SELECT N'dbo.Transactions', N'CashCustomerName'
    UNION ALL SELECT N'dbo.Transactions', N'CashCustomerPhone'
    UNION ALL SELECT N'dbo.Transactions', N'RechargeValue'
    UNION ALL SELECT N'dbo.Transaction_Details', N'Transaction_ID'
    UNION ALL SELECT N'dbo.Transaction_Details', N'ItemSerial'
    UNION ALL SELECT N'dbo.Transaction_Details', N'Quantity'
    UNION ALL SELECT N'dbo.TransactionTypes', N'Transaction_Type'
    UNION ALL SELECT N'dbo.TransactionTypes', N'StockEffect'
    UNION ALL SELECT N'dbo.TblCusCsh', N'CardNo'
    UNION ALL SELECT N'dbo.TblCusCsh', N'CardId'
    UNION ALL SELECT N'dbo.TblCusCsh', N'EasyCashType'
) c;

PRINT '=== 5) Volume by date, last 14 days, POS transaction_type=21 ===';
SELECT
    CONVERT(DATE, t.Transaction_Date) AS TransactionDate,
    COUNT(1) AS TransactionCount,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1 THEN 1 ELSE 0 END) AS CardCount,
    SUM(CASE WHEN ISNULL(t.IsCashOut, 0) = 1 THEN 1 ELSE 0 END) AS CashOutCount,
    SUM(CASE WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN 1 ELSE 0 END) AS ViolationsCount
FROM dbo.Transactions t WITH (NOLOCK)
WHERE t.Transaction_Type = 21
  AND t.Transaction_Date >= DATEADD(DAY, -14, CONVERT(DATE, GETDATE()))
GROUP BY CONVERT(DATE, t.Transaction_Date)
ORDER BY TransactionDate DESC;

PRINT '=== 6) Service type distribution for diagnostic period ===';
SELECT
    OperationType =
        CASE
            WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1 THEN N'card'
            WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
            ELSE N'cash-in'
        END,
    COUNT(1) AS TransactionCount,
    MIN(t.Transaction_ID) AS MinTransactionId,
    MAX(t.Transaction_ID) AS MaxTransactionId
FROM dbo.Transactions t WITH (NOLOCK)
WHERE t.Transaction_Type = 21
  AND ISNULL(t.IsCancelled, 0) = 0
  AND t.Transaction_Date >= @FromDate
  AND t.Transaction_Date < @ToDate
  AND (@BranchId <= 0 OR t.BranchId = @BranchId)
  AND (@StoreId IS NULL OR t.StoreID = @StoreId)
GROUP BY
    CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1 THEN N'card'
        WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
        ELSE N'cash-in'
    END
ORDER BY TransactionCount DESC;

PRINT '=== 7) Existing indexes on report-critical tables ===';
SELECT
    TableName = OBJECT_NAME(i.object_id),
    i.name AS IndexName,
    i.type_desc,
    i.is_unique,
    IndexColumns = STUFF((
        SELECT N', ' + c.name
        FROM sys.index_columns ic
        INNER JOIN sys.columns c
            ON c.object_id = ic.object_id
           AND c.column_id = ic.column_id
        WHERE ic.object_id = i.object_id
          AND ic.index_id = i.index_id
          AND ic.is_included_column = 0
        ORDER BY ic.key_ordinal, ic.index_column_id
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, N''),
    IncludedColumns = STUFF((
        SELECT N', ' + c.name
        FROM sys.index_columns ic
        INNER JOIN sys.columns c
            ON c.object_id = ic.object_id
           AND c.column_id = ic.column_id
        WHERE ic.object_id = i.object_id
          AND ic.index_id = i.index_id
          AND ic.is_included_column = 1
        ORDER BY ic.index_column_id
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, N'')
FROM sys.indexes i
WHERE i.object_id IN (OBJECT_ID(N'dbo.Transactions'), OBJECT_ID(N'dbo.Transaction_Details'), OBJECT_ID(N'dbo.TransactionTypes'), OBJECT_ID(N'dbo.TblCusCsh'))
  AND i.index_id > 0
ORDER BY TableName, i.name;

PRINT '=== 8) Recommended missing indexes for this report. Review before applying. ===';
SELECT Recommendation = N'
-- Speeds date/service filtering for POS operational report
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N''dbo.Transactions'') AND name = N''IX_POS_Report_Transactions_TypeDateBranchStoreUser'')
    CREATE NONCLUSTERED INDEX IX_POS_Report_Transactions_TypeDateBranchStoreUser
    ON dbo.Transactions (Transaction_Type, Transaction_Date, BranchId, StoreID, UserID)
    INCLUDE (Transaction_ID, IsCancelled, IsCashOut, IsPOS, TrafficViolations, VisaNumber, NOTS, NoteSerial1, CashCustomerName, CashCustomerPhone, RechargeValue, NetValue, VAT, Transaction_NetValue, PayedValue);

-- Speeds token/card stock movement checks
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N''dbo.Transaction_Details'') AND name = N''IX_POS_Report_TransactionDetails_SerialTransaction'')
    CREATE NONCLUSTERED INDEX IX_POS_Report_TransactionDetails_SerialTransaction
    ON dbo.Transaction_Details (ItemSerial, Transaction_ID)
    INCLUDE (Quantity, Item_ID, StoreID2);
';

PRINT '=== 9) Execute stored procedure small-window smoke test ===';
BEGIN TRY
    SET @StartedAt = GETDATE();

    EXEC dbo.usp_POS_Report_Run
        @reportKey = N'daily-trans',
        @fromDate = @FromDate,
        @toDate = @SmokeToDate,
        @branchId = @BranchId,
        @userId = @UserId,
        @canChangeDefaults = 1,
        @branchFromId = NULL,
        @branchToId = NULL,
        @showEmptyBranches = 0,
        @serviceSearch = NULL,
        @serviceType = @ServiceType,
        @storeId = @StoreId,
        @filterUserId = NULL;

    SET @EndedAt = GETDATE();
    SELECT
        SmokeTestStatus = N'OK',
        StartedAt = @StartedAt,
        EndedAt = @EndedAt,
        DurationMs = DATEDIFF(MILLISECOND, @StartedAt, @EndedAt);
END TRY
BEGIN CATCH
    SELECT
        SmokeTestStatus = N'FAILED',
        ErrorNumber = ERROR_NUMBER(),
        ErrorSeverity = ERROR_SEVERITY(),
        ErrorState = ERROR_STATE(),
        ErrorLine = ERROR_LINE(),
        ErrorProcedure = ERROR_PROCEDURE(),
        ErrorMessage = ERROR_MESSAGE();
END CATCH;

PRINT '=== 10) Fast TOP card issue sample query, no stored procedure ===';
BEGIN TRY
    SET @StartedAt = GETDATE();

    ;WITH BaseTransactions AS
    (
        SELECT TOP (100)
            t.Transaction_ID,
            t.NoteSerial1,
            t.Transaction_Date,
            t.BranchId,
            t.StoreID,
            t.UserID,
            NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') AS CardNumber,
            CASE
                WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1 THEN N'card'
                WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
                ELSE N'cash-in'
            END AS OperationType,
            CASE
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N'') IS NOT NULL
                  AND NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N'') NOT LIKE N'%[^0-9]%'
                    THEN CONVERT(INT, NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N''))
                ELSE NULL
            END AS IssueTransactionID
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE t.Transaction_Type = 21
          AND ISNULL(t.IsCancelled, 0) = 0
          AND t.Transaction_Date >= @FromDate
          AND t.Transaction_Date < @ToDate
          AND (@BranchId <= 0 OR t.BranchId = @BranchId)
          AND (@StoreId IS NULL OR t.StoreID = @StoreId)
          AND
          (
              @ServiceType IS NULL
              OR @ServiceType = N''
              OR
              CASE
                  WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                  WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.IsPOS, 0) = 1 THEN N'card'
                  WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
                  ELSE N'cash-in'
              END = @ServiceType
          )
        ORDER BY t.Transaction_ID DESC
    )
    SELECT
        bt.Transaction_ID,
        bt.NoteSerial1,
        bt.Transaction_Date,
        bt.OperationType,
        bt.CardNumber,
        bt.IssueTransactionID,
        ISNULL(card.StockBefore, 0) AS StockBefore,
        ISNULL(card.IssueQty, 0) AS IssueQty,
        ISNULL(card.StockBefore, 0) - ISNULL(card.IssueQty, 0) AS StockAfter,
        ISNULL(card.DuplicateInvoiceCount, 0) AS DuplicateInvoiceCount,
        ISNULL(card.HasKycCustomer, 0) AS HasKycCustomer,
        CASE
            WHEN bt.OperationType <> N'card' THEN N''
            WHEN bt.CardNumber IS NULL THEN N'Problematic Card'
            WHEN ISNULL(card.DuplicateInvoiceCount, 0) > 1 THEN N'Problematic Card'
            WHEN ISNULL(card.HasKycCustomer, 0) = 0 THEN N'Problematic Card'
            WHEN ISNULL(card.IssueQty, 0) <= 0 THEN N'Problematic Card'
            WHEN ISNULL(card.StockBefore, 0) <= 0 THEN N'Insufficient Balance'
            WHEN ISNULL(card.StockBefore, 0) - ISNULL(card.IssueQty, 0) < 0 THEN N'Negative'
            ELSE N'Normal'
        END AS CardIssueStatus
    FROM BaseTransactions bt
    OUTER APPLY
    (
        SELECT
            ISNULL((
                SELECT SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0))
                FROM dbo.Transaction_Details td WITH (NOLOCK)
                INNER JOIN dbo.Transactions mt WITH (NOLOCK)
                    ON mt.Transaction_ID = td.Transaction_ID
                INNER JOIN dbo.TransactionTypes tt WITH (NOLOCK)
                    ON tt.Transaction_Type = mt.Transaction_Type
                WHERE bt.OperationType = N'card'
                  AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = bt.CardNumber
                  AND ISNULL(tt.StockEffect, 0) <> 0
                  AND mt.Transaction_ID < bt.Transaction_ID
            ), 0) AS StockBefore,
            ISNULL((
                SELECT SUM(ISNULL(td.Quantity, 0))
                FROM dbo.Transaction_Details td WITH (NOLOCK)
                WHERE bt.OperationType = N'card'
                  AND td.Transaction_ID = bt.IssueTransactionID
                  AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = bt.CardNumber
            ), 0) AS IssueQty,
            (
                SELECT COUNT(1)
                FROM dbo.Transactions dup WITH (NOLOCK)
                WHERE bt.OperationType = N'card'
                  AND dup.Transaction_Type = 21
                  AND ISNULL(dup.IsCancelled, 0) = 0
                  AND NULLIF(LTRIM(RTRIM(ISNULL(dup.VisaNumber, N''))), N'') = bt.CardNumber
            ) AS DuplicateInvoiceCount,
            CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.TblCusCsh c WITH (NOLOCK)
                WHERE bt.OperationType = N'card'
                  AND ISNULL(c.EasyCashType, 0) = 0
                  AND (LTRIM(RTRIM(ISNULL(c.CardNo, N''))) = bt.CardNumber OR LTRIM(RTRIM(ISNULL(c.CardId, N''))) = bt.CardNumber)
            ) THEN 1 ELSE 0 END AS HasKycCustomer
    ) card
    ORDER BY bt.Transaction_ID DESC;

    SET @EndedAt = GETDATE();
    SELECT
        TopSampleStatus = N'OK',
        StartedAt = @StartedAt,
        EndedAt = @EndedAt,
        DurationMs = DATEDIFF(MILLISECOND, @StartedAt, @EndedAt);
END TRY
BEGIN CATCH
    SELECT
        TopSampleStatus = N'FAILED',
        ErrorNumber = ERROR_NUMBER(),
        ErrorSeverity = ERROR_SEVERITY(),
        ErrorState = ERROR_STATE(),
        ErrorLine = ERROR_LINE(),
        ErrorProcedure = ERROR_PROCEDURE(),
        ErrorMessage = ERROR_MESSAGE();
END CATCH;
