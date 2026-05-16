/*
    POS invoice save idempotency guard.
    SQL Server 2012 compatible.
    Prevents duplicate invoices when the same browser save request is submitted again.
*/

IF OBJECT_ID(N'dbo.POS_SaveIdempotency', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SaveIdempotency
    (
        ClientRequestId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_POS_SaveIdempotency PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_SaveIdempotency_CreatedAt DEFAULT (GETDATE()),
        LastSeenAt DATETIME NULL,
        CompletedAt DATETIME NULL,
        UserID INT NULL,
        BranchId INT NULL,
        StoreID INT NULL,
        BoxID INT NULL,
        TransactionType NVARCHAR(50) NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_POS_SaveIdempotency_Status DEFAULT (N'InProgress'),
        Transaction_ID INT NULL,
        NoteSerial1 NVARCHAR(100) NULL,
        DurationMs INT NULL,
        ErrorMessage NVARCHAR(1000) NULL
    );
END
GO

IF COL_LENGTH(N'dbo.POS_SaveIdempotency', N'LastSeenAt') IS NULL ALTER TABLE dbo.POS_SaveIdempotency ADD LastSeenAt DATETIME NULL;
IF COL_LENGTH(N'dbo.POS_SaveIdempotency', N'CompletedAt') IS NULL ALTER TABLE dbo.POS_SaveIdempotency ADD CompletedAt DATETIME NULL;
IF COL_LENGTH(N'dbo.POS_SaveIdempotency', N'DurationMs') IS NULL ALTER TABLE dbo.POS_SaveIdempotency ADD DurationMs INT NULL;
IF COL_LENGTH(N'dbo.POS_SaveIdempotency', N'ErrorMessage') IS NULL ALTER TABLE dbo.POS_SaveIdempotency ADD ErrorMessage NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveIdempotency_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SaveIdempotency'))
BEGIN
    CREATE INDEX IX_POS_SaveIdempotency_Status_CreatedAt
    ON dbo.POS_SaveIdempotency(Status, CreatedAt DESC)
    INCLUDE (UserID, BranchId, StoreID, BoxID, TransactionType, Transaction_ID, DurationMs);
END
GO
