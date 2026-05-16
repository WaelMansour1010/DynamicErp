/*
    Kishny POS - Card token item mismatch diagnostics.
    SQL Server 2012 compatible. Read-only script.

    Purpose:
    - Find tokens that moved or were sold on more than one card item.
    - Highlight card sales where Transactions.VisaNumber does not match the
      positive stock item for that token in the sale store.
*/

SET NOCOUNT ON;

DECLARE @FromDate DATETIME;
DECLARE @ToDate DATETIME;
DECLARE @Token NVARCHAR(255);

SET @FromDate = DATEADD(DAY, -30, CONVERT(DATE, GETDATE()));
SET @ToDate = CONVERT(DATE, GETDATE());
SET @Token = NULL; -- Example: N'R9b8477000000000a5'

PRINT N'1) Tokens with movements on more than one item';

;WITH TokenItem AS
(
    SELECT
        Token = LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))),
        d.Item_ID,
        ItemName = COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), d.Item_ID)),
        StoreID = COALESCE(d.StoreID2, t.StoreID),
        StoreName = COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), COALESCE(d.StoreID2, t.StoreID))),
        SignedQty = SUM(ISNULL(d.Quantity, 0) * ISNULL(tt.StockEffect, 0)),
        FirstDate = MIN(t.Transaction_Date),
        LastDate = MAX(t.Transaction_Date),
        LastTransactionId = MAX(t.Transaction_ID)
    FROM dbo.Transaction_Details d WITH (NOLOCK)
    INNER JOIN dbo.Transactions t WITH (NOLOCK)
        ON t.Transaction_ID = d.Transaction_ID
    LEFT JOIN dbo.TransactionTypes tt WITH (NOLOCK)
        ON tt.Transaction_Type = t.Transaction_Type
    LEFT JOIN dbo.TblItems i WITH (NOLOCK)
        ON i.ItemID = d.Item_ID
    LEFT JOIN dbo.TblStore s WITH (NOLOCK)
        ON s.StoreID = COALESCE(d.StoreID2, t.StoreID)
    WHERE NULLIF(LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))), N'') IS NOT NULL
      AND LEN(LTRIM(RTRIM(ISNULL(d.ItemSerial, N'')))) IN (8, 18)
      AND t.Transaction_Date >= @FromDate
      AND t.Transaction_Date < DATEADD(DAY, 1, @ToDate)
      AND (@Token IS NULL OR LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))) = @Token)
      AND ISNULL(tt.StockEffect, 0) <> 0
      AND d.Item_ID IN (1, 19)
    GROUP BY
        LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))),
        d.Item_ID,
        COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), d.Item_ID)),
        COALESCE(d.StoreID2, t.StoreID),
        COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), COALESCE(d.StoreID2, t.StoreID)))
),
MixedTokens AS
(
    SELECT Token
    FROM TokenItem
    GROUP BY Token
    HAVING COUNT(DISTINCT Item_ID) > 1
)
SELECT ti.*
FROM TokenItem ti
INNER JOIN MixedTokens mt ON mt.Token = ti.Token
ORDER BY ti.Token, ti.StoreID, ti.Item_ID;

PRINT N'2) Card sales whose sold item differs from current positive stock item in same store';

;WITH CardSales AS
(
    SELECT
        SaleTransactionId = t.Transaction_ID,
        t.Transaction_Date,
        Token = LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))),
        t.BranchId,
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)),
        StoreID = t.StoreID,
        StoreName = COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), t.StoreID)),
        SaleItemId = d.Item_ID,
        SaleItemName = COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), d.Item_ID)),
        t.NoteSerial1,
        t.CashCustomerName,
        t.CashCustomerPhone,
        t.UserID,
        u.UserName
    FROM dbo.Transactions t WITH (NOLOCK)
    INNER JOIN dbo.Transaction_Details d WITH (NOLOCK)
        ON d.Transaction_ID = t.Transaction_ID
    LEFT JOIN dbo.TblItems i WITH (NOLOCK)
        ON i.ItemID = d.Item_ID
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK)
        ON b.branch_id = t.BranchId
    LEFT JOIN dbo.TblStore s WITH (NOLOCK)
        ON s.StoreID = t.StoreID
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK)
        ON u.UserID = t.UserID
    WHERE t.Transaction_Type = 21
      AND ISNULL(t.IsCancelled, 0) = 0
      AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL
      AND d.Item_ID IN (1, 19)
      AND t.Transaction_Date >= @FromDate
      AND t.Transaction_Date < DATEADD(DAY, 1, @ToDate)
      AND (@Token IS NULL OR LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))) = @Token)
),
PositiveStock AS
(
    SELECT
        Token = LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))),
        StoreID = COALESCE(d.StoreID2, t.StoreID),
        d.Item_ID,
        ItemName = COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), d.Item_ID)),
        Qty = SUM(ISNULL(d.Quantity, 0) * ISNULL(tt.StockEffect, 0))
    FROM dbo.Transaction_Details d WITH (NOLOCK)
    INNER JOIN dbo.Transactions t WITH (NOLOCK)
        ON t.Transaction_ID = d.Transaction_ID
    INNER JOIN dbo.TransactionTypes tt WITH (NOLOCK)
        ON tt.Transaction_Type = t.Transaction_Type
    LEFT JOIN dbo.TblItems i WITH (NOLOCK)
        ON i.ItemID = d.Item_ID
    WHERE NULLIF(LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))), N'') IS NOT NULL
      AND ISNULL(tt.StockEffect, 0) <> 0
      AND (@Token IS NULL OR LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))) = @Token)
      AND d.Item_ID IN (1, 19)
    GROUP BY
        LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))),
        COALESCE(d.StoreID2, t.StoreID),
        d.Item_ID,
        COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), d.Item_ID))
    HAVING SUM(ISNULL(d.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0
)
SELECT
    cs.*,
    StockItemId = ps.Item_ID,
    StockItemName = ps.ItemName,
    StockQty = ps.Qty,
    Problem = CASE
        WHEN ps.Item_ID IS NULL THEN N'No positive stock for token in sale store'
        WHEN ps.Item_ID <> cs.SaleItemId THEN N'Sale item differs from positive stock item'
        ELSE N''
    END
FROM CardSales cs
LEFT JOIN PositiveStock ps
    ON ps.Token = cs.Token
   AND ps.StoreID = cs.StoreID
WHERE ps.Item_ID IS NULL
   OR ps.Item_ID <> cs.SaleItemId
ORDER BY cs.Transaction_Date DESC, cs.SaleTransactionId DESC;
