/*
    Kishny POS - Runtime infrastructure preflight
    SQL Server 2012 compatible.

    Run this script once with a database owner / DDL-capable login before using
    the POS site SQL login in production.

    Purpose:
    - Create POS helper tables that the application may need for logging,
      import audit, limited-edit audit, permissions, and POS DEV_Serial allocation.
    - Keep CREATE TABLE / CREATE INDEX work out of live cashier save requests.
*/

SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.Transactions', N'AccountTypeName1') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions ADD AccountTypeName1 NVARCHAR(255) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Transactions', N'CashCustomerName') IS NOT NULL
AND EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Transactions')
      AND name = N'CashCustomerName'
      AND max_length < 510
)
BEGIN
    ALTER TABLE dbo.Transactions ALTER COLUMN CashCustomerName NVARCHAR(255) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Transactions', N'CashCustomerPhone') IS NOT NULL
AND EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Transactions')
      AND name = N'CashCustomerPhone'
      AND max_length < 510
)
BEGIN
    ALTER TABLE dbo.Transactions ALTER COLUMN CashCustomerPhone NVARCHAR(255) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Transactions', N'Phone2') IS NOT NULL
AND EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Transactions')
      AND name = N'Phone2'
      AND max_length < 510
)
BEGIN
    ALTER TABLE dbo.Transactions ALTER COLUMN Phone2 NVARCHAR(255) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Transactions', N'ManualNo2') IS NOT NULL
AND EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Transactions')
      AND name = N'ManualNo2'
      AND max_length < 510
)
BEGIN
    ALTER TABLE dbo.Transactions ALTER COLUMN ManualNo2 NVARCHAR(255) NULL;
END;
GO

