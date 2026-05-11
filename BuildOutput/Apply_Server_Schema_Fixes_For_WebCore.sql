
/* ===== 0004_ServiceInvoice_CashReceiptVoucher.sql ===== */

IF OBJECT_ID(N'dbo.ServiceInvoiceActualPayment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceInvoiceActualPayment
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ServiceInvoiceActualPayment PRIMARY KEY,
        ServiceInvoiceId int NULL,
        ServiceInvoiceDate datetime NULL,
        ServiceInvoiceDocNumber nvarchar(500) NULL,
        CashReceiptVoucherId int NULL,
        Amount money NULL,
        PaymentDate datetime NULL,
        UserId int NULL,
        IsActive bit NOT NULL CONSTRAINT DF_ServiceInvoiceActualPayment_IsActive DEFAULT (1),
        IsDeleted bit NOT NULL CONSTRAINT DF_ServiceInvoiceActualPayment_IsDeleted DEFAULT (0),
        Notes nvarchar(max) NULL,
        NetAmountRemain money NULL
    );

    ALTER TABLE dbo.ServiceInvoiceActualPayment
        ADD CONSTRAINT FK_ServiceInvoiceActualPayment_ServiceInvoice
        FOREIGN KEY (ServiceInvoiceId) REFERENCES dbo.ServiceInvoice(Id);

    ALTER TABLE dbo.ServiceInvoiceActualPayment
        ADD CONSTRAINT FK_ServiceInvoiceActualPayment_CashReceiptVoucher
        FOREIGN KEY (CashReceiptVoucherId) REFERENCES dbo.CashReceiptVoucher(Id);

    ALTER TABLE dbo.ServiceInvoiceActualPayment
        ADD CONSTRAINT FK_ServiceInvoiceActualPayment_ERPUser
        FOREIGN KEY (UserId) REFERENCES dbo.ERPUser(Id);
END
GO

IF OBJECT_ID(N'dbo.GetSalesInvoiceActualPayment', N'P') IS NOT NULL
    DROP PROCEDURE dbo.GetSalesInvoiceActualPayment;
GO

