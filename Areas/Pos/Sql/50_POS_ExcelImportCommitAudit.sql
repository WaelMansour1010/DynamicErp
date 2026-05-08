IF OBJECT_ID(N'dbo.POS_ImportBatchRow', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_POS_ImportBatchRow_Batch'
          AND parent_object_id = OBJECT_ID(N'dbo.POS_ImportBatchRow')
    )
    AND OBJECT_ID(N'dbo.POS_ImportBatch', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.POS_ImportBatchRow
        ADD CONSTRAINT FK_POS_ImportBatchRow_Batch
        FOREIGN KEY (BatchId) REFERENCES dbo.POS_ImportBatch(BatchId);
    END
END
GO

IF OBJECT_ID(N'dbo.POS_ImportBatch', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_ImportBatch
    (
        BatchId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_ImportBatch PRIMARY KEY,
        SourceFileName NVARCHAR(255) NOT NULL,
        SourceFileHash NVARCHAR(128) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        CreatedByUserId INT NULL,
        BranchId INT NULL,
        ImportedInvoicesCount INT NOT NULL CONSTRAINT DF_POS_ImportBatch_ImportedInvoicesCount DEFAULT (0),
        FailedRowsCount INT NOT NULL CONSTRAINT DF_POS_ImportBatch_FailedRowsCount DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ImportBatch_CreatedAt DEFAULT (GETDATE()),
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
        SourceSheet NVARCHAR(255) NOT NULL,
        SourceRow INT NOT NULL,
        SourceInvoiceNo NVARCHAR(255) NULL,
        Token NVARCHAR(255) NULL,
        Status NVARCHAR(50) NOT NULL,
        TransactionId INT NULL,
        Message NVARCHAR(1000) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ImportBatchRow_CreatedAt DEFAULT (GETDATE())
    );

    ALTER TABLE dbo.POS_ImportBatchRow
    ADD CONSTRAINT FK_POS_ImportBatchRow_Batch
    FOREIGN KEY (BatchId) REFERENCES dbo.POS_ImportBatch(BatchId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_ImportBatch_SourceFileHash' AND object_id = OBJECT_ID(N'dbo.POS_ImportBatch'))
BEGIN
    CREATE INDEX IX_POS_ImportBatch_SourceFileHash ON dbo.POS_ImportBatch(SourceFileHash);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_ImportBatchRow_Source' AND object_id = OBJECT_ID(N'dbo.POS_ImportBatchRow'))
BEGIN
    CREATE INDEX IX_POS_ImportBatchRow_Source ON dbo.POS_ImportBatchRow(BatchId, SourceSheet, SourceRow, Status);
END
GO
