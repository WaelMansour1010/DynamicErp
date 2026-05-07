/*
    POS Excel Import audit/mapping schema.
    SQL Server 2012 compatible.
    POS-only location: DynamicErp\Areas\Pos\Sql
*/

IF OBJECT_ID(N'dbo.POS_ImportBatch', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_ImportBatch
    (
        BatchId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_ImportBatch PRIMARY KEY,
        SourceFileName NVARCHAR(260) NOT NULL,
        SourceFileHash CHAR(64) NOT NULL,
        WorkbookType NVARCHAR(100) NULL,
        TokenMatchingStrategy NVARCHAR(50) NOT NULL CONSTRAINT DF_POS_ImportBatch_TokenStrategy DEFAULT(N'Sequential'),
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_POS_ImportBatch_Status DEFAULT(N'Previewed'),
        BranchId INT NULL,
        StoreId INT NULL,
        PaymentTypeId INT NULL,
        ImportUserId INT NULL,
        ReadyRowsCount INT NOT NULL CONSTRAINT DF_POS_ImportBatch_ReadyRows DEFAULT(0),
        WarningRowsCount INT NOT NULL CONSTRAINT DF_POS_ImportBatch_WarningRows DEFAULT(0),
        RejectedRowsCount INT NOT NULL CONSTRAINT DF_POS_ImportBatch_RejectedRows DEFAULT(0),
        UnmatchedTokensCount INT NOT NULL CONSTRAINT DF_POS_ImportBatch_UnmatchedTokens DEFAULT(0),
        ImportedInvoicesCount INT NOT NULL CONSTRAINT DF_POS_ImportBatch_ImportedInvoices DEFAULT(0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ImportBatch_CreatedAt DEFAULT(GETDATE()),
        CreatedByUserId INT NULL,
        CommittedAt DATETIME NULL,
        CommittedByUserId INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.POS_ImportBatchRow', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_ImportBatchRow
    (
        BatchRowId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_ImportBatchRow PRIMARY KEY,
        BatchId BIGINT NOT NULL,
        SourceSheetName NVARCHAR(128) NOT NULL,
        SourceRowNumber INT NOT NULL,
        IPN NVARCHAR(510) NULL,
        CustomerName NVARCHAR(255) NULL,
        CustomerPhone NVARCHAR(100) NULL,
        ServiceType NVARCHAR(100) NULL,
        TransactionDate SMALLDATETIME NULL,
        Amount MONEY NULL,
        Fee MONEY NULL,
        GrossTotal MONEY NULL,
        MatchedToken NVARCHAR(510) NULL,
        MatchedTokenRowNumber INT NULL,
        RowStatus NVARCHAR(30) NOT NULL,
        CreatedTransactionId INT NULL,
        CreatedInvoiceNumber NVARCHAR(100) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ImportBatchRow_CreatedAt DEFAULT(GETDATE()),
        CONSTRAINT FK_POS_ImportBatchRow_Batch FOREIGN KEY (BatchId) REFERENCES dbo.POS_ImportBatch(BatchId)
    );
END
GO

IF OBJECT_ID(N'dbo.POS_ImportValidationResult', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_ImportValidationResult
    (
        ValidationId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_ImportValidationResult PRIMARY KEY,
        BatchId BIGINT NOT NULL,
        BatchRowId BIGINT NULL,
        SourceSheetName NVARCHAR(128) NULL,
        SourceRowNumber INT NULL,
        Severity NVARCHAR(20) NOT NULL,
        RuleCode NVARCHAR(100) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ImportValidationResult_CreatedAt DEFAULT(GETDATE()),
        CONSTRAINT FK_POS_ImportValidationResult_Batch FOREIGN KEY (BatchId) REFERENCES dbo.POS_ImportBatch(BatchId),
        CONSTRAINT FK_POS_ImportValidationResult_Row FOREIGN KEY (BatchRowId) REFERENCES dbo.POS_ImportBatchRow(BatchRowId)
    );
END
GO

IF OBJECT_ID(N'dbo.POS_ImportMapping', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_ImportMapping
    (
        MappingId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_ImportMapping PRIMARY KEY,
        MappingType NVARCHAR(50) NOT NULL,
        SourceValue NVARCHAR(255) NOT NULL,
        TargetId INT NULL,
        TargetText NVARCHAR(255) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_POS_ImportMapping_IsActive DEFAULT(1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ImportMapping_CreatedAt DEFAULT(GETDATE()),
        CreatedByUserId INT NULL,
        UpdatedAt DATETIME NULL,
        UpdatedByUserId INT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_ImportBatch_FileHash' AND object_id = OBJECT_ID(N'dbo.POS_ImportBatch'))
BEGIN
    /*
        Duplicate protection support: validation can warn/block committed reimports
        while still allowing repeated preview attempts.
    */
    CREATE INDEX IX_POS_ImportBatch_FileHash
    ON dbo.POS_ImportBatch(SourceFileHash);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_POS_ImportBatchRow_SourceRow' AND object_id = OBJECT_ID(N'dbo.POS_ImportBatchRow'))
BEGIN
    /*
        Traceability: one operational Excel row can appear once per batch.
    */
    CREATE UNIQUE INDEX UX_POS_ImportBatchRow_SourceRow
    ON dbo.POS_ImportBatchRow(BatchId, SourceSheetName, SourceRowNumber);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_ImportBatchRow_IPN' AND object_id = OBJECT_ID(N'dbo.POS_ImportBatchRow'))
BEGIN
    /*
        Duplicate IPN checks during validation/commit.
    */
    CREATE INDEX IX_POS_ImportBatchRow_IPN
    ON dbo.POS_ImportBatchRow(IPN)
    WHERE IPN IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_ImportBatchRow_Token' AND object_id = OBJECT_ID(N'dbo.POS_ImportBatchRow'))
BEGIN
    /*
        Duplicate token checks and token traceability.
    */
    CREATE INDEX IX_POS_ImportBatchRow_Token
    ON dbo.POS_ImportBatchRow(MatchedToken)
    WHERE MatchedToken IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_POS_ImportMapping_TypeSource' AND object_id = OBJECT_ID(N'dbo.POS_ImportMapping'))
BEGIN
    /*
        Mapping lookup is by mapping type and normalized source text.
    */
    CREATE UNIQUE INDEX UX_POS_ImportMapping_TypeSource
    ON dbo.POS_ImportMapping(MappingType, SourceValue);
END
GO
