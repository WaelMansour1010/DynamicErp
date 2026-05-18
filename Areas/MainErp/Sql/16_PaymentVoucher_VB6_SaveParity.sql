/*
    16_PaymentVoucher_VB6_SaveParity.sql

    Payment Voucher / سند الصرف save safety hardening.

    VB6 source of truth:
      F:\Source Code\SatriahMain\Frm\FrmPayments.frm
      - SaveData
      - Del_Trans
      - saveChequeBoxContents1
      - saveBillBuy / saveBillProject / saveBillVendor

    Numbering source of truth:
      F:\Source Code\SatriahMain\Bas\registry.bas
      - Notes_coding(my_branch, date1)
      - Voucher_coding(my_branch, date1, 4, 5, ...)

    SQL Server compatibility: SQL Server 2012+
*/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.SerialCounters_V2', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SerialCounters_V2
    (
        CounterID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SerialCounters_V2 PRIMARY KEY,
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
        EndAt int NULL,
        LastUpdated datetime NOT NULL CONSTRAINT DF_SerialCounters_V2_LastUpdated DEFAULT(GETDATE()),
        UpdatedByUser varchar(50) NULL,
        UpdateCount bigint NOT NULL CONSTRAINT DF_SerialCounters_V2_UpdateCount DEFAULT(1)
    );

    CREATE UNIQUE INDEX UQ_SerialCounters_V2
        ON dbo.SerialCounters_V2(SourceTable, BranchID, TypeCode, Prefix, StoreID, YearNum, MonthNum);
END
GO