CREATE PROCEDURE dbo.GetSalesInvoiceActualPayment
    @VendorOrCustomerId int,
    @CashReceiptVoucherId int,
    @DepartmentId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.Id,
        s.DocumentNumber,
        s.VoucherDate,
        s.DueDate,
        s.DepartmentId,
        d.ArName AS DepartmentArName,
        s.TotalAfterTaxes,
        m.Amount,
        ISNULL(m.Amount, 0) AS PaidAmount,
        ISNULL(ISNULL(s.TotalAfterTaxes, 0) - (ISNULL(m.NetAmountRemain, 0) + ISNULL(m.Amount, 0)), 0) AS Paid,
        ISNULL(m.Amount, 0) AS PaidInTransaction,
        ISNULL(m.NetAmountRemain, 0) AS RemainAmount,
        CAST(N'SalesInvoice' AS nvarchar(50)) AS InvoiceSourceType,
        CAST(N'Sales Invoice' AS nvarchar(100)) AS InvoiceSourceName,
        s.Id AS SalesInvoiceId,
        CAST(NULL AS int) AS ServiceInvoiceId
    FROM dbo.SalesInvoice s
    INNER JOIN dbo.Department d ON s.DepartmentId = d.Id
    INNER JOIN dbo.SalesInvoiceActualPayment m ON s.Id = m.SalesInvoiceId
    WHERE s.Id IN (SELECT SalesInvoiceId FROM dbo.SalesInvoiceActualPayment WHERE CashReceiptVoucherId = @CashReceiptVoucherId)
      AND s.VendorOrCustomerId = @VendorOrCustomerId
      AND s.IsActive = 1
      AND s.IsDeleted = 0
      AND (@DepartmentId IS NULL OR s.DepartmentId = @DepartmentId)

    UNION ALL

    SELECT
        s.Id,
        s.DocumentNumber,
        s.VoucherDate,
        s.DueDate,
        s.DepartmentId,
        d.ArName AS DepartmentArName,
        s.TotalAfterTaxes,
        m.Amount,
        ISNULL(m.PaidAmount, 0) AS PaidAmount,
        ISNULL(((s.TotalAfterTaxes - m.Amount) + ISNULL(m.PaidAmount, 0)), 0) AS Paid,
        0 AS PaidInTransaction,
        ISNULL(m.Amount - ISNULL(m.PaidAmount, 0), 0) AS RemainAmount,
        CAST(N'SalesInvoice' AS nvarchar(50)) AS InvoiceSourceType,
        CAST(N'Sales Invoice' AS nvarchar(100)) AS InvoiceSourceName,
        s.Id AS SalesInvoiceId,
        CAST(NULL AS int) AS ServiceInvoiceId
    FROM dbo.SalesInvoice s
    INNER JOIN dbo.Department d ON s.DepartmentId = d.Id
    INNER JOIN dbo.SalesInvoicePaymentMethods m ON s.Id = m.SalesInvoiceId
    WHERE s.Id IN (
            SELECT SalesInvoiceId
            FROM dbo.SalesInvoicePaymentMethods
            WHERE PaymentMethodId = 2
              AND Amount > 0
              AND Amount > ISNULL(PaidAmount, 0)
        )
      AND s.Id NOT IN (
            SELECT SalesInvoiceId
            FROM dbo.SalesInvoiceActualPayment
            WHERE CashReceiptVoucherId = @CashReceiptVoucherId
              AND SalesInvoiceId IS NOT NULL
        )
      AND s.VendorOrCustomerId = @VendorOrCustomerId
      AND s.IsActive = 1
      AND s.IsDeleted = 0
      AND m.PaymentMethodId = 2
      AND (@DepartmentId IS NULL OR s.DepartmentId = @DepartmentId)

    UNION ALL

    SELECT
        s.Id,
        s.DocumentNumber,
        s.VoucherDate,
        s.DueDate,
        s.DepartmentId,
        d.ArName AS DepartmentArName,
        s.TotalAfterTaxes,
        m.Amount,
        ISNULL(m.Amount, 0) AS PaidAmount,
        ISNULL(ISNULL(s.TotalAfterTaxes, 0) - (ISNULL(m.NetAmountRemain, 0) + ISNULL(m.Amount, 0)), 0) AS Paid,
        ISNULL(m.Amount, 0) AS PaidInTransaction,
        ISNULL(m.NetAmountRemain, 0) AS RemainAmount,
        CAST(N'ServiceInvoice' AS nvarchar(50)) AS InvoiceSourceType,
        CAST(N'Service Invoice' AS nvarchar(100)) AS InvoiceSourceName,
        CAST(NULL AS int) AS SalesInvoiceId,
        s.Id AS ServiceInvoiceId
    FROM dbo.ServiceInvoice s
    INNER JOIN dbo.Department d ON s.DepartmentId = d.Id
    INNER JOIN dbo.ServiceInvoiceActualPayment m ON s.Id = m.ServiceInvoiceId
    WHERE s.Id IN (SELECT ServiceInvoiceId FROM dbo.ServiceInvoiceActualPayment WHERE CashReceiptVoucherId = @CashReceiptVoucherId)
      AND s.CustomerId = @VendorOrCustomerId
      AND s.IsActive = 1
      AND s.IsDeleted = 0
      AND (@DepartmentId IS NULL OR s.DepartmentId = @DepartmentId)

    UNION ALL

    SELECT
        s.Id,
        s.DocumentNumber,
        s.VoucherDate,
        s.DueDate,
        s.DepartmentId,
        d.ArName AS DepartmentArName,
        s.TotalAfterTaxes,
        m.Amount,
        ISNULL(m.PaidAmount, 0) AS PaidAmount,
        ISNULL(((s.TotalAfterTaxes - m.Amount) + ISNULL(m.PaidAmount, 0)), 0) AS Paid,
        0 AS PaidInTransaction,
        ISNULL(m.Amount - ISNULL(m.PaidAmount, 0), 0) AS RemainAmount,
        CAST(N'ServiceInvoice' AS nvarchar(50)) AS InvoiceSourceType,
        CAST(N'Service Invoice' AS nvarchar(100)) AS InvoiceSourceName,
        CAST(NULL AS int) AS SalesInvoiceId,
        s.Id AS ServiceInvoiceId
    FROM dbo.ServiceInvoice s
    INNER JOIN dbo.Department d ON s.DepartmentId = d.Id
    INNER JOIN dbo.ServiceInvoicePaymentMethod m ON s.Id = m.ServiceInvoiceId
    WHERE s.Id IN (
            SELECT ServiceInvoiceId
            FROM dbo.ServiceInvoicePaymentMethod
            WHERE PaymentMethodId = 2
              AND Amount > 0
              AND Amount > ISNULL(PaidAmount, 0)
        )
      AND s.Id NOT IN (
            SELECT ServiceInvoiceId
            FROM dbo.ServiceInvoiceActualPayment
            WHERE CashReceiptVoucherId = @CashReceiptVoucherId
              AND ServiceInvoiceId IS NOT NULL
        )
      AND s.CustomerId = @VendorOrCustomerId
      AND s.IsActive = 1
      AND s.IsDeleted = 0
      AND m.PaymentMethodId = 2
      AND (@DepartmentId IS NULL OR s.DepartmentId = @DepartmentId)
    ORDER BY DueDate;
