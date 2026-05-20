/*
RSMDB Account Mapping Intelligence - Clone/Staging only
Purpose: discover legacy VB6 accounts, suggest target ChartOfAccount mappings, classify families, and reduce accounting review blockers.
No source writes. No accounting postings. SQL Server 2012 compatible.
*/

DECLARE @BatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(100) = N'$(CustomerCode)';
DECLARE @SourceDatabaseName sysname = N'$(SourceDatabaseName)';
DECLARE @TargetDatabaseName sysname = N'$(TargetCloneDatabaseName)';

IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN
    RAISERROR('Unsafe target database for Account Mapping Intelligence.', 16, 1);
    RETURN;
END;

IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp')
BEGIN
    RAISERROR('Refusing to run Account Mapping Intelligence on source/reference/production database.', 16, 1);
    RETURN;
END;

IF @SourceDatabaseName <> N'RSMDB'
BEGIN
    RAISERROR('This account intelligence script is RSMDB-specific.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.PropertyMigrationAccountDiscovery', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationAccountDiscovery(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        SourceDatabaseName sysname NOT NULL,
        SourceAccountCode nvarchar(200) NOT NULL,
        SourceAccountName nvarchar(max) NULL,
        SourceAccountNameEn nvarchar(max) NULL,
        SourceParentAccountCode nvarchar(200) NULL,
        IsLeaf bit NULL,
        UsageCount int NOT NULL DEFAULT(0),
        DebitCount int NOT NULL DEFAULT(0),
        CreditCount int NOT NULL DEFAULT(0),
        TotalDebit money NOT NULL DEFAULT(0),
        TotalCredit money NOT NULL DEFAULT(0),
        RelatedNoteTypes nvarchar(max) NULL,
        SuggestedFamily nvarchar(120) NULL,
        PostingBehavior nvarchar(120) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationAccountFamily', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationAccountFamily(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        SourceAccountCode nvarchar(200) NOT NULL,
        SuggestedFamily nvarchar(120) NOT NULL,
        FamilyConfidence int NOT NULL,
        Evidence nvarchar(max) NULL,
        SuggestedParentTargetAccountId int NULL,
        SuggestedParentTargetAccountCode nvarchar(200) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationAccountMatchCandidate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationAccountMatchCandidate(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        SourceAccountCode nvarchar(200) NOT NULL,
        SourceAccountName nvarchar(max) NULL,
        TargetAccountId int NULL,
        TargetAccountCode nvarchar(200) NULL,
        TargetAccountName nvarchar(max) NULL,
        MatchType nvarchar(120) NOT NULL,
        SuggestedFamily nvarchar(120) NULL,
        ConfidenceScore int NOT NULL,
        ConfidenceBand nvarchar(60) NOT NULL,
        SuggestedAction nvarchar(max) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationAccountConfidence', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationAccountConfidence(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        SourceAccountCode nvarchar(200) NOT NULL,
        BestTargetAccountId int NULL,
        BestTargetAccountCode nvarchar(200) NULL,
        Score int NOT NULL,
        Band nvarchar(60) NOT NULL,
        SuggestedFamily nvarchar(120) NULL,
        AutoApproved bit NOT NULL DEFAULT(0),
        RequiresFinanceReview bit NOT NULL DEFAULT(1),
        BlockedReason nvarchar(max) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationAccountReviewQueue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationAccountReviewQueue(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        Priority int NOT NULL,
        Severity nvarchar(60) NOT NULL,
        SourceAccountCode nvarchar(200) NOT NULL,
        SourceAccountName nvarchar(max) NULL,
        SuggestedFamily nvarchar(120) NULL,
        SuggestedTargetAccountId int NULL,
        SuggestedTargetAccountCode nvarchar(200) NULL,
        ConfidenceScore int NOT NULL,
        IssueType nvarchar(200) NOT NULL,
        UsageSummary nvarchar(max) NULL,
        SuggestedAction nvarchar(max) NULL,
        Status nvarchar(100) NOT NULL DEFAULT(N'Open'),
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationAccountResolution', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationAccountResolution(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        SourceAccountCode nvarchar(200) NOT NULL,
        TargetAccountId int NULL,
        TargetAccountCode nvarchar(200) NULL,
        ResolutionStatus nvarchar(100) NOT NULL,
        ConfidenceScore int NOT NULL,
        ApprovedBy nvarchar(200) NULL,
        ApprovedAt datetime NULL,
        Notes nvarchar(max) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationSuspenseUsage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationSuspenseUsage(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        SourceAccountCode nvarchar(200) NOT NULL,
        SourceEntityType nvarchar(120) NULL,
        SourceId nvarchar(400) NULL,
        Amount money NULL,
        Reason nvarchar(max) NOT NULL,
        RequiresFinanceSignOff bit NOT NULL DEFAULT(1),
        Status nvarchar(100) NOT NULL DEFAULT(N'Open'),
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

DELETE FROM dbo.PropertyMigrationAccountDiscovery WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationAccountFamily WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationAccountMatchCandidate WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationAccountConfidence WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationAccountReviewQueue WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationAccountResolution WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationSuspenseUsage WHERE MigrationBatchId=@BatchId;

IF OBJECT_ID('tempdb..#UsedAccounts') IS NOT NULL DROP TABLE #UsedAccounts;
CREATE TABLE #UsedAccounts(
    AccountCode nvarchar(200) NOT NULL,
    UsageCount int NOT NULL,
    DebitCount int NOT NULL,
    CreditCount int NOT NULL,
    TotalDebit money NOT NULL,
    TotalCredit money NOT NULL
);

INSERT INTO #UsedAccounts(AccountCode, UsageCount, DebitCount, CreditCount, TotalDebit, TotalCredit)
SELECT
    d.Account_Code,
    COUNT(*),
    SUM(CASE WHEN d.Credit_Or_Debit=0 THEN 1 ELSE 0 END),
    SUM(CASE WHEN d.Credit_Or_Debit=1 THEN 1 ELSE 0 END),
    SUM(CASE WHEN d.Credit_Or_Debit=0 THEN ISNULL(d.Value,0) ELSE 0 END),
    SUM(CASE WHEN d.Credit_Or_Debit=1 THEN ISNULL(d.Value,0) ELSE 0 END)
FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS d
WHERE NULLIF(LTRIM(RTRIM(ISNULL(d.Account_Code,''))), '') IS NOT NULL
  AND EXISTS (SELECT 1 FROM dbo.PropertyMigrationSourceJournal j WHERE j.MigrationBatchId=@BatchId AND ISNUMERIC(j.SourceId)=1 AND CONVERT(int,j.SourceId)=d.Notes_ID)
GROUP BY d.Account_Code;

/* Include account codes found on property notes even if not in the currently staged journal subset. */
INSERT INTO #UsedAccounts(AccountCode, UsageCount, DebitCount, CreditCount, TotalDebit, TotalCredit)
SELECT x.AccountCode, COUNT(*), 0, 0, 0, 0
FROM (
    SELECT AccountPaym AccountCode FROM RSMDB.dbo.Notes WHERE NULLIF(LTRIM(RTRIM(ISNULL(AccountPaym,''))), '') IS NOT NULL
    UNION ALL SELECT DebitSide FROM RSMDB.dbo.Notes WHERE NULLIF(LTRIM(RTRIM(ISNULL(DebitSide,''))), '') IS NOT NULL
    UNION ALL SELECT CreditSide FROM RSMDB.dbo.Notes WHERE NULLIF(LTRIM(RTRIM(ISNULL(CreditSide,''))), '') IS NOT NULL
    UNION ALL SELECT Account_Code1 FROM RSMDB.dbo.Notes WHERE NULLIF(LTRIM(RTRIM(ISNULL(Account_Code1,''))), '') IS NOT NULL
    UNION ALL SELECT Account_Code2 FROM RSMDB.dbo.Notes WHERE NULLIF(LTRIM(RTRIM(ISNULL(Account_Code2,''))), '') IS NOT NULL
    UNION ALL SELECT AccountsCode FROM RSMDB.dbo.Notes WHERE NULLIF(LTRIM(RTRIM(ISNULL(AccountsCode,''))), '') IS NOT NULL
) x
WHERE NOT EXISTS (SELECT 1 FROM #UsedAccounts u WHERE u.AccountCode = x.AccountCode COLLATE DATABASE_DEFAULT)
GROUP BY x.AccountCode;

INSERT INTO dbo.PropertyMigrationAccountDiscovery(
    MigrationBatchId, CustomerCode, SourceDatabaseName, SourceAccountCode, SourceAccountName, SourceAccountNameEn,
    SourceParentAccountCode, IsLeaf, UsageCount, DebitCount, CreditCount, TotalDebit, TotalCredit, RelatedNoteTypes,
    SuggestedFamily, PostingBehavior)
SELECT
    @BatchId, @CustomerCode, @SourceDatabaseName, u.AccountCode,
    a.Account_Name, a.Account_NameEng, a.Parent_Account_Code, a.last_account,
    u.UsageCount, u.DebitCount, u.CreditCount, u.TotalDebit, u.TotalCredit,
    RelatedNoteTypes = STUFF((SELECT DISTINCT N',' + CONVERT(nvarchar(20), n.NoteType)
                              FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS d2
                              JOIN RSMDB.dbo.Notes n ON n.NoteID=d2.Notes_ID
                              WHERE d2.Account_Code = u.AccountCode COLLATE DATABASE_DEFAULT
                              FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N''),
    SuggestedFamily = CASE
        WHEN ISNULL(a.Account_Name,N'') LIKE N'%صندوق%' OR ISNULL(a.Account_Name,N'') LIKE N'%نقد%' THEN N'Cash'
        WHEN ISNULL(a.Account_Name,N'') LIKE N'%بنك%' OR ISNULL(a.Account_Name,N'') LIKE N'%مصرف%' THEN N'Banks'
        WHEN ISNULL(a.Account_Name,N'') LIKE N'%مستأجر%' OR ISNULL(a.Account_Name,N'') LIKE N'%عميل%' OR ISNULL(a.Account_Name,N'') LIKE N'%عملاء%' THEN N'RenterReceivable'
        WHEN ISNULL(a.Account_Name,N'') LIKE N'%مالك%' OR ISNULL(a.Account_Name,N'') LIKE N'%ملاك%' THEN N'OwnerPayable'
        WHEN ISNULL(a.Account_Name,N'') LIKE N'%ايراد%' OR ISNULL(a.Account_Name,N'') LIKE N'%إيراد%' OR ISNULL(a.Account_Name,N'') LIKE N'%ايجار%' OR ISNULL(a.Account_Name,N'') LIKE N'%إيجار%' THEN N'Revenue'
        WHEN ISNULL(a.Account_Name,N'') LIKE N'%مصروف%' OR ISNULL(a.Account_Name,N'') LIKE N'%صيانة%' OR ISNULL(a.Account_Name,N'') LIKE N'%كهرب%' OR ISNULL(a.Account_Name,N'') LIKE N'%مياه%' THEN N'Expense'
        WHEN ISNULL(a.Account_Name,N'') LIKE N'%ضريبة%' OR ISNULL(a.Account_Name,N'') LIKE N'%قيمة مضافة%' OR ISNULL(a.Account_Name,N'') LIKE N'%VAT%' THEN N'VAT'
        WHEN u.DebitCount > u.CreditCount * 3 THEN N'Receivables'
        WHEN u.CreditCount > u.DebitCount * 3 THEN N'Payables'
        ELSE N'Unknown'
    END,
    PostingBehavior = CASE WHEN u.DebitCount > u.CreditCount * 2 THEN N'DebitDominant' WHEN u.CreditCount > u.DebitCount * 2 THEN N'CreditDominant' ELSE N'Mixed' END
FROM #UsedAccounts u
LEFT JOIN RSMDB.dbo.ACCOUNTS a ON a.Account_Code = u.AccountCode COLLATE DATABASE_DEFAULT;

/* Suggested target parent/leaf by family, selected by names/codes not hardcoded ids. */
INSERT INTO dbo.PropertyMigrationAccountFamily(MigrationBatchId, CustomerCode, SourceAccountCode, SuggestedFamily, FamilyConfidence, Evidence, SuggestedParentTargetAccountId, SuggestedParentTargetAccountCode)
SELECT d.MigrationBatchId, d.CustomerCode, d.SourceAccountCode, d.SuggestedFamily,
       CASE WHEN d.SuggestedFamily='Unknown' THEN 25 WHEN d.SourceAccountName IS NULL THEN 45 ELSE 75 END,
       N'Name=' + ISNULL(CONVERT(nvarchar(max),d.SourceAccountName),N'') + N'; Usage=' + CONVERT(nvarchar(20),d.UsageCount) + N'; Behavior=' + d.PostingBehavior,
       ca.Id, ca.Code
FROM dbo.PropertyMigrationAccountDiscovery d
OUTER APPLY (
    SELECT TOP 1 c.Id, c.Code
    FROM dbo.ChartOfAccount c
    WHERE ISNULL(c.IsDeleted,0)=0 AND ISNULL(c.IsActive,1)=1
      AND (
        (d.SuggestedFamily=N'Cash' AND (c.ArName LIKE N'%صندوق%' OR c.Code LIKE N'110101%')) OR
        (d.SuggestedFamily=N'Banks' AND (c.ArName LIKE N'%بنك%' OR c.Code LIKE N'110102%')) OR
        (d.SuggestedFamily IN (N'RenterReceivable',N'Receivables') AND (c.ArName LIKE N'%مستأجر%' OR c.ArName LIKE N'%عملاء%' OR c.Code LIKE N'1105%' OR c.Code LIKE N'1102%')) OR
        (d.SuggestedFamily=N'OwnerPayable' AND (c.ArName LIKE N'%ملاك%' OR c.ArName LIKE N'%دائن%' OR c.Code LIKE N'2%')) OR
        (d.SuggestedFamily=N'Revenue' AND (c.ArName LIKE N'%ايراد%' OR c.ArName LIKE N'%إيراد%' OR c.Code LIKE N'4%')) OR
        (d.SuggestedFamily=N'Expense' AND (c.ArName LIKE N'%مصروف%' OR c.ArName LIKE N'%صيانة%' OR c.Code LIKE N'3%')) OR
        (d.SuggestedFamily=N'VAT' AND (c.ArName LIKE N'%ضريبة%' OR c.ArName LIKE N'%قيمة مضافة%'))
      )
    ORDER BY LEN(c.Code) DESC, c.Id
) ca
WHERE d.MigrationBatchId=@BatchId;

/* Candidate 1: exact code. */
INSERT INTO dbo.PropertyMigrationAccountMatchCandidate(MigrationBatchId, CustomerCode, SourceAccountCode, SourceAccountName, TargetAccountId, TargetAccountCode, TargetAccountName, MatchType, SuggestedFamily, ConfidenceScore, ConfidenceBand, SuggestedAction)
SELECT d.MigrationBatchId, d.CustomerCode, d.SourceAccountCode, d.SourceAccountName, ca.Id, ca.Code, ca.ArName,
       N'ExactCode', d.SuggestedFamily, 100, N'AutoApproved', N'Exact account code match; approve unless finance rejects.'
FROM dbo.PropertyMigrationAccountDiscovery d
JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT = d.SourceAccountCode COLLATE DATABASE_DEFAULT AND ISNULL(ca.IsDeleted,0)=0
WHERE d.MigrationBatchId=@BatchId;

/* Candidate 2: exact/contains Arabic name. */
INSERT INTO dbo.PropertyMigrationAccountMatchCandidate(MigrationBatchId, CustomerCode, SourceAccountCode, SourceAccountName, TargetAccountId, TargetAccountCode, TargetAccountName, MatchType, SuggestedFamily, ConfidenceScore, ConfidenceBand, SuggestedAction)
SELECT d.MigrationBatchId, d.CustomerCode, d.SourceAccountCode, d.SourceAccountName, ca.Id, ca.Code, ca.ArName,
       CASE WHEN LTRIM(RTRIM(ISNULL(ca.ArName,N''))) = LTRIM(RTRIM(ISNULL(CONVERT(nvarchar(max),d.SourceAccountName),N''))) THEN N'ExactArabicName' ELSE N'ArabicNameContains' END,
       d.SuggestedFamily,
       CASE WHEN LTRIM(RTRIM(ISNULL(ca.ArName,N''))) = LTRIM(RTRIM(ISNULL(CONVERT(nvarchar(max),d.SourceAccountName),N''))) THEN 90 ELSE 72 END,
       CASE WHEN LTRIM(RTRIM(ISNULL(ca.ArName,N''))) = LTRIM(RTRIM(ISNULL(CONVERT(nvarchar(max),d.SourceAccountName),N''))) THEN N'HighConfidence' ELSE N'NeedsFinanceReview' END,
       N'Review name-based match; code systems differ between VB6 and DynamicErp.'
FROM dbo.PropertyMigrationAccountDiscovery d
JOIN dbo.ChartOfAccount ca ON ISNULL(ca.IsDeleted,0)=0 AND ISNULL(ca.IsActive,1)=1
WHERE d.MigrationBatchId=@BatchId
  AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max),d.SourceAccountName))), N'') IS NOT NULL
  AND (
      LTRIM(RTRIM(ISNULL(ca.ArName,N''))) = LTRIM(RTRIM(CONVERT(nvarchar(max),d.SourceAccountName)))
      OR (LEN(LTRIM(RTRIM(CONVERT(nvarchar(max),d.SourceAccountName)))) >= 5 AND ca.ArName LIKE N'%' + LTRIM(RTRIM(CONVERT(nvarchar(max),d.SourceAccountName))) + N'%')
      OR (LEN(LTRIM(RTRIM(ISNULL(ca.ArName,N'')))) >= 5 AND CONVERT(nvarchar(max),d.SourceAccountName) LIKE N'%' + LTRIM(RTRIM(ca.ArName)) + N'%')
  );

/* Candidate 3: family target suggestion. */
INSERT INTO dbo.PropertyMigrationAccountMatchCandidate(MigrationBatchId, CustomerCode, SourceAccountCode, SourceAccountName, TargetAccountId, TargetAccountCode, TargetAccountName, MatchType, SuggestedFamily, ConfidenceScore, ConfidenceBand, SuggestedAction)
SELECT d.MigrationBatchId, d.CustomerCode, d.SourceAccountCode, d.SourceAccountName, f.SuggestedParentTargetAccountId, f.SuggestedParentTargetAccountCode, ca.ArName,
       N'FamilyParentSuggestion', d.SuggestedFamily,
       CASE WHEN d.SuggestedFamily=N'Unknown' OR f.SuggestedParentTargetAccountId IS NULL THEN 25 ELSE 65 END,
       CASE WHEN d.SuggestedFamily=N'Unknown' OR f.SuggestedParentTargetAccountId IS NULL THEN N'Blocked' ELSE N'NeedsFinanceReview' END,
       CASE WHEN f.SuggestedParentTargetAccountId IS NULL THEN N'No target family account found; configure ChartOfAccount parent or suspense with sign-off.' ELSE N'Use as parent/family suggestion only; create/map leaf account after finance approval.' END
FROM dbo.PropertyMigrationAccountDiscovery d
JOIN dbo.PropertyMigrationAccountFamily f ON f.MigrationBatchId=d.MigrationBatchId AND f.SourceAccountCode=d.SourceAccountCode
LEFT JOIN dbo.ChartOfAccount ca ON ca.Id=f.SuggestedParentTargetAccountId
WHERE d.MigrationBatchId=@BatchId;

/* Best confidence per source account. */
;WITH Ranked AS (
    SELECT mc.*, rn = ROW_NUMBER() OVER (PARTITION BY mc.SourceAccountCode ORDER BY mc.ConfidenceScore DESC, mc.Id ASC)
    FROM dbo.PropertyMigrationAccountMatchCandidate mc
    WHERE mc.MigrationBatchId=@BatchId
)
INSERT INTO dbo.PropertyMigrationAccountConfidence(MigrationBatchId, CustomerCode, SourceAccountCode, BestTargetAccountId, BestTargetAccountCode, Score, Band, SuggestedFamily, AutoApproved, RequiresFinanceReview, BlockedReason)
SELECT d.MigrationBatchId, d.CustomerCode, d.SourceAccountCode,
       r.TargetAccountId, r.TargetAccountCode,
       ISNULL(r.ConfidenceScore, 20),
       CASE WHEN ISNULL(r.ConfidenceScore,20) >= 95 THEN N'AutoApproved' WHEN ISNULL(r.ConfidenceScore,20) >= 80 THEN N'HighConfidence' WHEN ISNULL(r.ConfidenceScore,20) >= 60 THEN N'NeedsFinanceReview' WHEN ISNULL(r.ConfidenceScore,20) >= 40 THEN N'WeakMatch' ELSE N'Blocked' END,
       d.SuggestedFamily,
       CASE WHEN ISNULL(r.ConfidenceScore,20) >= 95 THEN 1 ELSE 0 END,
       CASE WHEN ISNULL(r.ConfidenceScore,20) >= 95 THEN 0 ELSE 1 END,
       CASE WHEN r.Id IS NULL THEN N'No candidate target account.' WHEN r.ConfidenceScore < 40 THEN N'Low confidence or unknown family.' ELSE NULL END
FROM dbo.PropertyMigrationAccountDiscovery d
LEFT JOIN Ranked r ON r.SourceAccountCode=d.SourceAccountCode AND r.rn=1
WHERE d.MigrationBatchId=@BatchId;

INSERT INTO dbo.PropertyMigrationAccountReviewQueue(MigrationBatchId, CustomerCode, Priority, Severity, SourceAccountCode, SourceAccountName, SuggestedFamily, SuggestedTargetAccountId, SuggestedTargetAccountCode, ConfidenceScore, IssueType, UsageSummary, SuggestedAction, Status)
SELECT c.MigrationBatchId, c.CustomerCode,
       CASE WHEN c.Band IN (N'Blocked',N'WeakMatch') THEN 1 WHEN c.Band=N'NeedsFinanceReview' THEN 2 ELSE 3 END,
       CASE WHEN c.Band=N'Blocked' THEN N'Critical' ELSE N'Warning' END,
       c.SourceAccountCode, d.SourceAccountName, c.SuggestedFamily, c.BestTargetAccountId, c.BestTargetAccountCode, c.Score,
       CASE WHEN c.Band=N'AutoApproved' THEN N'AutoApprovedAccountMapping' WHEN c.Band=N'HighConfidence' THEN N'HighConfidenceAccountMapping' WHEN c.Band=N'NeedsFinanceReview' THEN N'FinanceReviewAccountMapping' ELSE N'BlockedAccountMapping' END,
       N'Usage=' + CONVERT(nvarchar(20), d.UsageCount) + N'; DebitCount=' + CONVERT(nvarchar(20), d.DebitCount) + N'; CreditCount=' + CONVERT(nvarchar(20), d.CreditCount) + N'; NoteTypes=' + ISNULL(d.RelatedNoteTypes,N''),
       CASE WHEN c.Band=N'AutoApproved' THEN N'Review sample then allow account mapping.' WHEN c.Band=N'NeedsFinanceReview' THEN N'Finance must approve suggested family/target parent before posting.' ELSE N'Create explicit mapping or suspense account with finance sign-off.' END,
       CASE WHEN c.Band=N'AutoApproved' THEN N'AutoApprovedSuggested' WHEN c.Band=N'HighConfidence' THEN N'HighConfidenceReview' WHEN c.Band=N'NeedsFinanceReview' THEN N'FinanceReview' ELSE N'Blocked' END
FROM dbo.PropertyMigrationAccountConfidence c
JOIN dbo.PropertyMigrationAccountDiscovery d ON d.MigrationBatchId=c.MigrationBatchId AND d.SourceAccountCode=c.SourceAccountCode
WHERE c.MigrationBatchId=@BatchId;

/* Suspense candidates: source accounts with journal usage but no reliable mapping. No suspense is applied automatically. */
INSERT INTO dbo.PropertyMigrationSuspenseUsage(MigrationBatchId, CustomerCode, SourceAccountCode, SourceEntityType, SourceId, Amount, Reason, RequiresFinanceSignOff, Status)
SELECT @BatchId, @CustomerCode, c.SourceAccountCode, N'Account', c.SourceAccountCode,
       CASE WHEN ABS(d.TotalDebit) > ABS(d.TotalCredit) THEN d.TotalDebit ELSE d.TotalCredit END,
       N'Account mapping confidence is ' + c.Band + N'. Suspense/holding may be considered only with explicit finance sign-off and tracking.',
       1, N'CandidateOnly'
FROM dbo.PropertyMigrationAccountConfidence c
JOIN dbo.PropertyMigrationAccountDiscovery d ON d.MigrationBatchId=c.MigrationBatchId AND d.SourceAccountCode=c.SourceAccountCode
WHERE c.MigrationBatchId=@BatchId AND c.Band IN (N'Blocked',N'WeakMatch',N'NeedsFinanceReview');

/* Update journal review statuses when every source account in a balanced journal has at least NeedsFinanceReview mapping; still not migration-approved. */
IF OBJECT_ID('tempdb..#JournalAccountStatus') IS NOT NULL DROP TABLE #JournalAccountStatus;
SELECT d.Notes_ID,
       LineAccounts = COUNT(DISTINCT d.Account_Code),
       MappedOrReviewAccounts = COUNT(DISTINCT CASE WHEN c.Score >= 60 THEN d.Account_Code END),
       AutoAccounts = COUNT(DISTINCT CASE WHEN c.Score >= 95 THEN d.Account_Code END),
       BlockedAccounts = COUNT(DISTINCT CASE WHEN ISNULL(c.Score,0) < 60 THEN d.Account_Code END)
INTO #JournalAccountStatus
FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS d
LEFT JOIN dbo.PropertyMigrationAccountConfidence c ON c.MigrationBatchId=@BatchId AND c.SourceAccountCode = d.Account_Code COLLATE DATABASE_DEFAULT
WHERE NULLIF(LTRIM(RTRIM(ISNULL(d.Account_Code,''))), '') IS NOT NULL
GROUP BY d.Notes_ID;

UPDATE rr
SET ResolutionStatus = CASE WHEN jas.BlockedAccounts=0 THEN N'AccountMappedCandidate_FinanceReviewRequired' ELSE rr.ResolutionStatus END,
    ConfidenceScore = CASE WHEN jas.BlockedAccounts=0 AND rr.ConfidenceScore < 84 THEN 84 ELSE rr.ConfidenceScore END,
    Evidence = ISNULL(rr.Evidence,N'') + N'; AccountMappingIntelligence LineAccounts=' + CONVERT(nvarchar(20), jas.LineAccounts) + N'; MappedOrReview=' + CONVERT(nvarchar(20), jas.MappedOrReviewAccounts) + N'; BlockedAccounts=' + CONVERT(nvarchar(20), jas.BlockedAccounts)
FROM dbo.PropertyMigrationResolutionResult rr
JOIN #JournalAccountStatus jas ON CONVERT(nvarchar(400), jas.Notes_ID) = rr.SourceId COLLATE DATABASE_DEFAULT
WHERE rr.MigrationBatchId=@BatchId
  AND rr.EntityType=N'Journal'
  AND rr.ResolutionStatus=N'HighConfidenceSourceBalanced_TargetAccountMappingRequired'
  AND jas.LineAccounts=jas.MappedOrReviewAccounts;

SELECT Metric, CountValue
FROM (
    SELECT N'AccountsDiscovered' Metric, COUNT(*) CountValue FROM dbo.PropertyMigrationAccountDiscovery WHERE MigrationBatchId=@BatchId
    UNION ALL SELECT N'ExactCodeMatches', COUNT(*) FROM dbo.PropertyMigrationAccountMatchCandidate WHERE MigrationBatchId=@BatchId AND MatchType=N'ExactCode'
    UNION ALL SELECT N'AutoApproved', COUNT(*) FROM dbo.PropertyMigrationAccountConfidence WHERE MigrationBatchId=@BatchId AND Band=N'AutoApproved'
    UNION ALL SELECT N'HighConfidence', COUNT(*) FROM dbo.PropertyMigrationAccountConfidence WHERE MigrationBatchId=@BatchId AND Band=N'HighConfidence'
    UNION ALL SELECT N'NeedsFinanceReview', COUNT(*) FROM dbo.PropertyMigrationAccountConfidence WHERE MigrationBatchId=@BatchId AND Band=N'NeedsFinanceReview'
    UNION ALL SELECT N'WeakMatch', COUNT(*) FROM dbo.PropertyMigrationAccountConfidence WHERE MigrationBatchId=@BatchId AND Band=N'WeakMatch'
    UNION ALL SELECT N'Blocked', COUNT(*) FROM dbo.PropertyMigrationAccountConfidence WHERE MigrationBatchId=@BatchId AND Band=N'Blocked'
    UNION ALL SELECT N'SuspenseCandidates', COUNT(*) FROM dbo.PropertyMigrationSuspenseUsage WHERE MigrationBatchId=@BatchId
    UNION ALL SELECT N'JournalAccountMappedCandidates', COUNT(*) FROM dbo.PropertyMigrationResolutionResult WHERE MigrationBatchId=@BatchId AND EntityType=N'Journal' AND ResolutionStatus=N'AccountMappedCandidate_FinanceReviewRequired'
) s
ORDER BY Metric;
