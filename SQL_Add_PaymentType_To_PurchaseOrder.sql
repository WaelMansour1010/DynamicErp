/*
==============================================================
    PAYMENT TYPE FOR PURCHASE ORDER - DATABASE CHANGES
    Date: 2026-04-11
    Status: SHARED Architecture - DocumentPaymentMethods
==============================================================

    Summary:
    1. Create shared DocumentPaymentMethods table
    2. Add PaymentType column to PurchaseOrders
    3. Update PurchaseOrder_Insert stored procedure
    4. Update PurchaseOrder_Update stored procedure

    NOTE: PaymentMethod lookup table remains SHARED and UNCHANGED.
          No purchase-specific payment method types are created.
*/

USE [YourDatabaseName]  -- CHANGE THIS to your actual database name
GO

-- ============================================================
-- STEP 1: Create SHARED DocumentPaymentMethods Table
-- ============================================================
-- This table is shared across ALL document types (SalesInvoice, PurchaseOrder, etc.)
-- It does NOT duplicate payment method logic - only links documents to payment methods

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentPaymentMethods]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DocumentPaymentMethods] (
        [Id]                INT             IDENTITY(1,1) NOT NULL,
        [DocumentType]      NVARCHAR(50)    NOT NULL,     -- 'SalesInvoice', 'PurchaseOrder', etc.
        [DocumentId]        INT             NOT NULL,     -- FK to the owning document
        [PaymentMethodId]   INT             NOT NULL,     -- FK → PaymentMethods (SHARED lookup)
        [Amount]            DECIMAL(18, 2)  NULL,
        [CashBoxId]         INT             NULL,
        [BankId]            INT             NULL,
        [BankAccountId]     INT             NULL,
        [PaidAmount]        DECIMAL(18, 2)  NULL,
        [PaidDate]          DATETIME        NULL,
        
        CONSTRAINT [PK_DocumentPaymentMethods] PRIMARY KEY CLUSTERED ([Id] ASC),
        
        CONSTRAINT [FK_DocumentPaymentMethods_PaymentMethods] 
            FOREIGN KEY ([PaymentMethodId]) 
            REFERENCES [dbo].[PaymentMethods] ([Id])
    )

    PRINT '✓ Created DocumentPaymentMethods table'
END
ELSE
BEGIN
    PRINT '⚠ DocumentPaymentMethods table already exists - skipping'
END
GO

-- Create index for performance on DocumentType + DocumentId lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentPaymentMethods_DocumentType_DocumentId' AND object_id = OBJECT_ID(N'[dbo].[DocumentPaymentMethods]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocumentPaymentMethods_DocumentType_DocumentId]
    ON [dbo].[DocumentPaymentMethods]([DocumentType], [DocumentId])
    INCLUDE ([PaymentMethodId], [Amount])

    PRINT '✓ Created index IX_DocumentPaymentMethods_DocumentType_DocumentId'
END
ELSE
BEGIN
    PRINT '⚠ Index IX_DocumentPaymentMethods_DocumentType_DocumentId already exists - skipping'
END
GO


-- ============================================================
-- STEP 2: Add PaymentType Column to PurchaseOrders Table
-- ============================================================
-- 1 = Cash (نقدى), 2 = Credit (آجل), 3 = Multiple (متعدد)

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrders]') AND name = 'PaymentType')
BEGIN
    ALTER TABLE [dbo].[PurchaseOrders]
    ADD [PaymentType] INT NULL

    PRINT '✓ Added PaymentType column to PurchaseOrders'
END
ELSE
BEGIN
    PRINT '⚠ PaymentType column already exists in PurchaseOrders - skipping'
END
GO


-- ============================================================
-- STEP 3: Update PurchaseOrder_Insert Stored Procedure
-- ============================================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrder_Insert]') AND type in (N'P', N'PC'))
BEGIN
    PRINT '⚠ Dropping existing PurchaseOrder_Insert procedure...'
    DROP PROCEDURE [dbo].[PurchaseOrder_Insert]
END
GO

