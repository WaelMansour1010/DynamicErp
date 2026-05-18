SET ANSI_NULLS ON
GO

DECLARE @CriticalRecoveryCaptionAr nvarchar(220);
SET @CriticalRecoveryCaptionAr =
    NCHAR(0x0645) + NCHAR(0x0631) + NCHAR(0x0643) + NCHAR(0x0632) + N' ' +
    NCHAR(0x0627) + NCHAR(0x0644) + NCHAR(0x0627) + NCHAR(0x0633) + NCHAR(0x062A) + NCHAR(0x0631) + NCHAR(0x062C) + NCHAR(0x0627) + NCHAR(0x0639) + N' ' +
    NCHAR(0x0627) + NCHAR(0x0644) + NCHAR(0x062D) + NCHAR(0x0631) + NCHAR(0x062C);

IF OBJECT_ID(N'dbo.WebModules', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.WebScreens', N'U') IS NOT NULL
BEGIN
    DECLARE @AdminModuleId int;

    SELECT TOP (1) @AdminModuleId = WebModuleId
    FROM dbo.WebModules
    WHERE ModuleKey = N'POS.Admin';

    IF @AdminModuleId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.WebScreens WHERE ScreenKey = N'POS.CriticalRecovery.Index')
        BEGIN
            INSERT INTO dbo.WebScreens
            (
                WebModuleId,
                ScreenKey,
                ArabicCaption,
                EnglishCaption,
                AreaName,
                ControllerName,
                ActionName,
                RouteUrl,
                IconCss,
                DisplayOrder,
                IsActive,
                IsMenuVisible,
                CreatedAt,
                UpdatedAt
            )
            VALUES
            (
                @AdminModuleId,
                N'POS.CriticalRecovery.Index',
                @CriticalRecoveryCaptionAr,
                N'Critical Recovery Center',
                N'Pos',
                N'CriticalRecovery',
                N'Index',
                N'/Pos/CriticalRecovery/Index',
                N'fas fa-exclamation-triangle',
                45,
                1,
                1,
                GETDATE(),
                GETDATE()
            );
        END
        ELSE
        BEGIN
            UPDATE dbo.WebScreens
               SET WebModuleId = @AdminModuleId,
                   ArabicCaption = @CriticalRecoveryCaptionAr,
                   EnglishCaption = N'Critical Recovery Center',
                   AreaName = N'Pos',
                   ControllerName = N'CriticalRecovery',
                   ActionName = N'Index',
                   RouteUrl = N'/Pos/CriticalRecovery/Index',
                   IconCss = N'fas fa-exclamation-triangle',
                   DisplayOrder = 45,
                   IsActive = 1,
                   IsMenuVisible = 1,
                   UpdatedAt = GETDATE()
             WHERE ScreenKey = N'POS.CriticalRecovery.Index';
        END
    END
