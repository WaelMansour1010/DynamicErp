IF COL_LENGTH('dbo.DynamicReportColumns','DisplayFormat') IS NULL
    ALTER TABLE dbo.DynamicReportColumns ADD DisplayFormat NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.DynamicReportColumns','DecimalPlaces') IS NULL
    ALTER TABLE dbo.DynamicReportColumns ADD DecimalPlaces INT NULL;
GO

IF COL_LENGTH('dbo.DynamicReportColumns','TextAlign') IS NULL
    ALTER TABLE dbo.DynamicReportColumns ADD TextAlign NVARCHAR(10) NULL;
GO

IF COL_LENGTH('dbo.DynamicReportColumns','IsAggregatable') IS NULL
    ALTER TABLE dbo.DynamicReportColumns
      ADD IsAggregatable BIT NOT NULL CONSTRAINT DF_DRC_IsAggregatable DEFAULT 0;
GO

IF COL_LENGTH('dbo.DynamicReportColumns','AggregateFunction') IS NULL
    ALTER TABLE dbo.DynamicReportColumns ADD AggregateFunction NVARCHAR(20) NULL;
GO

IF OBJECT_ID('dbo.DynamicReportAuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynamicReportAuditLog (
        AuditId      INT IDENTITY(1,1) PRIMARY KEY,
        ReportId     INT NULL,
        ProjectScope NVARCHAR(20) NOT NULL,
        ActionType   NVARCHAR(50) NOT NULL,
        OldValue     NVARCHAR(MAX) NULL,
        NewValue     NVARCHAR(MAX) NULL,
        PerformedBy  INT NULL,
        PerformedAt  DATETIME NOT NULL CONSTRAINT DF_DRAL_PerformedAt DEFAULT GETDATE(),
        Notes        NVARCHAR(1000) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DRAL_Report' AND object_id = OBJECT_ID('dbo.DynamicReportAuditLog'))
    CREATE INDEX IX_DRAL_Report ON dbo.DynamicReportAuditLog(ProjectScope, ReportId, PerformedAt);
GO
