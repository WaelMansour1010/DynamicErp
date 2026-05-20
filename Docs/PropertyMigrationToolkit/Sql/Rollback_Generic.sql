/* Generic rollback template. Clone only; requires explicit confirmation. */
DECLARE @MigrationBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @Confirm nvarchar(100)=N'NO';
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%Sandbox%' BEGIN RAISERROR('Clone required',16,1); RETURN; END;
IF @Confirm<>N'YES_ROLLBACK_PROPERTY_MIGRATION' BEGIN RAISERROR('Explicit confirmation required.',16,1); RETURN; END;
RAISERROR('Template only: implement delete order per EntityMap for the approved customer migration scope.',16,1);
