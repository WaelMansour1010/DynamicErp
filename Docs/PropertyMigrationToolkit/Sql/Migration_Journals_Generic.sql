/* Generic voucher-linked journal migration from PropertyMigrationSourceJournal/Line staging. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier='$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
DECLARE @DefaultDepartmentId int=NULL,@DefaultBranchId int=NULL;
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceJournal','U') IS NULL OR OBJECT_ID(N'dbo.PropertyMigrationSourceJournalLine','U') IS NULL BEGIN RAISERROR('Journal staging tables are missing.',16,1); RETURN; END;
SELECT TOP 1 @DefaultDepartmentId=DefaultDepartmentId,@DefaultBranchId=DefaultPilotBranchId FROM dbo.PropertyMigrationConfig WHERE CustomerCode=@CustomerCode AND TargetCloneDatabaseName=DB_NAME() ORDER BY ConfigId DESC;

;WITH Totals AS (
    SELECT j.SourceId, SUM(ISNULL(l.Debit,0)) Debit, SUM(ISNULL(l.Credit,0)) Credit, SUM(CASE WHEN l.AccountId IS NULL THEN 1 ELSE 0 END) NullAccounts
    FROM dbo.PropertyMigrationSourceJournal j JOIN dbo.PropertyMigrationSourceJournalLine l ON l.MigrationBatchId=j.MigrationBatchId AND l.SourceJournalId=j.SourceId
    WHERE j.MigrationBatchId=@MigrationBatchId AND j.IsValid=1 AND l.IsValid=1
    GROUP BY j.SourceId
)
INSERT INTO dbo.PropertyMigrationError(MigrationBatchId,CustomerCode,Severity,Stage,StepName,IssueType,EntityType,SourceId,ErrorMessage)
SELECT @MigrationBatchId,@CustomerCode,N'Critical',N'Migration',N'Migration_Journals_Generic',
       CASE WHEN NullAccounts>0 THEN N'JournalLineAccountMissing' ELSE N'UnbalancedJournal' END,N'Journal',SourceId,
       N'Journal blocked: AccountId must be present and debit/credit must balance.'
FROM Totals WHERE NullAccounts>0 OR ABS(Debit-Credit)>0.01;

INSERT INTO dbo.PropertyMigrationExcludedRecord(MigrationBatchId,CustomerCode,EntityType,SourceDatabaseName,SourceTableName,SourceId,Severity,ExclusionReason,Decision,SuggestedAction)
SELECT @MigrationBatchId,@CustomerCode,N'Journal',j.SourceDatabaseName,j.SourceTableName,j.SourceId,N'Warning',N'Journal is not linked to migrated receipt/issue',N'ArchiveOrManualReview',N'Link to approved voucher before migration'
FROM dbo.PropertyMigrationSourceJournal j
LEFT JOIN dbo.PropertyMigrationEntityMap rm ON rm.MigrationBatchId=@MigrationBatchId AND rm.EntityType=N'Receipt' AND rm.SourceId=j.LinkedReceiptSourceId
LEFT JOIN dbo.PropertyMigrationEntityMap im ON im.MigrationBatchId=@MigrationBatchId AND im.EntityType=N'Issue' AND im.SourceId=j.LinkedIssueSourceId
WHERE j.MigrationBatchId=@MigrationBatchId AND j.IsValid=1 AND rm.TargetId IS NULL AND im.TargetId IS NULL
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationExcludedRecord e WHERE e.MigrationBatchId=@MigrationBatchId AND e.EntityType=N'Journal' AND e.SourceId=j.SourceId);

;WITH Totals AS (
    SELECT j.SourceId, SUM(ISNULL(l.Debit,0)) Debit, SUM(ISNULL(l.Credit,0)) Credit, SUM(CASE WHEN l.AccountId IS NULL THEN 1 ELSE 0 END) NullAccounts
    FROM dbo.PropertyMigrationSourceJournal j JOIN dbo.PropertyMigrationSourceJournalLine l ON l.MigrationBatchId=j.MigrationBatchId AND l.SourceJournalId=j.SourceId
    WHERE j.MigrationBatchId=@MigrationBatchId AND j.IsValid=1 AND l.IsValid=1
    GROUP BY j.SourceId
), ValidJournals AS (
    SELECT j.*, ISNULL(rm.TargetId,im.TargetId) LinkedTargetId
    FROM dbo.PropertyMigrationSourceJournal j
    JOIN Totals t ON t.SourceId=j.SourceId AND t.NullAccounts=0 AND ABS(t.Debit-t.Credit)<=0.01
    LEFT JOIN dbo.PropertyMigrationEntityMap rm ON rm.MigrationBatchId=@MigrationBatchId AND rm.EntityType=N'Receipt' AND rm.SourceId=j.LinkedReceiptSourceId
    LEFT JOIN dbo.PropertyMigrationEntityMap im ON im.MigrationBatchId=@MigrationBatchId AND im.EntityType=N'Issue' AND im.SourceId=j.LinkedIssueSourceId
    WHERE j.MigrationBatchId=@MigrationBatchId AND (rm.TargetId IS NOT NULL OR im.TargetId IS NOT NULL)
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Journal' AND m.SourceId=j.SourceId AND m.SourceTableName=j.SourceTableName)
)
SELECT * INTO #ValidJournals FROM ValidJournals;

DECLARE @Out TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(200),TargetId int);
MERGE dbo.JournalEntry AS tgt
USING #ValidJournals AS src ON 1=0
WHEN NOT MATCHED THEN INSERT(DocumentNumber,BranchId,Date,Notes,SourceId,IsActive,IsPosted,IsDeleted,DepartmentId,OriginalDocumentNumber,OriginalSerial,MigrationSource)
VALUES(ISNULL(src.DocumentNumber,N'MIG-JE-' + src.SourceId),ISNULL(src.BranchId,@DefaultBranchId),src.JournalDate,src.Notes,src.LinkedTargetId,1,1,0,ISNULL(src.DepartmentId,@DefaultDepartmentId),src.DocumentNumber,src.SourceId,N'PropertyMigration')
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id INTO @Out;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'JournalEntry',TargetId,N'Journal',N'Inserted',0,0,N'Generic voucher-linked journal migration'
FROM @Out;

INSERT INTO dbo.JournalEntryDetail(JournalEntryId,Debit,Credit,AccountId,Notes,IsPosted,IsDeleted,IsActive,DepartmentId)
SELECT jm.TargetId,l.Debit,l.Credit,l.AccountId,l.Notes,1,0,1,ISNULL(l.DepartmentId,@DefaultDepartmentId)
FROM dbo.PropertyMigrationSourceJournalLine l
JOIN dbo.PropertyMigrationEntityMap jm ON jm.MigrationBatchId=@MigrationBatchId AND jm.EntityType=N'Journal' AND jm.SourceId=l.SourceJournalId
WHERE l.MigrationBatchId=@MigrationBatchId AND l.IsValid=1
  AND NOT EXISTS(SELECT 1 FROM dbo.JournalEntryDetail d WHERE d.JournalEntryId=jm.TargetId AND d.AccountId=l.AccountId AND d.Debit=l.Debit AND d.Credit=l.Credit AND ISNULL(d.Notes,N'')=ISNULL(l.Notes,N''));

IF EXISTS(SELECT 1 FROM dbo.JournalEntryDetail WHERE AccountId IS NULL)
BEGIN RAISERROR('Accounting safety failed: JournalEntryDetail.AccountId NULL detected.',16,1); RETURN; END;
IF EXISTS(SELECT 1 FROM (SELECT je.Id FROM dbo.JournalEntry je JOIN dbo.JournalEntryDetail jd ON jd.JournalEntryId=je.Id AND ISNULL(jd.IsDeleted,0)=0 WHERE ISNULL(je.IsDeleted,0)=0 GROUP BY je.Id HAVING ABS(SUM(ISNULL(jd.Debit,0))-SUM(ISNULL(jd.Credit,0)))>0.01) q)
BEGIN RAISERROR('Accounting safety failed: unbalanced journal detected.',16,1); RETURN; END;

SELECT 'Journals' Stage, COUNT(*) InsertedJournals FROM @Out;
DROP TABLE #ValidJournals;
