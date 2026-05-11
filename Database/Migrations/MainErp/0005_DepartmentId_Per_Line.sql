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
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JournalEntryDetail_DepartmentId' AND object_id = OBJECT_ID('dbo.JournalEntryDetail'))
BEGIN
    CREATE INDEX IX_JournalEntryDetail_DepartmentId ON dbo.JournalEntryDetail(DepartmentId);
END
GO

IF OBJECT_ID('dbo.IssueAnalysis', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.IssueAnalysis', 'DepartmentId') IS NULL
BEGIN
    ALTER TABLE dbo.IssueAnalysis ADD DepartmentId int NULL;
END
GO

IF OBJECT_ID('dbo.IssueAnalysis', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.CashIssueVoucher', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.IssueAnalysis', 'DepartmentId') IS NOT NULL
BEGIN
    EXEC('
        UPDATE ia
        SET DepartmentId = civ.DepartmentId
        FROM dbo.IssueAnalysis ia
        INNER JOIN dbo.CashIssueVoucher civ ON civ.Id = ia.CashIssueVoucherId
        WHERE ia.DepartmentId IS NULL;
    ');
END
GO

IF OBJECT_ID('dbo.IssueAnalysis', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.IssueAnalysis', 'DepartmentId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IssueAnalysis_DepartmentId' AND object_id = OBJECT_ID('dbo.IssueAnalysis'))
BEGIN
    CREATE INDEX IX_IssueAnalysis_DepartmentId ON dbo.IssueAnalysis(DepartmentId);
END
GO

IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Department', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NOT NULL
   AND OBJECT_ID('dbo.FK_JournalEntryDetail_Department', 'F') IS NULL
BEGIN
    ALTER TABLE dbo.JournalEntryDetail WITH NOCHECK
    ADD CONSTRAINT FK_JournalEntryDetail_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(Id);
END
GO

IF OBJECT_ID('dbo.IssueAnalysis', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Department', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.IssueAnalysis', 'DepartmentId') IS NOT NULL
   AND OBJECT_ID('dbo.FK_IssueAnalysis_Department', 'F') IS NULL
BEGIN
    ALTER TABLE dbo.IssueAnalysis WITH NOCHECK
    ADD CONSTRAINT FK_IssueAnalysis_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(Id);
END
GO

IF OBJECT_ID('dbo.IssueAnalysis', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.IssueAnalysis_Update', 'P') IS NOT NULL
    DROP PROCEDURE dbo.IssueAnalysis_Update;
GO

IF OBJECT_ID('dbo.IssueAnalysis', 'U') IS NOT NULL
    EXEC('CREATE PROCEDURE dbo.IssueAnalysis_Update
    @Id int,
    @CashIssueVoucherId int,
    @Value money,
    @Reason nvarchar(max),
    @AccountId int,
    @DepartmentId int,
    @Notes nvarchar(max),
    @IsDeleted bit,
    @Total money,
    @Taxes money,
    @TaxesPrecentage float,
    @NetTotal money,
    @VendorId int,
    @VendorArName nvarchar(max),
    @TaxNumber nvarchar(max),
    @VATNumber nvarchar(max),
    @InvoiceNo nvarchar(max),
    @IssueAnalysisDetail ntext
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @trancount int;
    SET @trancount = @@TRANCOUNT;

    BEGIN TRY
        IF @trancount = 0
            BEGIN TRANSACTION;
        ELSE
            SAVE TRANSACTION IssueAnalysis_Update;

        DECLARE @NewIssueAnalysisId int;
        DECLARE @IssueAnalysisDetailOut int;

        EXEC sp_xml_preparedocument @IssueAnalysisDetailOut OUTPUT, @IssueAnalysisDetail;

        IF EXISTS (SELECT 1 FROM dbo.IssueAnalysis WHERE Id = @Id)
        BEGIN
            UPDATE dbo.IssueAnalysis
            SET CashIssueVoucherId = @CashIssueVoucherId,
                Value = @Value,
                Reason = @Reason,
                AccountId = @AccountId,
                DepartmentId = @DepartmentId,
                IsDeleted = @IsDeleted,
                Notes = @Notes,
                Total = @Total,
                Taxes = @Taxes,
                TaxesPrecentage = @TaxesPrecentage,
                NetTotal = @NetTotal,
                VendorId = @VendorId,
                VendorArName = @VendorArName,
                TaxNumber = @TaxNumber,
                VATNumber = @VATNumber,
                InvoiceNo = @InvoiceNo
            WHERE Id = @Id;

            SET @NewIssueAnalysisId = @Id;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.IssueAnalysis
                (CashIssueVoucherId, Value, Reason, AccountId, DepartmentId, IsDeleted, Notes,
                 Total, Taxes, TaxesPrecentage, NetTotal, VendorId, VendorArName,
                 TaxNumber, VATNumber, InvoiceNo)
            VALUES
                (@CashIssueVoucherId, @Value, @Reason, @AccountId, @DepartmentId, @IsDeleted, @Notes,
                 @Total, @Taxes, @TaxesPrecentage, @NetTotal, @VendorId, @VendorArName,
                 @TaxNumber, @VATNumber, @InvoiceNo);

            SET @NewIssueAnalysisId = SCOPE_IDENTITY();
        END

        DECLARE @IssueAnalysisDetailTemp TABLE
        (
            PropertyDetailId int,
            Price money,
            IssueAnalysisId int
        );

        INSERT INTO @IssueAnalysisDetailTemp (PropertyDetailId, Price, IssueAnalysisId)
        SELECT PropertyDetailId, Price, @NewIssueAnalysisId
        FROM OPENXML(@IssueAnalysisDetailOut, ''/DocumentElement/IssueAnalysisDetails'', 2)
        WITH
        (
            PropertyDetailId int,
            Price money,
            IssueAnalysisId int
        );

        DELETE FROM dbo.IssueAnalysisDetail
        WHERE IssueAnalysisId = @NewIssueAnalysisId;

        INSERT INTO dbo.IssueAnalysisDetail (PropertyDetailId, Price, IssueAnalysisId)
        SELECT PropertyDetailId, Price, IssueAnalysisId
        FROM @IssueAnalysisDetailTemp;

        EXEC sp_xml_removedocument @IssueAnalysisDetailOut;

        IF @trancount = 0
            COMMIT;
    END TRY
    BEGIN CATCH
        DECLARE @error int, @message varchar(4000), @xstate int;
        SELECT @error = ERROR_NUMBER(), @message = ERROR_MESSAGE() + '' at '' + CAST(ERROR_LINE() AS varchar(50)), @xstate = XACT_STATE();

        IF @xstate = -1
            ROLLBACK;
        IF @xstate = 1 AND @trancount = 0
            ROLLBACK;
        IF @xstate = 1 AND @trancount > 0
            ROLLBACK TRANSACTION IssueAnalysis_Update;

        RAISERROR (''IssueAnalysis_Update: %d: %s'', 16, 1, @error, @message);
    END CATCH
END');
GO
