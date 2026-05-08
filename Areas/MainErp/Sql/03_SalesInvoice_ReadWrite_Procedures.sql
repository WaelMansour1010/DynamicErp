/* MainErp sales invoice read/write foundation.
   SQL Server 2012 compatible.
   Source screen: SatriahMain\Frm\FrmSaleBill6.frm
   Scope:
   - Fast read/search for Workshop/Pump sales.
   - Fast multi-result details read.
   - Draft save gate only. No accounting posting and no inventory voucher creation.
*/

IF OBJECT_ID('dbo.MainErp_SalesInvoice_Search', 'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_SalesInvoice_Search;
GO
CREATE PROCEDURE dbo.MainErp_SalesInvoice_Search
    @TypeInvoice int,
    @SearchText nvarchar(200) = NULL,
    @FromDate datetime = NULL,
    @ToDate datetime = NULL,
    @BranchId int = NULL,
    @Page int = 1,
    @PageSize int = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page IS NULL OR @Page < 1 SET @Page = 1;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 20;

    DECLARE @StartRow int;
    DECLARE @EndRow int;
    DECLARE @SearchLike nvarchar(220);

    SET @StartRow = ((@Page - 1) * @PageSize) + 1;
    SET @EndRow = @Page * @PageSize;
    SET @SearchText = NULLIF(LTRIM(RTRIM(@SearchText)), '');
    SET @SearchLike = CASE WHEN @SearchText IS NULL THEN NULL ELSE N'%' + @SearchText + N'%' END;

    ;WITH InvoiceRows AS
    (
        SELECT
            ROW_NUMBER() OVER (ORDER BY t.Transaction_ID DESC) AS RowNo,
            COUNT(1) OVER() AS TotalCount,
            t.Transaction_ID,
            t.Transaction_Serial,
            t.NoteSerial,
            t.NoteSerial1,
            t.ManualNO,
            t.Transaction_Date,
            t.Transaction_Type,
            ISNULL(t.TypeInvoice, 0) AS TypeInvoice,
            t.CusID,
            t.BranchId,
            COALESCE(c.CusName, t.CashCustomerName) AS CustomerName,
            t.CashCustomerName,
            b.branch_name AS BranchName,
            CONVERT(nvarchar(50), t.StoreID) AS StoreName,
            t.Total,
            t.NetValue,
            t.VAT,
            t.PayedValue,
            t.RemainValue,
            t.NoteId,
            t.Closed
        FROM dbo.Transactions t
        LEFT JOIN dbo.TblCustemers c ON t.CusID = c.CusID
        LEFT JOIN dbo.TblBranchesData b ON t.BranchId = b.branch_id
        WHERE t.Transaction_Type IN (21, 42, 38, 9)
          AND ((@TypeInvoice = 2 AND ISNULL(t.TypeInvoice, 0) = 2) OR (@TypeInvoice = 1 AND ISNULL(t.TypeInvoice, 0) <> 2))
          AND (@SearchText IS NULL
               OR t.NoteSerial1 LIKE @SearchLike
               OR t.NoteSerial LIKE @SearchLike
               OR t.Transaction_Serial LIKE @SearchLike
               OR t.ManualNO LIKE @SearchLike
               OR t.CashCustomerName LIKE @SearchLike
               OR c.CusName LIKE @SearchLike)
          AND (@FromDate IS NULL OR t.Transaction_Date >= @FromDate)
          AND (@ToDate IS NULL OR t.Transaction_Date < DATEADD(day, 1, @ToDate))
          AND (@BranchId IS NULL OR t.BranchId = @BranchId)
    )
    SELECT *
    FROM InvoiceRows
    WHERE RowNo BETWEEN @StartRow AND @EndRow
    ORDER BY RowNo;
END
GO

IF OBJECT_ID('dbo.MainErp_PumpSales_SaveDraftFull', 'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_PumpSales_SaveDraftFull;
GO
CREATE PROCEDURE dbo.MainErp_PumpSales_SaveDraftFull
    @TransactionId int = NULL OUTPUT,
    @TransactionDate datetime,
    @BranchId int = NULL,
    @StoreId int = NULL,
    @BoxId int = NULL,
    @CusId int = NULL,
    @CashCustomerName nvarchar(250) = NULL,
    @ManualNo nvarchar(100) = NULL,
    @Remarks nvarchar(max) = NULL,
    @UserId int = NULL,
    @LinesXml xml,
    @PaymentsXml xml = NULL,
    @DryRun bit = 1,
    @EnableDraftWrite bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TransactionDate IS NULL
    BEGIN
        RAISERROR('TransactionDate is required.', 16, 1);
        RETURN;
    END

    DECLARE @Lines TABLE
    (
        RowNo int IDENTITY(1,1),
        Id int NULL,
        LineNumber int NULL,
        ItemId int NULL,
        UnitId int NULL,
        StoreId2 int NULL,
        PumpId int NULL,
        PrevQty decimal(18,4) NULL,
        CurrentQty decimal(18,4) NULL,
        ShowQty decimal(18,4) NULL,
        Quantity decimal(18,4) NULL,
        Price decimal(18,4) NULL,
        CostPrice decimal(18,4) NULL,
        Cash decimal(18,4) NULL,
        Mada decimal(18,4) NULL,
        Visa decimal(18,4) NULL,
        Deferred decimal(18,4) NULL,
        CashQty decimal(18,4) NULL,
        MadaQty decimal(18,4) NULL,
        VisaQty decimal(18,4) NULL,
        DeferredQty decimal(18,4) NULL,
        AmountH decimal(18,4) NULL,
        AmountHComm decimal(18,4) NULL,
        AccountCode nvarchar(100) NULL,
        AccountCodeComm nvarchar(100) NULL,
        IsOther bit NULL,
        DetailsPump nvarchar(max) NULL
    );

    INSERT INTO @Lines
    (
        Id, LineNumber, ItemId, UnitId, StoreId2, PumpId, PrevQty, CurrentQty, ShowQty, Quantity, Price, CostPrice,
        Cash, Mada, Visa, Deferred, CashQty, MadaQty, VisaQty, DeferredQty, AmountH, AmountHComm,
        AccountCode, AccountCodeComm, IsOther, DetailsPump
    )
    SELECT
        NULLIF(T.c.value('@Id', 'int'), 0),
        T.c.value('@LineNumber', 'int'),
        NULLIF(T.c.value('@ItemId', 'int'), 0),
        NULLIF(T.c.value('@UnitId', 'int'), 0),
        NULLIF(T.c.value('@StoreId2', 'int'), 0),
        NULLIF(T.c.value('@PumpId', 'int'), 0),
        T.c.value('@PrevQty', 'decimal(18,4)'),
        T.c.value('@CurrentQty', 'decimal(18,4)'),
        T.c.value('@ShowQty', 'decimal(18,4)'),
        T.c.value('@Quantity', 'decimal(18,4)'),
        T.c.value('@Price', 'decimal(18,4)'),
        T.c.value('@CostPrice', 'decimal(18,4)'),
        T.c.value('@Cash', 'decimal(18,4)'),
        T.c.value('@Mada', 'decimal(18,4)'),
        T.c.value('@Visa', 'decimal(18,4)'),
        T.c.value('@Deferred', 'decimal(18,4)'),
        T.c.value('@CashQty', 'decimal(18,4)'),
        T.c.value('@MadaQty', 'decimal(18,4)'),
        T.c.value('@VisaQty', 'decimal(18,4)'),
        T.c.value('@DeferredQty', 'decimal(18,4)'),
        T.c.value('@AmountH', 'decimal(18,4)'),
        T.c.value('@AmountHComm', 'decimal(18,4)'),
        NULLIF(T.c.value('@AccountCode', 'nvarchar(100)'), N''),
        NULLIF(T.c.value('@AccountCodeComm', 'nvarchar(100)'), N''),
        T.c.value('@IsOther', 'bit'),
        NULLIF(T.c.value('@DetailsPump', 'nvarchar(max)'), N'')
    FROM @LinesXml.nodes('/Lines/Line') AS T(c)
    WHERE NULLIF(T.c.value('@ItemId', 'int'), 0) IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM @Lines)
    BEGIN
        RAISERROR('At least one pump line is required.', 16, 1);
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM @Lines
        WHERE ABS(ISNULL(CurrentQty, 0) - ISNULL(PrevQty, 0) - ISNULL(CashQty, 0) - ISNULL(MadaQty, 0) - ISNULL(VisaQty, 0) - ISNULL(DeferredQty, 0)) > 0.0001
    )
    BEGIN
        RAISERROR('Pump quantities are not fully distributed. StillPumbQty must be zero for every line.', 16, 1);
        RETURN;
    END

    IF @TransactionId IS NOT NULL AND @TransactionId <> 0
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM dbo.Transactions
            WHERE Transaction_ID = @TransactionId
              AND Transaction_Type = 21
              AND ISNULL(TypeInvoice, 0) = 2
              AND (
                    ISNULL(Closed, 0) <> 0
                 OR ISNULL(Posted, 0) <> 0
                 OR ISNULL(Approved, 0) <> 0
                 OR ISNULL(IsPosted, 0) <> 0
              )
        )
        BEGIN
            RAISERROR('Pump invoice is already closed/posted/approved and cannot be edited.', 16, 1);
            RETURN;
        END
    END

    DECLARE @Payments TABLE
    (
        Id int NULL,
        PaymentId int NULL,
        Value decimal(18,4) NULL,
        CardNo nvarchar(100) NULL,
        MaxValue decimal(18,4) NULL
    );

    IF @PaymentsXml IS NOT NULL
    BEGIN
        INSERT INTO @Payments (Id, PaymentId, Value, CardNo, MaxValue)
        SELECT
            NULLIF(T.c.value('@Id', 'int'), 0),
            NULLIF(T.c.value('@PaymentId', 'int'), 0),
            T.c.value('@Value', 'decimal(18,4)'),
            NULLIF(T.c.value('@CardNo', 'nvarchar(100)'), N''),
            T.c.value('@MaxValue', 'decimal(18,4)')
        FROM @PaymentsXml.nodes('/Payments/Payment') AS T(c)
        WHERE T.c.value('@Value', 'decimal(18,4)') <> 0;
    END

    DECLARE @Total decimal(18,4);
    DECLARE @Vat decimal(18,4);
    DECLARE @PayedValue decimal(18,4);
    DECLARE @RemainValue decimal(18,4);
    DECLARE @CashValue decimal(18,4);
    DECLARE @CreditValue decimal(18,4);

    SELECT
        @Total = SUM(ISNULL(Cash, 0) + ISNULL(Mada, 0) + ISNULL(Visa, 0) + ISNULL(Deferred, 0)),
        @Vat = SUM(ISNULL(AmountH, 0)),
        @CashValue = SUM(ISNULL(Cash, 0) + ISNULL(Mada, 0) + ISNULL(Visa, 0)),
        @CreditValue = SUM(ISNULL(Deferred, 0))
    FROM @Lines;

    SET @PayedValue = @CashValue;
    SET @RemainValue = @CreditValue;

    SELECT
        @TransactionId AS TransactionId,
        @Total AS Total,
        @Vat AS Vat,
        @CashValue AS CashValue,
        @CreditValue AS CreditValue,
        @PayedValue AS PayedValue,
        @RemainValue AS RemainValue,
        (SELECT COUNT(1) FROM @Lines) AS LineCount,
        (SELECT COUNT(1) FROM @Payments) AS PaymentCount,
        @DryRun AS DryRun,
        @EnableDraftWrite AS EnableDraftWrite,
        CAST(N'Pump full draft save. Header, lines, payments, deferred distribution, and tblPumpType.PercentV only. No Notes, vouchers, or inventory documents are created.' AS nvarchar(500)) AS SafetyMessage;

    IF @DryRun = 1 OR @EnableDraftWrite = 0
    BEGIN
        RETURN;
    END

    BEGIN TRANSACTION;

    IF @TransactionId IS NOT NULL AND @TransactionId <> 0
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
            WHERE Transaction_ID = @TransactionId
              AND Transaction_Type = 21
              AND ISNULL(TypeInvoice, 0) = 2
              AND (
                    ISNULL(Closed, 0) <> 0
                 OR ISNULL(Posted, 0) <> 0
                 OR ISNULL(Approved, 0) <> 0
                 OR ISNULL(IsPosted, 0) <> 0
              )
        )
        BEGIN
            RAISERROR('Pump invoice is already closed/posted/approved and cannot be edited.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
            WHERE Transaction_ID = @TransactionId
              AND Transaction_Type = 21
              AND ISNULL(TypeInvoice, 0) = 2
        )
        BEGIN
            RAISERROR('Pump invoice was not found or is already closed/posted/approved.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END

    IF @TransactionId IS NULL OR @TransactionId = 0
    BEGIN
        SELECT @TransactionId = ISNULL(MAX(Transaction_ID), 0) + 1
        FROM dbo.Transactions WITH (TABLOCKX, HOLDLOCK);

        INSERT INTO dbo.Transactions
        (
            Transaction_ID, Transaction_Serial, Transaction_Date, Transaction_Type, TypeInvoice,
            CusID, StoreID, UserID, CashCustomerName, ManualNO, BranchId, BoxID,
            Closed, Posted, Approved, IsPosted,
            Total, NetValue, VAT, PayedValue, RemainValue, Transaction_NetValue,
            CashValue, CreditValue, TransactionComment, remark, DateRec
        )
        VALUES
        (
            @TransactionId,
            (SELECT ISNULL(MAX(CASE WHEN ISNUMERIC(Transaction_Serial) = 1 THEN CONVERT(bigint, CONVERT(float, Transaction_Serial)) ELSE 0 END), 0) + 1 FROM dbo.Transactions WITH (TABLOCKX, HOLDLOCK) WHERE Transaction_Type = 21),
            @TransactionDate,
            21,
            2,
            @CusId,
            @StoreId,
            @UserId,
            @CashCustomerName,
            @ManualNo,
            @BranchId,
            @BoxId,
            0,
            0,
            0,
            0,
            @Total,
            @Total,
            @Vat,
            @PayedValue,
            @RemainValue,
            @Total,
            @CashValue,
            @CreditValue,
            @Remarks,
            @Remarks,
            GETDATE()
        );
    END
    ELSE
    BEGIN
        UPDATE dbo.Transactions
        SET
            Transaction_Date = @TransactionDate,
            CusID = @CusId,
            StoreID = @StoreId,
            UserID = @UserId,
            CashCustomerName = @CashCustomerName,
            ManualNO = @ManualNo,
            BranchId = @BranchId,
            BoxID = @BoxId,
            Total = @Total,
            NetValue = @Total,
            VAT = @Vat,
            PayedValue = @PayedValue,
            RemainValue = @RemainValue,
            Transaction_NetValue = @Total,
            CashValue = @CashValue,
            CreditValue = @CreditValue,
            TransactionComment = @Remarks,
            remark = @Remarks,
            DateRec = ISNULL(DateRec, GETDATE())
        WHERE Transaction_ID = @TransactionId
          AND Transaction_Type = 21
          AND ISNULL(TypeInvoice, 0) = 2;

        DELETE FROM dbo.Transaction_DetailsPump WHERE Transaction_ID = @TransactionId;
        DELETE FROM dbo.TblSalesPayment WHERE TransID = @TransactionId;
        DELETE FROM dbo.Transaction_Details WHERE Transaction_ID = @TransactionId;
    END

    INSERT INTO dbo.Transaction_Details
    (
        Transaction_ID, Item_ID, UnitId, StoreID2, PumpId, LineID,
        Quantity, ShowQty, Price, showPrice, CostPrice,
        PrevQty, CurrentQty, Cash, Mada, Visa, Deferred,
        CashQty, MadaQty, VisaQty, DeferredQty,
        AmountH, AmountHComm, Account_Code, Account_CodeComm, IsOther, DetailsPump,
        ColorID, ItemSize, ClassId, BranchId, IsAutoDetail
    )
    SELECT
        @TransactionId,
        ItemId,
        UnitId,
        StoreId2,
        PumpId,
        RowNo,
        ISNULL(Quantity, ISNULL(ShowQty, 0)),
        ISNULL(ShowQty, ISNULL(Quantity, 0)),
        ISNULL(Price, 0),
        ISNULL(Price, 0),
        ISNULL(CostPrice, 0),
        PrevQty,
        CurrentQty,
        Cash,
        Mada,
        Visa,
        Deferred,
        CashQty,
        MadaQty,
        VisaQty,
        DeferredQty,
        AmountH,
        AmountHComm,
        AccountCode,
        AccountCodeComm,
        IsOther,
        DetailsPump,
        1,
        1,
        1,
        @BranchId,
        0
    FROM @Lines
    ORDER BY RowNo;

    UPDATE p
    SET p.PercentV = l.CurrentQty
    FROM dbo.tblPumpType p
    INNER JOIN @Lines l ON p.ID = l.PumpId
    WHERE l.PumpId IS NOT NULL;

    INSERT INTO dbo.TblSalesPayment (TransID, PaymentID, Value, CardNo, MaxValue)
    SELECT @TransactionId, PaymentId, Value, CardNo, MaxValue
    FROM @Payments;

    DECLARE @SavedLines TABLE
    (
        RowNo int IDENTITY(1,1),
        DetailId int,
        LineID int,
        DetailsPump nvarchar(max)
    );

    INSERT INTO @SavedLines (DetailId, LineID, DetailsPump)
    SELECT ID, LineID, CONVERT(nvarchar(max), DetailsPump)
    FROM dbo.Transaction_Details
    WHERE Transaction_ID = @TransactionId
    ORDER BY LineID, ID;

    DECLARE @LineId int;
    DECLARE @DetailsPump nvarchar(max);
    DECLARE @Rows TABLE (RowNo int IDENTITY(1,1), RowText nvarchar(max));

    DECLARE pump_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT LineID, DetailsPump
        FROM @SavedLines
        WHERE NULLIF(LTRIM(RTRIM(DetailsPump)), N'') IS NOT NULL;

    OPEN pump_cursor;
    FETCH NEXT FROM pump_cursor INTO @LineId, @DetailsPump;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DELETE FROM @Rows;

        INSERT INTO @Rows (RowText)
        SELECT LTRIM(RTRIM(x.i.value('.', 'nvarchar(max)')))
        FROM
        (
            SELECT CAST('<x><i>' + REPLACE(REPLACE(REPLACE(ISNULL(@DetailsPump, N''), '&', '&amp;'), '<', '&lt;'), '@', '</i><i>') + '</i></x>' AS xml) AS XmlData
        ) src
        CROSS APPLY src.XmlData.nodes('/x/i') x(i)
        WHERE NULLIF(LTRIM(RTRIM(x.i.value('.', 'nvarchar(max)'))), N'') IS NOT NULL;

        INSERT INTO dbo.Transaction_DetailsPump (LineID, CusID, ItemId, Amount, Transaction_ID, RecNo, Qty, Price)
        SELECT
            @LineId,
            CASE WHEN ISNUMERIC(parts.XmlData.value('(/r/f[1])[1]', 'nvarchar(100)')) = 1 THEN CONVERT(int, CONVERT(float, parts.XmlData.value('(/r/f[1])[1]', 'nvarchar(100)'))) ELSE NULL END,
            NULL,
            CASE WHEN ISNUMERIC(parts.XmlData.value('(/r/f[3])[1]', 'nvarchar(100)')) = 1 THEN CONVERT(float, parts.XmlData.value('(/r/f[3])[1]', 'nvarchar(100)')) ELSE NULL END,
            @TransactionId,
            CASE WHEN ISNUMERIC(parts.XmlData.value('(/r/f[7])[1]', 'nvarchar(100)')) = 1 THEN CONVERT(int, CONVERT(float, parts.XmlData.value('(/r/f[7])[1]', 'nvarchar(100)'))) ELSE NULL END,
            CASE WHEN ISNUMERIC(parts.XmlData.value('(/r/f[5])[1]', 'nvarchar(100)')) = 1 THEN CONVERT(float, parts.XmlData.value('(/r/f[5])[1]', 'nvarchar(100)')) ELSE NULL END,
            CASE WHEN ISNUMERIC(parts.XmlData.value('(/r/f[6])[1]', 'nvarchar(100)')) = 1 THEN CONVERT(float, parts.XmlData.value('(/r/f[6])[1]', 'nvarchar(100)')) ELSE NULL END
        FROM @Rows
        CROSS APPLY
        (
            SELECT CAST('<r><f>' + REPLACE(REPLACE(REPLACE(RowText, '&', '&amp;'), '<', '&lt;'), '#', '</f><f>') + '</f></r>' AS xml) AS XmlData
        ) parts
        WHERE LEN(RowText) > 0;

        DELETE FROM @Rows;
        FETCH NEXT FROM pump_cursor INTO @LineId, @DetailsPump;
    END

    CLOSE pump_cursor;
    DEALLOCATE pump_cursor;

    COMMIT TRANSACTION;

    SELECT @TransactionId AS TransactionId, CAST(N'Pump invoice draft saved fully. Posting/inventory documents are still disabled.' AS nvarchar(300)) AS ResultMessage;
END
GO

IF OBJECT_ID('dbo.MainErp_AuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MainErp_AuditLog
    (
        AuditId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OperationName nvarchar(100) NOT NULL,
        EntityName nvarchar(100) NOT NULL,
        EntityKey nvarchar(100) NULL,
        UserId int NULL,
        CorrelationId uniqueidentifier NOT NULL,
        Message nvarchar(max) NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_MainErp_AuditLog_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.MainErp_AuditLog', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.MainErp_AuditLog', 'BeforeSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.MainErp_AuditLog ADD BeforeSnapshot nvarchar(max) NULL;
END
GO

IF OBJECT_ID('dbo.MainErp_AuditLog', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.MainErp_AuditLog', 'AfterSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.MainErp_AuditLog ADD AfterSnapshot nvarchar(max) NULL;
END
GO

IF OBJECT_ID('dbo.MainErp_PumpSales_Post', 'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_PumpSales_Post;
GO
CREATE PROCEDURE dbo.MainErp_PumpSales_Post
    @TransactionId int,
    @UserId int = NULL,
    @ForceRebuild bit = 0,
    @DryRun bit = 0,
    @IncludeInventoryCost bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CorrelationId uniqueidentifier;
    SET @CorrelationId = NEWID();

    DECLARE
        @TransactionDate datetime,
        @BranchId int,
        @BoxId int,
        @StoreId int,
        @CusId int,
        @NoteId int,
        @NoteSerial nvarchar(100),
        @NoteSerial1 nvarchar(100),
        @ManualNo nvarchar(100),
        @CashAccount nvarchar(100),
        @SalesAccount nvarchar(100),
        @InventoryAccount nvarchar(100),
        @CostAccount nvarchar(100),
        @VatCreditAccount nvarchar(100),
        @VatDebitAccount nvarchar(100),
        @VoucherId bigint,
        @TotalValue decimal(18,4),
        @VatValue decimal(18,4),
        @SalesValue decimal(18,4),
        @InventoryCostValue decimal(18,4),
        @IssueTransactionId int,
        @IssueNoteId int,
        @IssueSerial nvarchar(100),
        @IssueNoteSerial nvarchar(100),
        @IssueVoucherSerial nvarchar(100);

    IF @TransactionId IS NULL OR @TransactionId <= 0
    BEGIN
        RAISERROR('TransactionId is required.', 16, 1);
        RETURN;
    END

    SELECT
        @TransactionDate = Transaction_Date,
        @BranchId = BranchId,
        @BoxId = BoxID,
        @StoreId = StoreID,
        @CusId = CusID,
        @NoteId = NoteId,
        @NoteSerial = NULLIF(LTRIM(RTRIM(NoteSerial)), ''),
        @NoteSerial1 = NULLIF(LTRIM(RTRIM(NoteSerial1)), ''),
        @ManualNo = ManualNO
    FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
    WHERE Transaction_ID = @TransactionId
      AND Transaction_Type = 21
      AND ISNULL(TypeInvoice, 0) = 2;

    IF @TransactionDate IS NULL
    BEGIN
        RAISERROR('Pump invoice was not found.', 16, 1);
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Transactions
        WHERE Transaction_ID = @TransactionId
          AND (ISNULL(Closed, 0) <> 0 OR ISNULL(Approved, 0) <> 0 OR ISNULL(Posted, 0) <> 0)
    )
    BEGIN
        RAISERROR('Closed, approved, or posted pump invoices cannot be posted from MainErp.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Transactions WHERE Transaction_ID = @TransactionId AND ISNULL(IsPosted, 0) <> 0) AND ISNULL(@ForceRebuild, 0) = 0
    BEGIN
        RAISERROR('Pump invoice is already posted. Use rebuild only after explicit review.', 16, 1);
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Transaction_Details
        WHERE Transaction_ID = @TransactionId
          AND ABS(ISNULL(CurrentQty, 0) - ISNULL(PrevQty, 0) - ISNULL(CashQty, 0) - ISNULL(MadaQty, 0) - ISNULL(VisaQty, 0) - ISNULL(DeferredQty, 0)) > 0.0001
    )
    BEGIN
        RAISERROR('Cannot post: one or more pump quantities are not fully distributed.', 16, 1);
        RETURN;
    END

    SELECT @CashAccount = NULLIF(LTRIM(RTRIM(Account_Code)), '')
    FROM dbo.TblBoxesData
    WHERE BoxID = @BoxId;

    SELECT TOP 1 @SalesAccount = NULLIF(LTRIM(RTRIM(a2)), '')
    FROM dbo.branches;

    SELECT TOP 1
        @InventoryAccount = NULLIF(LTRIM(RTRIM(a0)), ''),
        @CostAccount = NULLIF(LTRIM(RTRIM(a1)), '')
    FROM dbo.branches;

    SELECT TOP 1 @VatCreditAccount = NULLIF(LTRIM(RTRIM(AccCir)), '')
    FROM dbo.TblSettsReqLimK
    WHERE @TransactionDate BETWEEN RecordDate AND RecordDateTo
      AND ((AccOrTran = 1) OR (AccOrTran IS NULL))
      AND TransType = 21
    ORDER BY RecordDate DESC;

    SELECT TOP 1 @VatDebitAccount = NULLIF(LTRIM(RTRIM(AccDep)), '')
    FROM dbo.TblSettsReqLimK
    WHERE @TransactionDate BETWEEN RecordDate AND RecordDateTo
      AND AccDep IS NOT NULL
      AND LTRIM(RTRIM(AccDep)) <> ''
    ORDER BY CASE WHEN TransType = 8 THEN 0 ELSE 1 END, RecordDate DESC;

    IF @CashAccount IS NULL
    BEGIN
        RAISERROR('Cashbox account is missing.', 16, 1);
        RETURN;
    END

    IF @SalesAccount IS NULL
    BEGIN
        RAISERROR('Sales revenue account branches.a2 is missing.', 16, 1);
        RETURN;
    END

    IF @VatCreditAccount IS NULL
    BEGIN
        RAISERROR('VAT credit account for sales TransType=21 is missing.', 16, 1);
        RETURN;
    END

    SELECT @InventoryCostValue = SUM(ISNULL(CostPrice, 0) * ISNULL(Quantity, ISNULL(ShowQty, 0)))
    FROM dbo.Transaction_Details
    WHERE Transaction_ID = @TransactionId;

    SET @InventoryCostValue = ROUND(ISNULL(@InventoryCostValue, 0), 4);

    IF ISNULL(@IncludeInventoryCost, 0) = 1
       AND @InventoryCostValue <> 0
       AND (@InventoryAccount IS NULL OR @CostAccount IS NULL)
    BEGIN
        RAISERROR('Inventory cost posting was requested, but branches.a0 or branches.a1 is missing.', 16, 1);
        RETURN;
    END

    DECLARE @Entries TABLE
    (
        EntryLineNo int IDENTITY(1,1),
        AccountCode nvarchar(100) NOT NULL,
        Value decimal(18,4) NOT NULL,
        CreditOrDebit int NOT NULL,
        Description nvarchar(500) NULL
    );

    DECLARE @InvoiceLabel nvarchar(200);
    SET @InvoiceLabel = N'فاتورة بيع رقم ' + ISNULL(@NoteSerial1, CONVERT(nvarchar(50), @TransactionId));

    INSERT INTO @Entries (AccountCode, Value, CreditOrDebit, Description)
    SELECT @CashAccount, SUM(ISNULL(Cash, 0)), 0, @InvoiceLabel + N' نقدي'
    FROM dbo.Transaction_Details
    WHERE Transaction_ID = @TransactionId
    HAVING SUM(ISNULL(Cash, 0)) <> 0;

    DECLARE @CardRows TABLE
    (
        PaymentKind nvarchar(20),
        Value decimal(18,4),
        BankAccount nvarchar(100),
        CommissionAccount nvarchar(100),
        CommissionPercent decimal(18,6),
        PaymentName nvarchar(200)
    );

    INSERT INTO @CardRows (PaymentKind, Value, BankAccount, CommissionAccount, CommissionPercent, PaymentName)
    SELECT N'Visa', SUM(ISNULL(d.Visa, 0)), MAX(b.Account_Code), MAX(p.Accountcom), MAX(ISNULL(p.commision, 0)), MAX(p.PaymentName)
    FROM dbo.Transaction_Details d
    OUTER APPLY
    (
        SELECT TOP 1 p.*
        FROM dbo.TblSalesPayment sp
        INNER JOIN dbo.TblPaymentType p ON p.PaymentID = sp.PaymentID
        WHERE sp.TransID = @TransactionId AND p.PaymentName LIKE N'%فيزا%'
        ORDER BY sp.ID
    ) p
    LEFT JOIN dbo.BanksData b ON b.BankID = p.BankId
    WHERE d.Transaction_ID = @TransactionId
    HAVING SUM(ISNULL(d.Visa, 0)) <> 0;

    INSERT INTO @CardRows (PaymentKind, Value, BankAccount, CommissionAccount, CommissionPercent, PaymentName)
    SELECT N'Mada', SUM(ISNULL(d.Mada, 0)), MAX(b.Account_Code), MAX(p.Accountcom), MAX(ISNULL(p.commision, 0)), MAX(p.PaymentName)
    FROM dbo.Transaction_Details d
    OUTER APPLY
    (
        SELECT TOP 1 p.*
        FROM dbo.TblSalesPayment sp
        INNER JOIN dbo.TblPaymentType p ON p.PaymentID = sp.PaymentID
        WHERE sp.TransID = @TransactionId AND p.PaymentName LIKE N'%مدى%'
        ORDER BY sp.ID
    ) p
    LEFT JOIN dbo.BanksData b ON b.BankID = p.BankId
    WHERE d.Transaction_ID = @TransactionId
    HAVING SUM(ISNULL(d.Mada, 0)) <> 0;

    INSERT INTO @Entries (AccountCode, Value, CreditOrDebit, Description)
    SELECT BankAccount, Value, 0, @InvoiceLabel + N' ' + ISNULL(PaymentName, PaymentKind)
    FROM @CardRows
    WHERE Value <> 0 AND NULLIF(LTRIM(RTRIM(BankAccount)), '') IS NOT NULL;

    INSERT INTO @Entries (AccountCode, Value, CreditOrDebit, Description)
    SELECT CommissionAccount,
           ROUND((Value * CommissionPercent / 100.0) - ((Value * CommissionPercent / 100.0) / 1.15), 4),
           0,
           @InvoiceLabel + N' ' + ISNULL(PaymentName, PaymentKind) + N' عمولة'
    FROM @CardRows
    WHERE CommissionPercent <> 0 AND NULLIF(LTRIM(RTRIM(CommissionAccount)), '') IS NOT NULL;

    INSERT INTO @Entries (AccountCode, Value, CreditOrDebit, Description)
    SELECT @VatDebitAccount,
           ROUND((Value * CommissionPercent / 100.0) / 1.15, 4),
           0,
           @InvoiceLabel + N' ' + ISNULL(PaymentName, PaymentKind) + N' عمولة'
    FROM @CardRows
    WHERE CommissionPercent <> 0 AND @VatDebitAccount IS NOT NULL;

    INSERT INTO @Entries (AccountCode, Value, CreditOrDebit, Description)
    SELECT BankAccount,
           ROUND(Value * CommissionPercent / 100.0, 4),
           1,
           @InvoiceLabel + N' ' + ISNULL(PaymentName, PaymentKind) + N' عمولة'
    FROM @CardRows
    WHERE CommissionPercent <> 0 AND NULLIF(LTRIM(RTRIM(BankAccount)), '') IS NOT NULL;

    INSERT INTO @Entries (AccountCode, Value, CreditOrDebit, Description)
    SELECT c.Account_Code, SUM(ISNULL(dp.Amount, 0)), 0, @InvoiceLabel + N' آجل - ' + MAX(c.CusName)
    FROM dbo.Transaction_DetailsPump dp
    INNER JOIN dbo.TblCustemers c ON c.CusID = dp.CusID
    WHERE dp.Transaction_ID = @TransactionId
    GROUP BY c.Account_Code
    HAVING SUM(ISNULL(dp.Amount, 0)) <> 0 AND NULLIF(LTRIM(RTRIM(c.Account_Code)), '') IS NOT NULL;

    SELECT @TotalValue = SUM(ISNULL(Cash,0) + ISNULL(Mada,0) + ISNULL(Visa,0) + ISNULL(Deferred,0))
    FROM dbo.Transaction_Details
    WHERE Transaction_ID = @TransactionId;

    SET @TotalValue = ISNULL(@TotalValue, 0);
    SET @VatValue = ROUND(@TotalValue - (@TotalValue / 1.15), 4);
    SET @SalesValue = ROUND(@TotalValue - @VatValue, 4);

    INSERT INTO @Entries (AccountCode, Value, CreditOrDebit, Description)
    VALUES (@SalesAccount, @SalesValue, 1, @InvoiceLabel);

    IF @VatValue <> 0
    BEGIN
        INSERT INTO @Entries (AccountCode, Value, CreditOrDebit, Description)
        VALUES (@VatCreditAccount, @VatValue, 1, N'قيمة مضافة ' + @InvoiceLabel);
    END

    IF ISNULL(@IncludeInventoryCost, 0) = 1 AND @InventoryCostValue <> 0
    BEGIN
        INSERT INTO @Entries (AccountCode, Value, CreditOrDebit, Description)
        VALUES (@CostAccount, @InventoryCostValue, 0, N'تكلفة مبيعات ' + @InvoiceLabel);

        INSERT INTO @Entries (AccountCode, Value, CreditOrDebit, Description)
        VALUES (@InventoryAccount, @InventoryCostValue, 1, N'صرف مخزون ' + @InvoiceLabel);
    END

    IF EXISTS (SELECT 1 FROM @Entries WHERE AccountCode IS NULL OR LTRIM(RTRIM(AccountCode)) = '')
    BEGIN
        RAISERROR('Cannot post: one or more voucher accounts are missing.', 16, 1);
        RETURN;
    END

    IF ABS((SELECT ISNULL(SUM(CASE WHEN CreditOrDebit = 0 THEN Value ELSE -Value END), 0) FROM @Entries)) > 0.05
    BEGIN
        RAISERROR('Cannot post: generated voucher is not balanced.', 16, 1);
        RETURN;
    END

    IF @DryRun = 1
    BEGIN
        SELECT
            @TransactionId AS TransactionId,
            COUNT(1) AS VoucherLineCount,
            SUM(CASE WHEN CreditOrDebit = 0 THEN Value ELSE 0 END) AS DebitTotal,
            SUM(CASE WHEN CreditOrDebit = 1 THEN Value ELSE 0 END) AS CreditTotal,
            @IncludeInventoryCost AS IncludeInventoryCost,
            @InventoryCostValue AS InventoryCostValue,
            @CostAccount AS CostAccount,
            @InventoryAccount AS InventoryAccount,
            CAST(N'Pump posting preview succeeded. No database write was executed.' AS nvarchar(300)) AS ResultMessage
        FROM @Entries;

        SELECT EntryLineNo, AccountCode, Value, CreditOrDebit, Description
        FROM @Entries
        ORDER BY EntryLineNo;
        RETURN;
    END

    BEGIN TRANSACTION;

    IF @NoteId IS NULL OR @NoteId = 0
    BEGIN
        SELECT @NoteId = ISNULL(MAX(NoteID), 0) + 1 FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK);
    END

    IF @NoteSerial IS NULL
    BEGIN
        SELECT @NoteSerial = CONVERT(nvarchar(50), ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial) = 1 THEN CONVERT(bigint, CONVERT(float, NoteSerial)) ELSE 0 END), 0) + 1)
        FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK);
    END

    IF @NoteSerial1 IS NULL
    BEGIN
        SELECT @NoteSerial1 = CONVERT(nvarchar(50), ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial1) = 1 THEN CONVERT(bigint, CONVERT(float, NoteSerial1)) ELSE 0 END), 0) + 1)
        FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK);
    END

    IF EXISTS (SELECT 1 FROM dbo.Notes WHERE NoteID = @NoteId)
    BEGIN
        UPDATE dbo.Notes
        SET NoteDate = @TransactionDate,
            NoteType = 170,
            NoteSerial = @NoteSerial,
            NoteSerial1 = @NoteSerial1,
            Note_Value = 0,
            Transaction_ID = @TransactionId,
            ManualNo = @ManualNo
        WHERE NoteID = @NoteId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Notes (NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, Transaction_ID, ManualNo)
        VALUES (@NoteId, @TransactionDate, 170, @NoteSerial, @NoteSerial1, 0, @TransactionId, @ManualNo);
    END

    DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS
    WHERE Notes_ID = @NoteId OR Transaction_ID = @TransactionId;

    SELECT @VoucherId = ISNULL(MAX(Double_Entry_Vouchers_ID), 0) + 1
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (TABLOCKX, HOLDLOCK);

    INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
    (
        Double_Entry_Vouchers_ID,
        DEV_ID_Line_No,
        Account_Code,
        Value,
        Credit_Or_Debit,
        Double_Entry_Vouchers_Description,
        Notes_ID,
        RecordDate,
        Transaction_ID,
        UserID,
        branch_id
    )
    SELECT
        @VoucherId,
        EntryLineNo,
        AccountCode,
        Value,
        CreditOrDebit,
        Description,
        @NoteId,
        @TransactionDate,
        @TransactionId,
        @UserId,
        @BranchId
    FROM @Entries
    WHERE ABS(Value) > 0.0001
    ORDER BY EntryLineNo;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions
        WHERE Transaction_Type = 19
          AND LTRIM(RTRIM(CONVERT(nvarchar(100), nots))) = CONVERT(nvarchar(100), @TransactionId)
    )
    BEGIN
        SELECT @IssueTransactionId = ISNULL(MAX(Transaction_ID), 0) + 1
        FROM dbo.Transactions WITH (TABLOCKX, HOLDLOCK);

        SELECT @IssueSerial = CONVERT(nvarchar(50), ISNULL(MAX(CASE WHEN ISNUMERIC(Transaction_Serial) = 1 THEN CONVERT(bigint, CONVERT(float, Transaction_Serial)) ELSE 0 END), 0) + 1)
        FROM dbo.Transactions WITH (TABLOCKX, HOLDLOCK)
        WHERE Transaction_Type = 19;

        SELECT @IssueNoteId = ISNULL(MAX(NoteID), 0) + 1
        FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK);

        SET @IssueNoteSerial = @NoteSerial;
        SET @IssueVoucherSerial = @NoteSerial1;

        INSERT INTO dbo.Notes (NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, Transaction_ID, ManualNo)
        VALUES (@IssueNoteId, @TransactionDate, 180, @IssueNoteSerial, @IssueVoucherSerial, 0, @IssueTransactionId, @ManualNo);

        INSERT INTO dbo.Transactions
        (
            Transaction_ID, Transaction_Serial, Transaction_Date, Transaction_Type,
            CusID, StoreID, UserID, nots, nots2, NoteSerial, NoteSerial1, NoteId,
            BranchId, Closed, ManualNO, CashCustomerName, TypeInvoice
        )
        SELECT
            @IssueTransactionId, @IssueSerial, Transaction_Date, 19,
            CusID, StoreID, @UserId, CONVERT(nvarchar(100), @TransactionId), @NoteSerial1,
            @IssueNoteSerial, @IssueVoucherSerial, @IssueNoteId,
            BranchId, 1, ManualNO, CashCustomerName, TypeInvoice
        FROM dbo.Transactions
        WHERE Transaction_ID = @TransactionId;

        INSERT INTO dbo.Transaction_Details
        (
            Transaction_ID, Item_ID, UnitId, StoreID2, Quantity, Price,
            ColorID, ItemSize, ClassId, ShowQty, BranchId, CostPrice
        )
        SELECT
            @IssueTransactionId, Item_ID, UnitId, StoreID2,
            ISNULL(Quantity, ISNULL(ShowQty, 0)),
            ISNULL(CostPrice, ISNULL(Price, 0)),
            ISNULL(ColorID, 1), ISNULL(ItemSize, 1), ISNULL(ClassId, 1),
            ISNULL(ShowQty, ISNULL(Quantity, 0)),
            @BranchId,
            CostPrice
        FROM dbo.Transaction_Details
        WHERE Transaction_ID = @TransactionId
          AND ISNULL(Item_ID, 0) <> 0;
    END

    UPDATE dbo.Transactions
    SET NoteId = @NoteId,
        NoteSerial = @NoteSerial,
        NoteSerial1 = @NoteSerial1,
        IsPosted = 1,
        UserPosted = @UserId
    WHERE Transaction_ID = @TransactionId;

    INSERT INTO dbo.MainErp_AuditLog (OperationName, EntityName, EntityKey, UserId, CorrelationId, Message)
    VALUES
    (
        CASE WHEN ISNULL(@IncludeInventoryCost, 0) = 1 THEN N'PumpSales.PostWithCost' ELSE N'PumpSales.Post' END,
        N'Transactions',
        CONVERT(nvarchar(100), @TransactionId),
        @UserId,
        @CorrelationId,
        N'Pump invoice posted from MainErp migration. IncludeInventoryCost=' + CONVERT(nvarchar(10), @IncludeInventoryCost) + N', InventoryCostValue=' + CONVERT(nvarchar(50), @InventoryCostValue)
    );

    COMMIT TRANSACTION;

    SELECT
        @TransactionId AS TransactionId,
        @NoteId AS NoteId,
        @VoucherId AS VoucherId,
        @IssueTransactionId AS IssueTransactionId,
        @CorrelationId AS CorrelationId,
        CAST(CASE WHEN ISNULL(@IncludeInventoryCost, 0) = 1
             THEN N'Pump invoice posted with inventory cost entries. Notes, DOUBLE_ENTREY_VOUCHERS, issue voucher, and audit were created.'
             ELSE N'Pump invoice posted. Notes, DOUBLE_ENTREY_VOUCHERS, issue voucher, and audit were created.'
        END AS nvarchar(300)) AS ResultMessage;
