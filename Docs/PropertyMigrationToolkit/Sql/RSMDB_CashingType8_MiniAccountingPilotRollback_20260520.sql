/* RSMDB CashingType=8 Mini Accounting Pilot Rollback - clone only. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @PilotBatchId uniqueidentifier = '$(MigrationBatchId)';
IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN RAISERROR('Mini pilot rollback requires clone/sandbox target database.',16,1); RETURN; END;
IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp')
BEGIN RAISERROR('Blocked database for mini pilot rollback.',16,1); RETURN; END;

IF OBJECT_ID('tempdb..#Receipts') IS NOT NULL DROP TABLE #Receipts;
IF OBJECT_ID('tempdb..#Journals') IS NOT NULL DROP TABLE #Journals;
SELECT TargetId INTO #Receipts FROM dbo.PropertyMigrationEntityMap WHERE MigrationBatchId=@PilotBatchId AND EntityType=N'Receipt' AND TargetTableName=N'CashReceiptVoucher';
SELECT TargetId INTO #Journals FROM dbo.PropertyMigrationEntityMap WHERE MigrationBatchId=@PilotBatchId AND EntityType=N'Journal' AND TargetTableName=N'JournalEntry';

BEGIN TRAN;
DELETE l FROM dbo.CashReceiptVoucherPropertyContractBatch l JOIN #Receipts r ON r.TargetId=l.CashReceiptVoucherId;
DELETE d FROM dbo.JournalEntryDetail d JOIN #Journals j ON j.TargetId=d.JournalEntryId;
DELETE je FROM dbo.JournalEntry je JOIN #Journals j ON j.TargetId=je.Id;
DELETE cr FROM dbo.CashReceiptVoucher cr JOIN #Receipts r ON r.TargetId=cr.Id;
DELETE FROM dbo.PropertyMigrationEntityMap WHERE MigrationBatchId=@PilotBatchId AND EntityType IN (N'Receipt',N'Journal');
COMMIT;

SELECT RemainingReceipts=(SELECT COUNT(*) FROM dbo.CashReceiptVoucher cr JOIN #Receipts r ON r.TargetId=cr.Id),
       RemainingJournals=(SELECT COUNT(*) FROM dbo.JournalEntry je JOIN #Journals j ON j.TargetId=je.Id),
       RemainingLines=(SELECT COUNT(*) FROM dbo.JournalEntryDetail d JOIN #Journals j ON j.TargetId=d.JournalEntryId),
       RemainingEntityMap=(SELECT COUNT(*) FROM dbo.PropertyMigrationEntityMap WHERE MigrationBatchId=@PilotBatchId AND EntityType IN (N'Receipt',N'Journal'));
