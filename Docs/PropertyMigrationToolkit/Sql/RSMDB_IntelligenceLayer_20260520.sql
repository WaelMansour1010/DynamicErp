/*
RSMDB Intelligence Layer - Clone/Staging only
Purpose: enrich PropertyMigrationSource* staging with match candidates, confidence scores, and review reduction recommendations.
Safety: no migration into DynamicErp production tables; no source DB writes; no accounting entries created.
SQL Server 2012 compatible.
*/

DECLARE @BatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(100) = N'$(CustomerCode)';
DECLARE @SourceDatabaseName sysname = N'$(SourceDatabaseName)';
DECLARE @TargetDatabaseName sysname = N'$(TargetCloneDatabaseName)';
DECLARE @MigrationMode nvarchar(50) = N'$(MigrationMode)';

IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN
    RAISERROR('Unsafe target database for Property Migration Intelligence. Target must be a clone/sandbox/property pilot database.', 16, 1);
    RETURN;
END;

IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp')
BEGIN
    RAISERROR('Refusing to run Intelligence Layer on source/reference/production database.', 16, 1);
    RETURN;
END;

IF @SourceDatabaseName <> N'RSMDB'
BEGIN
    RAISERROR('This script is RSMDB-specific. Use a customer-specific intelligence script or generic adapter for other sources.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.PropertyMigrationMatchCandidate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationMatchCandidate(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        SourceDatabaseName sysname NOT NULL,
        EntityType nvarchar(100) NOT NULL,
        SourceTableName nvarchar(256) NULL,
        SourceId nvarchar(400) NOT NULL,
        CandidateTableName nvarchar(256) NULL,
        CandidateId nvarchar(400) NULL,
        CandidateEntityType nvarchar(100) NULL,
        MatchStrategy nvarchar(200) NOT NULL,
        Signals nvarchar(max) NULL,
        ConfidenceScore int NOT NULL,
        ConfidenceBand nvarchar(60) NOT NULL,
        ResolutionStatus nvarchar(100) NOT NULL,
        SuggestedAction nvarchar(max) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationConfidenceScore', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationConfidenceScore(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        EntityType nvarchar(100) NOT NULL,
        SourceId nvarchar(400) NOT NULL,
        Score int NOT NULL,
        Band nvarchar(60) NOT NULL,
        ScoreReason nvarchar(max) NULL,
        IsAccountingCritical bit NOT NULL DEFAULT(0),
        AllowsAutoApproval bit NOT NULL DEFAULT(0),
        RequiresManualReview bit NOT NULL DEFAULT(1),
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationResolutionResult', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationResolutionResult(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        EntityType nvarchar(100) NOT NULL,
        SourceId nvarchar(400) NOT NULL,
        ResolutionType nvarchar(120) NOT NULL,
        ResolutionStatus nvarchar(100) NOT NULL,
        ConfidenceScore int NOT NULL,
        Evidence nvarchar(max) NULL,
        CriticalBlocker nvarchar(max) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationClassificationResult', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationClassificationResult(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        EntityType nvarchar(100) NOT NULL,
        SourceTableName nvarchar(256) NULL,
        SourceId nvarchar(400) NOT NULL,
        SuggestedCategory nvarchar(120) NOT NULL,
        ConfidenceScore int NOT NULL,
        ConfidenceBand nvarchar(60) NOT NULL,
        Evidence nvarchar(max) NULL,
        SuggestedAction nvarchar(max) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationSuggestedMapping', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationSuggestedMapping(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        MappingType nvarchar(120) NOT NULL,
        SourceTableName nvarchar(256) NULL,
        SourceId nvarchar(400) NOT NULL,
        SuggestedTargetTable nvarchar(256) NULL,
        SuggestedTargetId nvarchar(400) NULL,
        ConfidenceScore int NOT NULL,
        Evidence nvarchar(max) NULL,
        RequiresApproval bit NOT NULL DEFAULT(1),
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.PropertyMigrationManualReview', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationManualReview(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        Priority int NOT NULL,
        Severity nvarchar(60) NOT NULL,
        EntityType nvarchar(100) NOT NULL,
        SourceTableName nvarchar(256) NULL,
        SourceId nvarchar(400) NOT NULL,
        IssueType nvarchar(200) NOT NULL,
        IntelligenceStatus nvarchar(100) NOT NULL,
        ConfidenceScore int NOT NULL,
        SuggestedAction nvarchar(max) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

DELETE FROM dbo.PropertyMigrationMatchCandidate WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationConfidenceScore WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationResolutionResult WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationClassificationResult WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationSuggestedMapping WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationManualReview WHERE MigrationBatchId=@BatchId;

/* Confidence helper bands are expressed inline for SQL Server 2012 compatibility. */

/* 1) Journal Line Resolver: source line discovery, balance detection, account-code extraction. */
IF OBJECT_ID('tempdb..#JournalKeys') IS NOT NULL DROP TABLE #JournalKeys;
CREATE TABLE #JournalKeys(SourceId int NOT NULL PRIMARY KEY);

INSERT INTO #JournalKeys(SourceId)
SELECT DISTINCT CONVERT(int, SourceId)
FROM dbo.PropertyMigrationSourceJournal
WHERE MigrationBatchId=@BatchId AND ISNUMERIC(SourceId)=1;

IF OBJECT_ID('tempdb..#DevLines') IS NOT NULL DROP TABLE #DevLines;
SELECT
    d.Notes_ID,
    d.Double_Entry_Vouchers_ID,
    d.DEV_ID_Line_No,
    d.Account_Code,
    ValueAmount = CAST(ISNULL(d.Value,0) AS money),
    d.Credit_Or_Debit,
    DebitAmount = CAST(CASE WHEN d.Credit_Or_Debit = 0 THEN ISNULL(d.Value,0) ELSE 0 END AS money),
    CreditAmount = CAST(CASE WHEN d.Credit_Or_Debit = 1 THEN ISNULL(d.Value,0) ELSE 0 END AS money),
    TargetAccountId = ca.Id
INTO #DevLines
FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS d
JOIN #JournalKeys jk ON jk.SourceId = d.Notes_ID
LEFT JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT = d.Account_Code COLLATE DATABASE_DEFAULT;

IF OBJECT_ID('tempdb..#JournalGroup') IS NOT NULL DROP TABLE #JournalGroup;
SELECT
    Notes_ID,
    Lines = COUNT(*),
    NullAccountCodeLines = SUM(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(Account_Code,''))), '') IS NULL THEN 1 ELSE 0 END),
    TargetAccountMissingLines = SUM(CASE WHEN TargetAccountId IS NULL THEN 1 ELSE 0 END),
    DebitTotal = SUM(DebitAmount),
    CreditTotal = SUM(CreditAmount),
    AmountTotal = SUM(ValueAmount)
