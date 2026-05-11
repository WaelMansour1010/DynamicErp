/*
    CompanyAllowedPages_Setup.sql
    Company/client-level screen availability for DynamicErp.

    SQL Server 2012 compatible.
    Idempotent: creates table, constraints, and indexes only when missing.
    No destructive data changes.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.CompanyAllowedPages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyAllowedPages
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CompanyAllowedPages PRIMARY KEY,
        SystemPageId int NOT NULL,
        IsSelected bit NOT NULL CONSTRAINT DF_CompanyAllowedPages_IsSelected DEFAULT (1),
        CreatedDate datetime NULL,
        CreatedBy int NULL,
        UpdatedDate datetime NULL,
        UpdatedBy int NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_CompanyAllowedPages_SystemPage'
      AND parent_object_id = OBJECT_ID(N'dbo.CompanyAllowedPages')
)
BEGIN
    ALTER TABLE dbo.CompanyAllowedPages
    ADD CONSTRAINT FK_CompanyAllowedPages_SystemPage
        FOREIGN KEY (SystemPageId) REFERENCES dbo.SystemPage(Id);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_CompanyAllowedPages_SystemPageId'
      AND object_id = OBJECT_ID(N'dbo.CompanyAllowedPages')
)
BEGIN
    CREATE UNIQUE INDEX UX_CompanyAllowedPages_SystemPageId
        ON dbo.CompanyAllowedPages(SystemPageId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_CompanyAllowedPages_IsSelected'
      AND object_id = OBJECT_ID(N'dbo.CompanyAllowedPages')
)
BEGIN
    CREATE INDEX IX_CompanyAllowedPages_IsSelected
        ON dbo.CompanyAllowedPages(IsSelected, SystemPageId);
END;

PRINT 'CompanyAllowedPages setup complete.';

/* Verification */
SELECT
    OBJECT_ID(N'dbo.CompanyAllowedPages', N'U') AS CompanyAllowedPagesObjectId,
    COUNT(*) AS ExistingRows
FROM dbo.CompanyAllowedPages;

SELECT TOP (25)
    cap.Id,
    cap.SystemPageId,
    sp.ArName AS SystemPageArName,
    sp.ControllerName,
    cap.IsSelected,
    cap.CreatedDate,
    cap.CreatedBy,
    cap.UpdatedDate,
    cap.UpdatedBy
FROM dbo.CompanyAllowedPages cap
INNER JOIN dbo.SystemPage sp ON sp.Id = cap.SystemPageId
ORDER BY sp.ArName;

SELECT
    sp.Id,
    sp.ArName,
    sp.ControllerName,
    CAST(CASE WHEN cap.Id IS NULL THEN 0 ELSE 1 END AS bit) AS HasCompanyAvailabilityRow
FROM dbo.SystemPage sp
LEFT JOIN dbo.CompanyAllowedPages cap ON cap.SystemPageId = sp.Id
WHERE sp.IsDeleted = 0
  AND sp.IsActive = 1
  AND ISNULL(sp.IsModule, 0) = 0
  AND sp.ControllerName IS NOT NULL
ORDER BY HasCompanyAvailabilityRow, sp.ArName;
