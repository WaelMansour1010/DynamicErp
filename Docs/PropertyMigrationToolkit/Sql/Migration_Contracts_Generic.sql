/* Generic contracts migration from PropertyMigrationSourceContract staging. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
DECLARE @MigrationMode nvarchar(30)=N'$(MigrationMode)';
DECLARE @UnknownPropertyId int=NULL,@UnknownUnitId int=NULL,@UnknownRenterId int=NULL,@DefaultDepartmentId int=NULL;
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceContract','U') IS NULL BEGIN RAISERROR('PropertyMigrationSourceContract staging table is missing.',16,1); RETURN; END;

SELECT TOP 1 @MigrationMode=MigrationMode,@UnknownPropertyId=UnknownPropertyId,@UnknownUnitId=UnknownUnitId,@UnknownRenterId=UnknownRenterId,@DefaultDepartmentId=DefaultDepartmentId
FROM dbo.PropertyMigrationConfig WHERE CustomerCode=@CustomerCode AND TargetCloneDatabaseName=DB_NAME() ORDER BY ConfigId DESC;
IF @MigrationMode IS NULL SET @MigrationMode=N'Hybrid';

;WITH Resolved AS (
    SELECT s.*, pm.TargetId PropertyId, um.TargetId UnitId, rm.TargetId RenterId,
           CASE WHEN pm.TargetId IS NULL THEN 1 ELSE 0 END MissingProperty,
           CASE WHEN um.TargetId IS NULL THEN 1 ELSE 0 END MissingUnit,
           CASE WHEN rm.TargetId IS NULL THEN 1 ELSE 0 END MissingRenter
    FROM dbo.PropertyMigrationSourceContract s
    LEFT JOIN dbo.PropertyMigrationEntityMap pm ON pm.MigrationBatchId=@MigrationBatchId AND pm.EntityType=N'Property' AND pm.SourceId=s.SourcePropertyId
    LEFT JOIN dbo.PropertyMigrationEntityMap um ON um.MigrationBatchId=@MigrationBatchId AND um.EntityType=N'Unit' AND um.SourceId=s.SourceUnitId
    LEFT JOIN dbo.PropertyMigrationEntityMap rm ON rm.MigrationBatchId=@MigrationBatchId AND rm.EntityType=N'Renter' AND rm.SourceId=s.SourceRenterId
    WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1 AND s.IsActiveContract=1
)
INSERT INTO dbo.PropertyMigrationWarning(MigrationBatchId,CustomerCode,Severity,IssueType,EntityType,SourceDatabaseName,SourceTableName,SourceId,OriginalValue,AppliedFix,FallbackEntity,RequiresManualReview,SuggestedAction,Message)
SELECT @MigrationBatchId,@CustomerCode,N'Warning',N'ContractMissingLink',N'Contract',SourceDatabaseName,SourceTableName,SourceId,
       N'Property=' + ISNULL(SourcePropertyId,N'') + N';Unit=' + ISNULL(SourceUnitId,N'') + N';Renter=' + ISNULL(SourceRenterId,N''),
       CASE WHEN @MigrationMode IN (N'Tolerant',N'Hybrid') THEN N'Fallback placeholder if configured' ELSE N'Excluded in strict mode' END,
       N'MIGRATION_UNKNOWN_ENTITY',1,N'Review contract links before GoLive',N'Contract has missing property/unit/renter link.'
FROM Resolved WHERE MissingProperty=1 OR MissingUnit=1 OR MissingRenter=1;

;WITH Resolved AS (
    SELECT s.*, pm.TargetId PropertyId, um.TargetId UnitId, rm.TargetId RenterId,
           CASE WHEN pm.TargetId IS NULL THEN 1 ELSE 0 END MissingProperty,
           CASE WHEN um.TargetId IS NULL THEN 1 ELSE 0 END MissingUnit,
           CASE WHEN rm.TargetId IS NULL THEN 1 ELSE 0 END MissingRenter
    FROM dbo.PropertyMigrationSourceContract s
    LEFT JOIN dbo.PropertyMigrationEntityMap pm ON pm.MigrationBatchId=@MigrationBatchId AND pm.EntityType=N'Property' AND pm.SourceId=s.SourcePropertyId
    LEFT JOIN dbo.PropertyMigrationEntityMap um ON um.MigrationBatchId=@MigrationBatchId AND um.EntityType=N'Unit' AND um.SourceId=s.SourceUnitId
    LEFT JOIN dbo.PropertyMigrationEntityMap rm ON rm.MigrationBatchId=@MigrationBatchId AND rm.EntityType=N'Renter' AND rm.SourceId=s.SourceRenterId
    WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1 AND s.IsActiveContract=1
)
INSERT INTO dbo.PropertyMigrationExcludedRecord(MigrationBatchId,CustomerCode,EntityType,SourceDatabaseName,SourceTableName,SourceId,Severity,ExclusionReason,Decision,SuggestedAction)
SELECT @MigrationBatchId,@CustomerCode,N'Contract',SourceDatabaseName,SourceTableName,SourceId,N'Warning',N'Missing property/unit/renter link',N'ExcludedByStrictMode',N'Fix mapping or use approved fallback placeholders'
FROM Resolved WHERE @MigrationMode=N'Strict' AND (MissingProperty=1 OR MissingUnit=1 OR MissingRenter=1);

;WITH Resolved AS (
    SELECT s.*, pm.TargetId PropertyId, um.TargetId UnitId, rm.TargetId RenterId,
           CASE WHEN pm.TargetId IS NULL THEN @UnknownPropertyId ELSE pm.TargetId END FinalPropertyId,
           CASE WHEN um.TargetId IS NULL THEN @UnknownUnitId ELSE um.TargetId END FinalUnitId,
           CASE WHEN rm.TargetId IS NULL THEN @UnknownRenterId ELSE rm.TargetId END FinalRenterId,
           CASE WHEN pm.TargetId IS NULL OR um.TargetId IS NULL OR rm.TargetId IS NULL THEN 1 ELSE 0 END UsedFallback
    FROM dbo.PropertyMigrationSourceContract s
    LEFT JOIN dbo.PropertyMigrationEntityMap pm ON pm.MigrationBatchId=@MigrationBatchId AND pm.EntityType=N'Property' AND pm.SourceId=s.SourcePropertyId
    LEFT JOIN dbo.PropertyMigrationEntityMap um ON um.MigrationBatchId=@MigrationBatchId AND um.EntityType=N'Unit' AND um.SourceId=s.SourceUnitId
    LEFT JOIN dbo.PropertyMigrationEntityMap rm ON rm.MigrationBatchId=@MigrationBatchId AND rm.EntityType=N'Renter' AND rm.SourceId=s.SourceRenterId
    WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1 AND s.IsActiveContract=1
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Contract' AND m.SourceId=s.SourceId AND m.SourceTableName=s.SourceTableName)
), ToInsert AS (
    SELECT * FROM Resolved
    WHERE (@MigrationMode<>N'Strict' OR UsedFallback=0)
      AND (FinalPropertyId IS NOT NULL OR PropertyId IS NOT NULL)
      AND (FinalUnitId IS NOT NULL OR UnitId IS NOT NULL)
      AND (FinalRenterId IS NOT NULL OR RenterId IS NOT NULL)
)
SELECT * INTO #ContractsToInsert FROM ToInsert;

INSERT INTO dbo.PropertyMigrationError(MigrationBatchId,CustomerCode,Severity,Stage,StepName,IssueType,EntityType,SourceTableName,SourceId,ErrorMessage)
SELECT @MigrationBatchId,@CustomerCode,N'Critical',N'Migration',N'Migration_Contracts_Generic',N'MissingFallbackConfig',N'Contract',SourceTableName,SourceId,N'Contract needs fallback but UnknownPropertyId/UnknownUnitId/UnknownRenterId is not configured.'
FROM #ContractsToInsert WHERE UsedFallback=1 AND (FinalPropertyId IS NULL OR FinalUnitId IS NULL OR FinalRenterId IS NULL);

DELETE FROM #ContractsToInsert WHERE UsedFallback=1 AND (FinalPropertyId IS NULL OR FinalUnitId IS NULL OR FinalRenterId IS NULL);

DECLARE @Out TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(200),TargetId int,UsedFallback bit);
MERGE dbo.PropertyContract AS tgt
USING #ContractsToInsert AS src ON 1=0
WHEN NOT MATCHED THEN INSERT(DocumentNumber,VoucherDate,PropertyId,PropertyUnitId,PropertyRenterId,RentValue,NetTotal,TotalAfterTaxes,VATPercentage,VATValue,ContractStartDate,ContractEndDate,PropertyUnitTypeId,DepartmentId,NumberOfBatches,FirstBatchDate,PeriodBetweenBatchesNum,PeriodBetweenBatchesTypeId,IsDeleted,IsRenewed,Notes)
VALUES(src.DocumentNumber,src.VoucherDate,src.FinalPropertyId,src.FinalUnitId,src.FinalRenterId,src.RentValue,src.NetTotal,src.TotalAfterTaxes,src.VATPercentage,src.VATValue,src.ContractStartDate,src.ContractEndDate,src.PropertyUnitTypeId,ISNULL(src.DepartmentId,@DefaultDepartmentId),src.NumberOfBatches,src.FirstBatchDate,src.PeriodBetweenBatchesNum,src.PeriodBetweenBatchesTypeId,0,0,src.Notes)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id,src.UsedFallback INTO @Out;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,FallbackEntity,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'PropertyContract',TargetId,N'Contract',N'Inserted',UsedFallback,CASE WHEN UsedFallback=1 THEN N'MIGRATION_UNKNOWN_CONTRACT_LINK' ELSE NULL END,UsedFallback,N'Generic contracts migration'
FROM @Out;

SELECT 'Contracts' Stage, COUNT(*) InsertedContracts FROM @Out;
DROP TABLE #ContractsToInsert;
