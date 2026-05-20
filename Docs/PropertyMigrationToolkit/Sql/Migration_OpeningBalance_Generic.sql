/* Generic opening balance migration from PropertyMigrationSourceOpeningBalance staging. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
DECLARE @CutoffDate date='$(CutoffDate)';
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceOpeningBalance','U') IS NULL BEGIN RAISERROR('PropertyMigrationSourceOpeningBalance staging table is missing.',16,1); RETURN; END;

IF OBJECT_ID(N'dbo.PropertyPilotOpeningBalanceStaging','U') IS NULL
CREATE TABLE dbo.PropertyPilotOpeningBalanceStaging(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    OldDatabaseName nvarchar(128) NULL,
    OldContractNo int NULL,
    OldRenterId int NULL,
    OldAccountCode nvarchar(100) NULL,
    DueInstallmentTotal money NULL,
    TruePaid money NULL,
    OpeningBalanceAmount money NOT NULL,
    CutoverDate datetime NOT NULL,
    NewPropertyContractId int NULL,
    NewPropertyRenterId int NULL,
    NewAccountId int NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE()),
    Notes nvarchar(max) NULL
);

INSERT INTO dbo.PropertyMigrationError(MigrationBatchId,CustomerCode,Severity,Stage,StepName,IssueType,EntityType,SourceId,ErrorMessage)
SELECT @MigrationBatchId,@CustomerCode,N'Critical',N'Migration',N'Migration_OpeningBalance_Generic',N'MissingOpeningBalanceAccount',N'OpeningBalance',CAST(Id AS nvarchar(200)),N'Opening balance has no AccountId.'
FROM dbo.PropertyMigrationSourceOpeningBalance WHERE MigrationBatchId=@MigrationBatchId AND IsValid=1 AND AccountId IS NULL;

INSERT INTO dbo.PropertyPilotOpeningBalanceStaging(MigrationBatchId,OldDatabaseName,OldContractNo,OldRenterId,OldAccountCode,OpeningBalanceAmount,CutoverDate,NewPropertyContractId,NewPropertyRenterId,NewAccountId,Notes)
SELECT s.MigrationBatchId,s.SourceDatabaseName,TRY_CONVERT(int,s.SourceContractId),TRY_CONVERT(int,s.SourceRenterId),s.AccountCode,s.OpeningBalanceAmount,s.CutoffDate,cm.TargetId,rm.TargetId,s.AccountId,s.Notes
FROM dbo.PropertyMigrationSourceOpeningBalance s
LEFT JOIN dbo.PropertyMigrationEntityMap cm ON cm.MigrationBatchId=@MigrationBatchId AND cm.EntityType=N'Contract' AND cm.SourceId=s.SourceContractId
LEFT JOIN dbo.PropertyMigrationEntityMap rm ON rm.MigrationBatchId=@MigrationBatchId AND rm.EntityType=N'Renter' AND rm.SourceId=s.SourceRenterId
WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1 AND s.AccountId IS NOT NULL
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyPilotOpeningBalanceStaging x WHERE x.MigrationBatchId=s.MigrationBatchId AND ISNULL(x.OldContractNo,-1)=ISNULL(TRY_CONVERT(int,s.SourceContractId),-1) AND ISNULL(x.OldRenterId,-1)=ISNULL(TRY_CONVERT(int,s.SourceRenterId),-1));

SELECT 'OpeningBalance' Stage, COUNT(*) StagingRows, ISNULL(SUM(OpeningBalanceAmount),0) OpeningBalanceAmount FROM dbo.PropertyPilotOpeningBalanceStaging WHERE MigrationBatchId=@MigrationBatchId;
