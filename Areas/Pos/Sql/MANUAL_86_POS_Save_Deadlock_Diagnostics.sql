/*
    Kishny POS save deadlock diagnostics.
    Read-only investigation script. SQL Server 2012 compatible.

    Usage:
    1. Set @FromDate/@ToDate/@BranchId.
    2. Run during or shortly after peak.
    3. Save all result grids plus XML deadlock graphs.
*/

SET NOCOUNT ON;

DECLARE @FromDate DATETIME = DATEADD(HOUR, -6, GETDATE());
DECLARE @ToDate DATETIME = GETDATE();
DECLARE @BranchId INT = NULL;

SELECT
    DiagnosticWindowFrom = @FromDate,
    DiagnosticWindowTo = @ToDate,
    FilterBranchId = @BranchId;

IF OBJECT_ID(N'dbo.POS_SaveAttemptLog', N'U') IS NOT NULL
BEGIN
    SELECT
        TotalEvents = COUNT(1),
        DistinctAttempts = COUNT(DISTINCT SaveAttemptId),
        DeadlockEvents = SUM(CASE WHEN EventName = N'Save.Retry.Deadlock' OR SqlErrorNumber = 1205 THEN 1 ELSE 0 END),
        TimeoutEvents = SUM(CASE WHEN SqlErrorNumber = -2 OR Message LIKE N'%timeout%' OR Message LIKE N'%مهلة%' THEN 1 ELSE 0 END),
        FailedEvents = SUM(CASE WHEN Status IN (N'Failed', N'RetriedFailed') THEN 1 ELSE 0 END),
        AvgDurationMs = AVG(CONVERT(DECIMAL(18, 2), ISNULL(DurationMs, 0))),
        MaxDurationMs = MAX(DurationMs),
        AvgDelayMs = AVG(CONVERT(DECIMAL(18, 2), ISNULL(DelayMs, 0))),
        MaxDelayMs = MAX(DelayMs)
    FROM dbo.POS_SaveAttemptLog WITH (NOLOCK)
    WHERE CreatedAt >= @FromDate
      AND CreatedAt <= @ToDate
      AND (@BranchId IS NULL OR BranchId = @BranchId);

    SELECT TOP (50)
        BranchId,
        StoreID,
        BoxID,
        TransactionType,
        PaymentType,
        ItemIDService,
        ItemIDService2,
        OperationFingerprint,
        Attempts = COUNT(DISTINCT SaveAttemptId),
        DeadlockEvents = SUM(CASE WHEN EventName = N'Save.Retry.Deadlock' OR SqlErrorNumber = 1205 THEN 1 ELSE 0 END),
        TimeoutEvents = SUM(CASE WHEN SqlErrorNumber = -2 OR Message LIKE N'%timeout%' OR Message LIKE N'%مهلة%' THEN 1 ELSE 0 END),
        FailedEvents = SUM(CASE WHEN Status IN (N'Failed', N'RetriedFailed') THEN 1 ELSE 0 END),
        MaxDurationMs = MAX(DurationMs),
        MaxDelayMs = MAX(DelayMs)
    FROM dbo.POS_SaveAttemptLog WITH (NOLOCK)
    WHERE CreatedAt >= @FromDate
      AND CreatedAt <= @ToDate
      AND (@BranchId IS NULL OR BranchId = @BranchId)
    GROUP BY BranchId, StoreID, BoxID, TransactionType, PaymentType, ItemIDService, ItemIDService2, OperationFingerprint
    ORDER BY DeadlockEvents DESC, TimeoutEvents DESC, FailedEvents DESC, MaxDurationMs DESC;

    SELECT TOP (200)
        CreatedAt,
        SaveAttemptId,
        EventName,
        UserID,
        EmpID,
        BranchId,
        StoreID,
        BoxID,
        TransactionType,
        RetryAttempt,
        SqlErrorNumber,
        DelayMs,
        DurationMs,
        Transaction_ID,
        Status,
        Message,
        RequestSummary
    FROM dbo.POS_SaveAttemptLog WITH (NOLOCK)
    WHERE CreatedAt >= @FromDate
      AND CreatedAt <= @ToDate
      AND (@BranchId IS NULL OR BranchId = @BranchId)
      AND (SqlErrorNumber IN (1205, -2) OR EventName LIKE N'%Retry%' OR Status IN (N'Failed', N'RetriedFailed'))
    ORDER BY CreatedAt DESC, Id DESC;
END
ELSE
BEGIN
    SELECT N'POS_SaveAttemptLog does not exist. Apply POS SQL 83/84 first.' AS Message;
