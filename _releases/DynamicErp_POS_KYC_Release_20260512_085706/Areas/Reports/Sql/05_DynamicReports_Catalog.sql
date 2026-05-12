/*
Dynamic Report Catalog schema
SQL Server 2012 compatible.
Creates the catalog table non-destructively and keeps repeat execution idempotent.
*/

IF OBJECT_ID(N'dbo.DynamicReportCatalog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynamicReportCatalog
    (
        CatalogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DynamicReportCatalog PRIMARY KEY,
        ProjectScope NVARCHAR(20) NOT NULL,
        SourceType NVARCHAR(20) NOT NULL,
        SourceSchema NVARCHAR(64) NOT NULL,
        SourceName NVARCHAR(128) NOT NULL,
        DiscoveredAt DATETIME NOT NULL CONSTRAINT DF_DRC_Disc DEFAULT(GETDATE()),
        LastSeenAt DATETIME NOT NULL CONSTRAINT DF_DRC_LS DEFAULT(GETDATE()),
        ClassificationStatus NVARCHAR(20) NOT NULL,
        ClassificationScore INT NOT NULL,
        RiskFlags NVARCHAR(500) NULL,
        SuggestedReportName NVARCHAR(200) NULL,
        ApprovedBy INT NULL,
        ApprovedAt DATETIME NULL,
        RejectionReason NVARCHAR(500) NULL,
        ImportedReportId INT NULL,
        ImportedAt DATETIME NULL,
        Notes NVARCHAR(1000) NULL
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_DRC_Identity'
      AND object_id = OBJECT_ID(N'dbo.DynamicReportCatalog')
)
BEGIN
    CREATE UNIQUE INDEX UX_DRC_Identity
        ON dbo.DynamicReportCatalog(ProjectScope, SourceType, SourceSchema, SourceName);
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_DRC_Status'
      AND object_id = OBJECT_ID(N'dbo.DynamicReportCatalog')
)
BEGIN
    CREATE INDEX IX_DRC_Status
        ON dbo.DynamicReportCatalog(ProjectScope, ClassificationStatus);
END
GO
