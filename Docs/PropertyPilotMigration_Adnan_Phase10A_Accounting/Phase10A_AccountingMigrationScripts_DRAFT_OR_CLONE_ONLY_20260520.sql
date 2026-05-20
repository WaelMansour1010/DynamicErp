/*
Phase10A_AccountingMigrationScripts_DRAFT_OR_CLONE_ONLY_20260520.sql
Target: Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520 or safe PropertyPilot/ReadyToTest clone only.
Source: Adnan read-only.
SQL Server 2012 compatible.

Scope implemented:
- Seed missing ChartOfAccount rows required by journal lines for safe property cash receipts.
- Migrate historical cash receipts linked to migrated contract installments.
- Migrate journal entries/details linked to those receipts from DOUBLE_ENTREY_VOUCHERS.
- Exclude cash issues / owner payments from operational migration and report them for manual review.

No writes to Adnan or Alromaizan production.
*/
SET NOCOUNT ON;
GO

IF DB_NAME() IN (N'Alromaizan', N'Adnan', N'RSMDB')
BEGIN RAISERROR('Blocked: source/production database.',16,1); RETURN; END;
GO
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%'
BEGIN RAISERROR('Blocked: clone/sandbox database name required.',16,1); RETURN; END;
GO

DECLARE @BatchId uniqueidentifier;
SET @BatchId = 'A10AD000-0000-4000-9000-202605200010';

IF NOT EXISTS (SELECT 1 FROM dbo.PropertyPilotMigrationBatch WHERE MigrationBatchId=@BatchId)
BEGIN
    INSERT INTO dbo.PropertyPilotMigrationBatch(MigrationBatchId,BatchName,SourceDatabaseName,TargetDatabaseName,CutoverDate,Strategy,Status,CreatedAt,CreatedBy,Notes)
    VALUES(@BatchId,N'Adnan Phase10A Accounting ReadyToTest',N'Adnan',DB_NAME(),'20260520',N'Accounting Hybrid Receipts Only',N'DraftCreated',GETDATE(),N'Codex',N'Clone-only accounting migration batch.');
END;
GO

