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

IF COL_LENGTH('dbo.DynamicReportDefinitions','ActivatedBy') IS NULL
    ALTER TABLE dbo.DynamicReportDefinitions ADD ActivatedBy INT NULL;
GO

IF COL_LENGTH('dbo.DynamicReportDefinitions','ActivatedAt') IS NULL
    ALTER TABLE dbo.DynamicReportDefinitions ADD ActivatedAt DATETIME NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_DRD_Lifecycle'
                 AND object_id = OBJECT_ID('dbo.DynamicReportDefinitions'))
    CREATE INDEX IX_DRD_Lifecycle
        ON dbo.DynamicReportDefinitions(ProjectScope, LifecycleStatus);
GO
