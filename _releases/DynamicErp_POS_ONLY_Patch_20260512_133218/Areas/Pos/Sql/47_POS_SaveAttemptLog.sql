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
BEGIN
    CREATE INDEX IX_POS_SaveAttemptLog_CreatedAt ON dbo.POS_SaveAttemptLog(CreatedAt DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_SaveAttemptId' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
BEGIN
    CREATE INDEX IX_POS_SaveAttemptLog_SaveAttemptId ON dbo.POS_SaveAttemptLog(SaveAttemptId, CreatedAt ASC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_Branch_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
BEGIN
    CREATE INDEX IX_POS_SaveAttemptLog_Branch_CreatedAt ON dbo.POS_SaveAttemptLog(BranchId, CreatedAt DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_User_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
BEGIN
    CREATE INDEX IX_POS_SaveAttemptLog_User_CreatedAt ON dbo.POS_SaveAttemptLog(UserID, CreatedAt DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_Event_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
BEGIN
    CREATE INDEX IX_POS_SaveAttemptLog_Event_CreatedAt ON dbo.POS_SaveAttemptLog(EventName, CreatedAt DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SaveAttemptLog_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SaveAttemptLog'))
BEGIN
    CREATE INDEX IX_POS_SaveAttemptLog_Status_CreatedAt ON dbo.POS_SaveAttemptLog(Status, CreatedAt DESC);
END;
GO
