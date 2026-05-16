/*
    Kishny POS - sequence / voucher allocator contention benchmark.

    TEST DATABASE ONLY.
    This script creates benchmark helper tables and procedures. It can consume real
    sequence values when the "Current" allocator is tested. Do not run against
    production during business hours.

    Recommended runner:
      Areas\Pos\Tools\Invoke-PosSequenceAllocatorBenchmark.ps1

    SQL Server compatibility: SQL Server 2012.
*/

SET NOCOUNT ON;

PRINT '=== Current allocator definitions ===';
SELECT OBJECT_NAME(object_id) AS ObjectName, definition
FROM sys.sql_modules
WHERE object_id IN
(
    OBJECT_ID(N'dbo.GetNextID_FromSequence', N'P'),
    OBJECT_ID(N'dbo.usp_POS_SaveTransaction', N'P'),
    OBJECT_ID(N'dbo.usp_Voucher_coding_V2', N'P'),
    OBJECT_ID(N'dbo.usp_Notes_coding_V2', N'P')
);

IF OBJECT_ID(N'dbo.POS_AllocatorBenchmarkRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_AllocatorBenchmarkRun
    (
        RunId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_POS_AllocatorBenchmarkRun PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_AllocatorBenchmarkRun_CreatedAt DEFAULT(GETDATE()),
        Label NVARCHAR(200) NULL,
        AllocatorName NVARCHAR(50) NOT NULL,
        WorkerCount INT NOT NULL,
        IterationsPerWorker INT NOT NULL,
        StartedAt DATETIME NULL,
        FinishedAt DATETIME NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_AllocatorBenchmarkResult', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_AllocatorBenchmarkResult
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_AllocatorBenchmarkResult PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        WorkerId INT NOT NULL,
        IterationNo INT NOT NULL,
        AllocatorName NVARCHAR(50) NOT NULL,
        SessionId INT NOT NULL,
        StartedAt DATETIME NOT NULL,
        FinishedAt DATETIME NOT NULL,
        DurationMs INT NOT NULL,
        Success BIT NOT NULL,
        ErrorNumber INT NULL,
        ErrorMessage NVARCHAR(1000) NULL,
        NextValue BIGINT NULL,
        Detail NVARCHAR(400) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_AllocatorBenchmarkWaitSample', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_AllocatorBenchmarkWaitSample
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_AllocatorBenchmarkWaitSample PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        CapturedAt DATETIME NOT NULL CONSTRAINT DF_POS_AllocatorBenchmarkWaitSample_CapturedAt DEFAULT(GETDATE()),
        session_id INT NULL,
        blocking_session_id INT NULL,
        wait_type NVARCHAR(120) NULL,
        wait_duration_ms BIGINT NULL,
        resource_description NVARCHAR(1000) NULL,
        resource_type NVARCHAR(120) NULL,
        request_mode NVARCHAR(120) NULL,
        request_status NVARCHAR(120) NULL,
        object_name NVARCHAR(256) NULL,
        index_name NVARCHAR(256) NULL,
        sql_text NVARCHAR(MAX) NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.POS_AllocatorBenchmarkResult') AND name = N'IX_POS_AllocatorBenchmarkResult_Run')
    CREATE INDEX IX_POS_AllocatorBenchmarkResult_Run ON dbo.POS_AllocatorBenchmarkResult(RunId, AllocatorName, DurationMs);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.POS_AllocatorBenchmarkWaitSample') AND name = N'IX_POS_AllocatorBenchmarkWaitSample_Run')
    CREATE INDEX IX_POS_AllocatorBenchmarkWaitSample_Run ON dbo.POS_AllocatorBenchmarkWaitSample(RunId, wait_type, CapturedAt);
GO

IF OBJECT_ID(N'dbo.usp_POS_AllocatorBenchmarkWorker', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_AllocatorBenchmarkWorker;
GO

CREATE PROCEDURE dbo.usp_POS_AllocatorBenchmarkWorker
    @RunId UNIQUEIDENTIFIER,
    @WorkerId INT,
    @AllocatorName NVARCHAR(50),
    @Iterations INT = 20,
    @BranchId INT = 1,
    @StoreID INT = 1,
    @UserID INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @i INT = 1,
        @StartedAt DATETIME,
        @FinishedAt DATETIME,
        @NextValue BIGINT,
        @ErrorMsg NVARCHAR(500),
        @ErrorNumber INT,
        @Result NVARCHAR(50),
        @MSerInv INT,
        @ReturnCode INT,
        @BenchmarkDate DATETIME,
        @CurrentSerial BIGINT,
        @Detail NVARCHAR(400);

    SET @BenchmarkDate = GETDATE();

    WHILE @i <= @Iterations
    BEGIN
        SET @StartedAt = GETDATE();
        SET @NextValue = NULL;
        SET @ErrorMsg = NULL;
        SET @ErrorNumber = NULL;
        SET @Detail = NULL;

        BEGIN TRY
            IF @AllocatorName = N'TransactionID'
            BEGIN
                EXEC dbo.GetNextID_FromSequence N'Transactions', N'Transaction_ID', @NextValue OUTPUT, @ErrorMsg OUTPUT;
                SET @Detail = N'dbo.GetNextID_FromSequence:Transactions.Transaction_ID';
            END
            ELSE IF @AllocatorName = N'NotesNoteID'
            BEGIN
                EXEC dbo.GetNextID_FromSequence N'Notes', N'NoteID', @NextValue OUTPUT, @ErrorMsg OUTPUT;
                SET @Detail = N'dbo.GetNextID_FromSequence:Notes.NoteID';
            END
            ELSE IF @AllocatorName = N'DoubleEntryVoucherID'
            BEGIN
                EXEC dbo.GetNextID_FromSequence N'DOUBLE_ENTREY_VOUCHERS', N'Double_Entry_Vouchers_ID', @NextValue OUTPUT, @ErrorMsg OUTPUT;
                SET @Detail = N'dbo.GetNextID_FromSequence:DOUBLE_ENTREY_VOUCHERS.Double_Entry_Vouchers_ID';
            END
            ELSE IF @AllocatorName = N'DEVSerial'
            BEGIN
                IF OBJECT_ID(N'dbo.POS_DEVSerialAllocator', N'U') IS NULL
                    RAISERROR('dbo.POS_DEVSerialAllocator does not exist.', 16, 1);

                BEGIN TRANSACTION;

                UPDATE dbo.POS_DEVSerialAllocator WITH (UPDLOCK, HOLDLOCK)
                    SET @CurrentSerial = LastSerialNo + 1,
                        LastSerialNo = LastSerialNo + 1,
                        UpdatedAt = GETDATE()
                WHERE SerialDate = CONVERT(DATE, @BenchmarkDate);

                IF @@ROWCOUNT = 0
                BEGIN
                    SELECT @CurrentSerial = ISNULL(MAX(CONVERT(BIGINT, DEV_Serial)), 0) + 1
                    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
                    WHERE RecordDate >= CONVERT(DATE, @BenchmarkDate)
                      AND RecordDate < DATEADD(DAY, 1, CONVERT(DATE, @BenchmarkDate))
                      AND ISNUMERIC(DEV_Serial) = 1;

                    INSERT INTO dbo.POS_DEVSerialAllocator(SerialDate, LastSerialNo, UpdatedAt)
                    VALUES (CONVERT(DATE, @BenchmarkDate), @CurrentSerial, GETDATE());
                END;

                COMMIT TRANSACTION;

                SET @NextValue = @CurrentSerial;
                SET @Detail = N'dbo.POS_DEVSerialAllocator by SerialDate';
            END
            ELSE IF @AllocatorName = N'VoucherCoding21'
            BEGIN
                EXEC @ReturnCode = dbo.usp_Voucher_coding_V2
                    @my_branch = @BranchId,
                    @date1 = @BenchmarkDate,
                    @Sanad_No = 7,
                    @NoteType = 170,
                    @departement_name = 1,
                    @Transaction_Type = 21,
                    @Prefix = NULL,
                    @StoreID = @StoreID,
                    @BillType = 0,
                    @MosemID = 0,
                    @mTableName = NULL,
                    @mUserID = @UserID,
                    @Result = @Result OUTPUT,
                    @mSerInv = @MSerInv OUTPUT;

                IF @ReturnCode <> 0 OR @Result IS NULL OR @Result = N'error'
                    RAISERROR('dbo.usp_Voucher_coding_V2 failed.', 16, 1);

                SET @NextValue = CONVERT(BIGINT, @MSerInv);
                SET @Detail = N'dbo.usp_Voucher_coding_V2:Transaction_Type=21;Sanad_No=7';
            END
            ELSE IF @AllocatorName = N'NotesCoding'
            BEGIN
                EXEC @ReturnCode = dbo.usp_Notes_coding_V2
                    @my_branch = @BranchId,
                    @date1 = @BenchmarkDate,
                    @departement_name = 1,
                    @Result = @Result OUTPUT;

                IF @ReturnCode <> 0 OR @Result IS NULL OR @Result = N'error'
                    RAISERROR('dbo.usp_Notes_coding_V2 failed.', 16, 1);

                IF ISNUMERIC(@Result) = 1
                    SET @NextValue = CONVERT(BIGINT, @Result);
                SET @Detail = N'dbo.usp_Notes_coding_V2';
            END
            ELSE
            BEGIN
                RAISERROR('Unknown allocator name.', 16, 1);
            END;

            IF @ErrorMsg IS NOT NULL
                RAISERROR(@ErrorMsg, 16, 1);

            SET @FinishedAt = GETDATE();

            INSERT INTO dbo.POS_AllocatorBenchmarkResult
            (
                RunId, WorkerId, IterationNo, AllocatorName, SessionId,
                StartedAt, FinishedAt, DurationMs, Success, NextValue, Detail
            )
            VALUES
            (
                @RunId, @WorkerId, @i, @AllocatorName, @@SPID,
                @StartedAt, @FinishedAt, DATEDIFF(MILLISECOND, @StartedAt, @FinishedAt),
                1, @NextValue, @Detail
            );
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;

            SET @FinishedAt = GETDATE();
            SET @ErrorNumber = ERROR_NUMBER();

            INSERT INTO dbo.POS_AllocatorBenchmarkResult
            (
                RunId, WorkerId, IterationNo, AllocatorName, SessionId,
                StartedAt, FinishedAt, DurationMs, Success, ErrorNumber, ErrorMessage, Detail
            )
            VALUES
            (
                @RunId, @WorkerId, @i, @AllocatorName, @@SPID,
                @StartedAt, @FinishedAt, DATEDIFF(MILLISECOND, @StartedAt, @FinishedAt),
                0, @ErrorNumber, LEFT(ERROR_MESSAGE(), 1000), @Detail
            );
        END CATCH;

        SET @i = @i + 1;
    END;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_AllocatorBenchmarkCaptureWaits', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_AllocatorBenchmarkCaptureWaits;
GO

CREATE PROCEDURE dbo.usp_POS_AllocatorBenchmarkCaptureWaits
    @RunId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.POS_AllocatorBenchmarkWaitSample
    (
        RunId,
        session_id,
        blocking_session_id,
        wait_type,
        wait_duration_ms,
        resource_description,
        resource_type,
        request_mode,
        request_status,
        object_name,
        index_name,
        sql_text
    )
    SELECT TOP (1000)
        @RunId,
        wt.session_id,
        r.blocking_session_id,
        wt.wait_type,
        wt.wait_duration_ms,
        wt.resource_description,
        tl.resource_type,
        tl.request_mode,
        tl.request_status,
        OBJECT_NAME(p.object_id, tl.resource_database_id) AS object_name,
        i.name AS index_name,
        LEFT(st.text, 4000) AS sql_text
    FROM sys.dm_os_waiting_tasks AS wt
    LEFT JOIN sys.dm_exec_requests AS r ON r.session_id = wt.session_id
    LEFT JOIN sys.dm_tran_locks AS tl ON tl.request_session_id = wt.session_id
    LEFT JOIN sys.partitions AS p ON p.hobt_id = tl.resource_associated_entity_id
    LEFT JOIN sys.indexes AS i ON i.object_id = p.object_id AND i.index_id = p.index_id
    OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) AS st
    WHERE wt.session_id <> @@SPID
      AND
      (
          st.text LIKE N'%usp_POS_AllocatorBenchmarkWorker%'
          OR wt.resource_description LIKE N'%GetNextID_FromSequence%'
          OR wt.wait_type LIKE N'LCK[_]M[_]%'
          OR wt.wait_type LIKE N'PAGELATCH[_]%'
          OR wt.wait_type IN (N'WRITELOG', N'CXPACKET', N'CXCONSUMER')
      );

    INSERT INTO dbo.POS_AllocatorBenchmarkWaitSample
    (
        RunId,
        session_id,
        blocking_session_id,
        wait_type,
        wait_duration_ms,
        resource_description,
        resource_type,
        request_mode,
        request_status,
        object_name,
        index_name,
        sql_text
    )
    SELECT
        @RunId,
        tl.request_session_id,
        r.blocking_session_id,
        r.wait_type,
        r.wait_time,
        tl.resource_description,
        tl.resource_type,
        tl.request_mode,
        tl.request_status,
        OBJECT_NAME(p.object_id, tl.resource_database_id) AS object_name,
        i.name AS index_name,
        LEFT(st.text, 4000) AS sql_text
    FROM sys.dm_tran_locks AS tl
    LEFT JOIN sys.dm_exec_requests AS r ON r.session_id = tl.request_session_id
    LEFT JOIN sys.partitions AS p ON p.hobt_id = tl.resource_associated_entity_id
    LEFT JOIN sys.indexes AS i ON i.object_id = p.object_id AND i.index_id = p.index_id
    OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) AS st
    WHERE tl.request_session_id <> @@SPID
      AND
      (
          tl.resource_type = N'APPLICATION'
          OR OBJECT_NAME(p.object_id, tl.resource_database_id) IN
          (
              N'POS_DEVSerialAllocator',
              N'Transactions',
              N'Notes',
              N'DOUBLE_ENTREY_VOUCHERS'
          )
      )
      AND
      (
          tl.request_status = N'WAIT'
          OR r.wait_type IS NOT NULL
          OR tl.resource_type = N'APPLICATION'
      );
END;
GO

PRINT '=== Benchmark helpers installed. Run Invoke-PosSequenceAllocatorBenchmark.ps1 to create concurrent sessions. ===';

/*
    Summary queries after a run:

    DECLARE @RunId UNIQUEIDENTIFIER = '<run id here>';

    ;WITH Ordered AS
    (
        SELECT
            r.*,
            ROW_NUMBER() OVER (PARTITION BY RunId, AllocatorName ORDER BY DurationMs) AS rn,
            COUNT(*) OVER (PARTITION BY RunId, AllocatorName) AS cnt
        FROM dbo.POS_AllocatorBenchmarkResult AS r
        WHERE RunId = @RunId
    )
    SELECT
        AllocatorName,
        COUNT(*) AS total_attempts,
        SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) AS success_count,
        SUM(CASE WHEN ErrorNumber = 1205 THEN 1 ELSE 0 END) AS deadlock_count,
        SUM(CASE WHEN ErrorNumber = -2 THEN 1 ELSE 0 END) AS timeout_count,
        AVG(DurationMs) AS avg_duration_ms,
        MAX(DurationMs) AS max_duration_ms,
        MIN(CASE WHEN rn >= CEILING(cnt * 0.95) THEN DurationMs END) AS p95_duration_ms,
        MIN(CASE WHEN rn >= CEILING(cnt * 0.99) THEN DurationMs END) AS p99_duration_ms
    FROM Ordered
    GROUP BY AllocatorName;

    SELECT wait_type, object_name, index_name, resource_description, COUNT(*) AS samples, MAX(wait_duration_ms) AS max_wait_ms
    FROM dbo.POS_AllocatorBenchmarkWaitSample
    WHERE RunId = @RunId
    GROUP BY wait_type, object_name, index_name, resource_description
    ORDER BY samples DESC, max_wait_ms DESC;

    -- Recent deadlocks from system_health; XML contains owner/victim/resource/statement details.
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
    SELECT TOP (20)
        event_time,
        CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%GetNextID_FromSequence%' THEN 1 ELSE 0 END AS contains_GetNextID,
        CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%usp_Voucher_coding_V2%' THEN 1 ELSE 0 END AS contains_VoucherCoding,
        CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%usp_Notes_coding_V2%' THEN 1 ELSE 0 END AS contains_NotesCoding,
        CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%POS_DEVSerialAllocator%' THEN 1 ELSE 0 END AS contains_DEVSerialAllocator,
        deadlock_xml
    FROM deadlocks
    ORDER BY event_time DESC;
*/
