SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET CONCAT_NULL_YIELDS_NULL ON;

/*
RSMDB Receipt Allocation Discovery - 2026-05-20
Purpose: discovery/enrichment only. No migration, no receipts, no journals, no posting.
Target: clone/sandbox only. Source RSMDB is read-only through SELECT statements.
*/
DECLARE @BatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50) = N'$(CustomerCode)';

IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN
    RAISERROR('Receipt allocation discovery requires a clone/sandbox target database.',16,1);
    RETURN;
END;

IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp')
BEGIN
    RAISERROR('Blocked database for receipt allocation discovery.',16,1);
    RETURN;
END;

IF OBJECT_ID('dbo.PropertyMigrationReceiptAllocationEvidence','U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationReceiptAllocationEvidence
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(50) NOT NULL,
        SourceDatabaseName nvarchar(128) NOT NULL,
        ReceiptNoteId int NOT NULL,
        NoteType int NULL,
        CashingType int NULL,
        NoteSerial nvarchar(255) NULL,
        NoteSerial1 nvarchar(255) NULL,
        ReceiptAmount decimal(18,4) NULL,
        ReceiptDate datetime NULL,
        Remark nvarchar(4000) NULL,
        DirectContNo int NULL,
        DirectCusId int NULL,
        DirectAqarId int NULL,
        DirectUnitNo int NULL,
        DoneInstallmentRows int NOT NULL DEFAULT(0),
        DoneInstallmentIds nvarchar(1000) NULL,
        AllocationHeaderRows int NOT NULL DEFAULT(0),
        AllocationDetailRows int NOT NULL DEFAULT(0),
        ReciveDetailsRows int NOT NULL DEFAULT(0),
        ReciveDetailsHasContractEvidence bit NOT NULL DEFAULT(0),
        UniqueTextCustomerMatches int NOT NULL DEFAULT(0),
        ExistingAmountDateCandidates int NOT NULL DEFAULT(0),
        ExistingBestLinkBand nvarchar(50) NULL,
        SuggestedContractId nvarchar(400) NULL,
        SuggestedInstallmentId nvarchar(400) NULL,
        SuggestedRenterId nvarchar(400) NULL,
        EvidenceSummary nvarchar(4000) NULL,
        ConfidenceScore int NOT NULL DEFAULT(0),
        Decision nvarchar(50) NOT NULL,
        RequiresManualReview bit NOT NULL DEFAULT(1),
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;

DELETE FROM dbo.PropertyMigrationReceiptAllocationEvidence WHERE MigrationBatchId=@BatchId;

;WITH Ready AS
(
    SELECT CONVERT(int, r.SourceId) AS NoteID
    FROM dbo.PropertyMigrationResolutionResult r
    JOIN RSMDB.dbo.Notes n ON n.NoteID = CONVERT(int, r.SourceId)
    WHERE r.MigrationBatchId=@BatchId
      AND r.EntityType=N'Journal'
      AND r.ResolutionStatus=N'ReadyForMigration_FinanceApproved'
      AND n.NoteType=4
), NoteBase AS
(
    SELECT n.NoteID,n.NoteType,n.CashingType,
           CONVERT(nvarchar(255),CONVERT(bigint,n.NoteSerial)) AS NoteSerial,
           CONVERT(nvarchar(255),CONVERT(bigint,n.NoteSerial1)) AS NoteSerial1,
           CONVERT(decimal(18,4),ISNULL(n.Note_Value,0)) AS ReceiptAmount,
           n.NoteDate AS ReceiptDate,
           CONVERT(nvarchar(4000),n.Remark) AS Remark,
           n.ContNo AS DirectContNo,
           n.CusID AS DirectCusId,
           n.akarid AS DirectAqarId,
           n.UnitNo AS DirectUnitNo
    FROM Ready r
    JOIN RSMDB.dbo.Notes n ON n.NoteID=r.NoteID
), DoneAgg AS
(
    SELECT d.NoteID,
           COUNT(*) AS DoneRows,
           STUFF((SELECT ',' + CONVERT(nvarchar(20),d2.istallid)
                  FROM RSMDB.dbo.ContracttBillInstallmentsDone d2
                  WHERE d2.NoteID=d.NoteID
                  FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,'') AS InstallmentIds
    FROM RSMDB.dbo.ContracttBillInstallmentsDone d
    GROUP BY d.NoteID
), ReciveAgg AS
(
    SELECT nb.NoteID, COUNT(*) AS ReciveRows
    FROM NoteBase nb
    JOIN RSMDB.dbo.ReciveDetails rd
      ON rd.NoteSerial1=nb.NoteSerial1 OR rd.NoteSerial=nb.NoteSerial
    GROUP BY nb.NoteID
), AllocationAgg AS
(
    SELECT nb.NoteID,
           SUM(CASE WHEN a.transID IS NOT NULL THEN 1 ELSE 0 END) + SUM(CASE WHEN a1.transID IS NOT NULL THEN 1 ELSE 0 END) AS HeaderRows,
           0 AS DetailRows
    FROM NoteBase nb
    LEFT JOIN RSMDB.dbo.tblContractInsAllocations a
      ON a.NoteID=nb.NoteID OR CONVERT(nvarchar(255),a.NoteSerial)=nb.NoteSerial OR CONVERT(nvarchar(255),a.NoteSerial)=nb.NoteSerial1
    LEFT JOIN RSMDB.dbo.tblContractInsAllocations1 a1
      ON a1.NoteID=nb.NoteID OR CONVERT(nvarchar(255),a1.NoteSerial)=nb.NoteSerial OR CONVERT(nvarchar(255),a1.NoteSerial)=nb.NoteSerial1
    GROUP BY nb.NoteID
), TextCustomer AS
(
    SELECT nb.NoteID, COUNT(DISTINCT c.CusID) AS CustomerMatches
    FROM NoteBase nb
    JOIN RSMDB.dbo.TblCustemers c
      ON LEN(ISNULL(c.CusName,N'')) >= 6
     AND nb.Remark IS NOT NULL
     AND nb.Remark LIKE N'%' + c.CusName + N'%'
    GROUP BY nb.NoteID
), ExistingCandidates AS
(
    SELECT CONVERT(int, SourceReceiptId) AS ReceiptNoteId,
           COUNT(*) AS CandidateRows,
           MAX(LinkBand) AS BestBand,
           MAX(SuggestedContractSourceId) AS SuggestedContractId,
           MAX(SuggestedInstallmentSourceId) AS SuggestedInstallmentId,
           MAX(SuggestedRenterSourceId) AS SuggestedRenterId
    FROM dbo.PropertyMigrationReceiptLinkCandidate
    WHERE MigrationBatchId=@BatchId
    GROUP BY SourceReceiptId
)
INSERT INTO dbo.PropertyMigrationReceiptAllocationEvidence
(
    MigrationBatchId,CustomerCode,SourceDatabaseName,ReceiptNoteId,NoteType,CashingType,NoteSerial,NoteSerial1,ReceiptAmount,ReceiptDate,Remark,
    DirectContNo,DirectCusId,DirectAqarId,DirectUnitNo,DoneInstallmentRows,DoneInstallmentIds,AllocationHeaderRows,AllocationDetailRows,
    ReciveDetailsRows,ReciveDetailsHasContractEvidence,UniqueTextCustomerMatches,ExistingAmountDateCandidates,ExistingBestLinkBand,
    SuggestedContractId,SuggestedInstallmentId,SuggestedRenterId,EvidenceSummary,ConfidenceScore,Decision,RequiresManualReview
)
SELECT @BatchId,@CustomerCode,N'RSMDB',nb.NoteID,nb.NoteType,nb.CashingType,nb.NoteSerial,nb.NoteSerial1,nb.ReceiptAmount,nb.ReceiptDate,nb.Remark,
       nb.DirectContNo,nb.DirectCusId,nb.DirectAqarId,nb.DirectUnitNo,
       ISNULL(da.DoneRows,0),da.InstallmentIds,ISNULL(aa.HeaderRows,0),ISNULL(aa.DetailRows,0),
       ISNULL(ra.ReciveRows,0),CONVERT(bit,0),ISNULL(tc.CustomerMatches,0),ISNULL(ec.CandidateRows,0),ec.BestBand,
       ec.SuggestedContractId,ec.SuggestedInstallmentId,ec.SuggestedRenterId,
       N'CashingType=' + ISNULL(CONVERT(nvarchar(20),nb.CashingType),N'NULL')
       + N'; DirectContNo=' + ISNULL(CONVERT(nvarchar(20),nb.DirectContNo),N'NULL')
       + N'; DoneRows=' + CONVERT(nvarchar(20),ISNULL(da.DoneRows,0))
       + N'; ReciveDetailsRows=' + CONVERT(nvarchar(20),ISNULL(ra.ReciveRows,0))
       + N'; AllocationHeaderRows=' + CONVERT(nvarchar(20),ISNULL(aa.HeaderRows,0))
       + N'; TextCustomerMatches=' + CONVERT(nvarchar(20),ISNULL(tc.CustomerMatches,0))
       + N'; ExistingAmountDateCandidates=' + CONVERT(nvarchar(20),ISNULL(ec.CandidateRows,0)),
       CASE
          WHEN nb.CashingType=8 AND nb.DirectContNo IS NOT NULL AND ISNULL(da.DoneRows,0)>0 THEN 100
          WHEN nb.CashingType=8 AND nb.DirectContNo IS NOT NULL THEN 90
          WHEN ISNULL(da.DoneRows,0)>0 THEN 85
          WHEN ec.BestBand=N'MediumReview' THEN 65
          WHEN ec.BestBand=N'WeakMatch' THEN 45
          ELSE 20
       END,
       CASE
          WHEN nb.CashingType=8 AND nb.DirectContNo IS NOT NULL AND ISNULL(da.DoneRows,0)>0 THEN N'AutoApprovedLink'
          WHEN nb.CashingType=8 AND nb.DirectContNo IS NOT NULL THEN N'HighConfidence'
          WHEN ISNULL(da.DoneRows,0)>0 THEN N'HighConfidence'
          WHEN ec.BestBand=N'MediumReview' THEN N'MediumReview'
          WHEN ec.BestBand=N'WeakMatch' THEN N'WeakMatch'
          ELSE N'Blocked'
       END,
       CASE
          WHEN nb.CashingType=8 AND nb.DirectContNo IS NOT NULL AND ISNULL(da.DoneRows,0)>0 THEN 0
          ELSE 1
       END
FROM NoteBase nb
LEFT JOIN DoneAgg da ON da.NoteID=nb.NoteID
LEFT JOIN ReciveAgg ra ON ra.NoteID=nb.NoteID
LEFT JOIN AllocationAgg aa ON aa.NoteID=nb.NoteID
LEFT JOIN TextCustomer tc ON tc.NoteID=nb.NoteID
LEFT JOIN ExistingCandidates ec ON ec.ReceiptNoteId=nb.NoteID;

SELECT Decision, COUNT(*) AS CountRows, MIN(ConfidenceScore) AS MinScore, MAX(ConfidenceScore) AS MaxScore
FROM dbo.PropertyMigrationReceiptAllocationEvidence
WHERE MigrationBatchId=@BatchId
GROUP BY Decision
ORDER BY CASE Decision WHEN N'AutoApprovedLink' THEN 1 WHEN N'HighConfidence' THEN 2 WHEN N'MediumReview' THEN 3 WHEN N'WeakMatch' THEN 4 ELSE 5 END;

SELECT
    Total=COUNT(*),
    CashingType8=SUM(CASE WHEN CashingType=8 THEN 1 ELSE 0 END),
    CashingType7=SUM(CASE WHEN CashingType=7 THEN 1 ELSE 0 END),
    WithDirectContNo=SUM(CASE WHEN DirectContNo IS NOT NULL THEN 1 ELSE 0 END),
    WithDoneRows=SUM(CASE WHEN DoneInstallmentRows>0 THEN 1 ELSE 0 END),
    WithAllocationHeader=SUM(CASE WHEN AllocationHeaderRows>0 THEN 1 ELSE 0 END),
    WithReciveDetails=SUM(CASE WHEN ReciveDetailsRows>0 THEN 1 ELSE 0 END),
    WithTextCustomer=SUM(CASE WHEN UniqueTextCustomerMatches>0 THEN 1 ELSE 0 END)
FROM dbo.PropertyMigrationReceiptAllocationEvidence
WHERE MigrationBatchId=@BatchId;

SELECT TOP 100 ReceiptNoteId,NoteSerial,NoteSerial1,ReceiptAmount,ReceiptDate,CashingType,DirectContNo,DirectCusId,DoneInstallmentRows,ReciveDetailsRows,AllocationHeaderRows,UniqueTextCustomerMatches,ExistingAmountDateCandidates,ExistingBestLinkBand,ConfidenceScore,Decision,Remark
FROM dbo.PropertyMigrationReceiptAllocationEvidence
WHERE MigrationBatchId=@BatchId
ORDER BY ReceiptNoteId;




