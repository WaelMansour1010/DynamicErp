/*
06_PropertyTypeMapping_DRAFT_SANDBOX_ONLY_20260520.sql
Purpose: Draft lookup mapping/staging for Adnan property and unit types in Sandbox only.
Status: DRAFT ONLY. Review before execution.
*/

IF DB_NAME() IN (N'Adnan', N'Alromaizan') OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN
    RAISERROR('Blocked: this script can run only inside a PropertyPilot/Sandbox database, never Adnan or Alromaizan.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('dbo.PropertyPilotLookupMapping','U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyPilotLookupMapping
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PropertyPilotLookupMapping PRIMARY KEY,
        MappingGroup NVARCHAR(100) NOT NULL,
        OldDatabaseName NVARCHAR(128) NOT NULL,
        OldTableName NVARCHAR(128) NOT NULL,
        OldId NVARCHAR(100) NOT NULL,
        OldName NVARCHAR(300) NULL,
        NewTableName NVARCHAR(128) NOT NULL,
        NewId INT NULL,
        NewName NVARCHAR(300) NULL,
        MappingStatus NVARCHAR(50) NOT NULL DEFAULT(N'PendingReview'),
        CreatedAt DATETIME NOT NULL DEFAULT(GETDATE()),
        Notes NVARCHAR(MAX) NULL
    );
END;

INSERT INTO dbo.PropertyPilotLookupMapping(MappingGroup,OldDatabaseName,OldTableName,OldId,OldName,NewTableName,NewId,NewName,MappingStatus,Notes)
SELECT N'PropertyType', N'Adnan', N'TblAqar.aqartypeid', CAST(x.aqartypeid AS NVARCHAR(100)),
       CASE x.aqartypeid WHEN 1 THEN N'سكنية' WHEN 2 THEN N'تجارية' ELSE N'غير محدد' END,
       N'PropertyType', pt.Id, pt.ArName,
       CASE WHEN pt.Id IS NULL THEN N'NeedsDecision' ELSE N'MappedByCode' END,
       N'Generated from active Adnan properties'
FROM (
    SELECT DISTINCT p.aqartypeid
    FROM Adnan.dbo.TblAqar p
    WHERE p.aqartypeid IS NOT NULL
) x
LEFT JOIN dbo.PropertyType pt ON pt.Code = CAST(x.aqartypeid AS NVARCHAR(100))
LEFT JOIN dbo.PropertyPilotLookupMapping existing ON existing.MappingGroup=N'PropertyType' AND existing.OldId=CAST(x.aqartypeid AS NVARCHAR(100))
WHERE existing.Id IS NULL;

INSERT INTO dbo.PropertyPilotLookupMapping(MappingGroup,OldDatabaseName,OldTableName,OldId,OldName,NewTableName,NewId,NewName,MappingStatus,Notes)
SELECT N'PropertyUnitType', N'Adnan', N'TblAkarUnit', CAST(au.id AS NVARCHAR(100)), au.name,
       N'PropertyUnitType', put.Id, put.ArName,
       CASE WHEN put.Id IS NULL THEN N'NeedsSeedOrManualMap' ELSE N'MappedByCode' END,
       N'Generated from Adnan unit type table'
FROM Adnan.dbo.TblAkarUnit au
LEFT JOIN dbo.PropertyUnitType put ON put.Code = CAST(au.id AS NVARCHAR(100))
LEFT JOIN dbo.PropertyPilotLookupMapping existing ON existing.MappingGroup=N'PropertyUnitType' AND existing.OldId=CAST(au.id AS NVARCHAR(100))
WHERE existing.Id IS NULL;

/* Optional after approval: seed missing PropertyUnitType rows based on Adnan.TblAkarUnit. */
-- INSERT INTO dbo.PropertyUnitType(Code, ArName, EnName, IsActive, IsDeleted)
-- SELECT OldId, OldName, NULL, 1, 0
-- FROM dbo.PropertyPilotLookupMapping
-- WHERE MappingGroup=N'PropertyUnitType' AND MappingStatus=N'NeedsSeedOrManualMap' AND NewId IS NULL;

SELECT * FROM dbo.PropertyPilotLookupMapping ORDER BY MappingGroup, OldId;