CREATE PROCEDURE [dbo].[PurchaseOrder_Insert]
    @Id                         INT             OUTPUT,
    @BranchId                   INT             = NULL,
    @WarehouseId                INT,
    @DepartmentId               INT,
    @VoucherDate                DATETIME,
    @VendorOrCustomerId         INT             = NULL,
    @CurrencyId                 INT             = NULL,
    @CurrencyEquivalent         FLOAT           = NULL,
    @Total                      DECIMAL(18,2)   = NULL,
    @TotalItemsDiscount         DECIMAL(18,2)   = NULL,
    @SalesTaxes                 DECIMAL(18,2)   = NULL,
    @TotalAfterTaxes            DECIMAL(18,2)   = NULL,
    @VoucherDiscountValue       DECIMAL(18,2)   = NULL,
    @VoucherDiscountPercentage  FLOAT           = NULL,
    @NetTotal                   DECIMAL(18,2)   = NULL,
    @Paid                       DECIMAL(18,2)   = NULL,
    @ValidityPeriod             FLOAT           = NULL,
    @DeliveryPeriod             FLOAT           = NULL,
    @CostPriceId                INT             = NULL,
    @CurrentQuantity            FLOAT           = NULL,
    @DestinationWarehouseId     INT             = NULL,
    @SystemPageId               INT             = NULL,
    @SelectedId                 INT             = NULL,
    @TotalCostPrice             DECIMAL(18,2)   = NULL,
    @TotalItemDirectExpenses    DECIMAL(18,2)   = NULL,
    @IsDelivered                BIT             = NULL,
    @IsAccepted                 BIT             = NULL,
    @IsLinked                   BIT             = NULL,
    @IsCompleted                BIT             = NULL,
    @IsPosted                   BIT             = NULL,
    @UserId                     INT             = NULL,
    @IsActive                   BIT,
    @IsDeleted                  BIT,
    @AutoCreated                BIT             = NULL,
    @Notes                      NVARCHAR(MAX)   = NULL,
    @Image                      NVARCHAR(MAX)   = NULL,
    @UpdatedId                  INT             = NULL,
    @CommercialRevenueTax       DECIMAL(18,2)   = NULL,
    @Details                    XML,
    @PaymentMethods             XML             = NULL,  -- NEW: Payment methods XML
    @PaymentType                INT             = NULL   -- NEW: Payment type (1=Cash, 2=Credit, 3=Multiple)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION

        -- Insert PurchaseOrder header
        INSERT INTO [dbo].[PurchaseOrders] (
            [BranchId], [WarehouseId], [DepartmentId], [VoucherDate],
            [VendorOrCustomerId], [CurrencyId], [CurrencyEquivalent],
            [Total], [TotalItemsDiscount], [SalesTaxes], [TotalAfterTaxes],
            [VoucherDiscountValue], [VoucherDiscountPercentage], [NetTotal], [Paid],
            [ValidityPeriod], [DeliveryPeriod], [CostPriceId], [CurrentQuantity],
            [DestinationWarehouseId], [SystemPageId], [SelectedId], [TotalCostPrice],
            [TotalItemDirectExpenses], [IsDelivered], [IsAccepted], [IsLinked],
            [IsCompleted], [IsPosted], [UserId], [IsActive], [IsDeleted], [AutoCreated],
            [Notes], [Image], [UpdatedId], [CommercialRevenueTax], [PaymentType]
        ) VALUES (
            @BranchId, @WarehouseId, @DepartmentId, @VoucherDate,
            @VendorOrCustomerId, @CurrencyId, @CurrencyEquivalent,
            @Total, @TotalItemsDiscount, @SalesTaxes, @TotalAfterTaxes,
            @VoucherDiscountValue, @VoucherDiscountPercentage, @NetTotal, @Paid,
            @ValidityPeriod, @DeliveryPeriod, @CostPriceId, @CurrentQuantity,
            @DestinationWarehouseId, @SystemPageId, @SelectedId, @TotalCostPrice,
            @TotalItemDirectExpenses, @IsDelivered, @IsAccepted, @IsLinked,
            @IsCompleted, @IsPosted, @UserId, @IsActive, @IsDeleted, @AutoCreated,
            @Notes, @Image, @UpdatedId, @CommercialRevenueTax, @PaymentType
        )

        SET @Id = SCOPE_IDENTITY()

        -- Insert PurchaseOrder details from XML
        IF @Details IS NOT NULL AND CAST(@Details AS NVARCHAR(MAX)) != ''
        BEGIN
            INSERT INTO [dbo].[PurchaseOrderDetails] (
                [PurchaseOrderId], [ItemPriceId], [Qty], [UnitEquivalent], [Price],
                [Total], [DiscountValue], [DiscountPercentage], [TaxAmount],
                [Notes], [SerialNumbers], [CostPrice], [DirectExpenseAmount],
                [CommercialRevenueTaxAmount]
            )
            SELECT
                @Id,
                T.c.value('(ItemPriceId/text())[1]', 'INT'),
                T.c.value('(Qty/text())[1]', 'FLOAT'),
                T.c.value('(UnitEquivalent/text())[1]', 'FLOAT'),
                T.c.value('(Price/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(Total/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(DiscountValue/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(DiscountPercentage/text())[1]', 'FLOAT'),
                T.c.value('(TaxAmount/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(Notes/text())[1]', 'NVARCHAR(MAX)'),
                T.c.value('(SerialNumbers/text())[1]', 'NVARCHAR(MAX)'),
                T.c.value('(CostPrice/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(DirectExpenseAmount/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(CommercialRevenueTaxAmount/text())[1]', 'DECIMAL(18,2)')
            FROM @Details.nodes('/Details/Detail') T(c)
        END

        -- Insert payment methods into SHARED DocumentPaymentMethods table
        IF @PaymentMethods IS NOT NULL AND CAST(@PaymentMethods AS NVARCHAR(MAX)) != ''
        BEGIN
            INSERT INTO [dbo].[DocumentPaymentMethods] (
                [DocumentType], [DocumentId], [PaymentMethodId], [Amount],
                [CashBoxId], [BankId], [BankAccountId], [PaidAmount], [PaidDate]
            )
            SELECT
                'PurchaseOrder',  -- DocumentType - distinguishes ownership
                @Id,              -- DocumentId - links to this PurchaseOrder
                T.c.value('(PaymentMethodId/text())[1]', 'INT'),
                T.c.value('(Amount/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(CashBoxId/text())[1]', 'INT'),
                T.c.value('(BankId/text())[1]', 'INT'),
                T.c.value('(BankAccountId/text())[1]', 'INT'),
                T.c.value('(PaidAmount/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(PaidDate/text())[1]', 'DATETIME')
            FROM @PaymentMethods.nodes('/PaymentMethods/PaymentMethod') T(c)
        END

        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE()
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY()
        DECLARE @ErrorState INT = ERROR_STATE()

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)
    END CATCH
