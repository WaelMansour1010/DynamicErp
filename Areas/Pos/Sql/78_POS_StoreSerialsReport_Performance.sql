IF OBJECT_ID(N'dbo.Transaction_Details', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'dbo.Transaction_Details', N'U')
         AND name = N'IX_POS_TransactionDetails_StoreSerials_Report'
   )
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TransactionDetails_StoreSerials_Report
    ON dbo.Transaction_Details (StoreID2, ItemSerial, Transaction_ID)
    INCLUDE (Item_ID, Quantity);
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_Report_StoreSerials', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_Report_StoreSerials;
GO

CREATE PROCEDURE dbo.usp_POS_Report_StoreSerials
    @storeId INT = NULL,
    @serialSearch NVARCHAR(255) = N'',
    @branchId INT,
    @userId INT,
    @canChangeDefaults BIT
AS
BEGIN
    SET NOCOUNT ON;

    SET @serialSearch = LTRIM(RTRIM(ISNULL(@serialSearch, N'')));

    IF @storeId IS NULL AND LEN(@serialSearch) < 3
    BEGIN
        SELECT
            CAST(NULL AS NVARCHAR(200)) AS StoreName,
            CAST(NULL AS NVARCHAR(80)) AS ItemCode,
            CAST(NULL AS NVARCHAR(255)) AS ItemName,
            CAST(NULL AS NVARCHAR(100)) AS ItemSerial,
            CAST(NULL AS FLOAT) AS StockBalance,
            CAST(NULL AS INT) AS LastTransactionId,
            CAST(NULL AS DATETIME) AS LastTransactionDate,
            CAST(NULL AS NVARCHAR(50)) AS SerialStatus
        WHERE 1 = 0;

        RETURN;
    END;

    SELECT TOP (1000)
        COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), N'مخزن ' + CONVERT(NVARCHAR(20), td.StoreID2)) AS StoreName,
        i.ItemCode,
        i.ItemName,
        td.ItemSerial,
        SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS StockBalance,
        MAX(t.Transaction_ID) AS LastTransactionId,
        MAX(t.Transaction_Date) AS LastTransactionDate,
        CASE WHEN SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0 THEN N'متاح' ELSE N'غير متاح' END AS SerialStatus
    FROM dbo.Transaction_Details td
    INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
    LEFT JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = t.Transaction_Type
    LEFT JOIN dbo.TblItems i ON i.ItemID = td.Item_ID
    LEFT JOIN dbo.TblStore s ON s.StoreID = td.StoreID2
    WHERE td.ItemSerial IS NOT NULL
      AND td.ItemSerial <> N''
      AND (@storeId IS NULL OR td.StoreID2 = @storeId)
      AND (@branchId <= 0 OR t.BranchId = @branchId)
      AND (@canChangeDefaults = 1 OR t.UserId = @userId)
      AND
      (
          @serialSearch = N''
          OR td.ItemSerial LIKE N'%' + @serialSearch + N'%'
          OR i.ItemName LIKE N'%' + @serialSearch + N'%'
          OR i.ItemCode LIKE N'%' + @serialSearch + N'%'
      )
    GROUP BY
        COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), N'مخزن ' + CONVERT(NVARCHAR(20), td.StoreID2)),
        i.ItemCode,
        i.ItemName,
        td.ItemSerial
    HAVING SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0
    ORDER BY StoreName, i.ItemName, td.ItemSerial;
END;
GO
