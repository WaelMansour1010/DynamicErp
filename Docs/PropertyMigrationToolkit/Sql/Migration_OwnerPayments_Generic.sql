/* Generic owner payments migration - review queue only until owner payment semantics are approved. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier='$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceOwnerPayment','U') IS NULL BEGIN RAISERROR('PropertyMigrationSourceOwnerPayment staging table is missing.',16,1); RETURN; END;

INSERT INTO dbo.PropertyMigrationReviewQueue(MigrationBatchId,CustomerCode,Priority,Severity,IssueType,EntityType,SourceDatabaseName,SourceTableName,SourceId,OriginalValue,SuggestedAction,Status)
SELECT @MigrationBatchId,@CustomerCode,1,N'Warning',N'OwnerPaymentRequiresApproval',N'OwnerPayment',SourceDatabaseName,SourceTableName,SourceId,CAST(MoneyAmount AS nvarchar(50)),N'Approve owner/property/account linkage and journal effect before migration',N'Open'
FROM dbo.PropertyMigrationSourceOwnerPayment s
WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationReviewQueue q WHERE q.MigrationBatchId=@MigrationBatchId AND q.EntityType=N'OwnerPayment' AND q.SourceId=s.SourceId);

INSERT INTO dbo.PropertyMigrationExcludedRecord(MigrationBatchId,CustomerCode,EntityType,SourceDatabaseName,SourceTableName,SourceId,Severity,ExclusionReason,Decision,SuggestedAction)
SELECT @MigrationBatchId,@CustomerCode,N'OwnerPayment',SourceDatabaseName,SourceTableName,SourceId,N'Warning',N'Owner payment migration is not enabled by default',N'ManualReviewOnly',N'Enable only after finance sign-off and verified owner account mapping'
FROM dbo.PropertyMigrationSourceOwnerPayment s
WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationExcludedRecord e WHERE e.MigrationBatchId=@MigrationBatchId AND e.EntityType=N'OwnerPayment' AND e.SourceId=s.SourceId);

SELECT 'OwnerPayments' Stage, 0 InsertedOwnerPayments, COUNT(*) ReviewQueueItems FROM dbo.PropertyMigrationSourceOwnerPayment WHERE MigrationBatchId=@MigrationBatchId;
