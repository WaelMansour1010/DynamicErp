/*
    Sync Enterprise Operations Platform - audit tables draft
    SQL Server 2012 compatible.

    Safe scope:
    - Draft only. Do not run on production without approval.
    - Creates only Sync_AdminOperation, Sync_AdminAudit, Sync_AdminApproval.
    - No invoice, accounting, payment, stock, or legacy transfer tables are modified.
    - Audit tables are designed to be append-only.
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.Sync_AdminOperation','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_AdminOperation
    (
        AdminOperationId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_AdminOperation PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_AdminOperation_CreatedAt DEFAULT (GETDATE()),
        UserName NVARCHAR(256) NOT NULL,
        MachineName NVARCHAR(256) NULL,
        IpAddress NVARCHAR(64) NULL,
        Operation NVARCHAR(100) NOT NULL,
        Permission NVARCHAR(100) NULL,
        ProfileName NVARCHAR(100) NULL,
        SyncKey NVARCHAR(100) NULL,
        Result NVARCHAR(50) NOT NULL,
        Reason NVARCHAR(MAX) NULL,
        RequestJson NVARCHAR(MAX) NULL
    );
END
GO

IF OBJECT_ID('dbo.Sync_AdminAudit','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_AdminAudit
    (
        AdminAuditId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_AdminAudit PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_AdminAudit_CreatedAt DEFAULT (GETDATE()),
        UserName NVARCHAR(256) NOT NULL,
        MachineName NVARCHAR(256) NULL,
        IpAddress NVARCHAR(64) NULL,
        Operation NVARCHAR(100) NOT NULL,
        Permission NVARCHAR(100) NULL,
        ProfileName NVARCHAR(100) NULL,
        SyncKey NVARCHAR(100) NULL,
        Result NVARCHAR(50) NOT NULL,
        Reason NVARCHAR(MAX) NULL,
        Details NVARCHAR(MAX) NULL
    );
END
GO

IF OBJECT_ID('dbo.Sync_AdminApproval','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_AdminApproval
    (
        AdminApprovalId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_AdminApproval PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_AdminApproval_CreatedAt DEFAULT (GETDATE()),
        RequestedBy NVARCHAR(256) NOT NULL,
        ApprovedBy NVARCHAR(256) NULL,
        Operation NVARCHAR(100) NOT NULL,
        ProfileName NVARCHAR(100) NULL,
        SyncKey NVARCHAR(100) NULL,
        Status NVARCHAR(50) NOT NULL,
        Reason NVARCHAR(MAX) NULL,
        ApprovedAt DATETIME NULL
    );
END
GO

IF OBJECT_ID('dbo.trg_Sync_AdminOperation_AppendOnly','TR') IS NULL
EXEC('
CREATE TRIGGER dbo.trg_Sync_AdminOperation_AppendOnly
ON dbo.Sync_AdminOperation
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    RAISERROR(''Sync_AdminOperation is append-only.'', 16, 1);
    ROLLBACK TRANSACTION;
END');
GO

IF OBJECT_ID('dbo.trg_Sync_AdminAudit_AppendOnly','TR') IS NULL
EXEC('
CREATE TRIGGER dbo.trg_Sync_AdminAudit_AppendOnly
ON dbo.Sync_AdminAudit
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    RAISERROR(''Sync_AdminAudit is append-only.'', 16, 1);
    ROLLBACK TRANSACTION;
END');
GO

IF OBJECT_ID('dbo.trg_Sync_AdminApproval_AppendOnly','TR') IS NULL
EXEC('
CREATE TRIGGER dbo.trg_Sync_AdminApproval_AppendOnly
ON dbo.Sync_AdminApproval
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    RAISERROR(''Sync_AdminApproval is append-only.'', 16, 1);
    ROLLBACK TRANSACTION;
END');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Sync_AdminAudit') AND name = N'IX_Sync_AdminAudit_CreatedAt')
    CREATE INDEX IX_Sync_AdminAudit_CreatedAt ON dbo.Sync_AdminAudit(CreatedAt DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Sync_AdminAudit') AND name = N'IX_Sync_AdminAudit_SyncKey')
    CREATE INDEX IX_Sync_AdminAudit_SyncKey ON dbo.Sync_AdminAudit(SyncKey, CreatedAt DESC);
GO