END
GO

IF OBJECT_ID(N'dbo.ServiceInvoiceActualPayment_SaveForCashReceipt', N'P') IS NOT NULL
    DROP PROCEDURE dbo.ServiceInvoiceActualPayment_SaveForCashReceipt;
GO

CREATE PROCEDURE dbo.ServiceInvoiceActualPayment_SaveForCashReceipt
    @CashReceiptVoucherId int,
    @Payments ntext,
    @UserId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TranCount int = @@TRANCOUNT;

    BEGIN TRY
        IF @TranCount = 0
            BEGIN TRANSACTION;
        ELSE
            SAVE TRANSACTION SvcInvPaySave;

        UPDATE pm
        SET pm.PaidAmount = CASE
                                WHEN ISNULL(pm.PaidAmount, 0) - ISNULL(p.Amount, 0) < 0 THEN 0
                                ELSE ISNULL(pm.PaidAmount, 0) - ISNULL(p.Amount, 0)
                            END
        FROM dbo.ServiceInvoicePaymentMethod pm
        INNER JOIN dbo.ServiceInvoiceActualPayment p
            ON p.ServiceInvoiceId = pm.ServiceInvoiceId
        WHERE p.CashReceiptVoucherId = @CashReceiptVoucherId
          AND pm.PaymentMethodId = 2;

        DELETE FROM dbo.ServiceInvoiceActualPayment
        WHERE CashReceiptVoucherId = @CashReceiptVoucherId;

        DECLARE @XmlHandle int;
        EXEC sp_xml_preparedocument @XmlHandle OUTPUT, @Payments;

        DECLARE @Incoming TABLE
        (
            ServiceInvoiceId int NULL,
            ServiceInvoiceDate datetime NULL,
            ServiceInvoiceDocNumber nvarchar(500) NULL,
            Amount money NULL,
            PaymentDate datetime NULL,
            Notes nvarchar(max) NULL,
            NetAmountRemain money NULL
        );

        INSERT INTO @Incoming
        (
            ServiceInvoiceId,
            ServiceInvoiceDate,
            ServiceInvoiceDocNumber,
            Amount,
            PaymentDate,
            Notes,
            NetAmountRemain
        )
        SELECT
            ServiceInvoiceId,
            SalesInvoiceDate,
            SalesInvoiceDocNumber,
            Amount,
            PaymentDate,
            Notes,
            NetAmountRemain
        FROM OPENXML(@XmlHandle, '/DocumentElement/SalesInvoiceActualPayment', 2)
        WITH
        (
            ServiceInvoiceId int,
            SalesInvoiceDate datetime,
            SalesInvoiceDocNumber nvarchar(500),
            Amount money,
            PaymentDate datetime,
            Notes nvarchar(max),
            NetAmountRemain money
        )
        WHERE ISNULL(Amount, 0) > 0
          AND ServiceInvoiceId IS NOT NULL;

        EXEC sp_xml_removedocument @XmlHandle;

        IF EXISTS
        (
            SELECT 1
            FROM @Incoming i
            INNER JOIN dbo.ServiceInvoicePaymentMethod pm
                ON pm.ServiceInvoiceId = i.ServiceInvoiceId
               AND pm.PaymentMethodId = 2
            WHERE ISNULL(i.Amount, 0) > ISNULL(pm.Amount, 0) - ISNULL(pm.PaidAmount, 0) + 0.01
        )
        BEGIN
            RAISERROR(N'Service invoice payment cannot exceed remaining amount.', 16, 1);
            RETURN;
        END

        INSERT INTO dbo.ServiceInvoiceActualPayment
        (
            ServiceInvoiceId,
            ServiceInvoiceDate,
            ServiceInvoiceDocNumber,
            CashReceiptVoucherId,
            Amount,
            PaymentDate,
            UserId,
            IsActive,
            IsDeleted,
            Notes,
            NetAmountRemain
        )
        SELECT
            ServiceInvoiceId,
            ServiceInvoiceDate,
            ServiceInvoiceDocNumber,
            @CashReceiptVoucherId,
            Amount,
            PaymentDate,
            @UserId,
            1,
            0,
            Notes,
            NetAmountRemain
        FROM @Incoming;

        UPDATE pm
        SET pm.PaidAmount = ISNULL(pm.PaidAmount, 0) + ISNULL(i.Amount, 0),
            pm.PaidDate = i.PaymentDate
        FROM dbo.ServiceInvoicePaymentMethod pm
        INNER JOIN @Incoming i
            ON i.ServiceInvoiceId = pm.ServiceInvoiceId
        WHERE pm.PaymentMethodId = 2;

        IF @TranCount = 0
            COMMIT;
    END TRY
    BEGIN CATCH
        DECLARE @Error int, @Message nvarchar(4000), @XState int;
        SELECT @Error = ERROR_NUMBER(),
               @Message = ERROR_MESSAGE() + N' at ' + CAST(ERROR_LINE() AS nvarchar(50)),
               @XState = XACT_STATE();

        IF @XState = -1
            ROLLBACK;
        IF @XState = 1 AND @TranCount = 0
            ROLLBACK;
        IF @XState = 1 AND @TranCount > 0
            ROLLBACK TRANSACTION SvcInvPaySave;

        RAISERROR(N'ServiceInvoiceActualPayment_SaveForCashReceipt: %d: %s', 16, 1, @Error, @Message);
    END CATCH
