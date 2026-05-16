IF OBJECT_ID(N'dbo.usp_POS_SaveStockTransfer', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SaveStockTransfer;
GO

IF OBJECT_ID(N'dbo.usp_POS_ImportStockTransferSerials', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_ImportStockTransferSerials;
GO

IF TYPE_ID(N'dbo.POS_StockTransferItems') IS NOT NULL
    DROP TYPE dbo.POS_StockTransferItems;
GO

IF TYPE_ID(N'dbo.POS_StockTransferSerials') IS NOT NULL
    DROP TYPE dbo.POS_StockTransferSerials;
GO

CREATE TYPE dbo.POS_StockTransferItems AS TABLE
(
    ItemId INT NOT NULL,
    UnitId INT NULL,
    Quantity DECIMAL(18, 4) NOT NULL,
    UnitFactor DECIMAL(18, 4) NOT NULL DEFAULT (1),
    Price DECIMAL(18, 4) NOT NULL DEFAULT (0),
    HaveSerial BIT NOT NULL DEFAULT (0),
    Serial NVARCHAR(50) NULL
);
GO

CREATE TYPE dbo.POS_StockTransferSerials AS TABLE
(
    RowNumber INT NOT NULL,
    Serial NVARCHAR(50) NULL
);
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE dbo.usp_POS_ImportStockTransferSerials
    @BranchId INT,
    @SourceStoreId INT,
    @TransferDate SMALLDATETIME,
    @Serials dbo.POS_StockTransferSerials READONLY
AS
BEGIN
    SET NOCOUNT ON;

    IF @TransferDate IS NULL SET @TransferDate = CONVERT(DATE, GETDATE());

    ;WITH CleanSerials AS
    (
        SELECT
            RowNumber,
            LTRIM(RTRIM(ISNULL(Serial, N''))) AS Serial
        FROM @Serials
    ),
    DuplicateSerials AS
    (
        SELECT Serial
        FROM CleanSerials
        WHERE Serial <> N''
        GROUP BY Serial
        HAVING COUNT(1) > 1
    ),
    SerialStock AS
    (
        SELECT
            td.ItemSerial,
            td.Item_ID,
            SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS AvailableQty
        FROM dbo.Transaction_Details td
        INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
        INNER JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = t.Transaction_Type
        INNER JOIN CleanSerials cs ON cs.Serial = td.ItemSerial
        WHERE t.StoreID = @SourceStoreId
          AND t.Transaction_Date <= @TransferDate
          AND ISNULL(tt.StockEffect, 0) <> 0
          AND ISNULL(td.ItemSerial, N'') <> N''
        GROUP BY td.ItemSerial, td.Item_ID
        HAVING SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0
    ),
    AnySerialStock AS
    (
        SELECT
            td.ItemSerial,
            SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS AnyAvailableQty
        FROM dbo.Transaction_Details td
        INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
        INNER JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = t.Transaction_Type
        INNER JOIN CleanSerials cs ON cs.Serial = td.ItemSerial
        WHERE t.Transaction_Date <= @TransferDate
          AND ISNULL(tt.StockEffect, 0) <> 0
          AND ISNULL(td.ItemSerial, N'') <> N''
        GROUP BY td.ItemSerial
        HAVING SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0
    )
    SELECT
        ss.Item_ID AS ItemId,
        COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), i.ItemID)) AS ItemName,
        iu.UnitID AS UnitId,
        COALESCE(NULLIF(u.UnitName, N''), NULLIF(u.UnitNamee, N''), CONVERT(NVARCHAR(50), iu.UnitID)) AS UnitName,
        CAST(COALESCE(iu.UnitFactor, 1) AS DECIMAL(18, 4)) AS UnitFactor,
        CAST(COALESCE(i.CostPrice, i.PurchasePrice, 0) AS DECIMAL(18, 4)) AS Price,
        ss.ItemSerial AS Serial
    FROM CleanSerials cs
    INNER JOIN SerialStock ss ON ss.ItemSerial = cs.Serial
    INNER JOIN dbo.TblItems i ON i.ItemID = ss.Item_ID AND ISNULL(i.HaveSerial, 0) = 1
    OUTER APPLY
    (
        SELECT TOP (1) iu0.UnitID, iu0.UnitFactor, iu0.JunckID
        FROM dbo.TblItemsUnits iu0
        WHERE iu0.ItemID = i.ItemID
        ORDER BY CASE WHEN ISNULL(iu0.DefaultUnit, 0) = 1 THEN 0 ELSE 1 END, iu0.JunckID
    ) iu
    LEFT JOIN dbo.TblUnites u ON u.UnitID = iu.UnitID
    WHERE cs.Serial <> N''
      AND NOT EXISTS (SELECT 1 FROM DuplicateSerials ds WHERE ds.Serial = cs.Serial)
    ORDER BY cs.RowNumber;

    ;WITH CleanSerials AS
    (
        SELECT
            RowNumber,
            LTRIM(RTRIM(ISNULL(Serial, N''))) AS Serial
        FROM @Serials
    ),
    DuplicateSerials AS
    (
        SELECT Serial
        FROM CleanSerials
        WHERE Serial <> N''
        GROUP BY Serial
        HAVING COUNT(1) > 1
    ),
    SerialExists AS
    (
        SELECT DISTINCT td.ItemSerial
        FROM dbo.Transaction_Details td
        INNER JOIN CleanSerials cs ON cs.Serial = td.ItemSerial
        WHERE ISNULL(td.ItemSerial, N'') <> N''
    ),
    SerialStock AS
    (
        SELECT
            td.ItemSerial,
            SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS AvailableQty
        FROM dbo.Transaction_Details td
        INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
        INNER JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = t.Transaction_Type
        INNER JOIN CleanSerials cs ON cs.Serial = td.ItemSerial
        WHERE t.StoreID = @SourceStoreId
          AND t.Transaction_Date <= @TransferDate
          AND ISNULL(tt.StockEffect, 0) <> 0
          AND ISNULL(td.ItemSerial, N'') <> N''
        GROUP BY td.ItemSerial
        HAVING SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0
    ),
    AnySerialStock AS
    (
        SELECT
            td.ItemSerial,
            SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS AnyAvailableQty
        FROM dbo.Transaction_Details td
        INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
        INNER JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = t.Transaction_Type
        INNER JOIN CleanSerials cs ON cs.Serial = td.ItemSerial
        WHERE t.Transaction_Date <= @TransferDate
          AND ISNULL(tt.StockEffect, 0) <> 0
          AND ISNULL(td.ItemSerial, N'') <> N''
        GROUP BY td.ItemSerial
        HAVING SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0
    )
    SELECT
        cs.RowNumber,
        cs.Serial,
        CASE
            WHEN cs.Serial = N'' THEN N'السيريال فارغ'
            WHEN ds.Serial IS NOT NULL THEN N'سيريال مكرر في نفس الملف'
            WHEN se.ItemSerial IS NULL THEN N'السيريال غير موجود'
            WHEN ss.ItemSerial IS NULL AND ass.ItemSerial IS NOT NULL THEN N'السيريال غير موجود في المخزن المحول منه'
            ELSE N'السيريال مستخدم أو غير متاح'
        END AS Reason
    FROM CleanSerials cs
    LEFT JOIN DuplicateSerials ds ON ds.Serial = cs.Serial
    LEFT JOIN SerialExists se ON se.ItemSerial = cs.Serial
    LEFT JOIN SerialStock ss ON ss.ItemSerial = cs.Serial
    LEFT JOIN AnySerialStock ass ON ass.ItemSerial = cs.Serial
    WHERE cs.Serial = N''
       OR ds.Serial IS NOT NULL
       OR ss.ItemSerial IS NULL
    ORDER BY cs.RowNumber;