INTO #JournalGroup
FROM #DevLines
GROUP BY Notes_ID;

INSERT INTO dbo.PropertyMigrationMatchCandidate(
    MigrationBatchId, CustomerCode, SourceDatabaseName, EntityType, SourceTableName, SourceId,
    CandidateTableName, CandidateId, CandidateEntityType, MatchStrategy, Signals,
    ConfidenceScore, ConfidenceBand, ResolutionStatus, SuggestedAction)
SELECT
    @BatchId, @CustomerCode, @SourceDatabaseName, N'JournalLine', N'DOUBLE_ENTREY_VOUCHERS', CONVERT(nvarchar(400), dl.Notes_ID) + N':' + CONVERT(nvarchar(40), dl.DEV_ID_Line_No),
    N'ChartOfAccount', CONVERT(nvarchar(400), dl.TargetAccountId), N'Account', N'JournalLineResolver.AccountCode',
    N'AccountCode=' + ISNULL(dl.Account_Code,N'') + N'; Direction=' + ISNULL(CONVERT(nvarchar(20), dl.Credit_Or_Debit),N'NULL') + N'; Value=' + CONVERT(nvarchar(40), dl.ValueAmount),
    CASE WHEN dl.TargetAccountId IS NOT NULL THEN 90 WHEN NULLIF(LTRIM(RTRIM(ISNULL(dl.Account_Code,''))), '') IS NOT NULL THEN 75 ELSE 20 END,
    CASE WHEN dl.TargetAccountId IS NOT NULL THEN N'HighConfidence' WHEN NULLIF(LTRIM(RTRIM(ISNULL(dl.Account_Code,''))), '') IS NOT NULL THEN N'ManualReviewRecommended' ELSE N'Blocked' END,
    CASE WHEN dl.TargetAccountId IS NOT NULL THEN N'AccountResolved' WHEN NULLIF(LTRIM(RTRIM(ISNULL(dl.Account_Code,''))), '') IS NOT NULL THEN N'SourceAccountCodeResolved_TargetAccountMissing' ELSE N'BlockedMissingAccountCode' END,
    CASE WHEN dl.TargetAccountId IS NOT NULL THEN N'Can be mapped after journal-level balance check.' ELSE N'Create/map ChartOfAccount for source Account_Code before journal migration.' END
