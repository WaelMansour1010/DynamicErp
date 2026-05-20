/*
RSMDB CashingType=8 Receipt Candidate Build - 2026-05-20
Candidate set + validation only. No migration, no posting, no receipt/journal creation.
Target clone only. RSMDB is read-only through SELECT queries.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET CONCAT_NULL_YIELDS_NULL ON;

DECLARE @BatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50) = N'$(CustomerCode)';

IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN RAISERROR('CashingType8 candidate build requires clone/sandbox target database.',16,1); RETURN; END;

IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp')
BEGIN RAISERROR('Blocked database for CashingType8 candidate build.',16,1); RETURN; END;

IF OBJECT_ID('dbo.PropertyMigrationCashingType8ReceiptCandidate','U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationCashingType8ReceiptCandidate
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(50) NOT NULL,
        SourceDatabaseName nvarchar(128) NOT NULL,
        ReceiptNoteId int NOT NULL,
        NoteSerial nvarchar(255) NULL,
        NoteSerial1 nvarchar(255) NULL,
        ReceiptDate datetime NULL,
        ReceiptAmount decimal(18,4) NULL,
        CashingType int NULL,
        SourceContractId nvarchar(400) NULL,
        SourceRenterId nvarchar(400) NULL,
        SourcePropertyId nvarchar(400) NULL,
        SourceUnitId nvarchar(400) NULL,
        AllocationRows int NOT NULL DEFAULT(0),
        DistinctInstallments int NOT NULL DEFAULT(0),
        AllocationAmount decimal(18,4) NULL,
        EarliestInstallmentDueDate datetime NULL,
        LatestInstallmentDueDate datetime NULL,
        HasContractLink bit NOT NULL DEFAULT(0),
        HasRenterLink bit NOT NULL DEFAULT(0),
        HasInstallmentLink bit NOT NULL DEFAULT(0),
        HasPropertyLink bit NOT NULL DEFAULT(0),
        HasUnitLink bit NOT NULL DEFAULT(0),
        JournalLineCount int NOT NULL DEFAULT(0),
        JournalDebit decimal(18,4) NULL,
        JournalCredit decimal(18,4) NULL,
        IsBalancedJournal bit NOT NULL DEFAULT(0),
        HasUnknownDirection bit NOT NULL DEFAULT(0),
        HasNullOrUnmappedAccount bit NOT NULL DEFAULT(0),
        AllAccountsFinanceApproved bit NOT NULL DEFAULT(0),
        UnmappedAccountCount int NOT NULL DEFAULT(0),
        ApprovedAccountCount int NOT NULL DEFAULT(0),
        ConfidenceScore int NOT NULL DEFAULT(0),
        Classification nvarchar(50) NOT NULL,
        EvidenceSummary nvarchar(4000) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID('dbo.PropertyMigrationCashingType8FinanceReviewAccount','U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationCashingType8FinanceReviewAccount
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(50) NOT NULL,
        SourceAccountCode nvarchar(255) NOT NULL,
        SourceAccountName nvarchar(500) NULL,
        UsageReceiptCount int NOT NULL DEFAULT(0),
        UsageJournalLineCount int NOT NULL DEFAULT(0),
        TotalDebit decimal(18,4) NULL,
        TotalCredit decimal(18,4) NULL,
        CandidateClassImpact nvarchar(100) NULL,
        SuggestedDecision nvarchar(50) NOT NULL DEFAULT(N'NeedsFinanceApproval'),
        SampleReceiptIds nvarchar(1000) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

DELETE FROM dbo.PropertyMigrationCashingType8ReceiptCandidate WHERE MigrationBatchId=@BatchId;
DELETE FROM dbo.PropertyMigrationCashingType8FinanceReviewAccount WHERE MigrationBatchId=@BatchId;

;WITH ReceiptBase AS
(
    SELECT n.NoteID,n.NoteSerial,n.NoteSerial1,n.NoteDate,n.Note_Value,n.CashingType,n.ContNo,n.CusID,n.akarid,n.UnitNo
    FROM RSMDB.dbo.Notes n
    WHERE n.NoteType=4
      AND n.CashingType=8
      AND n.Note_Value IS NOT NULL
      AND n.NoteDate IS NOT NULL
      AND EXISTS (SELECT 1 FROM RSMDB.dbo.ContracttBillInstallmentsDone d WHERE d.NoteID=n.NoteID)
), AllocationAgg AS
(
    SELECT d.NoteID,
           COUNT(*) AllocationRows,
           COUNT(DISTINCT d.istallid) DistinctInstallments,
           SUM(CONVERT(decimal(18,4),ISNULL(d.Value,ISNULL(d.total,0)))) AllocationAmount,
           MIN(ci.Installdate) EarliestDue,
           MAX(ci.Installdate) LatestDue
    FROM RSMDB.dbo.ContracttBillInstallmentsDone d
    LEFT JOIN RSMDB.dbo.TblContractInstallments ci ON ci.id=d.istallid
    GROUP BY d.NoteID
), JournalAgg AS
(
    SELECT dev.Notes_ID NoteID,
           COUNT(*) LineCount,
           SUM(CASE WHEN dev.Credit_Or_Debit=0 THEN CONVERT(decimal(18,4),ISNULL(dev.Value,0)) ELSE 0 END) Debit,
           SUM(CASE WHEN dev.Credit_Or_Debit=1 THEN CONVERT(decimal(18,4),ISNULL(dev.Value,0)) ELSE 0 END) Credit,
           SUM(CASE WHEN dev.Credit_Or_Debit NOT IN (0,1) OR dev.Credit_Or_Debit IS NULL THEN 1 ELSE 0 END) UnknownDirection,
           SUM(CASE WHEN dev.Account_Code IS NULL OR LTRIM(RTRIM(dev.Account_Code))='' THEN 1 ELSE 0 END) NullAccount,
           COUNT(DISTINCT dev.Account_Code) DistinctAccounts,
           COUNT(DISTINCT ar.SourceAccountCode) ApprovedAccounts,
           SUM(CASE WHEN ar.SourceAccountCode IS NULL THEN 1 ELSE 0 END) UnmappedLines
    FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS dev
    LEFT JOIN dbo.PropertyMigrationAccountResolution ar
      ON ar.MigrationBatchId=@BatchId
     AND ar.SourceAccountCode=dev.Account_Code COLLATE DATABASE_DEFAULT
     AND ar.ResolutionStatus=N'Approved'
     AND ar.TargetAccountId IS NOT NULL
    GROUP BY dev.Notes_ID
), EntityLinks AS
(
    SELECT rb.NoteID,
           MAX(CASE WHEN c.TargetId IS NOT NULL THEN 1 ELSE 0 END) HasContractLink,
           MAX(CASE WHEN r.TargetId IS NOT NULL THEN 1 ELSE 0 END) HasRenterLink,
           MAX(CASE WHEN p.TargetId IS NOT NULL THEN 1 ELSE 0 END) HasPropertyLink,
           MAX(CASE WHEN u.TargetId IS NOT NULL THEN 1 ELSE 0 END) HasUnitLink,
           MAX(CASE WHEN im.TargetId IS NOT NULL THEN 1 ELSE 0 END) HasInstallmentLink
    FROM ReceiptBase rb
    LEFT JOIN dbo.PropertyMigrationEntityMap c ON c.MigrationBatchId=@BatchId AND c.EntityType=N'Contract' AND c.SourceId=CONVERT(nvarchar(400),rb.ContNo)
    LEFT JOIN dbo.PropertyMigrationEntityMap r ON r.MigrationBatchId=@BatchId AND r.EntityType=N'Renter' AND r.SourceId=CONVERT(nvarchar(400),rb.CusID)
    LEFT JOIN dbo.PropertyMigrationEntityMap p ON p.MigrationBatchId=@BatchId AND p.EntityType=N'Property' AND p.SourceId=CONVERT(nvarchar(400),rb.akarid)
    LEFT JOIN dbo.PropertyMigrationEntityMap u ON u.MigrationBatchId=@BatchId AND u.EntityType=N'Unit' AND u.SourceId=CONVERT(nvarchar(400),rb.UnitNo)
    LEFT JOIN RSMDB.dbo.ContracttBillInstallmentsDone d ON d.NoteID=rb.NoteID
    LEFT JOIN dbo.PropertyMigrationEntityMap im ON im.MigrationBatchId=@BatchId AND im.EntityType=N'Installment' AND im.SourceId=CONVERT(nvarchar(400),d.istallid)
    GROUP BY rb.NoteID
)
INSERT INTO dbo.PropertyMigrationCashingType8ReceiptCandidate
(
    MigrationBatchId,CustomerCode,SourceDatabaseName,ReceiptNoteId,NoteSerial,NoteSerial1,ReceiptDate,ReceiptAmount,CashingType,
    SourceContractId,SourceRenterId,SourcePropertyId,SourceUnitId,AllocationRows,DistinctInstallments,AllocationAmount,EarliestInstallmentDueDate,LatestInstallmentDueDate,
    HasContractLink,HasRenterLink,HasInstallmentLink,HasPropertyLink,HasUnitLink,JournalLineCount,JournalDebit,JournalCredit,IsBalancedJournal,
    HasUnknownDirection,HasNullOrUnmappedAccount,AllAccountsFinanceApproved,UnmappedAccountCount,ApprovedAccountCount,ConfidenceScore,Classification,EvidenceSummary
)
SELECT @BatchId,@CustomerCode,N'RSMDB',rb.NoteID,
       CONVERT(nvarchar(255),CONVERT(bigint,rb.NoteSerial)),CONVERT(nvarchar(255),CONVERT(bigint,rb.NoteSerial1)),rb.NoteDate,CONVERT(decimal(18,4),rb.Note_Value),rb.CashingType,
       CONVERT(nvarchar(400),rb.ContNo),CONVERT(nvarchar(400),rb.CusID),CONVERT(nvarchar(400),rb.akarid),CONVERT(nvarchar(400),rb.UnitNo),
       aa.AllocationRows,aa.DistinctInstallments,aa.AllocationAmount,aa.EarliestDue,aa.LatestDue,
       CONVERT(bit,ISNULL(el.HasContractLink,0)),CONVERT(bit,ISNULL(el.HasRenterLink,0)),CONVERT(bit,ISNULL(el.HasInstallmentLink,0)),CONVERT(bit,ISNULL(el.HasPropertyLink,0)),CONVERT(bit,ISNULL(el.HasUnitLink,0)),
       ISNULL(ja.LineCount,0),ja.Debit,ja.Credit,
       CONVERT(bit,CASE WHEN ISNULL(ja.LineCount,0)>0 AND ISNULL(ja.UnknownDirection,0)=0 AND ABS(ISNULL(ja.Debit,0)-ISNULL(ja.Credit,0))<=0.01 THEN 1 ELSE 0 END),
       CONVERT(bit,CASE WHEN ISNULL(ja.UnknownDirection,0)>0 THEN 1 ELSE 0 END),
       CONVERT(bit,CASE WHEN ISNULL(ja.NullAccount,0)>0 OR ISNULL(ja.UnmappedLines,0)>0 THEN 1 ELSE 0 END),
       CONVERT(bit,CASE WHEN ISNULL(ja.LineCount,0)>0 AND ISNULL(ja.NullAccount,0)=0 AND ISNULL(ja.UnmappedLines,0)=0 THEN 1 ELSE 0 END),
       ISNULL(ja.UnmappedLines,0),ISNULL(ja.ApprovedAccounts,0),
       CASE
         WHEN ISNULL(el.HasContractLink,0)=1 AND ISNULL(el.HasRenterLink,0)=1 AND ISNULL(el.HasInstallmentLink,0)=1 AND ISNULL(ja.LineCount,0)>0 AND ISNULL(ja.NullAccount,0)=0 AND ISNULL(ja.UnmappedLines,0)=0 AND ISNULL(ja.UnknownDirection,0)=0 AND ABS(ISNULL(ja.Debit,0)-ISNULL(ja.Credit,0))<=0.01 THEN 100
         WHEN ISNULL(el.HasContractLink,0)=1 AND ISNULL(el.HasRenterLink,0)=1 AND ISNULL(el.HasInstallmentLink,0)=1 AND ISNULL(ja.LineCount,0)>0 AND ISNULL(ja.UnknownDirection,0)=0 AND ABS(ISNULL(ja.Debit,0)-ISNULL(ja.Credit,0))<=0.01 THEN 80
         WHEN ISNULL(el.HasContractLink,0)=1 AND ISNULL(el.HasRenterLink,0)=1 AND ISNULL(el.HasInstallmentLink,0)=1 THEN 70
         ELSE 30
       END,
       CASE
         WHEN ISNULL(el.HasContractLink,0)=1 AND ISNULL(el.HasRenterLink,0)=1 AND ISNULL(el.HasInstallmentLink,0)=1 AND ISNULL(ja.LineCount,0)>0 AND ISNULL(ja.NullAccount,0)=0 AND ISNULL(ja.UnmappedLines,0)=0 AND ISNULL(ja.UnknownDirection,0)=0 AND ABS(ISNULL(ja.Debit,0)-ISNULL(ja.Credit,0))<=0.01 THEN N'ReadyForAccountingPilot'
         WHEN ISNULL(el.HasContractLink,0)=1 AND ISNULL(el.HasRenterLink,0)=1 AND ISNULL(el.HasInstallmentLink,0)=1 AND ISNULL(ja.LineCount,0)>0 AND ISNULL(ja.UnknownDirection,0)=0 AND ABS(ISNULL(ja.Debit,0)-ISNULL(ja.Credit,0))<=0.01 THEN N'NeedsFinanceApproval'
         WHEN ISNULL(el.HasContractLink,0)=0 OR ISNULL(el.HasRenterLink,0)=0 OR ISNULL(el.HasInstallmentLink,0)=0 THEN N'NeedsLinkReview'
         ELSE N'Blocked'
       END,
       N'Evidence=ContracttBillInstallmentsDone; CashingType=8; AllocationRows=' + CONVERT(nvarchar(20),aa.AllocationRows)
       + N'; EntityLinks C/R/I=' + CONVERT(nvarchar(1),ISNULL(el.HasContractLink,0)) + N'/' + CONVERT(nvarchar(1),ISNULL(el.HasRenterLink,0)) + N'/' + CONVERT(nvarchar(1),ISNULL(el.HasInstallmentLink,0))
       + N'; JournalLines=' + CONVERT(nvarchar(20),ISNULL(ja.LineCount,0))
       + N'; UnmappedLines=' + CONVERT(nvarchar(20),ISNULL(ja.UnmappedLines,0))
FROM ReceiptBase rb
JOIN AllocationAgg aa ON aa.NoteID=rb.NoteID
LEFT JOIN JournalAgg ja ON ja.NoteID=rb.NoteID
LEFT JOIN EntityLinks el ON el.NoteID=rb.NoteID;

;WITH CandidateAccounts AS
(
    SELECT dev.Account_Code,
           MAX(acc.Account_Name) AccountName,
           COUNT(DISTINCT c.ReceiptNoteId) ReceiptCount,
           COUNT(*) LineCount,
           SUM(CASE WHEN dev.Credit_Or_Debit=0 THEN CONVERT(decimal(18,4),ISNULL(dev.Value,0)) ELSE 0 END) TotalDebit,
           SUM(CASE WHEN dev.Credit_Or_Debit=1 THEN CONVERT(decimal(18,4),ISNULL(dev.Value,0)) ELSE 0 END) TotalCredit,
           MAX(CASE WHEN c.Classification=N'NeedsFinanceApproval' THEN 1 ELSE 0 END) ImpactsFinanceReadySet,
           STUFF((SELECT TOP 10 ',' + CONVERT(nvarchar(20),c2.ReceiptNoteId)
                  FROM dbo.PropertyMigrationCashingType8ReceiptCandidate c2
                  JOIN RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS dev2 ON dev2.Notes_ID=c2.ReceiptNoteId
                  WHERE c2.MigrationBatchId=@BatchId AND dev2.Account_Code COLLATE DATABASE_DEFAULT=dev.Account_Code COLLATE DATABASE_DEFAULT
                  GROUP BY c2.ReceiptNoteId
                  ORDER BY c2.ReceiptNoteId
                  FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,'') SampleIds
    FROM dbo.PropertyMigrationCashingType8ReceiptCandidate c
    JOIN RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS dev ON dev.Notes_ID=c.ReceiptNoteId
    LEFT JOIN RSMDB.dbo.Accounts acc ON acc.Account_Code COLLATE DATABASE_DEFAULT=dev.Account_Code COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.PropertyMigrationAccountResolution ar ON ar.MigrationBatchId=@BatchId AND ar.SourceAccountCode=dev.Account_Code COLLATE DATABASE_DEFAULT AND ar.ResolutionStatus=N'Approved'
    WHERE c.MigrationBatchId=@BatchId
      AND ar.SourceAccountCode IS NULL
      AND dev.Account_Code IS NOT NULL
    GROUP BY dev.Account_Code
)
INSERT INTO dbo.PropertyMigrationCashingType8FinanceReviewAccount
(MigrationBatchId,CustomerCode,SourceAccountCode,SourceAccountName,UsageReceiptCount,UsageJournalLineCount,TotalDebit,TotalCredit,CandidateClassImpact,SuggestedDecision,SampleReceiptIds)
SELECT @BatchId,@CustomerCode,Account_Code,AccountName,ReceiptCount,LineCount,TotalDebit,TotalCredit,
       CASE WHEN ImpactsFinanceReadySet=1 THEN N'Can unlock linked balanced receipts' ELSE N'Broader CashingType8 coverage' END,
       N'NeedsFinanceApproval',SampleIds
FROM CandidateAccounts
ORDER BY ReceiptCount DESC, (TotalDebit+TotalCredit) DESC;

SELECT Classification, COUNT(*) Receipts, SUM(ReceiptAmount) ReceiptAmount, SUM(JournalDebit) JournalDebit, SUM(JournalCredit) JournalCredit
FROM dbo.PropertyMigrationCashingType8ReceiptCandidate
WHERE MigrationBatchId=@BatchId
GROUP BY Classification
ORDER BY CASE Classification WHEN N'ReadyForAccountingPilot' THEN 1 WHEN N'NeedsFinanceApproval' THEN 2 WHEN N'NeedsLinkReview' THEN 3 ELSE 4 END;

SELECT
    TotalCandidates=COUNT(*),
    WithContractInstallmentDone=SUM(CASE WHEN AllocationRows>0 THEN 1 ELSE 0 END),
    WithContractLink=SUM(CASE WHEN HasContractLink=1 THEN 1 ELSE 0 END),
    WithInstallmentLink=SUM(CASE WHEN HasInstallmentLink=1 THEN 1 ELSE 0 END),
    WithRenterLink=SUM(CASE WHEN HasRenterLink=1 THEN 1 ELSE 0 END),
    WithFinanceApprovedAccounts=SUM(CASE WHEN AllAccountsFinanceApproved=1 THEN 1 ELSE 0 END),
    WithBalancedJournal=SUM(CASE WHEN IsBalancedJournal=1 THEN 1 ELSE 0 END),
    ReadyForAccountingPilot=SUM(CASE WHEN Classification=N'ReadyForAccountingPilot' THEN 1 ELSE 0 END),
    ReadyValue=SUM(CASE WHEN Classification=N'ReadyForAccountingPilot' THEN ReceiptAmount ELSE 0 END),
    ReadyJournals=SUM(CASE WHEN Classification=N'ReadyForAccountingPilot' THEN 1 ELSE 0 END),
    ReadyJournalLines=SUM(CASE WHEN Classification=N'ReadyForAccountingPilot' THEN JournalLineCount ELSE 0 END),
    ReadyContracts=COUNT(DISTINCT CASE WHEN Classification=N'ReadyForAccountingPilot' THEN SourceContractId END),
    ReadyRenters=COUNT(DISTINCT CASE WHEN Classification=N'ReadyForAccountingPilot' THEN SourceRenterId END),
    ReadyInstallments=COUNT(DISTINCT CASE WHEN Classification=N'ReadyForAccountingPilot' THEN ReceiptNoteId END)
FROM dbo.PropertyMigrationCashingType8ReceiptCandidate
WHERE MigrationBatchId=@BatchId;

SELECT TOP 100 *
FROM dbo.PropertyMigrationCashingType8FinanceReviewAccount
WHERE MigrationBatchId=@BatchId
ORDER BY UsageReceiptCount DESC, ISNULL(TotalDebit,0)+ISNULL(TotalCredit,0) DESC;