END
GO

/* ===== Add_DepartmentId_To_JournalEntryDetail_Only.sql ===== */

IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NULL
BEGIN
    ALTER TABLE dbo.JournalEntryDetail ADD DepartmentId int NULL;
END
GO

IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.JournalEntry', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NOT NULL
BEGIN
    EXEC('
        UPDATE jed
        SET DepartmentId = je.DepartmentId
        FROM dbo.JournalEntryDetail jed
        INNER JOIN dbo.JournalEntry je ON je.Id = jed.JournalEntryId
        WHERE jed.DepartmentId IS NULL;
    ');
END
GO

IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = 'IX_JournalEntryDetail_DepartmentId'
         AND object_id = OBJECT_ID('dbo.JournalEntryDetail')
   )
BEGIN
    CREATE INDEX IX_JournalEntryDetail_DepartmentId
    ON dbo.JournalEntryDetail(DepartmentId);
END
GO

IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Department', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NOT NULL
   AND OBJECT_ID('dbo.FK_JournalEntryDetail_Department', 'F') IS NULL
BEGIN
    ALTER TABLE dbo.JournalEntryDetail WITH NOCHECK
    ADD CONSTRAINT FK_JournalEntryDetail_Department
    FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(Id);
END
GO

/* ===== Add_LastModified_To_SalesInvoice_Only.sql ===== */

IF OBJECT_ID('dbo.SalesInvoice', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SalesInvoice', 'LastModifiedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.SalesInvoice ADD LastModifiedByUserId int NULL;
END
GO

IF OBJECT_ID('dbo.SalesInvoice', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SalesInvoice', 'LastModifiedDate') IS NULL
BEGIN
    ALTER TABLE dbo.SalesInvoice ADD LastModifiedDate datetime NULL;
END
GO

IF OBJECT_ID('dbo.SalesInvoice', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SalesInvoice', 'LastModifiedByUserId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = 'IX_SalesInvoice_LastModifiedByUserId'
         AND object_id = OBJECT_ID('dbo.SalesInvoice')
   )
BEGIN
    CREATE INDEX IX_SalesInvoice_LastModifiedByUserId
    ON dbo.SalesInvoice(LastModifiedByUserId);
END
GO

IF OBJECT_ID('dbo.SalesInvoice', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ERPUsers', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SalesInvoice', 'LastModifiedByUserId') IS NOT NULL
   AND OBJECT_ID('dbo.FK_SalesInvoice_LastModifiedByUser', 'F') IS NULL
BEGIN
    ALTER TABLE dbo.SalesInvoice WITH NOCHECK
    ADD CONSTRAINT FK_SalesInvoice_LastModifiedByUser
    FOREIGN KEY (LastModifiedByUserId) REFERENCES dbo.ERPUsers(Id);
END
GO
