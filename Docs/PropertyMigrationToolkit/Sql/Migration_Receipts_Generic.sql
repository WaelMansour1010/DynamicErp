/* Generic receipts migration from PropertyMigrationSourceReceipt staging. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier='$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
DECLARE @DefaultBranchId int=NULL,@DefaultDepartmentId int=NULL,@DefaultCashBoxId int=NULL,@DefaultBankAccountId int=NULL,@CashReceiptPaymentMethodId int=NULL;
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceReceipt','U') IS NULL BEGIN RAISERROR('PropertyMigrationSourceReceipt staging table is missing.',16,1); RETURN; END;

SELECT TOP 1 @DefaultBranchId=DefaultPilotBranchId,@DefaultDepartmentId=DefaultDepartmentId,@DefaultCashBoxId=DefaultCashBoxId,@DefaultBankAccountId=DefaultBankAccountId,@CashReceiptPaymentMethodId=CashReceiptPaymentMethodId
FROM dbo.PropertyMigrationConfig WHERE CustomerCode=@CustomerCode AND TargetCloneDatabaseName=DB_NAME() ORDER BY ConfigId DESC;

INSERT INTO dbo.PropertyMigrationError(MigrationBatchId,CustomerCode,Severity,Stage,StepName,IssueType,EntityType,SourceTableName,SourceId,ErrorMessage)
SELECT @MigrationBatchId,@CustomerCode,N'Critical',N'Migration',N'Migration_Receipts_Generic',N'ReceiptAccountMissing',N'Receipt',SourceTableName,SourceId,N'Receipt AccountId is required to avoid unsafe accounting.'
FROM dbo.PropertyMigrationSourceReceipt WHERE MigrationBatchId=@MigrationBatchId AND IsValid=1 AND AccountId IS NULL;

DECLARE @Out TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(200),TargetId int);
MERGE dbo.CashReceiptVoucher AS tgt
USING (
    SELECT s.*, cm.TargetId ContractId, im.TargetId InstallmentId, rm.TargetId RenterTargetId
    FROM dbo.PropertyMigrationSourceReceipt s
    LEFT JOIN dbo.PropertyMigrationEntityMap cm ON cm.MigrationBatchId=@MigrationBatchId AND cm.EntityType=N'Contract' AND cm.SourceId=s.SourceContractId
    LEFT JOIN dbo.PropertyMigrationEntityMap im ON im.MigrationBatchId=@MigrationBatchId AND im.EntityType=N'Installment' AND im.SourceId=s.SourceInstallmentId
    LEFT JOIN dbo.PropertyMigrationEntityMap rm ON rm.MigrationBatchId=@MigrationBatchId AND rm.EntityType=N'Renter' AND rm.SourceId=s.SourceRenterId
    WHERE s.MigrationBatchId=@MigrationBatchId AND s.IsValid=1 AND s.AccountId IS NOT NULL AND s.MoneyAmount>0
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Receipt' AND m.SourceId=s.SourceId AND m.SourceTableName=s.SourceTableName)
) AS src ON 1=0
WHEN NOT MATCHED THEN INSERT(DocumentNumber,BranchId,MoneyAmount,SourceTypeId,Date,AccountId,IsLinked,IsPosted,IsActive,IsDeleted,Notes,DepartmentId,CashBoxId,BankAccountId,CashReceiptPaymentMethodId,PropertyContractId,RenterId,IsAgainstOpeningBalance)
VALUES(src.DocumentNumber,ISNULL(src.BranchId,@DefaultBranchId),src.MoneyAmount,13,src.ReceiptDate,src.AccountId,1,1,1,0,src.Notes,ISNULL(src.DepartmentId,@DefaultDepartmentId),ISNULL(src.CashBoxId,@DefaultCashBoxId),ISNULL(src.BankAccountId,@DefaultBankAccountId),ISNULL(src.PaymentMethodId,@CashReceiptPaymentMethodId),src.ContractId,src.RenterTargetId,0)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id INTO @Out;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'CashReceiptVoucher',TargetId,N'Receipt',N'Inserted',0,0,N'Generic receipts migration'
FROM @Out;

SELECT 'Receipts' Stage, COUNT(*) InsertedReceipts FROM @Out;
