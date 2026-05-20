/*
RSMDB Staging Mapping Draft - SELECT to PropertyMigrationSource* staging tables
SQL Server 2012 compatible.
Purpose: prepare staging data on a SAFE TARGET CLONE only. This script reads RSMDB and writes only to staging/review tables in the target clone.
Do NOT run on RSMDB, Adnan, Alromaizan, MyErp, or production.
*/
SET NOCOUNT ON;

DECLARE @MigrationBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50) = N'$(CustomerCode)';
DECLARE @SourceDatabase sysname = N'$(SourceDatabaseName)';
DECLARE @CutoffDate date = '$(CutoffDate)';
DECLARE @SourceDbQuoted nvarchar(300);
DECLARE @Sql nvarchar(max);

IF @SourceDatabase IS NULL OR @SourceDatabase = N'' BEGIN RAISERROR('SourceDatabaseName is required.',16,1); RETURN; END;
IF @MigrationBatchId IS NULL BEGIN RAISERROR('MigrationBatchId is required.',16,1); RETURN; END;
IF DB_NAME() IN (N'RSMDB',N'Adnan',N'Alromaizan',N'MyErp') BEGIN RAISERROR('Blocked: this mapping must run on target clone only, never on source/reference databases.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Blocked: target clone/sandbox database name required.',16,1); RETURN; END;
IF DB_ID(@SourceDatabase) IS NULL BEGIN RAISERROR('Source database not found.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceProperty','U') IS NULL BEGIN RAISERROR('Run 01_SourceStagingTables_Generic.sql first.',16,1); RETURN; END;

SET @SourceDbQuoted = QUOTENAME(@SourceDatabase);