END
GO

PRINT '✓ Created/Updated PurchaseOrder_Insert stored procedure'
GO


-- ============================================================
-- STEP 4: Update PurchaseOrder_Update Stored Procedure
-- ============================================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrder_Update]') AND type in (N'P', N'PC'))
BEGIN
    PRINT '⚠ Dropping existing PurchaseOrder_Update procedure...'
    DROP PROCEDURE [dbo].[PurchaseOrder_Update]
END
GO

CREATE PROCEDURE [dbo].[PurchaseOrder_Update]
    @Id                         INT,
    @DocumentNumber             NVARCHAR(50),
    @BranchId                   INT             = NULL,
    @WarehouseId                INT,
    @DepartmentId               INT,
    @VoucherDate                DATETIME,
    @VendorOrCustomerId         INT             = NULL,
    @CurrencyId                 INT             = NULL,
    @CurrencyEquivalent         FLOAT           = NULL,
    @Total                      DECIMAL(18,2)   = NULL,
    @TotalItemsDiscount         DECIMAL(18,2)   = NULL,
    @SalesTaxes                 DECIMAL(18,2)   = NULL,
    @TotalAfterTaxes            DECIMAL(18,2)   = NULL,
    @VoucherDiscountValue       DECIMAL(18,2)   = NULL,
    @VoucherDiscountPercentage  FLOAT           = NULL,
    @NetTotal                   DECIMAL(18,2)   = NULL,
    @Paid                       DECIMAL(18,2)   = NULL,
    @ValidityPeriod             FLOAT           = NULL,
    @DeliveryPeriod             FLOAT           = NULL,
    @CostPriceId                INT             = NULL,
    @CurrentQuantity            FLOAT           = NULL,
    @DestinationWarehouseId     INT             = NULL,
    @SystemPageId               INT             = NULL,
    @SelectedId                 INT             = NULL,
    @TotalCostPrice             DECIMAL(18,2)   = NULL,
    @TotalItemDirectExpenses    DECIMAL(18,2)   = NULL,
    @IsDelivered                BIT             = NULL,
    @IsAccepted                 BIT             = NULL,
    @IsLinked                   BIT             = NULL,
    @IsCompleted                BIT             = NULL,
    @IsPosted                   BIT             = NULL,
    @UserId                     INT             = NULL,
    @IsActive                   BIT,
    @IsDeleted                  BIT,
    @AutoCreated                BIT             = NULL,
    @Notes                      NVARCHAR(MAX)   = NULL,
    @Image                      NVARCHAR(MAX)   = NULL,
    @UpdatedId                  INT             = NULL,
    @CommercialRevenueTax       DECIMAL(18,2)   = NULL,
    @Details                    XML,
    @PaymentMethods             XML             = NULL,  -- NEW: Payment methods XML
    @PaymentType                INT             = NULL   -- NEW: Payment type (1=Cash, 2=Credit, 3=Multiple)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION

        -- Update PurchaseOrder header
        UPDATE [dbo].[PurchaseOrders]
        SET
            [DocumentNumber]            = @DocumentNumber,
            [BranchId]                  = @BranchId,
            [WarehouseId]               = @WarehouseId,
            [DepartmentId]              = @DepartmentId,
            [VoucherDate]               = @VoucherDate,
            [VendorOrCustomerId]        = @VendorOrCustomerId,
            [CurrencyId]                = @CurrencyId,
            [CurrencyEquivalent]        = @CurrencyEquivalent,
            [Total]                     = @Total,
            [TotalItemsDiscount]        = @TotalItemsDiscount,
            [SalesTaxes]                = @SalesTaxes,
            [TotalAfterTaxes]           = @TotalAfterTaxes,
            [VoucherDiscountValue]      = @VoucherDiscountValue,
            [VoucherDiscountPercentage] = @VoucherDiscountPercentage,
            [NetTotal]                  = @NetTotal,
            [Paid]                      = @Paid,
            [ValidityPeriod]            = @ValidityPeriod,
            [DeliveryPeriod]            = @DeliveryPeriod,
            [CostPriceId]               = @CostPriceId,
            [CurrentQuantity]           = @CurrentQuantity,
            [DestinationWarehouseId]    = @DestinationWarehouseId,
            [SystemPageId]              = @SystemPageId,
            [SelectedId]                = @SelectedId,
            [TotalCostPrice]            = @TotalCostPrice,
            [TotalItemDirectExpenses]   = @TotalItemDirectExpenses,
            [IsDelivered]               = @IsDelivered,
            [IsAccepted]                = @IsAccepted,
            [IsLinked]                  = @IsLinked,
            [IsCompleted]               = @IsCompleted,
            [IsPosted]                  = @IsPosted,
            [UserId]                    = @UserId,
            [IsActive]                  = @IsActive,
            [IsDeleted]                 = @IsDeleted,
            [AutoCreated]               = @AutoCreated,
            [Notes]                     = @Notes,
            [Image]                     = @Image,
            [UpdatedId]                 = @UpdatedId,
            [CommercialRevenueTax]      = @CommercialRevenueTax,
            [PaymentType]               = @PaymentType
        WHERE [Id] = @Id

        -- Delete existing details
        DELETE FROM [dbo].[PurchaseOrderDetails]
        WHERE [PurchaseOrderId] = @Id

        -- Insert updated details from XML
        IF @Details IS NOT NULL AND CAST(@Details AS NVARCHAR(MAX)) != ''
        BEGIN
            INSERT INTO [dbo].[PurchaseOrderDetails] (
                [PurchaseOrderId], [ItemPriceId], [Qty], [UnitEquivalent], [Price],
                [Total], [DiscountValue], [DiscountPercentage], [TaxAmount],
                [Notes], [SerialNumbers], [CostPrice], [DirectExpenseAmount],
                [CommercialRevenueTaxAmount]
            )
            SELECT
                @Id,
                T.c.value('(ItemPriceId/text())[1]', 'INT'),
                T.c.value('(Qty/text())[1]', 'FLOAT'),
                T.c.value('(UnitEquivalent/text())[1]', 'FLOAT'),
                T.c.value('(Price/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(Total/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(DiscountValue/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(DiscountPercentage/text())[1]', 'FLOAT'),
                T.c.value('(TaxAmount/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(Notes/text())[1]', 'NVARCHAR(MAX)'),
                T.c.value('(SerialNumbers/text())[1]', 'NVARCHAR(MAX)'),
                T.c.value('(CostPrice/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(DirectExpenseAmount/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(CommercialRevenueTaxAmount/text())[1]', 'DECIMAL(18,2)')
            FROM @Details.nodes('/Details/Detail') T(c)
        END

        -- Update payment methods in SHARED DocumentPaymentMethods table
        -- Delete existing payment methods for this PurchaseOrder
        DELETE FROM [dbo].[DocumentPaymentMethods]
        WHERE [DocumentType] = 'PurchaseOrder' AND [DocumentId] = @Id

        -- Insert updated payment methods from XML
        IF @PaymentMethods IS NOT NULL AND CAST(@PaymentMethods AS NVARCHAR(MAX)) != ''
        BEGIN
            INSERT INTO [dbo].[DocumentPaymentMethods] (
                [DocumentType], [DocumentId], [PaymentMethodId], [Amount],
                [CashBoxId], [BankId], [BankAccountId], [PaidAmount], [PaidDate]
            )
            SELECT
                'PurchaseOrder',  -- DocumentType - distinguishes ownership
                @Id,              -- DocumentId - links to this PurchaseOrder
                T.c.value('(PaymentMethodId/text())[1]', 'INT'),
                T.c.value('(Amount/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(CashBoxId/text())[1]', 'INT'),
                T.c.value('(BankId/text())[1]', 'INT'),
                T.c.value('(BankAccountId/text())[1]', 'INT'),
                T.c.value('(PaidAmount/text())[1]', 'DECIMAL(18,2)'),
                T.c.value('(PaidDate/text())[1]', 'DATETIME')
            FROM @PaymentMethods.nodes('/PaymentMethods/PaymentMethod') T(c)
        END

        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE()
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY()
        DECLARE @ErrorState INT = ERROR_STATE()

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)
    END CATCH
