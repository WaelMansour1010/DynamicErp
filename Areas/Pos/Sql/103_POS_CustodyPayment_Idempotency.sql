/*
    POS custody replenishment / treasury funding save idempotency guard.
    SQL Server 2012 compatible.
    Prevents duplicate Notes/DOUBLE_ENTREY_VOUCHERS rows when the same browser save request is retried.
*/

IF OBJECT_ID(N'dbo.POS_PaymentSaveIdempotency', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_PaymentSaveIdempotency
    (
        ClientRequestId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_POS_PaymentSaveIdempotency PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_PaymentSaveIdempotency_CreatedAt DEFAULT (GETDATE()),
        LastSeenAt DATETIME NULL,
        CompletedAt DATETIME NULL,
        UserID INT NULL,
        BranchId INT NULL,
        CashingType INT NULL,
        PaymentMethod INT NULL,
        BoxID INT NULL,
        BankID INT NULL,
        Amount DECIMAL(19,4) NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_POS_PaymentSaveIdempotency_Status DEFAULT (N'InProgress'),
        NoteID INT NULL,
        NoteSerial NVARCHAR(100) NULL,
        NoteSerial1 NVARCHAR(100) NULL,
        DurationMs INT NULL,
        ErrorMessage NVARCHAR(1000) NULL
    );
END
GO

IF COL_LENGTH(N'dbo.POS_PaymentSaveIdempotency', N'LastSeenAt') IS NULL ALTER TABLE dbo.POS_PaymentSaveIdempotency ADD LastSeenAt DATETIME NULL;
IF COL_LENGTH(N'dbo.POS_PaymentSaveIdempotency', N'CompletedAt') IS NULL ALTER TABLE dbo.POS_PaymentSaveIdempotency ADD CompletedAt DATETIME NULL;
IF COL_LENGTH(N'dbo.POS_PaymentSaveIdempotency', N'DurationMs') IS NULL ALTER TABLE dbo.POS_PaymentSaveIdempotency ADD DurationMs INT NULL;
IF COL_LENGTH(N'dbo.POS_PaymentSaveIdempotency', N'ErrorMessage') IS NULL ALTER TABLE dbo.POS_PaymentSaveIdempotency ADD ErrorMessage NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_PaymentSaveIdempotency_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_PaymentSaveIdempotency'))
BEGIN
    CREATE INDEX IX_POS_PaymentSaveIdempotency_Status_CreatedAt
    ON dbo.POS_PaymentSaveIdempotency(Status, CreatedAt DESC)
    INCLUDE (UserID, BranchId, CashingType, PaymentMethod, NoteID, NoteSerial1, DurationMs);
END
GO
