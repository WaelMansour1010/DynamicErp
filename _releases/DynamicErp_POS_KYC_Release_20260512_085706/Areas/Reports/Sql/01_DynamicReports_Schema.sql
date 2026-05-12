/*
Dynamic Report Designer schema
SQL Server 2012 compatible.
Tables are created non-destructively to preserve production data.
*/

IF OBJECT_ID(N'dbo.DynamicReportDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynamicReportDefinitions
    (
        ReportId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DynamicReportDefinitions PRIMARY KEY,
        ReportCode NVARCHAR(100) NOT NULL,
        ReportNameAr NVARCHAR(250) NOT NULL,
        ReportNameEn NVARCHAR(250) NULL,
        ProjectScope NVARCHAR(20) NOT NULL CONSTRAINT DF_DynamicReportDefinitions_ProjectScope DEFAULT(N'Shared'),
        SourceType NVARCHAR(30) NOT NULL,
        SourceName NVARCHAR(256) NOT NULL,
        RequireDateRange BIT NOT NULL CONSTRAINT DF_DynamicReportDefinitions_RequireDateRange DEFAULT(0),
        MaxRows INT NOT NULL CONSTRAINT DF_DynamicReportDefinitions_MaxRows DEFAULT(1000),
        CommandTimeoutSeconds INT NOT NULL CONSTRAINT DF_DynamicReportDefinitions_CommandTimeout DEFAULT(30),
        IsActive BIT NOT NULL CONSTRAINT DF_DynamicReportDefinitions_IsActive DEFAULT(1),
        CreatedBy INT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_DynamicReportDefinitions_CreatedAt DEFAULT(GETDATE()),
        UpdatedBy INT NULL,
        UpdatedAt DATETIME NULL
    );

    CREATE UNIQUE INDEX UX_DynamicReportDefinitions_ReportCode ON dbo.DynamicReportDefinitions(ReportCode);
    CREATE INDEX IX_DynamicReportDefinitions_ProjectScope ON dbo.DynamicReportDefinitions(ProjectScope, IsActive);
END
GO

IF OBJECT_ID(N'dbo.DynamicReportParameters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynamicReportParameters
    (
        ParameterId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DynamicReportParameters PRIMARY KEY,
        ReportId INT NOT NULL,
        ParameterName NVARCHAR(128) NOT NULL,
        CaptionAr NVARCHAR(250) NULL,
        CaptionEn NVARCHAR(250) NULL,
        DataType NVARCHAR(30) NOT NULL,
        IsRequired BIT NOT NULL CONSTRAINT DF_DynamicReportParameters_IsRequired DEFAULT(0),
        DefaultValue NVARCHAR(1000) NULL,
        LookupKey NVARCHAR(100) NULL,
        LookupSql NVARCHAR(MAX) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_DynamicReportParameters_SortOrder DEFAULT(0),
        CONSTRAINT FK_DynamicReportParameters_Report FOREIGN KEY (ReportId)
            REFERENCES dbo.DynamicReportDefinitions(ReportId)
    );

    CREATE UNIQUE INDEX UX_DynamicReportParameters_Report_Name ON dbo.DynamicReportParameters(ReportId, ParameterName);
END
GO

IF OBJECT_ID(N'dbo.DynamicReportColumns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynamicReportColumns
    (
        ColumnId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DynamicReportColumns PRIMARY KEY,
        ReportId INT NOT NULL,
        FieldName NVARCHAR(128) NOT NULL,
        CaptionAr NVARCHAR(250) NULL,
        CaptionEn NVARCHAR(250) NULL,
        DataType NVARCHAR(50) NULL,
        IsVisibleDefault BIT NOT NULL CONSTRAINT DF_DynamicReportColumns_IsVisibleDefault DEFAULT(1),
        IsFilterable BIT NOT NULL CONSTRAINT DF_DynamicReportColumns_IsFilterable DEFAULT(1),
        IsSortable BIT NOT NULL CONSTRAINT DF_DynamicReportColumns_IsSortable DEFAULT(1),
        IsGroupable BIT NOT NULL CONSTRAINT DF_DynamicReportColumns_IsGroupable DEFAULT(0),
        IsSummable BIT NOT NULL CONSTRAINT DF_DynamicReportColumns_IsSummable DEFAULT(0),
        Width INT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_DynamicReportColumns_SortOrder DEFAULT(0),
        CONSTRAINT FK_DynamicReportColumns_Report FOREIGN KEY (ReportId)
            REFERENCES dbo.DynamicReportDefinitions(ReportId)
    );

    CREATE UNIQUE INDEX UX_DynamicReportColumns_Report_Field ON dbo.DynamicReportColumns(ReportId, FieldName);
END
GO

IF OBJECT_ID(N'dbo.DynamicReportLayouts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynamicReportLayouts
    (
        LayoutId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DynamicReportLayouts PRIMARY KEY,
        ReportId INT NOT NULL,
        UserId INT NOT NULL,
        ProjectScope NVARCHAR(20) NOT NULL CONSTRAINT DF_DynamicReportLayouts_ProjectScope DEFAULT(N'Web'),
        LayoutName NVARCHAR(250) NOT NULL,
        LayoutJson NVARCHAR(MAX) NOT NULL,
        IsDefault BIT NOT NULL CONSTRAINT DF_DynamicReportLayouts_IsDefault DEFAULT(0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_DynamicReportLayouts_CreatedAt DEFAULT(GETDATE()),
        UpdatedAt DATETIME NULL,
        CONSTRAINT FK_DynamicReportLayouts_Report FOREIGN KEY (ReportId)
            REFERENCES dbo.DynamicReportDefinitions(ReportId)
    );

    CREATE INDEX IX_DynamicReportLayouts_User_Report ON dbo.DynamicReportLayouts(UserId, ProjectScope, ReportId);
END
GO

IF OBJECT_ID(N'dbo.DynamicReportPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynamicReportPermissions
    (
        PermissionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DynamicReportPermissions PRIMARY KEY,
        ReportId INT NOT NULL,
        ProjectScope NVARCHAR(20) NOT NULL CONSTRAINT DF_DynamicReportPermissions_ProjectScope DEFAULT(N'Shared'),
        UserId INT NULL,
        RoleId INT NULL,
        CanView BIT NOT NULL CONSTRAINT DF_DynamicReportPermissions_CanView DEFAULT(1),
        CanDesign BIT NOT NULL CONSTRAINT DF_DynamicReportPermissions_CanDesign DEFAULT(0),
        CanExport BIT NOT NULL CONSTRAINT DF_DynamicReportPermissions_CanExport DEFAULT(0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_DynamicReportPermissions_CreatedAt DEFAULT(GETDATE()),
        CONSTRAINT FK_DynamicReportPermissions_Report FOREIGN KEY (ReportId)
            REFERENCES dbo.DynamicReportDefinitions(ReportId)
    );

    CREATE INDEX IX_DynamicReportPermissions_ReportScope ON dbo.DynamicReportPermissions(ReportId, ProjectScope, UserId, RoleId);
END
GO

IF OBJECT_ID(N'dbo.DynamicReportLayouts', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DynamicReportLayouts_User_Report_Name' AND object_id = OBJECT_ID(N'dbo.DynamicReportLayouts'))
BEGIN
    CREATE UNIQUE INDEX UX_DynamicReportLayouts_User_Report_Name
        ON dbo.DynamicReportLayouts(ReportId, UserId, ProjectScope, LayoutName);
END
GO