END
GO

IF OBJECT_ID('dbo.MainErp_PumpSales_DeleteDraft', 'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_PumpSales_DeleteDraft;
GO
CREATE PROCEDURE dbo.MainErp_PumpSales_DeleteDraft
    @TransactionId int,
    @UserId int = NULL,
    @DryRun bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CorrelationId uniqueidentifier;
    SET @CorrelationId = NEWID();

    IF @TransactionId IS NULL OR @TransactionId <= 0
    BEGIN
        RAISERROR('TransactionId is required.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions
        WHERE Transaction_ID = @TransactionId
          AND Transaction_Type = 21
          AND ISNULL(TypeInvoice, 0) = 2
    )
    BEGIN
        RAISERROR('Pump invoice was not found.', 16, 1);
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Transactions
        WHERE Transaction_ID = @TransactionId
          AND (
                ISNULL(Closed, 0) <> 0
             OR ISNULL(Posted, 0) <> 0
             OR ISNULL(Approved, 0) <> 0
             OR ISNULL(IsPosted, 0) <> 0
          )
    )
    BEGIN
        RAISERROR('Only unposted, unapproved, open pump drafts can be deleted.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Notes WHERE Transaction_ID = @TransactionId)
       OR EXISTS (SELECT 1 FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Transaction_ID = @TransactionId OR Transaction_ID1 = @TransactionId)
       OR EXISTS
          (
              SELECT 1
              FROM dbo.Transactions
              WHERE Transaction_Type IN (19, 20)
                AND LTRIM(RTRIM(CONVERT(nvarchar(100), nots))) = CONVERT(nvarchar(100), @TransactionId)
          )
    BEGIN
        RAISERROR('This pump invoice already has linked notes, vouchers, or inventory documents. Use a reviewed cancel/reversal workflow, not draft delete.', 16, 1);
        RETURN;
    END

    DECLARE @LineCount int;
    DECLARE @PaymentCount int;
    DECLARE @DeferredCount int;
    DECLARE @PumpReadingRollbackCount int;

    SELECT @LineCount = COUNT(1)
    FROM dbo.Transaction_Details
    WHERE Transaction_ID = @TransactionId;

    SELECT @PaymentCount = COUNT(1)
    FROM dbo.TblSalesPayment
    WHERE TransID = @TransactionId;

    SELECT @DeferredCount = COUNT(1)
    FROM dbo.Transaction_DetailsPump
    WHERE Transaction_ID = @TransactionId;

    SELECT @PumpReadingRollbackCount = COUNT(1)
    FROM dbo.Transaction_Details d
    INNER JOIN dbo.tblPumpType p ON p.ID = d.PumpId
    WHERE d.Transaction_ID = @TransactionId
      AND d.PumpId IS NOT NULL
      AND ABS(ISNULL(p.PercentV, 0) - ISNULL(d.CurrentQty, 0)) <= 0.0001;

    IF @DryRun = 1
    BEGIN
        SELECT
            @TransactionId AS TransactionId,
            @LineCount AS LineCount,
            @PaymentCount AS PaymentCount,
            @DeferredCount AS DeferredCount,
            @PumpReadingRollbackCount AS PumpReadingRollbackCount,
            CAST(N'Draft delete preview only. No database write was executed.' AS nvarchar(300)) AS ResultMessage;
        RETURN;
    END

    BEGIN TRANSACTION;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
        WHERE Transaction_ID = @TransactionId
          AND Transaction_Type = 21
          AND ISNULL(TypeInvoice, 0) = 2
          AND (
                ISNULL(Closed, 0) <> 0
             OR ISNULL(Posted, 0) <> 0
             OR ISNULL(Approved, 0) <> 0
             OR ISNULL(IsPosted, 0) <> 0
          )
    )
    BEGIN
        RAISERROR('Only unposted, unapproved, open pump drafts can be deleted.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    UPDATE p
    SET p.PercentV = d.PrevQty
    FROM dbo.tblPumpType p
    INNER JOIN dbo.Transaction_Details d ON d.PumpId = p.ID
    WHERE d.Transaction_ID = @TransactionId
      AND d.PumpId IS NOT NULL
      AND ABS(ISNULL(p.PercentV, 0) - ISNULL(d.CurrentQty, 0)) <= 0.0001;

    DELETE FROM dbo.Transaction_DetailsPump WHERE Transaction_ID = @TransactionId;
    DELETE FROM dbo.TblSalesPayment WHERE TransID = @TransactionId;
    DELETE FROM dbo.Transaction_Details WHERE Transaction_ID = @TransactionId;
    DELETE FROM dbo.Transactions
    WHERE Transaction_ID = @TransactionId
      AND Transaction_Type = 21
      AND ISNULL(TypeInvoice, 0) = 2
      AND ISNULL(Closed, 0) = 0
      AND ISNULL(Posted, 0) = 0
      AND ISNULL(Approved, 0) = 0
      AND ISNULL(IsPosted, 0) = 0;

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('Pump draft delete failed because the invoice status changed.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    INSERT INTO dbo.MainErp_AuditLog (OperationName, EntityName, EntityKey, UserId, CorrelationId, Message)
    VALUES (N'PumpSales.DeleteDraft', N'Transactions', CONVERT(nvarchar(100), @TransactionId), @UserId, @CorrelationId, N'Unposted pump draft deleted from MainErp migration.');

    COMMIT TRANSACTION;

    SELECT
        @TransactionId AS TransactionId,
        @LineCount AS LineCount,
        @PaymentCount AS PaymentCount,
        @DeferredCount AS DeferredCount,
        @PumpReadingRollbackCount AS PumpReadingRollbackCount,
        @CorrelationId AS CorrelationId,
        CAST(N'Pump draft deleted. No Notes or DOUBLE_ENTREY_VOUCHERS existed for this draft.' AS nvarchar(300)) AS ResultMessage;
END
GO

IF OBJECT_ID('dbo.MainErp_PumpSales_CancelPreview', 'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_PumpSales_CancelPreview;
GO
CREATE PROCEDURE dbo.MainErp_PumpSales_CancelPreview
    @TransactionId int,
    @UserId int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TransactionId IS NULL OR @TransactionId <= 0
    BEGIN
        RAISERROR('TransactionId is required.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions
        WHERE Transaction_ID = @TransactionId
          AND Transaction_Type = 21
          AND ISNULL(TypeInvoice, 0) = 2
    )
    BEGIN
        RAISERROR('Pump invoice was not found.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions
        WHERE Transaction_ID = @TransactionId
          AND ISNULL(IsPosted, 0) <> 0
    )
    BEGIN
        RAISERROR('Only posted pump invoices can use cancellation/reversal preview.', 16, 1);
        RETURN;
    END

    DECLARE @NoteId int;
    DECLARE @BranchId int;
    DECLARE @InventoryAccount nvarchar(100);
    DECLARE @CostAccount nvarchar(100);
    DECLARE @RevenueAccount nvarchar(100);
    DECLARE @CostValue decimal(18,4);
    DECLARE @DebitTotal decimal(18,4);
    DECLARE @CreditTotal decimal(18,4);
    DECLARE @IssueCount int;

    SELECT @NoteId = NoteId, @BranchId = BranchId
    FROM dbo.Transactions
    WHERE Transaction_ID = @TransactionId;

    SELECT TOP 1
        @InventoryAccount = NULLIF(LTRIM(RTRIM(a0)), ''),
        @CostAccount = NULLIF(LTRIM(RTRIM(a1)), ''),
        @RevenueAccount = NULLIF(LTRIM(RTRIM(a2)), '')
    FROM dbo.branches;

    SELECT @CostValue = SUM(ISNULL(CostPrice, 0) * ISNULL(Quantity, ISNULL(ShowQty, 0)))
    FROM dbo.Transaction_Details
    WHERE Transaction_ID = @TransactionId;

    SELECT
        @DebitTotal = SUM(CASE WHEN Credit_Or_Debit = 0 THEN ISNULL(Value, 0) ELSE 0 END),
        @CreditTotal = SUM(CASE WHEN Credit_Or_Debit = 1 THEN ISNULL(Value, 0) ELSE 0 END)
    FROM dbo.DOUBLE_ENTREY_VOUCHERS
    WHERE Transaction_ID = @TransactionId
       OR Transaction_ID1 = @TransactionId
       OR (@NoteId IS NOT NULL AND Notes_ID = @NoteId);

    SELECT @IssueCount = COUNT(1)
    FROM dbo.Transactions
    WHERE Transaction_Type = 19
      AND LTRIM(RTRIM(CONVERT(nvarchar(100), nots))) = CONVERT(nvarchar(100), @TransactionId);

    SELECT
        @TransactionId AS TransactionId,
        @NoteId AS SourceNoteId,
        ISNULL(@DebitTotal, 0) AS SourceDebitTotal,
        ISNULL(@CreditTotal, 0) AS SourceCreditTotal,
        ISNULL(@CostValue, 0) AS EstimatedCostValue,
        @InventoryAccount AS InventoryAccount,
        @CostAccount AS CostAccount,
        @RevenueAccount AS RevenueAccount,
        @IssueCount AS LinkedIssueVoucherCount,
        CAST(N'Cancellation preview only. A future approved workflow should create reversal Notes/DOUBLE_ENTREY_VOUCHERS and a reverse inventory document instead of deleting the posted invoice.' AS nvarchar(500)) AS ResultMessage;

    SELECT
        v.Double_Entry_Vouchers_ID,
        v.DEV_ID_Line_No,
        v.Notes_ID,
        v.Account_Code,
        a.Account_Serial,
        COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName,
        CASE WHEN v.Credit_Or_Debit = 0 THEN 1 ELSE 0 END AS ReversalCreditOrDebit,
        ISNULL(v.Value, 0) AS ReversalValue,
        CAST(N'Reversal preview for pump invoice cancellation' AS nvarchar(300)) AS ReversalDescription
    FROM dbo.DOUBLE_ENTREY_VOUCHERS v
    LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = v.Account_Code
    WHERE v.Transaction_ID = @TransactionId
       OR v.Transaction_ID1 = @TransactionId
       OR (@NoteId IS NOT NULL AND v.Notes_ID = @NoteId)
    ORDER BY v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No;

    SELECT
        d.ID,
        d.Item_ID,
        i.ItemName,
        d.UnitId,
        d.StoreID2,
        d.Quantity,
        d.ShowQty,
        d.CostPrice,
        ISNULL(d.CostPrice, 0) * ISNULL(d.Quantity, ISNULL(d.ShowQty, 0)) AS EstimatedLineCost,
        CAST(N'Reverse inventory candidate. Final implementation must create a reviewed receive/reversal document, not mutate the posted issue voucher.' AS nvarchar(300)) AS PreviewNote
    FROM dbo.Transaction_Details d
    LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
    WHERE d.Transaction_ID = @TransactionId
      AND ISNULL(d.Item_ID, 0) <> 0
    ORDER BY d.ID;
END
GO

IF OBJECT_ID('dbo.MainErp_PumpSales_CancelPosted', 'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_PumpSales_CancelPosted;
GO
CREATE PROCEDURE dbo.MainErp_PumpSales_CancelPosted
    @TransactionId int,
    @UserId int = NULL,
    @DryRun bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CorrelationId uniqueidentifier;
    SET @CorrelationId = NEWID();

    DECLARE
        @TransactionDate datetime,
        @BranchId int,
        @StoreId int,
        @CusId int,
        @BoxId int,
        @SourceNoteId int,
        @CancelNoteId int,
        @CancelNoteSerial nvarchar(100),
        @CancelNoteSerial1 nvarchar(100),
        @ManualNo nvarchar(100),
        @CancelVoucherId bigint,
        @ReceiveTransactionId int,
        @ReceiveSerial nvarchar(100),
        @ReceiveNoteId int,
        @ReceiveLineCount int,
        @VoucherLineCount int,
        @DebitTotal decimal(18,4),
        @CreditTotal decimal(18,4);

    IF @TransactionId IS NULL OR @TransactionId <= 0
    BEGIN
        RAISERROR('TransactionId is required.', 16, 1);
        RETURN;
    END

    SELECT
        @TransactionDate = Transaction_Date,
        @BranchId = BranchId,
        @StoreId = StoreID,
        @CusId = CusID,
        @BoxId = BoxID,
        @SourceNoteId = NoteId,
        @ManualNo = ManualNO
    FROM dbo.Transactions
    WHERE Transaction_ID = @TransactionId
      AND Transaction_Type = 21
      AND ISNULL(TypeInvoice, 0) = 2;

    IF @TransactionDate IS NULL
    BEGIN
        RAISERROR('Pump invoice was not found.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Transactions WHERE Transaction_ID = @TransactionId AND ISNULL(IsPosted, 0) <> 0)
    BEGIN
        RAISERROR('Only posted pump invoices can be cancelled from this workflow.', 16, 1);
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Transactions
        WHERE Transaction_Type = 20
          AND LTRIM(RTRIM(CONVERT(nvarchar(100), nots))) = CONVERT(nvarchar(100), @TransactionId)
    )
    BEGIN
        RAISERROR('This pump invoice already has a linked receive/reversal transaction.', 16, 1);
        RETURN;
    END

    IF @SourceNoteId IS NULL OR @SourceNoteId = 0
    BEGIN
        RAISERROR('Cannot cancel: source posted invoice has no NoteId.', 16, 1);
        RETURN;
    END

    SELECT
        @VoucherLineCount = COUNT(1),
        @DebitTotal = SUM(CASE WHEN Credit_Or_Debit = 0 THEN ISNULL(Value, 0) ELSE 0 END),
        @CreditTotal = SUM(CASE WHEN Credit_Or_Debit = 1 THEN ISNULL(Value, 0) ELSE 0 END)
    FROM dbo.DOUBLE_ENTREY_VOUCHERS
    WHERE Transaction_ID = @TransactionId
       OR Transaction_ID1 = @TransactionId
       OR Notes_ID = @SourceNoteId;

    SELECT @ReceiveLineCount = COUNT(1)
    FROM dbo.Transaction_Details
    WHERE Transaction_ID = @TransactionId
      AND ISNULL(Item_ID, 0) <> 0;

    IF ISNULL(@VoucherLineCount, 0) = 0
    BEGIN
        RAISERROR('Cannot cancel: source posted invoice has no voucher lines to reverse.', 16, 1);
        RETURN;
    END

    IF ABS(ISNULL(@DebitTotal, 0) - ISNULL(@CreditTotal, 0)) > 0.05
    BEGIN
        RAISERROR('Cannot cancel: source voucher is not balanced.', 16, 1);
        RETURN;
    END

    IF @DryRun = 1
    BEGIN
        SELECT
            @TransactionId AS TransactionId,
            @SourceNoteId AS SourceNoteId,
            @VoucherLineCount AS ReversalVoucherLineCount,
            ISNULL(@DebitTotal, 0) AS SourceDebitTotal,
            ISNULL(@CreditTotal, 0) AS SourceCreditTotal,
            @ReceiveLineCount AS ReceiveLineCount,
            CAST(N'Pump posted cancellation preview succeeded. No database write was executed.' AS nvarchar(300)) AS ResultMessage;

        SELECT
            v.DEV_ID_Line_No AS SourceLineNo,
            v.Account_Code,
            CASE WHEN v.Credit_Or_Debit = 0 THEN 1 ELSE 0 END AS ReversalCreditOrDebit,
            ISNULL(v.Value, 0) AS ReversalValue,
            N'عكس ' + ISNULL(v.Double_Entry_Vouchers_Description, N'فاتورة مضخات') AS ReversalDescription
        FROM dbo.DOUBLE_ENTREY_VOUCHERS v
        WHERE v.Transaction_ID = @TransactionId
           OR v.Transaction_ID1 = @TransactionId
           OR v.Notes_ID = @SourceNoteId
        ORDER BY v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No;
        RETURN;
    END

    BEGIN TRANSACTION;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
        WHERE Transaction_Type = 20
          AND LTRIM(RTRIM(CONVERT(nvarchar(100), nots))) = CONVERT(nvarchar(100), @TransactionId)
    )
    BEGIN
        RAISERROR('This pump invoice already has a linked receive/reversal transaction.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    SELECT @CancelNoteId = ISNULL(MAX(NoteID), 0) + 1 FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK);

    SELECT @CancelNoteSerial = CONVERT(nvarchar(50), ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial) = 1 THEN CONVERT(bigint, CONVERT(float, NoteSerial)) ELSE 0 END), 0) + 1)
    FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK);

    SELECT @CancelNoteSerial1 = CONVERT(nvarchar(50), ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial1) = 1 THEN CONVERT(bigint, CONVERT(float, NoteSerial1)) ELSE 0 END), 0) + 1)
    FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK);

    INSERT INTO dbo.Notes (NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, Transaction_ID, ManualNo)
    VALUES (@CancelNoteId, GETDATE(), 171, @CancelNoteSerial, @CancelNoteSerial1, 0, @TransactionId, @ManualNo);

    SELECT @CancelVoucherId = ISNULL(MAX(Double_Entry_Vouchers_ID), 0) + 1
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (TABLOCKX, HOLDLOCK);

    INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
    (
        Double_Entry_Vouchers_ID,
        DEV_ID_Line_No,
        Account_Code,
        Value,
        Credit_Or_Debit,
        Double_Entry_Vouchers_Description,
        Notes_ID,
        RecordDate,
        Transaction_ID,
        Transaction_ID1,
        UserID,
        branch_id
    )
    SELECT
        @CancelVoucherId,
        ROW_NUMBER() OVER (ORDER BY v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No),
        v.Account_Code,
        ISNULL(v.Value, 0),
        CASE WHEN v.Credit_Or_Debit = 0 THEN 1 ELSE 0 END,
        N'عكس ' + ISNULL(v.Double_Entry_Vouchers_Description, N'فاتورة مضخات'),
        @CancelNoteId,
        GETDATE(),
        @TransactionId,
        @TransactionId,
        @UserId,
        @BranchId
    FROM dbo.DOUBLE_ENTREY_VOUCHERS v
    WHERE v.Transaction_ID = @TransactionId
       OR v.Transaction_ID1 = @TransactionId
       OR v.Notes_ID = @SourceNoteId
    ORDER BY v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No;

    SELECT @ReceiveTransactionId = ISNULL(MAX(Transaction_ID), 0) + 1
    FROM dbo.Transactions WITH (TABLOCKX, HOLDLOCK);

    SELECT @ReceiveSerial = CONVERT(nvarchar(50), ISNULL(MAX(CASE WHEN ISNUMERIC(Transaction_Serial) = 1 THEN CONVERT(bigint, CONVERT(float, Transaction_Serial)) ELSE 0 END), 0) + 1)
    FROM dbo.Transactions WITH (TABLOCKX, HOLDLOCK)
    WHERE Transaction_Type = 20;

    SELECT @ReceiveNoteId = ISNULL(MAX(NoteID), 0) + 1
    FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK);

    INSERT INTO dbo.Transactions
    (
        Transaction_ID, Transaction_Serial, Transaction_Date, Transaction_Type,
        CusID, StoreID, UserID, nots, nots2, NoteSerial, NoteSerial1, NoteId,
        BranchId, Closed, ManualNO, CashCustomerName, TypeInvoice
    )
    SELECT
        @ReceiveTransactionId, @ReceiveSerial, GETDATE(), 20,
        CusID, StoreID, @UserId, CONVERT(nvarchar(100), @TransactionId), @CancelNoteSerial1,
        @CancelNoteSerial, @CancelNoteSerial1, NULL,
        BranchId, 1, ManualNO, CashCustomerName, TypeInvoice
    FROM dbo.Transactions
    WHERE Transaction_ID = @TransactionId;

    INSERT INTO dbo.Notes (NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, Transaction_ID, ManualNo)
    VALUES (@ReceiveNoteId, GETDATE(), 181, @CancelNoteSerial, @CancelNoteSerial1, 0, @ReceiveTransactionId, @ManualNo);

    UPDATE dbo.Transactions
    SET NoteId = @ReceiveNoteId
    WHERE Transaction_ID = @ReceiveTransactionId;

    INSERT INTO dbo.Transaction_Details
    (
        Transaction_ID, Item_ID, UnitId, StoreID2, Quantity, Price,
        ColorID, ItemSize, ClassId, ShowQty, BranchId, CostPrice
    )
    SELECT
        @ReceiveTransactionId, Item_ID, UnitId, StoreID2,
        ISNULL(Quantity, ISNULL(ShowQty, 0)),
        ISNULL(CostPrice, ISNULL(Price, 0)),
        ISNULL(ColorID, 1), ISNULL(ItemSize, 1), ISNULL(ClassId, 1),
        ISNULL(ShowQty, ISNULL(Quantity, 0)),
        @BranchId,
        CostPrice
    FROM dbo.Transaction_Details
    WHERE Transaction_ID = @TransactionId
      AND ISNULL(Item_ID, 0) <> 0;

    UPDATE dbo.Transactions
    SET Closed = 1
    WHERE Transaction_ID = @TransactionId;

    INSERT INTO dbo.MainErp_AuditLog (OperationName, EntityName, EntityKey, UserId, CorrelationId, Message, BeforeSnapshot, AfterSnapshot)
    VALUES
    (
        N'PumpSales.CancelPosted',
        N'Transactions',
        CONVERT(nvarchar(100), @TransactionId),
        @UserId,
        @CorrelationId,
        N'Posted pump invoice cancelled by reversal voucher and Transaction_Type=20 receive document.',
        N'SourceNoteId=' + CONVERT(nvarchar(50), @SourceNoteId) + N'; VoucherLines=' + CONVERT(nvarchar(50), @VoucherLineCount),
        N'CancelNoteId=' + CONVERT(nvarchar(50), @CancelNoteId) + N'; ReceiveTransactionId=' + CONVERT(nvarchar(50), @ReceiveTransactionId)
    );

    COMMIT TRANSACTION;

    SELECT
        @TransactionId AS TransactionId,
        @CancelNoteId AS CancelNoteId,
        @CancelVoucherId AS CancelVoucherId,
        @ReceiveTransactionId AS ReceiveTransactionId,
        @CorrelationId AS CorrelationId,
        CAST(N'Pump posted invoice cancelled. Reversal voucher and Transaction_Type=20 receive document were created.' AS nvarchar(300)) AS ResultMessage;