END
GO

CREATE PROCEDURE dbo.usp_POS_SaveStockTransfer
    @VoucherNumber NVARCHAR(50) = NULL,
    @TransferDate SMALLDATETIME,
    @BranchId INT,
    @SourceStoreId INT,
    @DestinationStoreId INT,
    @UserId INT,
    @EmpId INT = NULL,
    @Remarks NVARCHAR(1000) = NULL,
    @Items dbo.POS_StockTransferItems READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @lockResult INT,
        @sourceTransactionId INT,
        @destinationTransactionId INT,
        @sourceTransactionSerial INT,
        @destinationTransactionSerial INT,
        @noteId INT,
        @noteSerial VARCHAR(50),
        @voucherReturnCode INT,
        @noteReturnCode INT,
        @mSerInv BIGINT,
        @description NVARCHAR(1000),
        @totalValue MONEY,
        @voucherId INT,
        @lineNo INT,
        @inventoryWorkType INT,
        @sourceBranchId INT,
        @destinationBranchId INT,
        @sourceStoreAccount NVARCHAR(50),
        @destinationStoreAccount NVARCHAR(50),
        @sourceBranchCurrentAccount NVARCHAR(255),
        @destinationBranchCurrentAccount NVARCHAR(255),
        @branchInventoryAccount NVARCHAR(50);

    IF @TransferDate IS NULL SET @TransferDate = CONVERT(DATE, GETDATE());
    IF @BranchId IS NULL OR @BranchId <= 0 RAISERROR(N'الفرع مطلوب', 16, 1);
    IF @SourceStoreId IS NULL OR @SourceStoreId <= 0 RAISERROR(N'المخزن المحول منه مطلوب', 16, 1);
    IF @DestinationStoreId IS NULL OR @DestinationStoreId <= 0 RAISERROR(N'المخزن المحول إليه مطلوب', 16, 1);
    IF @SourceStoreId = @DestinationStoreId RAISERROR(N'لا يمكن التحويل إلى نفس المخزن', 16, 1);
    IF NOT EXISTS (SELECT 1 FROM @Items) RAISERROR(N'يجب إضافة صنف واحد على الأقل', 16, 1);
    IF EXISTS (SELECT 1 FROM @Items WHERE ItemId <= 0 OR Quantity <= 0 OR UnitFactor <= 0 OR Price < 0)
        RAISERROR(N'بيانات الأصناف غير صحيحة', 16, 1);

    IF EXISTS
    (
        SELECT 1
        FROM @Items it
        INNER JOIN dbo.TblItems i ON i.ItemID = it.ItemId
        WHERE ISNULL(i.HaveSerial, 0) = 1
          AND (it.Quantity <> 1 OR NULLIF(LTRIM(RTRIM(ISNULL(it.Serial, N''))), N'') IS NULL)
    )
        RAISERROR(N'الصنف المسلسل يجب أن تكون كميته 1 ويجب إدخال السيريال', 16, 1);

    IF EXISTS
    (
        SELECT LTRIM(RTRIM(Serial))
        FROM @Items
        WHERE NULLIF(LTRIM(RTRIM(ISNULL(Serial, N''))), N'') IS NOT NULL
        GROUP BY LTRIM(RTRIM(Serial))
        HAVING COUNT(1) > 1
    )
        RAISERROR(N'يوجد سيريال مكرر في نفس سند التحويل', 16, 1);

    IF EXISTS
    (
        SELECT 1
        FROM @Items it
        INNER JOIN dbo.TblItems i ON i.ItemID = it.ItemId
        WHERE ISNULL(i.HaveSerial, 0) = 0
          AND NULLIF(LTRIM(RTRIM(ISNULL(it.Serial, N''))), N'') IS NOT NULL
    )
        RAISERROR(N'لا يمكن إدخال سيريال لصنف غير مسلسل', 16, 1);

    DECLARE @RequiredStock TABLE
    (
        ItemId INT NOT NULL,
        Serial NVARCHAR(50) NOT NULL,
        RequiredQty FLOAT NOT NULL
    );

    DECLARE @AvailableStock TABLE
    (
        ItemId INT NOT NULL,
        Serial NVARCHAR(50) NOT NULL,
        AvailableQty FLOAT NOT NULL
    );

    INSERT INTO @RequiredStock (ItemId, Serial, RequiredQty)
    SELECT
        it.ItemId,
        LTRIM(RTRIM(ISNULL(it.Serial, N''))) AS Serial,
        SUM(CAST(it.Quantity * it.UnitFactor AS FLOAT)) AS RequiredQty
    FROM @Items it
    GROUP BY it.ItemId, LTRIM(RTRIM(ISNULL(it.Serial, N'')));

    INSERT INTO @AvailableStock (ItemId, Serial, AvailableQty)
    SELECT
        td.Item_ID,
        LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) AS Serial,
        SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS AvailableQty
    FROM dbo.Transaction_Details td
    INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
    INNER JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = t.Transaction_Type
    INNER JOIN @RequiredStock rs ON rs.ItemId = td.Item_ID
        AND (rs.Serial = N'' OR rs.Serial = LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))))
    WHERE t.StoreID = @SourceStoreId
      AND t.Transaction_Date <= @TransferDate
      AND ISNULL(tt.StockEffect, 0) <> 0
    GROUP BY td.Item_ID, LTRIM(RTRIM(ISNULL(td.ItemSerial, N'')));

    IF EXISTS
    (
        SELECT 1
        FROM @RequiredStock rs
        LEFT JOIN @AvailableStock av ON av.ItemId = rs.ItemId AND av.Serial = rs.Serial
        WHERE ISNULL(av.AvailableQty, 0) < rs.RequiredQty
    )
        RAISERROR(N'يوجد صنف أو سيريال غير متاح في المخزن المحول منه', 16, 1);

    IF EXISTS
    (
        SELECT 1
        FROM @Items it
        INNER JOIN dbo.TblItems i ON i.ItemID = it.ItemId AND ISNULL(i.HaveSerial, 0) = 1
        OUTER APPLY
        (
            SELECT TOP (1) td.Item_ID
            FROM dbo.Transaction_Details td
            INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
            INNER JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = t.Transaction_Type
            WHERE t.StoreID = @SourceStoreId
              AND t.Transaction_Date <= @TransferDate
              AND ISNULL(tt.StockEffect, 0) <> 0
              AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = LTRIM(RTRIM(ISNULL(it.Serial, N'')))
            GROUP BY td.Item_ID
            HAVING SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0
        ) serialItem
        WHERE serialItem.Item_ID IS NOT NULL
          AND serialItem.Item_ID <> it.ItemId
    )
        RAISERROR(N'يوجد سيريال لا يخص الصنف المحدد', 16, 1);

    SELECT @sourceBranchId = ISNULL(BranchId, @BranchId) FROM dbo.TblStore WHERE StoreID = @SourceStoreId;
    SELECT @destinationBranchId = ISNULL(BranchId, @BranchId) FROM dbo.TblStore WHERE StoreID = @DestinationStoreId;

    SELECT @totalValue = SUM(CAST(Quantity * UnitFactor * Price AS MONEY))
    FROM @Items;
    SET @totalValue = ISNULL(@totalValue, 0);
    SET @description = N'أذن تحويل بضائع بين المخازن رقم ' + ISNULL(NULLIF(@VoucherNumber, N''), N'');

    SELECT TOP (1)
        @inventoryWorkType = CASE
            WHEN ISNULL(opt_group, 0) = 0 THEN 1
            WHEN ISNULL(Opt_Inventory_create_account, 0) = 1 THEN 2
            WHEN ISNULL(opt_inv_and_branch_create_account, 0) = 1 THEN 3
            ELSE 1
        END
    FROM dbo.TblOptions;
    SET @inventoryWorkType = ISNULL(@inventoryWorkType, 1);

    BEGIN TRANSACTION;

    EXEC @lockResult = sp_getapplock
        @Resource = N'POS_STOCK_TRANSFER_SAVE',
        @LockMode = N'Exclusive',
        @LockOwner = N'Transaction',
        @LockTimeout = 30000;

    IF @lockResult < 0
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR(N'تعذر حجز رقم سند التحويل، حاول مرة أخرى', 16, 1);
        RETURN;
    END

    SELECT @sourceTransactionId = ISNULL(MAX(Transaction_ID), 0) + 1 FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK);
    SET @destinationTransactionId = @sourceTransactionId + 1;

    SELECT @sourceTransactionSerial = ISNULL(MAX(CASE WHEN ISNUMERIC(Transaction_Serial) = 1 THEN CAST(Transaction_Serial AS INT) ELSE 0 END), 0) + 1
    FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
    WHERE Transaction_Type IN (10, 992);

    SELECT @destinationTransactionSerial = ISNULL(MAX(CASE WHEN ISNUMERIC(Transaction_Serial) = 1 THEN CAST(Transaction_Serial AS INT) ELSE 0 END), 0) + 1
    FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
    WHERE Transaction_Type IN (11, 993);

    IF NULLIF(LTRIM(RTRIM(@VoucherNumber)), N'') IS NULL
    BEGIN
        EXEC @voucherReturnCode = dbo.usp_Voucher_coding_V2
            @my_branch = @BranchId,
            @date1 = @TransferDate,
            @Sanad_No = 12,
            @NoteType = 190,
            @departement_name = 1,
            @Transaction_Type = 10,
            @Prefix = NULL,
            @StoreID = @SourceStoreId,
            @BillType = 0,
            @MosemID = 0,
            @mTableName = NULL,
            @mUserID = @UserId,
            @Result = @VoucherNumber OUTPUT,
            @mSerInv = @mSerInv OUTPUT;

        IF @voucherReturnCode <> 0 OR @VoucherNumber IS NULL OR @VoucherNumber = N'error'
            RAISERROR(N'تعذر توليد رقم سند التحويل', 16, 1);
    END

    EXEC @noteReturnCode = dbo.usp_Notes_coding_V2
        @my_branch = @BranchId,
        @date1 = @TransferDate,
        @departement_name = 1,
        @Result = @noteSerial OUTPUT;

    IF @noteReturnCode <> 0 OR @noteSerial IS NULL OR @noteSerial = 'error'
        RAISERROR(N'تعذر توليد رقم القيد', 16, 1);

    SELECT @noteId = ISNULL(MAX(NoteID), 0) + 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK);

    SET @description = N'أذن تحويل بضائع بين المخازن رقم ' + CONVERT(NVARCHAR(50), @VoucherNumber);

    INSERT INTO dbo.Transactions
    (
        Transaction_ID, Transaction_Serial, Transaction_Date, Transaction_Type,
        UserID, StoreID, BranchId, Emp_ID, NoteSerial, NoteSerial1, NoteId,
        TransactionComment, OldNoteSerial1
    )
    VALUES
    (
        @sourceTransactionId, CONVERT(NVARCHAR(50), @sourceTransactionSerial), @TransferDate, 10,
        @UserId, @SourceStoreId, @sourceBranchId, @EmpId, @noteSerial, CONVERT(VARCHAR(50), @VoucherNumber), @noteId,
        @Remarks, CONVERT(NVARCHAR(255), @VoucherNumber)
    );

    INSERT INTO dbo.Transactions
    (
        Transaction_ID, Transaction_Serial, Transaction_Date, Transaction_Type,
        UserID, StoreID, ReturnID, BranchId, Emp_ID, NoteSerial, NoteSerial1, NoteId,
        TransactionComment, OldNoteSerial1
    )
    VALUES
    (
        @destinationTransactionId, CONVERT(NVARCHAR(50), @destinationTransactionSerial), @TransferDate, 11,
        @UserId, @DestinationStoreId, @sourceTransactionId, @destinationBranchId, @EmpId, @noteSerial, CONVERT(VARCHAR(50), @VoucherNumber), @noteId,
        @Remarks, CONVERT(NVARCHAR(255), @VoucherNumber)
    );

    INSERT INTO dbo.Notes
    (
        NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value,
        Transaction_ID, UserID, Remark, branch_no, numbering_type,
        numbering_type1, sanad_year, sanad_month, OldNoteSerial1
    )
    VALUES
    (
        @noteId, @TransferDate, 190,
        CASE WHEN ISNUMERIC(@noteSerial) = 1 THEN CAST(@noteSerial AS FLOAT) ELSE NULL END,
        CASE WHEN ISNUMERIC(@VoucherNumber) = 1 THEN CAST(@VoucherNumber AS FLOAT) ELSE NULL END,
        @totalValue, @sourceTransactionId, @UserId, @VoucherNumber, @BranchId,
        0, 12, YEAR(@TransferDate), MONTH(@TransferDate), @VoucherNumber
    );

    INSERT INTO dbo.Transaction_Details
    (
        Transaction_ID, Item_ID, ItemCase, ItemSerial, Quantity, Price, UnitId,
        ShowQty, QtyBySmalltUnit, showPrice, Transaction_Date, BranchId,
        ColorID, ItemSize, ClassId, OldQty, OldCost, NewQty, NewCost,
        StoreID2, FromStoreAr, ToStoreAr
    )
    SELECT
        @sourceTransactionId,
        it.ItemId,
        ISNULL(i.ItemCase, 1),
        NULLIF(LTRIM(RTRIM(it.Serial)), N''),
        CAST(it.Quantity * it.UnitFactor AS FLOAT),
        CAST(CASE WHEN it.UnitFactor = 0 THEN it.Price ELSE it.Price / it.UnitFactor END AS FLOAT),
        it.UnitId,
        CAST(it.Quantity AS FLOAT),
        CAST(it.UnitFactor AS FLOAT),
        CAST(it.Price AS FLOAT),
        @TransferDate,
        @sourceBranchId,
        1,
        N'1',
        1,
        ISNULL(stock.AvailableQty, 0),
        it.Price,
        ISNULL(stock.AvailableQty, 0) - CAST(it.Quantity * it.UnitFactor AS FLOAT),
        it.Price,
        @DestinationStoreId,
        src.StoreName,
        dst.StoreName
    FROM @Items it
    INNER JOIN dbo.TblItems i ON i.ItemID = it.ItemId
    LEFT JOIN dbo.TblStore src ON src.StoreID = @SourceStoreId
    LEFT JOIN dbo.TblStore dst ON dst.StoreID = @DestinationStoreId
    OUTER APPLY
    (
        SELECT SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS AvailableQty
        FROM dbo.Transaction_Details td
        INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
        INNER JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = t.Transaction_Type
        WHERE t.StoreID = @SourceStoreId
          AND t.Transaction_Date <= @TransferDate
          AND td.Item_ID = it.ItemId
          AND (NULLIF(LTRIM(RTRIM(ISNULL(it.Serial, N''))), N'') IS NULL OR LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = LTRIM(RTRIM(ISNULL(it.Serial, N''))))
          AND ISNULL(tt.StockEffect, 0) <> 0
    ) stock;

    INSERT INTO dbo.Transaction_Details
    (
        Transaction_ID, Item_ID, ItemCase, ItemSerial, Quantity, Price, UnitId,
        ShowQty, QtyBySmalltUnit, showPrice, Transaction_Date, BranchId,
        ColorID, ItemSize, ClassId, OldQty, OldCost, NewQty, NewCost,
        StoreID2, FromStoreAr, ToStoreAr
    )
    SELECT
        @destinationTransactionId,
        it.ItemId,
        ISNULL(i.ItemCase, 1),
        NULLIF(LTRIM(RTRIM(it.Serial)), N''),
        CAST(it.Quantity * it.UnitFactor AS FLOAT),
        CAST(CASE WHEN it.UnitFactor = 0 THEN it.Price ELSE it.Price / it.UnitFactor END AS FLOAT),
        it.UnitId,
        CAST(it.Quantity AS FLOAT),
        CAST(it.UnitFactor AS FLOAT),
        CAST(it.Price AS FLOAT),
        @TransferDate,
        @destinationBranchId,
        1,
        N'1',
        1,
        ISNULL(stock.AvailableQty, 0),
        ISNULL(stock.AvgCost, it.Price),
        ISNULL(stock.AvailableQty, 0) + CAST(it.Quantity * it.UnitFactor AS FLOAT),
        CASE
            WHEN ISNULL(stock.AvailableQty, 0) + CAST(it.Quantity * it.UnitFactor AS FLOAT) = 0 THEN 0
            ELSE ((ISNULL(stock.AvailableQty, 0) * ISNULL(stock.AvgCost, it.Price)) + (CAST(it.Quantity * it.UnitFactor AS FLOAT) * it.Price))
                 / (ISNULL(stock.AvailableQty, 0) + CAST(it.Quantity * it.UnitFactor AS FLOAT))
        END,
        @SourceStoreId,
        src.StoreName,
        dst.StoreName
    FROM @Items it
    INNER JOIN dbo.TblItems i ON i.ItemID = it.ItemId
    LEFT JOIN dbo.TblStore src ON src.StoreID = @SourceStoreId
    LEFT JOIN dbo.TblStore dst ON dst.StoreID = @DestinationStoreId
    OUTER APPLY
    (
        SELECT
            SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) AS AvailableQty,
            MAX(ISNULL(td.NewCost, td.OldCost)) AS AvgCost
        FROM dbo.Transaction_Details td
        INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
        INNER JOIN dbo.TransactionTypes tt ON tt.Transaction_Type = t.Transaction_Type
        WHERE t.StoreID = @DestinationStoreId
          AND t.Transaction_Date <= @TransferDate
          AND td.Item_ID = it.ItemId
          AND (NULLIF(LTRIM(RTRIM(ISNULL(it.Serial, N''))), N'') IS NULL OR LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = LTRIM(RTRIM(ISNULL(it.Serial, N''))))
          AND ISNULL(tt.StockEffect, 0) <> 0
    ) stock;

    SELECT @voucherId = ISNULL(MAX(Double_Entry_Vouchers_ID), 0) + 1
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (UPDLOCK, HOLDLOCK);
    SET @lineNo = 1;

    IF @inventoryWorkType = 1
    BEGIN
        SELECT TOP (1) @branchInventoryAccount = NULLIF(a0, N'') FROM dbo.branches;
        IF @branchInventoryAccount IS NULL RAISERROR(N'لم يتم تحديد حساب مخزون الفرع', 16, 1);

        INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
        (
            Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
            Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
            Notes_ID, Transaction_ID, UserID, currency, valuee, rate, branch_id
        )
        VALUES
        (@voucherId, @lineNo, @branchInventoryAccount, @totalValue, 1, @description, @TransferDate, @noteId, @sourceTransactionId, @UserId, N'', @totalValue, 1, @sourceBranchId),
        (@voucherId, @lineNo + 1, @branchInventoryAccount, @totalValue, 0, @description, @TransferDate, @noteId, @sourceTransactionId, @UserId, N'', @totalValue, 1, @destinationBranchId);
        SET @lineNo = @lineNo + 2;
    END
    ELSE IF @inventoryWorkType = 2
    BEGIN
        SELECT @sourceStoreAccount = NULLIF(Account_Code, N'') FROM dbo.TblStore WHERE StoreID = @SourceStoreId;
        SELECT @destinationStoreAccount = NULLIF(Account_Code, N'') FROM dbo.TblStore WHERE StoreID = @DestinationStoreId;
        IF @sourceStoreAccount IS NULL RAISERROR(N'لم يتم تحديد حساب المخزن المحول منه', 16, 1);
        IF @destinationStoreAccount IS NULL RAISERROR(N'لم يتم تحديد حساب المخزن المحول إليه', 16, 1);

        INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
        (
            Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
            Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
            Notes_ID, Transaction_ID, UserID, currency, valuee, rate, branch_id
        )
        VALUES
        (@voucherId, @lineNo, @sourceStoreAccount, @totalValue, 1, @description, @TransferDate, @noteId, @sourceTransactionId, @UserId, N'', @totalValue, 1, @sourceBranchId),
        (@voucherId, @lineNo + 1, @destinationStoreAccount, @totalValue, 0, @description, @TransferDate, @noteId, @sourceTransactionId, @UserId, N'', @totalValue, 1, @destinationBranchId);
        SET @lineNo = @lineNo + 2;

        IF @sourceBranchId <> @destinationBranchId
        BEGIN
            SELECT @sourceBranchCurrentAccount = NULLIF(Account_Code, N'') FROM dbo.TblBranchesData WHERE branch_id = @sourceBranchId;
            SELECT @destinationBranchCurrentAccount = NULLIF(Account_Code, N'') FROM dbo.TblBranchesData WHERE branch_id = @destinationBranchId;

            IF @sourceBranchCurrentAccount IS NOT NULL AND @destinationBranchCurrentAccount IS NOT NULL
            BEGIN
                INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
                (
                    Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
                    Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
                    Notes_ID, Transaction_ID, UserID, currency, valuee, rate, branch_id
                )
                VALUES
                (@voucherId, @lineNo, @destinationBranchCurrentAccount, @totalValue, 0, @description, @TransferDate, @noteId, @sourceTransactionId, @UserId, N'', @totalValue, 1, @sourceBranchId),
                (@voucherId, @lineNo + 1, @sourceBranchCurrentAccount, @totalValue, 1, @description, @TransferDate, @noteId, @sourceTransactionId, @UserId, N'', @totalValue, 1, @destinationBranchId);
                SET @lineNo = @lineNo + 2;
            END
        END
    END
    ELSE
    BEGIN
        ;WITH GroupLines AS
        (
            SELECT
                it.ItemId,
                SUM(CAST(it.Quantity * it.UnitFactor * it.Price AS MONEY)) AS LineValue
            FROM @Items it
            GROUP BY it.ItemId
        )
        INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
        (
            Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
            Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
            Notes_ID, Transaction_ID, UserID, currency, valuee, rate, branch_id
        )
        SELECT
            @voucherId,
            ROW_NUMBER() OVER (ORDER BY gl.ItemId),
            ga.account_code,
            gl.LineValue,
            1,
            @description,
            @TransferDate,
            @noteId,
            @sourceTransactionId,
            @UserId,
            N'',
            gl.LineValue,
            1,
            @sourceBranchId
        FROM GroupLines gl
        INNER JOIN dbo.TblItems i ON i.ItemID = gl.ItemId
        INNER JOIN dbo.groups_account_in_inventory ga ON ga.group_id = i.GroupID
            AND ga.inventory_id = @SourceStoreId
            AND ga.account_type_code = '0';

        SET @lineNo = ISNULL((SELECT MAX(DEV_ID_Line_No) FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Double_Entry_Vouchers_ID = @voucherId), 0) + 1;

        ;WITH GroupLines AS
        (
            SELECT
                it.ItemId,
                SUM(CAST(it.Quantity * it.UnitFactor * it.Price AS MONEY)) AS LineValue
            FROM @Items it
            GROUP BY it.ItemId
        )
        INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
        (
            Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
            Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
            Notes_ID, Transaction_ID, UserID, currency, valuee, rate, branch_id
        )
        SELECT
            @voucherId,
            @lineNo + ROW_NUMBER() OVER (ORDER BY gl.ItemId) - 1,
            ga.account_code,
            gl.LineValue,
            0,
            @description,
            @TransferDate,
            @noteId,
            @sourceTransactionId,
            @UserId,
            N'',
            gl.LineValue,
            1,
            @destinationBranchId
        FROM GroupLines gl
        INNER JOIN dbo.TblItems i ON i.ItemID = gl.ItemId
        INNER JOIN dbo.groups_account_in_inventory ga ON ga.group_id = i.GroupID
            AND ga.inventory_id = @DestinationStoreId
            AND ga.account_type_code = '0';

        IF EXISTS
        (
            SELECT 1
            FROM @Items it
            INNER JOIN dbo.TblItems i ON i.ItemID = it.ItemId
            LEFT JOIN dbo.groups_account_in_inventory gas ON gas.group_id = i.GroupID AND gas.inventory_id = @SourceStoreId AND gas.account_type_code = '0'
            LEFT JOIN dbo.groups_account_in_inventory gad ON gad.group_id = i.GroupID AND gad.inventory_id = @DestinationStoreId AND gad.account_type_code = '0'
            WHERE gas.account_code IS NULL OR gad.account_code IS NULL
        )
            RAISERROR(N'لم يتم تحديد حساب مجموعة الصنف للمخازن', 16, 1);
    END

    UPDATE dbo.Notes
    SET Double_Entry_Vouchers_ID = @voucherId,
        Note_Value = @totalValue
    WHERE NoteID = @noteId;

    COMMIT TRANSACTION;

    SELECT
        @sourceTransactionId AS SourceTransaction_ID,
        @destinationTransactionId AS DestinationTransaction_ID,
        @noteId AS NoteID,
        CONVERT(NVARCHAR(50), @VoucherNumber) AS VoucherNumber,
        CONVERT(NVARCHAR(50), @noteSerial) AS NoteSerial,
        @voucherId AS Double_Entry_Vouchers_ID;
END
GO
