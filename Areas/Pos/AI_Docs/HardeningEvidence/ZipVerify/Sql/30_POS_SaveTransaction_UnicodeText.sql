IF OBJECT_ID(N'dbo.usp_POS_SaveTransaction', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SaveTransaction;
GO

CREATE PROCEDURE dbo.usp_POS_SaveTransaction
    @TransactionDate SMALLDATETIME,
    @BranchId INT,
    @StoreID INT = NULL,
    @UserID INT = NULL,
    @Emp_ID INT = NULL,
    @CustomerID INT = NULL,
    @PaymentType INT = 0,
    @BoxID INT = NULL,
    @PayedValue MONEY = NULL,
    @NetValue MONEY = NULL,
    @RemainValue MONEY = NULL,
    @PaymentNetid INT = NULL,
    @IsCashOut BIT = NULL,
    @IsPOS BIT = NULL,
    @OtherItems BIT = NULL,
    @PayType INT = NULL,
    @POSBillType INT = NULL,
    @STableID INT = NULL,
    @SessionD INT = NULL,
    @BillBasedOn INT = NULL,
    @CashCustomerName NVARCHAR(100) = NULL,
    @CashCustomerPhone NVARCHAR(100) = NULL,
    @Phone2 NVARCHAR(110) = NULL,
    @IPN NVARCHAR(510) = NULL,
    @ManualNO NVARCHAR(510) = NULL,
    @NoID VARCHAR(50) = NULL,
    @ManualNo2 NVARCHAR(1000) = NULL,
    @VisaNumber NVARCHAR(510) = NULL,
    @RechargeValue FLOAT = NULL,
    @Tet_NumPoket FLOAT = NULL,
    @TrafficViolations BIT = NULL,
    @ViolationsValue FLOAT = NULL,
    @ItemIDService INT = NULL,
    @ItemIDService2 INT = NULL,
    @isRecharg BIT = NULL,
    @IsWallet BIT = NULL,
    @HaveGuarantee BIT = NULL,
    @CardSerial NVARCHAR(510) = NULL,
    @ExistingTransactionID INT = NULL,
    @Prefix VARCHAR(10) = NULL,
    @Items dbo.POS_TransactionItems READONLY,
    @SalesPayments dbo.POS_SalesPayments READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TransactionID INT;
    DECLARE @NextTransactionID BIGINT;
    DECLARE @NextIDError NVARCHAR(500);
    DECLARE @NoteSerial1 VARCHAR(50);
    DECLARE @mSerInv BIGINT;
    DECLARE @VoucherReturnCode INT;
    DECLARE @IssueTransactionID INT;
    DECLARE @IssueNextTransactionID BIGINT;
    DECLARE @IssueStoreID INT;
    DECLARE @IssueNoteID INT;
    DECLARE @IssueNextNoteID BIGINT;
    DECLARE @IssueNoteSerial VARCHAR(50);
    DECLARE @IssueNoteSerial1 VARCHAR(50);
    DECLARE @IssueMSerOut BIGINT;
    DECLARE @IssueNoteReturnCode INT;
    DECLARE @IssueTotalCost FLOAT;
    DECLARE @LastIssueTransactionID INT;
    DECLARE @InvoiceNoteID INT;
    DECLARE @InvoiceNextNoteID BIGINT;
    DECLARE @InvoiceNoteSerial VARCHAR(50);
    DECLARE @InvoiceNoteReturnCode INT;
    DECLARE @DevID INT;
    DECLARE @NextDevID BIGINT;
    DECLARE @DevSerial NVARCHAR(50);
    DECLARE @RechargeAmount MONEY;
    DECLARE @LineNetAmount MONEY;
    DECLARE @VatAmount MONEY;
    DECLARE @LineGrossAmount MONEY;
    DECLARE @InvoicePaidAmount MONEY;
    DECLARE @ServiceChargeAmount MONEY;
    DECLARE @NoteValue MONEY;
    DECLARE @DebitTotal MONEY;
    DECLARE @CreditTotal MONEY;
    DECLARE @CustomerAccount NVARCHAR(255);
    DECLARE @CashAccount NVARCHAR(255);
    DECLARE @BankAccount NVARCHAR(255);
    DECLARE @WalletAccount NVARCHAR(255);
    DECLARE @CardAccount NVARCHAR(255);
    DECLARE @SalesAccount NVARCHAR(255);
    DECLARE @TaxAccount NVARCHAR(255);
    DECLARE @VatAccount NVARCHAR(255);
    DECLARE @ServiceChargeAccount NVARCHAR(255);
    DECLARE @UserBox2Account NVARCHAR(255);
    DECLARE @BranchBoxAccount NVARCHAR(255);
    DECLARE @TerminalBoxAccount NVARCHAR(255);
    DECLARE @ItemSupplierAccount NVARCHAR(255);
    DECLARE @ItemAccount NVARCHAR(255);
    DECLARE @ItemRevenueAccount NVARCHAR(255);
    DECLARE @BankCommissionAmount MONEY;
    DECLARE @WalletCostAmount MONEY;
    DECLARE @ViolationShareAmount MONEY;
    DECLARE @PricePercent FLOAT;
    DECLARE @BranchName NVARCHAR(255);
    DECLARE @BranchCustodyParent NVARCHAR(255);
    DECLARE @CashParentAccount NVARCHAR(255);
    DECLARE @AccountingDescription NVARCHAR(4000);
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;
    DECLARE @OldIssueTransactionID INT;
    DECLARE @AllocationError NVARCHAR(4000);
    DECLARE @DevSerialLockResult INT;
    DECLARE @DevSerialLockResource NVARCHAR(255);

    DECLARE @IssueVouchers TABLE
    (
        IssueTransactionID INT NOT NULL,
        StoreID INT NULL,
        NoteID INT NULL,
        NoteSerial VARCHAR(50) NULL,
        NoteSerial1 VARCHAR(50) NULL
    );

    BEGIN TRY
        IF @TransactionDate IS NULL
            RAISERROR('TransactionDate is required.', 16, 1);

        IF @BranchId IS NULL
            RAISERROR('BranchId is required.', 16, 1);

        IF NOT EXISTS (SELECT 1 FROM @Items)
            RAISERROR('At least one sales detail row is required.', 16, 1);

        BEGIN TRANSACTION;

        IF ISNULL(@ExistingTransactionID, 0) > 0
        BEGIN
            SELECT TOP (1)
                @TransactionID = Transaction_ID,
                @NoteSerial1 = NoteSerial1,
                @mSerInv = Ser,
                @OldIssueTransactionID =
                    CASE
                        WHEN ISNUMERIC(NULLIF(LTRIM(RTRIM(ISNULL(NOTS, N''))), N'')) = 1
                            THEN CONVERT(INT, NULLIF(LTRIM(RTRIM(ISNULL(NOTS, N''))), N''))
                        ELSE NULL
                    END
            FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
            WHERE Transaction_ID = @ExistingTransactionID
              AND Transaction_Type = 21;

            IF @TransactionID IS NULL
                RAISERROR('Existing POS transaction was not found for update.', 16, 1);

            DELETE dev
            FROM dbo.DOUBLE_ENTREY_VOUCHERS AS dev
            INNER JOIN dbo.Notes AS n ON n.NoteID = dev.Notes_ID
            WHERE n.Transaction_ID IN (@TransactionID, ISNULL(@OldIssueTransactionID, -2147483648));

            DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS
            WHERE Transaction_ID IN (@TransactionID, ISNULL(@OldIssueTransactionID, -2147483648));

            DELETE FROM dbo.Notes
            WHERE Transaction_ID IN (@TransactionID, ISNULL(@OldIssueTransactionID, -2147483648));

            IF @OldIssueTransactionID IS NOT NULL
            BEGIN
                DELETE FROM dbo.Transaction_Details WHERE Transaction_ID = @OldIssueTransactionID;
                DELETE FROM dbo.TblSalesPayment WHERE TransID = @OldIssueTransactionID;
                DELETE FROM dbo.Transactions WHERE Transaction_ID = @OldIssueTransactionID AND Transaction_Type = 19;
            END

            DELETE FROM dbo.Transaction_Details WHERE Transaction_ID = @TransactionID;
            DELETE FROM dbo.TblSalesPayment WHERE TransID = @TransactionID;
        END
        ELSE
        BEGIN
            /*
                Transaction_ID allocation:
                Verified VB6 SaveData calls:
                    new_id("Transactions", "Transaction_ID", "", True)

                Verified SQL Server new_id implementation first calls:
                    dbo.GetNextID_FromSequence
                        @TableName = N'Transactions',
                        @FieldName = N'Transaction_ID'

                Verified POS database objects:
                    dbo.GetNextID_FromSequence
                    dbo.seq_Transactions_Transaction_ID

                The VB6 fallback is MAX + 1 WITH (NOLOCK), but this Step 1 draft uses
                the primary SQL Server sequence path only.
            */
            SET @NextIDError = NULL;

            EXEC dbo.GetNextID_FromSequence
                @TableName = N'Transactions',
                @FieldName = N'Transaction_ID',
                @NextValue = @NextTransactionID OUTPUT,
                @ErrorMsg = @NextIDError OUTPUT;

            IF @NextIDError IS NOT NULL OR @NextTransactionID IS NULL
            BEGIN
                SET @AllocationError = N'Unable to allocate Transaction_ID. Source=dbo.GetNextID_FromSequence; Table=dbo.Transactions; Field=Transaction_ID; Error=' + ISNULL(@NextIDError, N'<null>') + N'; NextValue=' + ISNULL(CONVERT(NVARCHAR(50), @NextTransactionID), N'<null>');
                RAISERROR(@AllocationError, 16, 1);
            END;

            IF @NextTransactionID > 2147483647
                RAISERROR('Allocated Transaction_ID exceeds INT range.', 16, 1);

            SET @TransactionID = CONVERT(INT, @NextTransactionID);

            /*
                VB6 FrmSaleBill6.SaveData calls Voucher_coding with Sanad_No=7,
                NoteType=170, Transaction_Type=21, StoreID and UserID. The web
                caller must not send service type values such as cash-in/cash-out
                as @Prefix, because CASH numbering settings use NULL prefix with
                YearDigit=2.
            */
            EXEC @VoucherReturnCode = dbo.usp_Voucher_coding_V2
                @my_branch = @BranchId,
                @date1 = @TransactionDate,
                @Sanad_No = 7,
                @NoteType = 170,
                @departement_name = 1,
                @Transaction_Type = 21,
                @Prefix = @Prefix,
                @StoreID = @StoreID,
                @BillType = 0,
                @MosemID = 0,
                @mTableName = NULL,
                @mUserID = @UserID,
                @Result = @NoteSerial1 OUTPUT,
                @mSerInv = @mSerInv OUTPUT;

            IF @VoucherReturnCode <> 0 OR @NoteSerial1 IS NULL OR @NoteSerial1 = 'error'
                RAISERROR('Unable to generate NoteSerial1 using dbo.usp_Voucher_coding_V2.', 16, 1);
        END

        SELECT
            @LineNetAmount = ISNULL(SUM(CONVERT(MONEY, ISNULL(Price, 0)) * CONVERT(MONEY, ISNULL(Quantity, 0))), 0),
            @VatAmount = ISNULL(SUM(CONVERT(MONEY, ISNULL(Vat, 0))), 0)
        FROM @Items;

        SET @LineGrossAmount = ISNULL(@LineNetAmount, 0) + ISNULL(@VatAmount, 0);

        IF NULLIF(LTRIM(RTRIM(ISNULL(@IPN, N''))), N'') IS NULL
            RAISERROR('Screen ID is required for POS transactions.', 16, 1);

        IF ISNULL(@IsCashOut, 0) = 0
           AND ISNULL(@TrafficViolations, 0) = 0
           AND NULLIF(LTRIM(RTRIM(ISNULL(@ManualNO, N''))), N'') IS NULL
            RAISERROR('Screen IPN is required for Cash In and Keshni Card transactions.', 16, 1);

        IF ISNULL(@IsCashOut, 0) = 0
           AND ISNULL(@TrafficViolations, 0) = 0
           AND NULLIF(LTRIM(RTRIM(ISNULL(@ManualNO, N''))), N'') IS NOT NULL
           AND EXISTS
           (
               SELECT 1
               FROM dbo.Transactions t
               WHERE t.Transaction_Type = 21
                 AND NULLIF(LTRIM(RTRIM(ISNULL(t.ManualNO, N''))), N'') = NULLIF(LTRIM(RTRIM(ISNULL(@ManualNO, N''))), N'')
                 AND (@ExistingTransactionID IS NULL OR @ExistingTransactionID <= 0 OR t.Transaction_ID <> @ExistingTransactionID)
                 AND
                 (
                     NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL
                     OR
                     (
                         ISNULL(t.IsCashOut, 0) = 0
                         AND ISNULL(t.TrafficViolations, 0) = 0
                         AND (ISNULL(t.isRecharg, 0) = 1 OR ISNULL(t.RechargeValue, 0) > 0)
                     )
                 )
           )
            RAISERROR('Screen IPN already exists for Cash In or Keshni Card transactions.', 16, 1);

        IF ISNULL(@ExistingTransactionID, 0) <= 0
        BEGIN
        INSERT INTO dbo.Transactions
        (
            Transaction_ID,
            Transaction_Date,
            Transaction_Type,
            PaymentType,
            Trans_Discount,
            Currency_id,
            Currency_rate,
            CusID,
            StoreID,
            UserID,
            Emp_ID,
            NoteSerial1,
            Ser,
            BranchId,
            PayedValue,
            NetValue,
            RemainValue,
            ManualNO,
            CBoBasedON,
            ManualNo2,
            Transaction_NetValue,
            PaymentNetid,
            BoxID,
            POSBillType,
            STableID,
            SessionD,
            BillBasedOn,
            IsCashOut,
            IsPOS,
            OtherItems,
            PayType,
            CashCustomerName,
            CashCustomerPhone,
            Phone2,
            IPN,
            NoID,
            VisaNumber,
            RechargeValue,
            Tet_NumPoket,
            VAT,
            TrafficViolations,
            ViolationsValue,
            ItemIDService,
            ItemIDService2,
            isRecharg,
            IsWallet,
            HaveGuarantee
        )
        VALUES
        (
            @TransactionID,
            @TransactionDate,
            21,
            ISNULL(NULLIF(@PaymentType, 0), 1),
            0,
            1,
            1,
            @CustomerID,
            @StoreID,
            @UserID,
            @Emp_ID,
            @NoteSerial1,
            @mSerInv,
            @BranchId,
            @PayedValue,
            @NetValue,
            @RemainValue,
            @ManualNO,
            ISNULL(@BillBasedOn, 0),
            @ManualNo2,
            @LineGrossAmount,
            @PaymentNetid,
            @BoxID,
            @POSBillType,
            @STableID,
            @SessionD,
            @BillBasedOn,
            @IsCashOut,
            @IsPOS,
            @OtherItems,
            ISNULL(@PayType, 1),
            @CashCustomerName,
            @CashCustomerPhone,
            @Phone2,
            @IPN,
            @NoID,
            @VisaNumber,
            @RechargeValue,
            @Tet_NumPoket,
            CASE WHEN ISNULL(@TrafficViolations, 0) = 1 THEN 0 ELSE @VatAmount END,
            @TrafficViolations,
            @ViolationsValue,
            @ItemIDService,
            @ItemIDService2,
            @isRecharg,
            @IsWallet,
            @HaveGuarantee
        );
        END
        ELSE
        BEGIN
            UPDATE dbo.Transactions
            SET
                Transaction_Date = @TransactionDate,
                PaymentType = ISNULL(NULLIF(@PaymentType, 0), 1),
                CusID = @CustomerID,
                StoreID = @StoreID,
                UserID = @UserID,
                Emp_ID = @Emp_ID,
                BranchId = @BranchId,
                PayedValue = @PayedValue,
                NetValue = @NetValue,
                RemainValue = @RemainValue,
                ManualNO = @ManualNO,
                CBoBasedON = ISNULL(@BillBasedOn, 0),
                ManualNo2 = @ManualNo2,
                Transaction_NetValue = @LineGrossAmount,
                PaymentNetid = @PaymentNetid,
                BoxID = @BoxID,
                POSBillType = @POSBillType,
                STableID = @STableID,
                SessionD = @SessionD,
                BillBasedOn = @BillBasedOn,
                IsCashOut = @IsCashOut,
                IsPOS = @IsPOS,
                OtherItems = @OtherItems,
                PayType = ISNULL(@PayType, 1),
                CashCustomerName = @CashCustomerName,
                CashCustomerPhone = @CashCustomerPhone,
                Phone2 = @Phone2,
                IPN = @IPN,
                NoID = @NoID,
                VisaNumber = @VisaNumber,
                RechargeValue = @RechargeValue,
                Tet_NumPoket = @Tet_NumPoket,
                VAT = CASE WHEN ISNULL(@TrafficViolations, 0) = 1 THEN 0 ELSE @VatAmount END,
                TrafficViolations = @TrafficViolations,
                ViolationsValue = @ViolationsValue,
                ItemIDService = @ItemIDService,
                ItemIDService2 = @ItemIDService2,
                isRecharg = @isRecharg,
                IsWallet = @IsWallet,
                HaveGuarantee = @HaveGuarantee
            WHERE Transaction_ID = @TransactionID
              AND Transaction_Type = 21;
        END

        INSERT INTO dbo.Transaction_Details
        (
            Transaction_ID,
            Item_ID,
            Quantity,
            Price,
            UnitId,
            ShowQty,
            QtyBySmalltUnit,
            showPrice,
            TotalPrice,
            StoreID2,
            Vat,
            Vatyo,
            TypeVAT,
            discountvalue,
            TotalDiscountPerLine,
            ItemCase,
            ColorID,
            ItemSize,
            OpeningSalesQty,
            ClassId,
            CostPrice,
            SavedItemType,
            BranchId
        )
        SELECT
            @TransactionID,
            i.Item_ID,
            i.Quantity,
            i.Price,
            i.UnitId,
            i.ShowQty,
            i.QtyBySmalltUnit,
            i.showPrice,
            NULL,
            ISNULL(i.StoreID2, @StoreID),
            i.Vat,
            i.Vatyo,
            i.Vatyo,
            i.discountvalue,
            i.TotalDiscountPerLine,
            ISNULL(i.ItemCase, 1),
            1,
            1,
            ISNULL(i.Quantity, 1),
            1,
            i.CostPrice,
            i.SavedItemType,
            @BranchId
        FROM @Items AS i;

        /*
            Payments:
            Verified VB6 SaveSalesPayment writes dbo.TblSalesPayment only when the
            multi-payment grid is active. This draft inserts only supplied rows and
            does not infer payment rows from header totals.

            dbo.TblTransactionPayments exists, but exact save-time insert logic was
            not verified in the allowed VB6 inspection scope, so it is not used here.
        */
        INSERT INTO dbo.TblSalesPayment
        (
            TransID,
            PaymentID,
            Value,
            CardNo,
            MaxValue
        )
        SELECT
            @TransactionID,
            p.PaymentID,
            p.Value,
            p.CardNo,
            p.MaxValue
        FROM @SalesPayments AS p;

        /*
            Card basic fields:
            dbo.Transactions.VisaNumber is verified and is inserted above from
            @VisaNumber. @CardSerial is kept for the first Step 1 draft contract and
            updates VisaNumber when supplied. VB6 also writes the token into
            Transaction_Details.ItemSerial. No duplicate-card validation is
            implemented in Step 1 because the exact VB6 rule is not yet mapped.
        */
        IF NULLIF(LTRIM(RTRIM(@CardSerial)), N'') IS NOT NULL
        BEGIN
            UPDATE dbo.Transactions
            SET VisaNumber = @CardSerial
            WHERE Transaction_ID = @TransactionID;

            UPDATE dbo.Transaction_Details
            SET ItemSerial = @CardSerial
            WHERE Transaction_ID = @TransactionID
              AND Item_ID = ISNULL(@ItemIDService, Item_ID)
              AND NULLIF(LTRIM(RTRIM(ISNULL(ItemSerial, N''))), N'') IS NULL;
        END

        /*
            TODO Step 2 - Violations:
            Verified table: dbo.TblConfirmViolation.
            Verified columns include ID, DurationID, VendorID, ViolationID,
            ViolationType, MinistryContractID, MinistryContractValue, [Date],
            DateH, Value, UserID, CreationDate, MonthID, CarID, AbsenceCount,
            Vcount, Remarks, SchoolID.
            Do not insert until FrmSaleBill6 violation save logic is mapped exactly.
        */

        /*
            Invoice Voucher / PG accounting:
            Implements the invoice-level NoteType = 170 and
            dbo.DOUBLE_ENTREY_VOUCHERS rows for the saved POS sale.
            Closing accounting is not part of this block.
        */
        SELECT
            @RechargeAmount = ISNULL(@RechargeValue, 0),
            @LineNetAmount = ISNULL(SUM(CONVERT(MONEY, ISNULL(Price, 0)) * CONVERT(MONEY, ISNULL(Quantity, 0))), 0),
            @VatAmount = ISNULL(SUM(CONVERT(MONEY, ISNULL(Vat, 0))), 0)
        FROM @Items;

        SET @LineGrossAmount = ISNULL(@LineNetAmount, 0) + ISNULL(@VatAmount, 0);
        SET @InvoicePaidAmount = ISNULL(NULLIF(@PayedValue, 0), ISNULL(NULLIF(@NetValue, 0), @LineGrossAmount + @RechargeAmount));
        SET @ServiceChargeAmount =
            CASE
                WHEN @InvoicePaidAmount - @RechargeAmount - @LineGrossAmount > 0
                    THEN @InvoicePaidAmount - @RechargeAmount - @LineGrossAmount
                ELSE 0
            END;

        SET @NoteValue = @RechargeAmount + @LineGrossAmount + @ServiceChargeAmount;
        SET @AccountingDescription = N'›« Ê—… »Ì⁄ —ﬁ„ ' + CONVERT(NVARCHAR(50), @NoteSerial1);

        SET @NextIDError = NULL;

        EXEC dbo.GetNextID_FromSequence
            @TableName = N'Notes',
            @FieldName = N'NoteID',
            @NextValue = @InvoiceNextNoteID OUTPUT,
            @ErrorMsg = @NextIDError OUTPUT;

        IF @NextIDError IS NOT NULL OR @InvoiceNextNoteID IS NULL
        BEGIN
            SET @AllocationError = N'Unable to allocate invoice accounting NoteID. Source=dbo.GetNextID_FromSequence; Table=dbo.Notes; Field=NoteID; Error=' + ISNULL(@NextIDError, N'<null>') + N'; NextValue=' + ISNULL(CONVERT(NVARCHAR(50), @InvoiceNextNoteID), N'<null>');
            RAISERROR(@AllocationError, 16, 1);
        END;

        IF @InvoiceNextNoteID > 2147483647
            RAISERROR('Allocated invoice accounting NoteID exceeds INT range.', 16, 1);

        SET @InvoiceNoteID = CONVERT(INT, @InvoiceNextNoteID);

        EXEC @InvoiceNoteReturnCode = dbo.usp_Notes_coding_V2
            @my_branch = @BranchId,
            @date1 = @TransactionDate,
            @departement_name = 1,
            @Result = @InvoiceNoteSerial OUTPUT;

        IF @InvoiceNoteReturnCode <> 0 OR @InvoiceNoteSerial IS NULL OR @InvoiceNoteSerial = 'error'
            RAISERROR('Unable to generate invoice accounting NoteSerial using dbo.usp_Notes_coding_V2.', 16, 1);

        SET @NextIDError = NULL;

        EXEC dbo.GetNextID_FromSequence
            @TableName = N'DOUBLE_ENTREY_VOUCHERS',
            @FieldName = N'Double_Entry_Vouchers_ID',
            @NextValue = @NextDevID OUTPUT,
            @ErrorMsg = @NextIDError OUTPUT;

        IF @NextIDError IS NOT NULL OR @NextDevID IS NULL
        BEGIN
            SET @AllocationError = N'Unable to allocate invoice accounting Double_Entry_Vouchers_ID. Source=dbo.GetNextID_FromSequence; Table=dbo.DOUBLE_ENTREY_VOUCHERS; Field=Double_Entry_Vouchers_ID; Error=' + ISNULL(@NextIDError, N'<null>') + N'; NextValue=' + ISNULL(CONVERT(NVARCHAR(50), @NextDevID), N'<null>');
            RAISERROR(@AllocationError, 16, 1);
        END;

        IF @NextDevID > 2147483647
            RAISERROR('Allocated invoice accounting Double_Entry_Vouchers_ID exceeds INT range.', 16, 1);

        SET @DevID = CONVERT(INT, @NextDevID);

        SET @DevSerialLockResource = N'POS.DEV_Serial.' + CONVERT(CHAR(8), @TransactionDate, 112);

        EXEC @DevSerialLockResult = sys.sp_getapplock
            @Resource = @DevSerialLockResource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @DevSerialLockResult < 0
        BEGIN
            SET @AllocationError = N'Unable to allocate invoice accounting DEV_Serial. Source=sys.sp_getapplock; Resource=' + @DevSerialLockResource + N'; LockResult=' + CONVERT(NVARCHAR(20), @DevSerialLockResult) + N'; DevID=' + CONVERT(NVARCHAR(50), @DevID);
            RAISERROR(@AllocationError, 16, 1);
        END;

        SET @DevSerial =
            CONVERT(CHAR(8), @TransactionDate, 112) + N'0' +
            CONVERT(NVARCHAR(20),
                1 + (
                    SELECT COUNT(DISTINCT Double_Entry_Vouchers_ID)
                    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
                    WHERE RecordDate >= CONVERT(DATE, @TransactionDate)
                      AND RecordDate < DATEADD(DAY, 1, CONVERT(DATE, @TransactionDate))
                )
            );

        SELECT
            @BranchName = NULLIF(LTRIM(RTRIM(branch_name)), N'')
        FROM dbo.TblBranchesData
        WHERE branch_id = @BranchId;

        SET @BranchCustodyParent = NULL;
        SET @CashParentAccount = NULL;

        SET @CustomerAccount = NULLIF(dbo.GetMyAccountCode(N'TblCustemers', N'CusID', ISNULL(@CustomerID, 0), N'Account_Code'), N'');

        IF @CustomerAccount IS NULL
        BEGIN
            SELECT TOP (1)
                @CustomerAccount = dev.Account_Code
            FROM dbo.DOUBLE_ENTREY_VOUCHERS AS dev
            INNER JOIN dbo.Transactions AS hist ON hist.Transaction_ID = dev.Transaction_ID
            WHERE hist.Transaction_Type = 21
              AND hist.BranchId = @BranchId
              AND dev.Account_Code LIKE N'a1a2a4%'
              AND dev.Credit_Or_Debit IN (0, 1)
            ORDER BY hist.Transaction_ID DESC, dev.DEV_ID_Line_No;
        END;

        IF @CustomerAccount IS NULL
        BEGIN
            SELECT TOP (1)
                @CustomerAccount = Account_Code
            FROM dbo.ACCOUNTS
            WHERE Parent_Account_Code = @BranchCustodyParent
              AND (@BranchName IS NULL OR account_name LIKE N'%' + LEFT(@BranchName, 12) + N'%')
            ORDER BY Account_Code;
        END;

        SET @CashAccount = NULLIF(dbo.GetMyAccountCode(N'BanksData', N'BankID', ISNULL(@PaymentNetid, 0), N'Account_Code'), N'');
        SET @UserBox2Account = NULL;

        SELECT TOP (1)
            @UserBox2Account = NULLIF(box2.Account_Code, N'')
        FROM dbo.TblUsers AS u
        INNER JOIN dbo.TblBoxesData AS box2 ON box2.BoxID = u.BoxID2
        WHERE u.UserID = @UserID;

        IF @CashAccount IS NULL
        BEGIN
            SELECT TOP (1)
                @CashAccount = Account_Code
            FROM dbo.ACCOUNTS
            WHERE Parent_Account_Code = @CashParentAccount
              AND account_name LIKE N'%«·‘Õ‰%'
            ORDER BY Account_Code;
        END;

        SET @WalletAccount = NULL;

        SELECT TOP (1)
            @WalletAccount = NULLIF(Account_Code, N'')
        FROM dbo.TblBoxesData
        WHERE ISNULL(IsWallet, 0) = 1
          AND BranchId = @BranchId
        ORDER BY BoxID;

        SELECT TOP (1)
            @WalletAccount = dev.Account_Code
        FROM dbo.DOUBLE_ENTREY_VOUCHERS AS dev
        INNER JOIN dbo.Transactions AS hist ON hist.Transaction_ID = dev.Transaction_ID
        WHERE hist.Transaction_Type = 21
          AND hist.BranchId = @BranchId
          AND ISNULL(hist.IsWallet, 0) = 1
          AND @WalletAccount IS NULL
          AND dev.Account_Code LIKE N'a1a2a1a1%'
          AND dev.Credit_Or_Debit = 0
        ORDER BY hist.Transaction_ID DESC, dev.DEV_ID_Line_No;

        IF @WalletAccount IS NULL
        BEGIN
            SELECT TOP (1)
            @WalletAccount = Account_Code
            FROM dbo.ACCOUNTS
            WHERE Parent_Account_Code = N'a1a2a1a3'
              AND (@BranchName IS NULL OR account_name LIKE N'%' + LEFT(@BranchName, 12) + N'%')
            ORDER BY Account_Code;
        END;

        SET @CardAccount = NULLIF(dbo.GetMyAccountCode(N'TblBoxesData', N'BoxID', ISNULL(@BoxID, 0), N'Account_Code'), N'');
        SET @SalesAccount = dbo.get_account_code_branch(2, CONVERT(VARCHAR(50), @BranchId));
        SET @VatAccount = NULL;

        SELECT TOP (1)
            @VatAccount = NULLIF(AccCir, N'')
        FROM dbo.TblSettsReqLimK
        WHERE @TransactionDate BETWEEN RecordDate AND RecordDateTo
          AND (AccOrTran = 1 OR AccOrTran IS NULL)
          AND TransType = 21
        ORDER BY RecordDate DESC, ID DESC;

        -- VB6 PG() uses TblUsers.BankID for the recharge bank account, not PaymentNetid.
        SELECT TOP (1)
            @BankAccount = NULLIF(bank.Account_Code, N'')
        FROM dbo.TblUsers AS u
        LEFT JOIN dbo.BanksData AS bank ON bank.BankID = u.BankID
        WHERE u.UserID = @UserID;

        SELECT TOP (1)
            @BranchBoxAccount = NULLIF(Account_Code, N'')
        FROM dbo.TblBoxesData
        WHERE BranchId = @BranchId
        ORDER BY BoxID;

        SELECT TOP (1)
            @TerminalBoxAccount = NULLIF(Account_Code, N'')
        FROM dbo.TblBoxesData
        WHERE ISNULL(IsTerminalPOS, 0) = 1
        ORDER BY BoxID;

        SELECT TOP (1)
            @ItemSupplierAccount = NULLIF(c.Account_Code, N''),
            @ItemAccount = NULLIF(i.Account_Code, N''),
            @ItemRevenueAccount = NULLIF(i.Account_Code3, N''),
            @PricePercent = ISNULL(i.PricePercent, 0)
        FROM dbo.TblItems AS i
        LEFT JOIN dbo.TblCustemers AS c ON c.CusID = i.DefaultSupplier
        WHERE i.ItemID = @ItemIDService;

        SET @BankCommissionAmount = 0;

        SELECT TOP (1)
            @BankCommissionAmount =
                CASE
                    WHEN @RechargeAmount * ISNULL(PercentVisaPur, 0) / 100 < ISNULL(MinVisaPur, 0)
                        THEN ISNULL(MinVisaPur, 0)
                    WHEN @RechargeAmount * ISNULL(PercentVisaPur, 0) / 100 > ISNULL(MaxVisaPur, 0)
                         AND ISNULL(MaxVisaPur, 0) > 0
                        THEN ISNULL(MaxVisaPur, 0)
                    ELSE @RechargeAmount * ISNULL(PercentVisaPur, 0) / 100
                END
        FROM dbo.TblOptions;

        IF @RechargeAmount = 0
            SET @BankCommissionAmount = 0;

        SET @WalletCostAmount = 0;

        IF ISNULL(@IsWallet, 0) = 1 OR ISNULL(@HaveGuarantee, 0) = 1
        BEGIN
            IF @TransactionDate > '20250621'
            BEGIN
                SELECT TOP (1)
                    @WalletCostAmount =
                        CONVERT(MONEY, ISNULL(Cost, 0)) - CONVERT(MONEY, ISNULL(CashBack, 0))
                FROM dbo.CheckPriceRangeSales3(CONVERT(INT, ROUND(@RechargeAmount, 0)), CONVERT(INT, ROUND(@RechargeAmount, 0)), @ItemIDService);
            END
            ELSE
                SET @WalletCostAmount = (@LineGrossAmount + @RechargeAmount) * 0.008;
        END;

        DECLARE @AccountingLines TABLE
        (
            LineNumber INT NOT NULL,
            AccountCode NVARCHAR(255) NULL,
            EntryValue MONEY NOT NULL,
            CreditOrDebit SMALLINT NOT NULL,
            EntryDescription NVARCHAR(4000) NULL
        );

        IF ISNULL(@TrafficViolations, 0) = 1
        BEGIN
            SET @ViolationShareAmount = CONVERT(MONEY, ISNULL(@ViolationsValue, 0)) * CONVERT(MONEY, ISNULL(@PricePercent, 0));

            INSERT INTO @AccountingLines (LineNumber, AccountCode, EntryValue, CreditOrDebit, EntryDescription)
            VALUES
                (1, @BranchBoxAccount, @LineGrossAmount, 0, N'›« Ê—… —ﬁ„ ' + CONVERT(NVARCHAR(50), @NoteSerial1) + N' Œ“‰… «·›—⁄'),
                (2, @ItemSupplierAccount, @ViolationShareAmount, 0, N'›« Ê—… —ﬁ„ ' + CONVERT(NVARCHAR(50), @NoteSerial1)),
                (3, @ItemRevenueAccount, @ViolationShareAmount, 1, N'›« Ê—… —ﬁ„ ' + CONVERT(NVARCHAR(50), @NoteSerial1) + N' ≈Ì—«œ —”Ê„ ”œ«œ „Œ«·›«  - —”Ê„ À«» …'),
                (4, @ItemRevenueAccount, @LineGrossAmount, 1, N'›« Ê—… —ﬁ„ ' + CONVERT(NVARCHAR(50), @NoteSerial1) + N' ≈Ì—«œ —”Ê„ ”œ«œ „Œ«·›«  - ‰”»… √Ê—«‰Ã');
        END
        ELSE IF ISNULL(@OtherItems, 0) = 1
        BEGIN
            INSERT INTO @AccountingLines (LineNumber, AccountCode, EntryValue, CreditOrDebit, EntryDescription)
            VALUES
                (1, @ItemSupplierAccount, @LineGrossAmount + @RechargeAmount, 0, N'›« Ê—… —ﬁ„ ' + CONVERT(NVARCHAR(50), @NoteSerial1)),
                (2, @ItemAccount, @LineGrossAmount + @RechargeAmount, 1, N'›« Ê—… —ﬁ„ ' + CONVERT(NVARCHAR(50), @NoteSerial1));
        END
        ELSE IF ISNULL(@HaveGuarantee, 0) = 1
        BEGIN
            INSERT INTO @AccountingLines (LineNumber, AccountCode, EntryValue, CreditOrDebit, EntryDescription)
            VALUES
                (1, @BranchBoxAccount, @LineGrossAmount + @RechargeAmount, 0, @AccountingDescription),
                (2, dbo.get_account_code_branch(218, CONVERT(VARCHAR(50), @BranchId)), @LineGrossAmount, 1, @AccountingDescription),
                (3, @TerminalBoxAccount, @RechargeAmount, 1, @AccountingDescription);
        END
        ELSE IF NULLIF(LTRIM(RTRIM(ISNULL(@VisaNumber, N''))), N'') IS NOT NULL
           OR NULLIF(LTRIM(RTRIM(ISNULL(@CardSerial, N''))), N'') IS NOT NULL
        BEGIN
            INSERT INTO @AccountingLines (LineNumber, AccountCode, EntryValue, CreditOrDebit, EntryDescription)
            VALUES
                (2, @CardAccount, @LineGrossAmount, 0, @AccountingDescription),
                (4, @SalesAccount, @LineNetAmount, 1, @AccountingDescription),
                (5, @VatAccount, @VatAmount, 1, @AccountingDescription);
        END
        ELSE IF ISNULL(@IsCashOut, 0) = 1 OR ISNULL(@IsWallet, 0) = 1
        BEGIN
            SET @ServiceChargeAccount = dbo.get_account_code_branch(129, CONVERT(VARCHAR(50), @BranchId));
            SET @TaxAccount = dbo.get_account_code_branch(122, CONVERT(VARCHAR(50), @BranchId));
            INSERT INTO @AccountingLines (LineNumber, AccountCode, EntryValue, CreditOrDebit, EntryDescription)
            VALUES
                (1, @WalletAccount, @RechargeAmount + @LineGrossAmount, 0, @AccountingDescription),
                (2, @ServiceChargeAccount, @WalletCostAmount, 0, @AccountingDescription),
                (3, @UserBox2Account, @RechargeAmount, 1, @AccountingDescription),
                (4, @TaxAccount, @LineGrossAmount, 1, @AccountingDescription),
                (5, @WalletAccount, @WalletCostAmount, 1, @AccountingDescription);
        END
        ELSE
        BEGIN
            SET @ServiceChargeAccount = dbo.get_account_code_branch(52, CONVERT(VARCHAR(50), @BranchId));
            SET @TaxAccount = dbo.get_account_code_branch(23, CONVERT(VARCHAR(50), @BranchId));

            INSERT INTO @AccountingLines (LineNumber, AccountCode, EntryValue, CreditOrDebit, EntryDescription)
            VALUES
                (1, @UserBox2Account, @RechargeAmount, 0, N'«·⁄„Ì· : ' + ISNULL(@CashCustomerName, N'') + N' IPN ' + ISNULL(@IPN, N'') + N' ›—⁄ ' + ISNULL(@BranchName, N'') + N' ›« Ê—… —ﬁ„ ' + CONVERT(NVARCHAR(50), @NoteSerial1)),
                (2, @BankAccount, @RechargeAmount, 1, N'«·⁄„Ì· : ' + ISNULL(@CashCustomerName, N'') + N' IPN ' + ISNULL(@IPN, N'') + N' ›—⁄ ' + ISNULL(@BranchName, N'') + N' ›« Ê—… —ﬁ„ ' + CONVERT(NVARCHAR(50), @NoteSerial1)),
                (3, @ServiceChargeAccount, @BankCommissionAmount, 0, N''),
                (4, @BankAccount, @BankCommissionAmount, 1, N'«·⁄„Ì· : ' + ISNULL(@CashCustomerName, N'') + N' IPN ' + ISNULL(@IPN, N'') + N' ›—⁄ ' + ISNULL(@BranchName, N'') + N' ›« Ê—… —ﬁ„ ' + CONVERT(NVARCHAR(50), @NoteSerial1)),
                (5, @CardAccount, @LineGrossAmount, 0, @AccountingDescription),
                (7, @TaxAccount, @LineNetAmount, 1, @AccountingDescription),
                (8, @VatAccount, @VatAmount, 1, @AccountingDescription);
        END;

        IF EXISTS
        (
            SELECT 1
            FROM @AccountingLines
            WHERE EntryValue > 0
              AND (NULLIF(LTRIM(RTRIM(AccountCode)), N'') IS NULL OR AccountCode = N'NO account')
        )
            RAISERROR('Invoice accounting account mapping is missing for one or more required rows.', 16, 1);

        SELECT
            @DebitTotal = ISNULL(SUM(CASE WHEN CreditOrDebit = 0 THEN EntryValue ELSE 0 END), 0),
            @CreditTotal = ISNULL(SUM(CASE WHEN CreditOrDebit = 1 THEN EntryValue ELSE 0 END), 0)
        FROM @AccountingLines
        WHERE EntryValue > 0;

        IF ROUND(ISNULL(@DebitTotal, 0), 2) <> ROUND(ISNULL(@CreditTotal, 0), 2)
            RAISERROR('Invoice accounting entries are not balanced.', 16, 1);

        SET @NoteValue = @DebitTotal;

        INSERT INTO dbo.Notes
        (
            NoteID,
            NoteDate,
            NoteType,
            NoteSerial,
            NoteSerial1,
            Note_Value,
            Transaction_ID,
            UserID,
            Remark,
            CusID,
            BoxID,
            type,
            branch_no,
            sanad_type,
            sanad_source,
            Double_Entry_Vouchers_ID,
            PaymentType,
            Prefix
        )
        VALUES
        (
            @InvoiceNoteID,
            @TransactionDate,
            170,
            CAST(@InvoiceNoteSerial AS DECIMAL(38, 0)),
            CAST(@NoteSerial1 AS DECIMAL(38, 0)),
            CONVERT(FLOAT, @NoteValue),
            @TransactionID,
            @UserID,
            @AccountingDescription,
            @CustomerID,
            @BoxID,
            N'POS',
            @BranchId,
            N'170',
            N'POS',
            @DevID,
            @PaymentType,
            @Prefix
        );

        UPDATE dbo.Transactions
        SET NoteId = @InvoiceNoteID,
            NoteSerial = CONVERT(NVARCHAR(50), CAST(@InvoiceNoteSerial AS DECIMAL(38, 0)))
        WHERE Transaction_ID = @TransactionID;

        INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
        (
            Double_Entry_Vouchers_ID,
            DEV_ID_Line_No,
            Account_Code,
            Value,
            Credit_Or_Debit,
            Double_Entry_Vouchers_Description,
            RecordDate,
            Notes_ID,
            Transaction_ID,
            UserID,
            DEV_Serial,
            currency,
            rate,
            branch_id,
            DueDate
        )
        SELECT
            @DevID,
            LineNumber,
            AccountCode,
            EntryValue,
            CreditOrDebit,
            EntryDescription,
            @TransactionDate,
            @InvoiceNoteID,
            @TransactionID,
            @UserID,
            @DevSerial,
            N'',
            1,
            @BranchId,
            @TransactionDate
        FROM @AccountingLines
        WHERE EntryValue > 0;

        /*
            Issue Voucher / stock:
            Replicates the safe, verified part of FrmSaleBill6.CreateIssueVoucher
            and CreateIssueVoucher2:
            - Create Transaction_Type = 19.
            - Group by Transaction_Details.StoreID2 for multi-store vouchers.
            - Copy detail rows from the sale where SavedItemType = 0.
            - Copy Transaction_Details.ItemSerial, because VB6 writes txtVisaNumber/token into FG.Serial.
            - Link sale NOTS to the last created issue voucher, matching the VB6 loop.
            - Link issue voucher nots back to the sale transaction.
            - Create Notes row with NoteType = 180 and link it to the issue voucher.

            Intentionally not included here:
            - Closing Voucher accounting. Daily branch/day closing belongs to
              project_status/createVoucher8/CREATE_VOUCHER_GE4 and links
              transactions through Transactions.NoteIDClose.
            - UpdateTransactionsCost detailed recalculation.
            - RawMaterMix / FillMixToVoucher expansion.
        */
        DECLARE IssueStoreCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT DISTINCT StoreID2
            FROM dbo.Transaction_Details
            WHERE Transaction_ID = @TransactionID
              AND ISNULL(@TrafficViolations, 0) = 0
              AND ISNULL(SavedItemType, 0) = 0
            ORDER BY StoreID2;

        OPEN IssueStoreCursor;
        FETCH NEXT FROM IssueStoreCursor INTO @IssueStoreID;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @IssueTransactionID = NULL;
            SET @IssueNextTransactionID = NULL;
            SET @IssueNoteID = NULL;
            SET @IssueNextNoteID = NULL;
            SET @IssueNoteSerial = NULL;
            SET @IssueNoteSerial1 = NULL;
            SET @IssueMSerOut = NULL;
            SET @IssueTotalCost = 0;
            SET @NextIDError = NULL;

            EXEC dbo.GetNextID_FromSequence
                @TableName = N'Transactions',
                @FieldName = N'Transaction_ID',
                @NextValue = @IssueNextTransactionID OUTPUT,
                @ErrorMsg = @NextIDError OUTPUT;

            IF @NextIDError IS NOT NULL OR @IssueNextTransactionID IS NULL
            BEGIN
                SET @AllocationError = N'Unable to allocate Issue Voucher Transaction_ID. Source=dbo.GetNextID_FromSequence; Table=dbo.Transactions; Field=Transaction_ID; Error=' + ISNULL(@NextIDError, N'<null>') + N'; NextValue=' + ISNULL(CONVERT(NVARCHAR(50), @IssueNextTransactionID), N'<null>');
                RAISERROR(@AllocationError, 16, 1);
            END;

            IF @IssueNextTransactionID > 2147483647
                RAISERROR('Allocated Issue Voucher Transaction_ID exceeds INT range.', 16, 1);

            SET @IssueTransactionID = CONVERT(INT, @IssueNextTransactionID);
            SET @NextIDError = NULL;

            EXEC dbo.GetNextID_FromSequence
                @TableName = N'Notes',
                @FieldName = N'NoteID',
                @NextValue = @IssueNextNoteID OUTPUT,
                @ErrorMsg = @NextIDError OUTPUT;

            IF @NextIDError IS NOT NULL OR @IssueNextNoteID IS NULL
            BEGIN
                SET @AllocationError = N'Unable to allocate Issue Voucher NoteID. Source=dbo.GetNextID_FromSequence; Table=dbo.Notes; Field=NoteID; Error=' + ISNULL(@NextIDError, N'<null>') + N'; NextValue=' + ISNULL(CONVERT(NVARCHAR(50), @IssueNextNoteID), N'<null>');
                RAISERROR(@AllocationError, 16, 1);
            END;

            IF @IssueNextNoteID > 2147483647
                RAISERROR('Allocated Issue Voucher NoteID exceeds INT range.', 16, 1);

            SET @IssueNoteID = CONVERT(INT, @IssueNextNoteID);

            EXEC @IssueNoteReturnCode = dbo.usp_Notes_coding_V2
                @my_branch = @BranchId,
                @date1 = @TransactionDate,
                @departement_name = 1,
                @Result = @IssueNoteSerial OUTPUT;

            IF @IssueNoteReturnCode <> 0 OR @IssueNoteSerial IS NULL OR @IssueNoteSerial = 'error'
                RAISERROR('Unable to generate Issue Voucher NoteSerial using dbo.usp_Notes_coding_V2.', 16, 1);

            EXEC @VoucherReturnCode = dbo.usp_Voucher_coding_V2
                @my_branch = @BranchId,
                @date1 = @TransactionDate,
                @Sanad_No = 10,
                @NoteType = 180,
                @departement_name = 1,
                @Transaction_Type = 19,
                @Prefix = @Prefix,
                @StoreID = @IssueStoreID,
                @BillType = 0,
                @MosemID = 0,
                @mTableName = NULL,
                @mUserID = @UserID,
                @Result = @IssueNoteSerial1 OUTPUT,
                @mSerInv = @IssueMSerOut OUTPUT;

            IF @VoucherReturnCode <> 0 OR @IssueNoteSerial1 IS NULL OR @IssueNoteSerial1 = 'error'
                RAISERROR('Unable to generate Issue Voucher NoteSerial1 using dbo.usp_Voucher_coding_V2.', 16, 1);

            SELECT
                @IssueTotalCost = SUM(ISNULL(CostPrice, 0) * ISNULL(Quantity, 0))
            FROM dbo.Transaction_Details
            WHERE Transaction_ID = @TransactionID
              AND ISNULL(SavedItemType, 0) = 0
              AND ISNULL(StoreID2, -2147483648) = ISNULL(@IssueStoreID, -2147483648);

            INSERT INTO dbo.Transactions
            (
                Transaction_ID,
                Transaction_Serial,
                Transaction_Date,
                Transaction_Type,
                CusID,
                StoreID,
                UserID,
                nots,
                nots2,
                NoteSerial,
                NoteSerial1,
                NoteId,
                BranchId,
                CashCustomerName,
                CashCustomerPhone,
                Ser
            )
            SELECT
                @IssueTransactionID,
                CONVERT(NVARCHAR(50), @IssueMSerOut),
                t.Transaction_Date,
                19,
                t.CusID,
                @IssueStoreID,
                t.UserID,
                CONVERT(NVARCHAR(50), @TransactionID),
                t.NoteSerial1,
                @IssueNoteSerial,
                @IssueNoteSerial1,
                @IssueNoteID,
                t.BranchId,
                t.CashCustomerName,
                t.CashCustomerPhone,
                CONVERT(INT, @IssueMSerOut)
            FROM dbo.Transactions AS t
            WHERE t.Transaction_ID = @TransactionID
              AND t.Transaction_Type = 21;

            INSERT INTO dbo.Notes
            (
                NoteID,
                NoteDate,
                NoteType,
                NoteSerial,
                NoteSerial1,
                Note_Value,
                Transaction_ID,
                UserID,
                Remark,
                branch_no,
                sanad_year,
                sanad_month,
                DateTimeEntry
            )
            VALUES
            (
                @IssueNoteID,
                @TransactionDate,
                180,
                CASE WHEN ISNUMERIC(@IssueNoteSerial) = 1 THEN CAST(@IssueNoteSerial AS DECIMAL(38, 0)) ELSE NULL END,
                CASE WHEN ISNUMERIC(@IssueNoteSerial1) = 1 THEN CAST(@IssueNoteSerial1 AS DECIMAL(38, 0)) ELSE NULL END,
                @IssueTotalCost,
                @IssueTransactionID,
                @UserID,
                @IssueNoteSerial1,
                @BranchId,
                YEAR(@TransactionDate),
                MONTH(@TransactionDate),
                GETDATE()
            );

            INSERT INTO dbo.Transaction_Details
            (
                Transaction_ID,
                Item_ID,
                Quantity,
                Price,
                UnitId,
                ShowQty,
                QtyBySmalltUnit,
                showPrice,
                TotalPrice,
                StoreID2,
                ItemSerial,
                ItemCase,
                CostPrice,
                SavedItemType,
                BranchId
            )
            SELECT
                @IssueTransactionID,
                d.Item_ID,
                d.Quantity,
                d.Price,
                d.UnitId,
                d.ShowQty,
                ISNULL(NULLIF(d.QtyBySmalltUnit, 0), 1),
                d.showPrice,
                d.TotalPrice,
                d.StoreID2,
                d.ItemSerial,
                d.ItemCase,
                d.CostPrice,
                d.SavedItemType,
                d.BranchId
            FROM dbo.Transaction_Details AS d
            WHERE d.Transaction_ID = @TransactionID
              AND ISNULL(d.SavedItemType, 0) = 0
              AND ISNULL(d.StoreID2, -2147483648) = ISNULL(@IssueStoreID, -2147483648);

            INSERT INTO @IssueVouchers
            (
                IssueTransactionID,
                StoreID,
                NoteID,
                NoteSerial,
                NoteSerial1
            )
            VALUES
            (
                @IssueTransactionID,
                @IssueStoreID,
                @IssueNoteID,
                @IssueNoteSerial,
                @IssueNoteSerial1
            );

            SET @LastIssueTransactionID = @IssueTransactionID;

            FETCH NEXT FROM IssueStoreCursor INTO @IssueStoreID;
        END

        CLOSE IssueStoreCursor;
        DEALLOCATE IssueStoreCursor;

        IF @LastIssueTransactionID IS NOT NULL
        BEGIN
            UPDATE dbo.Transactions
            SET NOTS = CONVERT(NVARCHAR(50), @LastIssueTransactionID)
            WHERE Transaction_ID = @TransactionID;
        END

        /*
            TODO - Closing Voucher accounting:
            Closing accounting is a separate daily branch/day process.
            Source logic is project_status/createVoucher8/CREATE_VOUCHER_GE4.
            It links saved transactions through Transactions.NoteIDClose.
            Closing must not replace or alter the invoice-level PG accounting
            created above.
        */

        COMMIT TRANSACTION;

        SELECT
            @TransactionID AS Transaction_ID,
            @NoteSerial1 AS NoteSerial1,
            @mSerInv AS NoteSerial1TailNumber;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH
END

GO


