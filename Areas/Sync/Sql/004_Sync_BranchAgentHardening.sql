/*
    Branch Agent hardening schema.
    SQL Server 2012 compatible.

    Safe scope:
    - Extends branch heartbeat monitoring metadata only.
    - Does not modify invoice/accounting/stock/payment tables.
    - Does not enable ApplyMode or batch apply.
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.Sync_BranchHeartbeat','U') IS NULL
BEGIN
    RAISERROR('Sync_BranchHeartbeat table is missing. Apply approved script 003 first.', 16, 1);
END
GO

IF COL_LENGTH('dbo.Sync_BranchHeartbeat', 'ConfigVersion') IS NULL
    ALTER TABLE dbo.Sync_BranchHeartbeat ADD ConfigVersion NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.Sync_BranchHeartbeat', 'PayloadSchemaVersion') IS NULL
    ALTER TABLE dbo.Sync_BranchHeartbeat ADD PayloadSchemaVersion NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.Sync_BranchHeartbeat', 'FailedOutboxCount') IS NULL
    ALTER TABLE dbo.Sync_BranchHeartbeat ADD FailedOutboxCount INT NOT NULL CONSTRAINT DF_Sync_BranchHeartbeat_FailedOutbox DEFAULT (0);
GO

IF COL_LENGTH('dbo.Sync_BranchHeartbeat', 'AuthFailureCount') IS NULL
    ALTER TABLE dbo.Sync_BranchHeartbeat ADD AuthFailureCount INT NOT NULL CONSTRAINT DF_Sync_BranchHeartbeat_AuthFailure DEFAULT (0);
GO

IF COL_LENGTH('dbo.Sync_BranchHeartbeat', 'LastAuthFailureAt') IS NULL
    ALTER TABLE dbo.Sync_BranchHeartbeat ADD LastAuthFailureAt DATETIME NULL;
GO

IF OBJECT_ID('dbo.Sync_BranchUpload','U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Sync_BranchUpload') AND name = N'IX_Sync_BranchUpload_BranchStatus')
    CREATE INDEX IX_Sync_BranchUpload_BranchStatus ON dbo.Sync_BranchUpload(BranchId, Status, CreatedAt DESC);
GO