IF OBJECT_ID(N'dbo.SerialTableMapping', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SerialTableMapping
    (
        MappingID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SerialTableMapping PRIMARY KEY,
        SourceTable varchar(50) NOT NULL,
        BranchField varchar(50) NOT NULL,
        DateField varchar(50) NOT NULL,
        SerialField varchar(50) NOT NULL,
        SerialDataType varchar(20) NOT NULL,
        TypeField varchar(50) NULL,
        DefaultTypeCode int NOT NULL CONSTRAINT DF_SerialTableMapping_DefaultTypeCode DEFAULT(0),
        SanadNo int NOT NULL,
        Description nvarchar(200) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SerialTableMapping WHERE SourceTable = 'Notes')
BEGIN
    INSERT INTO dbo.SerialTableMapping
        (SourceTable, BranchField, DateField, SerialField, SerialDataType, TypeField, DefaultTypeCode, SanadNo, Description)
    VALUES
        ('Notes', 'branch_no', 'NoteDate', 'NoteSerial1', 'FLOAT', 'NoteType', 0, 0, N'جدول القيود');
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_SeedSerialCounters', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_SeedSerialCounters;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_SeedSerialCounters
    @userId int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @kedDigits int,
        @branchDigits int,
        @jlCodeBasedOnBranch bit,
        @updatedBy varchar(50);

    SELECT TOP (1)
        @kedDigits = ISNULL(Ked_digit, 3),
        @branchDigits = CONVERT(int, ISNULL(BranchDigit, 1)),
        @jlCodeBasedOnBranch = ISNULL(JLCodeBasedOnBranch, 0)
    FROM dbo.TblOptions WITH (NOLOCK);

    SET @kedDigits = ISNULL(NULLIF(@kedDigits, 0), 3);
    SET @branchDigits = ISNULL(NULLIF(@branchDigits, 0), 1);
    SET @jlCodeBasedOnBranch = ISNULL(@jlCodeBasedOnBranch, 0);
    SET @updatedBy = LEFT(CONVERT(varchar(20), ISNULL(@userId, 0)) + ';FinanceSeed', 50);

    /*
        Runtime serial generation must not discover the next value from Notes.
        This seed step snapshots existing data into SerialCounters_V2 once, then
        dbo.usp_DynamicErpVoucher_NextSerial allocates only from the counter row.

        Scope rules:
        - NoteSerial / Notes_coding: sanad_no=0, NoteType<>1, global unless
          TblOptions.JLCodeBasedOnBranch=1.
        - NoteSerial1 / Voucher_coding for payment voucher: sanad_no=4,
          NoteType=5, branch + prefix + period.
        - numbering_id 1 is continuous, 2 is monthly, 3 is yearly.
    */

    ;WITH NoteSerialConfiguredScopes AS
    (
        SELECT DISTINCT
            SourceTable = 'Notes.NoteSerial',
            BranchID = CASE WHEN @jlCodeBasedOnBranch = 1 THEN sn.branch_no ELSE 0 END,
            TypeCode = 200,
            Prefix = CAST(NULL AS varchar(10)),
            StoreID = CAST(NULL AS int),
            NumberingType = CONVERT(tinyint, ISNULL(sn.numbering_id, 0)),
            NoOfDigits = CONVERT(tinyint, @kedDigits),
            YearDigits = CONVERT(tinyint, 4),
            StartAt = CASE WHEN ISNULL(sn.start_at, 0) <= 0 THEN 1 ELSE CONVERT(int, sn.start_at) END,
            EndAt = NULLIF(CONVERT(int, ISNULL(sn.end_at, 0)), 0),
            ConfigBranchID = sn.branch_no
        FROM dbo.sanad_numbering sn WITH (NOLOCK)
        WHERE sn.sanad_no = 0
          AND ISNULL(sn.numbering_id, 0) IN (1, 2, 3)
    ),
    NoteSerialExisting AS
    (
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
                    WHEN s.NumberingType = 1 AND @jlCodeBasedOnBranch = 1
                        THEN CONVERT(bigint, RIGHT(CONVERT(varchar(50), CONVERT(decimal(38,0), n.NoteSerial)), LEN(CONVERT(varchar(50), CONVERT(decimal(38,0), n.NoteSerial))) - @branchDigits))
                    WHEN s.NumberingType = 1
                        THEN CONVERT(bigint, CONVERT(decimal(38,0), n.NoteSerial))
                    ELSE CONVERT(bigint, RIGHT(CONVERT(varchar(50), CONVERT(decimal(38,0), n.NoteSerial)), s.NoOfDigits))
                END
            ),
            s.NoOfDigits,
            s.YearDigits,
            s.StartAt,
            s.EndAt
        FROM NoteSerialConfiguredScopes s
        INNER JOIN dbo.Notes n WITH (NOLOCK)
            ON n.NoteSerial IS NOT NULL
           AND n.NoteType <> 1
           AND (@jlCodeBasedOnBranch = 0 OR n.branch_no = s.ConfigBranchID)
           AND (s.NumberingType <> 2 OR (YEAR(n.NoteDate) = YEAR(n.NoteDate) AND MONTH(n.NoteDate) = MONTH(n.NoteDate)))
        WHERE ISNUMERIC(n.NoteSerial) = 1
        GROUP BY s.SourceTable, s.BranchID, s.TypeCode, s.Prefix, s.StoreID, s.NumberingType,
                 CASE WHEN s.NumberingType IN (2, 3) THEN YEAR(n.NoteDate) ELSE 0 END,
                 CASE WHEN s.NumberingType = 2 THEN MONTH(n.NoteDate) ELSE 0 END,
                 s.NoOfDigits, s.YearDigits, s.StartAt, s.EndAt
    )
    INSERT INTO dbo.SerialCounters_V2
        (SourceTable, BranchID, TypeCode, Prefix, StoreID, NumberingType, YearNum, MonthNum, CurrentTail, NoOfDigits, YearDigits, StartAt, EndAt, UpdatedByUser, UpdateCount)
    SELECT SourceTable, BranchID, TypeCode, Prefix, StoreID, NumberingType, YearNum, MonthNum, CurrentTail, NoOfDigits, YearDigits, StartAt, EndAt, @updatedBy, 1
    FROM NoteSerialExisting e
    WHERE e.CurrentTail IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.SerialCounters_V2 c WITH (UPDLOCK, HOLDLOCK)
          WHERE c.SourceTable = e.SourceTable
            AND c.BranchID = e.BranchID
            AND c.TypeCode = e.TypeCode
            AND (c.Prefix = e.Prefix OR (c.Prefix IS NULL AND e.Prefix IS NULL))
            AND (c.StoreID = e.StoreID OR (c.StoreID IS NULL AND e.StoreID IS NULL))
            AND c.YearNum = e.YearNum
            AND c.MonthNum = e.MonthNum
      );

    ;WITH VoucherConfiguredScopes AS
    (
        SELECT
            SourceTable = 'Notes.NoteSerial1',
            BranchID = sn.branch_no,
            TypeCode = 5,
            Prefix = CONVERT(varchar(10), sn.Prefix),
            StoreID = CAST(NULL AS int),
            NumberingType = CONVERT(tinyint, ISNULL(sn.numbering_id, 0)),
            NoOfDigits = CONVERT(tinyint, CASE WHEN ISNULL(sn.no_of_digit, 0) <= 0 THEN 3 ELSE sn.no_of_digit END),
            YearDigits = CONVERT(tinyint, CASE WHEN ISNULL(sn.YearDigit, 0) <= 0 THEN 4 ELSE sn.YearDigit END),
            StartAt = CASE WHEN ISNULL(sn.start_at, 0) <= 0 THEN 1 ELSE CONVERT(int, sn.start_at) END,
            EndAt = NULLIF(CONVERT(int, ISNULL(sn.end_at, 0)), 0)
        FROM dbo.sanad_numbering sn WITH (NOLOCK)
        WHERE sn.sanad_no = 4
          AND ISNULL(sn.numbering_id, 0) IN (1, 2, 3)
    ),
    VoucherExisting AS
    (
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
        FROM VoucherConfiguredScopes s
        INNER JOIN dbo.Notes n WITH (NOLOCK)
            ON n.NoteSerial1 IS NOT NULL
           AND n.NoteType = 5
           AND n.branch_no = s.BranchID
           AND (n.Prefix = s.Prefix OR (n.Prefix IS NULL AND s.Prefix IS NULL))
        WHERE ISNUMERIC(n.NoteSerial1) = 1
        GROUP BY s.SourceTable, s.BranchID, s.TypeCode, s.Prefix, s.StoreID, s.NumberingType,
                 CASE WHEN s.NumberingType IN (2, 3) THEN YEAR(n.NoteDate) ELSE 0 END,
                 CASE WHEN s.NumberingType = 2 THEN MONTH(n.NoteDate) ELSE 0 END,
                 s.NoOfDigits, s.YearDigits, s.StartAt, s.EndAt
    )
    INSERT INTO dbo.SerialCounters_V2
        (SourceTable, BranchID, TypeCode, Prefix, StoreID, NumberingType, YearNum, MonthNum, CurrentTail, NoOfDigits, YearDigits, StartAt, EndAt, UpdatedByUser, UpdateCount)
    SELECT SourceTable, BranchID, TypeCode, Prefix, StoreID, NumberingType, YearNum, MonthNum, CurrentTail, NoOfDigits, YearDigits, StartAt, EndAt, @updatedBy, 1
    FROM VoucherExisting e
    WHERE e.CurrentTail IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.SerialCounters_V2 c WITH (UPDLOCK, HOLDLOCK)
          WHERE c.SourceTable = e.SourceTable
            AND c.BranchID = e.BranchID
            AND c.TypeCode = e.TypeCode
            AND (c.Prefix = e.Prefix OR (c.Prefix IS NULL AND e.Prefix IS NULL))
            AND (c.StoreID = e.StoreID OR (c.StoreID IS NULL AND e.StoreID IS NULL))
            AND c.YearNum = e.YearNum
            AND c.MonthNum = e.MonthNum
      );
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_NextSerial', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_NextSerial;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_NextSerial
    @serialKind int, -- 0 = Notes_coding/NoteSerial, 1 = Voucher_coding/NoteSerial1
    @branchId int,
    @noteDate date,
    @sanadNo int,
    @noteType int,
    @prefix varchar(10) = NULL,
    @userId int = NULL,
    @result varchar(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @numberingType int,
        @startAt int,
        @endAt int,
        @noOfDigits int,
        @yearDigits int,
        @kedDigits int,
        @branchDigits int,
        @jlCodeBasedOnBranch bit,
        @sourceTable varchar(50),
        @typeCode int,
        @counterBranchId int,
        @yearNum int,
        @monthNum int,
        @newTail bigint,
        @rowsAffected int,
        @branchCode varchar(10),
        @tailText varchar(30),
        @yearText varchar(4),
        @monthText varchar(2),
        @candidate varchar(50),
        @attempt int,
        @lockResult int,
        @lockResource nvarchar(255),
        @hasExistingData bit;

    IF @@TRANCOUNT = 0
    BEGIN
        SET @result = 'error';
        RETURN -1;
    END

    IF @branchId IS NULL OR @branchId = 0
    BEGIN
        SET @result = 'error';
        RETURN -1;
    END

    SELECT TOP (1)
        @kedDigits = ISNULL(Ked_digit, 3),
        @branchDigits = CONVERT(int, ISNULL(BranchDigit, 1)),
        @jlCodeBasedOnBranch = ISNULL(JLCodeBasedOnBranch, 0)
    FROM dbo.TblOptions WITH (NOLOCK);

    SET @kedDigits = ISNULL(NULLIF(@kedDigits, 0), 3);
    SET @branchDigits = ISNULL(NULLIF(@branchDigits, 0), 1);
    SET @jlCodeBasedOnBranch = ISNULL(@jlCodeBasedOnBranch, 0);

    SELECT TOP (1)
        @numberingType = ISNULL(numbering_id, 0),
        @startAt = ISNULL(start_at, 0),
        @endAt = NULLIF(end_at, 0),
        @noOfDigits = ISNULL(NULLIF(no_of_digit, 0), 3),
        @yearDigits = ISNULL(NULLIF(YearDigit, 0), 4)
    FROM dbo.sanad_numbering WITH (NOLOCK)
    WHERE branch_no = @branchId
      AND sanad_no = @sanadNo
      AND (Prefix = @prefix OR (Prefix IS NULL AND @prefix IS NULL));

    SET @numberingType = ISNULL(@numberingType, 0);
    IF @numberingType = 0
    BEGIN
        SET @result = '';
        RETURN 0;
    END

    SET @startAt = CASE WHEN ISNULL(@startAt, 0) <= 0 THEN 1 ELSE @startAt END;
    SET @noOfDigits = CASE WHEN ISNULL(@noOfDigits, 0) <= 0 THEN 3 ELSE @noOfDigits END;
    SET @yearDigits = CASE WHEN ISNULL(@yearDigits, 0) <= 0 THEN 4 ELSE @yearDigits END;

    IF @serialKind = 0
    BEGIN
        SET @sourceTable = 'Notes.NoteSerial';
        SET @typeCode = 200;
        SET @counterBranchId = CASE WHEN @jlCodeBasedOnBranch = 1 THEN @branchId ELSE 0 END;
        SET @noOfDigits = @kedDigits;
        SET @yearDigits = 4;
    END
    ELSE
    BEGIN
        SET @sourceTable = 'Notes.NoteSerial1';
        SET @typeCode = @noteType;
        SET @counterBranchId = @branchId;
    END

    SET @yearNum = CASE WHEN @numberingType IN (2, 3) THEN YEAR(@noteDate) ELSE 0 END;
    SET @monthNum = CASE WHEN @numberingType = 2 THEN MONTH(@noteDate) ELSE 0 END;
    SET @attempt = 0;
    SET @lockResource = N'DynamicErpVoucherSerial:' + CONVERT(nvarchar(50), @sourceTable) + N':' + CONVERT(nvarchar(20), @counterBranchId) + N':' + CONVERT(nvarchar(20), @typeCode) + N':' + ISNULL(CONVERT(nvarchar(20), @prefix), N'<NULL>') + N':' + CONVERT(nvarchar(20), @yearNum) + N':' + CONVERT(nvarchar(20), @monthNum);

    EXEC @lockResult = sys.sp_getapplock
        @Resource = @lockResource,
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 15000;

    IF @lockResult < 0
    BEGIN
        SET @result = 'error';
        RETURN -1;
    END

    WHILE @attempt < 10
    BEGIN
        SET @attempt = @attempt + 1;
        SET @newTail = NULL;

        UPDATE dbo.SerialCounters_V2 WITH (UPDLOCK, HOLDLOCK)
        SET CurrentTail = CurrentTail + 1,
            @newTail = CurrentTail + 1,
            LastUpdated = GETDATE(),
            UpdatedByUser = LEFT(CONVERT(varchar(20), ISNULL(@userId, 0)) + ';FinanceVoucher', 50),
            UpdateCount = UpdateCount + 1
        WHERE SourceTable = @sourceTable
          AND BranchID = @counterBranchId
          AND TypeCode = @typeCode
          AND (Prefix = @prefix OR (Prefix IS NULL AND @prefix IS NULL))
          AND StoreID IS NULL
          AND YearNum = @yearNum
          AND MonthNum = @monthNum;

        SET @rowsAffected = @@ROWCOUNT;

        IF @rowsAffected = 0
        BEGIN
            SET @hasExistingData = 0;

            IF @serialKind = 0
            BEGIN
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK)
                    WHERE NoteSerial IS NOT NULL
                      AND NoteType <> 1
                      AND (@numberingType <> 2 OR (NoteDate >= DATEADD(month, DATEDIFF(month, 0, @noteDate), 0) AND NoteDate < DATEADD(month, 1, DATEADD(month, DATEDIFF(month, 0, @noteDate), 0))))
                      AND (@numberingType <> 3 OR (NoteDate >= DATEADD(year, DATEDIFF(year, 0, @noteDate), 0) AND NoteDate < DATEADD(year, 1, DATEADD(year, DATEDIFF(year, 0, @noteDate), 0))))
                      AND (@jlCodeBasedOnBranch = 0 OR branch_no = @branchId)
                )
                    SET @hasExistingData = 1;
            END
            ELSE
            BEGIN
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK)
                    WHERE NoteSerial1 IS NOT NULL
                      AND NoteType = @noteType
                      AND branch_no = @branchId
                      AND (@numberingType <> 2 OR (NoteDate >= DATEADD(month, DATEDIFF(month, 0, @noteDate), 0) AND NoteDate < DATEADD(month, 1, DATEADD(month, DATEDIFF(month, 0, @noteDate), 0))))
                      AND (@numberingType <> 3 OR (NoteDate >= DATEADD(year, DATEDIFF(year, 0, @noteDate), 0) AND NoteDate < DATEADD(year, 1, DATEADD(year, DATEDIFF(year, 0, @noteDate), 0))))
                      AND (Prefix = @prefix OR (Prefix IS NULL AND @prefix IS NULL))
                )
                    SET @hasExistingData = 1;
            END

            IF @hasExistingData = 1
            BEGIN
                SET @result = 'error';
                RETURN -1;
            END

            BEGIN TRY
                INSERT INTO dbo.SerialCounters_V2
                    (SourceTable, BranchID, TypeCode, Prefix, StoreID, NumberingType, YearNum, MonthNum, CurrentTail, NoOfDigits, YearDigits, StartAt, EndAt, UpdatedByUser, UpdateCount)
                VALUES
                    (@sourceTable, @counterBranchId, @typeCode, @prefix, NULL, @numberingType, @yearNum, @monthNum, @startAt - 1, @noOfDigits, @yearDigits, @startAt, @endAt, LEFT(CONVERT(varchar(20), ISNULL(@userId, 0)) + ';FinanceVoucher', 50), 0);
            END TRY
            BEGIN CATCH
                IF ERROR_NUMBER() NOT IN (2601, 2627)
                    RETURN -1;
            END CATCH

            CONTINUE;
        END

        IF @endAt IS NOT NULL AND @newTail > @endAt
        BEGIN
            SET @result = 'error';
            RETURN -1;
        END

        SET @branchCode = RIGHT(REPLICATE('0', @branchDigits) + CONVERT(varchar(10), @branchId), CASE WHEN LEN(CONVERT(varchar(10), @branchId)) > @branchDigits THEN LEN(CONVERT(varchar(10), @branchId)) ELSE @branchDigits END);
        SET @tailText = RIGHT(REPLICATE('0', @noOfDigits) + CONVERT(varchar(30), @newTail), CASE WHEN LEN(CONVERT(varchar(30), @newTail)) > @noOfDigits THEN LEN(CONVERT(varchar(30), @newTail)) ELSE @noOfDigits END);
        SET @yearText = CASE WHEN @yearDigits = 2 THEN RIGHT(CONVERT(varchar(4), YEAR(@noteDate)), 2) ELSE CONVERT(varchar(4), YEAR(@noteDate)) END;
        SET @monthText = RIGHT('0' + CONVERT(varchar(2), MONTH(@noteDate)), 2);

        IF @numberingType = 1
            SET @candidate = CASE WHEN @serialKind = 0 AND @jlCodeBasedOnBranch = 1 THEN @branchCode ELSE '' END + CONVERT(varchar(30), @newTail);
        ELSE IF @numberingType = 2
            SET @candidate = CASE WHEN @serialKind = 1 OR @jlCodeBasedOnBranch = 1 THEN @branchCode ELSE '' END + @yearText + @monthText + @tailText;
        ELSE
            SET @candidate = CASE WHEN @serialKind = 1 OR @jlCodeBasedOnBranch = 1 THEN @branchCode ELSE '' END + @yearText + @tailText;

        IF @serialKind = 0
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteSerial = CONVERT(float, @candidate) AND NoteType <> 1)
            BEGIN
                SET @result = @candidate;
                RETURN 0;
            END
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteSerial1 = CONVERT(float, @candidate) AND NoteType = @noteType AND branch_no = @branchId AND (Prefix = @prefix OR (Prefix IS NULL AND @prefix IS NULL)))
            BEGIN
                SET @result = @candidate;
                RETURN 0;
            END
        END
    END

    SET @result = 'error';
    RETURN -1;
