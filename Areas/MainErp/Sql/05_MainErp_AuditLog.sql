IF OBJECT_ID('dbo.MainErp_AuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MainErp_AuditLog
    (
        AuditId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OperationName nvarchar(100) NOT NULL,
        EntityName nvarchar(100) NOT NULL,
        EntityKey nvarchar(100) NULL,
        UserId int NULL,
        CorrelationId uniqueidentifier NOT NULL,
        Message nvarchar(max) NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_MainErp_AuditLog_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.MainErp_AuditLog', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.MainErp_AuditLog', 'BeforeSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.MainErp_AuditLog ADD BeforeSnapshot nvarchar(max) NULL;
END
GO

IF OBJECT_ID('dbo.MainErp_AuditLog', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.MainErp_AuditLog', 'AfterSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.MainErp_AuditLog ADD AfterSnapshot nvarchar(max) NULL;
END
GO