FROM #DevLines dl;

INSERT INTO dbo.PropertyMigrationResolutionResult(MigrationBatchId, CustomerCode, EntityType, SourceId, ResolutionType, ResolutionStatus, ConfidenceScore, Evidence, CriticalBlocker)
SELECT
    @BatchId, @CustomerCode, N'Journal', CONVERT(nvarchar(400), j.SourceId), N'JournalLineResolver',
    CASE
        WHEN g.Notes_ID IS NULL THEN N'BlockedNoSourceLines'
        WHEN g.Lines < 2 THEN N'BlockedIncompleteJournal'
        WHEN g.NullAccountCodeLines > 0 THEN N'BlockedMissingSourceAccountCode'
        WHEN ABS(g.DebitTotal - g.CreditTotal) >= 0.01 THEN N'BlockedUnbalancedSourceJournal'
        WHEN g.TargetAccountMissingLines > 0 THEN N'HighConfidenceSourceBalanced_TargetAccountMappingRequired'
        ELSE N'AutoApprovedSourceAndTargetBalanced'
    END,
    CASE
        WHEN g.Notes_ID IS NULL THEN 20
        WHEN g.Lines < 2 THEN 35
        WHEN g.NullAccountCodeLines > 0 THEN 30
        WHEN ABS(g.DebitTotal - g.CreditTotal) >= 0.01 THEN 25
        WHEN g.TargetAccountMissingLines > 0 THEN 82
        ELSE 97
    END,
    N'Lines=' + ISNULL(CONVERT(nvarchar(20), g.Lines),N'0') + N'; Debit=' + ISNULL(CONVERT(nvarchar(40), g.DebitTotal),N'0') + N'; Credit=' + ISNULL(CONVERT(nvarchar(40), g.CreditTotal),N'0') + N'; MissingTargetAccounts=' + ISNULL(CONVERT(nvarchar(20), g.TargetAccountMissingLines),N'0'),
    CASE
        WHEN g.Notes_ID IS NULL THEN N'No source journal lines found for staged journal SourceId.'
        WHEN g.Lines < 2 THEN N'Journal has fewer than two source lines.'
        WHEN g.NullAccountCodeLines > 0 THEN N'Source journal line has blank Account_Code.'
        WHEN ABS(g.DebitTotal - g.CreditTotal) >= 0.01 THEN N'Source journal is not balanced; cannot migrate.'
        WHEN g.TargetAccountMissingLines > 0 THEN N'Source journal is balanced but target ChartOfAccount mapping is missing.'
        ELSE NULL
    END
FROM dbo.PropertyMigrationSourceJournal j
LEFT JOIN #JournalGroup g ON g.Notes_ID = CONVERT(int, j.SourceId)
WHERE j.MigrationBatchId=@BatchId AND ISNUMERIC(j.SourceId)=1;

