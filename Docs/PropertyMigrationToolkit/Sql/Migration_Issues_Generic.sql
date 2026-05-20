/* Generic issues migration - strict/manual review by default. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier='$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
DECLARE @ExcludeUnsafeOwnerPayments bit=1;
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceIssue','U') IS NULL BEGIN RAISERROR('PropertyMigrationSourceIssue staging table is missing.',16,1); RETURN; END;
SELECT TOP 1 @ExcludeUnsafeOwnerPayments=ExcludeUnsafeOwnerPayments FROM dbo.PropertyMigrationConfig WHERE CustomerCode=@CustomerCode AND TargetCloneDatabaseName=DB_NAME() ORDER BY ConfigId DESC;

INSERT INTO dbo.PropertyMigrationReviewQueue(MigrationBatchId,CustomerCode,Priority,Severity,IssueType,EntityType,SourceDatabaseName,SourceTableName,SourceId,OriginalValue,SuggestedAction,Status)
SELECT @MigrationBatchId,@CustomerCode,1,N'Warning',N'CashIssueRequiresManualReview',N'Issue',SourceDatabaseName,SourceTableName,SourceId,CAST(MoneyAmount AS nvarchar(50)),N'Review expense/source account and owner payment semantics before migration',N'Open'
FROM dbo.PropertyMigrationSourceIssue s
WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationReviewQueue q WHERE q.MigrationBatchId=@MigrationBatchId AND q.EntityType=N'Issue' AND q.SourceId=s.SourceId);

INSERT INTO dbo.PropertyMigrationExcludedRecord(MigrationBatchId,CustomerCode,EntityType,SourceDatabaseName,SourceTableName,SourceId,Severity,ExclusionReason,Decision,SuggestedAction)
SELECT @MigrationBatchId,@CustomerCode,N'Issue',SourceDatabaseName,SourceTableName,SourceId,N'Warning',N'Cash issue migration is manual-review by default',N'ExcludedUntilApproved',N'Approve safe issue scenario in customer config'
FROM dbo.PropertyMigrationSourceIssue s
WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationExcludedRecord e WHERE e.MigrationBatchId=@MigrationBatchId AND e.EntityType=N'Issue' AND e.SourceId=s.SourceId);

SELECT 'Issues' Stage, 0 InsertedIssues, COUNT(*) ReviewQueueItems FROM dbo.PropertyMigrationSourceIssue WHERE MigrationBatchId=@MigrationBatchId;
