/*
RSMDB CashingType=8 Mini Accounting Pilot - 2026-05-20
Clone only. Executes a 32-receipt accounting pilot from the approved CashingType=8 ReadySet.
Source RSMDB is read-only. No Issues, OwnerPayments, Terminations, or 9088 are migrated.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @OperationalBatchId uniqueidentifier = '$(OperationalBatchId)';
DECLARE @PilotBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50) = N'$(CustomerCode)';
DECLARE @ExpectedReceipts int = 32;
DECLARE @ExpectedJournals int = 32;
DECLARE @ExpectedLines int = 64;
DECLARE @ExpectedValue decimal(18,4) = 966568.2500;
DECLARE @DefaultDepartmentId int = 44;
DECLARE @DefaultCashBoxId int = (SELECT TOP 1 Id FROM dbo.CashBox WHERE ISNULL(IsDeleted,0)=0 ORDER BY Id);

IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN RAISERROR('Mini accounting pilot requires clone/sandbox target database.',16,1); RETURN; END;
IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp')
BEGIN RAISERROR('Blocked database for mini accounting pilot.',16,1); RETURN; END;
IF @PilotBatchId=@OperationalBatchId
BEGIN RAISERROR('PilotBatchId must be different from OperationalBatchId to keep rollback isolated.',16,1); RETURN; END;

IF OBJECT_ID('dbo.PropertyMigrationCashingType8ReceiptCandidate','U') IS NULL
BEGIN RAISERROR('CashingType8 candidate table is missing.',16,1); RETURN; END;

IF OBJECT_ID('tempdb..#Ready') IS NOT NULL DROP TABLE #Ready;
SELECT c.*
INTO #Ready
FROM dbo.PropertyMigrationCashingType8ReceiptCandidate c
WHERE c.MigrationBatchId=@OperationalBatchId
  AND c.Classification=N'ReadyForAccountingPilot'
  AND c.CashingType=8
  AND c.HasContractLink=1
  AND c.HasInstallmentLink=1
  AND c.HasRenterLink=1
  AND c.IsBalancedJournal=1
  AND c.AllAccountsFinanceApproved=1
  AND c.HasUnknownDirection=0
  AND c.HasNullOrUnmappedAccount=0;

DECLARE @ReadyReceipts int=(SELECT COUNT(*) FROM #Ready);
DECLARE @ReadyJournals int=(SELECT COUNT(DISTINCT ReceiptNoteId) FROM #Ready);
DECLARE @ReadyLines int=(SELECT COUNT(*) FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS dev JOIN #Ready r ON r.ReceiptNoteId=dev.Notes_ID);
DECLARE @ReadyValue decimal(18,4)=(SELECT CONVERT(decimal(18,4),ISNULL(SUM(ReceiptAmount),0)) FROM #Ready);

IF @ReadyReceipts<>@ExpectedReceipts OR @ReadyJournals<>@ExpectedJournals OR @ReadyLines<>@ExpectedLines OR ABS(@ReadyValue-@ExpectedValue)>0.01
BEGIN
    RAISERROR('ReadySet mismatch. Expected 32 receipts/32 journals/64 lines/966568.2500.',16,1);
    SELECT ReadyReceipts=@ReadyReceipts,ReadyJournals=@ReadyJournals,ReadyLines=@ReadyLines,ReadyValue=@ReadyValue;
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.PropertyMigrationEntityMap m JOIN #Ready r ON m.SourceId=CONVERT(nvarchar(400),r.ReceiptNoteId) WHERE m.EntityType IN (N'Receipt',N'Journal') AND m.Status<>N'RolledBack')
BEGIN RAISERROR('Duplicate receipt/journal entity map found for pilot ReadySet.',16,1); RETURN; END;

IF EXISTS (
    SELECT 1
    FROM #Ready r
    JOIN RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS dev ON dev.Notes_ID=r.ReceiptNoteId
    LEFT JOIN dbo.PropertyMigrationAccountResolution ar ON ar.MigrationBatchId=@OperationalBatchId AND ar.SourceAccountCode=dev.Account_Code COLLATE DATABASE_DEFAULT AND ar.ResolutionStatus=N'Approved' AND ar.TargetAccountId IS NOT NULL
    WHERE ar.TargetAccountId IS NULL OR dev.Credit_Or_Debit NOT IN (0,1)
)
BEGIN RAISERROR('ReadySet contains unmapped account or unknown journal direction.',16,1); RETURN; END;

BEGIN TRAN;

DECLARE @ReceiptOut TABLE(SourceReceiptId int PRIMARY KEY, TargetReceiptId int);

MERGE dbo.CashReceiptVoucher AS tgt
USING (
    SELECT r.ReceiptNoteId,r.NoteSerial,r.ReceiptDate,r.ReceiptAmount,
           ContractId=cm.TargetId,
           RenterId=rm.TargetId,
           AccountId=(SELECT TOP 1 ar.TargetAccountId FROM RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS dev JOIN dbo.PropertyMigrationAccountResolution ar ON ar.MigrationBatchId=@OperationalBatchId AND ar.SourceAccountCode=dev.Account_Code COLLATE DATABASE_DEFAULT AND ar.ResolutionStatus=N'Approved' WHERE dev.Notes_ID=r.ReceiptNoteId AND dev.Credit_Or_Debit=1 ORDER BY dev.DEV_ID_Line_No),
           CashBoxId=@DefaultCashBoxId
    FROM #Ready r
    JOIN dbo.PropertyMigrationEntityMap cm ON cm.MigrationBatchId=@OperationalBatchId AND cm.EntityType=N'Contract' AND cm.SourceId=r.SourceContractId
    JOIN dbo.PropertyMigrationEntityMap rm ON rm.MigrationBatchId=@OperationalBatchId AND rm.EntityType=N'Renter' AND rm.SourceId=r.SourceRenterId
) AS src ON 1=0
WHEN NOT MATCHED THEN INSERT(DocumentNumber,BranchId,MoneyAmount,SourceTypeId,Date,AccountId,IsLinked,IsPosted,IsActive,IsDeleted,Notes,DepartmentId,CashBoxId,PropertyContractId,RenterId,IsAgainstOpeningBalance,VendorReceiptNumber)
VALUES(N'RSMDB-C8-' + CONVERT(nvarchar(50),src.ReceiptNoteId),NULL,src.ReceiptAmount,13,src.ReceiptDate,src.AccountId,1,1,1,0,N'RSMDB CashingType=8 mini accounting pilot. Source NoteID=' + CONVERT(nvarchar(50),src.ReceiptNoteId),@DefaultDepartmentId,src.CashBoxId,src.ContractId,src.RenterId,0,src.NoteSerial)
OUTPUT src.ReceiptNoteId, inserted.Id INTO @ReceiptOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @PilotBatchId,@CustomerCode,N'RSMDB',N'Notes',CONVERT(nvarchar(400),SourceReceiptId),N'CashReceiptVoucher',TargetReceiptId,N'Receipt',N'Inserted',0,0,N'RSMDB CashingType=8 mini accounting pilot receipt'
FROM @ReceiptOut;

INSERT INTO dbo.CashReceiptVoucherPropertyContractBatch(CashReceiptVoucherId,PropertyContractBatchId,IsDelivered,Paid,Remain)
SELECT ro.TargetReceiptId, im.TargetId, 1,
       CONVERT(money,ISNULL(d.Value,ISNULL(d.total,0))),
       CONVERT(money,CASE WHEN ISNULL(pcb.BatchTotal,0)-ISNULL(d.Value,ISNULL(d.total,0)) < 0 THEN 0 ELSE ISNULL(pcb.BatchTotal,0)-ISNULL(d.Value,ISNULL(d.total,0)) END)
FROM @ReceiptOut ro
JOIN RSMDB.dbo.ContracttBillInstallmentsDone d ON d.NoteID=ro.SourceReceiptId
JOIN dbo.PropertyMigrationEntityMap im ON im.MigrationBatchId=@OperationalBatchId AND im.EntityType=N'Installment' AND im.SourceId=CONVERT(nvarchar(400),d.istallid)
LEFT JOIN dbo.PropertyContractBatch pcb ON pcb.Id=im.TargetId;

DECLARE @JournalOut TABLE(SourceReceiptId int PRIMARY KEY, TargetJournalId int);

MERGE dbo.JournalEntry AS tgt
USING (
    SELECT r.ReceiptNoteId, r.NoteSerial, r.ReceiptDate, ro.TargetReceiptId,
           DocumentNumber = N'RSMDB-C8-JE-' + CONVERT(nvarchar(50),r.ReceiptNoteId)
    FROM #Ready r
    JOIN @ReceiptOut ro ON ro.SourceReceiptId=r.ReceiptNoteId
) AS src ON 1=0
WHEN NOT MATCHED THEN INSERT(DocumentNumber,BranchId,Date,Notes,SourceId,IsActive,IsPosted,IsDeleted,DepartmentId,OriginalDocumentNumber,OriginalNoteId,OriginalNoteType,OriginalSerial,MigrationSource)
VALUES(src.DocumentNumber,NULL,src.ReceiptDate,N'RSMDB CashingType=8 mini accounting pilot journal. Source NoteID=' + CONVERT(nvarchar(50),src.ReceiptNoteId),src.TargetReceiptId,1,1,0,@DefaultDepartmentId,src.DocumentNumber,src.ReceiptNoteId,4,src.NoteSerial,N'RSMDB_CashingType8_MiniPilot')
OUTPUT src.ReceiptNoteId, inserted.Id INTO @JournalOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @PilotBatchId,@CustomerCode,N'RSMDB',N'DOUBLE_ENTREY_VOUCHERS',CONVERT(nvarchar(400),SourceReceiptId),N'JournalEntry',TargetJournalId,N'Journal',N'Inserted',0,0,N'RSMDB CashingType=8 mini accounting pilot journal'
FROM @JournalOut;

INSERT INTO dbo.JournalEntryDetail(JournalEntryId,Debit,Credit,AccountId,Notes,IsPosted,IsDeleted,IsActive,DepartmentId,SourceId)
SELECT jo.TargetJournalId,
       CONVERT(money,CASE WHEN dev.Credit_Or_Debit=0 THEN dev.Value ELSE 0 END),
       CONVERT(money,CASE WHEN dev.Credit_Or_Debit=1 THEN dev.Value ELSE 0 END),
       ar.TargetAccountId,
       dev.Double_Entry_Vouchers_Description,
       1,0,1,@DefaultDepartmentId,ro.TargetReceiptId
FROM @JournalOut jo
JOIN @ReceiptOut ro ON ro.SourceReceiptId=jo.SourceReceiptId
JOIN RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS dev ON dev.Notes_ID=jo.SourceReceiptId
JOIN dbo.PropertyMigrationAccountResolution ar ON ar.MigrationBatchId=@OperationalBatchId AND ar.SourceAccountCode=dev.Account_Code COLLATE DATABASE_DEFAULT AND ar.ResolutionStatus=N'Approved' AND ar.TargetAccountId IS NOT NULL;

IF EXISTS (SELECT 1 FROM dbo.JournalEntryDetail jd JOIN @JournalOut jo ON jo.TargetJournalId=jd.JournalEntryId WHERE jd.AccountId IS NULL)
BEGIN RAISERROR('Accounting safety failed: AccountId NULL after insert.',16,1); ROLLBACK; RETURN; END;
IF EXISTS (
    SELECT 1
    FROM dbo.JournalEntryDetail jd JOIN @JournalOut jo ON jo.TargetJournalId=jd.JournalEntryId
    GROUP BY jd.JournalEntryId
    HAVING ABS(SUM(ISNULL(jd.Debit,0))-SUM(ISNULL(jd.Credit,0)))>0.01
)
BEGIN RAISERROR('Accounting safety failed: unbalanced journal after insert.',16,1); ROLLBACK; RETURN; END;

DECLARE @InsertedReceipts int=(SELECT COUNT(*) FROM @ReceiptOut);
DECLARE @InsertedJournals int=(SELECT COUNT(*) FROM @JournalOut);
DECLARE @InsertedLines int=(SELECT COUNT(*) FROM dbo.JournalEntryDetail jd JOIN @JournalOut jo ON jo.TargetJournalId=jd.JournalEntryId);
DECLARE @Debit decimal(18,4)=(SELECT SUM(CONVERT(decimal(18,4),jd.Debit)) FROM dbo.JournalEntryDetail jd JOIN @JournalOut jo ON jo.TargetJournalId=jd.JournalEntryId);
DECLARE @Credit decimal(18,4)=(SELECT SUM(CONVERT(decimal(18,4),jd.Credit)) FROM dbo.JournalEntryDetail jd JOIN @JournalOut jo ON jo.TargetJournalId=jd.JournalEntryId);

IF @InsertedReceipts<>@ExpectedReceipts OR @InsertedJournals<>@ExpectedJournals OR @InsertedLines<>@ExpectedLines OR ABS(@Debit-@ExpectedValue)>0.01 OR ABS(@Credit-@ExpectedValue)>0.01
BEGIN RAISERROR('Inserted counts/totals mismatch; rolling back.',16,1); ROLLBACK; RETURN; END;

COMMIT;

SELECT PilotBatchId=@PilotBatchId, ReceiptsMigrated=@InsertedReceipts, JournalsMigrated=@InsertedJournals, JournalLinesMigrated=@InsertedLines, DebitTotal=@Debit, CreditTotal=@Credit;