END

IF OBJECT_ID(N'dbo.POS_SaveIdempotency', N'U') IS NOT NULL
BEGIN
    SELECT TOP (100)
        ClientRequestId,
        CreatedAt,
        LastSeenAt,
        CompletedAt,
        Status,
        UserID,
        BranchId,
        StoreID,
        BoxID,
        TransactionType,
        Transaction_ID,
        NoteSerial1,
        DurationMs,
        ErrorMessage
    FROM dbo.POS_SaveIdempotency WITH (NOLOCK)
    WHERE CreatedAt >= @FromDate
      AND CreatedAt <= @ToDate
      AND (@BranchId IS NULL OR BranchId = @BranchId)
    ORDER BY CreatedAt DESC;
END

SELECT
    r.session_id,
    r.blocking_session_id,
    r.status,
    r.command,
    r.wait_type,
    r.wait_time,
    r.wait_resource,
    r.cpu_time,
    r.total_elapsed_time,
    DB_NAME(r.database_id) AS database_name,
    s.host_name,
    s.program_name,
    s.login_name,
    SUBSTRING(t.text, (r.statement_start_offset / 2) + 1,
        ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(t.text) ELSE r.statement_end_offset END - r.statement_start_offset) / 2) + 1) AS running_statement,
    t.text AS batch_text
FROM sys.dm_exec_requests AS r
INNER JOIN sys.dm_exec_sessions AS s ON s.session_id = r.session_id
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) AS t
WHERE r.database_id = DB_ID()
  AND r.session_id <> @@SPID
ORDER BY r.blocking_session_id DESC, r.total_elapsed_time DESC;

SELECT TOP (50)
    wait_type,
    waiting_tasks_count,
    wait_time_ms,
    max_wait_time_ms,
    signal_wait_time_ms
FROM sys.dm_os_wait_stats
WHERE wait_type LIKE N'LCK%'
   OR wait_type IN (N'PAGEIOLATCH_SH', N'PAGEIOLATCH_EX', N'WRITELOG', N'SOS_SCHEDULER_YIELD', N'CXPACKET')
ORDER BY wait_time_ms DESC;

SELECT
    TableName = OBJECT_SCHEMA_NAME(i.object_id) + N'.' + OBJECT_NAME(i.object_id),
    i.name AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.dm_db_index_usage_stats AS s
INNER JOIN sys.indexes AS i ON i.object_id = s.object_id AND i.index_id = s.index_id
WHERE s.database_id = DB_ID()
  AND OBJECT_NAME(i.object_id) IN
  (
      N'Transactions', N'Transaction_Details', N'TblSalesPayment',
      N'Notes', N'DOUBLE_ENTREY_VOUCHERS', N'POS_SaveAttemptLog',
      N'POS_SaveIdempotency', N'POS_DEVSerialAllocator'
  )
ORDER BY TableName, s.user_updates DESC;

DECLARE @xelPath NVARCHAR(4000);

SELECT @xelPath = CAST(t.target_data AS XML).value(N'(EventFileTarget/File/@name)[1]', N'NVARCHAR(4000)')
FROM sys.dm_xe_session_targets t
INNER JOIN sys.dm_xe_sessions s ON s.address = t.event_session_address
WHERE s.name = N'system_health'
  AND t.target_name = N'event_file';

IF @xelPath IS NOT NULL
BEGIN
    SET @xelPath = REPLACE(@xelPath, N'.xel', N'*.xel');

    ;WITH DeadlockEvents AS
    (
        SELECT EventXml = CAST(event_data AS XML)
        FROM sys.fn_xe_file_target_read_file(@xelPath, NULL, NULL, NULL)
        WHERE object_name = N'xml_deadlock_report'
    )
    SELECT TOP (50)
        DeadlockTimeUtc = EventXml.value(N'(/event/@timestamp)[1]', N'DATETIME2'),
        VictimProcess = EventXml.value(N'(/event/data/value/deadlock/victim-list/victimProcess/@id)[1]', N'NVARCHAR(100)'),
        DeadlockXml = EventXml.query(N'/event/data/value/deadlock')
    FROM DeadlockEvents
    WHERE EventXml.value(N'(/event/@timestamp)[1]', N'DATETIME2') >= DATEADD(HOUR, -2, @FromDate)
    ORDER BY DeadlockTimeUtc DESC;
END
ELSE
BEGIN
    SELECT N'system_health event_file target not available or current login cannot read it.' AS Message;
END