/* 2) Receipt Matching: improve unresolved receipts using ContNo/CusID/amount/date proximity. */
IF OBJECT_ID('tempdb..#ReceiptCandidates') IS NOT NULL DROP TABLE #ReceiptCandidates;
;WITH CandidateBase AS (
    SELECT
        r.SourceId,
        r.MoneyAmount,
        r.ReceiptDate,
        NoteContNo = n.ContNo,
        NoteCusID = n.CusID,
        c.SourceId AS CandidateContractId,
        i.SourceId AS CandidateInstallmentId,
        c.SourceRenterId,
        i.BatchDate,
        i.BatchTotal,
        ContractSignal = CASE WHEN n.ContNo IS NOT NULL AND c.SourceId IS NOT NULL THEN 35 ELSE 0 END,
        RenterSignal = CASE WHEN n.CusID IS NOT NULL AND c.SourceRenterId = CONVERT(nvarchar(400), n.CusID) COLLATE DATABASE_DEFAULT THEN 15 ELSE 0 END,
        AmountSignal = CASE WHEN i.BatchTotal IS NOT NULL AND ABS(CAST(r.MoneyAmount AS decimal(19,4)) - CAST(i.BatchTotal AS decimal(19,4))) < 0.01 THEN 25 WHEN i.BatchTotal IS NOT NULL AND ABS(CAST(r.MoneyAmount AS decimal(19,4)) - CAST(i.BatchTotal AS decimal(19,4))) <= 10 THEN 15 ELSE 0 END,
        DateSignal = CASE WHEN i.BatchDate IS NOT NULL AND ABS(DATEDIFF(day, r.ReceiptDate, i.BatchDate)) <= 7 THEN 15 WHEN i.BatchDate IS NOT NULL AND ABS(DATEDIFF(day, r.ReceiptDate, i.BatchDate)) <= 31 THEN 8 ELSE 0 END
    FROM dbo.PropertyMigrationSourceReceipt r
    JOIN RSMDB.dbo.Notes n ON CONVERT(nvarchar(400), n.NoteID) COLLATE DATABASE_DEFAULT = r.SourceId COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.PropertyMigrationSourceContract c ON c.MigrationBatchId=@BatchId AND c.SourceId = CONVERT(nvarchar(400), n.ContNo) COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.PropertyMigrationSourceInstallment i ON i.MigrationBatchId=@BatchId AND i.SourceContractId = c.SourceId
    WHERE r.MigrationBatchId=@BatchId
      AND (r.SourceContractId IS NULL OR r.SourceInstallmentId IS NULL)
), Ranked AS (
    SELECT *,
        TotalScore = ContractSignal + RenterSignal + AmountSignal + DateSignal,
        rn = ROW_NUMBER() OVER (PARTITION BY SourceId ORDER BY ContractSignal + RenterSignal + AmountSignal + DateSignal DESC, ABS(DATEDIFF(day, ReceiptDate, ISNULL(BatchDate, ReceiptDate))) ASC)
    FROM CandidateBase
    WHERE CandidateContractId IS NOT NULL
)
SELECT * INTO #ReceiptCandidates FROM Ranked WHERE rn = 1;

INSERT INTO dbo.PropertyMigrationMatchCandidate(
    MigrationBatchId, CustomerCode, SourceDatabaseName, EntityType, SourceTableName, SourceId,
    CandidateTableName, CandidateId, CandidateEntityType, MatchStrategy, Signals,
    ConfidenceScore, ConfidenceBand, ResolutionStatus, SuggestedAction)
SELECT
    @BatchId, @CustomerCode, @SourceDatabaseName, N'Receipt', N'Notes', SourceId,
    N'PropertyMigrationSourceInstallment', CandidateInstallmentId, N'Installment', N'ReceiptMatching.ContNoRenterAmountDate',
    N'ContNo=' + ISNULL(CONVERT(nvarchar(40), NoteContNo),N'') + N'; CusID=' + ISNULL(CONVERT(nvarchar(40), NoteCusID),N'') + N'; Contract=' + ISNULL(CandidateContractId,N'') + N'; Installment=' + ISNULL(CandidateInstallmentId,N'') + N'; AmountSignal=' + CONVERT(nvarchar(10), AmountSignal) + N'; DateSignal=' + CONVERT(nvarchar(10), DateSignal),
    TotalScore,
    CASE WHEN TotalScore >= 95 THEN N'AutoApproved' WHEN TotalScore >= 80 THEN N'HighConfidence' WHEN TotalScore >= 60 THEN N'ManualReviewRecommended' WHEN TotalScore >= 40 THEN N'WeakMatch' ELSE N'Blocked' END,
    CASE WHEN TotalScore >= 95 THEN N'AutoApprovedCandidate' WHEN TotalScore >= 80 THEN N'HighConfidenceReview' WHEN TotalScore >= 60 THEN N'MediumReview' WHEN TotalScore >= 40 THEN N'WeakMatch' ELSE N'Blocked' END,
    CASE WHEN TotalScore >= 80 THEN N'Review sample, then approve receipt link candidate.' ELSE N'Keep in manual review; insufficient matching signals.' END
FROM #ReceiptCandidates;

