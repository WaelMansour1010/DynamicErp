/*
    POS system error log.
    SQL Server 2012 compatible.
    Safe to run repeatedly.
*/

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
BEGIN
    CREATE INDEX IX_POS_SystemErrorLog_CreatedAt ON dbo.POS_SystemErrorLog(CreatedAt DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SystemErrorLog_UserBranch' AND object_id = OBJECT_ID(N'dbo.POS_SystemErrorLog'))
BEGIN
    CREATE INDEX IX_POS_SystemErrorLog_UserBranch ON dbo.POS_SystemErrorLog(UserId, BranchId, CreatedAt DESC);
END;
GO
