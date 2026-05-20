/* Enterprise Diagnostics Generic - supports Strict/Tolerant/Hybrid modes. */
DECLARE @MigrationBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
DECLARE @MigrationMode nvarchar(30);
SELECT @MigrationMode = MigrationMode FROM dbo.PropertyMigrationBatch WHERE MigrationBatchId=@MigrationBatchId;
IF @MigrationMode IS NULL SELECT @MigrationMode = MigrationMode FROM dbo.PropertyMigrationConfig WHERE CustomerCode=@CustomerCode;
IF @MigrationMode IS NULL SET @MigrationMode=N'Hybrid';

SELECT 'DiagnosticsMode' Metric, @MigrationMode Value;

IF OBJECT_ID('tempdb..#PropertyMigrationDiagnosticChecks') IS NOT NULL DROP TABLE #PropertyMigrationDiagnosticChecks;
CREATE TABLE #PropertyMigrationDiagnosticChecks(
    IssueType nvarchar(100) NOT NULL,
    EntityType nvarchar(100) NOT NULL,
    CountValue int NOT NULL,
    Severity nvarchar(30) NOT NULL
);

INSERT INTO #PropertyMigrationDiagnosticChecks(IssueType,EntityType,CountValue,Severity)
SELECT 'ContractsWithoutUnit' IssueType, 'Contract' EntityType, COUNT(*) CountValue, 'Warning' Severity
FROM dbo.PropertyContract WHERE IsDeleted=0 AND (PropertyUnitId IS NULL OR PropertyUnitId=0)
UNION ALL SELECT 'ContractsWithoutTenant','Contract',COUNT(*),'Warning' FROM dbo.PropertyContract WHERE IsDeleted=0 AND (PropertyRenterId IS NULL OR PropertyRenterId=0)
UNION ALL SELECT 'BatchesWithoutContract','Installment',COUNT(*),'Critical' FROM dbo.PropertyContractBatch b LEFT JOIN dbo.PropertyContract c ON c.Id=b.MainDocId WHERE c.Id IS NULL
UNION ALL SELECT 'JournalLinesWithNullAccount','Journal',COUNT(*),'Critical' FROM dbo.JournalEntryDetail jd JOIN dbo.JournalEntry je ON je.Id=jd.JournalEntryId WHERE ISNULL(je.IsDeleted,0)=0 AND ISNULL(jd.IsDeleted,0)=0 AND jd.AccountId IS NULL
UNION ALL SELECT 'UnbalancedJournals','Journal',COUNT(*),'Critical' FROM (SELECT je.Id FROM dbo.JournalEntry je JOIN dbo.JournalEntryDetail jd ON jd.JournalEntryId=je.Id AND ISNULL(jd.IsDeleted,0)=0 WHERE ISNULL(je.IsDeleted,0)=0 GROUP BY je.Id HAVING ABS(SUM(ISNULL(jd.Debit,0))-SUM(ISNULL(jd.Credit,0)))>0.01) q;

SELECT * FROM #PropertyMigrationDiagnosticChecks;

INSERT INTO dbo.PropertyMigrationWarning(MigrationBatchId,CustomerCode,Severity,IssueType,EntityType,Message,RequiresManualReview,SuggestedAction)
SELECT @MigrationBatchId,@CustomerCode,Severity,IssueType,EntityType,
       IssueType + N' count=' + CAST(CountValue AS nvarchar(30)),
       CASE WHEN Severity='Critical' THEN 1 ELSE 0 END,
       CASE WHEN Severity='Critical' THEN N'Fix before migration/GoLive' ELSE N'AutoFix or review according to mode' END
FROM #PropertyMigrationDiagnosticChecks
WHERE CountValue>0 AND Severity<>'Critical';

INSERT INTO dbo.PropertyMigrationError(MigrationBatchId,CustomerCode,Severity,Stage,StepName,IssueType,EntityType,ErrorMessage)
SELECT @MigrationBatchId,@CustomerCode,Severity,N'Diagnostics',N'Diagnostics_Generic',IssueType,EntityType,IssueType + N' count=' + CAST(CountValue AS nvarchar(30))
FROM #PropertyMigrationDiagnosticChecks
WHERE CountValue>0 AND Severity='Critical';

SELECT * FROM #PropertyMigrationDiagnosticChecks;