INSERT INTO dbo.PropertyMigrationConfidenceScore(MigrationBatchId, CustomerCode, EntityType, SourceId, Score, Band, ScoreReason, IsAccountingCritical, AllowsAutoApproval, RequiresManualReview)
SELECT
    @BatchId, @CustomerCode, N'Receipt', r.SourceId,
    CASE WHEN r.IsValid=1 AND r.SourceContractId IS NOT NULL AND r.SourceInstallmentId IS NOT NULL THEN 98 ELSE ISNULL(rc.TotalScore, 20) END,
    CASE WHEN r.IsValid=1 AND r.SourceContractId IS NOT NULL AND r.SourceInstallmentId IS NOT NULL THEN N'AutoApproved' WHEN rc.TotalScore >= 95 THEN N'AutoApproved' WHEN rc.TotalScore >= 80 THEN N'HighConfidence' WHEN rc.TotalScore >= 60 THEN N'ManualReviewRecommended' WHEN rc.TotalScore >= 40 THEN N'WeakMatch' ELSE N'Blocked' END,
    CASE WHEN r.IsValid=1 AND r.SourceContractId IS NOT NULL AND r.SourceInstallmentId IS NOT NULL THEN N'Already staged with safe contract/installment link.' ELSE N'Intelligence candidate score from contract/renter/amount/date signals.' END,
    1,
    CASE WHEN (r.IsValid=1 AND r.SourceContractId IS NOT NULL AND r.SourceInstallmentId IS NOT NULL) OR rc.TotalScore >= 95 THEN 1 ELSE 0 END,
    CASE WHEN (r.IsValid=1 AND r.SourceContractId IS NOT NULL AND r.SourceInstallmentId IS NOT NULL) OR rc.TotalScore >= 95 THEN 0 ELSE 1 END
FROM dbo.PropertyMigrationSourceReceipt r
LEFT JOIN #ReceiptCandidates rc ON rc.SourceId = r.SourceId
WHERE r.MigrationBatchId=@BatchId;

/* 3) Owner/Issue payment classification. */
INSERT INTO dbo.PropertyMigrationClassificationResult(MigrationBatchId, CustomerCode, EntityType, SourceTableName, SourceId, SuggestedCategory, ConfidenceScore, ConfidenceBand, Evidence, SuggestedAction)
SELECT
    @BatchId, @CustomerCode, N'Issue', N'Notes', i.SourceId,
    CASE
        WHEN n.AkarID IS NOT NULL AND NULLIF(LTRIM(RTRIM(ISNULL(n.AccountPaym,''))), '') IS NOT NULL THEN N'PropertyLinkedPaymentOrExpense'
        WHEN n.AkarID IS NOT NULL THEN N'PropertyLinkedIssue'
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(n.AccountPaym,''))), '') IS NOT NULL THEN N'AccountPaymentOrExpense'
        WHEN n.CusID IS NOT NULL THEN N'CounterpartyPayment'
        ELSE N'UnknownIssue'
    END,
    CASE
        WHEN n.AkarID IS NOT NULL AND NULLIF(LTRIM(RTRIM(ISNULL(n.AccountPaym,''))), '') IS NOT NULL THEN 72
        WHEN n.AkarID IS NOT NULL THEN 68
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(n.AccountPaym,''))), '') IS NOT NULL THEN 62
        WHEN n.CusID IS NOT NULL THEN 55
        ELSE 30
    END,
    CASE
        WHEN n.AkarID IS NOT NULL AND NULLIF(LTRIM(RTRIM(ISNULL(n.AccountPaym,''))), '') IS NOT NULL THEN N'ManualReviewRecommended'
        WHEN n.AkarID IS NOT NULL THEN N'ManualReviewRecommended'
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(n.AccountPaym,''))), '') IS NOT NULL THEN N'ManualReviewRecommended'
        WHEN n.CusID IS NOT NULL THEN N'WeakMatch'
        ELSE N'Blocked'
    END,
    N'NoteType=' + ISNULL(CONVERT(nvarchar(20), n.NoteType),N'') + N'; AkarID=' + ISNULL(CONVERT(nvarchar(40), n.AkarID),N'') + N'; CusID=' + ISNULL(CONVERT(nvarchar(40), n.CusID),N'') + N'; AccountPaym=' + ISNULL(n.AccountPaym,N''),
    CASE WHEN n.AkarID IS NOT NULL THEN N'Finance/property review required before owner/vendor/expense classification is approved.' ELSE N'Keep excluded until source semantics are reviewed.' END
FROM dbo.PropertyMigrationSourceIssue i
JOIN RSMDB.dbo.Notes n ON CONVERT(nvarchar(400), n.NoteID) COLLATE DATABASE_DEFAULT = i.SourceId COLLATE DATABASE_DEFAULT
WHERE i.MigrationBatchId=@BatchId;

