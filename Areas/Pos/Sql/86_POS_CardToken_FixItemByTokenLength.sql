/*
    Kishny POS - cautiously fix card item by token length.
    SQL Server 2012 compatible.

    Purpose:
    - Correct Transaction_Details.Item_ID for Kishny card token rows only.
    - 18-character token => Item_ID 1  (Bank Misr / Meeza Kishny)
    - 8-character token  => Item_ID 19 (National Bank of Egypt)

    Safety:
    - Preview mode by default: @Execute = 0.
    - Restricts updates to existing card items only: Item_ID IN (1, 19).
    - Restricts to non-cancelled POS card sale/issue voucher rows by default.
    - Does not change prices, totals, invoices, KYC rows, or customer data.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Execute BIT;
DECLARE @FromDate DATE;
DECLARE @ToDate DATE;
DECLARE @Token NVARCHAR(255);
DECLARE @StoreId INT;
DECLARE @OnlyCardSaleAndIssue BIT;

SET @Execute = 0; -- 0 = preview only, 1 = apply update
SET @FromDate = '2026-05-01';
SET @ToDate = CONVERT(DATE, GETDATE());
SET @Token = NULL; -- Example: N'R9b8477000000000a5'
SET @StoreId = NULL; -- Example: 76
SET @OnlyCardSaleAndIssue = 1; -- 1 = only sale invoice/issue voucher rows, 0 = all stock movements with card item

IF OBJECT_ID('tempdb..#CardItemFixCandidates') IS NOT NULL
    DROP TABLE #CardItemFixCandidates;

;WITH Candidates AS
(
    SELECT
        t.Transaction_ID,
        t.Transaction_Date,
        t.Transaction_Type,
        TransactionTypeName = COALESCE(NULLIF(tt.TransactionTypeName, N''), CONVERT(NVARCHAR(50), t.Transaction_Type)),
        t.BranchId,
        BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)),
        EffectiveStoreID = COALESCE(d.StoreID2, t.StoreID),
        StoreName = COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), COALESCE(d.StoreID2, t.StoreID))),
        d.ID,
        d.Item_ID AS OldItemId,
        OldItemName = COALESCE(NULLIF(oldItem.ItemName, N''), NULLIF(oldItem.ItemNamee, N''), oldItem.ItemCode, CONVERT(NVARCHAR(50), d.Item_ID)),
        NewItemId =
            CASE
                WHEN LEN(LTRIM(RTRIM(ISNULL(d.ItemSerial, N'')))) = 18 THEN 1
                WHEN LEN(LTRIM(RTRIM(ISNULL(d.ItemSerial, N'')))) = 8 THEN 19
                ELSE NULL
            END,
        Token = LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))),
        TokenLength = LEN(LTRIM(RTRIM(ISNULL(d.ItemSerial, N'')))),
        d.Quantity,
        SignedQuantity = ISNULL(d.Quantity, 0) * ISNULL(tt.StockEffect, 0),
        t.NoteSerial1,
        t.VisaNumber,
        t.CashCustomerName,
        t.CashCustomerPhone,
        t.UserID,
        UserName = u.UserName
    FROM dbo.Transaction_Details d WITH (NOLOCK)
    INNER JOIN dbo.Transactions t WITH (NOLOCK)
        ON t.Transaction_ID = d.Transaction_ID
    LEFT JOIN dbo.TransactionTypes tt WITH (NOLOCK)
        ON tt.Transaction_Type = t.Transaction_Type
    LEFT JOIN dbo.TblItems oldItem WITH (NOLOCK)
        ON oldItem.ItemID = d.Item_ID
    LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK)
        ON b.branch_id = t.BranchId
    LEFT JOIN dbo.TblStore s WITH (NOLOCK)
        ON s.StoreID = COALESCE(d.StoreID2, t.StoreID)
    LEFT JOIN dbo.TblUsers u WITH (NOLOCK)
        ON u.UserID = t.UserID
    WHERE ISNULL(t.IsCancelled, 0) = 0
      AND d.Item_ID IN (1, 19)
      AND NULLIF(LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))), N'') IS NOT NULL
      AND LEN(LTRIM(RTRIM(ISNULL(d.ItemSerial, N'')))) IN (8, 18)
      AND t.Transaction_Date >= @FromDate
      AND t.Transaction_Date < DATEADD(DAY, 1, @ToDate)
      AND (@Token IS NULL OR LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))) = @Token)
      AND (@StoreId IS NULL OR COALESCE(d.StoreID2, t.StoreID) = @StoreId)
      AND (@OnlyCardSaleAndIssue = 0 OR t.Transaction_Type IN (19, 21))
)
SELECT
    c.*,
    NewItemName = COALESCE(NULLIF(newItem.ItemName, N''), NULLIF(newItem.ItemNamee, N''), newItem.ItemCode, CONVERT(NVARCHAR(50), c.NewItemId))
INTO #CardItemFixCandidates
FROM Candidates c
LEFT JOIN dbo.TblItems newItem WITH (NOLOCK)
    ON newItem.ItemID = c.NewItemId
WHERE c.NewItemId IS NOT NULL
  AND c.OldItemId <> c.NewItemId;

PRINT N'1) Preview rows that will be corrected';

SELECT
    Transaction_ID,
    Transaction_Date,
    Transaction_Type,
    TransactionTypeName,
    BranchId,
    BranchName,
    StoreID = EffectiveStoreID,
    StoreName,
    DetailID = ID,
    Token,
    TokenLength,
    OldItemId,
    OldItemName,
    NewItemId,
    NewItemName,
    Quantity,
    SignedQuantity,
    NoteSerial1,
    VisaNumber,
    CashCustomerName,
    CashCustomerPhone,
    UserID,
    UserName
FROM #CardItemFixCandidates
ORDER BY Transaction_Date, Transaction_ID, ID;

PRINT N'2) Summary by old/new item';

SELECT
    TokenLength,
    OldItemId,
    OldItemName,
    NewItemId,
    NewItemName,
    RowsCount = COUNT(1),
    TotalQuantity = SUM(ISNULL(Quantity, 0)),
    TotalSignedQuantity = SUM(ISNULL(SignedQuantity, 0))
FROM #CardItemFixCandidates
GROUP BY TokenLength, OldItemId, OldItemName, NewItemId, NewItemName
ORDER BY TokenLength, OldItemId, NewItemId;

DECLARE @RowsToFix INT;
SELECT @RowsToFix = COUNT(1) FROM #CardItemFixCandidates;

PRINT N'Rows to fix: ' + CONVERT(NVARCHAR(50), @RowsToFix);

IF @Execute = 0
BEGIN
    PRINT N'Preview only. No data was changed. Set @Execute = 1 after reviewing the preview.';
    RETURN;
END;

IF @RowsToFix = 0
BEGIN
    PRINT N'No rows to update.';
    RETURN;
END;

BEGIN TRANSACTION;

UPDATE d
    SET d.Item_ID = c.NewItemId
FROM dbo.Transaction_Details d
INNER JOIN #CardItemFixCandidates c
    ON c.ID = d.ID
   AND c.Transaction_ID = d.Transaction_ID
WHERE d.Item_ID = c.OldItemId;

DECLARE @UpdatedRows INT;
SET @UpdatedRows = @@ROWCOUNT;

IF @UpdatedRows <> @RowsToFix
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR(N'Updated rows count mismatch. Expected %d, updated %d. Rolled back.', 16, 1, @RowsToFix, @UpdatedRows);
    RETURN;
END;

COMMIT TRANSACTION;

PRINT N'Update completed successfully. Updated rows: ' + CONVERT(NVARCHAR(50), @UpdatedRows);

PRINT N'3) Post-check rows still mismatched in the same filter';

SELECT
    RemainingMismatches = COUNT(1)
FROM dbo.Transaction_Details d WITH (NOLOCK)
INNER JOIN dbo.Transactions t WITH (NOLOCK)
    ON t.Transaction_ID = d.Transaction_ID
WHERE ISNULL(t.IsCancelled, 0) = 0
  AND d.Item_ID IN (1, 19)
  AND NULLIF(LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))), N'') IS NOT NULL
  AND LEN(LTRIM(RTRIM(ISNULL(d.ItemSerial, N'')))) IN (8, 18)
  AND t.Transaction_Date >= @FromDate
  AND t.Transaction_Date < DATEADD(DAY, 1, @ToDate)
  AND (@Token IS NULL OR LTRIM(RTRIM(ISNULL(d.ItemSerial, N''))) = @Token)
  AND (@StoreId IS NULL OR COALESCE(d.StoreID2, t.StoreID) = @StoreId)
  AND (@OnlyCardSaleAndIssue = 0 OR t.Transaction_Type IN (19, 21))
  AND d.Item_ID <>
      CASE
          WHEN LEN(LTRIM(RTRIM(ISNULL(d.ItemSerial, N'')))) = 18 THEN 1
          WHEN LEN(LTRIM(RTRIM(ISNULL(d.ItemSerial, N'')))) = 8 THEN 19
          ELSE d.Item_ID
      END;
