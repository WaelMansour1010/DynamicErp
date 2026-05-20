/* Phase5 corrected PropertyUnitType mapping - SANDBOX ONLY */
IF DB_NAME() IN (N'Adnan', N'Alromaizan') OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;

BEGIN TRY
  BEGIN TRAN;

  INSERT INTO dbo.PropertyUnitType(Code, ArName, EnName, IsActive, IsDeleted)
  SELECT N'ADNAN-' + CAST(au.id AS NVARCHAR(20)), au.name, au.namee, 1, 0
  FROM Adnan.dbo.TblAkarUnit au
  LEFT JOIN dbo.PropertyUnitType put ON put.Code = N'ADNAN-' + CAST(au.id AS NVARCHAR(20))
  WHERE put.Id IS NULL;

  UPDATE m
  SET NewId=put.Id, NewName=put.ArName, MappingStatus=N'MappedToAdnanSeededLookup',
      Notes=ISNULL(m.Notes,N'') + N' | Corrected Phase5: mapped by ADNAN-prefixed code to avoid semantic collision.'
  FROM dbo.PropertyPilotLookupMapping m
  INNER JOIN dbo.PropertyUnitType put ON put.Code = N'ADNAN-' + m.OldId
  WHERE m.MappingGroup=N'PropertyUnitType';

  COMMIT TRAN;
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK TRAN;
  DECLARE @Msg NVARCHAR(4000); SET @Msg=ERROR_MESSAGE(); RAISERROR(@Msg,16,1);
END CATCH;

SELECT MappingGroup, OldId, OldName, NewId, NewName, MappingStatus
FROM dbo.PropertyPilotLookupMapping
WHERE MappingGroup IN (N'PropertyType', N'PropertyUnitType')
ORDER BY MappingGroup, OldId;