/* Owners: VB6 uses TblAqar.ownerid -> TblCustemers.CusID. Owner rows are staged as owners, not renters. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceOwner(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,SourceCode,ArName,EnName,AccountCode,Mobile,Phone,VATNo,BankAccountNo,BankName,DepartmentId,Notes)
SELECT DISTINCT @BatchId,@CustomerCode,@SourceDatabase,N''TblCustemers'',CAST(c.CusID AS nvarchar(200)),ISNULL(c.Fullcode,CAST(c.CusID AS nvarchar(50))),c.CusName,c.CusNamee,
       COALESCE(NULLIF(c.Account_Code,N''''),NULLIF(c.Account_Code_As_Supplier,N''''),NULLIF(c.Account_Code2,N'''')),c.Cus_mobile,c.Cus_Phone,c.VATNO,c.BankAccount,c.BankName,c.BranchId,
       N''RSMDB owner from TblAqar.ownerid''
FROM ' + @SourceDbQuoted + N'.dbo.TblAqar a
JOIN ' + @SourceDbQuoted + N'.dbo.TblCustemers c ON c.CusID=a.ownerid
WHERE a.ownerid IS NOT NULL AND a.ownerid<>0
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceOwner s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceTableName=N''TblCustemers'' AND s.SourceId=CAST(c.CusID AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname',@MigrationBatchId,@CustomerCode,@SourceDatabase;

/* Properties: TblAqar is the property/building table. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceProperty(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,SourceCode,ArName,EnName,PropertyTypeId,DepartmentId,Notes)
SELECT @BatchId,@CustomerCode,@SourceDatabase,N''TblAqar'',CAST(a.Aqarid AS nvarchar(200)),ISNULL(a.aqarNo,CAST(a.Aqarid AS nvarchar(50))),a.aqarname,a.aqarname,a.aqartypeid,a.BranchId,
       N''ownerid='' + ISNULL(CAST(a.ownerid AS nvarchar(50)),N'''') + N''; street='' + ISNULL(a.streetname,N'''')
FROM ' + @SourceDbQuoted + N'.dbo.TblAqar a
WHERE NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceProperty s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceTableName=N''TblAqar'' AND s.SourceId=CAST(a.Aqarid AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname',@MigrationBatchId,@CustomerCode,@SourceDatabase;

/* Property-owner links: DynamicErp has primary Property.PropertyOwnerId. Percentage is assumed 100 until a multi-owner table is proven. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourcePropertyOwner(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,SourcePropertyId,SourceOwnerId,OwnershipPercentage,IsPrimaryOwner,Notes)
SELECT @BatchId,@CustomerCode,@SourceDatabase,N''TblAqar'',CAST(a.Aqarid AS nvarchar(200)) + N''-'' + CAST(a.ownerid AS nvarchar(200)),CAST(a.Aqarid AS nvarchar(200)),CAST(a.ownerid AS nvarchar(200)),100,1,N''Primary owner from TblAqar.ownerid''
FROM ' + @SourceDbQuoted + N'.dbo.TblAqar a
WHERE a.ownerid IS NOT NULL AND a.ownerid<>0
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourcePropertyOwner s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceTableName=N''TblAqar'' AND s.SourceId=CAST(a.Aqarid AS nvarchar(200)) + N''-'' + CAST(a.ownerid AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname',@MigrationBatchId,@CustomerCode,@SourceDatabase;

/* Units: actual units are TblAqarDetai; TblUnites appears to be an auxiliary/unit-type table and is not the unit master. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceUnit(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,SourcePropertyId,SourceCode,ArName,EnName,PropertyUnitTypeId,PropertyUnitStatusId,Notes)
SELECT @BatchId,@CustomerCode,@SourceDatabase,N''TblAqarDetai'',CAST(u.Id AS nvarchar(200)),CAST(u.Aqarid AS nvarchar(200)),ISNULL(u.unitno,CAST(u.Id AS nvarchar(50))),ISNULL(u.unitdesc,u.unitno),u.unitdesc,u.unittype,u.Status,
       N''RentValue='' + ISNULL(CAST(u.RentValue AS nvarchar(50)),N'''') + N''; customerid='' + ISNULL(CAST(u.customerid AS nvarchar(50)),N'''')
FROM ' + @SourceDbQuoted + N'.dbo.TblAqarDetai u
WHERE NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceUnit s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceTableName=N''TblAqarDetai'' AND s.SourceId=CAST(u.Id AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname',@MigrationBatchId,@CustomerCode,@SourceDatabase;

/* Renters: tenant source is TblContract.CusID -> TblCustemers.CusID. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceRenter(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,SourceCode,ArName,EnName,AccountCode,Mobile,Phone,NationalNo,VATNo,DepartmentId,Notes)
SELECT DISTINCT @BatchId,@CustomerCode,@SourceDatabase,N''TblCustemers'',CAST(cu.CusID AS nvarchar(200)),ISNULL(cu.Fullcode,CAST(cu.CusID AS nvarchar(50))),cu.CusName,cu.CusNamee,
       COALESCE(NULLIF(cu.Account_Code,N''''),NULLIF(cu.Account_Code_As_Client,N''''),NULLIF(cu.Account_Code1,N'''')),cu.Cus_mobile,cu.Cus_Phone,cu.NationalNo,cu.VATNO,cu.BranchId,N''Renter from TblContract.CusID''
FROM ' + @SourceDbQuoted + N'.dbo.TblContract c
JOIN ' + @SourceDbQuoted + N'.dbo.TblCustemers cu ON cu.CusID=c.CusID
WHERE c.CusID IS NOT NULL
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceRenter s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceTableName=N''TblCustemers'' AND s.SourceId=CAST(cu.CusID AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname',@MigrationBatchId,@CustomerCode,@SourceDatabase;

/* Contracts: active rule is draft: EndContract=0 and EndDate >= cutoff. Must be reviewed before migration. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceContract(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,DocumentNumber,VoucherDate,SourcePropertyId,SourceUnitId,SourceRenterId,ContractStartDate,ContractEndDate,RentValue,NetTotal,TotalAfterTaxes,VATPercentage,VATValue,PropertyUnitTypeId,DepartmentId,NumberOfBatches,FirstBatchDate,PeriodBetweenBatchesNum,PeriodBetweenBatchesTypeId,Notes,IsActiveContract)
SELECT @BatchId,@CustomerCode,@SourceDatabase,N''TblContract'',CAST(c.ContNo AS nvarchar(200)),CAST(c.ContNo AS nvarchar(200)),ISNULL(c.ContDate,ISNULL(c.StrDate,GETDATE())),CAST(c.Iqar AS nvarchar(200)),CAST(c.UnitNo AS nvarchar(200)),CAST(c.CusID AS nvarchar(200)),c.StrDate,c.EndDate,c.MeterValue,c.NetValue,c.TotalValue,c.FATYou,c.FATValue,c.UnitType,c.Branch_NO,c.PaymentCount,c.FristPaymentDate,c.Periods,c.PeriodsID,
       N''ownerid='' + ISNULL(CAST(c.ownerid AS nvarchar(50)),N'''') + N''; EndContract='' + ISNULL(CAST(c.EndContract AS nvarchar(10)),N'''') + N''; NoteID='' + ISNULL(CAST(c.NoteID AS nvarchar(50)),N''''),
       CASE WHEN ISNULL(c.EndContract,0)=0 AND (c.EndDate IS NULL OR c.EndDate>=@CutoffDate) THEN 1 ELSE 0 END
FROM ' + @SourceDbQuoted + N'.dbo.TblContract c
WHERE NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceContract s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceTableName=N''TblContract'' AND s.SourceId=CAST(c.ContNo AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname,@CutoffDate date',@MigrationBatchId,@CustomerCode,@SourceDatabase,@CutoffDate;

/* Installments. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceInstallment(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,SourceContractId,BatchNo,BatchDate,BatchRentValue,BatchCommissionValue,BatchInsuranceValue,BatchWaterValue,BatchElectricityValue,BatchTotal,IsFuture,Notes)
SELECT @BatchId,@CustomerCode,@SourceDatabase,N''TblContractInstallments'',CAST(i.id AS nvarchar(200)),CAST(i.ContNo AS nvarchar(200)),i.InstallNo,i.Installdate,i.RentValue,i.Commissions,i.Insurance,i.Water,i.Electric,i.installValue,
       CASE WHEN i.Installdate>@CutoffDate THEN 1 ELSE 0 END,
       N''Payed='' + ISNULL(CAST(i.payed AS nvarchar(50)),N'''') + N''; Remains='' + ISNULL(CAST(i.Remains AS nvarchar(50)),N'''')
FROM ' + @SourceDbQuoted + N'.dbo.TblContractInstallments i
WHERE NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceInstallment s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceTableName=N''TblContractInstallments'' AND s.SourceId=CAST(i.id AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname,@CutoffDate date',@MigrationBatchId,@CustomerCode,@SourceDatabase,@CutoffDate;

/* Receipts: NoteType 4 is staged only when linked to contract/installment path. AccountId remains NULL until target account mapping is approved. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceReceipt(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,SourceContractId,SourceInstallmentId,SourceRenterId,DocumentNumber,ReceiptDate,MoneyAmount,BranchId,Notes,IsValid,ValidationMessage)
SELECT @BatchId,@CustomerCode,@SourceDatabase,N''Notes'',CAST(n.NoteID AS nvarchar(200)),CAST(n.ContNo AS nvarchar(200)),CAST(d.istallid AS nvarchar(200)),CAST(n.CusID AS nvarchar(200)),CAST(n.NoteSerial AS nvarchar(200)),ISNULL(n.NoteDate,GETDATE()),ISNULL(n.Note_Value,0),n.branch_no,
       N''NoteType=4; CashingType='' + ISNULL(CAST(n.CashingType AS nvarchar(20)),N'''') + N''; AccountCode='' + ISNULL(n.AccountsCode,N''''),
       CASE WHEN n.ContNo IS NULL OR d.istallid IS NULL THEN 0 ELSE 1 END,
       CASE WHEN n.ContNo IS NULL OR d.istallid IS NULL THEN N''Receipt link to contract/installment not proven'' ELSE NULL END
FROM ' + @SourceDbQuoted + N'.dbo.Notes n
LEFT JOIN ' + @SourceDbQuoted + N'.dbo.ContracttBillInstallmentsDone d ON d.NoteID=n.NoteID
WHERE n.NoteType=4
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceReceipt s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceTableName=N''Notes'' AND s.SourceId=CAST(n.NoteID AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname',@MigrationBatchId,@CustomerCode,@SourceDatabase;

/* Issues: NoteType 5 is review-only until payment source and owner/non-owner semantics are approved. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceIssue(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,DocumentNumber,IssueDate,MoneyAmount,BranchId,SourceTypeId,RequiresManualReview,Notes,IsValid,ValidationMessage)
SELECT @BatchId,@CustomerCode,@SourceDatabase,N''Notes'',CAST(n.NoteID AS nvarchar(200)),CAST(n.NoteSerial AS nvarchar(200)),ISNULL(n.NoteDate,GETDATE()),ISNULL(n.Note_Value,0),n.branch_no,n.CashingType,1,N''NoteType=5 review-only; source/payment semantics not approved'',1,N''Manual review required''
FROM ' + @SourceDbQuoted + N'.dbo.Notes n
WHERE n.NoteType=5
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceIssue s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceTableName=N''Notes'' AND s.SourceId=CAST(n.NoteID AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname',@MigrationBatchId,@CustomerCode,@SourceDatabase;

/* Owner payable schedules/payments: TblAqrOwin is staged as owner balance/payable review; owner payments are review-only. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceOwnerBalance(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceOwnerId,SourcePropertyId,BalanceAmount,BalanceDirection,CutoffDate,Notes,IsValid,ValidationMessage)
SELECT @BatchId,@CustomerCode,@SourceDatabase,CAST(a.ownerid AS nvarchar(200)),CAST(o.AqrID AS nvarchar(200)),ISNULL(o.value,0),N''Payable'',@CutoffDate,N''TblAqrOwin owner payable schedule; TotalPayed='' + ISNULL(CAST(o.TotalPayed AS nvarchar(50)),N''''),1,N''Review before accounting migration''
FROM ' + @SourceDbQuoted + N'.dbo.TblAqrOwin o
JOIN ' + @SourceDbQuoted + N'.dbo.TblAqar a ON a.Aqarid=o.AqrID
WHERE NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceOwnerBalance s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourcePropertyId=CAST(o.AqrID AS nvarchar(200)) AND s.BalanceAmount=ISNULL(o.value,0));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname,@CutoffDate date',@MigrationBatchId,@CustomerCode,@SourceDatabase,@CutoffDate;

/* Journals: only lines linked to staged receipts/contracts can be staged. AccountId requires matching target ChartOfAccount.Code. Unmatched lines go to review queue, not migration. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceJournal(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,LinkedReceiptSourceId,DocumentNumber,JournalDate,BranchId,Notes,IsValid,ValidationMessage)
SELECT DISTINCT @BatchId,@CustomerCode,@SourceDatabase,N''DOUBLE_ENTREY_VOUCHERS'',CAST(dev.Double_Entry_Vouchers_ID AS nvarchar(200)),CAST(dev.Notes_ID AS nvarchar(200)),CAST(dev.Double_Entry_Vouchers_ID AS nvarchar(200)),ISNULL(dev.RecordDate,GETDATE()),dev.branch_id,N''Voucher-linked journal candidate'',1,NULL
FROM ' + @SourceDbQuoted + N'.dbo.DOUBLE_ENTREY_VOUCHERS dev
JOIN dbo.PropertyMigrationSourceReceipt r ON r.MigrationBatchId=@BatchId AND r.SourceDatabaseName=@SourceDatabase AND r.SourceId=CAST(dev.Notes_ID AS nvarchar(200)) AND r.IsValid=1
WHERE dev.Notes_ID IS NOT NULL
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceJournal s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceId=CAST(dev.Double_Entry_Vouchers_ID AS nvarchar(200)));

INSERT INTO dbo.PropertyMigrationSourceJournalLine(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceJournalId,SourceLineId,AccountId,Debit,Credit,DepartmentId,Notes,IsValid,ValidationMessage)
SELECT @BatchId,@CustomerCode,@SourceDatabase,CAST(dev.Double_Entry_Vouchers_ID AS nvarchar(200)),CAST(dev.DEV_ID_Line_No AS nvarchar(200)),coa.Id,
       CASE WHEN dev.Credit_Or_Debit=2 THEN ISNULL(dev.Value,ISNULL(dev.depet_value,0)) ELSE 0 END,
       CASE WHEN dev.Credit_Or_Debit=1 THEN ISNULL(dev.Value,ISNULL(dev.credit_value,0)) ELSE 0 END,
       TRY_CONVERT(int,dev.Departementid),dev.Double_Entry_Vouchers_Description,1,NULL
FROM ' + @SourceDbQuoted + N'.dbo.DOUBLE_ENTREY_VOUCHERS dev
JOIN dbo.PropertyMigrationSourceJournal j ON j.MigrationBatchId=@BatchId AND j.SourceDatabaseName=@SourceDatabase AND j.SourceId=CAST(dev.Double_Entry_Vouchers_ID AS nvarchar(200))
JOIN dbo.ChartOfAccount coa ON coa.Code COLLATE DATABASE_DEFAULT = dev.Account_Code COLLATE DATABASE_DEFAULT
WHERE NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceJournalLine l WHERE l.MigrationBatchId=@BatchId AND l.SourceDatabaseName=@SourceDatabase AND l.SourceJournalId=CAST(dev.Double_Entry_Vouchers_ID AS nvarchar(200)) AND l.SourceLineId=CAST(dev.DEV_ID_Line_No AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname',@MigrationBatchId,@CustomerCode,@SourceDatabase;

/* Terminations: NoteType -1 is staged as review-only until VB6 settlement logic is signed off. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationSourceTermination(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,SourceContractId,TerminationDate,Amount,AccountId,Notes,RequiresManualReview,IsValid,ValidationMessage)
SELECT @BatchId,@CustomerCode,@SourceDatabase,N''Notes'',CAST(n.NoteID AS nvarchar(200)),ISNULL(CAST(n.ContNo AS nvarchar(200)),N''UNLINKED-NOTE-'' + CAST(n.NoteID AS nvarchar(200))),ISNULL(n.NoteDate,GETDATE()),ISNULL(n.Note_Value,0),NULL,N''NoteType=-1 termination candidate; requires VB6 settlement confirmation'',1,1,N''Review only''
FROM ' + @SourceDbQuoted + N'.dbo.Notes n
WHERE n.NoteType=-1
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationSourceTermination s WHERE s.MigrationBatchId=@BatchId AND s.SourceDatabaseName=@SourceDatabase AND s.SourceTableName=N''Notes'' AND s.SourceId=CAST(n.NoteID AS nvarchar(200)));
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname',@MigrationBatchId,@CustomerCode,@SourceDatabase;

/* NoteType 9088 is intentionally NOT mapped to migration entities; logged as warning/review candidate. */
SET @Sql = N'
INSERT INTO dbo.PropertyMigrationReviewQueue(MigrationBatchId,CustomerCode,Priority,Severity,IssueType,EntityType,SourceDatabaseName,SourceTableName,SourceId,OriginalValue,SuggestedAction,Status)
SELECT @BatchId,@CustomerCode,2,N''Warning'',N''UnclassifiedNoteType9088'',N''Note'',@SourceDatabase,N''Notes'',CAST(n.NoteID AS nvarchar(200)),CAST(n.Note_Value AS nvarchar(100)),N''Confirm if 9088 is VAT/installment/journal before migration'',N''Open''
FROM ' + @SourceDbQuoted + N'.dbo.Notes n
WHERE n.NoteType=9088
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationReviewQueue q WHERE q.MigrationBatchId=@BatchId AND q.SourceDatabaseName=@SourceDatabase AND q.SourceTableName=N''Notes'' AND q.SourceId=CAST(n.NoteID AS nvarchar(200)) AND q.IssueType=N''UnclassifiedNoteType9088'');
';
EXEC sp_executesql @Sql,N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@SourceDatabase sysname',@MigrationBatchId,@CustomerCode,@SourceDatabase;

SELECT 'RSMDBStagingDraft' Stage,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourceProperty WHERE MigrationBatchId=@MigrationBatchId) Properties,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourceOwner WHERE MigrationBatchId=@MigrationBatchId) Owners,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourcePropertyOwner WHERE MigrationBatchId=@MigrationBatchId) PropertyOwnerLinks,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourceUnit WHERE MigrationBatchId=@MigrationBatchId) Units,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourceRenter WHERE MigrationBatchId=@MigrationBatchId) Renters,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourceContract WHERE MigrationBatchId=@MigrationBatchId) Contracts,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourceInstallment WHERE MigrationBatchId=@MigrationBatchId) Installments,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourceReceipt WHERE MigrationBatchId=@MigrationBatchId) Receipts,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourceIssue WHERE MigrationBatchId=@MigrationBatchId) IssuesReviewOnly,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourceOwnerBalance WHERE MigrationBatchId=@MigrationBatchId) OwnerBalances,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationSourceTermination WHERE MigrationBatchId=@MigrationBatchId) TerminationsReviewOnly;