END
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.CriticalRecoveryTableMap','U') IS NULL
BEGIN
    CREATE TABLE dbo.CriticalRecoveryTableMap
    (
        TableMapId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CriticalRecoveryTableMap PRIMARY KEY,
        ModuleName nvarchar(100) NOT NULL,
        TableName sysname NOT NULL,
        PrimaryKeyColumn sysname NOT NULL,
        RelationColumn sysname NULL,
        RelationType nvarchar(30) NOT NULL,
        SnapshotOrder int NOT NULL,
        ReverseOrder int NOT NULL,
        ActionPolicy nvarchar(30) NOT NULL CONSTRAINT DF_CriticalRecoveryTableMap_ActionPolicy DEFAULT('ArchiveAndSoftMark'),
        IsKycMaster bit NOT NULL CONSTRAINT DF_CriticalRecoveryTableMap_IsKycMaster DEFAULT(0),
        IsProtected bit NOT NULL CONSTRAINT DF_CriticalRecoveryTableMap_IsProtected DEFAULT(0),
        IsActive bit NOT NULL CONSTRAINT DF_CriticalRecoveryTableMap_IsActive DEFAULT(1),
        CreatedAt datetime NOT NULL CONSTRAINT DF_CriticalRecoveryTableMap_CreatedAt DEFAULT(GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.CriticalRecoverySnapshotBatch','U') IS NULL
BEGIN
    CREATE TABLE dbo.CriticalRecoverySnapshotBatch
    (
        SnapshotBatchId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CriticalRecoverySnapshotBatch PRIMARY KEY,
        RequestId int NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_CriticalRecoverySnapshotBatch_CreatedAt DEFAULT(GETDATE()),
        CompletedAt datetime NULL,
        RequestedBy nvarchar(128) NOT NULL,
        ApprovedBy nvarchar(128) NULL,
        MachineName nvarchar(128) NULL,
        IpAddress nvarchar(64) NULL,
        SessionId nvarchar(128) NULL,
        Mode nvarchar(50) NOT NULL,
        Reason nvarchar(1000) NOT NULL,
        BranchId int NULL,
        InvoiceType int NULL,
        DateFrom datetime NULL,
        DateTo datetime NULL,
        Status nvarchar(30) NOT NULL CONSTRAINT DF_CriticalRecoverySnapshotBatch_Status DEFAULT('SnapshotCreated'),
        InvoiceCount int NOT NULL CONSTRAINT DF_CriticalRecoverySnapshotBatch_InvoiceCount DEFAULT(0),
        SnapshotRowCount int NOT NULL CONSTRAINT DF_CriticalRecoverySnapshotBatch_RowCount DEFAULT(0),
        KycPolicy nvarchar(400) NOT NULL CONSTRAINT DF_CriticalRecoverySnapshotBatch_KycPolicy DEFAULT('KYC master data preserved. Only invoice links are archived/restored.'),
        FailureMessage nvarchar(max) NULL
    );
END
GO

IF OBJECT_ID('dbo.CriticalRecoverySnapshotRow','U') IS NULL
BEGIN
    CREATE TABLE dbo.CriticalRecoverySnapshotRow
    (
        SnapshotRowId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CriticalRecoverySnapshotRow PRIMARY KEY,
        SnapshotBatchId bigint NOT NULL,
        OriginalTable sysname NOT NULL,
        OriginalPrimaryKey nvarchar(200) NOT NULL,
        Transaction_ID bigint NULL,
        BranchId int NULL,
        InvoiceType int NULL,
        InvoiceNo nvarchar(100) NULL,
        XmlData xml NOT NULL,
        RowHash varbinary(32) NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_CriticalRecoverySnapshotRow_CreatedAt DEFAULT(GETDATE())
    );

    CREATE INDEX IX_CriticalRecoverySnapshotRow_BatchTable ON dbo.CriticalRecoverySnapshotRow(SnapshotBatchId, OriginalTable);
    CREATE INDEX IX_CriticalRecoverySnapshotRow_Transaction ON dbo.CriticalRecoverySnapshotRow(Transaction_ID);
END
GO

IF OBJECT_ID('dbo.CriticalRecoveryRequest','U') IS NULL
BEGIN
    CREATE TABLE dbo.CriticalRecoveryRequest
    (
        RequestId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CriticalRecoveryRequest PRIMARY KEY,
        RequestedAt datetime NOT NULL CONSTRAINT DF_CriticalRecoveryRequest_RequestedAt DEFAULT(GETDATE()),
        RequestedBy nvarchar(128) NOT NULL,
        ApprovedAt datetime NULL,
        ApprovedBy nvarchar(128) NULL,
        Status nvarchar(30) NOT NULL CONSTRAINT DF_CriticalRecoveryRequest_Status DEFAULT('PendingApproval'),
        Mode nvarchar(50) NOT NULL,
        Reason nvarchar(1000) NOT NULL,
        BranchId int NULL,
        DateFrom datetime NULL,
        DateTo datetime NULL,
        InvoiceType int NULL,
        InvoiceNo nvarchar(100) NULL,
        SelectedTransactionIds nvarchar(max) NULL,
        DryRun bit NOT NULL CONSTRAINT DF_CriticalRecoveryRequest_DryRun DEFAULT(0),
        AllowPhysicalDelete bit NOT NULL CONSTRAINT DF_CriticalRecoveryRequest_Physical DEFAULT(0),
        DeleteOrphanKycRecords bit NOT NULL CONSTRAINT DF_CriticalRecoveryRequest_Kyc DEFAULT(0),
        MachineName nvarchar(128) NULL,
        IpAddress nvarchar(64) NULL,
        SessionId nvarchar(128) NULL,
        SnapshotBatchId bigint NULL,
        FailureMessage nvarchar(max) NULL
    );
END
GO

IF OBJECT_ID('dbo.CriticalRecoveryRequest','U') IS NOT NULL
   AND COL_LENGTH('dbo.CriticalRecoveryRequest','InvoiceSource') IS NULL
BEGIN
    ALTER TABLE dbo.CriticalRecoveryRequest ADD InvoiceSource nvarchar(30) NULL;
END
GO

IF OBJECT_ID('dbo.CriticalRecoveryRequest','U') IS NOT NULL
   AND COL_LENGTH('dbo.CriticalRecoveryRequest','OperationKind') IS NULL
BEGIN
    ALTER TABLE dbo.CriticalRecoveryRequest ADD OperationKind nvarchar(30) NULL;
END
GO

IF OBJECT_ID('dbo.Transactions','U') IS NOT NULL
   AND COL_LENGTH('dbo.Transactions','CriticalRecoveryStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions ADD CriticalRecoveryStatus nvarchar(30) NULL;
END
GO

IF OBJECT_ID('dbo.Transactions','U') IS NOT NULL
   AND COL_LENGTH('dbo.Transactions','CriticalRecoveryBatchId') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions ADD CriticalRecoveryBatchId bigint NULL;
END
GO

IF OBJECT_ID('dbo.Transactions','U') IS NOT NULL
   AND COL_LENGTH('dbo.Transactions','CriticalRecoveryAt') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions ADD CriticalRecoveryAt datetime NULL;
END
GO

IF OBJECT_ID('dbo.CriticalRecoveryArchive_Transactions','U') IS NOT NULL
   AND COL_LENGTH('dbo.CriticalRecoveryArchive_Transactions','CriticalRecoveryStatus') IS NULL
BEGIN
    ALTER TABLE dbo.CriticalRecoveryArchive_Transactions ADD CriticalRecoveryStatus nvarchar(30) NULL;
END
GO

IF OBJECT_ID('dbo.CriticalRecoveryArchive_Transactions','U') IS NOT NULL
   AND COL_LENGTH('dbo.CriticalRecoveryArchive_Transactions','CriticalRecoveryBatchId') IS NULL
BEGIN
    ALTER TABLE dbo.CriticalRecoveryArchive_Transactions ADD CriticalRecoveryBatchId bigint NULL;
END
GO

IF OBJECT_ID('dbo.CriticalRecoveryArchive_Transactions','U') IS NOT NULL
   AND COL_LENGTH('dbo.CriticalRecoveryArchive_Transactions','CriticalRecoveryAt') IS NULL
BEGIN
    ALTER TABLE dbo.CriticalRecoveryArchive_Transactions ADD CriticalRecoveryAt datetime NULL;
END
GO

IF OBJECT_ID('dbo.CriticalRecoveryAudit','U') IS NULL
BEGIN
    CREATE TABLE dbo.CriticalRecoveryAudit
    (
        AuditId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CriticalRecoveryAudit PRIMARY KEY,
        CreatedAt datetime NOT NULL CONSTRAINT DF_CriticalRecoveryAudit_CreatedAt DEFAULT(GETDATE()),
        SnapshotBatchId bigint NULL,
        RequestId int NULL,
        ActionName nvarchar(80) NOT NULL,
        OperatorName nvarchar(128) NOT NULL,
        ApproverName nvarchar(128) NULL,
        MachineName nvarchar(128) NULL,
        IpAddress nvarchar(64) NULL,
        SessionId nvarchar(128) NULL,
        OldValues xml NULL,
        NewValues xml NULL,
        Result nvarchar(30) NOT NULL,
        Message nvarchar(max) NULL
    );

    CREATE INDEX IX_CriticalRecoveryAudit_Batch ON dbo.CriticalRecoveryAudit(SnapshotBatchId, CreatedAt);
END
GO

IF OBJECT_ID('dbo.CriticalRecoveryPolicy','U') IS NULL
BEGIN
    CREATE TABLE dbo.CriticalRecoveryPolicy
    (
        PolicyKey nvarchar(100) NOT NULL CONSTRAINT PK_CriticalRecoveryPolicy PRIMARY KEY,
        PolicyValue nvarchar(200) NOT NULL,
        Description nvarchar(500) NULL,
        UpdatedAt datetime NOT NULL CONSTRAINT DF_CriticalRecoveryPolicy_UpdatedAt DEFAULT(GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.CriticalRecoveryExecutionLock','U') IS NULL
BEGIN
    CREATE TABLE dbo.CriticalRecoveryExecutionLock
    (
        LockName nvarchar(100) NOT NULL CONSTRAINT PK_CriticalRecoveryExecutionLock PRIMARY KEY,
        AcquiredAt datetime NOT NULL,
        AcquiredBy nvarchar(128) NOT NULL,
        SessionId nvarchar(128) NULL
    );
END
GO

IF OBJECT_ID('dbo.CriticalRecoverySecondaryCredential','U') IS NULL
BEGIN
    CREATE TABLE dbo.CriticalRecoverySecondaryCredential
    (
        UserName nvarchar(128) NOT NULL CONSTRAINT PK_CriticalRecoverySecondaryCredential PRIMARY KEY,
        PasswordHash varbinary(32) NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_CriticalRecoverySecondaryCredential_IsActive DEFAULT(1),
        UpdatedAt datetime NOT NULL CONSTRAINT DF_CriticalRecoverySecondaryCredential_UpdatedAt DEFAULT(GETDATE()),
        UpdatedBy nvarchar(128) NULL
    );
END
GO

IF OBJECT_ID('dbo.usp_CriticalRecovery_SetSecondaryPassword','P') IS NOT NULL
    DROP PROCEDURE dbo.usp_CriticalRecovery_SetSecondaryPassword
GO
CREATE PROCEDURE dbo.usp_CriticalRecovery_SetSecondaryPassword
    @UserName nvarchar(128),
    @NewSecondaryPassword nvarchar(256),
    @UpdatedBy nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;
    IF ISNULL(@UserName,N'')=N'' OR LEN(ISNULL(@NewSecondaryPassword,N'')) < 10
    BEGIN
        RAISERROR(N'Secondary password requires a user name and at least 10 characters.', 16, 1);
        RETURN;
    END

    UPDATE dbo.CriticalRecoverySecondaryCredential
       SET PasswordHash=HASHBYTES('SHA2_256', CONVERT(varbinary(4000), @UserName + N':' + @NewSecondaryPassword)),
           IsActive=1,
           UpdatedAt=GETDATE(),
           UpdatedBy=@UpdatedBy
     WHERE UserName=@UserName;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT dbo.CriticalRecoverySecondaryCredential(UserName, PasswordHash, IsActive, UpdatedBy)
        VALUES(@UserName, HASHBYTES('SHA2_256', CONVERT(varbinary(4000), @UserName + N':' + @NewSecondaryPassword)), 1, @UpdatedBy);
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CriticalRecoveryPolicy WHERE PolicyKey='ClosedPeriodPolicy')
    INSERT dbo.CriticalRecoveryPolicy(PolicyKey, PolicyValue, Description) VALUES('ClosedPeriodPolicy', 'RequireHigherApproval', 'Block or require higher approval for closed/audited/exported periods.');
IF NOT EXISTS (SELECT 1 FROM dbo.CriticalRecoveryPolicy WHERE PolicyKey='KycDefaultPolicy')
    INSERT dbo.CriticalRecoveryPolicy(PolicyKey, PolicyValue, Description) VALUES('KycDefaultPolicy', 'PreserveMasterData', 'KYC customer/card/token/attachment data is preserved by default.');
GO

DECLARE @CriticalRecoveryMap TABLE
(
    ModuleName nvarchar(100),
    TableName sysname,
    PrimaryKeyColumn sysname,
    RelationColumn sysname NULL,
    RelationType nvarchar(30),
    SnapshotOrder int,
    ReverseOrder int,
    ActionPolicy nvarchar(30),
    IsKycMaster bit,
    IsProtected bit
);

INSERT @CriticalRecoveryMap(ModuleName, TableName, PrimaryKeyColumn, RelationColumn, RelationType, SnapshotOrder, ReverseOrder, ActionPolicy, IsKycMaster, IsProtected)
    SELECT N'Sales invoice' ModuleName, N'Transactions' TableName, N'Transaction_ID' PrimaryKeyColumn, N'Transaction_ID' RelationColumn, N'TransactionId' RelationType, 10 SnapshotOrder, 900 ReverseOrder, N'ArchiveAndSoftMark' ActionPolicy, 0 IsKycMaster, 0 IsProtected
    UNION ALL SELECT N'Sales invoice lines', N'Transaction_Details', N'ID', N'Transaction_ID', N'TransactionId', 20, 100, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'Accounting notes', N'Notes', N'NoteID', N'Transaction_ID', N'TransactionId', 25, 210, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'Accounting', N'DOUBLE_ENTREY_VOUCHERS', N'Double_Entry_Vouchers_ID', N'Transaction_ID', N'TransactionId', 30, 200, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'Payments', N'Payments', N'Payment_ID', N'Transaction_ID', N'TransactionId', 50, 300, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'Transaction values', N'TransactionsValues', N'id', N'Transaction_ID', N'TransactionId', 51, 110, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'Transaction criteria', N'TransactionsCreteria', N'id', N'Transaction_ID', N'TransactionId', 52, 111, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'Maintenance links', N'MaintenanceJuncTransaction', N'JuncID', N'Transaction_ID', N'TransactionId', 53, 112, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'Product order expenses', N'TblProductOrderFactoryExpenses', N'id', N'Transaction_ID', N'TransactionId', 54, 113, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'Product order lines', N'TblProductOrderLines', N'id', N'Transaction_ID', N'TransactionId', 55, 114, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'Product order workers', N'TblProductOrderWorker', N'id', N'Transaction_ID', N'TransactionId', 56, 115, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'POS closure', N'POSClosures', N'ClosureId', N'Transaction_ID', N'TransactionId', 60, 400, N'ArchiveAndRebuild', 0, 0
    UNION ALL SELECT N'Generated vouchers', N'TblTransctionIDES', N'ID', N'MainTransaction_ID', N'TransactionId', 70, 500, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'Commissions', N'TblSalesrepComm', N'ID', N'Transaction_ID', N'TransactionId', 80, 600, N'ArchiveAndReverse', 0, 0
    UNION ALL SELECT N'KYC links only', N'TransactionKycLinks', N'ID', N'Transaction_ID', N'TransactionId', 90, 700, N'ArchiveLinkOnly', 0, 0
    UNION ALL SELECT N'KYC protected master', N'KycCustomers', N'KycCustomerId', N'KycCustomerId', N'KycMaster', 1000, 1000, N'ProtectNeverDelete', 1, 1
    UNION ALL SELECT N'KYC protected master', N'KycAttachments', N'KycAttachmentId', N'KycCustomerId', N'KycMaster', 1001, 1001, N'ProtectNeverDelete', 1, 1
    UNION ALL SELECT N'KYC protected master', N'KycCards', N'KycCardId', N'KycCustomerId', N'KycMaster', 1002, 1002, N'ProtectNeverDelete', 1, 1;

UPDATE target
   SET ModuleName=source.ModuleName,
       PrimaryKeyColumn=source.PrimaryKeyColumn,
       RelationColumn=source.RelationColumn,
       RelationType=source.RelationType,
       SnapshotOrder=source.SnapshotOrder,
       ReverseOrder=source.ReverseOrder,
       ActionPolicy=source.ActionPolicy,
       IsKycMaster=source.IsKycMaster,
       IsProtected=source.IsProtected
  FROM dbo.CriticalRecoveryTableMap target
  INNER JOIN @CriticalRecoveryMap source ON target.TableName = source.TableName;

INSERT dbo.CriticalRecoveryTableMap(ModuleName, TableName, PrimaryKeyColumn, RelationColumn, RelationType, SnapshotOrder, ReverseOrder, ActionPolicy, IsKycMaster, IsProtected)
SELECT source.ModuleName, source.TableName, source.PrimaryKeyColumn, source.RelationColumn, source.RelationType, source.SnapshotOrder, source.ReverseOrder, source.ActionPolicy, source.IsKycMaster, source.IsProtected
  FROM @CriticalRecoveryMap source
 WHERE NOT EXISTS
 (
     SELECT 1
       FROM dbo.CriticalRecoveryTableMap target
      WHERE target.TableName = source.TableName
 );
GO

IF OBJECT_ID('dbo.usp_CriticalRecovery_AnalyzeInvoices','P') IS NOT NULL
    DROP PROCEDURE dbo.usp_CriticalRecovery_AnalyzeInvoices
GO
CREATE PROCEDURE dbo.usp_CriticalRecovery_AnalyzeInvoices
    @BranchId int = NULL,
    @DateFrom datetime = NULL,
    @DateTo datetime = NULL,
    @InvoiceType int = NULL,
    @InvoiceScope nvarchar(30) = N'SalesOnly',
    @InvoiceSource nvarchar(30) = N'Both',
    @OperationKind nvarchar(30) = N'All',
    @InvoiceNo nvarchar(100) = N'',
    @CashierUserId nvarchar(128) = N'',
    @CustomerSearch nvarchar(200) = N'',
    @ClosingStatus nvarchar(30) = N'',
    @PostedStatus nvarchar(30) = N'',
    @HasAccountingEntry bit = 0,
    @HasStockMovement bit = 0,
    @HasKycLink bit = 0,
    @HasGeneratedVoucher bit = 0,
    @SelectedTransactionIds nvarchar(max) = N''
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #AffectedInvoices
    (
        Transaction_ID bigint NOT NULL PRIMARY KEY,
        NoteID bigint NULL,
        InvoiceNo nvarchar(100) NULL,
        InvoiceType int NULL,
        OperationTypeName nvarchar(50) NULL,
        BranchId int NULL,
        Transaction_Date datetime NULL,
        CustomerName nvarchar(300) NULL,
        Note_Value decimal(19,4) NULL,
        IsPosted int NOT NULL DEFAULT(0),
        IsClosed int NOT NULL DEFAULT(0),
        KycReferenceIds nvarchar(1000) NULL
    );

    IF OBJECT_ID('dbo.Transactions','U') IS NULL
    BEGIN
        SELECT * FROM #AffectedInvoices;
        SELECT CAST(NULL AS bigint) Transaction_ID, CAST(N'' AS nvarchar(100)) ModuleName, CAST(N'' AS sysname) TableName, CAST(0 AS int) [RowCount], CAST(0 AS int) IsProtected, CAST(N'' AS nvarchar(30)) ActionPolicy WHERE 1=0;
        SELECT N'dbo.Transactions was not found.' WarningMessage;
        RETURN;
    END

    CREATE TABLE #ExcelImported(Transaction_ID bigint NOT NULL PRIMARY KEY);
    IF OBJECT_ID('dbo.POS_ImportBatchRow','U') IS NOT NULL
    BEGIN
        INSERT #ExcelImported(Transaction_ID)
        SELECT DISTINCT CAST(TransactionId AS bigint)
        FROM dbo.POS_ImportBatchRow
        WHERE TransactionId IS NOT NULL AND Status = N'Imported';
    END

    DECLARE @noteExpr nvarchar(200);
    SET @noteExpr = CASE
        WHEN COL_LENGTH('dbo.Transactions','NoteID') IS NOT NULL THEN N'T.NoteID'
        WHEN COL_LENGTH('dbo.Transactions','Notes_ID') IS NOT NULL THEN N'T.Notes_ID'
        ELSE N'NULL'
    END;

    DECLARE @branchExpr nvarchar(200);
    SET @branchExpr = CASE WHEN COL_LENGTH('dbo.Transactions','BranchId') IS NOT NULL THEN N'T.BranchId' WHEN COL_LENGTH('dbo.Transactions','branch_id') IS NOT NULL THEN N'T.branch_id' ELSE N'NULL' END;

    DECLARE @serialExpr nvarchar(200);
    SET @serialExpr = CASE WHEN COL_LENGTH('dbo.Transactions','NoteSerial1') IS NOT NULL THEN N'CONVERT(nvarchar(100),T.NoteSerial1)' WHEN COL_LENGTH('dbo.Transactions','ManualNO') IS NOT NULL THEN N'CONVERT(nvarchar(100),T.ManualNO)' ELSE N'CONVERT(nvarchar(100),T.Transaction_ID)' END;

    DECLARE @postedExpr nvarchar(200);
    SET @postedExpr = CASE WHEN COL_LENGTH('dbo.Transactions','Posted') IS NOT NULL THEN N'CASE WHEN ISNULL(T.Posted,0)<>0 THEN 1 ELSE 0 END' ELSE N'0' END;

    DECLARE @valueExpr nvarchar(200);
    SET @valueExpr = CASE
        WHEN COL_LENGTH('dbo.Transactions','Note_Value') IS NOT NULL THEN N'T.Note_Value'
        WHEN COL_LENGTH('dbo.Transactions','NetValue') IS NOT NULL THEN N'T.NetValue'
        WHEN COL_LENGTH('dbo.Transactions','Total') IS NOT NULL THEN N'T.Total'
        WHEN COL_LENGTH('dbo.Transactions','FinacilaTotal') IS NOT NULL THEN N'T.FinacilaTotal'
        WHEN COL_LENGTH('dbo.Transactions','Transaction_NetValue') IS NOT NULL THEN N'T.Transaction_NetValue'
        ELSE N'0'
    END;

    DECLARE @operationTypeExpr nvarchar(700);
    SET @operationTypeExpr = N'CASE '
        + CASE WHEN COL_LENGTH('dbo.Transactions','TrafficViolations') IS NOT NULL THEN N'WHEN ISNULL(T.TrafficViolations,0) = 1 THEN N''مخالفات'' ' ELSE N'' END
        + CASE WHEN COL_LENGTH('dbo.Transactions','ViolationsValue') IS NOT NULL THEN N'WHEN ISNULL(T.ViolationsValue,0) > 0 THEN N''مخالفات'' ' ELSE N'' END
        + CASE WHEN COL_LENGTH('dbo.Transactions','IsPOS') IS NOT NULL THEN N'WHEN ISNULL(T.IsPOS,0) = 1 THEN N''كارت'' ' ELSE N'' END
        + CASE WHEN COL_LENGTH('dbo.Transactions','VisaNumber') IS NOT NULL THEN N'WHEN NULLIF(LTRIM(RTRIM(ISNULL(T.VisaNumber,N''''))),N'''') IS NOT NULL THEN N''كارت'' ' ELSE N'' END
        + CASE WHEN COL_LENGTH('dbo.Transactions','IsCashOut') IS NOT NULL THEN N'WHEN ISNULL(T.IsCashOut,0) = 1 THEN N''كاش أوت'' ' ELSE N'' END
        + N'WHEN T.Transaction_Type = 9 THEN N''مرتجع'' ELSE N''كاش إن'' END';

    DECLARE @isCashOutExpr nvarchar(200), @isCardExpr nvarchar(400), @isViolationExpr nvarchar(400);
    SET @isCashOutExpr = CASE WHEN COL_LENGTH('dbo.Transactions','IsCashOut') IS NOT NULL THEN N'CASE WHEN ISNULL(T.IsCashOut,0)=1 THEN 1 ELSE 0 END' ELSE N'0' END;
    SET @isCardExpr = N'CASE WHEN 1=0 THEN 1 '
        + CASE WHEN COL_LENGTH('dbo.Transactions','IsPOS') IS NOT NULL THEN N'WHEN ISNULL(T.IsPOS,0)=1 THEN 1 ' ELSE N'' END
        + CASE WHEN COL_LENGTH('dbo.Transactions','VisaNumber') IS NOT NULL THEN N'WHEN NULLIF(LTRIM(RTRIM(ISNULL(T.VisaNumber,N''''))),N'''') IS NOT NULL THEN 1 ' ELSE N'' END
        + N'ELSE 0 END';
    SET @isViolationExpr = N'CASE WHEN 1=0 THEN 1 '
        + CASE WHEN COL_LENGTH('dbo.Transactions','TrafficViolations') IS NOT NULL THEN N'WHEN ISNULL(T.TrafficViolations,0)=1 THEN 1 ' ELSE N'' END
        + CASE WHEN COL_LENGTH('dbo.Transactions','ViolationsValue') IS NOT NULL THEN N'WHEN ISNULL(T.ViolationsValue,0)>0 THEN 1 ' ELSE N'' END
        + N'ELSE 0 END';

    DECLARE @customerJoin nvarchar(max);
    SET @customerJoin = CASE WHEN OBJECT_ID('dbo.TblCustemers','U') IS NOT NULL AND COL_LENGTH('dbo.Transactions','CusID') IS NOT NULL THEN N' LEFT JOIN dbo.TblCustemers C ON T.CusID=C.CusID ' ELSE N'' END;

    DECLARE @customerExpr nvarchar(400);
    SET @customerExpr = CASE WHEN OBJECT_ID('dbo.TblCustemers','U') IS NOT NULL AND COL_LENGTH('dbo.Transactions','CusID') IS NOT NULL THEN N'COALESCE(C.CusName,C.CusNamee,N'''')' ELSE N'N'''' ' END;

    DECLARE @sql nvarchar(max);
    SET @sql = N'
INSERT #AffectedInvoices(Transaction_ID, NoteID, InvoiceNo, InvoiceType, OperationTypeName, BranchId, Transaction_Date, CustomerName, Note_Value, IsPosted, IsClosed, KycReferenceIds)
SELECT TOP 5000 CAST(T.Transaction_ID AS bigint), CAST(' + @noteExpr + N' AS bigint), ' + @serialExpr + N',
       CAST(T.Transaction_Type AS int), ' + @operationTypeExpr + N', CAST(' + @branchExpr + N' AS int), T.Transaction_Date,
       ' + @customerExpr + N', CAST(ISNULL(' + @valueExpr + N',0) AS decimal(19,4)), ' + @postedExpr + N', 0, N''''
FROM dbo.Transactions T ' + @customerJoin + N'
WHERE (@BranchId IS NULL OR ' + @branchExpr + N' = @BranchId)
  AND (@DateFrom IS NULL OR T.Transaction_Date >= @DateFrom)
  AND (@DateTo IS NULL OR T.Transaction_Date < DATEADD(DAY,1,@DateTo))
  AND (
        (@InvoiceType IS NOT NULL AND T.Transaction_Type = @InvoiceType)
        OR (@InvoiceType IS NULL AND @InvoiceScope = N''SalesAndReturns'' AND T.Transaction_Type IN (21,9))
        OR (@InvoiceType IS NULL AND ISNULL(@InvoiceScope,N''SalesOnly'') <> N''SalesAndReturns'' AND T.Transaction_Type = 21)
      )
  AND (@InvoiceNo = N'''' OR ' + @serialExpr + N' LIKE N''%'' + @InvoiceNo + N''%'')
  AND (
        @InvoiceSource IS NULL OR @InvoiceSource = N''Both''
        OR (@InvoiceSource = N''ExcelOnly'' AND EXISTS (SELECT 1 FROM #ExcelImported R WHERE R.Transaction_ID = T.Transaction_ID))
        OR (@InvoiceSource = N''ManualOnly'' AND NOT EXISTS (SELECT 1 FROM #ExcelImported R WHERE R.Transaction_ID = T.Transaction_ID))
      )
  AND (
        @OperationKind IS NULL OR @OperationKind = N''All''
        OR (@OperationKind = N''CashOutOnly'' AND (' + @isCashOutExpr + N') = 1)
        OR (@OperationKind = N''CardsOnly'' AND (' + @isCardExpr + N') = 1)
        OR (@OperationKind = N''ViolationsOnly'' AND (' + @isViolationExpr + N') = 1)
        OR (@OperationKind = N''CashInOnly'' AND (' + @isCashOutExpr + N') = 0 AND (' + @isCardExpr + N') = 0 AND (' + @isViolationExpr + N') = 0)
      )
  AND (@SelectedTransactionIds = N'''' OR CHARINDEX(N'','' + CONVERT(nvarchar(30),T.Transaction_ID) + N'','', N'','' + @SelectedTransactionIds + N'','') > 0)
ORDER BY T.Transaction_Date DESC, T.Transaction_ID DESC';

    EXEC sp_executesql @sql,
        N'@BranchId int,@DateFrom datetime,@DateTo datetime,@InvoiceType int,@InvoiceScope nvarchar(30),@InvoiceSource nvarchar(30),@OperationKind nvarchar(30),@InvoiceNo nvarchar(100),@SelectedTransactionIds nvarchar(max)',
        @BranchId,@DateFrom,@DateTo,@InvoiceType,@InvoiceScope,@InvoiceSource,@OperationKind,@InvoiceNo,@SelectedTransactionIds;

    IF OBJECT_ID('dbo.TransactionKycLinks','U') IS NOT NULL
    BEGIN
        UPDATE A
        SET KycReferenceIds = STUFF((SELECT N',' + CONVERT(nvarchar(50), K.KycCustomerId)
                                     FROM dbo.TransactionKycLinks K
                                     WHERE K.Transaction_ID = A.Transaction_ID
                                     FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'')
        FROM #AffectedInvoices A;
    END

    SELECT Transaction_ID, InvoiceNo, InvoiceType, OperationTypeName, BranchId, Transaction_Date, CustomerName, Note_Value, IsPosted, IsClosed, KycReferenceIds
    FROM #AffectedInvoices
    ORDER BY Transaction_Date DESC, Transaction_ID DESC;

    CREATE TABLE #Dependency(Transaction_ID bigint, ModuleName nvarchar(100), TableName sysname, [RowCount] int, IsProtected int, ActionPolicy nvarchar(30));

    DECLARE c CURSOR LOCAL FAST_FORWARD FOR
        SELECT ModuleName, TableName, PrimaryKeyColumn, RelationColumn, RelationType, IsProtected, ActionPolicy
        FROM dbo.CriticalRecoveryTableMap
        WHERE IsActive=1 AND OBJECT_ID('dbo.' + TableName, 'U') IS NOT NULL AND IsKycMaster=0;

    DECLARE @module nvarchar(100), @table sysname, @pk sysname, @rel sysname, @relType nvarchar(30), @isProtected bit, @policy nvarchar(30);
    OPEN c;
    FETCH NEXT FROM c INTO @module,@table,@pk,@rel,@relType,@isProtected,@policy;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF @rel IS NOT NULL AND COL_LENGTH('dbo.' + @table, @rel) IS NOT NULL
        BEGIN
            SET @sql = N'INSERT #Dependency(Transaction_ID, ModuleName, TableName, [RowCount], IsProtected, ActionPolicy)
SELECT A.Transaction_ID, @ModuleName, @TableName, COUNT(1), @IsProtected, @ActionPolicy
FROM #AffectedInvoices A
JOIN dbo.' + QUOTENAME(@table) + N' X ON X.' + QUOTENAME(@rel) + N' = CASE WHEN @RelationType=N''NoteId'' THEN A.NoteID ELSE A.Transaction_ID END
GROUP BY A.Transaction_ID';
            EXEC sp_executesql @sql, N'@ModuleName nvarchar(100),@TableName sysname,@IsProtected bit,@ActionPolicy nvarchar(30),@RelationType nvarchar(30)', @module,@table,@isProtected,@policy,@relType;
        END
        FETCH NEXT FROM c INTO @module,@table,@pk,@rel,@relType,@isProtected,@policy;
    END
    CLOSE c;
    DEALLOCATE c;

    SELECT Transaction_ID, ModuleName, TableName, [RowCount], IsProtected, ActionPolicy
    FROM #Dependency
    WHERE [RowCount] > 0
    ORDER BY Transaction_ID, ModuleName, TableName;

    SELECT WarningMessage FROM
    (
        SELECT N'KYC master data is protected: customer/card/token/attachment rows are not deleted; only invoice links are archived for restore.' WarningMessage
        UNION ALL SELECT N'Closed/audited/exported period policy: ' + PolicyValue FROM dbo.CriticalRecoveryPolicy WHERE PolicyKey='ClosedPeriodPolicy'
    ) W;
END
GO

IF OBJECT_ID('dbo.usp_CriticalRecovery_InitiateRequest','P') IS NOT NULL
    DROP PROCEDURE dbo.usp_CriticalRecovery_InitiateRequest
GO
CREATE PROCEDURE dbo.usp_CriticalRecovery_InitiateRequest
    @BranchId int = NULL,
    @DateFrom datetime = NULL,
    @DateTo datetime = NULL,
    @InvoiceType int = NULL,
    @InvoiceScope nvarchar(30) = N'SalesOnly',
    @InvoiceSource nvarchar(30) = N'Both',
    @OperationKind nvarchar(30) = N'All',
    @InvoiceNo nvarchar(100) = N'',
    @CashierUserId nvarchar(128) = N'',
    @CustomerSearch nvarchar(200) = N'',
    @ClosingStatus nvarchar(30) = N'',
    @PostedStatus nvarchar(30) = N'',
    @HasAccountingEntry bit = 0,
    @HasStockMovement bit = 0,
    @HasKycLink bit = 0,
    @HasGeneratedVoucher bit = 0,
    @SelectedTransactionIds nvarchar(max) = N'',
    @Mode nvarchar(50),
    @Reason nvarchar(1000),
    @RequestedBy nvarchar(128),
    @SecondaryPassword nvarchar(256),
    @DangerConfirmation nvarchar(50) = N'',
    @DryRun bit = 0,
    @AllowPhysicalDelete bit = 0,
    @RequestHigherApprovalForClosedPeriod bit = 0,
    @DeleteOrphanKycRecords bit = 0,
    @MachineName nvarchar(128) = NULL,
    @IpAddress nvarchar(64) = NULL,
    @SessionId nvarchar(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@SecondaryPassword,N'') = N''
    BEGIN
        SELECT 0 Success, CAST(NULL AS int) RequestId, CAST(NULL AS bigint) SnapshotBatchId, N'Current admin password is required.' Message;
        RETURN;
    END
    SET @Reason = ISNULL(NULLIF(@Reason,N''), N'Admin password confirmed critical recovery.');
    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.TblUsers
        WHERE UserName=@RequestedBy
          AND [PassWord]=@SecondaryPassword
          AND ISNULL(isDeactivated,0)=0
          AND ISNULL(UserType,-1)=0
    )
    BEGIN
        SELECT 0 Success, CAST(NULL AS int) RequestId, CAST(NULL AS bigint) SnapshotBatchId, N'Current admin password validation failed.' Message;
        RETURN;
    END
    IF @DeleteOrphanKycRecords = 1
    BEGIN
        SELECT 0 Success, CAST(NULL AS int) RequestId, CAST(NULL AS bigint) SnapshotBatchId, N'Orphan KYC deletion is intentionally blocked in the request phase. Use preserve/archive KYC policy unless a separate compliance script validates all references.' Message;
        RETURN;
    END

    DECLARE @RequestId int;
    IF @InvoiceType IS NULL AND ISNULL(@InvoiceScope,N'SalesOnly') = N'SalesAndReturns'
    BEGIN
        SET @InvoiceType = -1;
    END

    IF @InvoiceType IS NULL AND ISNULL(@InvoiceScope,N'SalesOnly') <> N'SalesAndReturns'
    BEGIN
        SET @InvoiceType = 21;
    END

    INSERT dbo.CriticalRecoveryRequest(RequestedBy, Mode, Reason, BranchId, DateFrom, DateTo, InvoiceType, InvoiceSource, OperationKind, InvoiceNo, SelectedTransactionIds, DryRun, AllowPhysicalDelete, DeleteOrphanKycRecords, MachineName, IpAddress, SessionId)
    VALUES(@RequestedBy, @Mode, @Reason, @BranchId, @DateFrom, @DateTo, @InvoiceType, @InvoiceSource, @OperationKind, @InvoiceNo, @SelectedTransactionIds, @DryRun, @AllowPhysicalDelete, @DeleteOrphanKycRecords, @MachineName, @IpAddress, @SessionId);
    SET @RequestId = SCOPE_IDENTITY();

    INSERT dbo.CriticalRecoveryAudit(RequestId, ActionName, OperatorName, MachineName, IpAddress, SessionId, Result, Message)
    VALUES(@RequestId, N'InitiateRequest', @RequestedBy, @MachineName, @IpAddress, @SessionId, N'PendingApproval', N'Critical recovery request created. Execution requires admin password and explicit danger confirmation.');

    SELECT 1 Success, @RequestId RequestId, CAST(NULL AS bigint) SnapshotBatchId, N'Request #' + CONVERT(nvarchar(20),@RequestId) + N' is pending admin approval/execution.' Message;
END
GO

IF OBJECT_ID('dbo.usp_CriticalRecovery_ApproveAndExecute','P') IS NOT NULL
    DROP PROCEDURE dbo.usp_CriticalRecovery_ApproveAndExecute
GO
CREATE PROCEDURE dbo.usp_CriticalRecovery_ApproveAndExecute
    @RequestId int,
    @ApprovedBy nvarchar(128),
    @ApproverSecondaryPassword nvarchar(256),
    @DangerConfirmation nvarchar(50) = N'',
    @AllowPhysicalDelete bit = 0,
    @DeleteOrphanKycRecords bit = 0,
    @MachineName nvarchar(128) = NULL,
    @IpAddress nvarchar(64) = NULL,
    @SessionId nvarchar(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF ISNULL(@ApproverSecondaryPassword,N'') = N''
    BEGIN
        SELECT 0 Success, @RequestId RequestId, CAST(NULL AS bigint) SnapshotBatchId, N'Current admin password is required.' Message;
        RETURN;
    END
    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.TblUsers
        WHERE UserName=@ApprovedBy
          AND [PassWord]=@ApproverSecondaryPassword
          AND ISNULL(isDeactivated,0)=0
          AND ISNULL(UserType,-1)=0
    )
    BEGIN
        SELECT 0 Success, @RequestId RequestId, CAST(NULL AS bigint) SnapshotBatchId, N'Approver admin password validation failed.' Message;
        RETURN;
    END

    DECLARE @RequestedBy nvarchar(128), @Mode nvarchar(50), @Reason nvarchar(1000), @BranchId int, @DateFrom datetime, @DateTo datetime, @InvoiceType int, @InvoiceSource nvarchar(30), @OperationKind nvarchar(30), @InvoiceNo nvarchar(100), @Selected nvarchar(max), @DryRun bit;
    SELECT @RequestedBy=RequestedBy, @Mode=Mode, @Reason=Reason, @BranchId=BranchId, @DateFrom=DateFrom, @DateTo=DateTo, @InvoiceType=InvoiceType, @InvoiceSource=ISNULL(InvoiceSource,N'Both'), @OperationKind=ISNULL(OperationKind,N'All'), @InvoiceNo=InvoiceNo, @Selected=SelectedTransactionIds, @DryRun=DryRun
    FROM dbo.CriticalRecoveryRequest WITH (UPDLOCK, HOLDLOCK)
    WHERE RequestId=@RequestId AND Status='PendingApproval';

    IF @RequestedBy IS NULL
    BEGIN
        SELECT 0 Success, @RequestId RequestId, CAST(NULL AS bigint) SnapshotBatchId, N'Request was not found or is no longer pending.' Message;
        RETURN;
    END
    IF @DeleteOrphanKycRecords = 1
    BEGIN
        SELECT 0 Success, @RequestId RequestId, CAST(NULL AS bigint) SnapshotBatchId, N'KYC orphan physical deletion is blocked by this procedure. Preserve/archive KYC master data and handle compliance cleanup separately.' Message;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT dbo.CriticalRecoveryExecutionLock(LockName, AcquiredAt, AcquiredBy, SessionId)
        SELECT N'SalesInvoiceRecovery', GETDATE(), @ApprovedBy, @SessionId
        WHERE NOT EXISTS (SELECT 1 FROM dbo.CriticalRecoveryExecutionLock WITH (UPDLOCK, HOLDLOCK) WHERE LockName=N'SalesInvoiceRecovery');

        IF @@ROWCOUNT = 0
            RAISERROR(N'Another critical recovery execution is already running.', 16, 1);

        CREATE TABLE #AffectedInvoices(Transaction_ID bigint NOT NULL PRIMARY KEY, NoteID bigint NULL, InvoiceNo nvarchar(100) NULL, InvoiceType int NULL, BranchId int NULL, Transaction_Date datetime NULL, CustomerName nvarchar(300) NULL, Note_Value decimal(19,4) NULL, IsPosted int NOT NULL DEFAULT(0), IsClosed int NOT NULL DEFAULT(0), KycReferenceIds nvarchar(1000) NULL);
        CREATE TABLE #ExcelImported(Transaction_ID bigint NOT NULL PRIMARY KEY);
        IF OBJECT_ID('dbo.POS_ImportBatchRow','U') IS NOT NULL
        BEGIN
            INSERT #ExcelImported(Transaction_ID)
            SELECT DISTINCT CAST(TransactionId AS bigint)
            FROM dbo.POS_ImportBatchRow
            WHERE TransactionId IS NOT NULL AND Status = N'Imported';
        END

        DECLARE @noteExpr nvarchar(200);
        SET @noteExpr = CASE
            WHEN COL_LENGTH('dbo.Transactions','NoteID') IS NOT NULL THEN N'T.NoteID'
            WHEN COL_LENGTH('dbo.Transactions','Notes_ID') IS NOT NULL THEN N'T.Notes_ID'
            ELSE N'NULL'
        END;
        DECLARE @branchExpr nvarchar(200);
        SET @branchExpr = CASE WHEN COL_LENGTH('dbo.Transactions','BranchId') IS NOT NULL THEN N'T.BranchId' WHEN COL_LENGTH('dbo.Transactions','branch_id') IS NOT NULL THEN N'T.branch_id' ELSE N'NULL' END;
        DECLARE @serialExpr nvarchar(200);
        SET @serialExpr = CASE WHEN COL_LENGTH('dbo.Transactions','NoteSerial1') IS NOT NULL THEN N'CONVERT(nvarchar(100),T.NoteSerial1)' WHEN COL_LENGTH('dbo.Transactions','ManualNO') IS NOT NULL THEN N'CONVERT(nvarchar(100),T.ManualNO)' ELSE N'CONVERT(nvarchar(100),T.Transaction_ID)' END;
        DECLARE @postedExpr nvarchar(200);
        SET @postedExpr = CASE WHEN COL_LENGTH('dbo.Transactions','Posted') IS NOT NULL THEN N'CASE WHEN ISNULL(T.Posted,0)<>0 THEN 1 ELSE 0 END' ELSE N'0' END;

        DECLARE @valueExpr nvarchar(200);
        SET @valueExpr = CASE
            WHEN COL_LENGTH('dbo.Transactions','Note_Value') IS NOT NULL THEN N'T.Note_Value'
            WHEN COL_LENGTH('dbo.Transactions','NetValue') IS NOT NULL THEN N'T.NetValue'
            WHEN COL_LENGTH('dbo.Transactions','Total') IS NOT NULL THEN N'T.Total'
            WHEN COL_LENGTH('dbo.Transactions','FinacilaTotal') IS NOT NULL THEN N'T.FinacilaTotal'
            WHEN COL_LENGTH('dbo.Transactions','Transaction_NetValue') IS NOT NULL THEN N'T.Transaction_NetValue'
            ELSE N'0'
        END;
        DECLARE @customerJoin nvarchar(max);
        SET @customerJoin = CASE WHEN OBJECT_ID('dbo.TblCustemers','U') IS NOT NULL AND COL_LENGTH('dbo.Transactions','CusID') IS NOT NULL THEN N' LEFT JOIN dbo.TblCustemers C ON T.CusID=C.CusID ' ELSE N'' END;
        DECLARE @customerExpr nvarchar(400);
        SET @customerExpr = CASE WHEN OBJECT_ID('dbo.TblCustemers','U') IS NOT NULL AND COL_LENGTH('dbo.Transactions','CusID') IS NOT NULL THEN N'COALESCE(C.CusName,C.CusNamee,N'''')' ELSE N'N'''' ' END;

        DECLARE @isCashOutExpr nvarchar(200), @isCardExpr nvarchar(400), @isViolationExpr nvarchar(400);
        SET @isCashOutExpr = CASE WHEN COL_LENGTH('dbo.Transactions','IsCashOut') IS NOT NULL THEN N'CASE WHEN ISNULL(T.IsCashOut,0)=1 THEN 1 ELSE 0 END' ELSE N'0' END;
        SET @isCardExpr = N'CASE WHEN 1=0 THEN 1 '
            + CASE WHEN COL_LENGTH('dbo.Transactions','IsPOS') IS NOT NULL THEN N'WHEN ISNULL(T.IsPOS,0)=1 THEN 1 ' ELSE N'' END
            + CASE WHEN COL_LENGTH('dbo.Transactions','VisaNumber') IS NOT NULL THEN N'WHEN NULLIF(LTRIM(RTRIM(ISNULL(T.VisaNumber,N''''))),N'''') IS NOT NULL THEN 1 ' ELSE N'' END
            + N'ELSE 0 END';
        SET @isViolationExpr = N'CASE WHEN 1=0 THEN 1 '
            + CASE WHEN COL_LENGTH('dbo.Transactions','TrafficViolations') IS NOT NULL THEN N'WHEN ISNULL(T.TrafficViolations,0)=1 THEN 1 ' ELSE N'' END
            + CASE WHEN COL_LENGTH('dbo.Transactions','ViolationsValue') IS NOT NULL THEN N'WHEN ISNULL(T.ViolationsValue,0)>0 THEN 1 ' ELSE N'' END
            + N'ELSE 0 END';

        DECLARE @buildAffectedSql nvarchar(max);
        SET @buildAffectedSql = N'
INSERT #AffectedInvoices(Transaction_ID, NoteID, InvoiceNo, InvoiceType, BranchId, Transaction_Date, CustomerName, Note_Value, IsPosted, IsClosed, KycReferenceIds)
SELECT TOP 5000 CAST(T.Transaction_ID AS bigint), CAST(' + @noteExpr + N' AS bigint), ' + @serialExpr + N',
       CAST(T.Transaction_Type AS int), CAST(' + @branchExpr + N' AS int), T.Transaction_Date,
       ' + @customerExpr + N', CAST(ISNULL(' + @valueExpr + N',0) AS decimal(19,4)), ' + @postedExpr + N', 0, N''''
FROM dbo.Transactions T ' + @customerJoin + N'
WHERE (@BranchId IS NULL OR ' + @branchExpr + N' = @BranchId)
  AND (@DateFrom IS NULL OR T.Transaction_Date >= @DateFrom)
  AND (@DateTo IS NULL OR T.Transaction_Date < DATEADD(DAY,1,@DateTo))
  AND (
        @InvoiceType IS NULL
        OR (@InvoiceType = -1 AND T.Transaction_Type IN (21,9))
        OR T.Transaction_Type = @InvoiceType
      )
  AND (@InvoiceNo = N'''' OR ' + @serialExpr + N' LIKE N''%'' + @InvoiceNo + N''%'')
  AND (
        @InvoiceSource IS NULL OR @InvoiceSource = N''Both''
        OR (@InvoiceSource = N''ExcelOnly'' AND EXISTS (SELECT 1 FROM #ExcelImported R WHERE R.Transaction_ID = T.Transaction_ID))
        OR (@InvoiceSource = N''ManualOnly'' AND NOT EXISTS (SELECT 1 FROM #ExcelImported R WHERE R.Transaction_ID = T.Transaction_ID))
      )
  AND (
        @OperationKind IS NULL OR @OperationKind = N''All''
        OR (@OperationKind = N''CashOutOnly'' AND (' + @isCashOutExpr + N') = 1)
        OR (@OperationKind = N''CardsOnly'' AND (' + @isCardExpr + N') = 1)
        OR (@OperationKind = N''ViolationsOnly'' AND (' + @isViolationExpr + N') = 1)
        OR (@OperationKind = N''CashInOnly'' AND (' + @isCashOutExpr + N') = 0 AND (' + @isCardExpr + N') = 0 AND (' + @isViolationExpr + N') = 0)
      )
  AND (@Selected = N'''' OR CHARINDEX(N'','' + CONVERT(nvarchar(30),T.Transaction_ID) + N'','', N'','' + @Selected + N'','') > 0)
ORDER BY T.Transaction_Date DESC, T.Transaction_ID DESC';

        EXEC sp_executesql @buildAffectedSql,
            N'@BranchId int,@DateFrom datetime,@DateTo datetime,@InvoiceType int,@InvoiceSource nvarchar(30),@OperationKind nvarchar(30),@InvoiceNo nvarchar(100),@Selected nvarchar(max)',
            @BranchId,@DateFrom,@DateTo,@InvoiceType,@InvoiceSource,@OperationKind,@InvoiceNo,@Selected;

        DECLARE @BatchId bigint;
        INSERT dbo.CriticalRecoverySnapshotBatch(RequestId, RequestedBy, ApprovedBy, MachineName, IpAddress, SessionId, Mode, Reason, BranchId, InvoiceType, DateFrom, DateTo, InvoiceCount, Status)
        SELECT @RequestId, @RequestedBy, @ApprovedBy, @MachineName, @IpAddress, @SessionId, @Mode, @Reason, @BranchId, @InvoiceType, @DateFrom, @DateTo, COUNT(1), CASE WHEN @DryRun=1 THEN N'DryRunSnapshot' ELSE N'SnapshotCreated' END
        FROM #AffectedInvoices;
        SET @BatchId = SCOPE_IDENTITY();

        DECLARE c CURSOR LOCAL FAST_FORWARD FOR
            SELECT TableName, PrimaryKeyColumn, RelationColumn, RelationType
            FROM dbo.CriticalRecoveryTableMap
            WHERE IsActive=1 AND IsKycMaster=0 AND OBJECT_ID('dbo.' + TableName, 'U') IS NOT NULL
            ORDER BY SnapshotOrder;

        DECLARE @table sysname, @pk sysname, @rel sysname, @relType nvarchar(30), @sql nvarchar(max), @archive sysname, @cols nvarchar(max), @colsArchive nvarchar(max), @join nvarchar(max), @archiveHasIdentity bit;
        OPEN c;
        FETCH NEXT FROM c INTO @table,@pk,@rel,@relType;
        WHILE @@FETCH_STATUS=0
        BEGIN
            IF COL_LENGTH('dbo.' + @table, @pk) IS NOT NULL AND @rel IS NOT NULL AND COL_LENGTH('dbo.' + @table, @rel) IS NOT NULL
            BEGIN
                SET @archive = N'CriticalRecoveryArchive_' + @table;
                IF OBJECT_ID('dbo.' + @archive, 'U') IS NULL
                BEGIN
                    SET @sql = N'SELECT TOP 0 CAST(NULL AS bigint) SnapshotBatchId, CAST(NULL AS datetime) ArchivedAt, CAST(NULL AS nvarchar(128)) ArchivedBy, X.* INTO dbo.' + QUOTENAME(@archive) + N' FROM dbo.' + QUOTENAME(@table) + N' X WHERE 1=0';
                    EXEC(@sql);
                END

                SELECT @cols = STUFF((SELECT N',' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.' + @table) AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
                SELECT @colsArchive = STUFF((SELECT N',X.' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.' + @table) AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
                SELECT @archiveHasIdentity = CASE WHEN EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.' + @archive) AND is_identity=1) THEN 1 ELSE 0 END;
                SET @join = CASE WHEN @relType=N'NoteId' THEN N'A.NoteID' ELSE N'A.Transaction_ID' END;

                SET @sql = N'
' + CASE WHEN @archiveHasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@archive) + N' ON;' ELSE N'' END + N'
INSERT dbo.' + QUOTENAME(@archive) + N'(SnapshotBatchId, ArchivedAt, ArchivedBy,' + @cols + N')
SELECT @BatchId, GETDATE(), @ApprovedBy,' + @colsArchive + N'
FROM dbo.' + QUOTENAME(@table) + N' X
JOIN #AffectedInvoices A ON X.' + QUOTENAME(@rel) + N' = ' + @join + N';
' + CASE WHEN @archiveHasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@archive) + N' OFF;' ELSE N'' END + N'

INSERT dbo.CriticalRecoverySnapshotRow(SnapshotBatchId, OriginalTable, OriginalPrimaryKey, Transaction_ID, BranchId, InvoiceType, InvoiceNo, XmlData, RowHash)
SELECT @BatchId, @TableName, CONVERT(nvarchar(200),X.' + QUOTENAME(@pk) + N'), A.Transaction_ID, A.BranchId, A.InvoiceType, A.InvoiceNo,
       (SELECT X.* FOR XML PATH(''row''), TYPE), NULL
FROM dbo.' + QUOTENAME(@table) + N' X
JOIN #AffectedInvoices A ON X.' + QUOTENAME(@rel) + N' = ' + @join + N';';
                EXEC sp_executesql @sql, N'@BatchId bigint,@ApprovedBy nvarchar(128),@TableName sysname', @BatchId,@ApprovedBy,@table;
            END
            FETCH NEXT FROM c INTO @table,@pk,@rel,@relType;
        END
        CLOSE c;
        DEALLOCATE c;

        CREATE TABLE #AffectedClosingDays(BranchId int NOT NULL, ClosingDate date NOT NULL, CONSTRAINT PK_AffectedClosingDays PRIMARY KEY(BranchId, ClosingDate));
        INSERT #AffectedClosingDays(BranchId, ClosingDate)
        SELECT DISTINCT BranchId, CAST(Transaction_Date AS date)
        FROM #AffectedInvoices
        WHERE BranchId IS NOT NULL AND Transaction_Date IS NOT NULL;

        CREATE TABLE #AffectedClosingNotes(NoteID int NOT NULL PRIMARY KEY);

        IF OBJECT_ID('dbo.TBLClosePos','U') IS NOT NULL
        BEGIN
            INSERT #AffectedClosingNotes(NoteID)
            SELECT DISTINCT C.NoteID
            FROM dbo.TBLClosePos C
            JOIN #AffectedClosingDays D ON D.BranchId = C.BranchID AND D.ClosingDate = CAST(C.OrderDate AS date)
            WHERE C.NoteID IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM #AffectedClosingNotes X WHERE X.NoteID = C.NoteID);

            SET @archive = N'CriticalRecoveryArchive_TBLClosePos';
            IF OBJECT_ID('dbo.' + @archive, 'U') IS NULL
            BEGIN
                SET @sql = N'SELECT TOP 0 CAST(NULL AS bigint) SnapshotBatchId, CAST(NULL AS datetime) ArchivedAt, CAST(NULL AS nvarchar(128)) ArchivedBy, X.* INTO dbo.' + QUOTENAME(@archive) + N' FROM dbo.TBLClosePos X WHERE 1=0';
                EXEC(@sql);
            END

            SELECT @cols = STUFF((SELECT N',' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.TBLClosePos') AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @colsArchive = STUFF((SELECT N',X.' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.TBLClosePos') AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @archiveHasIdentity = CASE WHEN EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.' + @archive) AND is_identity=1) THEN 1 ELSE 0 END;
            SET @sql = N'
' + CASE WHEN @archiveHasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@archive) + N' ON;' ELSE N'' END + N'
INSERT dbo.' + QUOTENAME(@archive) + N'(SnapshotBatchId, ArchivedAt, ArchivedBy,' + @cols + N')
SELECT @BatchId, GETDATE(), @ApprovedBy,' + @colsArchive + N'
FROM dbo.TBLClosePos X
JOIN #AffectedClosingDays D ON D.BranchId = X.BranchID AND D.ClosingDate = CAST(X.OrderDate AS date);
' + CASE WHEN @archiveHasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@archive) + N' OFF;' ELSE N'' END + N'

INSERT dbo.CriticalRecoverySnapshotRow(SnapshotBatchId, OriginalTable, OriginalPrimaryKey, Transaction_ID, BranchId, InvoiceType, InvoiceNo, XmlData, RowHash)
SELECT @BatchId, N''TBLClosePos'', CONVERT(nvarchar(200),X.ID), NULL, X.BranchID, NULL, CONVERT(nvarchar(100),X.NoteSerial1),
       (SELECT X.* FOR XML PATH(''row''), TYPE), NULL
FROM dbo.TBLClosePos X
JOIN #AffectedClosingDays D ON D.BranchId = X.BranchID AND D.ClosingDate = CAST(X.OrderDate AS date);';
            EXEC sp_executesql @sql, N'@BatchId bigint,@ApprovedBy nvarchar(128)', @BatchId,@ApprovedBy;
        END

        IF OBJECT_ID('dbo.POS_DailyClosingSummary','U') IS NOT NULL
        BEGIN
            SET @archive = N'CriticalRecoveryArchive_POS_DailyClosingSummary';
            IF OBJECT_ID('dbo.' + @archive, 'U') IS NULL
            BEGIN
                SET @sql = N'SELECT TOP 0 CAST(NULL AS bigint) SnapshotBatchId, CAST(NULL AS datetime) ArchivedAt, CAST(NULL AS nvarchar(128)) ArchivedBy, X.* INTO dbo.' + QUOTENAME(@archive) + N' FROM dbo.POS_DailyClosingSummary X WHERE 1=0';
                EXEC(@sql);
            END

            SELECT @cols = STUFF((SELECT N',' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.POS_DailyClosingSummary') AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @colsArchive = STUFF((SELECT N',X.' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.POS_DailyClosingSummary') AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @archiveHasIdentity = CASE WHEN EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.' + @archive) AND is_identity=1) THEN 1 ELSE 0 END;
            SET @sql = N'
' + CASE WHEN @archiveHasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@archive) + N' ON;' ELSE N'' END + N'
INSERT dbo.' + QUOTENAME(@archive) + N'(SnapshotBatchId, ArchivedAt, ArchivedBy,' + @cols + N')
SELECT @BatchId, GETDATE(), @ApprovedBy,' + @colsArchive + N'
FROM dbo.POS_DailyClosingSummary X
JOIN #AffectedClosingDays D ON D.BranchId = X.BranchId AND D.ClosingDate = X.SummaryDate;
' + CASE WHEN @archiveHasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@archive) + N' OFF;' ELSE N'' END + N'

INSERT dbo.CriticalRecoverySnapshotRow(SnapshotBatchId, OriginalTable, OriginalPrimaryKey, Transaction_ID, BranchId, InvoiceType, InvoiceNo, XmlData, RowHash)
SELECT @BatchId, N''POS_DailyClosingSummary'', CONVERT(nvarchar(200),X.SummaryId), NULL, X.BranchId, NULL, CONVERT(nvarchar(100),X.SummaryDate,120),
       (SELECT X.* FOR XML PATH(''row''), TYPE), NULL
FROM dbo.POS_DailyClosingSummary X
JOIN #AffectedClosingDays D ON D.BranchId = X.BranchId AND D.ClosingDate = X.SummaryDate;';
            EXEC sp_executesql @sql, N'@BatchId bigint,@ApprovedBy nvarchar(128)', @BatchId,@ApprovedBy;
        END

        IF OBJECT_ID('dbo.Notes','U') IS NOT NULL AND EXISTS (SELECT 1 FROM #AffectedClosingNotes)
        BEGIN
            SET @archive = N'CriticalRecoveryArchive_Notes';
            IF OBJECT_ID('dbo.' + @archive, 'U') IS NULL
            BEGIN
                SET @sql = N'SELECT TOP 0 CAST(NULL AS bigint) SnapshotBatchId, CAST(NULL AS datetime) ArchivedAt, CAST(NULL AS nvarchar(128)) ArchivedBy, X.* INTO dbo.' + QUOTENAME(@archive) + N' FROM dbo.Notes X WHERE 1=0';
                EXEC(@sql);
            END

            SELECT @cols = STUFF((SELECT N',' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Notes') AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @colsArchive = STUFF((SELECT N',X.' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Notes') AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @archiveHasIdentity = CASE WHEN EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.' + @archive) AND is_identity=1) THEN 1 ELSE 0 END;
            SET @sql = N'
' + CASE WHEN @archiveHasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@archive) + N' ON;' ELSE N'' END + N'
INSERT dbo.' + QUOTENAME(@archive) + N'(SnapshotBatchId, ArchivedAt, ArchivedBy,' + @cols + N')
SELECT @BatchId, GETDATE(), @ApprovedBy,' + @colsArchive + N'
FROM dbo.Notes X
JOIN #AffectedClosingNotes N ON N.NoteID = X.NoteID;
' + CASE WHEN @archiveHasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@archive) + N' OFF;' ELSE N'' END + N'

INSERT dbo.CriticalRecoverySnapshotRow(SnapshotBatchId, OriginalTable, OriginalPrimaryKey, Transaction_ID, BranchId, InvoiceType, InvoiceNo, XmlData, RowHash)
SELECT @BatchId, N''Notes'', CONVERT(nvarchar(200),X.NoteID), NULL, X.branch_no, NULL, CONVERT(nvarchar(100),X.NoteSerial1),
       (SELECT X.* FOR XML PATH(''row''), TYPE), NULL
FROM dbo.Notes X
JOIN #AffectedClosingNotes N ON N.NoteID = X.NoteID;';
            EXEC sp_executesql @sql, N'@BatchId bigint,@ApprovedBy nvarchar(128)', @BatchId,@ApprovedBy;
        END

        IF OBJECT_ID('dbo.DOUBLE_ENTREY_VOUCHERS','U') IS NOT NULL AND EXISTS (SELECT 1 FROM #AffectedClosingNotes)
        BEGIN
            SET @archive = N'CriticalRecoveryArchive_DOUBLE_ENTREY_VOUCHERS';
            IF OBJECT_ID('dbo.' + @archive, 'U') IS NULL
            BEGIN
                SET @sql = N'SELECT TOP 0 CAST(NULL AS bigint) SnapshotBatchId, CAST(NULL AS datetime) ArchivedAt, CAST(NULL AS nvarchar(128)) ArchivedBy, X.* INTO dbo.' + QUOTENAME(@archive) + N' FROM dbo.DOUBLE_ENTREY_VOUCHERS X WHERE 1=0';
                EXEC(@sql);
            END

            SELECT @cols = STUFF((SELECT N',' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.DOUBLE_ENTREY_VOUCHERS') AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @colsArchive = STUFF((SELECT N',X.' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.DOUBLE_ENTREY_VOUCHERS') AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @archiveHasIdentity = CASE WHEN EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.' + @archive) AND is_identity=1) THEN 1 ELSE 0 END;
            SET @sql = N'
' + CASE WHEN @archiveHasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@archive) + N' ON;' ELSE N'' END + N'
INSERT dbo.' + QUOTENAME(@archive) + N'(SnapshotBatchId, ArchivedAt, ArchivedBy,' + @cols + N')
SELECT @BatchId, GETDATE(), @ApprovedBy,' + @colsArchive + N'
FROM dbo.DOUBLE_ENTREY_VOUCHERS X
JOIN #AffectedClosingNotes N ON N.NoteID = X.Notes_ID;
' + CASE WHEN @archiveHasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@archive) + N' OFF;' ELSE N'' END + N'

INSERT dbo.CriticalRecoverySnapshotRow(SnapshotBatchId, OriginalTable, OriginalPrimaryKey, Transaction_ID, BranchId, InvoiceType, InvoiceNo, XmlData, RowHash)
SELECT @BatchId, N''DOUBLE_ENTREY_VOUCHERS'', CONVERT(nvarchar(200),X.Double_Entry_Vouchers_ID), NULL, X.branch_id, NULL, CONVERT(nvarchar(100),X.Notes_ID),
       (SELECT X.* FOR XML PATH(''row''), TYPE), NULL
FROM dbo.DOUBLE_ENTREY_VOUCHERS X
JOIN #AffectedClosingNotes N ON N.NoteID = X.Notes_ID;';
            EXEC sp_executesql @sql, N'@BatchId bigint,@ApprovedBy nvarchar(128)', @BatchId,@ApprovedBy;
        END

        UPDATE dbo.CriticalRecoverySnapshotBatch
        SET SnapshotRowCount = (SELECT COUNT(1) FROM dbo.CriticalRecoverySnapshotRow WHERE SnapshotBatchId=@BatchId)
        WHERE SnapshotBatchId=@BatchId;

        IF @DryRun = 0
        BEGIN
            IF COL_LENGTH('dbo.Transactions','CriticalRecoveryStatus') IS NULL
                ALTER TABLE dbo.Transactions ADD CriticalRecoveryStatus nvarchar(30) NULL, CriticalRecoveryBatchId bigint NULL, CriticalRecoveryAt datetime NULL;

            SET @sql = N'
UPDATE T
SET CriticalRecoveryStatus = CASE WHEN @Mode=N''CancelOnly'' THEN N''Cancelled'' ELSE N''Archived'' END,
    CriticalRecoveryBatchId = @BatchId,
    CriticalRecoveryAt = GETDATE()
FROM dbo.Transactions T
JOIN #AffectedInvoices A ON T.Transaction_ID=A.Transaction_ID;';
            EXEC sp_executesql @sql, N'@Mode nvarchar(50),@BatchId bigint', @Mode,@BatchId;

            IF @AllowPhysicalDelete = 1 AND @Mode IN (N'FullRollback', N'PeriodCleanup', N'BranchCleanup')
            BEGIN
                DECLARE d CURSOR LOCAL FAST_FORWARD FOR
                    SELECT TableName, PrimaryKeyColumn, RelationColumn, RelationType
                    FROM dbo.CriticalRecoveryTableMap
                    WHERE IsActive=1 AND IsProtected=0 AND IsKycMaster=0 AND TableName <> N'Transactions' AND OBJECT_ID('dbo.' + TableName, 'U') IS NOT NULL
                    ORDER BY ReverseOrder;
                OPEN d;
                FETCH NEXT FROM d INTO @table,@pk,@rel,@relType;
                WHILE @@FETCH_STATUS=0
                BEGIN
                    IF @rel IS NOT NULL AND COL_LENGTH('dbo.' + @table, @rel) IS NOT NULL
                    BEGIN
                        SET @join = CASE WHEN @relType=N'NoteId' THEN N'A.NoteID' ELSE N'A.Transaction_ID' END;
                        SET @sql = N'DELETE X FROM dbo.' + QUOTENAME(@table) + N' X JOIN #AffectedInvoices A ON X.' + QUOTENAME(@rel) + N' = ' + @join;
                        EXEC(@sql);
                    END
                    FETCH NEXT FROM d INTO @table,@pk,@rel,@relType;
                END
                CLOSE d;
                DEALLOCATE d;

                IF OBJECT_ID('dbo.DOUBLE_ENTREY_VOUCHERS','U') IS NOT NULL
                    DELETE X FROM dbo.DOUBLE_ENTREY_VOUCHERS X JOIN #AffectedClosingNotes N ON N.NoteID = X.Notes_ID;

                IF OBJECT_ID('dbo.Notes','U') IS NOT NULL
                    DELETE X FROM dbo.Notes X JOIN #AffectedClosingNotes N ON N.NoteID = X.NoteID;

                IF OBJECT_ID('dbo.TBLClosePos','U') IS NOT NULL
                    DELETE X FROM dbo.TBLClosePos X JOIN #AffectedClosingDays D ON D.BranchId = X.BranchID AND D.ClosingDate = CAST(X.OrderDate AS date);

                IF OBJECT_ID('dbo.POS_DailyClosingSummary','U') IS NOT NULL
                    DELETE X FROM dbo.POS_DailyClosingSummary X JOIN #AffectedClosingDays D ON D.BranchId = X.BranchId AND D.ClosingDate = X.SummaryDate;

                DELETE T
                FROM dbo.Transactions T
                JOIN #AffectedInvoices A ON T.Transaction_ID = A.Transaction_ID;
            END
        END

        UPDATE dbo.CriticalRecoveryRequest
        SET ApprovedAt=GETDATE(), ApprovedBy=@ApprovedBy, Status=CASE WHEN @DryRun=1 THEN N'DryRunCompleted' ELSE N'Executed' END, SnapshotBatchId=@BatchId
        WHERE RequestId=@RequestId;

        UPDATE dbo.CriticalRecoverySnapshotBatch
        SET CompletedAt=GETDATE(), Status=CASE WHEN @DryRun=1 THEN N'DryRunCompleted' ELSE N'Executed' END
        WHERE SnapshotBatchId=@BatchId;

        INSERT dbo.CriticalRecoveryAudit(SnapshotBatchId, RequestId, ActionName, OperatorName, ApproverName, MachineName, IpAddress, SessionId, Result, Message)
        VALUES(@BatchId, @RequestId, N'ApproveAndExecute', @RequestedBy, @ApprovedBy, @MachineName, @IpAddress, @SessionId, N'Success', N'Snapshot completed before controlled reversal. KYC master tables preserved.');

        DELETE dbo.CriticalRecoveryExecutionLock WHERE LockName=N'SalesInvoiceRecovery';
        COMMIT TRANSACTION;

        SELECT 1 Success, @RequestId RequestId, @BatchId SnapshotBatchId, N'Critical recovery executed with snapshot batch #' + CONVERT(nvarchar(30),@BatchId) + N'.' Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DELETE dbo.CriticalRecoveryExecutionLock WHERE LockName=N'SalesInvoiceRecovery' AND AcquiredBy=@ApprovedBy;
        UPDATE dbo.CriticalRecoveryRequest SET Status=N'Failed', FailureMessage=ERROR_MESSAGE() WHERE RequestId=@RequestId;
        INSERT dbo.CriticalRecoveryAudit(RequestId, ActionName, OperatorName, ApproverName, MachineName, IpAddress, SessionId, Result, Message)
        VALUES(@RequestId, N'ApproveAndExecute', ISNULL(@RequestedBy,N''), @ApprovedBy, @MachineName, @IpAddress, @SessionId, N'Failed', ERROR_MESSAGE());
        SELECT 0 Success, @RequestId RequestId, CAST(NULL AS bigint) SnapshotBatchId, ERROR_MESSAGE() Message;
    END CATCH
END
GO

IF OBJECT_ID('dbo.usp_CriticalRecovery_Restore','P') IS NOT NULL
    DROP PROCEDURE dbo.usp_CriticalRecovery_Restore
GO
CREATE PROCEDURE dbo.usp_CriticalRecovery_Restore
    @SnapshotBatchId bigint,
    @TransactionId bigint = NULL,
    @RestoreScope nvarchar(30) = N'Full',
    @RestoreBy nvarchar(128),
    @SecondaryPassword nvarchar(256),
    @Reason nvarchar(1000),
    @MachineName nvarchar(128) = NULL,
    @IpAddress nvarchar(64) = NULL,
    @SessionId nvarchar(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF ISNULL(@SecondaryPassword,N'')=N''
    BEGIN
        SELECT 0 Success, @SnapshotBatchId SnapshotBatchId, N'Restore admin password is required.' Message;
        RETURN;
    END
    SET @Reason = ISNULL(NULLIF(@Reason,N''), N'Admin password confirmed restore.');
    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.TblUsers
        WHERE UserName=@RestoreBy
          AND [PassWord]=@SecondaryPassword
          AND ISNULL(isDeactivated,0)=0
          AND ISNULL(UserType,-1)=0
    )
    BEGIN
        SELECT 0 Success, @SnapshotBatchId SnapshotBatchId, N'Restore admin password validation failed.' Message;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        CREATE TABLE #RestoreTransaction(Transaction_ID bigint NOT NULL PRIMARY KEY);
        INSERT #RestoreTransaction(Transaction_ID)
        SELECT DISTINCT Transaction_ID
        FROM dbo.CriticalRecoverySnapshotRow
        WHERE SnapshotBatchId=@SnapshotBatchId AND (@TransactionId IS NULL OR Transaction_ID=@TransactionId) AND Transaction_ID IS NOT NULL;

        DECLARE c CURSOR LOCAL FAST_FORWARD FOR
            SELECT TableName, PrimaryKeyColumn
            FROM dbo.CriticalRecoveryTableMap
            WHERE IsActive=1 AND IsKycMaster=0 AND OBJECT_ID('dbo.CriticalRecoveryArchive_' + TableName, 'U') IS NOT NULL
            ORDER BY SnapshotOrder;

        DECLARE @table sysname, @pk sysname, @archive sysname, @cols nvarchar(max), @sql nvarchar(max), @hasIdentity bit;
        OPEN c;
        FETCH NEXT FROM c INTO @table,@pk;
        WHILE @@FETCH_STATUS=0
        BEGIN
            SET @archive=N'CriticalRecoveryArchive_' + @table;
            SELECT @cols = STUFF((SELECT N',' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.' + @table) AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @hasIdentity = CASE WHEN EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.' + @table) AND is_identity=1) THEN 1 ELSE 0 END;

            SET @sql = N'
' + CASE WHEN @hasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@table) + N' ON;' ELSE N'' END + N'
INSERT dbo.' + QUOTENAME(@table) + N'(' + @cols + N')
SELECT ' + @cols + N'
FROM dbo.' + QUOTENAME(@archive) + N' A
WHERE A.SnapshotBatchId=@SnapshotBatchId
  AND (@TransactionId IS NULL OR EXISTS(SELECT 1 FROM dbo.CriticalRecoverySnapshotRow SR WHERE SR.SnapshotBatchId=@SnapshotBatchId AND SR.Transaction_ID=@TransactionId AND SR.OriginalTable=@TableName AND SR.OriginalPrimaryKey=CONVERT(nvarchar(200),A.' + QUOTENAME(@pk) + N')))
  AND NOT EXISTS(SELECT 1 FROM dbo.' + QUOTENAME(@table) + N' X WHERE X.' + QUOTENAME(@pk) + N'=A.' + QUOTENAME(@pk) + N');
' + CASE WHEN @hasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@table) + N' OFF;' ELSE N'' END;
            EXEC sp_executesql @sql, N'@SnapshotBatchId bigint,@TransactionId bigint,@TableName sysname', @SnapshotBatchId,@TransactionId,@table;
            FETCH NEXT FROM c INTO @table,@pk;
        END
        CLOSE c;
        DEALLOCATE c;

        IF OBJECT_ID('dbo.TBLClosePos','U') IS NOT NULL AND OBJECT_ID('dbo.CriticalRecoveryArchive_TBLClosePos','U') IS NOT NULL
        BEGIN
            SET @table = N'TBLClosePos';
            SET @archive = N'CriticalRecoveryArchive_TBLClosePos';
            SET @pk = N'ID';
            SELECT @cols = STUFF((SELECT N',' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.TBLClosePos') AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @hasIdentity = CASE WHEN EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.TBLClosePos') AND is_identity=1) THEN 1 ELSE 0 END;

            SET @sql = N'
' + CASE WHEN @hasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.TBLClosePos ON;' ELSE N'' END + N'
INSERT dbo.TBLClosePos(' + @cols + N')
SELECT ' + @cols + N'
FROM dbo.CriticalRecoveryArchive_TBLClosePos A
WHERE A.SnapshotBatchId=@SnapshotBatchId
  AND NOT EXISTS(SELECT 1 FROM dbo.TBLClosePos X WHERE X.ID=A.ID);
' + CASE WHEN @hasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.TBLClosePos OFF;' ELSE N'' END;
            EXEC sp_executesql @sql, N'@SnapshotBatchId bigint', @SnapshotBatchId;
        END

        IF OBJECT_ID('dbo.POS_DailyClosingSummary','U') IS NOT NULL AND OBJECT_ID('dbo.CriticalRecoveryArchive_POS_DailyClosingSummary','U') IS NOT NULL
        BEGIN
            SET @table = N'POS_DailyClosingSummary';
            SET @archive = N'CriticalRecoveryArchive_POS_DailyClosingSummary';
            SET @pk = N'SummaryId';
            SELECT @cols = STUFF((SELECT N',' + QUOTENAME(name) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.POS_DailyClosingSummary') AND is_computed=0 AND system_type_id <> 189 FOR XML PATH(''), TYPE).value('.','nvarchar(max)'),1,1,N'');
            SELECT @hasIdentity = CASE WHEN EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.POS_DailyClosingSummary') AND is_identity=1) THEN 1 ELSE 0 END;

            SET @sql = N'
' + CASE WHEN @hasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.POS_DailyClosingSummary ON;' ELSE N'' END + N'
INSERT dbo.POS_DailyClosingSummary(' + @cols + N')
SELECT ' + @cols + N'
FROM dbo.CriticalRecoveryArchive_POS_DailyClosingSummary A
WHERE A.SnapshotBatchId=@SnapshotBatchId
  AND NOT EXISTS(SELECT 1 FROM dbo.POS_DailyClosingSummary X WHERE X.SummaryId=A.SummaryId);
' + CASE WHEN @hasIdentity=1 THEN N'SET IDENTITY_INSERT dbo.POS_DailyClosingSummary OFF;' ELSE N'' END;
            EXEC sp_executesql @sql, N'@SnapshotBatchId bigint', @SnapshotBatchId;
        END

        IF COL_LENGTH('dbo.Transactions','CriticalRecoveryStatus') IS NOT NULL
        BEGIN
            SET @sql = N'
UPDATE T
SET CriticalRecoveryStatus=NULL, CriticalRecoveryBatchId=NULL, CriticalRecoveryAt=NULL
FROM dbo.Transactions T
JOIN #RestoreTransaction R ON T.Transaction_ID=R.Transaction_ID;';
            EXEC(@sql);
        END

        UPDATE dbo.CriticalRecoverySnapshotBatch
        SET Status=N'Restored'
        WHERE SnapshotBatchId=@SnapshotBatchId;

        INSERT dbo.CriticalRecoveryAudit(SnapshotBatchId, ActionName, OperatorName, MachineName, IpAddress, SessionId, Result, Message)
        VALUES(@SnapshotBatchId, N'Restore', @RestoreBy, @MachineName, @IpAddress, @SessionId, N'Success', N'Restore completed from archive tables. Scope=' + @RestoreScope + N'. KYC records were re-linked to existing master rows only.');

        COMMIT TRANSACTION;
        SELECT 1 Success, @SnapshotBatchId SnapshotBatchId, N'Restore completed from snapshot batch #' + CONVERT(nvarchar(30),@SnapshotBatchId) + N'.' Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        INSERT dbo.CriticalRecoveryAudit(SnapshotBatchId, ActionName, OperatorName, MachineName, IpAddress, SessionId, Result, Message)
        VALUES(@SnapshotBatchId, N'Restore', @RestoreBy, @MachineName, @IpAddress, @SessionId, N'Failed', ERROR_MESSAGE());
        SELECT 0 Success, @SnapshotBatchId SnapshotBatchId, ERROR_MESSAGE() Message;
    END CATCH
END
GO
