/*
    Sync Admin queued operation architecture.
    SQL Server 2012 compatible.

    Safe scope:
    - Creates Sync_AdminOperation, Sync_AdminAudit, Sync_AdminApproval,
      Sync_AdminNotification, Sync_AdminRolePermission.
    - Does not modify invoice/accounting/stock/payment tables.
    - Does not enable ApplyMode.
    - Does not permit batch apply.
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.Sync_AdminOperation','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_AdminOperation
    (
        AdminOperationId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_AdminOperation PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_AdminOperation_CreatedAt DEFAULT (GETDATE()),
        ApprovedAt DATETIME NULL,
        StartedAt DATETIME NULL,
        CompletedAt DATETIME NULL,
        RequestedBy NVARCHAR(256) NOT NULL,
        ApprovedBy NVARCHAR(256) NULL,
        MachineName NVARCHAR(256) NULL,
        IpAddress NVARCHAR(64) NULL,
        OperationType NVARCHAR(100) NOT NULL,
        Permission NVARCHAR(100) NULL,
        ProfileName NVARCHAR(100) NULL,
        SyncKey NVARCHAR(100) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        Result NVARCHAR(50) NULL,
        Reason NVARCHAR(MAX) NOT NULL,
        ApplySingleSyncKeyOnly BIT NOT NULL CONSTRAINT DF_Sync_AdminOperation_Single DEFAULT (1),
        MaxInvoicesPerRun INT NOT NULL CONSTRAINT DF_Sync_AdminOperation_Max DEFAULT (1),
        WorkerName NVARCHAR(256) NULL,
        LastError NVARCHAR(MAX) NULL,
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

IF OBJECT_ID('dbo.Sync_AdminNotification','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_AdminNotification
    (
        NotificationId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_AdminNotification PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_AdminNotification_CreatedAt DEFAULT (GETDATE()),
        ReadAt DATETIME NULL,
        NotificationType NVARCHAR(50) NOT NULL,
        Severity NVARCHAR(20) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(MAX) NULL,
        SyncKey NVARCHAR(100) NULL,
        BranchId NVARCHAR(20) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Sync_AdminNotification_Status DEFAULT (N'Unread')
    );
END
GO

IF OBJECT_ID('dbo.Sync_AdminRolePermission','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_AdminRolePermission
    (
        RolePermissionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_AdminRolePermission PRIMARY KEY,
        RoleName NVARCHAR(100) NOT NULL,
        Permission NVARCHAR(100) NOT NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_Sync_AdminRolePermission_Enabled DEFAULT (1),
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Sync_AdminRolePermission_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Sync_AdminOperation') AND name = N'IX_Sync_AdminOperation_Worker')
    CREATE INDEX IX_Sync_AdminOperation_Worker ON dbo.Sync_AdminOperation(Status, CreatedAt) INCLUDE (SyncKey, OperationType, ProfileName);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Sync_AdminNotification') AND name = N'IX_Sync_AdminNotification_Status')
    CREATE INDEX IX_Sync_AdminNotification_Status ON dbo.Sync_AdminNotification(Status, CreatedAt DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Sync_AdminRolePermission') AND name = N'UX_Sync_AdminRolePermission')
    CREATE UNIQUE INDEX UX_Sync_AdminRolePermission ON dbo.Sync_AdminRolePermission(RoleName, Permission);
GO
