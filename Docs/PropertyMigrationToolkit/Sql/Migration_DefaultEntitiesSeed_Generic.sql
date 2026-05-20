/* Generic default entity/fallback registration from approved config. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier='$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;

DECLARE @ConfigId int;
SELECT TOP 1 @ConfigId=ConfigId FROM dbo.PropertyMigrationConfig WHERE CustomerCode=@CustomerCode AND TargetCloneDatabaseName=DB_NAME() ORDER BY ConfigId DESC;
IF @ConfigId IS NULL BEGIN RAISERROR('PropertyMigrationConfig is required before default entity registration.',16,1); RETURN; END;

INSERT INTO dbo.PropertyMigrationFallback(CustomerCode,FallbackCode,EntityType,TargetTableName,TargetId,RequiresManualReview,Notes)
SELECT @CustomerCode,v.FallbackCode,v.EntityType,v.TargetTableName,v.TargetId,1,N'Registered by generic default entity seed'
FROM (
    SELECT N'MIGRATION_UNKNOWN_PROPERTY' FallbackCode,N'Property' EntityType,N'Property' TargetTableName,UnknownPropertyId TargetId FROM dbo.PropertyMigrationConfig WHERE ConfigId=@ConfigId
    UNION ALL SELECT N'MIGRATION_UNKNOWN_UNIT',N'Unit',N'PropertyUnit',UnknownUnitId FROM dbo.PropertyMigrationConfig WHERE ConfigId=@ConfigId
    UNION ALL SELECT N'MIGRATION_UNKNOWN_RENTER',N'Renter',N'PropertyRenter',UnknownRenterId FROM dbo.PropertyMigrationConfig WHERE ConfigId=@ConfigId
    UNION ALL SELECT N'MIGRATION_UNKNOWN_PROPERTY_TYPE',N'PropertyType',N'PropertyType',UnknownPropertyTypeId FROM dbo.PropertyMigrationConfig WHERE ConfigId=@ConfigId
    UNION ALL SELECT N'MIGRATION_UNKNOWN_UNIT_TYPE',N'UnitType',N'PropertyUnitType',UnknownUnitTypeId FROM dbo.PropertyMigrationConfig WHERE ConfigId=@ConfigId
    UNION ALL SELECT N'MIGRATION_DEFAULT_CASHBOX',N'CashBox',N'CashBox',DefaultCashBoxId FROM dbo.PropertyMigrationConfig WHERE ConfigId=@ConfigId
    UNION ALL SELECT N'MIGRATION_DEFAULT_BANK',N'BankAccount',N'BankAccount',DefaultBankAccountId FROM dbo.PropertyMigrationConfig WHERE ConfigId=@ConfigId
    UNION ALL SELECT N'MIGRATION_SUSPENSE_ACCOUNT',N'Account',N'ChartOfAccount',SuspenseAccountId FROM dbo.PropertyMigrationConfig WHERE ConfigId=@ConfigId
    UNION ALL SELECT N'MIGRATION_HOLDING_ACCOUNT',N'Account',N'ChartOfAccount',HoldingAccountId FROM dbo.PropertyMigrationConfig WHERE ConfigId=@ConfigId
) v
WHERE v.TargetId IS NOT NULL
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationFallback f WHERE f.CustomerCode=@CustomerCode AND f.FallbackCode=v.FallbackCode AND ISNULL(f.TargetId,-1)=ISNULL(v.TargetId,-1));

SELECT 'DefaultEntitiesSeed' Stage, COUNT(*) RegisteredFallbacks FROM dbo.PropertyMigrationFallback WHERE CustomerCode=@CustomerCode;
