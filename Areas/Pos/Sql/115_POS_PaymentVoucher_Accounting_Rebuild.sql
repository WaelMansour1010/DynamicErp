/*
    17_PaymentVoucher_Accounting_Rebuild.sql

    Rebuilds Payment Voucher / سند الصرف accounting generation from VB6 intent:
    - Debit the paid party/account.
    - Debit VAT when present using TblSettsReqLimK AccDep for TransType=23.
    - Debit transfer expense and transfer-expense VAT when explicitly supplied.
    - Credit the treasury/bank/account source for the total paid amount.
    - Edit deletes old DOUBLE_ENTREY_VOUCHERS rows then rebuilds atomically.

    SQL Server compatibility: SQL Server 2012+
*/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.DynamicErpIdCounters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynamicErpIdCounters
    (
        TableName sysname NOT NULL,
        FieldName sysname NOT NULL,
        CurrentValue bigint NOT NULL,
        LastSeededValue bigint NOT NULL,
        LastUpdated datetime NOT NULL CONSTRAINT DF_DynamicErpIdCounters_LastUpdated DEFAULT(GETDATE()),
        UpdatedBy nvarchar(128) NULL,
        CONSTRAINT PK_DynamicErpIdCounters PRIMARY KEY (TableName, FieldName)
    );
END
GO

DECLARE @SeedLockResult int;
EXEC @SeedLockResult = sys.sp_getapplock
    @Resource = N'DynamicErpIdCounters:PaymentVoucherSeed',
    @LockMode = 'Exclusive',
    @LockOwner = 'Session',
    @LockTimeout = 30000;

IF @SeedLockResult < 0
BEGIN
    RAISERROR(N'Unable to acquire payment voucher ID-counter seed lock.', 16, 1);
END
ELSE
BEGIN
    DECLARE @MaxNotesNoteId bigint;
    DECLARE @MaxDevId bigint;
    DECLARE @MaxDevLineNo1 bigint;

    SELECT @MaxNotesNoteId = ISNULL(MAX(CONVERT(bigint, NoteID)), 0)
    FROM dbo.Notes WITH (HOLDLOCK, UPDLOCK);

    SELECT
        @MaxDevId = ISNULL(MAX(CONVERT(bigint, Double_Entry_Vouchers_ID)), 0),
        @MaxDevLineNo1 = ISNULL(MAX(CONVERT(bigint, DEV_ID_Line_No1)), 0)
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (HOLDLOCK, UPDLOCK);

    IF NOT EXISTS (SELECT 1 FROM dbo.DynamicErpIdCounters WITH (UPDLOCK, HOLDLOCK) WHERE TableName = N'Notes' AND FieldName = N'NoteID')
        INSERT INTO dbo.DynamicErpIdCounters (TableName, FieldName, CurrentValue, LastSeededValue, UpdatedBy)
        VALUES (N'Notes', N'NoteID', @MaxNotesNoteId, @MaxNotesNoteId, N'17_PaymentVoucher_Accounting_Rebuild seed');
    ELSE
        UPDATE dbo.DynamicErpIdCounters
        SET CurrentValue = CASE WHEN CurrentValue < @MaxNotesNoteId THEN @MaxNotesNoteId ELSE CurrentValue END,
            LastSeededValue = @MaxNotesNoteId,
            LastUpdated = GETDATE(),
            UpdatedBy = N'17_PaymentVoucher_Accounting_Rebuild seed'
        WHERE TableName = N'Notes' AND FieldName = N'NoteID';

    IF NOT EXISTS (SELECT 1 FROM dbo.DynamicErpIdCounters WITH (UPDLOCK, HOLDLOCK) WHERE TableName = N'DOUBLE_ENTREY_VOUCHERS' AND FieldName = N'Double_Entry_Vouchers_ID')
        INSERT INTO dbo.DynamicErpIdCounters (TableName, FieldName, CurrentValue, LastSeededValue, UpdatedBy)
        VALUES (N'DOUBLE_ENTREY_VOUCHERS', N'Double_Entry_Vouchers_ID', @MaxDevId, @MaxDevId, N'17_PaymentVoucher_Accounting_Rebuild seed');
    ELSE
        UPDATE dbo.DynamicErpIdCounters
        SET CurrentValue = CASE WHEN CurrentValue < @MaxDevId THEN @MaxDevId ELSE CurrentValue END,
            LastSeededValue = @MaxDevId,
            LastUpdated = GETDATE(),
            UpdatedBy = N'17_PaymentVoucher_Accounting_Rebuild seed'
        WHERE TableName = N'DOUBLE_ENTREY_VOUCHERS' AND FieldName = N'Double_Entry_Vouchers_ID';

    IF NOT EXISTS (SELECT 1 FROM dbo.DynamicErpIdCounters WITH (UPDLOCK, HOLDLOCK) WHERE TableName = N'DOUBLE_ENTREY_VOUCHERS' AND FieldName = N'DEV_ID_Line_No1')
        INSERT INTO dbo.DynamicErpIdCounters (TableName, FieldName, CurrentValue, LastSeededValue, UpdatedBy)
        VALUES (N'DOUBLE_ENTREY_VOUCHERS', N'DEV_ID_Line_No1', @MaxDevLineNo1, @MaxDevLineNo1, N'17_PaymentVoucher_Accounting_Rebuild seed');
    ELSE
        UPDATE dbo.DynamicErpIdCounters
        SET CurrentValue = CASE WHEN CurrentValue < @MaxDevLineNo1 THEN @MaxDevLineNo1 ELSE CurrentValue END,
            LastSeededValue = @MaxDevLineNo1,
            LastUpdated = GETDATE(),
            UpdatedBy = N'17_PaymentVoucher_Accounting_Rebuild seed'
        WHERE TableName = N'DOUBLE_ENTREY_VOUCHERS' AND FieldName = N'DEV_ID_Line_No1';

    EXEC sys.sp_releaseapplock
        @Resource = N'DynamicErpIdCounters:PaymentVoucherSeed',
        @LockOwner = 'Session';
END
GO

