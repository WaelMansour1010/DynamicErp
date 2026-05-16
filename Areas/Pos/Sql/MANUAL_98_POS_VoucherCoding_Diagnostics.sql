/*
    MANUAL_98_POS_VoucherCoding_Diagnostics.sql

    Purpose:
      Read-only diagnostics for dbo.usp_Voucher_coding_V2 / dbo.usp_GetNextSerial_V2
      under POS save pressure.

    Focus:
      - Transactions / Transaction_Type = 21 / Sanad_No = 7
      - Transactions / Transaction_Type = 19 / Sanad_No = 10
      - SerialCounters_V2 contention, waits, duplicates, and stage timing

    SQL Server compatibility: SQL Server 2012+
*/

SET NOCOUNT ON;

DECLARE @FromDate DATETIME;
DECLARE @ToDate DATETIME;

SET @ToDate = GETDATE();
SET @FromDate = DATEADD(DAY, -7, @ToDate);

PRINT '01. Environment';
SELECT
    DatabaseName = DB_NAME(),
    ServerName = @@SERVERNAME,
    CapturedAt = GETDATE();

PRINT '02. Object existence';
SELECT
    ObjectName,
    ObjectId = OBJECT_ID(ObjectName),
    ObjectType = OBJECTPROPERTY(OBJECT_ID(ObjectName), 'IsProcedure')
FROM (VALUES
    (N'dbo.usp_Voucher_coding_V2'),
    (N'dbo.usp_GetNextSerial_V2'),
    (N'dbo.SerialCounters_V2'),
    (N'dbo.SerialTableMapping'),
    (N'dbo.sanad_numbering'),
    (N'dbo.POS_SaveAllocationStageLog')
) AS x(ObjectName);

PRINT '03. Procedure definitions';
SELECT ObjectName = N'dbo.usp_Voucher_coding_V2', DefinitionText = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_Voucher_coding_V2'));
SELECT ObjectName = N'dbo.usp_GetNextSerial_V2', DefinitionText = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_GetNextSerial_V2'));

PRINT '04. Referenced entities';
BEGIN TRY
    SELECT ReferencingObject = N'dbo.usp_Voucher_coding_V2', *
    FROM sys.dm_sql_referenced_entities(N'dbo.usp_Voucher_coding_V2', N'OBJECT');

    SELECT ReferencingObject = N'dbo.usp_GetNextSerial_V2', *
    FROM sys.dm_sql_referenced_entities(N'dbo.usp_GetNextSerial_V2', N'OBJECT');
END TRY
BEGIN CATCH
    SELECT ReferenceError = ERROR_MESSAGE();
END CATCH;

PRINT '05. Serial mapping for POS tables';
IF OBJECT_ID(N'dbo.SerialTableMapping', N'U') IS NOT NULL
BEGIN
    SELECT *
    FROM dbo.SerialTableMapping WITH (READCOMMITTEDLOCK)
    WHERE SourceTable IN ('Transactions', 'Notes')
    ORDER BY SourceTable;
END

PRINT '06. Sanad numbering for POS voucher coding';
IF OBJECT_ID(N'dbo.sanad_numbering', N'U') IS NOT NULL
BEGIN
    SELECT
        branch_no,
        sanad_no,
        numbering_id,
        start_at,
        end_at,
        no_of_digit,
        YearDigit,
        Prefix,
        StoreCoding
    FROM dbo.sanad_numbering WITH (READCOMMITTEDLOCK)
    WHERE sanad_no IN (7, 10)
    ORDER BY branch_no, sanad_no, Prefix;
END

PRINT '06B. Effective POS voucher serial scope by sanad_numbering.StoreCoding';
DECLARE @ConfiguredPOSVoucherSerialScope NVARCHAR(20);
SET @ConfiguredPOSVoucherSerialScope = N'Company';

IF OBJECT_ID(N'dbo.TblOptions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblOptions', N'POSVoucherSerialScope') IS NOT NULL
BEGIN
    EXEC sp_executesql
        N'SELECT TOP (1) @scopeOut = POSVoucherSerialScope FROM dbo.TblOptions',
        N'@scopeOut NVARCHAR(20) OUTPUT',
        @scopeOut = @ConfiguredPOSVoucherSerialScope OUTPUT;
