/*
RSMDB CashingType8 Finance Approval - 2026-05-20
Finance pack + impact simulation + approval table scope columns only.
No migration, no receipts, no journals, no RSMDB modification.
Approvals are NOT applied unless @ApplyTopN is set explicitly by reviewer.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET CONCAT_NULL_YIELDS_NULL ON;

DECLARE @BatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50) = N'$(CustomerCode)';
DECLARE @ScopeName nvarchar(100) = N'RSMDB_CashingType8';
DECLARE @ApprovalBatchId uniqueidentifier = NEWID();
DECLARE @ApplyTopN int = 0; -- Safety default: 0 means simulation only. Set to 25/50/100 only after explicit finance approval.

IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN RAISERROR('CashingType8 finance approval requires clone/sandbox target database.',16,1); RETURN; END;

IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp')
BEGIN RAISERROR('Blocked database for CashingType8 finance approval.',16,1); RETURN; END;

IF OBJECT_ID('dbo.PropertyMigrationAccountFinanceApproval','U') IS NULL
BEGIN RAISERROR('PropertyMigrationAccountFinanceApproval table is required.',16,1); RETURN; END;

IF COL_LENGTH('dbo.PropertyMigrationAccountFinanceApproval','ScopeName') IS NULL
    ALTER TABLE dbo.PropertyMigrationAccountFinanceApproval ADD ScopeName nvarchar(100) NULL;
IF COL_LENGTH('dbo.PropertyMigrationAccountFinanceApproval','ApprovalBatchId') IS NULL
    ALTER TABLE dbo.PropertyMigrationAccountFinanceApproval ADD ApprovalBatchId uniqueidentifier NULL;

IF OBJECT_ID('dbo.PropertyMigrationCashingType8FinanceApprovalPack','U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationCashingType8FinanceApprovalPack
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(50) NOT NULL,
        ScopeName nvarchar(100) NOT NULL,
        SourceAccountCode nvarchar(255) NOT NULL,
        SourceAccountName nvarchar(500) NULL,
        SuggestedTargetAccountId int NULL,
        SuggestedTargetAccountSerial nvarchar(255) NULL,
        SuggestedTargetAccountName nvarchar(500) NULL,
        SuggestedFamily nvarchar(100) NULL,
        ConfidenceScore int NOT NULL DEFAULT(0),
        UsageCount int NOT NULL DEFAULT(0),
        TotalDebit decimal(18,4) NULL,
        TotalCredit decimal(18,4) NULL,
        RelatedReceiptsCount int NOT NULL DEFAULT(0),
        RelatedJournalsCount int NOT NULL DEFAULT(0),
        SampleReceipt nvarchar(1000) NULL,
        SampleJournal nvarchar(1000) NULL,
        SuggestedDecision nvarchar(50) NOT NULL,
        PriorityRank int NULL,
        CanUnlockLinkedReceipts bit NOT NULL DEFAULT(0),
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID('dbo.PropertyMigrationCashingType8ApprovalSimulation','U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationCashingType8ApprovalSimulation
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(50) NOT NULL,
        ScopeName nvarchar(100) NOT NULL,
        ScenarioName nvarchar(100) NOT NULL,
        ApprovedAccountCount int NOT NULL,
        ReadyReceipts int NOT NULL,
        ReadyJournals int NOT NULL,
        ReadyJournalLines int NOT NULL,
        ReadyValue decimal(18,4) NOT NULL DEFAULT(0),
        AccountsStillReview int NOT NULL,
        BlockedReceipts int NOT NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

DELETE FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack WHERE MigrationBatchId=@BatchId AND ScopeName=@ScopeName;
DELETE FROM dbo.PropertyMigrationCashingType8ApprovalSimulation WHERE MigrationBatchId=@BatchId AND ScopeName=@ScopeName;

;WITH BestMatch AS
(
    SELECT *
    FROM
    (
        SELECT mc.*, ROW_NUMBER() OVER(PARTITION BY mc.SourceAccountCode ORDER BY mc.ConfidenceScore DESC, mc.TargetAccountId ASC) rn
        FROM dbo.PropertyMigrationAccountMatchCandidate mc
        WHERE mc.MigrationBatchId=@BatchId
    ) x
    WHERE rn=1
), Family AS
(
    SELECT *
    FROM
    (
        SELECT af.*, ROW_NUMBER() OVER(PARTITION BY af.SourceAccountCode ORDER BY af.FamilyConfidence DESC, af.Id ASC) rn
        FROM dbo.PropertyMigrationAccountFamily af
        WHERE af.MigrationBatchId=@BatchId
    ) x
    WHERE rn=1
), PackBase AS
(
    SELECT fr.SourceAccountCode,
           fr.SourceAccountName,
           COALESCE(bm.TargetAccountId, fam.SuggestedParentTargetAccountId) SuggestedTargetAccountId,
           COALESCE(bm.TargetAccountCode, fam.SuggestedParentTargetAccountCode) SuggestedTargetAccountSerial,
           bm.TargetAccountName SuggestedTargetAccountName,
           COALESCE(bm.SuggestedFamily, fam.SuggestedFamily, N'Unknown') SuggestedFamily,
           COALESCE(bm.ConfidenceScore, fam.FamilyConfidence, 0) ConfidenceScore,
           fr.UsageJournalLineCount UsageCount,
           fr.TotalDebit,
           fr.TotalCredit,
           fr.UsageReceiptCount RelatedReceiptsCount,
           fr.UsageJournalLineCount RelatedJournalsCount,
           fr.SampleReceiptIds SampleReceipt,
           fr.SampleReceiptIds SampleJournal,
           CASE WHEN fr.CandidateClassImpact LIKE N'Can unlock%' THEN 1 ELSE 0 END CanUnlockLinkedReceipts,
           ROW_NUMBER() OVER(ORDER BY CASE WHEN fr.CandidateClassImpact LIKE N'Can unlock%' THEN 0 ELSE 1 END,
                                      fr.UsageReceiptCount DESC,
                                      ISNULL(fr.TotalDebit,0)+ISNULL(fr.TotalCredit,0) DESC,
                                      COALESCE(bm.ConfidenceScore, fam.FamilyConfidence, 0) DESC) PriorityRank
    FROM dbo.PropertyMigrationCashingType8FinanceReviewAccount fr
    LEFT JOIN BestMatch bm ON bm.SourceAccountCode = fr.SourceAccountCode COLLATE DATABASE_DEFAULT
    LEFT JOIN Family fam ON fam.SourceAccountCode = fr.SourceAccountCode COLLATE DATABASE_DEFAULT
    WHERE fr.MigrationBatchId=@BatchId
)
INSERT INTO dbo.PropertyMigrationCashingType8FinanceApprovalPack
(MigrationBatchId,CustomerCode,ScopeName,SourceAccountCode,SourceAccountName,SuggestedTargetAccountId,SuggestedTargetAccountSerial,SuggestedTargetAccountName,SuggestedFamily,ConfidenceScore,UsageCount,TotalDebit,TotalCredit,RelatedReceiptsCount,RelatedJournalsCount,SampleReceipt,SampleJournal,SuggestedDecision,PriorityRank,CanUnlockLinkedReceipts)
SELECT @BatchId,@CustomerCode,@ScopeName,SourceAccountCode,SourceAccountName,SuggestedTargetAccountId,SuggestedTargetAccountSerial,SuggestedTargetAccountName,SuggestedFamily,ConfidenceScore,UsageCount,TotalDebit,TotalCredit,RelatedReceiptsCount,RelatedJournalsCount,SampleReceipt,SampleJournal,
       CASE WHEN SuggestedTargetAccountId IS NULL THEN N'NeedsMoreInfo' ELSE N'ApproveAfterFinanceReview' END,
       PriorityRank,CanUnlockLinkedReceipts
FROM PackBase;

;WITH Scenarios AS
(
    SELECT N'Top25' ScenarioName, SourceAccountCode FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack WHERE MigrationBatchId=@BatchId AND ScopeName=@ScopeName AND PriorityRank<=25 AND SuggestedTargetAccountId IS NOT NULL
    UNION ALL SELECT N'Top50', SourceAccountCode FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack WHERE MigrationBatchId=@BatchId AND ScopeName=@ScopeName AND PriorityRank<=50 AND SuggestedTargetAccountId IS NOT NULL
    UNION ALL SELECT N'Top100', SourceAccountCode FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack WHERE MigrationBatchId=@BatchId AND ScopeName=@ScopeName AND PriorityRank<=100 AND SuggestedTargetAccountId IS NOT NULL
    UNION ALL SELECT N'ScoreGE60', SourceAccountCode FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack WHERE MigrationBatchId=@BatchId AND ScopeName=@ScopeName AND ConfidenceScore>=60 AND SuggestedTargetAccountId IS NOT NULL
), ScenarioNames AS
(
    SELECT N'Top25' ScenarioName UNION ALL SELECT N'Top50' UNION ALL SELECT N'Top100' UNION ALL SELECT N'ScoreGE60'
), ExistingApproved AS
(
    SELECT SourceAccountCode FROM dbo.PropertyMigrationAccountResolution WHERE MigrationBatchId=@BatchId AND ResolutionStatus=N'Approved' AND TargetAccountId IS NOT NULL
), CandidateRequired AS
(
    SELECT c.ReceiptNoteId, dev.Account_Code SourceAccountCode
    FROM dbo.PropertyMigrationCashingType8ReceiptCandidate c
    JOIN RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS dev ON dev.Notes_ID=c.ReceiptNoteId
    WHERE c.MigrationBatchId=@BatchId
      AND c.HasContractLink=1 AND c.HasInstallmentLink=1 AND c.HasRenterLink=1
      AND c.IsBalancedJournal=1 AND c.HasUnknownDirection=0
), ReceiptScenario AS
(
    SELECT sn.ScenarioName,c.ReceiptNoteId,c.ReceiptAmount,c.JournalLineCount,
           SUM(CASE WHEN ea.SourceAccountCode IS NOT NULL OR sc.SourceAccountCode IS NOT NULL THEN 0 ELSE 1 END) MissingAccounts
    FROM ScenarioNames sn
    JOIN dbo.PropertyMigrationCashingType8ReceiptCandidate c ON c.MigrationBatchId=@BatchId
      AND c.HasContractLink=1 AND c.HasInstallmentLink=1 AND c.HasRenterLink=1
      AND c.IsBalancedJournal=1 AND c.HasUnknownDirection=0
    JOIN CandidateRequired cr ON cr.ReceiptNoteId=c.ReceiptNoteId
    LEFT JOIN ExistingApproved ea ON ea.SourceAccountCode=cr.SourceAccountCode COLLATE DATABASE_DEFAULT
    LEFT JOIN Scenarios sc ON sc.ScenarioName=sn.ScenarioName AND sc.SourceAccountCode=cr.SourceAccountCode COLLATE DATABASE_DEFAULT
    GROUP BY sn.ScenarioName,c.ReceiptNoteId,c.ReceiptAmount,c.JournalLineCount
), Ready AS
(
    SELECT ScenarioName,COUNT(*) ReadyReceipts,SUM(ReceiptAmount) ReadyValue,SUM(JournalLineCount) ReadyLines
    FROM ReceiptScenario
    WHERE MissingAccounts=0
    GROUP BY ScenarioName
), ScenarioApprovedCounts AS
(
    SELECT sn.ScenarioName, COUNT(DISTINCT sc.SourceAccountCode) ApprovedAccountCount
    FROM ScenarioNames sn
    LEFT JOIN Scenarios sc ON sc.ScenarioName=sn.ScenarioName
    GROUP BY sn.ScenarioName
)
INSERT INTO dbo.PropertyMigrationCashingType8ApprovalSimulation
(MigrationBatchId,CustomerCode,ScopeName,ScenarioName,ApprovedAccountCount,ReadyReceipts,ReadyJournals,ReadyJournalLines,ReadyValue,AccountsStillReview,BlockedReceipts)
SELECT @BatchId,@CustomerCode,@ScopeName,sn.ScenarioName,
       ISNULL(ac.ApprovedAccountCount,0),ISNULL(r.ReadyReceipts,0),ISNULL(r.ReadyReceipts,0),ISNULL(r.ReadyLines,0),ISNULL(r.ReadyValue,0),
       (SELECT COUNT(*) FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack p WHERE p.MigrationBatchId=@BatchId AND p.ScopeName=@ScopeName)
        - ISNULL(ac.ApprovedAccountCount,0),
       (SELECT COUNT(*) FROM dbo.PropertyMigrationCashingType8ReceiptCandidate c WHERE c.MigrationBatchId=@BatchId AND c.HasContractLink=1 AND c.HasInstallmentLink=1 AND c.HasRenterLink=1 AND c.IsBalancedJournal=1)
        - ISNULL(r.ReadyReceipts,0)
FROM ScenarioNames sn
LEFT JOIN ScenarioApprovedCounts ac ON ac.ScenarioName=sn.ScenarioName
LEFT JOIN Ready r ON r.ScenarioName=sn.ScenarioName;

IF @ApplyTopN > 0
BEGIN
    EXEC sp_executesql N'
    INSERT INTO dbo.PropertyMigrationAccountFinanceApproval
    (BatchId,CustomerCode,SourceAccountCode,SourceAccountName,SuggestedTargetAccountSerial,ApprovedTargetAccountSerial,Decision,ApprovedBy,ApprovedAt,Notes,Status,ScopeName,ApprovalBatchId)
    SELECT @BatchId,@CustomerCode,p.SourceAccountCode,p.SourceAccountName,p.SuggestedTargetAccountSerial,p.SuggestedTargetAccountSerial,N''Approved'',N''FinanceApprovalScope:RSMDB_CashingType8'',GETDATE(),N''Applied by RSMDB CashingType8 Finance Approval script. Scope-limited approval.'',N''Approved'',@ScopeName,@ApprovalBatchId
    FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack p
    WHERE p.MigrationBatchId=@BatchId AND p.ScopeName=@ScopeName
      AND p.PriorityRank<=@ApplyTopN
      AND p.SuggestedTargetAccountId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.PropertyMigrationAccountFinanceApproval a WHERE a.BatchId=@BatchId AND a.ScopeName=@ScopeName AND a.SourceAccountCode=p.SourceAccountCode COLLATE DATABASE_DEFAULT AND a.Status=N''Approved'');',
    N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@ScopeName nvarchar(100),@ApprovalBatchId uniqueidentifier,@ApplyTopN int',
    @BatchId,@CustomerCode,@ScopeName,@ApprovalBatchId,@ApplyTopN;

    INSERT INTO dbo.PropertyMigrationAccountResolution
    (MigrationBatchId,CustomerCode,SourceAccountCode,TargetAccountId,TargetAccountCode,ResolutionStatus,ConfidenceScore,ApprovedBy,ApprovedAt,Notes)
    SELECT @BatchId,@CustomerCode,p.SourceAccountCode,p.SuggestedTargetAccountId,p.SuggestedTargetAccountSerial,N'Approved',100,N'FinanceApprovalScope:RSMDB_CashingType8',GETDATE(),N'Scope-limited CashingType8 approval.'
    FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack p
    WHERE p.MigrationBatchId=@BatchId AND p.ScopeName=@ScopeName
      AND p.PriorityRank<=@ApplyTopN
      AND p.SuggestedTargetAccountId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.PropertyMigrationAccountResolution r WHERE r.MigrationBatchId=@BatchId AND r.SourceAccountCode=p.SourceAccountCode COLLATE DATABASE_DEFAULT AND r.ResolutionStatus=N'Approved');
END;

SELECT COUNT(*) AS FinancePackAccounts FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack WHERE MigrationBatchId=@BatchId AND ScopeName=@ScopeName;
SELECT ScenarioName,ApprovedAccountCount,ReadyReceipts,ReadyJournals,ReadyJournalLines,ReadyValue,AccountsStillReview,BlockedReceipts
FROM dbo.PropertyMigrationCashingType8ApprovalSimulation
WHERE MigrationBatchId=@BatchId AND ScopeName=@ScopeName
ORDER BY CASE ScenarioName WHEN N'Top25' THEN 1 WHEN N'Top50' THEN 2 WHEN N'Top100' THEN 3 ELSE 4 END;
SELECT TOP 100 * FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack WHERE MigrationBatchId=@BatchId AND ScopeName=@ScopeName ORDER BY PriorityRank;
EXEC sp_executesql N'SELECT AppliedApprovals=COUNT(*) FROM dbo.PropertyMigrationAccountFinanceApproval WHERE BatchId=@BatchId AND ScopeName=@ScopeName AND ApprovalBatchId=@ApprovalBatchId;',
    N'@BatchId uniqueidentifier,@ScopeName nvarchar(100),@ApprovalBatchId uniqueidentifier',
    @BatchId,@ScopeName,@ApprovalBatchId;