IF OBJECT_ID(N'dbo.SerialCounters_V2', N'U') IS NOT NULL
BEGIN
    DECLARE @ReceiptCounters TABLE
    (
        SourceTable varchar(50) NOT NULL,
        BranchID int NOT NULL,
        TypeCode int NOT NULL,
        Prefix varchar(10) NULL,
        StoreID int NULL,
        NumberingType tinyint NOT NULL,
        YearNum int NOT NULL,
        MonthNum int NOT NULL,
        CurrentTail bigint NOT NULL,
        NoOfDigits tinyint NOT NULL,
        YearDigits tinyint NOT NULL,
        StartAt int NOT NULL,
        EndAt int NULL
    );

    ;WITH ReceiptConfiguredScopes AS
    (
        SELECT
            SourceTable = 'Notes.NoteSerial1',
            BranchID = sn.branch_no,
            TypeCode = 4,
            Prefix = CONVERT(varchar(10), sn.Prefix),
            StoreID = CAST(NULL AS int),
            NumberingType = CONVERT(tinyint, ISNULL(sn.numbering_id, 0)),
            NoOfDigits = CONVERT(tinyint, CASE WHEN ISNULL(sn.no_of_digit, 0) <= 0 THEN 3 ELSE sn.no_of_digit END),
            YearDigits = CONVERT(tinyint, CASE WHEN ISNULL(sn.YearDigit, 0) <= 0 THEN 4 ELSE sn.YearDigit END),
            StartAt = CASE WHEN ISNULL(sn.start_at, 0) <= 0 THEN 1 ELSE CONVERT(int, sn.start_at) END,
            EndAt = NULLIF(CONVERT(int, ISNULL(sn.end_at, 0)), 0)
        FROM dbo.sanad_numbering sn WITH (NOLOCK)
        WHERE sn.sanad_no = 2
          AND ISNULL(sn.numbering_id, 0) IN (1, 2, 3)
    )
    INSERT INTO @ReceiptCounters
        (SourceTable, BranchID, TypeCode, Prefix, StoreID, NumberingType, YearNum, MonthNum, CurrentTail, NoOfDigits, YearDigits, StartAt, EndAt)
    SELECT
        s.SourceTable,
        s.BranchID,
        s.TypeCode,
        s.Prefix,
        s.StoreID,
        s.NumberingType,
        YearNum = CASE WHEN s.NumberingType IN (2, 3) THEN YEAR(n.NoteDate) ELSE 0 END,
        MonthNum = CASE WHEN s.NumberingType = 2 THEN MONTH(n.NoteDate) ELSE 0 END,
        CurrentTail = MAX
        (
            CASE
                WHEN s.NumberingType = 1 THEN CONVERT(bigint, CONVERT(decimal(38,0), n.NoteSerial1))
                ELSE CONVERT(bigint, RIGHT(CONVERT(varchar(50), CONVERT(decimal(38,0), n.NoteSerial1)), s.NoOfDigits))
            END
        ),
        s.NoOfDigits,
        s.YearDigits,
        s.StartAt,
        s.EndAt
    FROM ReceiptConfiguredScopes s
    INNER JOIN dbo.Notes n WITH (NOLOCK)
        ON n.NoteSerial1 IS NOT NULL
       AND n.NoteType = 4
       AND n.branch_no = s.BranchID
       AND (n.Prefix = s.Prefix OR (n.Prefix IS NULL AND s.Prefix IS NULL))
    WHERE ISNUMERIC(n.NoteSerial1) = 1
    GROUP BY s.SourceTable, s.BranchID, s.TypeCode, s.Prefix, s.StoreID, s.NumberingType,
             CASE WHEN s.NumberingType IN (2, 3) THEN YEAR(n.NoteDate) ELSE 0 END,
             CASE WHEN s.NumberingType = 2 THEN MONTH(n.NoteDate) ELSE 0 END,
             s.NoOfDigits, s.YearDigits, s.StartAt, s.EndAt;

    UPDATE c
    SET CurrentTail = CASE WHEN c.CurrentTail < r.CurrentTail THEN r.CurrentTail ELSE c.CurrentTail END,
        NoOfDigits = r.NoOfDigits,
        YearDigits = r.YearDigits,
        StartAt = r.StartAt,
        EndAt = r.EndAt,
        LastUpdated = GETDATE(),
        UpdatedByUser = '17_ReceiptVoucher seed',
        UpdateCount = c.UpdateCount + 1
    FROM dbo.SerialCounters_V2 c WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN @ReceiptCounters r
        ON r.SourceTable = c.SourceTable
       AND r.BranchID = c.BranchID
       AND r.TypeCode = c.TypeCode
       AND (r.Prefix = c.Prefix OR (r.Prefix IS NULL AND c.Prefix IS NULL))
       AND (r.StoreID = c.StoreID OR (r.StoreID IS NULL AND c.StoreID IS NULL))
       AND r.YearNum = c.YearNum
       AND r.MonthNum = c.MonthNum;

    INSERT INTO dbo.SerialCounters_V2
        (SourceTable, BranchID, TypeCode, Prefix, StoreID, NumberingType, YearNum, MonthNum, CurrentTail, NoOfDigits, YearDigits, StartAt, EndAt, UpdatedByUser, UpdateCount)
    SELECT r.SourceTable, r.BranchID, r.TypeCode, r.Prefix, r.StoreID, r.NumberingType, r.YearNum, r.MonthNum, r.CurrentTail, r.NoOfDigits, r.YearDigits, r.StartAt, r.EndAt, '17_ReceiptVoucher seed', 1
    FROM @ReceiptCounters r
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.SerialCounters_V2 c WITH (UPDLOCK, HOLDLOCK)
        WHERE c.SourceTable = r.SourceTable
          AND c.BranchID = r.BranchID
          AND c.TypeCode = r.TypeCode
          AND (c.Prefix = r.Prefix OR (c.Prefix IS NULL AND r.Prefix IS NULL))
          AND (c.StoreID = r.StoreID OR (c.StoreID IS NULL AND r.StoreID IS NULL))
          AND c.YearNum = r.YearNum
          AND c.MonthNum = r.MonthNum
    );
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErp_AllocateIntId', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErp_AllocateIntId;
GO
CREATE PROCEDURE dbo.usp_DynamicErp_AllocateIntId
    @tableName sysname,
    @fieldName sysname,
    @nextValue bigint OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @@TRANCOUNT = 0
    BEGIN
        RAISERROR(N'ID allocation must run inside the voucher transaction.', 16, 1);
        RETURN 1;
    END

    IF @tableName NOT IN (N'Notes', N'DOUBLE_ENTREY_VOUCHERS')
       OR (@tableName = N'Notes' AND @fieldName <> N'NoteID')
       OR (@tableName = N'DOUBLE_ENTREY_VOUCHERS' AND @fieldName NOT IN (N'Double_Entry_Vouchers_ID', N'DEV_ID_Line_No1'))
    BEGIN
        RAISERROR(N'Unsupported ID allocator target.', 16, 1);
        RETURN 1;
    END

    DECLARE @lockResult int;
    DECLARE @resource nvarchar(255);
    DECLARE @candidateExists bit;

    SET @nextValue = NULL;
    SET @resource = N'DynamicErpIdCounters:' + @tableName + N'.' + @fieldName;

    EXEC @lockResult = sys.sp_getapplock
        @Resource = @resource,
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 30000;

    IF @lockResult < 0
    BEGIN
        RAISERROR(N'Unable to acquire ID allocation lock.', 16, 1);
        RETURN 1;
    END

    UPDATE dbo.DynamicErpIdCounters WITH (UPDLOCK, HOLDLOCK)
    SET CurrentValue = CurrentValue + 1,
        @nextValue = CurrentValue + 1,
        LastUpdated = GETDATE(),
        UpdatedBy = N'usp_DynamicErp_AllocateIntId'
    WHERE TableName = @tableName
      AND FieldName = @fieldName;

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'ID counter is not seeded. Run the payment voucher migration script before saving.', 16, 1);
        RETURN 1;
    END

    SET @candidateExists = 1;
    WHILE @candidateExists = 1
    BEGIN
        IF @tableName = N'Notes'
            SELECT @candidateExists = CASE WHEN EXISTS (SELECT 1 FROM dbo.Notes WITH (READCOMMITTEDLOCK) WHERE NoteID = CONVERT(int, @nextValue)) THEN 1 ELSE 0 END;
        ELSE IF @fieldName = N'Double_Entry_Vouchers_ID'
            SELECT @candidateExists = CASE WHEN EXISTS (SELECT 1 FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK) WHERE Double_Entry_Vouchers_ID = CONVERT(int, @nextValue)) THEN 1 ELSE 0 END;
        ELSE
            SELECT @candidateExists = CASE WHEN EXISTS (SELECT 1 FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK) WHERE DEV_ID_Line_No1 = CONVERT(float, @nextValue)) THEN 1 ELSE 0 END;

        IF @candidateExists = 1
        BEGIN
            UPDATE dbo.DynamicErpIdCounters WITH (UPDLOCK, HOLDLOCK)
            SET CurrentValue = CurrentValue + 1,
                @nextValue = CurrentValue + 1,
                LastUpdated = GETDATE(),
                UpdatedBy = N'usp_DynamicErp_AllocateIntId duplicate repair'
            WHERE TableName = @tableName
              AND FieldName = @fieldName;
        END
    END

    IF @nextValue IS NULL OR @nextValue > 2147483647
    BEGIN
        RAISERROR(N'Allocated ID is outside the supported integer range.', 16, 1);
        RETURN 1;
    END

    RETURN 0;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_CleanupLinkedRows', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_CleanupLinkedRows;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_CleanupLinkedRows
    @noteId int,
    @noteSerial1 nvarchar(50) = NULL,
    @mode nvarchar(20) = N'REBUILD',
    @deleteNote bit = 0,
    @noteType int = 5
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @startedTransaction bit,
        @payDes nvarchar(4000),
        @payDes1 nvarchar(4000),
        @supplierPayDes nvarchar(4000),
        @advanceId int,
        @dueId int,
        @endServiceId int,
        @cashingType int,
        @xml xml;

    DECLARE @PrepaidIds TABLE (ID int NOT NULL PRIMARY KEY);
    DECLARE @AttributionIds TABLE (ID int NOT NULL PRIMARY KEY);
    DECLARE @QestIds TABLE (ID int NOT NULL PRIMARY KEY);

    SET @startedTransaction = 0;
    IF @@TRANCOUNT = 0
    BEGIN
        SET @startedTransaction = 1;
        BEGIN TRANSACTION;
    END

    SELECT
        @noteSerial1 = COALESCE(NULLIF(@noteSerial1, N''), CONVERT(nvarchar(50), CONVERT(decimal(38,0), NoteSerial1))),
        @payDes = PayDes,
        @payDes1 = PayDes1,
        @supplierPayDes = TxtNoSupplerDes,
        @advanceId = NULLIF(AdvanceID, 0),
        @dueId = NULLIF(Due, 0),
        @endServiceId = NULLIF(TxtEndService, 0),
        @cashingType = CashingType
    FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK)
    WHERE NoteID = @noteId AND NoteType = @noteType;

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'سند الصرف غير موجود.', 16, 1);
        IF @startedTransaction = 1 ROLLBACK TRANSACTION;
        RETURN 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.Notes WHERE NoteID = @noteId AND NoteType = @noteType AND ISNULL(NotePosted, 0) = 1)
    BEGIN
        RAISERROR(N'لا يمكن تعديل أو حذف سند صرف مرحل.', 16, 1);
        IF @startedTransaction = 1 ROLLBACK TRANSACTION;
        RETURN 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.Notes WHERE NoteID = @noteId AND NoteType = @noteType AND ISNULL(AssestPayd, 0) = 1)
    BEGIN
        RAISERROR(N'لا يمكن تعديل أو حذف السند لوجود عملية أصول مرتبطة به.', 16, 1);
        IF @startedTransaction = 1 ROLLBACK TRANSACTION;
        RETURN 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.TblChecqueBoxContent1 WITH (UPDLOCK, HOLDLOCK) WHERE NoteID = @noteId AND ISNULL(Payed, 0) = 1)
    BEGIN
        RAISERROR(N'لا يمكن تعديل أو حذف السند لوجود شيك مرتبط تم تحصيله أو ترحيله.', 16, 1);
        IF @startedTransaction = 1 ROLLBACK TRANSACTION;
        RETURN 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.TblChecqueBoxContent WITH (UPDLOCK, HOLDLOCK) WHERE NoteID = @noteId AND (ISNULL(Deposited, 0) = 1 OR ISNULL(Collected, 0) = 1))
    BEGIN
        RAISERROR(N'لا يمكن تعديل أو حذف السند لوجود شيك مرتبط تم إيداعه أو تحصيله.', 16, 1);
        IF @startedTransaction = 1 ROLLBACK TRANSACTION;
        RETURN 1;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.TblEmpAdvancePayedDet WITH (UPDLOCK, HOLDLOCK)
        WHERE AdvanceID = @advanceId
           OR AdvanceID = @noteId
           OR AdvanceID IN (SELECT AdvanceID FROM dbo.TblEmpAdvance WHERE NoteID = @noteId OR AdvanceID = @noteId)
    )
    BEGIN
        RAISERROR(N'لا يمكن تعديل أو حذف السند لوجود عملية رد سلفة مرتبطة به.', 16, 1);
        IF @startedTransaction = 1 ROLLBACK TRANSACTION;
        RETURN 1;
    END

    IF NULLIF(LTRIM(RTRIM(ISNULL(@payDes, N''))), N'') IS NOT NULL
    BEGIN
        BEGIN TRY
            SET @xml = CONVERT(xml, N'<x>' + REPLACE(REPLACE(REPLACE(@payDes, N' ', N''), N'،', N','), N',', N'</x><x>') + N'</x>');
            INSERT INTO @PrepaidIds (ID)
            SELECT DISTINCT T.C.value(N'.', N'int')
            FROM @xml.nodes(N'/x') AS T(C)
            WHERE ISNUMERIC(T.C.value(N'.', N'nvarchar(50)')) = 1;
        END TRY
        BEGIN CATCH
            RAISERROR(N'بيانات ربط المصروفات المقدمة غير صالحة ولا يمكن تنظيفها بأمان.', 16, 1);
            IF @startedTransaction = 1 ROLLBACK TRANSACTION;
            RETURN 1;
        END CATCH
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.TblPripaidExpChiled WITH (UPDLOCK, HOLDLOCK)
        WHERE etfa = 1
          AND paidexiddet IN (SELECT ID FROM @PrepaidIds)
    )
    BEGIN
        RAISERROR(N'لا يمكن تعديل أو حذف السند لأن بعض المصروفات المقدمة تم إطفاؤها.', 16, 1);
        IF @startedTransaction = 1 ROLLBACK TRANSACTION;
        RETURN 1;
    END

    IF NULLIF(LTRIM(RTRIM(ISNULL(@supplierPayDes, N''))), N'') IS NOT NULL
    BEGIN
        BEGIN TRY
            SET @xml = CONVERT(xml, N'<x>' + REPLACE(REPLACE(REPLACE(@supplierPayDes, N' ', N''), N'،', N','), N',', N'</x><x>') + N'</x>');
            INSERT INTO @AttributionIds (ID)
            SELECT DISTINCT T.C.value(N'.', N'int')
            FROM @xml.nodes(N'/x') AS T(C)
            WHERE ISNUMERIC(T.C.value(N'.', N'nvarchar(50)')) = 1;
        END TRY
        BEGIN CATCH
            RAISERROR(N'بيانات ربط أقساط المورد غير صالحة ولا يمكن تنظيفها بأمان.', 16, 1);
            IF @startedTransaction = 1 ROLLBACK TRANSACTION;
            RETURN 1;
        END CATCH
    END

    IF EXISTS (SELECT 1 FROM @PrepaidIds)
        UPDATE dbo.TblPripaidExpensesDet
        SET PaymentPayed = 0
        WHERE ID IN (SELECT ID FROM @PrepaidIds);

    IF EXISTS (SELECT 1 FROM @AttributionIds)
        UPDATE dbo.TblAttributionInstallmentDivided
        SET NoteSerial1 = NULL,
            noteid = NULL,
            PayMentPayed = NULL
        WHERE ID IN (SELECT ID FROM @AttributionIds);

    UPDATE t
    SET TotalPayed = 0
    FROM dbo.Transactions t
    WHERE t.Transaction_ID IN (SELECT NoteID FROM dbo.TblNotesBillBuyPayment WHERE NoteID1 = @noteId);

    UPDATE p
    SET TotalPayed = 0
    FROM dbo.project_billl p
    WHERE p.ID IN (SELECT NoteID FROM dbo.TblNotesBillProjectPayment WHERE NoteID1 = @noteId);

    UPDATE n
    SET TotalPayed = 0,
        FlgPaye = NULL
    FROM dbo.notes_all n
    WHERE n.NoteID IN (SELECT NoteID FROM dbo.TblNotesBillVindorPayment WHERE NoteID1 = @noteId);

    BEGIN TRY
        INSERT INTO @QestIds (ID)
        SELECT DISTINCT T.C.value(N'.', N'int')
        FROM
        (
            SELECT CONVERT(xml, N'<x>' + REPLACE(REPLACE(REPLACE(StrQest, N' ', N''), N'،', N','), N',', N'</x><x>') + N'</x>') AS QestXml
            FROM dbo.TblNotesBillVindorPayment
            WHERE NoteID1 = @noteId
              AND NULLIF(LTRIM(RTRIM(ISNULL(StrQest, N''))), N'') IS NOT NULL
        ) q
        CROSS APPLY q.QestXml.nodes(N'/x') AS T(C)
        WHERE ISNUMERIC(T.C.value(N'.', N'nvarchar(50)')) = 1;
    END TRY
    BEGIN CATCH
        RAISERROR(N'بيانات ربط الأقساط غير صالحة ولا يمكن تنظيفها بأمان.', 16, 1);
        IF @startedTransaction = 1 ROLLBACK TRANSACTION;
        RETURN 1;
    END CATCH

    IF EXISTS (SELECT 1 FROM @QestIds)
        UPDATE dbo.TblQestFexed
        SET FlgPaye = NULL
        WHERE QestID IN (SELECT ID FROM @QestIds);

    IF @cashingType = 8 AND @dueId IS NOT NULL
        UPDATE dbo.TblVocationEntitlements SET PayedPayment = NULL WHERE ID = @dueId;

    IF @cashingType = 10 AND @endServiceId IS NOT NULL
        UPDATE dbo.End_of_service SET PaymPaid = NULL WHERE ID = @endServiceId;

    IF @cashingType = 12 AND @endServiceId IS NOT NULL
        UPDATE dbo.TblVATAvowal SET Paid = NULL WHERE ID = @endServiceId;

    DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId;
    DELETE FROM dbo.TblSalaryNotesPayment WHERE TransID = @noteId;
    DELETE FROM dbo.marakes_taklefa_temp WHERE kedno = @noteId;
    DELETE FROM dbo.ReciveDetails WHERE NoteSerial1 = @noteSerial1;
    DELETE FROM dbo.TblChecqueBoxContent1 WHERE NoteID = @noteId;
    DELETE FROM dbo.TblChecqueBoxContent WHERE NoteID = @noteId;
    DELETE FROM dbo.ContracttBillInstallmentsDone WHERE NoteID = @noteId;
    DELETE FROM dbo.TblUnitNoInformation WHERE NoteID = @noteId;
    DELETE FROM dbo.TblAqrEarnest WHERE NoteID = @noteId;
    DELETE FROM dbo.ProjectBillBuy WHERE noteid = @noteId OR NoteSerial1 = @noteSerial1;
    DELETE FROM dbo.TblAqarCommissions WHERE NoteID = @noteId;
    DELETE FROM dbo.TblOtheExpensAqar WHERE NoteID = @noteId OR NoteSerial1 = @noteSerial1;

    DELETE FROM dbo.TblEmpAdvanceDetails
    WHERE AdvanceID = @noteId
       OR AdvanceID IN (SELECT AdvanceID FROM dbo.TblEmpAdvance WHERE NoteID = @noteId OR AdvanceID = @noteId);

    DELETE FROM dbo.TblEmpAdvance WHERE NoteID = @noteId OR AdvanceID = @noteId;

    UPDATE dbo.TblEmpAdvanceRequest
    SET AccAproved = NULL
    WHERE AdvanceID = @noteId OR AdvanceID = @advanceId;

    DELETE FROM dbo.TblBillBuyPayment WHERE TypTrans IS NULL AND NoteID = @noteId;
    DELETE FROM dbo.TblNotesBillBuyPayment WHERE NoteID1 = @noteId;
    DELETE FROM dbo.TblBillProjectPayment WHERE NoteID = @noteId;
    DELETE FROM dbo.TblNotesBillProjectPayment WHERE NoteID1 = @noteId;
    DELETE FROM dbo.TblBillVindorPayment WHERE NoteID = @noteId;
    DELETE FROM dbo.TblNotesBillVindorPayment WHERE NoteID1 = @noteId;

    IF EXISTS (SELECT 1 FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblSalaryNotesPayment WHERE TransID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.marakes_taklefa_temp WHERE kedno = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.ReciveDetails WHERE NoteSerial1 = @noteSerial1)
       OR EXISTS (SELECT 1 FROM dbo.TblChecqueBoxContent1 WHERE NoteID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblChecqueBoxContent WHERE NoteID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.ContracttBillInstallmentsDone WHERE NoteID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblUnitNoInformation WHERE NoteID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblAqrEarnest WHERE NoteID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.ProjectBillBuy WHERE noteid = @noteId OR NoteSerial1 = @noteSerial1)
       OR EXISTS (SELECT 1 FROM dbo.TblAqarCommissions WHERE NoteID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblOtheExpensAqar WHERE NoteID = @noteId OR NoteSerial1 = @noteSerial1)
       OR EXISTS (SELECT 1 FROM dbo.TblEmpAdvance WHERE NoteID = @noteId OR AdvanceID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblEmpAdvanceDetails WHERE AdvanceID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblNotesBillBuyPayment WHERE NoteID1 = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblBillBuyPayment WHERE TypTrans IS NULL AND NoteID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblNotesBillProjectPayment WHERE NoteID1 = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblBillProjectPayment WHERE NoteID = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblNotesBillVindorPayment WHERE NoteID1 = @noteId)
       OR EXISTS (SELECT 1 FROM dbo.TblBillVindorPayment WHERE NoteID = @noteId)
    BEGIN
        RAISERROR(N'تعذر تنظيف كل الجداول المرتبطة بسند الصرف. تم إلغاء العملية لحماية البيانات.', 16, 1);
        IF @startedTransaction = 1 ROLLBACK TRANSACTION;
        RETURN 1;
    END

    IF @deleteNote = 1
        DELETE FROM dbo.Notes WHERE NoteID = @noteId AND NoteType = @noteType;

    IF @startedTransaction = 1 COMMIT TRANSACTION;
    RETURN 0;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_Save;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_Save
    @noteType int,
    @noteId int = NULL,
    @noteDate datetime,
    @manualNo nvarchar(255) = NULL,
    @orderNo nvarchar(50) = NULL,
    @partyAccountCode nvarchar(55),
    @partyDisplay nvarchar(4000) = NULL,
    @branchId int = NULL,
    @boxId int = NULL,
    @bankId int = NULL,
    @paymentMethod int = NULL,
    @cashingType int = NULL,
    @receiptClass int = NULL,
    @chequeNumber nvarchar(255) = NULL,
    @chequeDueDate datetime = NULL,
    @amount decimal(18,2),
    @vat decimal(18,2) = 0,
    @includeVat int = 0,
    @remark nvarchar(4000) = NULL,
    @payDes nvarchar(4000) = NULL,
    @payDes1 nvarchar(4000) = NULL,
    @userId int,
    @transferExpense decimal(18,2) = 0,
    @transferExpenseAccountCode nvarchar(55) = NULL,
    @transferExpenseVat decimal(18,2) = 0,
    @transferExpenseVatAccountCode nvarchar(55) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @noteType NOT IN (4, 5) BEGIN RAISERROR(N'نوع السند غير مدعوم في الحفظ الآمن.', 16, 1); RETURN; END
    IF ISNULL(@amount, 0) <= 0 BEGIN RAISERROR(N'يجب إدخال قيمة سند صحيحة.', 16, 1); RETURN; END
    IF NULLIF(LTRIM(RTRIM(ISNULL(@partyAccountCode, N''))), N'') IS NULL BEGIN RAISERROR(N'يجب اختيار الحساب.', 16, 1); RETURN; END
    IF @branchId IS NULL OR @branchId = 0 BEGIN RAISERROR(N'يجب اختيار الفرع.', 16, 1); RETURN; END
    IF ISNULL(@paymentMethod, -1) NOT IN (0, 2) BEGIN RAISERROR(N'طريقة الدفع المختارة غير مدعومة في الحفظ الآمن الحالي. المدعوم حاليا: نقدي أو تحويل بنكي.', 16, 1); RETURN; END
    IF @noteType = 5 AND ISNULL(@cashingType, -1) NOT IN (0, 1, 2, 5) BEGIN RAISERROR(N'نوع سند الصرف المختار يحتاج منطق ربط خاص من VB6 ولم يتم تفعيله بعد في الحفظ الآمن.', 16, 1); RETURN; END
    IF @noteType = 4 AND ISNULL(@cashingType, -1) NOT IN (0, 1, 2, 7) BEGIN RAISERROR(N'نوع سند القبض المختار يحتاج منطق ربط خاص من VB6 ولم يتم تفعيله بعد في الحفظ الآمن.', 16, 1); RETURN; END
    IF (@paymentMethod = 0 AND @boxId IS NULL) BEGIN RAISERROR(N'يجب اختيار الصندوق لطريقة الدفع النقدي.', 16, 1); RETURN; END
    IF (@paymentMethod = 2 AND @bankId IS NULL) BEGIN RAISERROR(N'يجب اختيار البنك لطريقة التحويل البنكي.', 16, 1); RETURN; END
    IF (@paymentMethod = 2 AND NULLIF(LTRIM(RTRIM(ISNULL(@chequeNumber, N''))), N'') IS NULL) BEGIN RAISERROR(N'يجب إدخال رقم الحوالة لطريقة التحويل البنكي.', 16, 1); RETURN; END
    IF (@boxId IS NOT NULL AND @bankId IS NOT NULL) BEGIN RAISERROR(N'اختر صندوقا أو بنكا فقط.', 16, 1); RETURN; END

    DECLARE
        @partyAccount nvarchar(55),
        @creditAccount nvarchar(55),
        @vatAccount nvarchar(55),
        @transferExpenseAccount nvarchar(55),
        @transferExpenseVatAccount nvarchar(55),
        @mainDebitAmount decimal(18,2),
        @vatAmount decimal(18,2),
        @totalPaymentAmount decimal(18,2),
        @transferExpenseAmount decimal(18,2),
        @transferExpenseVatAmount decimal(18,2),
        @nextDevIdBig bigint,
        @nextLineNo1Big bigint,
        @lastVoucherId int,
        @noteSerial varchar(50),
        @noteSerial1 varchar(50),
        @noteSerialNumeric float,
        @noteSerial1Numeric float,
        @isEdit bit,
        @isReceipt bit,
        @voucherSanadNo int,
        @voucherPrefix varchar(10),
        @description nvarchar(4000),
        @accountIntervalId int,
        @rc int;

    DECLARE @JournalAllocations TABLE
    (
        [LineNo] int NOT NULL PRIMARY KEY,
        DoubleEntryVoucherId int NOT NULL,
        LineNo1 float NOT NULL
    );

    DECLARE @JournalLines TABLE
    (
        [LineNo] int IDENTITY(1,1) NOT NULL,
        AccountCode nvarchar(55) NOT NULL,
        NextAccountCode nvarchar(55) NULL,
        Amount decimal(18,2) NOT NULL,
        CreditOrDebit smallint NOT NULL,
        LineDescription nvarchar(4000) NULL,
        FlgVat int NULL,
        VatValue decimal(18,2) NULL,
        TotalValue decimal(18,2) NULL
    );

    SET @partyAccount = LTRIM(RTRIM(@partyAccountCode));
    SET @isReceipt = CASE WHEN @noteType = 4 THEN 1 ELSE 0 END;
    SET @description = COALESCE(NULLIF(@remark, N''), NULLIF(@payDes, N''), CASE WHEN @noteType = 4 THEN N'سند قبض' ELSE N'سند صرف' END);
    SET @isEdit = CASE WHEN ISNULL(@noteId, 0) > 0 THEN 1 ELSE 0 END;
    SET @voucherSanadNo = CASE WHEN @noteType = 4 THEN 2 ELSE 4 END;
    SET @voucherPrefix = CASE WHEN @noteType = 5 THEN CASE @paymentMethod WHEN 0 THEN 'CSH' WHEN 1 THEN 'CHQ' WHEN 2 THEN 'TRN' ELSE NULL END ELSE NULL END;
    SET @vatAmount = ISNULL(@vat, 0);
    SET @transferExpenseAmount = ISNULL(@transferExpense, 0);
    SET @transferExpenseVatAmount = ISNULL(@transferExpenseVat, 0);

    IF @vatAmount < 0 OR @transferExpenseAmount < 0 OR @transferExpenseVatAmount < 0
    BEGIN RAISERROR(N'لا يجوز إدخال مبالغ سالبة في السند أو الضريبة أو مصروف التحويل.', 16, 1); RETURN; END

    IF @isReceipt = 1 AND (@transferExpenseAmount > 0 OR @transferExpenseVatAmount > 0)
    BEGIN RAISERROR(N'مصروفات التحويل غير مدعومة في الحفظ الآمن لسند القبض.', 16, 1); RETURN; END

    IF @vatAmount > ISNULL(@amount, 0) AND ISNULL(@includeVat, 0) = 1
    BEGIN RAISERROR(N'قيمة الضريبة لا يمكن أن تكون أكبر من قيمة السند عند اختيار شامل الضريبة.', 16, 1); RETURN; END

    SET @mainDebitAmount = CASE WHEN @vatAmount > 0 AND ISNULL(@includeVat, 0) = 1 THEN @amount - @vatAmount ELSE @amount END;
    SET @totalPaymentAmount = CASE WHEN @vatAmount > 0 AND ISNULL(@includeVat, 0) = 0 THEN @amount + @vatAmount ELSE @amount END;
    SET @totalPaymentAmount = @totalPaymentAmount + @transferExpenseAmount + @transferExpenseVatAmount;

    IF @mainDebitAmount <= 0
    BEGIN RAISERROR(N'صافي قيمة السند بعد الضريبة يجب أن يكون أكبر من صفر.', 16, 1); RETURN; END

    IF @noteType = 5 AND NOT EXISTS
    (
        SELECT 1
        FROM dbo.sanad_numbering WITH (NOLOCK)
        WHERE branch_no = @branchId
          AND sanad_no = @voucherSanadNo
          AND Prefix = @voucherPrefix
    )
        SET @voucherPrefix = NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.ACCOUNTS WITH (READCOMMITTEDLOCK) WHERE Account_Code = @partyAccount AND ISNULL(last_account, 0) = 1 AND ISNULL(Block, 0) = 0)
    BEGIN RAISERROR(N'الحساب المختار غير موجود أو ليس حسابا نهائيا.', 16, 1); RETURN; END

    IF @paymentMethod = 0
        SELECT @creditAccount = NULLIF(Account_Code, N'') FROM dbo.TblBoxesData WITH (READCOMMITTEDLOCK) WHERE BoxID = @boxId;
    ELSE
        SELECT @creditAccount = NULLIF(Account_Code, N'') FROM dbo.BanksData WITH (READCOMMITTEDLOCK) WHERE BankID = @bankId;

    IF @creditAccount IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.ACCOUNTS WITH (READCOMMITTEDLOCK) WHERE Account_Code = @creditAccount AND ISNULL(last_account, 0) = 1 AND ISNULL(Block, 0) = 0)
    BEGIN RAISERROR(N'حساب الصندوق أو البنك غير موجود أو غير مفعل.', 16, 1); RETURN; END

    IF @vatAmount > 0
    BEGIN
        SELECT TOP (1) @vatAccount = NULLIF(AccDep, N'')
        FROM dbo.TblSettsReqLimK WITH (READCOMMITTEDLOCK)
        WHERE @noteDate BETWEEN RecordDate AND RecordDateTo
          AND (AccOrTran = 1 OR AccOrTran IS NULL)
          AND TransType = 23
        ORDER BY RecordDate DESC;

        IF @vatAccount IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.ACCOUNTS WITH (READCOMMITTEDLOCK) WHERE Account_Code = @vatAccount AND ISNULL(last_account, 0) = 1 AND ISNULL(Block, 0) = 0)
        BEGIN RAISERROR(N'حساب ضريبة القيمة المضافة غير مضبوط لهذا التاريخ. لا يمكن حفظ قيد غير صحيح.', 16, 1); RETURN; END
    END

    IF @transferExpenseAmount > 0
    BEGIN
        SET @transferExpenseAccount = NULLIF(LTRIM(RTRIM(@transferExpenseAccountCode)), N'');
        IF @transferExpenseAccount IS NULL
        BEGIN RAISERROR(N'يجب اختيار حساب مصروف التحويل عند إدخال مصروف تحويل.', 16, 1); RETURN; END
        IF NOT EXISTS (SELECT 1 FROM dbo.ACCOUNTS WITH (READCOMMITTEDLOCK) WHERE Account_Code = @transferExpenseAccount AND ISNULL(last_account, 0) = 1 AND ISNULL(Block, 0) = 0)
        BEGIN RAISERROR(N'حساب مصروف التحويل غير موجود أو غير مفعل.', 16, 1); RETURN; END
    END

    IF @transferExpenseVatAmount > 0
    BEGIN
        SET @transferExpenseVatAccount = NULLIF(LTRIM(RTRIM(@transferExpenseVatAccountCode)), N'');
        IF @transferExpenseVatAccount IS NULL
        BEGIN
            SELECT TOP (1) @transferExpenseVatAccount = NULLIF(AccDep, N'')
            FROM dbo.TblSettsReqLimK WITH (READCOMMITTEDLOCK)
            WHERE @noteDate BETWEEN RecordDate AND RecordDateTo
              AND (AccOrTran = 1 OR AccOrTran IS NULL)
              AND TransType = 23
            ORDER BY RecordDate DESC;
        END
        IF @transferExpenseVatAccount IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.ACCOUNTS WITH (READCOMMITTEDLOCK) WHERE Account_Code = @transferExpenseVatAccount AND ISNULL(last_account, 0) = 1 AND ISNULL(Block, 0) = 0)
        BEGIN RAISERROR(N'حساب ضريبة مصروف التحويل غير مضبوط لهذا التاريخ.', 16, 1); RETURN; END
    END

    IF @isReceipt = 1
    BEGIN
        INSERT INTO @JournalLines (AccountCode, NextAccountCode, Amount, CreditOrDebit, LineDescription)
        VALUES (@creditAccount, @partyAccount, @totalPaymentAmount, 0, @description);

        INSERT INTO @JournalLines (AccountCode, NextAccountCode, Amount, CreditOrDebit, LineDescription)
        VALUES (@partyAccount, @creditAccount, @mainDebitAmount, 1, @description);

        IF @vatAmount > 0
            INSERT INTO @JournalLines (AccountCode, NextAccountCode, Amount, CreditOrDebit, LineDescription, FlgVat, VatValue, TotalValue)
            VALUES (@vatAccount, @creditAccount, @vatAmount, 1, N'ضريبة القيمة المضافة - سند قبض', 1, @vatAmount, @vatAmount);
    END
    ELSE
    BEGIN
        INSERT INTO @JournalLines (AccountCode, NextAccountCode, Amount, CreditOrDebit, LineDescription)
        VALUES (@partyAccount, @creditAccount, @mainDebitAmount, 0, @description);

        IF @vatAmount > 0
            INSERT INTO @JournalLines (AccountCode, NextAccountCode, Amount, CreditOrDebit, LineDescription, FlgVat, VatValue, TotalValue)
            VALUES (@vatAccount, @creditAccount, @vatAmount, 0, N'ضريبة القيمة المضافة - سند صرف', 1, @vatAmount, @vatAmount);

        IF @transferExpenseAmount > 0
            INSERT INTO @JournalLines (AccountCode, NextAccountCode, Amount, CreditOrDebit, LineDescription)
            VALUES (@transferExpenseAccount, @creditAccount, @transferExpenseAmount, 0, N'مصروف تحويل بنكي - سند صرف');

        IF @transferExpenseVatAmount > 0
            INSERT INTO @JournalLines (AccountCode, NextAccountCode, Amount, CreditOrDebit, LineDescription, FlgVat, VatValue, TotalValue)
            VALUES (@transferExpenseVatAccount, @creditAccount, @transferExpenseVatAmount, 0, N'ضريبة مصروف التحويل البنكي - سند صرف', 1, @transferExpenseVatAmount, @transferExpenseVatAmount);

        INSERT INTO @JournalLines (AccountCode, NextAccountCode, Amount, CreditOrDebit, LineDescription)
        VALUES (@creditAccount, @partyAccount, @totalPaymentAmount, 1, @description);
    END

    IF EXISTS (SELECT 1 FROM @JournalLines WHERE Amount <= 0)
    BEGIN RAISERROR(N'لا يمكن إنشاء قيد محاسبي بقيمة صفرية أو سالبة.', 16, 1); RETURN; END

    IF ABS((SELECT ISNULL(SUM(CASE WHEN CreditOrDebit = 0 THEN Amount ELSE -Amount END), 0) FROM @JournalLines)) > 0.01
    BEGIN RAISERROR(N'القيد المحاسبي غير متوازن قبل الحفظ. تم إيقاف العملية.', 16, 1); RETURN; END

    SELECT TOP (1) @accountIntervalId = Account_Interval_ID
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
    WHERE Account_Interval_ID IS NOT NULL
    ORDER BY RecordDate DESC;
    SET @accountIntervalId = ISNULL(@accountIntervalId, 0);

    BEGIN TRANSACTION;

    IF @isEdit = 1
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteID = @noteId AND NoteType = @noteType)
        BEGIN RAISERROR(N'سند الصرف غير موجود.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
        IF EXISTS (SELECT 1 FROM dbo.Notes WHERE NoteID = @noteId AND ISNULL(NotePosted, 0) = 1)
        BEGIN RAISERROR(N'لا يمكن تعديل سند مرحل.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

        SELECT
            @noteSerial = CONVERT(varchar(50), CONVERT(decimal(38,0), NoteSerial)),
            @noteSerial1 = CONVERT(varchar(50), CONVERT(decimal(38,0), NoteSerial1))
        FROM dbo.Notes
        WHERE NoteID = @noteId;

        EXEC @rc = dbo.usp_DynamicErpVoucher_CleanupLinkedRows
            @noteId = @noteId,
            @noteType = @noteType,
            @noteSerial1 = @noteSerial1,
            @mode = N'REBUILD',
            @deleteNote = 0;

        IF @rc <> 0
        BEGIN
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END
    ELSE
    BEGIN
        DECLARE @nextNoteIdBig bigint;
        EXEC @rc = dbo.usp_DynamicErp_AllocateIntId
            @tableName = N'Notes',
            @fieldName = N'NoteID',
            @nextValue = @nextNoteIdBig OUTPUT;
        IF @rc <> 0 OR @nextNoteIdBig IS NULL
        BEGIN RAISERROR(N'تعذر حجز رقم داخلي آمن لسند الصرف.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

        SET @noteId = CONVERT(int, @nextNoteIdBig);

        EXEC @rc = dbo.usp_DynamicErpVoucher_NextSerial
            @serialKind = 0,
            @branchId = @branchId,
            @noteDate = @noteDate,
            @sanadNo = 0,
            @noteType = 200,
            @prefix = NULL,
            @userId = @userId,
            @result = @noteSerial OUTPUT;
        IF @rc <> 0 OR @noteSerial = 'error'
        BEGIN RAISERROR(N'لا يمكن توليد رقم القيد حسب إعدادات VB6.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
        IF NULLIF(@noteSerial, '') IS NULL
        BEGIN RAISERROR(N'إعداد ترقيم القيود يدوي، ولا يمكن الحفظ الآلي بدون رقم.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

        EXEC @rc = dbo.usp_DynamicErpVoucher_NextSerial
            @serialKind = 1,
            @branchId = @branchId,
            @noteDate = @noteDate,
            @sanadNo = @voucherSanadNo,
            @noteType = @noteType,
            @prefix = @voucherPrefix,
            @userId = @userId,
            @result = @noteSerial1 OUTPUT;
        IF @rc <> 0 OR @noteSerial1 = 'error'
        BEGIN RAISERROR(N'لا يمكن توليد رقم سند الصرف حسب إعدادات VB6.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
        IF NULLIF(@noteSerial1, '') IS NULL
        BEGIN RAISERROR(N'إعداد ترقيم سند الصرف يدوي، ولا يمكن الحفظ الآلي بدون رقم.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
    END

    SET @noteSerialNumeric = CONVERT(float, @noteSerial);
    SET @noteSerial1Numeric = CONVERT(float, @noteSerial1);

    IF @isEdit = 1
    BEGIN
        UPDATE dbo.Notes
        SET NoteDate = @noteDate,
            Note_Value = CONVERT(float, @amount),
            Note_Value2 = CONVERT(float, @totalPaymentAmount),
            Note_ValueE = CONVERT(float, @totalPaymentAmount),
            Rate = 1,
            BankID = CASE WHEN @paymentMethod = 2 THEN @bankId ELSE NULL END,
            BoxID = CASE WHEN @paymentMethod = 0 THEN @boxId ELSE NULL END,
            ChqueNum = CASE WHEN @paymentMethod = 2 THEN @chequeNumber ELSE NULL END,
            DueDate = CASE WHEN @paymentMethod = 2 THEN COALESCE(@chequeDueDate, @noteDate) ELSE NULL END,
            UserID = @userId,
            Remark = @remark,
            CashingType = @cashingType,
            NoteCashingType = @paymentMethod,
            branch_no = @branchId,
            user_name = CONVERT(nvarchar(50), @userId),
            person = @partyDisplay,
            too = @partyDisplay,
            ORDER_NO = @orderNo,
            PaymentType = @paymentMethod,
            TxtChequeNumber1 = CASE WHEN @paymentMethod = 2 THEN @chequeNumber ELSE NULL END,
            DtpChequeDueDate1 = CASE WHEN @paymentMethod = 2 THEN COALESCE(@chequeDueDate, @noteDate) ELSE NULL END,
            ManualNo = @manualNo,
            NCashingType = @receiptClass,
            Prefix = @voucherPrefix,
            PayDes = @payDes,
            PayDes1 = @payDes1,
            VAT = CONVERT(float, @vatAmount),
            TotalValue = CONVERT(float, @totalPaymentAmount),
            TotalNotesValue = CONVERT(float, @totalPaymentAmount),
            IncludVAT = CASE WHEN ISNULL(@includeVat, 0) <> 0 THEN 1 ELSE 0 END,
            PreVAT = CASE WHEN @vatAmount > 0 THEN CONVERT(float, @mainDebitAmount) ELSE NULL END,
            AccountPaym = @partyAccount,
            Account_DebitSide = CASE WHEN @isReceipt = 1 THEN @creditAccount ELSE @partyAccount END,
            Account_CreditSide = CASE WHEN @isReceipt = 1 THEN @partyAccount ELSE @creditAccount END
        WHERE NoteID = @noteId AND NoteType = @noteType;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Notes
        (
            NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, OldNoteSerial1,
            Note_Value, Note_Value2, Note_ValueE, Rate,
            BankID, BoxID, ChqueNum, DueDate, UserID, Remark, CashingType, NoteCashingType, NotePosted,
            numbering_type, numbering_type1, sanad_year, sanad_month, branch_no, user_name,
            person, too, ORDER_NO, PaymentType, TxtChequeNumber1, DtpChequeDueDate1,
            ManualNo, NCashingType, Prefix, PayDes, PayDes1, VAT, TotalValue, TotalNotesValue, IncludVAT,
            PreVAT, AccountPaym, Account_DebitSide, Account_CreditSide
        )
        VALUES
        (
            @noteId, @noteDate, @noteType, @noteSerialNumeric, @noteSerial1Numeric, CONVERT(nvarchar(255), @noteSerial1),
            CONVERT(float, @amount), CONVERT(float, @totalPaymentAmount), CONVERT(float, @totalPaymentAmount), 1,
            CASE WHEN @paymentMethod = 2 THEN @bankId ELSE NULL END,
            CASE WHEN @paymentMethod = 0 THEN @boxId ELSE NULL END,
            CASE WHEN @paymentMethod = 2 THEN @chequeNumber ELSE NULL END,
            CASE WHEN @paymentMethod = 2 THEN COALESCE(@chequeDueDate, @noteDate) ELSE NULL END,
            @userId, @remark, @cashingType, @paymentMethod, 0,
            ISNULL((SELECT TOP (1) numbering_id FROM dbo.sanad_numbering WHERE branch_no = @branchId AND sanad_no = 0), 0),
            ISNULL((SELECT TOP (1) numbering_id FROM dbo.sanad_numbering WHERE branch_no = @branchId AND sanad_no = @voucherSanadNo AND (Prefix = @voucherPrefix OR (Prefix IS NULL AND @voucherPrefix IS NULL))), 0),
            YEAR(@noteDate), MONTH(@noteDate), @branchId, CONVERT(nvarchar(50), @userId),
            @partyDisplay,
            @partyDisplay,
            @orderNo,
            @paymentMethod,
            CASE WHEN @paymentMethod = 2 THEN @chequeNumber ELSE NULL END,
            CASE WHEN @paymentMethod = 2 THEN COALESCE(@chequeDueDate, @noteDate) ELSE NULL END,
            @manualNo, @receiptClass, @voucherPrefix, @payDes, @payDes1, CONVERT(float, @vatAmount), CONVERT(float, @totalPaymentAmount), CONVERT(float, @totalPaymentAmount), CASE WHEN ISNULL(@includeVat, 0) <> 0 THEN 1 ELSE 0 END,
            CASE WHEN @vatAmount > 0 THEN CONVERT(float, @mainDebitAmount) ELSE NULL END,
            @partyAccount,
            CASE WHEN @isReceipt = 1 THEN @creditAccount ELSE @partyAccount END,
            CASE WHEN @isReceipt = 1 THEN @partyAccount ELSE @creditAccount END
        );
    END

    DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId;
    DELETE FROM dbo.TblChecqueBoxContent1 WHERE NoteID = @noteId;
    DELETE FROM dbo.TblChecqueBoxContent WHERE NoteID = @noteId;

    DECLARE @allocLineNo int;
    DECLARE @maxLineNo int;

    SELECT @allocLineNo = MIN([LineNo]), @maxLineNo = MAX([LineNo])
    FROM @JournalLines;

    WHILE @allocLineNo IS NOT NULL AND @allocLineNo <= @maxLineNo
    BEGIN
        IF EXISTS (SELECT 1 FROM @JournalLines WHERE [LineNo] = @allocLineNo)
        BEGIN
            EXEC @rc = dbo.usp_DynamicErp_AllocateIntId
                @tableName = N'DOUBLE_ENTREY_VOUCHERS',
                @fieldName = N'Double_Entry_Vouchers_ID',
                @nextValue = @nextDevIdBig OUTPUT;
            IF @rc <> 0 OR @nextDevIdBig IS NULL
            BEGIN RAISERROR(N'تعذر حجز رقم داخلي آمن للقيد المحاسبي.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

            EXEC @rc = dbo.usp_DynamicErp_AllocateIntId
                @tableName = N'DOUBLE_ENTREY_VOUCHERS',
                @fieldName = N'DEV_ID_Line_No1',
                @nextValue = @nextLineNo1Big OUTPUT;
            IF @rc <> 0 OR @nextLineNo1Big IS NULL
            BEGIN RAISERROR(N'تعذر حجز رقم آمن لبند القيد المحاسبي.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

            INSERT INTO @JournalAllocations ([LineNo], DoubleEntryVoucherId, LineNo1)
            VALUES (@allocLineNo, CONVERT(int, @nextDevIdBig), CONVERT(float, @nextLineNo1Big));
        END

        SET @allocLineNo = @allocLineNo + 1;
    END

    EXEC @rc = sys.sp_getapplock
        @Resource = N'DynamicErpVoucherJournalLines',
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 15000;

    IF @rc < 0
    BEGIN
        RAISERROR(N'تعذر حجز أرقام القيد المحاسبي. حاول مرة أخرى.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    SELECT @nextDevIdBig = MIN(DoubleEntryVoucherId)
    FROM @JournalAllocations;

    SELECT @nextLineNo1Big = MIN(CONVERT(bigint, LineNo1))
    FROM @JournalAllocations;

    INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
    (
        Double_Entry_Vouchers_ID, DEV_ID_Line_No, DEV_ID_Line_No1,
        Account_Code, NextAccount_Code, Value, valuee, rate,
        Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
        Notes_ID, UserID, Posted, Account_Interval_ID, branch_id,
        depet_value, credit_value, FlgVat, Vat, TotalValue
    )
    SELECT
        a.DoubleEntryVoucherId,
        j.[LineNo],
        a.LineNo1,
        j.AccountCode,
        j.NextAccountCode,
        CONVERT(money, j.Amount),
        CONVERT(money, j.Amount),
        1,
        j.CreditOrDebit,
        j.LineDescription,
        @noteDate,
        @noteId,
        @userId,
        0,
        @accountIntervalId,
        @branchId,
        CASE WHEN j.CreditOrDebit = 0 THEN CONVERT(money, j.Amount) ELSE NULL END,
        CASE WHEN j.CreditOrDebit = 1 THEN CONVERT(money, j.Amount) ELSE NULL END,
        j.FlgVat,
        CONVERT(float, j.VatValue),
        CONVERT(float, j.TotalValue)
    FROM @JournalLines j
    INNER JOIN @JournalAllocations a
        ON a.[LineNo] = j.[LineNo]
    ORDER BY j.[LineNo];

    SELECT @lastVoucherId = MAX(DoubleEntryVoucherId)
    FROM @JournalAllocations;

    UPDATE dbo.Notes
    SET Double_Entry_Vouchers_ID = @lastVoucherId
    WHERE NoteID = @noteId AND NoteType = @noteType;

    IF EXISTS
    (
        SELECT DEV_ID_Line_No
        FROM dbo.DOUBLE_ENTREY_VOUCHERS
        WHERE Notes_ID = @noteId
        GROUP BY DEV_ID_Line_No
        HAVING COUNT(*) > 1
    )
    BEGIN
        RAISERROR(N'تم اكتشاف أرقام بنود مكررة داخل القيد. تم إلغاء الحفظ.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.DOUBLE_ENTREY_VOUCHERS
        WHERE Notes_ID = @noteId
        GROUP BY Notes_ID
        HAVING ABS(SUM(CASE WHEN Credit_Or_Debit = 0 THEN CONVERT(decimal(18,2), Value) ELSE -CONVERT(decimal(18,2), Value) END)) > 0.01
    )
    BEGIN
        RAISERROR(N'القيد المحاسبي غير متوازن، تم إلغاء الحفظ.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    COMMIT TRANSACTION;

    SELECT @noteId AS NoteID,
           CONVERT(nvarchar(50), @noteSerial1) AS NoteSerial,
           @lastVoucherId AS Double_Entry_Vouchers_ID,
           CAST(CASE
                    WHEN @isEdit = 1 AND @noteType = 4 THEN N'تم تعديل سند القبض والقيد المحاسبي.'
                    WHEN @isEdit = 0 AND @noteType = 4 THEN N'تم إنشاء سند القبض والقيد المحاسبي.'
                    WHEN @isEdit = 1 THEN N'تم تعديل سند الصرف والقيد المحاسبي.'
                    ELSE N'تم إنشاء سند الصرف والقيد المحاسبي.'
                END AS nvarchar(300)) AS ResultMessage;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_Delete', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_Delete;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_Delete
    @noteType int,
    @noteId int,
    @userId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @noteType NOT IN (4, 5)
    BEGIN
        RAISERROR(N'نوع السند غير مدعوم في الحذف الآمن.', 16, 1);
        RETURN;
    END

    DECLARE @noteSerial1 nvarchar(50), @rc int;

    BEGIN TRANSACTION;

    SELECT @noteSerial1 = CONVERT(nvarchar(50), CONVERT(decimal(38,0), NoteSerial1))
    FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK)
    WHERE NoteID = @noteId AND NoteType = @noteType;

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'سند الصرف غير موجود.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    EXEC @rc = dbo.usp_DynamicErpVoucher_CleanupLinkedRows
        @noteId = @noteId,
        @noteType = @noteType,
        @noteSerial1 = @noteSerial1,
        @mode = N'DELETE',
        @deleteNote = 1;

    IF @rc <> 0
    BEGIN
        ROLLBACK TRANSACTION;
        RETURN;
    END

    COMMIT TRANSACTION;

    SELECT CAST(CASE WHEN @noteType = 4 THEN N'تم حذف سند القبض وتنظيف كل الجداول المرتبطة به.' ELSE N'تم حذف سند الصرف وتنظيف كل الجداول المرتبطة به.' END AS nvarchar(300)) AS ResultMessage;
END
GO