IF OBJECT_ID(N'dbo.usp_PropertyPilot_MigrateCashReceipts_Adnan', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_MigrateCashReceipts_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_MigrateCashReceipts_Adnan
    @MigrationBatchId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() IN (N'Alromaizan', N'Adnan', N'RSMDB')
    BEGIN RAISERROR('Blocked: source/production database.',16,1); RETURN; END;
    IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%'
    BEGIN RAISERROR('Blocked: clone/sandbox database name required.',16,1); RETURN; END;

    BEGIN TRY
        BEGIN TRANSACTION;

        ;WITH ActiveInstallments AS (
            SELECT TRY_CONVERT(int, OldId) OldInstallmentId, NewId NewBatchId
            FROM dbo.PropertyPilotCrossReference
            WHERE EntityType=N'ContractBatch'
        ), ReceiptNotes AS (
            SELECT DISTINCT n.NoteID
            FROM Adnan.dbo.Notes n
            INNER JOIN Adnan.dbo.ContracttBillInstallmentsDone d ON d.NoteID=n.NoteID
            INNER JOIN ActiveInstallments ai ON ai.OldInstallmentId=d.istallid
            WHERE n.NoteType=4
        ), RequiredAccounts AS (
            SELECT DISTINCT v.Account_Code, a.Account_ID, a.Account_Name, a.Account_NameEng
            FROM Adnan.dbo.DOUBLE_ENTREY_VOUCHERS v
            INNER JOIN ReceiptNotes rn ON rn.NoteID=v.Notes_ID
            LEFT JOIN Adnan.dbo.ACCOUNTS a ON a.Account_Code=v.Account_Code
            WHERE ISNULL(v.Account_Code,N'')<>N''
        )
        INSERT INTO dbo.ChartOfAccount(Code,ArName,EnName,ParentId,TypeId,ClassificationId,CategoryId,IsActive,IsDeleted,UserId,Notes)
        SELECT ra.Account_Code,
               ISNULL(ra.Account_Name,N'Adnan accounting account '+ra.Account_Code),
               ra.Account_NameEng,
               CASE WHEN ra.Account_Code LIKE N'a1%' THEN 617 WHEN ra.Account_Code LIKE N'a2%' THEN 618 WHEN ra.Account_Code LIKE N'a3%' THEN 620 WHEN ra.Account_Code LIKE N'a4%' THEN 619 ELSE 617 END,
               1,3,
               CASE WHEN ra.Account_Code LIKE N'a1%' THEN 1 WHEN ra.Account_Code LIKE N'a2%' THEN 2 WHEN ra.Account_Code LIKE N'a3%' THEN 3 WHEN ra.Account_Code LIKE N'a4%' THEN 4 ELSE 1 END,
               1,0,NULL,
               N'PropertyPilot Phase10A seeded accounting account. OldAccountId=' + ISNULL(CAST(ra.Account_ID AS nvarchar(30)),N'NULL')
        FROM RequiredAccounts ra
        LEFT JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT = ra.Account_Code COLLATE DATABASE_DEFAULT
        WHERE ca.Id IS NULL;

        ;WITH ActiveInstallments AS (
            SELECT TRY_CONVERT(int, OldId) OldInstallmentId, NewId NewBatchId
            FROM dbo.PropertyPilotCrossReference
            WHERE EntityType=N'ContractBatch'
        ), ReceiptNotes AS (
            SELECT DISTINCT n.NoteID
            FROM Adnan.dbo.Notes n
            INNER JOIN Adnan.dbo.ContracttBillInstallmentsDone d ON d.NoteID=n.NoteID
            INNER JOIN ActiveInstallments ai ON ai.OldInstallmentId=d.istallid
            WHERE n.NoteType=4
        ), RequiredAccounts AS (
            SELECT DISTINCT v.Account_Code, a.Account_ID, a.Account_Name
            FROM Adnan.dbo.DOUBLE_ENTREY_VOUCHERS v
            INNER JOIN ReceiptNotes rn ON rn.NoteID=v.Notes_ID
            LEFT JOIN Adnan.dbo.ACCOUNTS a ON a.Account_Code=v.Account_Code
            WHERE ISNULL(v.Account_Code,N'')<>N''
        )
        INSERT INTO dbo.PropertyPilotCrossReference(MigrationBatchId,OldDatabaseName,OldTableName,OldId,NewTableName,NewId,EntityType,CreatedAt,Notes)
        SELECT @MigrationBatchId,N'Adnan',N'ACCOUNTS',ra.Account_Code,N'ChartOfAccount',ca.Id,N'AccountingAccount',GETDATE(),N'Phase10A accounting account seed'
        FROM RequiredAccounts ra
        INNER JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT=ra.Account_Code COLLATE DATABASE_DEFAULT
        LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'AccountingAccount' AND x.OldId COLLATE DATABASE_DEFAULT=ra.Account_Code COLLATE DATABASE_DEFAULT
        WHERE x.Id IS NULL;

        ;WITH ActiveInstallments AS (
            SELECT TRY_CONVERT(int, OldId) OldInstallmentId, NewId NewBatchId
            FROM dbo.PropertyPilotCrossReference
            WHERE EntityType=N'ContractBatch'
        ), ReceiptDetails AS (
            SELECT n.NoteID,
                   ai.NewBatchId,
                   d.id DoneId,
                   CAST(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0) AS money) Paid
            FROM Adnan.dbo.Notes n
            INNER JOIN Adnan.dbo.ContracttBillInstallmentsDone d ON d.NoteID=n.NoteID
            INNER JOIN ActiveInstallments ai ON ai.OldInstallmentId=d.istallid
            WHERE n.NoteType=4
        ), ReceiptHeaders AS (
            SELECT n.NoteID,
                   MAX(n.NoteDate) NoteDate,
                   MAX(n.NoteSerial1) NoteSerial1,
                   MAX(n.NoteSerial) NoteSerial,
                   MAX(n.Remark) Remark,
                   MAX(n.NoteCashingType) NoteCashingType,
                   SUM(rd.Paid) MoneyAmount,
                   MAX(pc.Id) PropertyContractId,
                   MAX(pc.PropertyRenterId) RenterId
            FROM ReceiptDetails rd
            INNER JOIN Adnan.dbo.Notes n ON n.NoteID=rd.NoteID
            INNER JOIN dbo.PropertyContractBatch pcb ON pcb.Id=rd.NewBatchId
            INNER JOIN dbo.PropertyContract pc ON pc.Id=pcb.MainDocId
            GROUP BY n.NoteID
            HAVING SUM(rd.Paid)>0
        )
        INSERT INTO dbo.CashReceiptVoucher(DocumentNumber,BranchId,MoneyAmount,SourceTypeId,OtherSourceName,Date,CurrencyId,AccountId,IsLinked,IsPosted,IsActive,IsDeleted,UserId,Notes,Image,CustomerId,VendorId,EmployeeId,CurrencyEquivalent,DepartmentId,CashBoxId,TechnicianId,DirectRevenueId,ShareholderId,CostCenterId,IsInvoiceSelected,BankAccountId,TransactionNo,TransactionDate,ChartOfAccountId,CashReceiptPaymentMethodId,VendorReceiptNumber,Month,Year,ChildrenId,ElderId,IssuedPerson,PropertyContractId,RenterId,ElectricityBillValue,GasBillValue,ViolationBillValue,PropertyContractTerminationId,IsAgainstOpeningBalance)
        SELECT N'ADNAN-R-' + CAST(CAST(rh.NoteSerial1 AS decimal(20,0)) AS nvarchar(50)),
               1,rh.MoneyAmount,11,NULL,ISNULL(rh.NoteDate,'20260520'),4,NULL,1,1,1,0,1,
               N'Phase10A migrated historical property receipt. OldNoteID=' + CAST(rh.NoteID AS nvarchar(30)) + N'. ' + ISNULL(rh.Remark,N''),
               NULL,NULL,NULL,NULL,1,44,
               CASE WHEN ISNULL(rh.NoteCashingType,0)=0 THEN 1022 ELSE NULL END,
               NULL,NULL,NULL,NULL,0,
               CASE WHEN ISNULL(rh.NoteCashingType,0)=0 THEN NULL ELSE 2024 END,
               N'ADNAN-' + CAST(rh.NoteID AS nvarchar(30)),rh.NoteDate,NULL,
               CASE WHEN ISNULL(rh.NoteCashingType,0)=0 THEN 5 ELSE 6 END,
               CAST(CAST(rh.NoteSerial1 AS decimal(20,0)) AS nvarchar(50)),MONTH(ISNULL(rh.NoteDate,'20260520')),YEAR(ISNULL(rh.NoteDate,'20260520')),NULL,NULL,NULL,rh.PropertyContractId,rh.RenterId,0,0,0,NULL,0
        FROM ReceiptHeaders rh
        LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'CashReceiptVoucher' AND x.OldId=CAST(rh.NoteID AS nvarchar(100))
        WHERE x.Id IS NULL;

        ;WITH ActiveInstallments AS (
            SELECT TRY_CONVERT(int, OldId) OldInstallmentId, NewId NewBatchId
            FROM dbo.PropertyPilotCrossReference
            WHERE EntityType=N'ContractBatch'
        ), ReceiptDetails AS (
            SELECT n.NoteID,
                   ai.NewBatchId,
                   d.id DoneId,
                   CAST(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0) AS money) Paid
            FROM Adnan.dbo.Notes n
            INNER JOIN Adnan.dbo.ContracttBillInstallmentsDone d ON d.NoteID=n.NoteID
            INNER JOIN ActiveInstallments ai ON ai.OldInstallmentId=d.istallid
            WHERE n.NoteType=4
        ), ReceiptHeaders AS (
            SELECT n.NoteID, MAX(n.NoteSerial1) NoteSerial1, SUM(rd.Paid) MoneyAmount
            FROM ReceiptDetails rd INNER JOIN Adnan.dbo.Notes n ON n.NoteID=rd.NoteID
            GROUP BY n.NoteID HAVING SUM(rd.Paid)>0
        )
        INSERT INTO dbo.PropertyPilotCrossReference(MigrationBatchId,OldDatabaseName,OldTableName,OldId,NewTableName,NewId,EntityType,CreatedAt,Notes)
        SELECT @MigrationBatchId,N'Adnan',N'Notes',CAST(rh.NoteID AS nvarchar(100)),N'CashReceiptVoucher',cr.Id,N'CashReceiptVoucher',GETDATE(),N'Phase10A migrated historical receipt'
        FROM ReceiptHeaders rh
        INNER JOIN dbo.CashReceiptVoucher cr ON cr.TransactionNo=N'ADNAN-' + CAST(rh.NoteID AS nvarchar(30)) AND cr.Notes LIKE N'Phase10A migrated historical property receipt%'
        LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'CashReceiptVoucher' AND x.OldId=CAST(rh.NoteID AS nvarchar(100))
        WHERE x.Id IS NULL;

        ;WITH ReceiptDetails AS (
            SELECT n.NoteID,
                   ai.NewId AS NewBatchId,
                   d.id DoneId,
                   CAST(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0) AS money) Paid,
                   n.NoteDate
            FROM Adnan.dbo.Notes n
            INNER JOIN Adnan.dbo.ContracttBillInstallmentsDone d ON d.NoteID=n.NoteID
            INNER JOIN dbo.PropertyPilotCrossReference ai ON ai.EntityType=N'ContractBatch' AND TRY_CONVERT(int, ai.OldId)=d.istallid
            WHERE n.NoteType=4
        ), Running AS (
            SELECT rd.NoteID, rd.NewBatchId, rd.DoneId, rd.Paid,
                   SUM(rd.Paid) OVER (PARTITION BY rd.NewBatchId ORDER BY rd.NoteDate, rd.NoteID, rd.DoneId ROWS UNBOUNDED PRECEDING) CumPaid
            FROM ReceiptDetails rd
            WHERE rd.Paid>0
        )
        INSERT INTO dbo.CashReceiptVoucherPropertyContractBatch(CashReceiptVoucherId,PropertyContractBatchId,IsDelivered,Paid,Remain)
        SELECT crx.NewId, r.NewBatchId,
               CASE WHEN ISNULL(pcb.BatchTotal,0)-r.CumPaid <= 0.01 THEN 1 ELSE 0 END,
               r.Paid,
               CASE WHEN ISNULL(pcb.BatchTotal,0)-r.CumPaid < 0 THEN 0 ELSE ISNULL(pcb.BatchTotal,0)-r.CumPaid END
        FROM Running r
        INNER JOIN dbo.PropertyPilotCrossReference crx ON crx.MigrationBatchId=@MigrationBatchId AND crx.EntityType=N'CashReceiptVoucher' AND crx.OldId=CAST(r.NoteID AS nvarchar(100))
        INNER JOIN dbo.PropertyContractBatch pcb ON pcb.Id=r.NewBatchId
        LEFT JOIN dbo.CashReceiptVoucherPropertyContractBatch existing ON existing.CashReceiptVoucherId=crx.NewId AND existing.PropertyContractBatchId=r.NewBatchId AND ISNULL(existing.Paid,0)=ISNULL(r.Paid,0)
        WHERE existing.Id IS NULL;

        ;WITH ReceiptX AS (
            SELECT TRY_CONVERT(int, OldId) NoteID, NewId ReceiptId FROM dbo.PropertyPilotCrossReference WHERE MigrationBatchId=@MigrationBatchId AND EntityType=N'CashReceiptVoucher'
        ), JeSource AS (
            SELECT rx.NoteID, rx.ReceiptId, MAX(n.NoteDate) NoteDate, MAX(n.NoteSerial1) NoteSerial1,
                   SUM(CASE WHEN v.Credit_Or_Debit=0 THEN ISNULL(v.Value,0) ELSE 0 END) Debit,
                   SUM(CASE WHEN v.Credit_Or_Debit=1 THEN ISNULL(v.Value,0) ELSE 0 END) Credit
            FROM ReceiptX rx
            INNER JOIN Adnan.dbo.Notes n ON n.NoteID=rx.NoteID
            INNER JOIN Adnan.dbo.DOUBLE_ENTREY_VOUCHERS v ON v.Notes_ID=rx.NoteID
            GROUP BY rx.NoteID, rx.ReceiptId
            HAVING ABS(SUM(CASE WHEN v.Credit_Or_Debit=0 THEN ISNULL(v.Value,0) ELSE 0 END)-SUM(CASE WHEN v.Credit_Or_Debit=1 THEN ISNULL(v.Value,0) ELSE 0 END))<=0.01
        )
        INSERT INTO dbo.JournalEntry(DocumentNumber,CompanyId,BranchId,Date,DateHijri,Notes,SourcePageId,SourceId,IsActive,IsPosted,IsDeleted,Image,UserId,DepartmentId,CurrencyId,Equivalent,OriginalDocumentNumber,OriginalNoteId,OriginalNoteType,OriginalSerial,MigrationSource)
        SELECT N'ADNAN-JE-R-' + CAST(js.NoteID AS nvarchar(30)),NULL,1,ISNULL(js.NoteDate,'20260520'),NULL,
               N'Phase10A migrated receipt journal. OldNoteID=' + CAST(js.NoteID AS nvarchar(30)),22,js.ReceiptId,1,1,0,NULL,1,44,4,1,
               CAST(CAST(js.NoteSerial1 AS decimal(20,0)) AS nvarchar(50)),js.NoteID,4,CAST(CAST(js.NoteSerial1 AS decimal(20,0)) AS nvarchar(50)),N'AdnanPhase10A'
        FROM JeSource js
        LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'JournalEntry' AND x.OldId=CAST(js.NoteID AS nvarchar(100))
        WHERE x.Id IS NULL;

        ;WITH ReceiptX AS (
            SELECT TRY_CONVERT(int, OldId) NoteID, NewId ReceiptId FROM dbo.PropertyPilotCrossReference WHERE MigrationBatchId=@MigrationBatchId AND EntityType=N'CashReceiptVoucher'
        )
        INSERT INTO dbo.PropertyPilotCrossReference(MigrationBatchId,OldDatabaseName,OldTableName,OldId,NewTableName,NewId,EntityType,CreatedAt,Notes)
        SELECT @MigrationBatchId,N'Adnan',N'DOUBLE_ENTREY_VOUCHERS',CAST(rx.NoteID AS nvarchar(100)),N'JournalEntry',je.Id,N'JournalEntry',GETDATE(),N'Phase10A migrated receipt journal header'
        FROM ReceiptX rx
        INNER JOIN dbo.JournalEntry je ON je.SourcePageId=22 AND je.SourceId=rx.ReceiptId AND je.MigrationSource=N'AdnanPhase10A'
        LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'JournalEntry' AND x.OldId=CAST(rx.NoteID AS nvarchar(100))
        WHERE x.Id IS NULL;

        INSERT INTO dbo.JournalEntryDetail(JournalEntryId,Debit,Credit,CurrencyId,Equivalent,AccountId,SourcePageId,SourceId,Notes,CostCenterId,IsPosted,IsDeleted,IsActive,VendorId,PartyType,PartyId,AccountId2,DepartmentId)
        SELECT jex.NewId,
               CASE WHEN v.Credit_Or_Debit=0 THEN v.Value ELSE 0 END,
               CASE WHEN v.Credit_Or_Debit=1 THEN v.Value ELSE 0 END,
               4,1,ca.Id,22,crx.NewId,v.Double_Entry_Vouchers_Description,NULL,1,0,1,NULL,NULL,NULL,NULL,44
        FROM dbo.PropertyPilotCrossReference jex
        INNER JOIN dbo.PropertyPilotCrossReference crx ON crx.MigrationBatchId=@MigrationBatchId AND crx.EntityType=N'CashReceiptVoucher' AND crx.OldId COLLATE DATABASE_DEFAULT=jex.OldId COLLATE DATABASE_DEFAULT
        INNER JOIN Adnan.dbo.DOUBLE_ENTREY_VOUCHERS v ON v.Notes_ID=TRY_CONVERT(int,jex.OldId)
        INNER JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT=v.Account_Code COLLATE DATABASE_DEFAULT
        LEFT JOIN dbo.JournalEntryDetail existing ON existing.JournalEntryId=jex.NewId AND existing.AccountId=ca.Id AND existing.Debit=CASE WHEN v.Credit_Or_Debit=0 THEN v.Value ELSE 0 END AND existing.Credit=CASE WHEN v.Credit_Or_Debit=1 THEN v.Value ELSE 0 END AND ISNULL(existing.Notes,N'') COLLATE DATABASE_DEFAULT=ISNULL(v.Double_Entry_Vouchers_Description,N'') COLLATE DATABASE_DEFAULT
        WHERE jex.MigrationBatchId=@MigrationBatchId AND jex.EntityType=N'JournalEntry' AND existing.Id IS NULL;

        UPDATE dbo.PropertyPilotMigrationBatch SET Status=N'AccountingMigrated', Notes=ISNULL(Notes,N'') + N' | Receipts and journals migrated.' WHERE MigrationBatchId=@MigrationBatchId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @Err nvarchar(4000); SET @Err=ERROR_MESSAGE();
        RAISERROR(@Err,16,1);
    END CATCH;
