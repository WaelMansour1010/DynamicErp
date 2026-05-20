/* Generic advance payments migration from PropertyMigrationSourceAdvancePayment staging. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceAdvancePayment','U') IS NULL BEGIN RAISERROR('PropertyMigrationSourceAdvancePayment staging table is missing.',16,1); RETURN; END;

IF OBJECT_ID(N'dbo.PropertyPilotAdvancePaymentStaging','U') IS NULL
CREATE TABLE dbo.PropertyPilotAdvancePaymentStaging(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    OldDatabaseName nvarchar(128) NULL,
    OldContractNo int NULL,
    OldInstallmentId int NULL,
    OldRenterId int NULL,
    FutureInstallmentValue money NULL,
    AdvancePaidAmount money NOT NULL,
    FutureRemainAfterAdvance money NULL,
    NewPropertyContractId int NULL,
    NewPropertyContractBatchId int NULL,
    NewPropertyRenterId int NULL,
    NewAccountId int NULL,
    TreatmentDecision nvarchar(100) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE()),
    Notes nvarchar(max) NULL
);

INSERT INTO dbo.PropertyPilotAdvancePaymentStaging(MigrationBatchId,OldDatabaseName,OldContractNo,OldInstallmentId,OldRenterId,FutureInstallmentValue,AdvancePaidAmount,FutureRemainAfterAdvance,NewPropertyContractId,NewPropertyContractBatchId,NewPropertyRenterId,TreatmentDecision,Notes)
SELECT s.MigrationBatchId,s.SourceDatabaseName,TRY_CONVERT(int,s.SourceContractId),TRY_CONVERT(int,s.SourceInstallmentId),TRY_CONVERT(int,s.SourceRenterId),s.FutureInstallmentValue,s.AdvancePaidAmount,s.FutureRemainAfterAdvance,cm.TargetId,im.TargetId,rm.TargetId,N'StageAsAdvancePayment',s.Notes
FROM dbo.PropertyMigrationSourceAdvancePayment s
LEFT JOIN dbo.PropertyMigrationEntityMap cm ON cm.MigrationBatchId=@MigrationBatchId AND cm.EntityType=N'Contract' AND cm.SourceId=s.SourceContractId
LEFT JOIN dbo.PropertyMigrationEntityMap im ON im.MigrationBatchId=@MigrationBatchId AND im.EntityType=N'Installment' AND im.SourceId=s.SourceInstallmentId
LEFT JOIN dbo.PropertyMigrationEntityMap rm ON rm.MigrationBatchId=@MigrationBatchId AND rm.EntityType=N'Renter' AND rm.SourceId=s.SourceRenterId
WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyPilotAdvancePaymentStaging x WHERE x.MigrationBatchId=s.MigrationBatchId AND ISNULL(x.OldInstallmentId,-1)=ISNULL(TRY_CONVERT(int,s.SourceInstallmentId),-1) AND ISNULL(x.OldContractNo,-1)=ISNULL(TRY_CONVERT(int,s.SourceContractId),-1));

SELECT 'AdvancePayments' Stage, COUNT(*) StagingRows, ISNULL(SUM(AdvancePaidAmount),0) AdvancePaidAmount FROM dbo.PropertyPilotAdvancePaymentStaging WHERE MigrationBatchId=@MigrationBatchId;
