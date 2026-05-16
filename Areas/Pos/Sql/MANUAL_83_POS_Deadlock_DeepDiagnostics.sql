/*
    Manual POS deadlock deep diagnostics.
    SQL Server 2012 compatible where possible.
    Run manually as DBA/read-only investigation. Does not change data.
*/

SET NOCOUNT ON;

DECLARE @FromDate DATETIME = DATEADD(HOUR, -6, GETDATE());
DECLARE @ToDate DATETIME = GETDATE();
DECLARE @BranchId INT = NULL; -- مثال: 55 لمجمع مرور حلوان

EXEC dbo.usp_POS_SaveAttemptDeadlockDiagnostics
    @FromDate = @FromDate,
    @ToDate = @ToDate,
    @BranchId = @BranchId,
    @Top = 50;

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
        ((CASE r.statement_end_offset
            WHEN -1 THEN DATALENGTH(t.text)
            ELSE r.statement_end_offset
          END - r.statement_start_offset) / 2) + 1) AS running_statement,
    t.text AS batch_text
FROM sys.dm_exec_requests AS r
INNER JOIN sys.dm_exec_sessions AS s ON s.session_id = r.session_id
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) AS t
WHERE r.database_id = DB_ID()
  AND r.session_id <> @@SPID
ORDER BY r.total_elapsed_time DESC;

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
    SELECT TOP (20)
        DeadlockTime = EventXml.value(N'(/event/@timestamp)[1]', N'DATETIME2'),
        VictimProcess = EventXml.value(N'(/event/data/value/deadlock/victim-list/victimProcess/@id)[1]', N'NVARCHAR(100)'),
        DeadlockXml = EventXml.query(N'/event/data/value/deadlock')
    FROM DeadlockEvents
    WHERE EventXml.value(N'(/event/@timestamp)[1]', N'DATETIME2') >= @FromDate
    ORDER BY DeadlockTime DESC;
END
ELSE
BEGIN
    SELECT N'system_health event_file target not available or not accessible.' AS Message;
END;