END
GO

EXEC dbo.usp_DynamicErpVoucher_SeedSerialCounters @userId = 0;
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
    @userId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @noteType <> 5 BEGIN RAISERROR(N'حفظ سند القبض من هذه الشاشة مؤجل حتى يتم إكمال مطابقته مع VB6.', 16, 1); RETURN; END
    IF ISNULL(@amount, 0) <= 0 BEGIN RAISERROR(N'يجب إدخال قيمة سند صحيحة.', 16, 1); RETURN; END
    IF ISNULL(@vat, 0) <> 0 OR ISNULL(@includeVat, 0) <> 0 BEGIN RAISERROR(N'حفظ ضريبة القيمة المضافة يحتاج ربط شاشة المصروفات/الحوالة كما في VB6، وتم إيقافه مؤقتا لمنع قيد غير صحيح.', 16, 1); RETURN; END
    IF NULLIF(LTRIM(RTRIM(ISNULL(@partyAccountCode, N''))), N'') IS NULL BEGIN RAISERROR(N'يجب اختيار الحساب.', 16, 1); RETURN; END
    IF @branchId IS NULL OR @branchId = 0 BEGIN RAISERROR(N'يجب اختيار الفرع.', 16, 1); RETURN; END
    IF ISNULL(@paymentMethod, -1) NOT IN (0, 2) BEGIN RAISERROR(N'طريقة الدفع المختارة غير مدعومة في الحفظ الآمن الحالي. المدعوم حاليا: نقدي أو تحويل بنكي بدون مصروفات.', 16, 1); RETURN; END
    IF ISNULL(@cashingType, -1) NOT IN (0, 1, 2, 5) BEGIN RAISERROR(N'نوع سند الصرف المختار يحتاج منطق ربط خاص من VB6 ولم يتم تفعيله بعد في الحفظ الآمن.', 16, 1); RETURN; END
    IF (@paymentMethod = 0 AND @boxId IS NULL) BEGIN RAISERROR(N'يجب اختيار الصندوق لطريقة الدفع النقدي.', 16, 1); RETURN; END
    IF (@paymentMethod = 2 AND @bankId IS NULL) BEGIN RAISERROR(N'يجب اختيار البنك لطريقة التحويل البنكي.', 16, 1); RETURN; END
    IF (@boxId IS NOT NULL AND @bankId IS NOT NULL) BEGIN RAISERROR(N'اختر صندوقا أو بنكا فقط.', 16, 1); RETURN; END

    DECLARE
        @partyAccount nvarchar(55),
        @creditAccount nvarchar(55),
        @noteSerial varchar(50),
        @noteSerial1 varchar(50),
        @voucherIdDebit int,
        @voucherIdCredit int,
        @lineNo1Debit float,
        @lineNo1Credit float,
        @noteSerialNumeric float,
        @noteSerial1Numeric float,
        @isEdit bit,
        @voucherPrefix varchar(10),
        @description nvarchar(4000),
        @accountIntervalId int,
        @rc int;

    SET @partyAccount = LTRIM(RTRIM(@partyAccountCode));
    SET @description = COALESCE(NULLIF(@remark, N''), NULLIF(@payDes, N''), N'سند صرف');
    SET @isEdit = CASE WHEN ISNULL(@noteId, 0) > 0 THEN 1 ELSE 0 END;
    SET @voucherPrefix = CASE @paymentMethod WHEN 0 THEN 'CSH' WHEN 1 THEN 'CHQ' WHEN 2 THEN 'TRN' ELSE NULL END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.sanad_numbering WITH (NOLOCK)
        WHERE branch_no = @branchId
          AND sanad_no = 4
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

    SELECT TOP (1) @accountIntervalId = Account_Interval_ID
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
    WHERE Account_Interval_ID IS NOT NULL
    ORDER BY RecordDate DESC;
    SET @accountIntervalId = ISNULL(@accountIntervalId, 0);

    BEGIN TRANSACTION;

    IF @isEdit = 1
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteID = @noteId AND NoteType = 5)
        BEGIN RAISERROR(N'سند الصرف غير موجود.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
        IF EXISTS (SELECT 1 FROM dbo.Notes WHERE NoteID = @noteId AND ISNULL(NotePosted, 0) = 1)
        BEGIN RAISERROR(N'لا يمكن تعديل سند مرحل.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
        IF EXISTS (SELECT 1 FROM dbo.TblNotesBillBuyPayment WHERE NoteID1 = @noteId)
           OR EXISTS (SELECT 1 FROM dbo.TblNotesBillProjectPayment WHERE NoteID1 = @noteId)
           OR EXISTS (SELECT 1 FROM dbo.TblNotesBillVindorPayment WHERE NoteID1 = @noteId)
           OR EXISTS (SELECT 1 FROM dbo.TblSalaryNotesPayment WHERE TransID = @noteId)
           OR EXISTS (SELECT 1 FROM dbo.TblEmpAdvance WHERE NoteID = @noteId OR AdvanceID = @noteId)
        BEGIN RAISERROR(N'هذا السند مرتبط بتوزيعات أو رواتب أو سلف ويجب تعديله من مساره الأصلي.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

        SELECT
            @noteSerial = CONVERT(varchar(50), CONVERT(decimal(38,0), NoteSerial)),
            @noteSerial1 = CONVERT(varchar(50), CONVERT(decimal(38,0), NoteSerial1))
        FROM dbo.Notes
        WHERE NoteID = @noteId;
    END
    ELSE
    BEGIN
        SELECT @noteId = ISNULL(MAX(NoteID), 0) + 1 FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK);

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
            @sanadNo = 4,
            @noteType = 5,
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
            Note_Value2 = CONVERT(float, @amount),
            Note_ValueE = CONVERT(float, @amount),
            Rate = 1,
            BankID = CASE WHEN @paymentMethod = 2 THEN @bankId ELSE NULL END,
            BoxID = CASE WHEN @paymentMethod = 0 THEN @boxId ELSE NULL END,
            ChqueNum = NULL,
            DueDate = NULL,
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
            TxtChequeNumber1 = NULL,
            DtpChequeDueDate1 = NULL,
            ManualNo = @manualNo,
            NCashingType = @receiptClass,
            Prefix = @voucherPrefix,
            PayDes = @payDes,
            PayDes1 = @payDes1,
            VAT = 0,
            TotalValue = CONVERT(float, @amount),
            TotalNotesValue = CONVERT(float, @amount),
            IncludVAT = 0,
            AccountPaym = @partyAccount,
            Account_DebitSide = @partyAccount,
            Account_CreditSide = @creditAccount
        WHERE NoteID = @noteId AND NoteType = 5;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Notes
        (
            NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, OldNoteSerial1,
            Note_Value, Note_Value2, Note_ValueE, Rate,
            BankID, BoxID, UserID, Remark, CashingType, NoteCashingType, NotePosted,
            numbering_type, numbering_type1, sanad_year, sanad_month, branch_no, user_name,
            person, too, ORDER_NO, PaymentType, TxtChequeNumber1, DtpChequeDueDate1,
            ManualNo, NCashingType, Prefix, PayDes, PayDes1, VAT, TotalValue, TotalNotesValue, IncludVAT,
            AccountPaym, Account_DebitSide, Account_CreditSide
        )
        VALUES
        (
            @noteId, @noteDate, 5, @noteSerialNumeric, @noteSerial1Numeric, CONVERT(nvarchar(255), @noteSerial1),
            CONVERT(float, @amount), CONVERT(float, @amount), CONVERT(float, @amount), 1,
            CASE WHEN @paymentMethod = 2 THEN @bankId ELSE NULL END,
            CASE WHEN @paymentMethod = 0 THEN @boxId ELSE NULL END,
            @userId, @remark, @cashingType, @paymentMethod, 0,
            ISNULL((SELECT TOP (1) numbering_id FROM dbo.sanad_numbering WHERE branch_no = @branchId AND sanad_no = 0), 0),
            ISNULL((SELECT TOP (1) numbering_id FROM dbo.sanad_numbering WHERE branch_no = @branchId AND sanad_no = 4), 0),
            YEAR(@noteDate), MONTH(@noteDate), @branchId, CONVERT(nvarchar(50), @userId),
            @partyDisplay, @partyDisplay, @orderNo, @paymentMethod, NULL, NULL,
            @manualNo, @receiptClass, @voucherPrefix, @payDes, @payDes1, 0, CONVERT(float, @amount), CONVERT(float, @amount), 0,
            @partyAccount, @partyAccount, @creditAccount
        );
    END

    DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId;
    DELETE FROM dbo.TblChecqueBoxContent1 WHERE NoteID = @noteId;

    SELECT @voucherIdDebit = ISNULL(MAX(Double_Entry_Vouchers_ID), 0) + 1
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (TABLOCKX, HOLDLOCK);
    SET @voucherIdCredit = @voucherIdDebit + 1;

    SELECT @lineNo1Debit = ISNULL(MAX(DEV_ID_Line_No1), 0) + 1
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (TABLOCKX, HOLDLOCK);
    SET @lineNo1Credit = @lineNo1Debit + 1;

    INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
    (
        Double_Entry_Vouchers_ID, DEV_ID_Line_No, DEV_ID_Line_No1,
        Account_Code, NextAccount_Code, Value, valuee, rate,
        Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
        Notes_ID, UserID, Posted, Account_Interval_ID, branch_id
    )
    VALUES
    (
        @voucherIdDebit, 1, @lineNo1Debit,
        @partyAccount, @creditAccount, CONVERT(money, @amount), CONVERT(money, @amount), 1,
        0, @description, @noteDate,
        @noteId, @userId, 0, @accountIntervalId, @branchId
    ),
    (
        @voucherIdCredit, 2, @lineNo1Credit,
        @creditAccount, @partyAccount, CONVERT(money, @amount), CONVERT(money, @amount), 1,
        1, @description, @noteDate,
        @noteId, @userId, 0, @accountIntervalId, @branchId
    );

    UPDATE dbo.Notes
    SET Double_Entry_Vouchers_ID = @voucherIdCredit
    WHERE NoteID = @noteId AND NoteType = 5;

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
           @voucherIdCredit AS Double_Entry_Vouchers_ID,
           CAST(CASE WHEN @isEdit = 1 THEN N'تم تعديل سند الصرف والقيد المحاسبي.' ELSE N'تم إنشاء سند الصرف والقيد المحاسبي.' END AS nvarchar(300)) AS ResultMessage;
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

    IF @noteType <> 5 BEGIN RAISERROR(N'حذف سند القبض من هذه الشاشة مؤجل حتى يتم إكمال مطابقته مع VB6.', 16, 1); RETURN; END

    DECLARE @noteSerial1 nvarchar(50);

    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteID = @noteId AND NoteType = 5)
    BEGIN RAISERROR(N'سند الصرف غير موجود.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

    IF EXISTS (SELECT 1 FROM dbo.Notes WHERE NoteID = @noteId AND ISNULL(NotePosted, 0) = 1)
    BEGIN RAISERROR(N'لا يمكن حذف سند مرحل.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

    IF EXISTS (SELECT 1 FROM dbo.TblChecqueBoxContent1 WHERE NoteID = @noteId AND ISNULL(Payed, 0) = 1)
    BEGIN RAISERROR(N'لا يمكن حذف السند لوجود شيك مرتبط تم تحصيله أو ترحيله.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

    SELECT @noteSerial1 = CONVERT(nvarchar(50), CONVERT(decimal(38,0), NoteSerial1))
    FROM dbo.Notes
    WHERE NoteID = @noteId AND NoteType = 5;

    DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId;
    DELETE FROM dbo.TblSalaryNotesPayment WHERE TransID = @noteId;
    DELETE FROM dbo.marakes_taklefa_temp WHERE kedno = @noteId;
    DELETE FROM dbo.ReciveDetails WHERE NoteSerial1 = @noteSerial1;
    DELETE FROM dbo.TblChecqueBoxContent1 WHERE NoteID = @noteId;

    DELETE FROM dbo.TblEmpAdvanceDetails WHERE AdvanceID = @noteId;
    DELETE FROM dbo.TblEmpAdvance WHERE NoteID = @noteId OR AdvanceID = @noteId;
    UPDATE dbo.TblEmpAdvanceRequest SET AccAproved = NULL WHERE AdvanceID = @noteId;

    DELETE FROM dbo.TblNotesBillBuyPayment WHERE NoteID1 = @noteId;
    DELETE FROM dbo.TblBillBuyPayment WHERE TypTrans IS NULL AND NoteID = @noteId;
    DELETE FROM dbo.TblNotesBillProjectPayment WHERE NoteID1 = @noteId;
    DELETE FROM dbo.TblBillProjectPayment WHERE NoteID = @noteId;
    DELETE FROM dbo.TblNotesBillVindorPayment WHERE NoteID1 = @noteId;
    DELETE FROM dbo.TblBillVindorPayment WHERE NoteID = @noteId;

    DELETE FROM dbo.Notes WHERE NoteID = @noteId AND NoteType = 5;

    COMMIT TRANSACTION;
END
GO