INSERT INTO dbo.PropertyMigrationClassificationResult(MigrationBatchId, CustomerCode, EntityType, SourceTableName, SourceId, SuggestedCategory, ConfidenceScore, ConfidenceBand, Evidence, SuggestedAction)
SELECT
    @BatchId, @CustomerCode, N'OwnerPayableCandidate', N'TblAqrOwin', CONVERT(nvarchar(400), ow.ID),
    N'OwnerPayableSchedule', 86, N'HighConfidence',
    N'AqrID=' + ISNULL(CONVERT(nvarchar(40), ow.AqrID),N'') + N'; Cont=' + ISNULL(CONVERT(nvarchar(40), ow.Cont),N'') + N'; Value=' + ISNULL(CONVERT(nvarchar(40), ow.value),N''),
    N'Use as owner payable schedule candidate after owner/property account mapping approval.'
FROM RSMDB.dbo.TblAqrOwin ow;

/* 4) NoteType intelligence from observed usage and posting behavior. */
INSERT INTO dbo.PropertyMigrationClassificationResult(MigrationBatchId, CustomerCode, EntityType, SourceTableName, SourceId, SuggestedCategory, ConfidenceScore, ConfidenceBand, Evidence, SuggestedAction)
SELECT
    @BatchId, @CustomerCode, N'NoteType', N'Notes', CONVERT(nvarchar(40), n.NoteType),
    CASE n.NoteType
        WHEN 4 THEN N'Receipt'
        WHEN 5 THEN N'IssueOrPayment'
        WHEN 60 THEN N'ContractJournalCandidate'
        WHEN -1 THEN N'TerminationCandidate'
        WHEN 9088 THEN N'VATOrInstallmentAdjustmentCandidate'
        ELSE N'Unknown'
    END,
    CASE n.NoteType WHEN 4 THEN 94 WHEN 5 THEN 72 WHEN 60 THEN 70 WHEN -1 THEN 65 WHEN 9088 THEN 45 ELSE 20 END,
    CASE n.NoteType WHEN 4 THEN N'HighConfidence' WHEN 5 THEN N'ManualReviewRecommended' WHEN 60 THEN N'ManualReviewRecommended' WHEN -1 THEN N'ManualReviewRecommended' WHEN 9088 THEN N'WeakMatch' ELSE N'Blocked' END,
    N'Count=' + CONVERT(nvarchar(40), COUNT(*)) + N'; TotalAmount=' + CONVERT(nvarchar(60), CAST(SUM(ISNULL(n.Note_Value,0)) AS money)) + N'; WithContNo=' + CONVERT(nvarchar(40), SUM(CASE WHEN n.ContNo IS NOT NULL THEN 1 ELSE 0 END)) + N'; WithInstall=' + CONVERT(nvarchar(40), SUM(CASE WHEN n.installIDCont IS NOT NULL THEN 1 ELSE 0 END)) + N'; WithCus=' + CONVERT(nvarchar(40), SUM(CASE WHEN n.CusID IS NOT NULL THEN 1 ELSE 0 END)),
    CASE n.NoteType WHEN 4 THEN N'Can be used for receipt candidates if contract/installment matching is proven.' WHEN 9088 THEN N'Do not migrate until VAT/installment meaning is confirmed from VB6 workflow.' ELSE N'Requires finance/VB6 evidence before accounting migration.' END
FROM RSMDB.dbo.Notes n
WHERE n.NoteType IN (4,5,60,-1,9088)
GROUP BY n.NoteType;

/* 5) Review Queue reduction recommendations. Do not delete or hide the original ReviewQueue. */
INSERT INTO dbo.PropertyMigrationManualReview(MigrationBatchId, CustomerCode, Priority, Severity, EntityType, SourceTableName, SourceId, IssueType, IntelligenceStatus, ConfidenceScore, SuggestedAction)
SELECT
    @BatchId, @CustomerCode,
    CASE WHEN rr.ConfidenceScore >= 80 THEN 2 WHEN rr.ConfidenceScore >= 60 THEN 3 ELSE 1 END,
    CASE WHEN rr.ConfidenceScore >= 80 THEN N'Warning' WHEN rr.ConfidenceScore >= 60 THEN N'Warning' ELSE N'Critical' END,
    rr.EntityType, N'PropertyMigrationSourceJournal', rr.SourceId, N'JournalWithoutMappedLines', rr.ResolutionStatus, rr.ConfidenceScore,
    CASE WHEN rr.ResolutionStatus LIKE N'HighConfidenceSourceBalanced%' THEN N'Map source Account_Code values to target ChartOfAccount, then journal lines can be staged.' ELSE ISNULL(rr.CriticalBlocker,N'Review journal resolver evidence.') END
