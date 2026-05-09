IF COL_LENGTH('dbo.DynamicReportDefinitions','LifecycleStatus') IS NULL
BEGIN
    ALTER TABLE dbo.DynamicReportDefinitions
        ADD LifecycleStatus NVARCHAR(20) NULL;

    EXEC('UPDATE dbo.DynamicReportDefinitions
            SET LifecycleStatus =
                CASE WHEN IsActive = 1 THEN ''Active'' ELSE ''Disabled'' END
          WHERE LifecycleStatus IS NULL');

    ALTER TABLE dbo.DynamicReportDefinitions
        ALTER COLUMN LifecycleStatus NVARCHAR(20) NOT NULL;

    ALTER TABLE dbo.DynamicReportDefinitions
        ADD CONSTRAINT DF_DRD_Lifecycle DEFAULT 'Draft' FOR LifecycleStatus;
END
GO

IF COL_LENGTH('dbo.DynamicReportDefinitions','LastValidatedAt') IS NULL
    ALTER TABLE dbo.DynamicReportDefinitions ADD LastValidatedAt DATETIME NULL;
GO

IF COL_LENGTH('dbo.DynamicReportDefinitions','CertificationLevel') IS NULL
BEGIN
    ALTER TABLE dbo.DynamicReportDefinitions ADD CertificationLevel NVARCHAR(20) NULL;
    EXEC('UPDATE dbo.DynamicReportDefinitions SET CertificationLevel = ''Internal'' WHERE CertificationLevel IS NULL');
    ALTER TABLE dbo.DynamicReportDefinitions ALTER COLUMN CertificationLevel NVARCHAR(20) NOT NULL;
    ALTER TABLE dbo.DynamicReportDefinitions ADD CONSTRAINT DF_DRD_Cert DEFAULT 'Internal' FOR CertificationLevel;
END
GO

IF COL_LENGTH('dbo.DynamicReportDefinitions','LastValidationLog') IS NULL
    ALTER TABLE dbo.DynamicReportDefinitions ADD LastValidationLog NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.DynamicReportDefinitions','ActivatedBy') IS NULL
    ALTER TABLE dbo.DynamicReportDefinitions ADD ActivatedBy INT NULL;
GO

IF COL_LENGTH('dbo.DynamicReportDefinitions','ActivatedAt') IS NULL
    ALTER TABLE dbo.DynamicReportDefinitions ADD ActivatedAt DATETIME NULL;
GO

IF COL_LENGTH('dbo.DynamicReportDefinitions','ReviewedBy') IS NULL
    ALTER TABLE dbo.DynamicReportDefinitions ADD ReviewedBy INT NULL;
GO

IF COL_LENGTH('dbo.DynamicReportDefinitions','ReviewedAt') IS NULL
    ALTER TABLE dbo.DynamicReportDefinitions ADD ReviewedAt DATETIME NULL;
GO

IF EXISTS (SELECT 1
           FROM sys.indexes
           WHERE name = 'IX_DRD_Lifecycle'
             AND object_id = OBJECT_ID('dbo.DynamicReportDefinitions'))
   AND NOT EXISTS (SELECT 1
                   FROM sys.index_columns ic
                   INNER JOIN sys.columns c
                       ON c.object_id = ic.object_id
                      AND c.column_id = ic.column_id
                   WHERE ic.object_id = OBJECT_ID('dbo.DynamicReportDefinitions')
                     AND ic.index_id = (SELECT index_id FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.DynamicReportDefinitions') AND name = 'IX_DRD_Lifecycle')
                     AND c.name = 'CertificationLevel')
    DROP INDEX IX_DRD_Lifecycle ON dbo.DynamicReportDefinitions;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_DRD_Lifecycle'
                 AND object_id = OBJECT_ID('dbo.DynamicReportDefinitions'))
    CREATE INDEX IX_DRD_Lifecycle
        ON dbo.DynamicReportDefinitions(ProjectScope, LifecycleStatus, CertificationLevel);
GO