END;

SET @ConfiguredPOSVoucherSerialScope =
    CASE
        WHEN UPPER(LTRIM(RTRIM(ISNULL(@ConfiguredPOSVoucherSerialScope, N'Company')))) = N'BRANCH' THEN N'Branch'
        WHEN UPPER(LTRIM(RTRIM(ISNULL(@ConfiguredPOSVoucherSerialScope, N'Company')))) = N'BRANCHSTORE' THEN N'BranchStore'
        ELSE N'Company'
    END;

IF OBJECT_ID(N'dbo.sanad_numbering', N'U') IS NOT NULL
BEGIN
    SELECT
        ConfiguredPOSVoucherSerialScope = @ConfiguredPOSVoucherSerialScope,
        branch_no,
        sanad_no,
        StoreCoding = ISNULL(StoreCoding, 0),
        EffectiveScope =
            CASE
                WHEN @ConfiguredPOSVoucherSerialScope = N'BranchStore' AND ISNULL(StoreCoding, 0) = 1 THEN N'BranchStore'
                WHEN @ConfiguredPOSVoucherSerialScope IN (N'Branch', N'BranchStore') THEN N'Branch'
                ELSE N'Company'
            END,
        EffectiveBranchKey =
            CASE
                WHEN @ConfiguredPOSVoucherSerialScope = N'Company' THEN 0
                ELSE branch_no
            END,
        EffectiveStoreKey =
            CASE
                WHEN @ConfiguredPOSVoucherSerialScope = N'BranchStore' AND ISNULL(StoreCoding, 0) = 1 THEN N'<passed StoreID>'
                ELSE N'0'
            END,
        CounterKeyPattern =
            N'SourceTable=Transactions;TypeCode=' +
            CASE sanad_no WHEN 7 THEN N'21' WHEN 10 THEN N'19' ELSE CONVERT(NVARCHAR(20), sanad_no) END +
            N';BranchKey=' +
            CASE WHEN @ConfiguredPOSVoucherSerialScope = N'Company' THEN N'0' ELSE CONVERT(NVARCHAR(20), branch_no) END +
            N';StoreKey=' +
            CASE WHEN @ConfiguredPOSVoucherSerialScope = N'BranchStore' AND ISNULL(StoreCoding, 0) = 1 THEN N'<passed StoreID>' ELSE N'0' END +
            N';YearNum=YEAR(@date);MonthNum=MONTH(@date)'
    FROM dbo.sanad_numbering WITH (READCOMMITTEDLOCK)
    WHERE sanad_no IN (7, 10)
    ORDER BY branch_no, sanad_no;
END

PRINT '07. SerialCounters_V2 columns and indexes';
IF OBJECT_ID(N'dbo.SerialCounters_V2', N'U') IS NOT NULL
BEGIN
    SELECT
        c.column_id,
        ColumnName = c.name,
        TypeName = t.name,
        c.max_length,
        c.precision,
        c.scale,
        c.is_nullable
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SerialCounters_V2')
    ORDER BY c.column_id;

    SELECT
        IndexName = i.name,
        i.type_desc,
        i.is_unique,
        ic.key_ordinal,
        ic.is_included_column,
        ColumnName = c.name
    FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.SerialCounters_V2')
    ORDER BY i.name, ic.key_ordinal, ic.index_column_id;
END

PRINT '08. Logical duplicate counter rows';
IF OBJECT_ID(N'dbo.SerialCounters_V2', N'U') IS NOT NULL
BEGIN
    SELECT TOP (100)
        SourceTable,
        BranchID,
        TypeCode,
        PrefixKey = ISNULL(Prefix, ''),
        StoreKey = ISNULL(StoreID, 0),
        YearNum,
        MonthNum,
        CounterRows = COUNT(*),
        MinCounterID = MIN(CounterID),
        MaxCounterID = MAX(CounterID),
        MinTail = MIN(CurrentTail),
        MaxTail = MAX(CurrentTail)
    FROM dbo.SerialCounters_V2 WITH (READCOMMITTEDLOCK)
    GROUP BY SourceTable, BranchID, TypeCode, ISNULL(Prefix, ''), ISNULL(StoreID, 0), YearNum, MonthNum
    HAVING COUNT(*) > 1
    ORDER BY CounterRows DESC, SourceTable, BranchID, TypeCode;
