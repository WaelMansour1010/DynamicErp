/* Generic terminations migration - review-gated staging, no duplicate close effect by default. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier='$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceTermination','U') IS NULL BEGIN RAISERROR('PropertyMigrationSourceTermination staging table is missing.',16,1); RETURN; END;

INSERT INTO dbo.PropertyMigrationReviewQueue(MigrationBatchId,CustomerCode,Priority,Severity,IssueType,EntityType,SourceDatabaseName,SourceTableName,SourceId,OriginalValue,SuggestedAction,Status)
SELECT @MigrationBatchId,@CustomerCode,1,N'Warning',N'TerminationRequiresManualReview',N'Termination',SourceDatabaseName,SourceTableName,SourceId,CAST(Amount AS nvarchar(50)),N'Validate termination effect and journal impact before migration',N'Open'
FROM dbo.PropertyMigrationSourceTermination s
WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationReviewQueue q WHERE q.MigrationBatchId=@MigrationBatchId AND q.EntityType=N'Termination' AND q.SourceId=s.SourceId);

INSERT INTO dbo.PropertyMigrationExcludedRecord(MigrationBatchId,CustomerCode,EntityType,SourceDatabaseName,SourceTableName,SourceId,Severity,ExclusionReason,Decision,SuggestedAction)
SELECT @MigrationBatchId,@CustomerCode,N'Termination',SourceDatabaseName,SourceTableName,SourceId,N'Warning',N'Termination migration is manual-review by default',N'ExcludedUntilApproved',N'Approve close effect and accounting before migration'
FROM dbo.PropertyMigrationSourceTermination s
WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationExcludedRecord e WHERE e.MigrationBatchId=@MigrationBatchId AND e.EntityType=N'Termination' AND e.SourceId=s.SourceId);

SELECT 'Terminations' Stage, 0 InsertedTerminations, COUNT(*) ReviewQueueItems FROM dbo.PropertyMigrationSourceTermination WHERE MigrationBatchId=@MigrationBatchId;
