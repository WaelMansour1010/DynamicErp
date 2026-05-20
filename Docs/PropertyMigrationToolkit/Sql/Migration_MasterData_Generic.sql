/* Enterprise Master Data Migration - Generic executable from staging contract. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
DECLARE @SourceDatabaseName sysname=N'$(SourceDatabaseName)';
DECLARE @MigrationMode nvarchar(30)=N'$(MigrationMode)';
DECLARE @AllowUnknownProperties bit=0,@AllowUnknownUnits bit=0,@AllowTemporaryRenterAccounts bit=0,@DefaultDepartmentId int=NULL,@UnknownPropertyTypeId int=NULL,@UnknownUnitTypeId int=NULL,@TempRenterAccountId int=NULL;

IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF @MigrationBatchId IS NULL BEGIN RAISERROR('MigrationBatchId is required.',16,1); RETURN; END;

SELECT TOP 1 @MigrationMode=MigrationMode,@AllowUnknownProperties=AllowUnknownProperties,@AllowUnknownUnits=AllowUnknownUnits,@AllowTemporaryRenterAccounts=AllowTemporaryRenterAccounts,
       @DefaultDepartmentId=DefaultDepartmentId,@UnknownPropertyTypeId=UnknownPropertyTypeId,@UnknownUnitTypeId=UnknownUnitTypeId,@TempRenterAccountId=TempRenterAccountId
FROM dbo.PropertyMigrationConfig WHERE CustomerCode=@CustomerCode AND TargetCloneDatabaseName=DB_NAME() ORDER BY ConfigId DESC;
IF @MigrationMode IS NULL SET @MigrationMode=N'Hybrid';

IF OBJECT_ID(N'dbo.PropertyMigrationSourceProperty','U') IS NULL OR OBJECT_ID(N'dbo.PropertyMigrationSourceUnit','U') IS NULL OR OBJECT_ID(N'dbo.PropertyMigrationSourceRenter','U') IS NULL
BEGIN RAISERROR('Source staging tables are missing. Run 01_SourceStagingTables_Generic.sql and customer mapping first.',16,1); RETURN; END;

INSERT INTO dbo.PropertyMigrationWarning(MigrationBatchId,CustomerCode,Severity,IssueType,EntityType,SourceDatabaseName,SourceTableName,SourceId,OriginalValue,AppliedFix,FallbackEntity,RequiresManualReview,SuggestedAction,Message)
SELECT @MigrationBatchId,@CustomerCode,N'Warning',N'MissingPropertyCodeOrName',N'Property',SourceDatabaseName,SourceTableName,SourceId,ISNULL(SourceCode,N'') + N'|' + ISNULL(ArName,N''),
       CASE WHEN @MigrationMode IN (N'Tolerant',N'Hybrid') THEN N'Generated migration code/name fallback' ELSE N'Excluded in strict mode' END,
       CASE WHEN @MigrationMode IN (N'Tolerant',N'Hybrid') THEN N'MIGRATION_UNKNOWN_PROPERTY' ELSE NULL END,
       1,N'Review source property identity before GoLive',N'Property has missing code or name.'
FROM dbo.PropertyMigrationSourceProperty s
WHERE s.MigrationBatchId=@MigrationBatchId AND (NULLIF(LTRIM(RTRIM(ISNULL(s.SourceCode,N''))),N'') IS NULL OR NULLIF(LTRIM(RTRIM(ISNULL(s.ArName,N''))),N'') IS NULL);

INSERT INTO dbo.PropertyMigrationExcludedRecord(MigrationBatchId,CustomerCode,EntityType,SourceDatabaseName,SourceTableName,SourceId,Severity,ExclusionReason,Decision,SuggestedAction)
SELECT @MigrationBatchId,@CustomerCode,N'Property',SourceDatabaseName,SourceTableName,SourceId,N'Warning',N'Missing code/name',N'ExcludedByStrictMode',N'Fix source mapping or run tolerant/hybrid mode'
FROM dbo.PropertyMigrationSourceProperty s
WHERE @MigrationMode=N'Strict' AND s.MigrationBatchId=@MigrationBatchId AND (NULLIF(LTRIM(RTRIM(ISNULL(s.SourceCode,N''))),N'') IS NULL OR NULLIF(LTRIM(RTRIM(ISNULL(s.ArName,N''))),N'') IS NULL);

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,s.SourceDatabaseName,s.SourceTableName,s.SourceId,N'Property',p.Id,N'Property',N'MappedExisting',0,0,N'Matched by Code'
FROM dbo.PropertyMigrationSourceProperty s JOIN dbo.Property p ON p.Code=s.SourceCode AND ISNULL(p.IsDeleted,0)=0
WHERE s.MigrationBatchId=@MigrationBatchId AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Property' AND m.SourceId=s.SourceId AND m.SourceTableName=s.SourceTableName);

DECLARE @PropertyOut TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(200),TargetId int,UsedFallback bit);
MERGE dbo.Property AS tgt
USING (
    SELECT SourceDatabaseName,SourceTableName,SourceId,
           CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(SourceCode,N''))),N'') IS NULL THEN N'MIG-PROP-' + CAST(Id AS nvarchar(20)) ELSE SourceCode END Code,
           CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ArName,N''))),N'') IS NULL THEN N'MIGRATION UNKNOWN PROPERTY ' + CAST(Id AS nvarchar(20)) ELSE ArName END ArName,
           EnName, ISNULL(PropertyTypeId,@UnknownPropertyTypeId) PropertyTypeId, ISNULL(DepartmentId,@DefaultDepartmentId) DepartmentId, Notes,
           CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(SourceCode,N''))),N'') IS NULL OR NULLIF(LTRIM(RTRIM(ISNULL(ArName,N''))),N'') IS NULL THEN 1 ELSE 0 END UsedFallback
    FROM dbo.PropertyMigrationSourceProperty
    WHERE MigrationBatchId=@MigrationBatchId AND IsValid=1
      AND (@MigrationMode<>N'Strict' OR (NULLIF(LTRIM(RTRIM(ISNULL(SourceCode,N''))),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM(ISNULL(ArName,N''))),N'') IS NOT NULL))
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Property' AND m.SourceId=PropertyMigrationSourceProperty.SourceId AND m.SourceTableName=PropertyMigrationSourceProperty.SourceTableName)
) AS src ON 1=0
WHEN NOT MATCHED THEN INSERT(Code,ArName,EnName,IsActive,IsDeleted,Notes,PropertyTypeId,DepartmentId)
VALUES(src.Code,src.ArName,src.EnName,1,0,src.Notes,src.PropertyTypeId,src.DepartmentId)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id,src.UsedFallback INTO @PropertyOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,FallbackEntity,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'Property',TargetId,N'Property',N'Inserted',UsedFallback,CASE WHEN UsedFallback=1 THEN N'MIGRATION_UNKNOWN_PROPERTY' ELSE NULL END,UsedFallback,N'Generic master data migration'
FROM @PropertyOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,s.SourceDatabaseName,s.SourceTableName,s.SourceId,N'PropertyUnit',u.Id,N'Unit',N'MappedExisting',0,0,N'Matched by Code'
FROM dbo.PropertyMigrationSourceUnit s JOIN dbo.PropertyUnit u ON u.Code=s.SourceCode AND ISNULL(u.IsDeleted,0)=0
WHERE s.MigrationBatchId=@MigrationBatchId AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Unit' AND m.SourceId=s.SourceId AND m.SourceTableName=s.SourceTableName);

DECLARE @UnitOut TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(200),TargetId int,UsedFallback bit);
MERGE dbo.PropertyUnit AS tgt
USING (
    SELECT SourceDatabaseName,SourceTableName,SourceId,
           CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(SourceCode,N''))),N'') IS NULL THEN N'MIG-UNIT-' + CAST(Id AS nvarchar(20)) ELSE SourceCode END Code,
           CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ArName,N''))),N'') IS NULL THEN N'MIGRATION UNKNOWN UNIT ' + CAST(Id AS nvarchar(20)) ELSE ArName END ArName,
           EnName, ISNULL(PropertyUnitTypeId,@UnknownUnitTypeId) PropertyUnitTypeId, PropertyUnitStatusId, Notes,
           CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(SourceCode,N''))),N'') IS NULL OR NULLIF(LTRIM(RTRIM(ISNULL(ArName,N''))),N'') IS NULL THEN 1 ELSE 0 END UsedFallback
    FROM dbo.PropertyMigrationSourceUnit
    WHERE MigrationBatchId=@MigrationBatchId AND IsValid=1
      AND (@MigrationMode<>N'Strict' OR (NULLIF(LTRIM(RTRIM(ISNULL(SourceCode,N''))),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM(ISNULL(ArName,N''))),N'') IS NOT NULL))
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Unit' AND m.SourceId=PropertyMigrationSourceUnit.SourceId AND m.SourceTableName=PropertyMigrationSourceUnit.SourceTableName)
) AS src ON 1=0
WHEN NOT MATCHED THEN INSERT(Code,ArName,EnName,IsActive,IsDeleted,Notes,PropertyUnitTypeId,PropertyUnitStatusId)
VALUES(src.Code,src.ArName,src.EnName,1,0,src.Notes,src.PropertyUnitTypeId,src.PropertyUnitStatusId)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id,src.UsedFallback INTO @UnitOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,FallbackEntity,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'PropertyUnit',TargetId,N'Unit',N'Inserted',UsedFallback,CASE WHEN UsedFallback=1 THEN N'MIGRATION_UNKNOWN_UNIT' ELSE NULL END,UsedFallback,N'Generic master data migration'
FROM @UnitOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,s.SourceDatabaseName,s.SourceTableName,s.SourceId,N'PropertyRenter',r.Id,N'Renter',N'MappedExisting',0,0,N'Matched by Code'
FROM dbo.PropertyMigrationSourceRenter s JOIN dbo.PropertyRenter r ON r.Code=s.SourceCode AND ISNULL(r.IsDeleted,0)=0
WHERE s.MigrationBatchId=@MigrationBatchId AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Renter' AND m.SourceId=s.SourceId AND m.SourceTableName=s.SourceTableName);

INSERT INTO dbo.PropertyMigrationWarning(MigrationBatchId,CustomerCode,Severity,IssueType,EntityType,SourceDatabaseName,SourceTableName,SourceId,OriginalValue,AppliedFix,FallbackEntity,RequiresManualReview,SuggestedAction,Message)
SELECT @MigrationBatchId,@CustomerCode,N'Warning',N'RenterMissingAccount',N'Renter',SourceDatabaseName,SourceTableName,SourceId,ISNULL(AccountCode,N''),
       CASE WHEN @AllowTemporaryRenterAccounts=1 THEN N'Use configured TempRenterAccountId' ELSE N'Keep NULL and block accounting usage' END,
       CASE WHEN @AllowTemporaryRenterAccounts=1 THEN N'MIGRATION_TEMP_RENTER_ACCOUNT' ELSE NULL END,1,N'Review renter account mapping',N'Renter has no mapped account.'
FROM dbo.PropertyMigrationSourceRenter s WHERE s.MigrationBatchId=@MigrationBatchId AND s.AccountId IS NULL;

DECLARE @RenterOut TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(200),TargetId int,UsedFallback bit);
MERGE dbo.PropertyRenter AS tgt
USING (
    SELECT SourceDatabaseName,SourceTableName,SourceId,
           CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(SourceCode,N''))),N'') IS NULL THEN N'MIG-RENTER-' + CAST(Id AS nvarchar(20)) ELSE SourceCode END Code,
           CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ArName,N''))),N'') IS NULL THEN N'MIGRATION UNKNOWN RENTER ' + CAST(Id AS nvarchar(20)) ELSE ArName END ArName,
           EnName, ISNULL(DepartmentId,@DefaultDepartmentId) DepartmentId, Mobile, Phone, NationalNo, VATNo, Notes,
           CASE WHEN AccountId IS NULL AND @AllowTemporaryRenterAccounts=1 THEN @TempRenterAccountId ELSE AccountId END AccountId,
           OpeningDebitBalance, OpeningCreditBalance,
           CASE WHEN AccountId IS NULL AND @AllowTemporaryRenterAccounts=1 THEN 1 ELSE 0 END UsedFallback
    FROM dbo.PropertyMigrationSourceRenter
    WHERE MigrationBatchId=@MigrationBatchId AND IsValid=1
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Renter' AND m.SourceId=PropertyMigrationSourceRenter.SourceId AND m.SourceTableName=PropertyMigrationSourceRenter.SourceTableName)
) AS src ON 1=0
WHEN NOT MATCHED THEN INSERT(Code,ArName,EnName,IsActive,IsDeleted,DepartmentId,Mobile,Phone,NationalNo,VATNo,Notes,AccountId,OpeningDebitBalance,OpeningCreditBalance)
VALUES(src.Code,src.ArName,src.EnName,1,0,src.DepartmentId,src.Mobile,src.Phone,src.NationalNo,src.VATNo,src.Notes,src.AccountId,src.OpeningDebitBalance,src.OpeningCreditBalance)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id,src.UsedFallback INTO @RenterOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,FallbackEntity,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'PropertyRenter',TargetId,N'Renter',N'Inserted',UsedFallback,CASE WHEN UsedFallback=1 THEN N'MIGRATION_TEMP_RENTER_ACCOUNT' ELSE NULL END,UsedFallback,N'Generic master data migration'
FROM @RenterOut;

SELECT 'MasterData' Stage,
       (SELECT COUNT(*) FROM @PropertyOut) InsertedProperties,
       (SELECT COUNT(*) FROM @UnitOut) InsertedUnits,
       (SELECT COUNT(*) FROM @RenterOut) InsertedRenters;