END

PRINT '09. Current POS counter pressure';
IF OBJECT_ID(N'dbo.SerialCounters_V2', N'U') IS NOT NULL
BEGIN
    SELECT TOP (100)
        ConfiguredPOSVoucherSerialScope = @ConfiguredPOSVoucherSerialScope,
        SourceTable,
        BranchID,
        TypeCode,
        Prefix,
        StoreID,
        YearNum,
        MonthNum,
        CurrentTail,
        UpdateCount,
        LastUpdated,
        UpdatedByUser
    FROM dbo.SerialCounters_V2 WITH (READCOMMITTEDLOCK)
    WHERE SourceTable = 'Transactions'
      AND TypeCode IN (19, 21)
    ORDER BY LastUpdated DESC, UpdateCount DESC;
END

PRINT '10. Recent POS serial pressure from Transactions';
IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
BEGIN
    SELECT TOP (100)
        BranchId,
        StoreID,
        Transaction_Type,
        TxRows = COUNT(*),
        DistinctSerials = COUNT(DISTINCT NoteSerial1),
        MinDate = MIN(Transaction_Date),
        MaxDate = MAX(Transaction_Date)
    FROM dbo.Transactions WITH (READCOMMITTEDLOCK)
    WHERE Transaction_Type IN (19, 21)
      AND Transaction_Date >= @FromDate
      AND Transaction_Date < DATEADD(DAY, 1, @ToDate)
    GROUP BY BranchId, StoreID, Transaction_Type
    ORDER BY COUNT(*) DESC;
END

PRINT '11. POS duplicate NoteSerial1 by branch/store/type/month';
IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
BEGIN
    SELECT TOP (100)
        BranchId,
        StoreID,
        Transaction_Type,
        YearNum = YEAR(Transaction_Date),
        MonthNum = MONTH(Transaction_Date),
        NoteSerial1,
        DuplicateCount = COUNT(*)
    FROM dbo.Transactions WITH (READCOMMITTEDLOCK)
    WHERE Transaction_Type IN (19, 21)
      AND Transaction_Date >= DATEADD(MONTH, -6, @ToDate)
      AND NoteSerial1 IS NOT NULL
    GROUP BY BranchId, StoreID, Transaction_Type, YEAR(Transaction_Date), MONTH(Transaction_Date), NoteSerial1
    HAVING COUNT(*) > 1
    ORDER BY DuplicateCount DESC, YearNum DESC, MonthNum DESC;
END

PRINT '12. Voucher coding stage timing p95/p99 if POS_SaveAllocationStageLog exists';
IF OBJECT_ID(N'dbo.POS_SaveAllocationStageLog', N'U') IS NOT NULL
BEGIN
    ;WITH s AS
    (
        SELECT
            StageName,
            DurationMs,
            rn = ROW_NUMBER() OVER (PARTITION BY StageName ORDER BY DurationMs),
            cnt = COUNT(*) OVER (PARTITION BY StageName)
        FROM dbo.POS_SaveAllocationStageLog WITH (READCOMMITTEDLOCK)
        WHERE CreatedAt >= @FromDate
          AND StageName IN (N'Invoice voucher coding allocation', N'Issue voucher coding allocation')
    )
    SELECT
        StageName,
        Samples = MAX(cnt),
        AvgMs = AVG(CONVERT(BIGINT, DurationMs)),
        MaxMs = MAX(DurationMs),
        P95Ms = MAX(CASE WHEN rn >= CEILING(cnt * 0.95) THEN DurationMs END),
        P99Ms = MAX(CASE WHEN rn >= CEILING(cnt * 0.99) THEN DurationMs END)
    FROM s
    GROUP BY StageName
    ORDER BY StageName;
END

