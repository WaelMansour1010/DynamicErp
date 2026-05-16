IF COL_LENGTH(N'dbo.Transaction_Details', N'ItemSerial') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transaction_Details') AND name = N'IX_POS_KycAvailableCards_Details')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_KycAvailableCards_Details
    ON dbo.Transaction_Details(Transaction_ID, ItemSerial)
    INCLUDE (Item_ID, Quantity);
END
GO

IF COL_LENGTH(N'dbo.Transactions', N'StoreID') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'IX_POS_KycAvailableCards_Transactions')
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_KycAvailableCards_Transactions
    ON dbo.Transactions(StoreID, Transaction_ID, Transaction_Type);
END
GO

IF OBJECT_ID(N'dbo.usp_POS_SearchAvailableKeshniCards', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SearchAvailableKeshniCards;
GO

CREATE PROCEDURE dbo.usp_POS_SearchAvailableKeshniCards
    @StoreId INT,
    @Term NVARCHAR(255) = NULL,
    @Take INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SET @Term = NULLIF(LTRIM(RTRIM(@Term)), N'');
    IF @Take IS NULL OR @Take <= 0 SET @Take = 20;
    IF @Take > 50 SET @Take = 50;

    ;WITH AvailableSerials AS
    (
        SELECT
            LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) AS Token,
            LEN(LTRIM(RTRIM(ISNULL(td.ItemSerial, N'')))) AS CardLength,
            td.Item_ID AS ItemId,
            SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS AvailableQty,
            MAX(t.Transaction_ID) AS LastTransactionId
        FROM dbo.Transactions t WITH (NOLOCK)
        INNER JOIN dbo.Transaction_Details td WITH (NOLOCK)
            ON td.Transaction_ID = t.Transaction_ID
        INNER JOIN dbo.TransactionTypes tt WITH (NOLOCK)
            ON tt.Transaction_Type = t.Transaction_Type
        WHERE t.StoreID = @StoreId
          AND ISNULL(tt.StockEffect, 0) <> 0
          AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) <> N''
          AND LEN(LTRIM(RTRIM(ISNULL(td.ItemSerial, N'')))) IN (8, 18)
          AND (@Term IS NULL OR LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) LIKE @Term + N'%')
        GROUP BY
            LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))),
            LEN(LTRIM(RTRIM(ISNULL(td.ItemSerial, N'')))),
            td.Item_ID
        HAVING SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0
    ),
    Filtered AS
    (
        SELECT TOP (@Take)
            av.Token,
            av.CardLength,
            av.ItemId,
            av.AvailableQty,
            av.LastTransactionId
        FROM AvailableSerials av
        WHERE NOT EXISTS
          (
              SELECT 1
              FROM dbo.TblCusCsh customer WITH (NOLOCK)
              WHERE ISNULL(customer.EasyCashType, 0) = 0
                AND
                (
                    LTRIM(RTRIM(ISNULL(customer.CardNo, N''))) = av.Token
                    OR LTRIM(RTRIM(ISNULL(customer.CardId, N''))) = av.Token
                )
          )
        ORDER BY
            av.CardLength,
            av.LastTransactionId DESC,
            av.Token
    )
    SELECT
        f.Token,
        f.ItemId,
        COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), f.ItemId)) AS ItemName,
        COALESCE(NULLIF(i.Fullcode, N''), NULLIF(i.ItemCode, N''), CONVERT(NVARCHAR(50), f.ItemId)) AS ItemCode,
        CAST(f.AvailableQty AS DECIMAL(18, 4)) AS AvailableQty,
        COALESCE(NULLIF(store.StoreName, N''), NULLIF(store.StoreNamee, N''), CONVERT(NVARCHAR(50), @StoreId)) AS StoreName
    FROM Filtered f
    LEFT JOIN dbo.TblItems i WITH (NOLOCK)
        ON i.ItemID = f.ItemId
    LEFT JOIN dbo.TblStore store WITH (NOLOCK)
        ON store.StoreID = @StoreId
    ORDER BY f.CardLength, f.LastTransactionId DESC, f.Token;
END
GO
