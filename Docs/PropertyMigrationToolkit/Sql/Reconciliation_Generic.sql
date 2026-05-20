/* Enterprise Reconciliation Framework. */
DECLARE @MigrationBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
SELECT 'Counts' Section, EntityType, COUNT(*) CountValue FROM dbo.PropertyMigrationEntityMap WHERE MigrationBatchId=@MigrationBatchId GROUP BY EntityType;
SELECT 'Exceptions' Section,
 (SELECT COUNT(*) FROM dbo.PropertyMigrationWarning WHERE MigrationBatchId=@MigrationBatchId AND IsResolved=0) OpenWarnings,
 (SELECT COUNT(*) FROM dbo.PropertyMigrationError WHERE MigrationBatchId=@MigrationBatchId) Errors,
 (SELECT COUNT(*) FROM dbo.PropertyMigrationAutoFix WHERE MigrationBatchId=@MigrationBatchId) AutoFixes,
 (SELECT COUNT(*) FROM dbo.PropertyMigrationReviewQueue WHERE MigrationBatchId=@MigrationBatchId AND Status<>N'Closed') OpenReviewItems,
 (SELECT COUNT(*) FROM dbo.PropertyMigrationSuspenseMapping WHERE MigrationBatchId=@MigrationBatchId AND Status<>N'Closed') OpenSuspenseItems;
SELECT 'JournalRisk' Section,
       (SELECT COUNT(*) FROM dbo.JournalEntryDetail jd JOIN dbo.JournalEntry je ON je.Id=jd.JournalEntryId WHERE ISNULL(je.IsDeleted,0)=0 AND ISNULL(jd.IsDeleted,0)=0 AND jd.AccountId IS NULL) NullAccountLines,
       (SELECT COUNT(*) FROM (SELECT je.Id FROM dbo.JournalEntry je JOIN dbo.JournalEntryDetail jd ON jd.JournalEntryId=je.Id AND ISNULL(jd.IsDeleted,0)=0 WHERE ISNULL(je.IsDeleted,0)=0 GROUP BY je.Id HAVING ABS(SUM(ISNULL(jd.Debit,0))-SUM(ISNULL(jd.Credit,0)))>0.01) q) UnbalancedJournals;