END
GO

IF OBJECT_ID('dbo.MainErp_SalesInvoice_GetDetails', 'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_SalesInvoice_GetDetails;
GO
CREATE PROCEDURE dbo.MainErp_SalesInvoice_GetDetails
    @TypeInvoice int,
    @TransactionId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        t.Transaction_ID,
        t.Transaction_Serial,
        t.NoteSerial,
        t.NoteSerial1,
        t.ManualNO,
        t.Transaction_Date,
        t.Transaction_Type,
        ISNULL(t.TypeInvoice, 0) AS TypeInvoice,
        t.CusID,
        t.BranchId,
        COALESCE(c.CusName, t.CashCustomerName) AS CustomerName,
        t.CashCustomerName,
        b.branch_name AS BranchName,
        CONVERT(nvarchar(50), t.StoreID) AS StoreName,
        t.Total,
        t.NetValue,
        t.VAT,
        t.PayedValue,
        t.RemainValue,
        t.NoteId,
        t.Closed,
        t.Posted,
        t.Approved,
        t.IsPosted,
        t.UserPosted,
        t.Prefix,
        t.Fullcode,
        t.CBoBasedON,
        t.POSBillType,
        t.Transaction_NetValue,
        t.SumValueLine,
        t.SumVATLine,
        t.DateRec,
        COALESCE(t.TransactionComment, t.remark, t.Nots) AS Remarks,
        t.PaymentType,
        t.Currency_id,
        t.Currency_rate,
        t.order_no,
        t.StoreID,
        t.BoxID,
        t.PaymentNetid
    FROM dbo.Transactions t
    LEFT JOIN dbo.TblCustemers c ON t.CusID = c.CusID
    LEFT JOIN dbo.TblBranchesData b ON t.BranchId = b.branch_id
    WHERE t.Transaction_ID = @TransactionId
      AND t.Transaction_Type IN (21, 42, 38, 9)
      AND ((@TypeInvoice = 2 AND ISNULL(t.TypeInvoice, 0) = 2) OR (@TypeInvoice = 1 AND ISNULL(t.TypeInvoice, 0) <> 2));

    SELECT
        d.ID,
        d.LineID AS DetailLineNo,
        d.Item_ID,
        i.ItemCode,
        COALESCE(i.ItemName, i.ItemNamee) AS ItemName,
        d.UnitId,
        d.StoreID2,
        u.UnitName,
        d.ShowQty,
        d.Quantity,
        d.ShowPrice,
        d.Price,
        d.CostPrice,
        d.discountvalue,
        d.TotalDiscountPerLine,
        d.Vat,
        d.Vatyo,
        d.Remarks,
        COALESCE(d.AccountCode, d.Account_Code) AS AccountCode,
        a.Account_Serial,
        COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName,
        d.Account_CodeComm,
        ac.Account_Serial AS CommissionAccountSerial,
        COALESCE(ac.Account_Name, ac.Account_NameEng) AS CommissionAccountName,
        d.PumpId,
        p.Name AS PumpName,
        d.IsOther,
        d.ColorID,
        d.PrevQty,
        d.CurrentQty,
        d.Cash,
        d.Mada,
        d.Visa,
        d.Deferred,
        d.CashQty,
        d.MadaQty,
        d.VisaQty,
        d.DeferredQty,
        d.AmountH,
        d.AmountHComm,
        CONVERT(nvarchar(max), d.DetailsPump) AS DetailsPump
    FROM dbo.Transaction_Details d
    LEFT JOIN dbo.TblItems i ON d.Item_ID = i.ItemID
    LEFT JOIN dbo.TblUnites u ON d.UnitId = u.UnitID
    LEFT JOIN dbo.tblPumpType p ON d.PumpId = p.ID
    LEFT JOIN dbo.ACCOUNTS a ON COALESCE(d.AccountCode, d.Account_Code) = a.Account_Code
    LEFT JOIN dbo.ACCOUNTS ac ON d.Account_CodeComm = ac.Account_Code
    WHERE d.Transaction_ID = @TransactionId
    ORDER BY d.ID;

    SELECT ID, TransID, PaymentID, Value, CardNo, MaxValue
    FROM dbo.TblSalesPayment
    WHERE TransID = @TransactionId
    ORDER BY ID;

    SELECT
        v.Double_Entry_Vouchers_ID,
        v.DEV_ID_Line_No,
        v.Notes_ID,
        n.NoteSerial,
        v.RecordDate,
        v.Account_Code,
        a.Account_Serial,
        COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName,
        CASE WHEN v.Credit_Or_Debit = 0 THEN ISNULL(v.Value, 0) ELSE ISNULL(v.depet_value, 0) END AS Debit,
        CASE WHEN v.Credit_Or_Debit = 1 THEN ISNULL(v.Value, 0) ELSE ISNULL(v.credit_value, 0) END AS Credit,
        COALESCE(v.Double_Entry_Vouchers_Description, v.des) AS Description
    FROM dbo.DOUBLE_ENTREY_VOUCHERS v
    LEFT JOIN dbo.Notes n ON v.Notes_ID = n.NoteID
    LEFT JOIN dbo.ACCOUNTS a ON v.Account_Code = a.Account_Code
    WHERE v.Transaction_ID = @TransactionId
       OR v.Transaction_ID1 = @TransactionId
       OR v.Notes_ID = (SELECT TOP 1 NoteId FROM dbo.Transactions WHERE Transaction_ID = @TransactionId)
    ORDER BY v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No;

    DECLARE @NoteSerial1 nvarchar(100);
    SELECT @NoteSerial1 = NoteSerial1 FROM dbo.Transactions WHERE Transaction_ID = @TransactionId;

    SELECT TOP 50
        t.Transaction_ID,
        t.Transaction_Serial,
        t.Transaction_Type,
        t.Transaction_Date,
        t.NoteSerial,
        t.NoteSerial1,
        CONVERT(nvarchar(50), t.StoreID) AS StoreName,
        t.Total,
        t.NetValue,
        t.NoteId,
        CASE
            WHEN t.Transaction_Type = 19 THEN N'Issue voucher generated from FrmSaleBill6'
            WHEN t.Transaction_Type = 20 THEN N'Receive voucher generated from FrmSaleBill6'
            ELSE N'Related inventory transaction'
        END AS LinkReason
    FROM dbo.Transactions t
    WHERE t.Transaction_Type IN (19, 20)
      AND (
            LTRIM(RTRIM(CONVERT(nvarchar(100), t.nots))) = CONVERT(nvarchar(100), @TransactionId)
            OR (@NoteSerial1 IS NOT NULL AND LTRIM(RTRIM(CONVERT(nvarchar(100), t.nots2))) = @NoteSerial1)
          )
    ORDER BY t.Transaction_ID DESC;
END
GO

IF OBJECT_ID('dbo.MainErp_SalesInvoice_SaveDraft', 'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_SalesInvoice_SaveDraft;
GO
CREATE PROCEDURE dbo.MainErp_SalesInvoice_SaveDraft
    @TransactionId int = NULL OUTPUT,
    @TypeInvoice int,
    @TransactionDate datetime,
    @BranchId int = NULL,
    @StoreId int = NULL,
    @BoxId int = NULL,
    @CusId int = NULL,
    @CashCustomerName nvarchar(250) = NULL,
    @ManualNo nvarchar(100) = NULL,
    @Remarks nvarchar(max) = NULL,
    @NetValue decimal(18, 4) = 0,
    @Vat decimal(18, 4) = 0,
    @PayedValue decimal(18, 4) = 0,
    @RemainValue decimal(18, 4) = 0,
    @UserId int = NULL,
    @DryRun bit = 1,
    @EnableDraftWrite bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TypeInvoice NOT IN (1, 2)
    BEGIN
        RAISERROR('Invalid TypeInvoice. Expected 1 for workshop or 2 for pump.', 16, 1);
        RETURN;
    END

    IF @TransactionDate IS NULL
    BEGIN
        RAISERROR('TransactionDate is required.', 16, 1);
        RETURN;
    END

    SELECT
        @TransactionId AS TransactionId,
        @TypeInvoice AS TypeInvoice,
        @TransactionDate AS TransactionDate,
        @BranchId AS BranchId,
        @StoreId AS StoreId,
        @BoxId AS BoxId,
        @CusId AS CusId,
        @CashCustomerName AS CashCustomerName,
        @NetValue AS NetValue,
        @Vat AS Vat,
        @PayedValue AS PayedValue,
        @RemainValue AS RemainValue,
        @DryRun AS DryRun,
        @EnableDraftWrite AS EnableDraftWrite,
        CAST(N'Draft header save only. No Notes, vouchers, inventory issue/receive, or posting is created.' AS nvarchar(400)) AS SafetyMessage;

    IF @DryRun = 1 OR @EnableDraftWrite = 0
    BEGIN
        RETURN;
    END

    BEGIN TRANSACTION;

    IF @TransactionId IS NULL OR @TransactionId = 0
    BEGIN
        SELECT @TransactionId = ISNULL(MAX(Transaction_ID), 0) + 1
        FROM dbo.Transactions WITH (TABLOCKX, HOLDLOCK);

        INSERT INTO dbo.Transactions
        (
            Transaction_ID,
            Transaction_Date,
            Transaction_Type,
            TypeInvoice,
            CusID,
            StoreID,
            UserID,
            CashCustomerName,
            ManualNO,
            BranchId,
            BoxID,
            Closed,
            Posted,
            Approved,
            NetValue,
            VAT,
            PayedValue,
            RemainValue,
            Transaction_NetValue,
            TransactionComment
        )
        VALUES
        (
            @TransactionId,
            @TransactionDate,
            21,
            @TypeInvoice,
            @CusId,
            @StoreId,
            @UserId,
            @CashCustomerName,
            @ManualNo,
            @BranchId,
            @BoxId,
            0,
            0,
            0,
            @NetValue,
            @Vat,
            @PayedValue,
            @RemainValue,
            @NetValue,
            @Remarks
        );
    END
    ELSE
    BEGIN
        UPDATE dbo.Transactions
        SET
            Transaction_Date = @TransactionDate,
            CusID = @CusId,
            StoreID = @StoreId,
            UserID = @UserId,
            CashCustomerName = @CashCustomerName,
            ManualNO = @ManualNo,
            BranchId = @BranchId,
            BoxID = @BoxId,
            NetValue = @NetValue,
            VAT = @Vat,
            PayedValue = @PayedValue,
            RemainValue = @RemainValue,
            Transaction_NetValue = @NetValue,
            TransactionComment = @Remarks
        WHERE Transaction_ID = @TransactionId
          AND Transaction_Type = 21
          AND ISNULL(TypeInvoice, 0) = @TypeInvoice
          AND ISNULL(Closed, 0) = 0
          AND ISNULL(Posted, 0) = 0
          AND ISNULL(Approved, 0) = 0;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Draft transaction was not found or is already closed/posted/approved.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END

    COMMIT TRANSACTION;

    SELECT @TransactionId AS TransactionId, CAST(N'Draft header saved. Posting is still disabled.' AS nvarchar(200)) AS ResultMessage;
END
GO

IF OBJECT_ID('dbo.MainErp_SalesInvoice_SavePumpDeferredDistribution', 'P') IS NOT NULL
    DROP PROCEDURE dbo.MainErp_SalesInvoice_SavePumpDeferredDistribution;
GO
CREATE PROCEDURE dbo.MainErp_SalesInvoice_SavePumpDeferredDistribution
    @TransactionId int,
    @LineId int,
    @DetailsPump nvarchar(max),
    @Deferred decimal(18, 4),
    @DeferredQty decimal(18, 4),
    @DryRun bit = 1,
    @EnableDraftWrite bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TransactionId IS NULL OR @LineId IS NULL
    BEGIN
        RAISERROR('TransactionId and LineId are required.', 16, 1);
        RETURN;
    END

    SELECT
        @TransactionId AS TransactionId,
        @LineId AS LineId,
        @Deferred AS Deferred,
        @DeferredQty AS DeferredQty,
        @DryRun AS DryRun,
        @EnableDraftWrite AS EnableDraftWrite,
        CAST(N'Pump deferred distribution save only. No Notes, vouchers, inventory issue/receive, or posting is created.' AS nvarchar(400)) AS SafetyMessage,
        @DetailsPump AS DetailsPump;

    IF @DryRun = 1 OR @EnableDraftWrite = 0
    BEGIN
        RETURN;
    END

    BEGIN TRANSACTION;

    DECLARE @GridLineId int;

    SELECT TOP 1 @GridLineId = COALESCE(d.LineID, rn.RowNo)
    FROM
    (
        SELECT ID, ROW_NUMBER() OVER (ORDER BY ID) AS RowNo
        FROM dbo.Transaction_Details
        WHERE Transaction_ID = @TransactionId
    ) rn
    INNER JOIN dbo.Transaction_Details d ON rn.ID = d.ID
    INNER JOIN dbo.Transactions t ON d.Transaction_ID = t.Transaction_ID
    WHERE d.Transaction_ID = @TransactionId
      AND d.ID = @LineId
      AND t.Transaction_Type = 21
      AND ISNULL(t.TypeInvoice, 0) = 2
      AND ISNULL(t.Closed, 0) = 0
      AND ISNULL(t.Posted, 0) = 0
      AND ISNULL(t.Approved, 0) = 0;

    IF @GridLineId IS NULL
    BEGIN
        RAISERROR('Pump line was not found or invoice is already closed/posted/approved.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    UPDATE d
    SET
        d.DetailsPump = @DetailsPump,
        d.Deferred = @Deferred,
        d.DeferredQty = @DeferredQty
    FROM dbo.Transaction_Details d
    INNER JOIN dbo.Transactions t ON d.Transaction_ID = t.Transaction_ID
    WHERE d.Transaction_ID = @TransactionId
      AND d.ID = @LineId
      AND t.Transaction_Type = 21
      AND ISNULL(t.TypeInvoice, 0) = 2
      AND ISNULL(t.Closed, 0) = 0
      AND ISNULL(t.Posted, 0) = 0
      AND ISNULL(t.Approved, 0) = 0;

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('Pump line was not found or invoice is already closed/posted/approved.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    DELETE FROM dbo.Transaction_DetailsPump
    WHERE Transaction_ID = @TransactionId
      AND LineID = @GridLineId;

    DECLARE @Rows TABLE
    (
        RowNo int IDENTITY(1,1),
        RowText nvarchar(max)
    );

    INSERT INTO @Rows (RowText)
    SELECT LTRIM(RTRIM(x.i.value('.', 'nvarchar(max)')))
    FROM
    (
        SELECT CAST('<x><i>' + REPLACE(REPLACE(REPLACE(ISNULL(@DetailsPump, N''), '&', '&amp;'), '<', '&lt;'), '@', '</i><i>') + '</i></x>' AS xml) AS XmlData
    ) src
    CROSS APPLY src.XmlData.nodes('/x/i') x(i)
    WHERE NULLIF(LTRIM(RTRIM(x.i.value('.', 'nvarchar(max)'))), N'') IS NOT NULL;

    INSERT INTO dbo.Transaction_DetailsPump
    (
        LineID,
        CusID,
        ItemId,
        Amount,
        Transaction_ID,
        RecNo,
        Qty,
        Price
    )
    SELECT
        @GridLineId,
        CASE WHEN ISNUMERIC(parts.XmlData.value('(/r/f[1])[1]', 'nvarchar(100)')) = 1 THEN CONVERT(int, CONVERT(float, parts.XmlData.value('(/r/f[1])[1]', 'nvarchar(100)'))) ELSE NULL END,
        NULL,
        CASE WHEN ISNUMERIC(parts.XmlData.value('(/r/f[3])[1]', 'nvarchar(100)')) = 1 THEN CONVERT(float, parts.XmlData.value('(/r/f[3])[1]', 'nvarchar(100)')) ELSE NULL END,
        @TransactionId,
        CASE WHEN ISNUMERIC(parts.XmlData.value('(/r/f[7])[1]', 'nvarchar(100)')) = 1 THEN CONVERT(int, CONVERT(float, parts.XmlData.value('(/r/f[7])[1]', 'nvarchar(100)'))) ELSE NULL END,
        CASE WHEN ISNUMERIC(parts.XmlData.value('(/r/f[5])[1]', 'nvarchar(100)')) = 1 THEN CONVERT(float, parts.XmlData.value('(/r/f[5])[1]', 'nvarchar(100)')) ELSE NULL END,
        CASE WHEN ISNUMERIC(parts.XmlData.value('(/r/f[6])[1]', 'nvarchar(100)')) = 1 THEN CONVERT(float, parts.XmlData.value('(/r/f[6])[1]', 'nvarchar(100)')) ELSE NULL END
    FROM @Rows
    CROSS APPLY
    (
        SELECT CAST('<r><f>' + REPLACE(REPLACE(REPLACE(RowText, '&', '&amp;'), '<', '&lt;'), '#', '</f><f>') + '</f></r>' AS xml) AS XmlData
    ) parts
    WHERE LEN(RowText) > 0;

    COMMIT TRANSACTION;

    SELECT @TransactionId AS TransactionId, @LineId AS LineId, CAST(N'Pump deferred distribution saved. Posting is still disabled.' AS nvarchar(200)) AS ResultMessage;
END
GO