END
GO

PRINT '✓ Created/Updated PurchaseOrder_Update stored procedure'
GO


-- ============================================================
-- VERIFICATION
-- ============================================================

PRINT ''
PRINT '=============================================================='
PRINT 'VERIFICATION - Checking created objects:'
PRINT '=============================================================='

-- Check DocumentPaymentMethods table
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentPaymentMethods]') AND type in (N'U'))
    PRINT '✓ DocumentPaymentMethods table exists'
ELSE
    PRINT '✗ DocumentPaymentMethods table NOT found'

-- Check PaymentType column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrders]') AND name = 'PaymentType')
    PRINT '✓ PurchaseOrders.PaymentType column exists'
ELSE
    PRINT '✗ PurchaseOrders.PaymentType column NOT found'

-- Check PurchaseOrder_Insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrder_Insert]') AND type in (N'P', N'PC'))
    PRINT '✓ PurchaseOrder_Insert stored procedure exists'
ELSE
    PRINT '✗ PurchaseOrder_Insert stored procedure NOT found'

-- Check PurchaseOrder_Update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrder_Update]') AND type in (N'P', N'PC'))
    PRINT '✓ PurchaseOrder_Update stored procedure exists'
ELSE
    PRINT '✗ PurchaseOrder_Update stored procedure NOT found'

PRINT '=============================================================='
PRINT 'DONE - Database changes complete.'
PRINT ''
PRINT 'NEXT STEPS (not part of this script):'
PRINT '1. Update Entity Framework model (regenerate from database)'
PRINT '2. Update PurchaseOrderController.cs'
PRINT '3. Update PurchaseOrder/AddEdit.cshtml view'
PRINT '=============================================================='
GO
