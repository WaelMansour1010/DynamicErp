IF OBJECT_ID(N'dbo.usp_POS_SavePurchaseInvoice', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SavePurchaseInvoice;
GO

IF TYPE_ID(N'dbo.POS_PurchaseInvoiceItems') IS NOT NULL
    DROP TYPE dbo.POS_PurchaseInvoiceItems;
GO

CREATE TYPE dbo.POS_PurchaseInvoiceItems AS TABLE
(
    ItemId INT NOT NULL,
    UnitId INT NULL,
    Quantity DECIMAL(18, 4) NOT NULL,
    PurchasePrice DECIMAL(18, 4) NOT NULL,
    DiscountValue DECIMAL(18, 4) NOT NULL DEFAULT (0),
    VatValue DECIMAL(18, 4) NOT NULL DEFAULT (0),
    VatPercent DECIMAL(18, 4) NOT NULL DEFAULT (0)
);
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE dbo.usp_POS_SavePurchaseInvoice
    @InvoiceNumber NVARCHAR(50) = NULL,
    @InvoiceDate SMALLDATETIME,
    @SupplierId INT,
    @BranchId INT,
    @StoreId INT,
    @PaymentType INT,
    @BoxId INT = NULL,
    @BankId INT = NULL,
    @DiscountValue MONEY = 0,
    @VatValue MONEY = 0,
    @UserId INT,
    @EmpId INT = NULL,
    @ManualNo NVARCHAR(510) = NULL,
    @Remarks NVARCHAR(1000) = NULL,
    @Items dbo.POS_PurchaseInvoiceItems READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @lockResult INT,
        @transactionId INT,
        @receiveTransactionId INT,
        @transactionSerial INT,
        @receiveSerial INT,
        @noteId INT,
        @receiveNoteId INT,
        @noteSerial BIGINT,
        @receiveNoteSerial BIGINT,
        @receiveVoucherNumber NVARCHAR(50),
        @gross MONEY,
        @lineDiscount MONEY,
        @lineVat MONEY,
        @netBeforeVat MONEY,
        @netTotal MONEY,
        @purchaseAccount NVARCHAR(100),
        @supplierAccount NVARCHAR(100),
        @paymentAccount NVARCHAR(100),
        @vatAccount NVARCHAR(100),
        @voucherId INT,
        @lineNo INT,
        @description NVARCHAR(1000),
        @autoReceive BIT;

    IF @InvoiceDate IS NULL SET @InvoiceDate = CONVERT(DATE, GETDATE());
    IF @SupplierId IS NULL OR @SupplierId <= 0 RAISERROR(N'المورد مطلوب', 16, 1);
    IF @BranchId IS NULL OR @BranchId <= 0 RAISERROR(N'الفرع مطلوب', 16, 1);
    IF @StoreId IS NULL OR @StoreId <= 0 RAISERROR(N'المخزن مطلوب', 16, 1);
    IF NOT EXISTS (SELECT 1 FROM @Items) RAISERROR(N'يجب إضافة صنف واحد على الأقل', 16, 1);
    IF EXISTS (SELECT 1 FROM @Items WHERE ItemId <= 0 OR Quantity <= 0 OR PurchasePrice < 0 OR DiscountValue < 0 OR VatValue < 0)
        RAISERROR(N'بيانات الأصناف غير صحيحة', 16, 1);

    SELECT @supplierAccount = NULLIF(Account_Code, N'')
    FROM dbo.TblCustemers
    WHERE CusID = @SupplierId;

    IF @supplierAccount IS NULL
        RAISERROR(N'لم يتم تحديد حساب المورد', 16, 1);

    SELECT TOP (1) @purchaseAccount = NULLIF(a4, N'')
    FROM dbo.branches;

    IF @purchaseAccount IS NULL
        RAISERROR(N'لم يتم تحديد حساب المشتريات', 16, 1);

    IF @PaymentType = 0
    BEGIN
        SELECT @paymentAccount = NULLIF(Account_Code, N'') FROM dbo.TblBoxesData WHERE BoxID = @BoxId;
        IF @paymentAccount IS NULL RAISERROR(N'لم يتم تحديد حساب الخزنة', 16, 1);
    END
    ELSE IF @PaymentType = 3
    BEGIN
        SELECT @paymentAccount = NULLIF(Account_Code, N'') FROM dbo.BanksData WHERE BankID = @BankId;
        IF @paymentAccount IS NULL RAISERROR(N'لم يتم تحديد حساب البنك', 16, 1);
    END
    ELSE
    BEGIN
        SET @PaymentType = 1;
        SET @paymentAccount = @supplierAccount;
    END

    SELECT
        @gross = SUM(CAST(Quantity * PurchasePrice AS MONEY)),
        @lineDiscount = SUM(CAST(DiscountValue AS MONEY)),
        @lineVat = SUM(CAST(VatValue AS MONEY))
    FROM @Items;

    SET @lineDiscount = ISNULL(@lineDiscount, 0) + ISNULL(@DiscountValue, 0);
    SET @lineVat = CASE WHEN ISNULL(@VatValue, 0) > 0 THEN @VatValue ELSE ISNULL(@lineVat, 0) END;
    SET @netBeforeVat = ISNULL(@gross, 0) - ISNULL(@lineDiscount, 0);
    SET @netTotal = @netBeforeVat + ISNULL(@lineVat, 0);

    IF @netBeforeVat <= 0
        RAISERROR(N'إجمالي الفاتورة يجب أن يكون أكبر من صفر', 16, 1);

    SELECT TOP (1) @vatAccount = NULLIF(AccDep, N'')
    FROM dbo.TblSettsReqLimK
    WHERE @InvoiceDate BETWEEN RecordDate AND RecordDateTo
      AND ((AccOrTran = 1) OR (AccOrTran IS NULL))
      AND TransType = 22
    ORDER BY RecordDate DESC;

    BEGIN TRANSACTION;

    EXEC @lockResult = sp_getapplock
        @Resource = N'POS_PURCHASE_INVOICE_SAVE',
        @LockMode = N'Exclusive',
        @LockOwner = N'Transaction',
        @LockTimeout = 30000;

    IF @lockResult < 0
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR(N'تعذر حجز رقم فاتورة المشتريات، حاول مرة أخرى', 16, 1);
        RETURN;
    END

    SELECT @transactionId = ISNULL(MAX(Transaction_ID), 0) + 1 FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK);
    SELECT @transactionSerial = ISNULL(MAX(CASE WHEN ISNUMERIC(Transaction_Serial) = 1 THEN CAST(Transaction_Serial AS INT) ELSE 0 END), 0) + 1
    FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
    WHERE Transaction_Type = 22;

    SELECT @noteId = ISNULL(MAX(NoteID), 0) + 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK);
    SELECT @noteSerial = ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial) = 1 THEN CAST(NoteSerial AS BIGINT) ELSE 0 END), 0) + 1
    FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK);

    IF NULLIF(LTRIM(RTRIM(@InvoiceNumber)), N'') IS NULL
    BEGIN
        SELECT @InvoiceNumber = CONVERT(NVARCHAR(50), ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial1) = 1 THEN CAST(NoteSerial1 AS BIGINT) ELSE 0 END), 0) + 1)
        FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
        WHERE Transaction_Type = 22
          AND BranchId = @BranchId;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
        WHERE Transaction_Type = 22
          AND BranchId = @BranchId
          AND NoteSerial1 = CONVERT(VARCHAR(50), @InvoiceNumber)
    )
        RAISERROR(N'رقم فاتورة المشتريات مسجل مسبقاً في نفس الفرع', 16, 1);

    SET @description = N'فاتورة شراء رقم ' + ISNULL(@InvoiceNumber, N'') + CASE WHEN NULLIF(@Remarks, N'') IS NULL THEN N'' ELSE N' ' + @Remarks END;

    INSERT INTO dbo.Transactions
    (
        Transaction_ID, Transaction_Serial, Transaction_Date, Transaction_Type, PaymentType,
        UserID, CusID, StoreID, TaxFound, NoteSerial, NoteSerial1, NoteId,
        BranchId, PayedValue, NetValue, RemainValue, BoxID, BankID, ManualNO,
        Transaction_NetValue, VAT, Emp_ID, Currency_rate, Currency_id,
        DueDate, TransactionComment, OldNoteSerial1
    )
    VALUES
    (
        @transactionId, CONVERT(NVARCHAR(100), @transactionSerial), @InvoiceDate, 22, @PaymentType,
        @UserId, @SupplierId, @StoreId, CASE WHEN @lineVat > 0 THEN 1 ELSE 0 END,
        CONVERT(VARCHAR(50), @noteSerial), CONVERT(VARCHAR(50), @InvoiceNumber), @noteId,
        @BranchId, CASE WHEN @PaymentType = 1 THEN 0 ELSE @netTotal END,
        @netTotal, CASE WHEN @PaymentType = 1 THEN @netTotal ELSE 0 END,
        @BoxId, @BankId, @ManualNo,
        @netBeforeVat, @lineVat, @EmpId, 1, NULL,
        @InvoiceDate, @Remarks, CONVERT(NVARCHAR(510), @InvoiceNumber)
    );

    INSERT INTO dbo.Notes
    (
        NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value,
        Transaction_ID, CusID, BoxID, BankID, UserID, Remark, branch_no,
        numbering_type, numbering_type1, sanad_year, sanad_month, OldNoteSerial1,
        ManualNo
    )
    VALUES
    (
        @noteId, @InvoiceDate, 150, @noteSerial,
        CASE WHEN ISNUMERIC(@InvoiceNumber) = 1 THEN CAST(@InvoiceNumber AS FLOAT) ELSE NULL END,
        @netTotal, @transactionId, @SupplierId, @BoxId, @BankId, @UserId,
        @InvoiceNumber, @BranchId, 0, 6, YEAR(@InvoiceDate), MONTH(@InvoiceDate),
        @InvoiceNumber, @ManualNo
    );

    INSERT INTO dbo.Transaction_Details
    (
        Transaction_ID, Item_ID, ItemCase, Quantity, Price, UnitId, ShowQty,
        QtyBySmalltUnit, showPrice, Transaction_Date, BranchId, OpeningBurcahseQty,
        OpeningBurcahseValue, discountvalue, TotalDiscountPerLine, SupplierID,
        Vat, Vatyo, StoreID2, OrderArrivalDate
    )
    SELECT
        @transactionId,
        i.ItemId,
        ISNULL(ti.ItemCase, 1),
        CAST(i.Quantity * ISNULL(iu.UnitFactor, 1) AS FLOAT),
        CAST(i.PurchasePrice / ISNULL(NULLIF(iu.UnitFactor, 0), 1) AS FLOAT),
        ISNULL(i.UnitId, iu.UnitID),
        CAST(i.Quantity AS FLOAT),
        CAST(ISNULL(iu.UnitFactor, 1) AS FLOAT),
        CAST(i.PurchasePrice AS FLOAT),
        @InvoiceDate,
        @BranchId,
        CAST(ROUND(i.Quantity * ISNULL(iu.UnitFactor, 1), 0) AS INT),
        CAST((i.Quantity * i.PurchasePrice) - i.DiscountValue AS MONEY),
        CAST(CASE WHEN i.Quantity * ISNULL(iu.UnitFactor, 1) = 0 THEN 0 ELSE i.DiscountValue / (i.Quantity * ISNULL(iu.UnitFactor, 1)) END AS MONEY),
        0,
        @SupplierId,
        CAST(i.VatValue AS FLOAT),
        CAST(i.VatPercent AS FLOAT),
        @StoreId,
        @InvoiceDate
    FROM @Items i
    LEFT JOIN dbo.TblItems ti ON ti.ItemID = i.ItemId
    OUTER APPLY
    (
        SELECT TOP (1) iu0.UnitID, iu0.UnitFactor, iu0.JunckID
        FROM dbo.TblItemsUnits iu0
        WHERE iu0.ItemID = i.ItemId
          AND (i.UnitId IS NULL OR iu0.UnitID = i.UnitId)
        ORDER BY CASE WHEN i.UnitId IS NOT NULL AND iu0.UnitID = i.UnitId THEN 0 WHEN ISNULL(iu0.DefaultUnit, 0) = 1 THEN 1 ELSE 2 END, iu0.JunckID
    ) iu;

    SET @autoReceive = 0;
    SELECT TOP (1) @autoReceive = ISNULL(autoReseiveVoucher, 0) FROM dbo.TblOptions;

    IF @autoReceive = 1
    BEGIN
        SELECT @receiveTransactionId = ISNULL(MAX(Transaction_ID), 0) + 1 FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK);
        SELECT @receiveSerial = ISNULL(MAX(CASE WHEN ISNUMERIC(Transaction_Serial) = 1 THEN CAST(Transaction_Serial AS INT) ELSE 0 END), 0) + 1
        FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
        WHERE Transaction_Type = 20;
        SET @receiveNoteId = @noteId + 1;
        SELECT @receiveNoteSerial = ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial) = 1 THEN CAST(NoteSerial AS BIGINT) ELSE 0 END), 0) + 1
        FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK);
        SELECT @receiveVoucherNumber = CONVERT(NVARCHAR(50), ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial1) = 1 THEN CAST(NoteSerial1 AS BIGINT) ELSE 0 END), 0) + 1)
        FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
        WHERE Transaction_Type = 20
          AND BranchId = @BranchId;

        INSERT INTO dbo.Transactions
        (
            CBoBasedON, Transaction_ID, Transaction_Serial, Transaction_Date, Transaction_Type,
            PaymentType, UserID, CusID, StoreID, Emp_ID, nots, nots2, NoteSerial,
            NoteSerial1, NoteId, BranchId, TransactionComment, Currency_rate
        )
        VALUES
        (
            5, @receiveTransactionId, CONVERT(NVARCHAR(100), @receiveSerial), @InvoiceDate, 20,
            0, @UserId, @SupplierId, @StoreId, @EmpId, @InvoiceNumber, @InvoiceNumber,
            CONVERT(VARCHAR(50), @receiveNoteSerial), CONVERT(VARCHAR(50), @receiveVoucherNumber),
            @receiveNoteId, @BranchId, @description, 1
        );

        INSERT INTO dbo.Transaction_Details
        (
            Transaction_ID, Item_ID, ItemCase, Quantity, Price, UnitId, ShowQty,
            QtyBySmalltUnit, showPrice, Transaction_Date, BranchId, StoreID2,
            SupplierID, Vat, Vatyo, OrderArrivalDate
        )
        SELECT
            @receiveTransactionId, Item_ID, ItemCase, Quantity, Price, UnitId,
            ShowQty, QtyBySmalltUnit, showPrice, @InvoiceDate, @BranchId,
            @StoreId, SupplierID, Vat, Vatyo, @InvoiceDate
        FROM dbo.Transaction_Details
        WHERE Transaction_ID = @transactionId;

        INSERT INTO dbo.Notes
        (
            NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value,
            Transaction_ID, CusID, UserID, Remark, branch_no,
            numbering_type, numbering_type1, sanad_year, sanad_month, OldNoteSerial1
        )
        VALUES
        (
            @receiveNoteId, @InvoiceDate, 160, @receiveNoteSerial,
            CASE WHEN ISNUMERIC(@receiveVoucherNumber) = 1 THEN CAST(@receiveVoucherNumber AS FLOAT) ELSE NULL END,
            @netBeforeVat, @receiveTransactionId, @SupplierId, @UserId,
            @receiveVoucherNumber, @BranchId, 0, 9, YEAR(@InvoiceDate), MONTH(@InvoiceDate),
            @receiveVoucherNumber
        );

        UPDATE dbo.Transactions
        SET nots = CONVERT(NVARCHAR(100), @receiveTransactionId)
        WHERE Transaction_ID = @transactionId;
    END

    SELECT @voucherId = ISNULL(MAX(Double_Entry_Vouchers_ID), 0) + 1
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (UPDLOCK, HOLDLOCK);
    SET @lineNo = 1;

    INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
    (
        Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
        Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
        Notes_ID, Transaction_ID, UserID, currency, valuee, rate, branch_id,
        SupplierID, NextAccount_Code
    )
    VALUES
    (
        @voucherId, @lineNo, @purchaseAccount, @netBeforeVat,
        0, @description, @InvoiceDate,
        @noteId, @transactionId, @UserId, N'', @netBeforeVat, 1, @BranchId,
        @SupplierId, @paymentAccount
    );
    SET @lineNo = @lineNo + 1;

    IF @lineVat > 0
    BEGIN
        IF @vatAccount IS NULL
            RAISERROR(N'لم يتم تحديد حساب ضريبة القيمة المضافة لفواتير المشتريات', 16, 1);

        INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
        (
            Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
            Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
            Notes_ID, Transaction_ID, UserID, currency, valuee, rate, branch_id,
            SupplierID, NextAccount_Code, FlgVat
        )
        VALUES
        (
            @voucherId, @lineNo, @vatAccount, @lineVat,
            0, @description + N' القيمة المضافة', @InvoiceDate,
            @noteId, @transactionId, @UserId, N'', @lineVat, 1, @BranchId,
            @SupplierId, @paymentAccount, 1
        );
        SET @lineNo = @lineNo + 1;
    END

    INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
    (
        Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
        Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
        Notes_ID, Transaction_ID, UserID, currency, valuee, rate, branch_id,
        SupplierID, NextAccount_Code
    )
    VALUES
    (
        @voucherId, @lineNo, @paymentAccount, @netTotal,
        1, @description, @InvoiceDate,
        @noteId, @transactionId, @UserId, N'', @netTotal, 1, @BranchId,
        @SupplierId, @purchaseAccount
    );

    COMMIT TRANSACTION;

    SELECT
        @transactionId AS Transaction_ID,
        @InvoiceNumber AS InvoiceNumber,
        @noteId AS NoteID,
        @receiveTransactionId AS ReceiveTransaction_ID,
        @receiveVoucherNumber AS ReceiveVoucherNumber;
END;
GO
