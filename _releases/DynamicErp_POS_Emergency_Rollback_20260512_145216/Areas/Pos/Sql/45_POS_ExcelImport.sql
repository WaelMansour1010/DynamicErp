/*
    POS Excel Import audit/mapping schema.
    SQL Server 2012 compatible.
    POS-only location: DynamicErp\Areas\Pos\Sql

    This script is aligned with Areas\Pos\Data\PosSqlRepository.cs.
    It is intentionally idempotent and safe to rerun.
*/

IF OBJECT_ID(N'dbo.POS_ImportBatch', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_ImportBatch
    (
        BatchId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_ImportBatch PRIMARY KEY,
        SourceFileName NVARCHAR(255) NULL,
        SourceFileHash NVARCHAR(128) NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_POS_ImportBatch_Status DEFAULT(N'Previewed'),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ImportBatch_CreatedAt DEFAULT(GETDATE()),
        CreatedByUserId INT NULL,
        ImportedInvoicesCount INT NOT NULL CONSTRAINT DF_POS_ImportBatch_ImportedInvoices DEFAULT(0),
        FailedRowsCount INT NOT NULL CONSTRAINT DF_POS_ImportBatch_FailedRows DEFAULT(0),
        CompletedAt DATETIME NULL
    );
END
GO

IF OBJECT_ID(N'dbo.POS_ImportBatchRow', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_ImportBatchRow
    (
        RowId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_ImportBatchRow PRIMARY KEY,
        BatchId BIGINT NOT NULL,
        SourceSheet NVARCHAR(128) NULL,
        SourceRow INT NULL,
        SourceInvoiceNo NVARCHAR(100) NULL,
        Token NVARCHAR(255) NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_POS_ImportBatchRow_Status DEFAULT(N'Pending'),
        TransactionId INT NULL,
        Message NVARCHAR(1000) NULL,
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
        RowId BIGINT NULL,
        Severity NVARCHAR(20) NOT NULL,
        RuleCode NVARCHAR(100) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ImportValidationResult_CreatedAt DEFAULT(GETDATE()),
        CONSTRAINT FK_POS_ImportValidationResult_Batch FOREIGN KEY (BatchId) REFERENCES dbo.POS_ImportBatch(BatchId),
        CONSTRAINT FK_POS_ImportValidationResult_Row FOREIGN KEY (RowId) REFERENCES dbo.POS_ImportBatchRow(RowId)
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_ImportBatch_SourceFileHash' AND object_id = OBJECT_ID(N'dbo.POS_ImportBatch'))
BEGIN
    CREATE INDEX IX_POS_ImportBatch_SourceFileHash
    ON dbo.POS_ImportBatch(SourceFileHash);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_ImportBatchRow_Source' AND object_id = OBJECT_ID(N'dbo.POS_ImportBatchRow'))
BEGIN
    CREATE INDEX IX_POS_ImportBatchRow_Source
    ON dbo.POS_ImportBatchRow(BatchId, SourceSheet, SourceRow);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_ImportBatchRow_Token' AND object_id = OBJECT_ID(N'dbo.POS_ImportBatchRow'))
BEGIN
    CREATE INDEX IX_POS_ImportBatchRow_Token
    ON dbo.POS_ImportBatchRow(Token)
    WHERE Token IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_POS_ImportMapping_TypeSource' AND object_id = OBJECT_ID(N'dbo.POS_ImportMapping'))
BEGIN
    CREATE UNIQUE INDEX UX_POS_ImportMapping_TypeSource
    ON dbo.POS_ImportMapping(MappingType, SourceValue);
END
GO
