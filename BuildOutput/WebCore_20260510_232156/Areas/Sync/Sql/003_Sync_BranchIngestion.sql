/*
    Sync Branch central ingestion schema.
    SQL Server 2012 compatible.

    Safe scope:
    - Creates/extends central intake queue/log/heartbeat tables only.
    - Does not modify invoice/accounting/stock/payment tables.
    - Does not enable ApplyMode.
    - Does not permit batch apply.
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.Sync_Outbox','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_Outbox
    (
        SyncId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_Outbox PRIMARY KEY,
        BranchId INT NOT NULL,
        EntityType NVARCHAR(50) NOT NULL,
        EntityKey NVARCHAR(100) NOT NULL,
        OperationType NVARCHAR(50) NOT NULL CONSTRAINT DF_Sync_Outbox_OperationType DEFAULT (N'Upload'),
        Direction NVARCHAR(20) NOT NULL CONSTRAINT DF_Sync_Outbox_Direction DEFAULT (N'BranchToCentral'),
        Status NVARCHAR(50) NOT NULL,
        TryCount INT NOT NULL CONSTRAINT DF_Sync_Outbox_TryCount DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_Outbox_CreatedAt DEFAULT (GETDATE()),
        StartedAt DATETIME NULL,
        CompletedAt DATETIME NULL,
        NextRetryAt DATETIME NULL,
        LastError NVARCHAR(MAX) NULL,
        PayloadJson NVARCHAR(MAX) NULL,
        PayloadSummary NVARCHAR(MAX) NULL,
        PayloadHash VARBINARY(32) NOT NULL,
        SourceRowVersion NVARCHAR(100) NULL,
        DestinationRowVersion NVARCHAR(100) NULL,
        BatchId BIGINT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Sync_Outbox') AND name = N'UX_Sync_Outbox_BranchEntityKey')
    CREATE UNIQUE INDEX UX_Sync_Outbox_BranchEntityKey ON dbo.Sync_Outbox(BranchId, EntityType, EntityKey);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Sync_Outbox') AND name = N'IX_Sync_Outbox_Status')
    CREATE INDEX IX_Sync_Outbox_Status ON dbo.Sync_Outbox(Status, CreatedAt) INCLUDE (BranchId, EntityType, EntityKey);
GO

IF OBJECT_ID('dbo.Sync_Log','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_Log
    (
        LogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_Log PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_Log_CreatedAt DEFAULT (GETDATE()),
        BranchId INT NULL,
        EntityType NVARCHAR(50) NULL,
        EntityKey NVARCHAR(100) NULL,
        Status NVARCHAR(50) NOT NULL,
        Message NVARCHAR(MAX) NULL
    );
END
GO

IF OBJECT_ID('dbo.Sync_Error','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_Error
    (
        ErrorId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_Error PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_Error_CreatedAt DEFAULT (GETDATE()),
        BranchId INT NULL,
        EntityType NVARCHAR(50) NULL,
        EntityKey NVARCHAR(100) NULL,
        ErrorMessage NVARCHAR(MAX) NOT NULL,
        LastSql NVARCHAR(MAX) NULL
    );
END
GO

IF OBJECT_ID('dbo.Sync_BranchHeartbeat','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_BranchHeartbeat
    (
        BranchId INT NOT NULL CONSTRAINT PK_Sync_BranchHeartbeat PRIMARY KEY,
        MachineName NVARCHAR(256) NULL,
        LastSeenAt DATETIME NOT NULL,
        AgentVersion NVARCHAR(50) NULL,
        PendingOutboxCount INT NOT NULL CONSTRAINT DF_Sync_BranchHeartbeat_Pending DEFAULT (0),
        RejectedPayloadCount INT NOT NULL CONSTRAINT DF_Sync_BranchHeartbeat_Rejected DEFAULT (0),
        LastTransactionId BIGINT NOT NULL CONSTRAINT DF_Sync_BranchHeartbeat_LastTransaction DEFAULT (0),
        LastPayloadSyncKey NVARCHAR(100) NULL,
        LastError NVARCHAR(MAX) NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_BranchHeartbeat_UpdatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.Sync_BranchUpload','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_BranchUpload
    (
        UploadId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_BranchUpload PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_BranchUpload_CreatedAt DEFAULT (GETDATE()),
        BranchId INT NOT NULL,
        SyncKey NVARCHAR(100) NOT NULL,
        PayloadHash VARBINARY(32) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        Message NVARCHAR(MAX) NULL,
        RemoteIp NVARCHAR(64) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Sync_BranchUpload') AND name = N'IX_Sync_BranchUpload_Recent')
    CREATE INDEX IX_Sync_BranchUpload_Recent ON dbo.Sync_BranchUpload(CreatedAt DESC) INCLUDE (BranchId, SyncKey, Status);
GO
