/*
    Kishny POS - DOUBLE_ENTREY_VOUCHERS deadlock/performance diagnostics.

    Read mostly. The benchmark helper procedures/tables are created so the
    PowerShell benchmark can simulate POS journal inserts on a test database.

    SQL Server compatibility: SQL Server 2012.
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

PRINT '=== DOUBLE_ENTREY_VOUCHERS columns ===';
SELECT
    c.column_id,
    c.name AS column_name,
    t.name AS data_type,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable
FROM sys.columns AS c
INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
ORDER BY c.column_id;

PRINT '=== DOUBLE_ENTREY_VOUCHERS indexes ===';
SELECT
    i.index_id,
    i.name AS index_name,
    i.type_desc,
    i.is_unique,
    i.fill_factor,
    i.is_disabled,
    ic.key_ordinal,
    c.name AS column_name,
    ic.is_included_column
FROM sys.indexes AS i
LEFT JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
LEFT JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
ORDER BY i.index_id, ic.key_ordinal, ic.index_column_id;

PRINT '=== Index operational stats for latch/lock pressure since last SQL Server restart ===';
SELECT
    OBJECT_NAME(ios.object_id) AS object_name,
    i.name AS index_name,
    ios.leaf_insert_count,
    ios.leaf_allocation_count AS leaf_page_allocation_count,
    ios.page_lock_wait_count,
    ios.page_lock_wait_in_ms,
    ios.row_lock_wait_count,
    ios.row_lock_wait_in_ms,
    ios.page_latch_wait_count,
    ios.page_latch_wait_in_ms
FROM sys.dm_db_index_operational_stats(DB_ID(), OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS'), NULL, NULL) AS ios
INNER JOIN sys.indexes AS i ON i.object_id = ios.object_id AND i.index_id = ios.index_id
ORDER BY ios.page_latch_wait_in_ms DESC, ios.page_lock_wait_in_ms DESC, ios.leaf_allocation_count DESC;

PRINT '=== Triggers on DOUBLE_ENTREY_VOUCHERS ===';
SELECT
    tr.name,
    tr.is_disabled,
    tr.is_instead_of_trigger,
    OBJECT_DEFINITION(tr.object_id) AS trigger_definition
FROM sys.triggers AS tr
WHERE tr.parent_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS');

PRINT '=== Foreign keys touching DOUBLE_ENTREY_VOUCHERS ===';
SELECT
    fk.name,
    parent_table = OBJECT_NAME(fk.parent_object_id),
    referenced_table = OBJECT_NAME(fk.referenced_object_id),
    fk.delete_referential_action_desc,
    fk.update_referential_action_desc,
    parent_column = pc.name,
    referenced_column = rc.name
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns AS pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
INNER JOIN sys.columns AS rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
   OR fk.referenced_object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
ORDER BY fk.name, fkc.constraint_column_id;

PRINT '=== SQL modules reading DOUBLE_ENTREY_VOUCHERS, potential report blockers ===';
SELECT
    OBJECT_SCHEMA_NAME(m.object_id) AS schema_name,
    OBJECT_NAME(m.object_id) AS object_name,
    o.type_desc
FROM sys.sql_modules AS m
INNER JOIN sys.objects AS o ON o.object_id = m.object_id
WHERE m.definition LIKE N'%DOUBLE_ENTREY_VOUCHERS%'
ORDER BY schema_name, object_name;

PRINT '=== Live waits/locks involving DOUBLE_ENTREY_VOUCHERS ===';
SELECT
    r.session_id,
    r.blocking_session_id,
    r.status,
    r.command,
    r.wait_type,
    r.wait_time,
    r.wait_resource,
    tl.resource_type,
    tl.request_mode,
    tl.request_status,
    OBJECT_NAME(p.object_id, tl.resource_database_id) AS object_name,
    i.name AS index_name,
    tl.resource_description,
    LEFT(st.text, 4000) AS sql_text
FROM sys.dm_exec_requests AS r
LEFT JOIN sys.dm_tran_locks AS tl ON tl.request_session_id = r.session_id
LEFT JOIN sys.partitions AS p ON p.hobt_id = tl.resource_associated_entity_id
LEFT JOIN sys.indexes AS i ON i.object_id = p.object_id AND i.index_id = p.index_id
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) AS st
WHERE OBJECT_NAME(p.object_id, tl.resource_database_id) = N'DOUBLE_ENTREY_VOUCHERS'
   OR st.text LIKE N'%DOUBLE_ENTREY_VOUCHERS%'
ORDER BY r.wait_time DESC;

PRINT '=== Recent system_health deadlocks mentioning DOUBLE_ENTREY_VOUCHERS ===';
;WITH xevents AS
(
    SELECT CAST(xet.target_data AS XML) AS TargetData
    FROM sys.dm_xe_session_targets AS xet
    INNER JOIN sys.dm_xe_sessions AS xe ON xe.address = xet.event_session_address
    WHERE xe.name = N'system_health'
      AND xet.target_name = N'ring_buffer'
),
deadlocks AS
(
    SELECT
        DATEADD(mi, DATEDIFF(mi, GETUTCDATE(), GETDATE()), deadlock_event.value(N'(@timestamp)[1]', N'datetime2')) AS event_time,
        deadlock_event.query(N'(data/value/deadlock)[1]') AS deadlock_xml
    FROM xevents
    CROSS APPLY TargetData.nodes(N'RingBufferTarget/event[@name="xml_deadlock_report"]') AS tab(deadlock_event)
)
SELECT TOP (50)
    event_time,
    contains_double_entry = CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%DOUBLE_ENTREY_VOUCHERS%' THEN 1 ELSE 0 END,
    contains_notes = CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%Notes%' THEN 1 ELSE 0 END,
    contains_pos_save = CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%usp_POS_SaveTransaction%' THEN 1 ELSE 0 END,
    contains_voucher_coding = CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%usp_Voucher_coding_V2%' THEN 1 ELSE 0 END,
    deadlock_xml
FROM deadlocks
WHERE CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%DOUBLE_ENTREY_VOUCHERS%'
   OR CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%usp_POS_SaveTransaction%'
ORDER BY event_time DESC;

IF OBJECT_ID(N'dbo.POS_DoubleEntryBenchmarkRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DoubleEntryBenchmarkRun
    (
        RunId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_POS_DoubleEntryBenchmarkRun PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_DoubleEntryBenchmarkRun_CreatedAt DEFAULT(GETDATE()),
        Label NVARCHAR(200) NULL,
        WorkerCount INT NOT NULL,
        IterationsPerWorker INT NOT NULL,
        Mode NVARCHAR(30) NOT NULL,
        StartedAt DATETIME NULL,
        FinishedAt DATETIME NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_DoubleEntryBenchmarkResult', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DoubleEntryBenchmarkResult
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_DoubleEntryBenchmarkResult PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        WorkerId INT NOT NULL,
        IterationNo INT NOT NULL,
        Mode NVARCHAR(30) NOT NULL,
        SessionId INT NOT NULL,
        StartedAt DATETIME NOT NULL,
        FinishedAt DATETIME NOT NULL,
        InsertDurationMs INT NOT NULL,
        TotalDurationMs INT NOT NULL,
        Success BIT NOT NULL,
        ErrorNumber INT NULL,
        ErrorMessage NVARCHAR(1000) NULL,
        DoubleEntryVoucherId INT NULL,
        NoteId INT NULL,
        RowsInserted INT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_DoubleEntryBenchmarkWaitSample', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_DoubleEntryBenchmarkWaitSample
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_DoubleEntryBenchmarkWaitSample PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        CapturedAt DATETIME NOT NULL CONSTRAINT DF_POS_DoubleEntryBenchmarkWaitSample_CapturedAt DEFAULT(GETDATE()),
        session_id INT NULL,
        blocking_session_id INT NULL,
        wait_type NVARCHAR(120) NULL,
        wait_duration_ms BIGINT NULL,
        wait_resource NVARCHAR(1000) NULL,
        resource_type NVARCHAR(120) NULL,
        request_mode NVARCHAR(120) NULL,
        request_status NVARCHAR(120) NULL,
        object_name NVARCHAR(256) NULL,
        index_name NVARCHAR(256) NULL,
        resource_description NVARCHAR(1000) NULL,
        sql_text NVARCHAR(MAX) NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.POS_DoubleEntryBenchmarkResult') AND name = N'IX_POS_DoubleEntryBenchmarkResult_Run')
    CREATE INDEX IX_POS_DoubleEntryBenchmarkResult_Run ON dbo.POS_DoubleEntryBenchmarkResult(RunId, Mode, InsertDurationMs);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.POS_DoubleEntryBenchmarkWaitSample') AND name = N'IX_POS_DoubleEntryBenchmarkWaitSample_Run')
    CREATE INDEX IX_POS_DoubleEntryBenchmarkWaitSample_Run ON dbo.POS_DoubleEntryBenchmarkWaitSample(RunId, wait_type, CapturedAt);
GO

IF OBJECT_ID(N'dbo.usp_POS_DoubleEntryBenchmarkCaptureWaits', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_DoubleEntryBenchmarkCaptureWaits;
GO

CREATE PROCEDURE dbo.usp_POS_DoubleEntryBenchmarkCaptureWaits
    @RunId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.POS_DoubleEntryBenchmarkWaitSample
    (
        RunId, session_id, blocking_session_id, wait_type, wait_duration_ms,
        wait_resource, resource_type, request_mode, request_status,
        object_name, index_name, resource_description, sql_text
    )
    SELECT TOP (1000)
        @RunId,
        r.session_id,
        r.blocking_session_id,
        r.wait_type,
        r.wait_time,
        r.wait_resource,
        tl.resource_type,
        tl.request_mode,
        tl.request_status,
        OBJECT_NAME(p.object_id, tl.resource_database_id),
        i.name,
        tl.resource_description,
        LEFT(st.text, 4000)
    FROM sys.dm_exec_requests AS r
    LEFT JOIN sys.dm_tran_locks AS tl ON tl.request_session_id = r.session_id
    LEFT JOIN sys.partitions AS p ON p.hobt_id = tl.resource_associated_entity_id
    LEFT JOIN sys.indexes AS i ON i.object_id = p.object_id AND i.index_id = p.index_id
    OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) AS st
    WHERE r.session_id <> @@SPID
      AND
      (
          OBJECT_NAME(p.object_id, tl.resource_database_id) = N'DOUBLE_ENTREY_VOUCHERS'
          OR st.text LIKE N'%usp_POS_DoubleEntryVoucherBenchmarkWorker%'
          OR r.wait_type LIKE N'LCK[_]M[_]%'
          OR r.wait_type IN (N'PAGELATCH_EX', N'PAGELATCH_SH', N'WRITELOG')
          OR r.wait_type LIKE N'PAGEIOLATCH[_]%'
      );
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_DoubleEntryVoucherBenchmarkWorker', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_DoubleEntryVoucherBenchmarkWorker;
GO

CREATE PROCEDURE dbo.usp_POS_DoubleEntryVoucherBenchmarkWorker
    @RunId UNIQUEIDENTIFIER,
    @WorkerId INT,
    @Mode NVARCHAR(30),
    @Iterations INT = 20,
    @BranchId INT = 1,
    @UserID INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @i INT,
        @StartedAt DATETIME,
        @InsertStartedAt DATETIME,
        @FinishedAt DATETIME,
        @NoteIdBig BIGINT,
        @DevIdBig BIGINT,
        @NoteID INT,
        @DevID INT,
        @Err NVARCHAR(500),
        @RecordDate DATETIME,
        @DebitAccount NVARCHAR(100),
        @CreditAccount NVARCHAR(100),
        @Rows INT,
        @InsertDurationMs INT;

    SET @i = 1;
    SET @RecordDate = GETDATE();

    SELECT TOP (1) @DebitAccount = Account_Code
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
    WHERE NULLIF(LTRIM(RTRIM(ISNULL(Account_Code, N''))), N'') IS NOT NULL
    ORDER BY RecordDate DESC;

    SELECT TOP (1) @CreditAccount = Account_Code
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
    WHERE NULLIF(LTRIM(RTRIM(ISNULL(Account_Code, N''))), N'') IS NOT NULL
      AND Account_Code <> @DebitAccount
    ORDER BY RecordDate DESC;

    IF @DebitAccount IS NULL OR @CreditAccount IS NULL
        RAISERROR('No usable account codes found in DOUBLE_ENTREY_VOUCHERS.', 16, 1);

    WHILE @i <= @Iterations
    BEGIN
        SET @StartedAt = GETDATE();
        SET @InsertDurationMs = 0;
        SET @Rows = CASE WHEN @Mode = N'card' THEN 4 ELSE 2 END;

        BEGIN TRY
            SET @Err = NULL;
            EXEC dbo.GetNextID_FromSequence N'Notes', N'NoteID', @NoteIdBig OUTPUT, @Err OUTPUT;
            IF @Err IS NOT NULL OR @NoteIdBig IS NULL RAISERROR('NoteID allocation failed.', 16, 1);

            SET @Err = NULL;
            EXEC dbo.GetNextID_FromSequence N'DOUBLE_ENTREY_VOUCHERS', N'Double_Entry_Vouchers_ID', @DevIdBig OUTPUT, @Err OUTPUT;
            IF @Err IS NOT NULL OR @DevIdBig IS NULL RAISERROR('DoubleEntryVoucherID allocation failed.', 16, 1);

            SET @NoteID = CONVERT(INT, @NoteIdBig);
            SET @DevID = CONVERT(INT, @DevIdBig);

            BEGIN TRANSACTION;

            INSERT INTO dbo.Notes
            (
                NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value,
                Transaction_ID, UserID, Remark, type, branch_no, sanad_type,
                sanad_source, Double_Entry_Vouchers_ID, PaymentType, Prefix
            )
            VALUES
            (
                @NoteID, @RecordDate, 170, @DevID, @DevID, 100,
                NULL, @UserID, N'POS DEV benchmark', N'POS', @BranchId, N'170',
                N'POS', @DevID, 1, N'BM'
            );

            SET @InsertStartedAt = GETDATE();

            INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
            (
                Double_Entry_Vouchers_ID,
                DEV_ID_Line_No,
                Account_Code,
                Value,
                Credit_Or_Debit,
                Double_Entry_Vouchers_Description,
                RecordDate,
                Notes_ID,
                Transaction_ID,
                UserID,
                DEV_Serial,
                currency,
                rate,
                branch_id,
                DueDate
            )
            SELECT
                @DevID,
                v.LineNumber,
                v.AccountCode,
                v.EntryValue,
                v.CreditOrDebit,
                N'POS benchmark accounting insert',
                @RecordDate,
                @NoteID,
                NULL,
                @UserID,
                CONVERT(NVARCHAR(50), @DevID),
                N'',
                1,
                @BranchId,
                @RecordDate
            FROM
            (
                SELECT 1 AS LineNumber, @DebitAccount AS AccountCode, 100.00 AS EntryValue, 0 AS CreditOrDebit
                UNION ALL SELECT 2, @CreditAccount, 100.00, 1
                UNION ALL SELECT 3, @DebitAccount, 25.00, 0 WHERE @Mode = N'card'
                UNION ALL SELECT 4, @CreditAccount, 25.00, 1 WHERE @Mode = N'card'
            ) AS v
            ORDER BY v.LineNumber;

            SET @InsertDurationMs = DATEDIFF(MILLISECOND, @InsertStartedAt, GETDATE());

            COMMIT TRANSACTION;

            SET @FinishedAt = GETDATE();

            INSERT INTO dbo.POS_DoubleEntryBenchmarkResult
            (
                RunId, WorkerId, IterationNo, Mode, SessionId, StartedAt, FinishedAt,
                InsertDurationMs, TotalDurationMs, Success, DoubleEntryVoucherId, NoteId, RowsInserted
            )
            VALUES
            (
                @RunId, @WorkerId, @i, @Mode, @@SPID, @StartedAt, @FinishedAt,
                @InsertDurationMs, DATEDIFF(MILLISECOND, @StartedAt, @FinishedAt),
                1, @DevID, @NoteID, @Rows
            );
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;

            SET @FinishedAt = GETDATE();

            INSERT INTO dbo.POS_DoubleEntryBenchmarkResult
            (
                RunId, WorkerId, IterationNo, Mode, SessionId, StartedAt, FinishedAt,
                InsertDurationMs, TotalDurationMs, Success, ErrorNumber, ErrorMessage, RowsInserted
            )
            VALUES
            (
                @RunId, @WorkerId, @i, @Mode, @@SPID, @StartedAt, @FinishedAt,
                @InsertDurationMs, DATEDIFF(MILLISECOND, @StartedAt, @FinishedAt),
                0, ERROR_NUMBER(), LEFT(ERROR_MESSAGE(), 1000), @Rows
            );
        END CATCH;

        SET @i = @i + 1;
    END;
END;
GO

PRINT '=== DOUBLE_ENTREY_VOUCHERS diagnostics and benchmark helpers ready ===';
