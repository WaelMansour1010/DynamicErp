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
        d.Item_ID,
        i.ItemCode,
        COALESCE(i.ItemName, i.ItemNamee) AS ItemName,
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

    COMMIT TRANSACTION;

    SELECT @TransactionId AS TransactionId, @LineId AS LineId, CAST(N'Pump deferred distribution saved. Posting is still disabled.' AS nvarchar(200)) AS ResultMessage;
END
GO