FROM dbo.PropertyMigrationResolutionResult rr
WHERE rr.MigrationBatchId=@BatchId AND rr.EntityType=N'Journal';

INSERT INTO dbo.PropertyMigrationManualReview(MigrationBatchId, CustomerCode, Priority, Severity, EntityType, SourceTableName, SourceId, IssueType, IntelligenceStatus, ConfidenceScore, SuggestedAction)
SELECT
    @BatchId, @CustomerCode,
    CASE WHEN mc.ConfidenceScore >= 80 THEN 2 WHEN mc.ConfidenceScore >= 60 THEN 3 ELSE 1 END,
    N'Warning', N'Receipt', N'Notes', mc.SourceId, N'ReceiptWithoutSafeLink', mc.ResolutionStatus, mc.ConfidenceScore, mc.SuggestedAction
FROM dbo.PropertyMigrationMatchCandidate mc
WHERE mc.MigrationBatchId=@BatchId AND mc.EntityType=N'Receipt';

INSERT INTO dbo.PropertyMigrationManualReview(MigrationBatchId, CustomerCode, Priority, Severity, EntityType, SourceTableName, SourceId, IssueType, IntelligenceStatus, ConfidenceScore, SuggestedAction)
SELECT
    @BatchId, @CustomerCode,
    CASE WHEN cr.ConfidenceScore >= 70 THEN 3 WHEN cr.ConfidenceScore >= 60 THEN 4 ELSE 2 END,
    CASE WHEN cr.ConfidenceScore < 40 THEN N'Critical' ELSE N'Warning' END,
    cr.EntityType, cr.SourceTableName, cr.SourceId, N'IssuePaymentManualReview', cr.SuggestedCategory, cr.ConfidenceScore, cr.SuggestedAction
FROM dbo.PropertyMigrationClassificationResult cr
WHERE cr.MigrationBatchId=@BatchId AND cr.EntityType IN (N'Issue',N'OwnerPayableCandidate');

/* Mark original review items with non-destructive intelligence status where clear evidence exists. */
UPDATE rq
SET Status = CASE
        WHEN rq.IssueType=N'JournalWithoutMappedLines' AND rr.ConfidenceScore >= 80 THEN N'IntelligenceHighConfidence'
        WHEN rq.IssueType=N'JournalWithoutMappedLines' AND rr.ConfidenceScore < 40 THEN N'IntelligenceBlocked'
        WHEN rq.IssueType=N'ReceiptWithoutSafeLink' AND mc.ConfidenceScore >= 80 THEN N'IntelligenceHighConfidence'
        WHEN rq.IssueType=N'ReceiptWithoutSafeLink' AND mc.ConfidenceScore >= 60 THEN N'IntelligenceMediumReview'
        WHEN rq.IssueType=N'IssuePaymentManualReview' AND cr.ConfidenceScore >= 60 THEN N'IntelligenceClassifiedReview'
        ELSE rq.Status END,
    ResolutionNotes = ISNULL(rq.ResolutionNotes + CHAR(13)+CHAR(10), N'') + N'Intelligence pass: ' +
        ISNULL(rr.ResolutionStatus, ISNULL(mc.ResolutionStatus, ISNULL(cr.SuggestedCategory, N'No strong candidate')))
FROM dbo.PropertyMigrationReviewQueue rq
LEFT JOIN dbo.PropertyMigrationResolutionResult rr ON rr.MigrationBatchId=rq.MigrationBatchId AND rr.EntityType=N'Journal' AND rr.SourceId=rq.SourceId AND rq.IssueType=N'JournalWithoutMappedLines'
LEFT JOIN dbo.PropertyMigrationMatchCandidate mc ON mc.MigrationBatchId=rq.MigrationBatchId AND mc.EntityType=N'Receipt' AND mc.SourceId=rq.SourceId AND rq.IssueType=N'ReceiptWithoutSafeLink'
LEFT JOIN dbo.PropertyMigrationClassificationResult cr ON cr.MigrationBatchId=rq.MigrationBatchId AND cr.EntityType=N'Issue' AND cr.SourceId=rq.SourceId AND rq.IssueType=N'IssuePaymentManualReview'
WHERE rq.MigrationBatchId=@BatchId
  AND rq.IssueType IN (N'JournalWithoutMappedLines',N'ReceiptWithoutSafeLink',N'IssuePaymentManualReview');

