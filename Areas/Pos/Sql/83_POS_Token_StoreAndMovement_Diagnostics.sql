/*
    Kishny POS - Token store and movement diagnostics.
    SQL Server 2012 compatible. Read-only script.

    Purpose:
    - Show where a card/token currently exists by store.
    - Show the token movement path from Transaction_Details.ItemSerial and related
      sales/card references in Transactions.VisaNumber.
*/

SET NOCOUNT ON;

DECLARE @Token NVARCHAR(255);
SET @Token = N'R9b8477000000000a5';

DECLARE @TokenTrimmed NVARCHAR(255);
SET @TokenTrimmed = NULLIF(LTRIM(RTRIM(ISNULL(@Token, N''))), N'');

IF @TokenTrimmed IS NULL
BEGIN
    RAISERROR(N'Token is required.', 16, 1);
    RETURN;
END;

PRINT N'1) KYC/customer records for token';

SELECT
    c.Id,
    c.CardNo,
    c.CardId,
    c.Tet_NumPoket AS NationalId,
    c.name AS ArabicName,
    c.namee AS EnglishName,
    c.PhoneNo2,
    c.BranchID,
    CustomerBranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)),
    c.UserID,
    c.OrderDate,
    c.SaveDate,
    c.EasyCashType
FROM dbo.TblCusCsh c WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK)
    ON b.branch_id = c.BranchID
WHERE LTRIM(RTRIM(ISNULL(c.CardNo, N''))) = @TokenTrimmed
   OR LTRIM(RTRIM(ISNULL(c.CardId, N''))) = @TokenTrimmed
   OR LTRIM(RTRIM(ISNULL(c.card, N''))) = @TokenTrimmed
ORDER BY c.Id DESC;

PRINT N'2) Current stock by store from movement quantities';

;WITH TokenMovements AS
(
    SELECT
        StoreID = COALESCE(d.StoreID2, t.StoreID),
        d.Item_ID,
        MovementQty = CONVERT(DECIMAL(18, 4), ISNULL(d.Quantity, 0) * ISNULL(tt.StockEffect, 0)),
        LastTransactionId = t.Transaction_ID,
        LastTransactionDate = t.Transaction_Date
    FROM dbo.Transaction_Details d WITH (NOLOCK)
    INNER JOIN dbo.Transactions t WITH (NOLOCK)
        ON t.Transaction_ID = d.Transaction_ID
    LEFT JOIN dbo.TransactionTypes tt WITH (NOLOCK)
        ON tt.Transaction_Type = t.Transaction_Type
    WHERE LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))) = @TokenTrimmed
      AND ISNULL(tt.StockEffect, 0) <> 0
)
SELECT
    m.StoreID,
    StoreName = COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), m.StoreID)),
    m.Item_ID,
    ItemName = COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), m.Item_ID)),
    CurrentQty = SUM(m.MovementQty),
    LastTransactionId = MAX(m.LastTransactionId),
    LastTransactionDate = MAX(m.LastTransactionDate)
FROM TokenMovements m
LEFT JOIN dbo.TblStore s WITH (NOLOCK)
    ON s.StoreID = m.StoreID
LEFT JOIN dbo.TblItems i WITH (NOLOCK)
    ON i.ItemID = m.Item_ID
GROUP BY
    m.StoreID,
    COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), m.StoreID)),
    m.Item_ID,
    COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), m.Item_ID))
HAVING SUM(m.MovementQty) <> 0
ORDER BY CurrentQty DESC, LastTransactionDate DESC;

PRINT N'3) Full movement path from Transaction_Details.ItemSerial';

SELECT
    t.Transaction_ID,
    t.Transaction_Date,
    TransactionTypeId = t.Transaction_Type,
    TransactionTypeName = COALESCE(NULLIF(tt.TransactionTypeName, N''), NULLIF(tt.TransactionEnglishName, N''), CONVERT(NVARCHAR(50), t.Transaction_Type)),
    StockEffect = ISNULL(tt.StockEffect, 0),
    t.NoteSerial1,
    t.NoteSerial,
    t.BranchId,
    BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)),
    StoreID = COALESCE(d.StoreID2, t.StoreID),
    StoreName = COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), COALESCE(d.StoreID2, t.StoreID))),
    d.Item_ID,
    ItemName = COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), d.Item_ID)),
    d.ItemSerial,
    Quantity = CONVERT(DECIMAL(18, 4), ISNULL(d.Quantity, 0)),
    SignedQuantity = CONVERT(DECIMAL(18, 4), ISNULL(d.Quantity, 0) * ISNULL(tt.StockEffect, 0)),
    d.Price,
    d.TotalPrice,
    t.VisaNumber,
    t.CashCustomerName,
    t.CashCustomerPhone,
    t.UserID,
    UserName = u.UserName,
    LinkedTransactionId = CASE
        WHEN ISNUMERIC(NULLIF(LTRIM(RTRIM(ISNULL(t.nots, N''))), N'')) = 1
            THEN CONVERT(INT, NULLIF(LTRIM(RTRIM(ISNULL(t.nots, N''))), N''))
        ELSE NULL
    END,
    t.nots,
    t.nots2,
    t.IsCancelled
FROM dbo.Transaction_Details d WITH (NOLOCK)
INNER JOIN dbo.Transactions t WITH (NOLOCK)
    ON t.Transaction_ID = d.Transaction_ID
LEFT JOIN dbo.TransactionTypes tt WITH (NOLOCK)
    ON tt.Transaction_Type = t.Transaction_Type
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK)
    ON b.branch_id = t.BranchId
LEFT JOIN dbo.TblStore s WITH (NOLOCK)
    ON s.StoreID = COALESCE(d.StoreID2, t.StoreID)
LEFT JOIN dbo.TblItems i WITH (NOLOCK)
    ON i.ItemID = d.Item_ID
LEFT JOIN dbo.TblUsers u WITH (NOLOCK)
    ON u.UserID = t.UserID
WHERE LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))) = @TokenTrimmed
ORDER BY t.Transaction_Date, t.Transaction_ID, d.ID;

PRINT N'4) Sales/card invoices that reference token in Transactions.VisaNumber';

SELECT
    t.Transaction_ID,
    t.Transaction_Date,
    t.Transaction_Type,
    TransactionTypeName = COALESCE(NULLIF(tt.TransactionTypeName, N''), NULLIF(tt.TransactionEnglishName, N''), CONVERT(NVARCHAR(50), t.Transaction_Type)),
    t.NoteSerial1,
    t.BranchId,
    BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)),
    t.StoreID,
    StoreName = COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), t.StoreID)),
    t.VisaNumber,
    t.CashCustomerName,
    t.CashCustomerPhone,
    t.PayedValue,
    t.RechargeValue,
    t.NetValue,
    t.UserID,
    UserName = u.UserName,
    t.IsCancelled,
    t.nots,
    t.nots2
FROM dbo.Transactions t WITH (NOLOCK)
LEFT JOIN dbo.TransactionTypes tt WITH (NOLOCK)
    ON tt.Transaction_Type = t.Transaction_Type
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK)
    ON b.branch_id = t.BranchId
LEFT JOIN dbo.TblStore s WITH (NOLOCK)
    ON s.StoreID = t.StoreID
LEFT JOIN dbo.TblUsers u WITH (NOLOCK)
    ON u.UserID = t.UserID
WHERE LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))) = @TokenTrimmed
ORDER BY t.Transaction_Date, t.Transaction_ID;
