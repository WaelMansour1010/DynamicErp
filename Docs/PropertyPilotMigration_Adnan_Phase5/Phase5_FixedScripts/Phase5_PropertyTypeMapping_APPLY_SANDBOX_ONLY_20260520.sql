/* Phase5 Property/Unit Type Mapping Apply - SANDBOX ONLY */
IF DB_NAME() IN (N'Adnan', N'Alromaizan') OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;

BEGIN TRY
  BEGIN TRAN;

  /* Seed missing active Adnan unit types into sandbox lookup. */
  INSERT INTO dbo.PropertyUnitType(Code, ArName, EnName, IsActive, IsDeleted)
  SELECT CAST(au.id AS NVARCHAR(100)), au.name, au.namee, 1, 0
  FROM Adnan.dbo.TblAkarUnit au
  LEFT JOIN dbo.PropertyUnitType put ON put.Code = CAST(au.id AS NVARCHAR(100))
  WHERE put.Id IS NULL AND au.id IN (1,2,3,4,5,6,7,8,9,10,12);

  /* Refresh mapping rows for unit types. */
  UPDATE m
  SET NewId=put.Id, NewName=put.ArName, MappingStatus=N'MappedOrSeeded'
  FROM dbo.PropertyPilotLookupMapping m
  INNER JOIN dbo.PropertyUnitType put ON put.Code=m.OldId
  WHERE m.MappingGroup=N'PropertyUnitType';

  /* Property types 1/2 are mapped by code. Null remains intentionally unmapped. */
  UPDATE m
  SET NewId=pt.Id, NewName=pt.ArName, MappingStatus=N'MappedByCode'
  FROM dbo.PropertyPilotLookupMapping m
  INNER JOIN dbo.PropertyType pt ON pt.Code=m.OldId
  WHERE m.MappingGroup=N'PropertyType';

  COMMIT TRAN;
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK TRAN;
  DECLARE @Msg NVARCHAR(4000); SET @Msg=ERROR_MESSAGE(); RAISERROR(@Msg,16,1);
END CATCH;

SELECT MappingGroup, OldId, OldName, NewId, NewName, MappingStatus
FROM dbo.PropertyPilotLookupMapping
ORDER BY MappingGroup, TRY_CONVERT(int, OldId), OldId;
