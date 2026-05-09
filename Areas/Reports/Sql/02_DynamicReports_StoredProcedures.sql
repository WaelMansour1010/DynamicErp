/*
Dynamic Report Designer stored procedures.
SQL Server 2012 compatible; DROP then CREATE.
*/

IF OBJECT_ID(N'dbo.DynamicReport_ListAvailable', N'P') IS NOT NULL
    DROP PROCEDURE dbo.DynamicReport_ListAvailable;
GO
CREATE PROCEDURE dbo.DynamicReport_ListAvailable
    @ProjectScope NVARCHAR(20),
    @UserId INT = NULL,
    @RoleId INT = NULL,
    @IsAdmin BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        d.ReportId,
        d.ReportCode,
        d.ReportNameAr,
        d.ReportNameEn,
        d.ProjectScope,
        d.SourceType,
        d.SourceName,
        d.RequireDateRange,
        d.MaxRows,
        d.CommandTimeoutSeconds,
        d.IsActive
    FROM dbo.DynamicReportDefinitions d
    LEFT JOIN dbo.DynamicReportPermissions p
        ON p.ReportId = d.ReportId
       AND (p.ProjectScope = d.ProjectScope OR p.ProjectScope = N'Shared' OR d.ProjectScope = N'Shared')
       AND (p.UserId = @UserId OR p.RoleId = @RoleId OR (p.UserId IS NULL AND p.RoleId IS NULL))
    WHERE d.IsActive = 1
      AND (d.ProjectScope = @ProjectScope OR d.ProjectScope = N'Shared')
      AND (@IsAdmin = 1 OR ISNULL(p.CanView, 0) = 1)
    ORDER BY d.ReportNameAr, d.ReportNameEn;
END
GO

IF OBJECT_ID(N'dbo.DynamicReport_SeedSamples', N'P') IS NOT NULL
    DROP PROCEDURE dbo.DynamicReport_SeedSamples;
GO
CREATE PROCEDURE dbo.DynamicReport_SeedSamples
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.DynamicReportDefinitions WHERE ReportCode = N'WEB_USERS_SAMPLE')
    BEGIN
        INSERT INTO dbo.DynamicReportDefinitions
            (ReportCode, ReportNameAr, ReportNameEn, ProjectScope, SourceType, SourceName, MaxRows, CreatedBy)
        VALUES
            (N'WEB_USERS_SAMPLE', N'تقرير المستخدمين', N'Users Report', N'Web', N'View', N'dbo.DynamicReport_vw_WebUsersSample', 1000, @CreatedBy);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.DynamicReportDefinitions WHERE ReportCode = N'POS_SALES_SAMPLE')
    BEGIN
        INSERT INTO dbo.DynamicReportDefinitions
            (ReportCode, ReportNameAr, ReportNameEn, ProjectScope, SourceType, SourceName, RequireDateRange, MaxRows, CreatedBy)
        VALUES
            (N'POS_SALES_SAMPLE', N'تقرير مبيعات نقطة البيع', N'POS Sales Report', N'POS', N'View', N'dbo.DynamicReport_vw_PosSalesSample', 1, 1000, @CreatedBy);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.DynamicReportDefinitions WHERE ReportCode = N'MAINERP_JOURNAL_SAMPLE')
    BEGIN
        INSERT INTO dbo.DynamicReportDefinitions
            (ReportCode, ReportNameAr, ReportNameEn, ProjectScope, SourceType, SourceName, RequireDateRange, MaxRows, CreatedBy)
        VALUES
            (N'MAINERP_JOURNAL_SAMPLE', N'تقرير القيود اليومية', N'Journal Entries Report', N'MainErp', N'View', N'dbo.DynamicReport_vw_MainErpJournalSample', 1, 1000, @CreatedBy);
    END
END
GO