IF OBJECT_ID(N'dbo.POS_DEVSerialAllocator', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DEVSerialAllocator
    (
        SerialDate DATE NOT NULL CONSTRAINT PK_POS_DEVSerialAllocator PRIMARY KEY,
        LastSerialNo INT NOT NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_POS_DEVSerialAllocator_UpdatedAt DEFAULT(GETDATE())
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_SaveAttemptLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SaveAttemptLog
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SaveAttemptLog PRIMARY KEY,
        SaveAttemptId UNIQUEIDENTIFIER NOT NULL,
        EventName NVARCHAR(100) NOT NULL,
        UserID INT NULL,
        EmpID INT NULL,
        BranchId INT NULL,
        TransactionType NVARCHAR(50) NULL,
        RetryAttempt INT NULL,
        SqlErrorNumber INT NULL,
        DelayMs INT NULL,
        DurationMs INT NULL,
        Transaction_ID INT NULL,
        Status NVARCHAR(50) NULL,
        Message NVARCHAR(MAX) NULL,
        RequestSummary NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_SaveAttemptLog_CreatedAt DEFAULT (GETDATE())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
    CREATE INDEX IX_POS_SaveAttemptLog_CreatedAt ON dbo.POS_SaveAttemptLog(CreatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_SaveAttemptId' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
    CREATE INDEX IX_POS_SaveAttemptLog_SaveAttemptId ON dbo.POS_SaveAttemptLog(SaveAttemptId, CreatedAt ASC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_Branch_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
    CREATE INDEX IX_POS_SaveAttemptLog_Branch_CreatedAt ON dbo.POS_SaveAttemptLog(BranchId, CreatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_User_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
    CREATE INDEX IX_POS_SaveAttemptLog_User_CreatedAt ON dbo.POS_SaveAttemptLog(UserID, CreatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_Event_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
    CREATE INDEX IX_POS_SaveAttemptLog_Event_CreatedAt ON dbo.POS_SaveAttemptLog(EventName, CreatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
    CREATE INDEX IX_POS_SaveAttemptLog_Status_CreatedAt ON dbo.POS_SaveAttemptLog(Status, CreatedAt DESC);
GO

IF OBJECT_ID(N'dbo.POS_SystemErrorLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SystemErrorLog
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SystemErrorLog PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_SystemErrorLog_CreatedAt DEFAULT (GETDATE()),
        Severity NVARCHAR(20) NULL,
        Status NVARCHAR(40) NULL,
        UserId INT NULL,
        UserName NVARCHAR(100) NULL,
        BranchId INT NULL,
        ScreenName NVARCHAR(100) NULL,
        ActionName NVARCHAR(100) NULL,
        OperationType NVARCHAR(50) NULL,
        TransactionId INT NULL,
        ErrorMessage NVARCHAR(2000) NULL,
        StackTrace NVARCHAR(MAX) NULL,
        RequestSummary NVARCHAR(MAX) NULL,
        IpAddress NVARCHAR(64) NULL,
        UserAgent NVARCHAR(512) NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SystemErrorLog_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SystemErrorLog'))
    CREATE INDEX IX_POS_SystemErrorLog_CreatedAt ON dbo.POS_SystemErrorLog(CreatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SystemErrorLog_UserBranch' AND object_id = OBJECT_ID(N'dbo.POS_SystemErrorLog'))
    CREATE INDEX IX_POS_SystemErrorLog_UserBranch ON dbo.POS_SystemErrorLog(UserId, BranchId, CreatedAt DESC);
GO

IF OBJECT_ID(N'dbo.POS_UserPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_UserPermissions
    (
        UserID INT NOT NULL,
        PermissionKey NVARCHAR(100) NOT NULL,
        IsAllowed BIT NOT NULL CONSTRAINT DF_POS_UserPermissions_IsAllowed DEFAULT(0),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_POS_UserPermissions_UpdatedAt DEFAULT(GETDATE()),
        CONSTRAINT PK_POS_UserPermissions PRIMARY KEY(UserID, PermissionKey)
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_SalesInvoiceEditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SalesInvoiceEditLog
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SalesInvoiceEditLog PRIMARY KEY,
        Transaction_ID INT NOT NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        UserId INT NOT NULL,
        EditDateTime DATETIME NOT NULL CONSTRAINT DF_POS_SalesInvoiceEditLog_EditDateTime DEFAULT(GETDATE()),
        EditReason NVARCHAR(500) NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SalesInvoiceEditLog_Transaction_ID' AND object_id = OBJECT_ID(N'dbo.POS_SalesInvoiceEditLog'))
    CREATE INDEX IX_POS_SalesInvoiceEditLog_Transaction_ID ON dbo.POS_SalesInvoiceEditLog(Transaction_ID, EditDateTime);
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
END;
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
END;
GO

IF OBJECT_ID(N'dbo.POS_ImportValidationResult', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_ImportValidationResult
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_ImportValidationResult PRIMARY KEY,
        RowId BIGINT NOT NULL,
        FieldName NVARCHAR(100) NULL,
        Message NVARCHAR(1000) NOT NULL,
        Severity NVARCHAR(20) NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ImportValidationResult_CreatedAt DEFAULT (GETDATE())
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_ImportMapping', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_ImportMapping
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_ImportMapping PRIMARY KEY,
        SourceColumn NVARCHAR(255) NOT NULL,
        TargetField NVARCHAR(100) NOT NULL,
        IsRequired BIT NOT NULL CONSTRAINT DF_POS_ImportMapping_IsRequired DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ImportMapping_CreatedAt DEFAULT (GETDATE())
    );
END;
GO

SELECT
    POS_DEVSerialAllocator = OBJECT_ID(N'dbo.POS_DEVSerialAllocator', N'U'),
    POS_SaveAttemptLog = OBJECT_ID(N'dbo.POS_SaveAttemptLog', N'U'),
    POS_SystemErrorLog = OBJECT_ID(N'dbo.POS_SystemErrorLog', N'U'),
    POS_UserPermissions = OBJECT_ID(N'dbo.POS_UserPermissions', N'U'),
    POS_SalesInvoiceEditLog = OBJECT_ID(N'dbo.POS_SalesInvoiceEditLog', N'U'),
    POS_ImportBatch = OBJECT_ID(N'dbo.POS_ImportBatch', N'U'),
    POS_ImportBatchRow = OBJECT_ID(N'dbo.POS_ImportBatchRow', N'U'),
    POS_ImportValidationResult = OBJECT_ID(N'dbo.POS_ImportValidationResult', N'U'),
    POS_ImportMapping = OBJECT_ID(N'dbo.POS_ImportMapping', N'U'),
    AccountTypeName1Exists = COL_LENGTH(N'dbo.Transactions', N'AccountTypeName1'),
    CashCustomerNameMaxBytes = COL_LENGTH(N'dbo.Transactions', N'CashCustomerName'),
    CashCustomerPhoneMaxBytes = COL_LENGTH(N'dbo.Transactions', N'CashCustomerPhone'),
    Phone2MaxBytes = COL_LENGTH(N'dbo.Transactions', N'Phone2'),
    ManualNo2MaxBytes = COL_LENGTH(N'dbo.Transactions', N'ManualNo2');
GO
