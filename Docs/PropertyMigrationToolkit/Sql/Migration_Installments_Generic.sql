/* Generic installments migration from PropertyMigrationSourceInstallment staging. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceInstallment','U') IS NULL BEGIN RAISERROR('PropertyMigrationSourceInstallment staging table is missing.',16,1); RETURN; END;

INSERT INTO dbo.PropertyMigrationExcludedRecord(MigrationBatchId,CustomerCode,EntityType,SourceDatabaseName,SourceTableName,SourceId,Severity,ExclusionReason,Decision,SuggestedAction)
SELECT @MigrationBatchId,@CustomerCode,N'Installment',s.SourceDatabaseName,s.SourceTableName,s.SourceId,N'Critical',N'Contract cross-reference missing',N'Excluded',N'Migrate/link contract first'
FROM dbo.PropertyMigrationSourceInstallment s
LEFT JOIN dbo.PropertyMigrationEntityMap cm ON cm.MigrationBatchId=@MigrationBatchId AND cm.EntityType=N'Contract' AND cm.SourceId=s.SourceContractId
WHERE s.MigrationBatchId=@MigrationBatchId AND cm.TargetId IS NULL
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationExcludedRecord e WHERE e.MigrationBatchId=@MigrationBatchId AND e.EntityType=N'Installment' AND e.SourceId=s.SourceId AND e.SourceTableName=s.SourceTableName);

DECLARE @Out TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(200),TargetId int);
MERGE dbo.PropertyContractBatch AS tgt
USING (
    SELECT s.*, cm.TargetId ContractId
    FROM dbo.PropertyMigrationSourceInstallment s
    JOIN dbo.PropertyMigrationEntityMap cm ON cm.MigrationBatchId=@MigrationBatchId AND cm.EntityType=N'Contract' AND cm.SourceId=s.SourceContractId
    WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Installment' AND m.SourceId=s.SourceId AND m.SourceTableName=s.SourceTableName)
) AS src ON 1=0
WHEN NOT MATCHED THEN INSERT(MainDocId,BatchNo,BatchDate,BatchRentValue,BatchRentValueTaxes,BatchWaterValue,BatchWaterValueTaxes,BatchElectricityValue,BatchElectricityValueTaxes,BatchCommissionValue,BatchTotal,IsDeleted,Notes,IsDelivered,IsRegisteredAsDue,BatchCommissionValueTaxes,BatchGasValue,BatchGasValueTaxes,BatchServicesValue,BatchServicesValueTaxes,BatchInsuranceValue,BatchInsuranceValueTaxes,IsRegisteredAsRevenue)
VALUES(src.ContractId,src.BatchNo,src.BatchDate,ISNULL(src.BatchRentValue,0),ISNULL(src.BatchRentValueTaxes,0),ISNULL(src.BatchWaterValue,0),ISNULL(src.BatchWaterValueTaxes,0),ISNULL(src.BatchElectricityValue,0),ISNULL(src.BatchElectricityValueTaxes,0),ISNULL(src.BatchCommissionValue,0),ISNULL(src.BatchTotal,0),0,src.Notes,0,0,ISNULL(src.BatchCommissionValueTaxes,0),ISNULL(src.BatchGasValue,0),ISNULL(src.BatchGasValueTaxes,0),ISNULL(src.BatchServicesValue,0),ISNULL(src.BatchServicesValueTaxes,0),ISNULL(src.BatchInsuranceValue,0),ISNULL(src.BatchInsuranceValueTaxes,0),0)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id INTO @Out;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'PropertyContractBatch',TargetId,N'Installment',N'Inserted',0,0,N'Generic installments migration'
FROM @Out;

SELECT 'Installments' Stage, COUNT(*) InsertedInstallments FROM @Out;