/* Snapshot summary. */
SELECT Metric, CountValue
FROM (
    SELECT N'JournalLinesDiscovered' Metric, COUNT(*) CountValue FROM #DevLines
    UNION ALL SELECT N'JournalBalancedSourceButTargetAccountMissing', COUNT(*) FROM dbo.PropertyMigrationResolutionResult WHERE MigrationBatchId=@BatchId AND EntityType=N'Journal' AND ResolutionStatus=N'HighConfidenceSourceBalanced_TargetAccountMappingRequired'
    UNION ALL SELECT N'JournalBlocked', COUNT(*) FROM dbo.PropertyMigrationResolutionResult WHERE MigrationBatchId=@BatchId AND EntityType=N'Journal' AND ResolutionStatus LIKE N'Blocked%'
    UNION ALL SELECT N'ReceiptAutoApprovedExistingOrCandidate', COUNT(*) FROM dbo.PropertyMigrationConfidenceScore WHERE MigrationBatchId=@BatchId AND EntityType=N'Receipt' AND Band=N'AutoApproved'
    UNION ALL SELECT N'ReceiptHighConfidenceCandidates', COUNT(*) FROM dbo.PropertyMigrationMatchCandidate WHERE MigrationBatchId=@BatchId AND EntityType=N'Receipt' AND ConfidenceScore >= 80
    UNION ALL SELECT N'IssueClassifiedReview', COUNT(*) FROM dbo.PropertyMigrationClassificationResult WHERE MigrationBatchId=@BatchId AND EntityType=N'Issue' AND ConfidenceScore >= 60
    UNION ALL SELECT N'IssueBlockedUnknown', COUNT(*) FROM dbo.PropertyMigrationClassificationResult WHERE MigrationBatchId=@BatchId AND EntityType=N'Issue' AND ConfidenceScore < 40
    UNION ALL SELECT N'ManualReviewIntelligenceRows', COUNT(*) FROM dbo.PropertyMigrationManualReview WHERE MigrationBatchId=@BatchId
) s
ORDER BY Metric;

/* 6) Follow-up status normalization: classify weak/blocked review items without deleting original evidence. */
UPDATE rq
SET Status = CASE WHEN cs.Band=N'Blocked' THEN N'IntelligenceBlocked' WHEN cs.Band=N'WeakMatch' THEN N'IntelligenceWeakMatch' ELSE rq.Status END,
    ResolutionNotes = ISNULL(rq.ResolutionNotes + CHAR(13)+CHAR(10), N'') + N'Intelligence pass: ' + cs.Band + N' receipt confidence; no auto approval.'
FROM dbo.PropertyMigrationReviewQueue rq
JOIN dbo.PropertyMigrationConfidenceScore cs ON cs.MigrationBatchId=rq.MigrationBatchId AND cs.EntityType=N'Receipt' AND cs.SourceId=rq.SourceId
WHERE rq.MigrationBatchId=@BatchId AND rq.IssueType=N'ReceiptWithoutSafeLink' AND rq.Status=N'Open' AND cs.Band IN (N'Blocked',N'WeakMatch');

UPDATE rq
SET Status = CASE WHEN cr.ConfidenceBand=N'Blocked' THEN N'IntelligenceBlocked' WHEN cr.ConfidenceBand=N'WeakMatch' THEN N'IntelligenceWeakMatch' ELSE rq.Status END,
    ResolutionNotes = ISNULL(rq.ResolutionNotes + CHAR(13)+CHAR(10), N'') + N'Intelligence pass: ' + cr.SuggestedCategory + N'; no auto approval.'
FROM dbo.PropertyMigrationReviewQueue rq
JOIN dbo.PropertyMigrationClassificationResult cr ON cr.MigrationBatchId=rq.MigrationBatchId AND cr.EntityType=N'Issue' AND cr.SourceId=rq.SourceId
WHERE rq.MigrationBatchId=@BatchId AND rq.IssueType=N'IssuePaymentManualReview' AND rq.Status=N'Open' AND cr.ConfidenceBand IN (N'Blocked',N'WeakMatch');

UPDATE rq
SET Status = N'IntelligenceClassifiedReview',
    ResolutionNotes = ISNULL(rq.ResolutionNotes + CHAR(13)+CHAR(10), N'') + N'Intelligence pass: NoteType/owner-payable classification exists but requires evidence review.'
FROM dbo.PropertyMigrationReviewQueue rq
WHERE rq.MigrationBatchId=@BatchId AND rq.IssueType IN (N'TerminationManualReview',N'UnclassifiedNoteType9088',N'OwnerPayableCandidate') AND rq.Status=N'Open';
