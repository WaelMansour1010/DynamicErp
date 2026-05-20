/*
RSMDB Finance Account Review Pack - SELECT/export only plus optional approval table DDL draft.
Run on clone only: Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520.
Does not approve mappings and does not migrate accounting.
*/

DECLARE @BatchId uniqueidentifier = '1b5d8eb5-dd7e-4b63-8ddb-e7b00aad558b';
DECLARE @CustomerCode nvarchar(100) = N'RSMDB-STAGING';

IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN
    RAISERROR('Unsafe database. Finance review pack must run on a clone/staging database only.',16,1);
    RETURN;
END;

;WITH CandidateJournals AS (
    SELECT CONVERT(int, SourceId) Notes_ID
    FROM dbo.PropertyMigrationResolutionResult
    WHERE MigrationBatchId=@BatchId AND EntityType=N'Journal' AND ResolutionStatus=N'AccountMappedCandidate_FinanceReviewRequired'
), AccountImpact AS (
    SELECT d.Account_Code,
           CandidateJournalCount = COUNT(DISTINCT d.Notes_ID),
           LineCount = COUNT(*),
           TotalValue = SUM(ISNULL(d.Value,0)),
           SampleJournalReferences = STUFF((SELECT TOP 10 N',' + CONVERT(nvarchar(40), d2.Notes_ID)
                                            FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS d2
                                            JOIN CandidateJournals cj2 ON cj2.Notes_ID=d2.Notes_ID
                                            WHERE d2.Account_Code=d.Account_Code COLLATE DATABASE_DEFAULT
                                            GROUP BY d2.Notes_ID
                                            ORDER BY d2.Notes_ID
                                            FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'')
    FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS d
    JOIN CandidateJournals cj ON cj.Notes_ID=d.Notes_ID
    GROUP BY d.Account_Code
), Pack AS (
    SELECT ai.Account_Code SourceAccountCode,
           a.Account_Name SourceAccountName,
           a.Account_Serial SourceAccountSerial,
           ac.BestTargetAccountCode SuggestedTargetAccountSerial,
           ca.ArName SuggestedTargetAccountName,
           ac.SuggestedFamily,
           ac.Score Confidence,
           ac.Band ConfidenceBand,
           ai.CandidateJournalCount,
           ai.LineCount,
           ai.TotalValue,
           d.UsageCount,
           d.TotalDebit,
           d.TotalCredit,
           d.RelatedNoteTypes,
           ai.SampleJournalReferences,
           SuggestedDecision = CASE WHEN ac.Band IN (N'AutoApproved',N'HighConfidence') THEN N'Approve' WHEN ac.Band=N'NeedsFinanceReview' THEN N'Approve or Change Target' WHEN ac.Band=N'Blocked' THEN N'Change Target / Block / SuspenseApproved' ELSE N'NeedsMoreInfo' END,
           rnUsage = ROW_NUMBER() OVER(ORDER BY ai.CandidateJournalCount DESC, ai.TotalValue DESC),
           rnAmount = ROW_NUMBER() OVER(ORDER BY ai.TotalValue DESC, ai.CandidateJournalCount DESC)
    FROM AccountImpact ai
    LEFT JOIN RSMDB.dbo.ACCOUNTS a ON a.Account_Code=ai.Account_Code COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.PropertyMigrationAccountConfidence ac ON ac.MigrationBatchId=@BatchId AND ac.SourceAccountCode=ai.Account_Code COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.PropertyMigrationAccountDiscovery d ON d.MigrationBatchId=@BatchId AND d.SourceAccountCode=ai.Account_Code COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT=ac.BestTargetAccountCode COLLATE DATABASE_DEFAULT
)
SELECT 'Top100_ByJournalUsage' ResultSetName, *
FROM Pack
WHERE rnUsage <= 100
ORDER BY rnUsage;

;WITH CandidateJournals AS (
    SELECT CONVERT(int, SourceId) Notes_ID FROM dbo.PropertyMigrationResolutionResult WHERE MigrationBatchId=@BatchId AND EntityType=N'Journal' AND ResolutionStatus=N'AccountMappedCandidate_FinanceReviewRequired'
), AccountImpact AS (
    SELECT d.Account_Code, COUNT(DISTINCT d.Notes_ID) CandidateJournalCount, COUNT(*) LineCount, SUM(ISNULL(d.Value,0)) TotalValue
    FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS d JOIN CandidateJournals cj ON cj.Notes_ID=d.Notes_ID GROUP BY d.Account_Code
), Pack AS (
    SELECT ai.Account_Code SourceAccountCode, a.Account_Name SourceAccountName, ac.BestTargetAccountCode SuggestedTargetAccountSerial, ca.ArName SuggestedTargetAccountName, ac.SuggestedFamily, ac.Score Confidence, ac.Band ConfidenceBand, ai.CandidateJournalCount, ai.LineCount, ai.TotalValue,
           ROW_NUMBER() OVER(ORDER BY ai.TotalValue DESC, ai.CandidateJournalCount DESC) rnAmount
    FROM AccountImpact ai
    LEFT JOIN RSMDB.dbo.ACCOUNTS a ON a.Account_Code=ai.Account_Code COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.PropertyMigrationAccountConfidence ac ON ac.MigrationBatchId=@BatchId AND ac.SourceAccountCode=ai.Account_Code COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT=ac.BestTargetAccountCode COLLATE DATABASE_DEFAULT
)
SELECT 'Top100_ByAmount' ResultSetName, * FROM Pack WHERE rnAmount <= 100 ORDER BY rnAmount;

SELECT 'AccountsAffecting706Candidates' ResultSetName,
       rq.SourceAccountCode, rq.SourceAccountName, rq.SuggestedFamily, rq.SuggestedTargetAccountCode, rq.ConfidenceScore, rq.Status, rq.UsageSummary, rq.SuggestedAction
FROM dbo.PropertyMigrationAccountReviewQueue rq
WHERE rq.MigrationBatchId=@BatchId
ORDER BY rq.ConfidenceScore DESC, rq.Priority, rq.SourceAccountCode;

SELECT 'BlockedAccounts' ResultSetName,
       rq.SourceAccountCode, rq.SourceAccountName, rq.SuggestedFamily, rq.ConfidenceScore, rq.UsageSummary, rq.SuggestedAction
FROM dbo.PropertyMigrationAccountReviewQueue rq
WHERE rq.MigrationBatchId=@BatchId AND rq.Status=N'Blocked'
ORDER BY rq.ConfidenceScore DESC, rq.SourceAccountCode;

SELECT 'SuspenseCandidates' ResultSetName,
       su.SourceAccountCode, d.SourceAccountName, d.SuggestedFamily, su.Amount, su.Reason, su.Status
FROM dbo.PropertyMigrationSuspenseUsage su
LEFT JOIN dbo.PropertyMigrationAccountDiscovery d ON d.MigrationBatchId=su.MigrationBatchId AND d.SourceAccountCode=su.SourceAccountCode
WHERE su.MigrationBatchId=@BatchId
ORDER BY su.Amount DESC;

SELECT 'HighConfidenceMappings' ResultSetName,
       rq.SourceAccountCode, rq.SourceAccountName, rq.SuggestedFamily, rq.SuggestedTargetAccountCode, rq.ConfidenceScore, rq.UsageSummary
FROM dbo.PropertyMigrationAccountReviewQueue rq
WHERE rq.MigrationBatchId=@BatchId AND rq.Status=N'HighConfidenceReview'
ORDER BY rq.ConfidenceScore DESC;

SELECT 'ManualReviewMappings' ResultSetName,
       rq.SourceAccountCode, rq.SourceAccountName, rq.SuggestedFamily, rq.SuggestedTargetAccountCode, rq.ConfidenceScore, rq.UsageSummary, rq.SuggestedAction
FROM dbo.PropertyMigrationAccountReviewQueue rq
WHERE rq.MigrationBatchId=@BatchId AND rq.Status=N'FinanceReview'
ORDER BY rq.ConfidenceScore DESC, rq.SourceAccountCode;

;WITH CandidateJournals AS (
    SELECT CONVERT(int, SourceId) Notes_ID FROM dbo.PropertyMigrationResolutionResult WHERE MigrationBatchId=@BatchId AND EntityType=N'Journal' AND ResolutionStatus=N'AccountMappedCandidate_FinanceReviewRequired'
), AccountUsage AS (
    SELECT d.Account_Code, COUNT(DISTINCT d.Notes_ID) CandidateJournalCount, SUM(ISNULL(d.Value,0)) TotalValue
    FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS d JOIN CandidateJournals cj ON cj.Notes_ID=d.Notes_ID GROUP BY d.Account_Code
), Ranked AS (
    SELECT Account_Code, ROW_NUMBER() OVER(ORDER BY CandidateJournalCount DESC, TotalValue DESC) rn FROM AccountUsage
), JournalLines AS (
    SELECT d.Notes_ID, d.Account_Code, ISNULL(d.Value,0) Value FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS d JOIN CandidateJournals cj ON cj.Notes_ID=d.Notes_ID
), Sim AS (
    SELECT Scenario=N'Top50', jl.Notes_ID, MAX(CASE WHEN r.rn IS NULL OR r.rn>50 THEN 1 ELSE 0 END) HasUnapproved, SUM(jl.Value) TotalValue FROM JournalLines jl LEFT JOIN Ranked r ON r.Account_Code=jl.Account_Code GROUP BY jl.Notes_ID
    UNION ALL SELECT N'Top100', jl.Notes_ID, MAX(CASE WHEN r.rn IS NULL OR r.rn>100 THEN 1 ELSE 0 END), SUM(jl.Value) FROM JournalLines jl LEFT JOIN Ranked r ON r.Account_Code=jl.Account_Code GROUP BY jl.Notes_ID
    UNION ALL SELECT N'AllScore60Plus', jl.Notes_ID, MAX(CASE WHEN ac.Score<60 THEN 1 ELSE 0 END), SUM(jl.Value) FROM JournalLines jl LEFT JOIN dbo.PropertyMigrationAccountConfidence ac ON ac.MigrationBatchId=@BatchId AND ac.SourceAccountCode=jl.Account_Code COLLATE DATABASE_DEFAULT GROUP BY jl.Notes_ID
)
SELECT 'ImpactSimulation' ResultSetName, Scenario, SUM(CASE WHEN HasUnapproved=0 THEN 1 ELSE 0 END) ReadyForMigration, SUM(CASE WHEN HasUnapproved=1 THEN 1 ELSE 0 END) StillManualOrBlocked, SUM(CASE WHEN HasUnapproved=0 THEN TotalValue ELSE 0 END) ReadyValue
FROM Sim GROUP BY Scenario ORDER BY Scenario;