END;
GO

IF OBJECT_ID(N'dbo.usp_PropertyPilot_MigrateCashIssues_Adnan', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_MigrateCashIssues_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_MigrateCashIssues_Adnan
    @MigrationBatchId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() IN (N'Alromaizan', N'Adnan', N'RSMDB') BEGIN RAISERROR('Blocked: source/production database.',16,1); RETURN; END;
    IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%'
    BEGIN RAISERROR('Blocked: clone/sandbox database name required.',16,1); RETURN; END;

    ;WITH ActiveContracts AS (SELECT TRY_CONVERT(int, OldId) ContNo FROM dbo.PropertyPilotCrossReference WHERE EntityType=N'Contract'),
    ActiveTenants AS (SELECT TRY_CONVERT(int, OldId) CusID FROM dbo.PropertyPilotCrossReference WHERE EntityType=N'Tenant'),
    ActiveProperties AS (SELECT TRY_CONVERT(int, OldId) AqarId FROM dbo.PropertyPilotCrossReference WHERE EntityType=N'Property'),
    Candidates AS (
        SELECT DISTINCT n.NoteID,n.NoteType,n.NoteDate,n.NoteSerial1,n.Note_Value,n.ContNo,n.CusID,n.akarid,n.UnitNo,n.ExpensesID,n.Remark
        FROM Adnan.dbo.Notes n
        LEFT JOIN ActiveContracts c ON c.ContNo=n.ContNo
        LEFT JOIN ActiveTenants t ON t.CusID=n.CusID
        LEFT JOIN ActiveProperties p ON p.AqarId=n.akarid
        WHERE n.NoteType=5 AND (c.ContNo IS NOT NULL OR t.CusID IS NOT NULL OR p.AqarId IS NOT NULL OR n.ContNo IS NOT NULL OR n.akarid IS NOT NULL)
    )
    INSERT INTO dbo.PropertyPilotValidationIssue(MigrationBatchId,Severity,IssueType,EntityType,OldDatabaseName,OldTableName,OldId,Message,CreatedAt,IsResolved,ResolutionNotes)
    SELECT @MigrationBatchId,N'Warning',N'CashIssueExcluded',N'CashIssueVoucher',N'Adnan',N'Notes',CAST(c.NoteID AS nvarchar(100)),
           N'Cash issue candidate excluded from Phase10A operational migration. Requires manual review. NoteSerial=' + ISNULL(CAST(CAST(c.NoteSerial1 AS decimal(20,0)) AS nvarchar(50)),N'NULL') + N'; Value=' + ISNULL(CAST(c.Note_Value AS nvarchar(50)),N'NULL'),
           GETDATE(),0,N'Not migrated: payment/owner/refund semantics are not safely contract-linked.'
    FROM Candidates c
    LEFT JOIN dbo.PropertyPilotValidationIssue i ON i.MigrationBatchId=@MigrationBatchId AND i.IssueType=N'CashIssueExcluded' AND i.OldId=CAST(c.NoteID AS nvarchar(100))
    WHERE i.Id IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.usp_PropertyPilot_MigrateJournalEntries_Adnan', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_MigrateJournalEntries_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_MigrateJournalEntries_Adnan
    @MigrationBatchId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    PRINT 'Journal entries are migrated as part of usp_PropertyPilot_MigrateCashReceipts_Adnan for safe receipt-linked notes only.';
END;
GO

IF OBJECT_ID(N'dbo.usp_PropertyPilot_MigrateAdvancePayments_Adnan', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_MigrateAdvancePayments_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_MigrateAdvancePayments_Adnan
    @MigrationBatchId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    PRINT 'Advance payments are already staged in PropertyPilotAdvancePaymentStaging from Phase10 ReadyToTest. No extra operational posting in Phase10A.';
END;
GO

IF OBJECT_ID(N'dbo.usp_PropertyPilot_ReconcileAccounting_Adnan', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_ReconcileAccounting_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_ReconcileAccounting_Adnan
    @MigrationBatchId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() IN (N'Alromaizan', N'Adnan', N'RSMDB') BEGIN RAISERROR('Blocked: source/production database.',16,1); RETURN; END;

    ;WITH ActiveInstallments AS (
        SELECT TRY_CONVERT(int, OldId) OldInstallmentId FROM dbo.PropertyPilotCrossReference WHERE EntityType=N'ContractBatch'
    ), SourceReceipts AS (
        SELECT DISTINCT n.NoteID
        FROM Adnan.dbo.Notes n INNER JOIN Adnan.dbo.ContracttBillInstallmentsDone d ON d.NoteID=n.NoteID INNER JOIN ActiveInstallments ai ON ai.OldInstallmentId=d.istallid
        WHERE n.NoteType=4
    ), SourceReceiptDetails AS (
        SELECT n.NoteID, CAST(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0) AS money) Paid
        FROM Adnan.dbo.Notes n INNER JOIN Adnan.dbo.ContracttBillInstallmentsDone d ON d.NoteID=n.NoteID INNER JOIN ActiveInstallments ai ON ai.OldInstallmentId=d.istallid
        WHERE n.NoteType=4
    )
    SELECT 'ReceiptReconciliation' Metric,
           (SELECT COUNT(*) FROM SourceReceipts) SourceReceipts,
           (SELECT COUNT(*) FROM dbo.PropertyPilotCrossReference WHERE MigrationBatchId=@MigrationBatchId AND EntityType=N'CashReceiptVoucher') MigratedReceipts,
           (SELECT CAST(SUM(Paid) AS decimal(18,4)) FROM SourceReceiptDetails) SourceReceiptPaid,
           (SELECT CAST(SUM(MoneyAmount) AS decimal(18,4)) FROM dbo.CashReceiptVoucher cr INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewId=cr.Id AND x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'CashReceiptVoucher') MigratedReceiptAmount;

    SELECT 'JournalReconciliation' Metric,
           COUNT(*) JournalCount,
           CAST(SUM(Debit) AS decimal(18,4)) TotalDebit,
           CAST(SUM(Credit) AS decimal(18,4)) TotalCredit,
           SUM(CASE WHEN NullAccountLines>0 THEN 1 ELSE 0 END) JournalsWithNullAccount,
           SUM(CASE WHEN ABS(Debit-Credit)>0.01 THEN 1 ELSE 0 END) UnbalancedJournals
    FROM (
        SELECT je.Id, SUM(ISNULL(jd.Debit,0)) Debit, SUM(ISNULL(jd.Credit,0)) Credit, SUM(CASE WHEN jd.AccountId IS NULL THEN 1 ELSE 0 END) NullAccountLines
        FROM dbo.JournalEntry je
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'JournalEntry' AND x.NewId=je.Id
        LEFT JOIN dbo.JournalEntryDetail jd ON jd.JournalEntryId=je.Id AND ISNULL(jd.IsDeleted,0)=0
        GROUP BY je.Id
    ) q;

    SELECT 'ExcludedIssues' Metric, COUNT(*) CountValue
    FROM dbo.PropertyPilotValidationIssue WHERE MigrationBatchId=@MigrationBatchId AND IssueType=N'CashIssueExcluded';
END;
GO

IF OBJECT_ID(N'dbo.usp_PropertyPilot_RollbackAccountingBatch_Adnan', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_RollbackAccountingBatch_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_RollbackAccountingBatch_Adnan
    @MigrationBatchId uniqueidentifier,
    @Confirm nvarchar(100) = N'NO'
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() IN (N'Alromaizan', N'Adnan', N'RSMDB') BEGIN RAISERROR('Blocked: source/production database.',16,1); RETURN; END;
    IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%'
    BEGIN RAISERROR('Blocked: clone/sandbox database name required.',16,1); RETURN; END;
    IF @Confirm<>N'YES_ROLLBACK_ACCOUNTING'
    BEGIN RAISERROR('Rollback requires @Confirm = YES_ROLLBACK_ACCOUNTING.',16,1); RETURN; END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE jd
        FROM dbo.JournalEntryDetail jd
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'JournalEntry' AND x.NewId=jd.JournalEntryId;

        DELETE je
        FROM dbo.JournalEntry je
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'JournalEntry' AND x.NewId=je.Id;

        DELETE cbd
        FROM dbo.CashReceiptVoucherPropertyContractBatch cbd
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'CashReceiptVoucher' AND x.NewId=cbd.CashReceiptVoucherId;

        DELETE cr
        FROM dbo.CashReceiptVoucher cr
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'CashReceiptVoucher' AND x.NewId=cr.Id;

        DELETE ca
        FROM dbo.ChartOfAccount ca
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'AccountingAccount' AND x.NewId=ca.Id
        WHERE ca.Notes LIKE N'PropertyPilot Phase10A seeded accounting account%';

        DELETE FROM dbo.PropertyPilotValidationIssue WHERE MigrationBatchId=@MigrationBatchId;
        DELETE FROM dbo.PropertyPilotCrossReference WHERE MigrationBatchId=@MigrationBatchId AND EntityType IN (N'CashReceiptVoucher',N'JournalEntry',N'AccountingAccount');
        UPDATE dbo.PropertyPilotMigrationBatch SET Status=N'AccountingRolledBack', Notes=ISNULL(Notes,N'') + N' | Accounting rolled back.' WHERE MigrationBatchId=@MigrationBatchId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @Err nvarchar(4000); SET @Err=ERROR_MESSAGE();
        RAISERROR(@Err,16,1);
    END CATCH;
END;
GO

DECLARE @RunBatchId uniqueidentifier;
SET @RunBatchId='A10AD000-0000-4000-9000-202605200010';
EXEC dbo.usp_PropertyPilot_MigrateCashReceipts_Adnan @RunBatchId;
EXEC dbo.usp_PropertyPilot_MigrateCashIssues_Adnan @RunBatchId;
EXEC dbo.usp_PropertyPilot_MigrateAdvancePayments_Adnan @RunBatchId;
EXEC dbo.usp_PropertyPilot_MigrateJournalEntries_Adnan @RunBatchId;
EXEC dbo.usp_PropertyPilot_ReconcileAccounting_Adnan @RunBatchId;
GO