PRINT '13. Live requests and waits touching voucher coding objects';
SELECT
    r.session_id,
    r.status,
    r.command,
    r.cpu_time,
    r.total_elapsed_time,
    r.wait_type,
    r.wait_time,
    r.wait_resource,
    r.blocking_session_id,
    DBName = DB_NAME(r.database_id),
    SqlText = SUBSTRING(st.text, (r.statement_start_offset / 2) + 1,
        ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(st.text) ELSE r.statement_end_offset END - r.statement_start_offset) / 2) + 1)
FROM sys.dm_exec_requests r
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) st
WHERE r.database_id = DB_ID()
  AND (
        st.text LIKE '%usp_Voucher_coding_V2%'
     OR st.text LIKE '%usp_GetNextSerial_V2%'
     OR st.text LIKE '%SerialCounters_V2%'
     OR r.wait_resource LIKE '%SerialCounters%'
  )
ORDER BY r.total_elapsed_time DESC;

SELECT
    wt.session_id,
    wt.wait_type,
    wt.wait_duration_ms,
    wt.blocking_session_id,
    wt.resource_description,
    DBName = DB_NAME(r.database_id),
    r.command,
    r.status,
    r.wait_resource
FROM sys.dm_os_waiting_tasks wt
LEFT JOIN sys.dm_exec_requests r ON wt.session_id = r.session_id
WHERE r.database_id = DB_ID()
  AND (
        wt.wait_type LIKE 'LCK[_]M[_]%'
     OR wt.wait_type IN ('PAGELATCH_EX', 'PAGELATCH_SH', 'PAGEIOLATCH_EX', 'PAGEIOLATCH_SH', 'WRITELOG')
  )
ORDER BY wt.wait_duration_ms DESC;

PRINT '14. Current locks on SerialCounters_V2';
IF OBJECT_ID(N'dbo.SerialCounters_V2', N'U') IS NOT NULL
BEGIN
    SELECT
        l.request_session_id,
        l.resource_type,
        l.request_mode,
        l.request_status,
        l.resource_description,
        ObjectName = OBJECT_NAME(p.object_id),
        IndexName = i.name
    FROM sys.dm_tran_locks l
    LEFT JOIN sys.partitions p ON l.resource_associated_entity_id = p.hobt_id
    LEFT JOIN sys.indexes i ON p.object_id = i.object_id AND p.index_id = i.index_id
    WHERE l.resource_database_id = DB_ID()
      AND (p.object_id = OBJECT_ID(N'dbo.SerialCounters_V2') OR OBJECT_NAME(p.object_id) = N'SerialCounters_V2')
    ORDER BY l.request_session_id, l.resource_type, l.request_mode;
END

PRINT '15. Recent system_health deadlocks mentioning voucher coding';
BEGIN TRY
    ;WITH deadlocks AS
    (
        SELECT
            event_time = xed.value(N'(@timestamp)[1]', N'datetime2'),
            deadlock_xml = xed.query(N'(data/value/deadlock)[1]')
        FROM
        (
            SELECT CAST(target_data AS XML) AS target_data
            FROM sys.dm_xe_session_targets st
            JOIN sys.dm_xe_sessions s ON s.address = st.event_session_address
            WHERE s.name = N'system_health'
              AND st.target_name = N'ring_buffer'
        ) rb
        CROSS APPLY rb.target_data.nodes(N'RingBufferTarget/event[@name="xml_deadlock_report"]') AS tab(xed)
    )
    SELECT TOP (20)
        event_time,
        contains_voucher_coding = CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%usp_Voucher_coding_V2%' THEN 1 ELSE 0 END,
        contains_get_next_serial = CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%usp_GetNextSerial_V2%' THEN 1 ELSE 0 END,
        contains_serial_counters = CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%SerialCounters_V2%' THEN 1 ELSE 0 END,
        contains_transactions = CASE WHEN CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%Transactions%' THEN 1 ELSE 0 END,
        deadlock_xml
    FROM deadlocks
    WHERE CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%usp_Voucher_coding_V2%'
       OR CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%usp_GetNextSerial_V2%'
       OR CONVERT(NVARCHAR(MAX), deadlock_xml) LIKE N'%SerialCounters_V2%'
    ORDER BY event_time DESC;
END TRY
BEGIN CATCH
    SELECT DeadlockReadError = ERROR_MESSAGE();
END CATCH;
