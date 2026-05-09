/*
    POS Deadlock Diagnostics
    SQL Server 2012 compatible.

    Purpose:
    - Diagnose production POS save deadlocks without changing business logic.
    - Keep this script manual/admin only. Do not run it from application startup.

    Notes:
    - dbo.usp_POS_SaveTransaction is a long transaction by design because it saves
      Transactions, Transaction_Details, payments, Notes and DOUBLE_ENTREY_VOUCHERS
      as one atomic operation.
    - Application retry for SqlException 1205 is implemented in PosSqlRepository.
    - Use this script to capture the real deadlock graph before changing SQL locking
      order or accounting/voucher behavior.
*/

SET NOCOUNT ON;

DECLARE @FromDate DATETIME;
DECLARE @ToDate DATETIME;

SET @FromDate = DATEADD(DAY, -1, GETDATE());
SET @ToDate = GETDATE();

PRINT 'Recent POS deadlocks logged by the application';

IF OBJECT_ID(N'dbo.POS_SystemErrorLog', N'U') IS NOT NULL
BEGIN
    SELECT TOP (100)
        CreatedAt,
        Severity,
        Status,
        UserId,
        UserName,
        BranchId,
        ScreenName,
        ActionName,
        OperationType,
        TransactionId,
        ErrorMessage,
        RequestSummary,
        IpAddress
    FROM dbo.POS_SystemErrorLog
    WHERE CreatedAt >= @FromDate
      AND CreatedAt <= @ToDate
      AND (
            ErrorMessage LIKE N'%deadlock%'
         OR ErrorMessage LIKE N'%deadlocked%'
         OR StackTrace LIKE N'%deadlock%'
         OR StackTrace LIKE N'%deadlocked%'
      )
    ORDER BY CreatedAt DESC;
END
ELSE
BEGIN
    PRINT 'dbo.POS_SystemErrorLog does not exist in this database.';
END

PRINT 'Recent POS deadlock retry logs are written to App_Data/Logs/pos-deadlock-retry-yyyyMMdd.log';

/*
    Optional DBA commands:

    1) Enable SQL Server deadlock graph logging globally.
       Run as sysadmin only during an investigation window:

       DBCC TRACEON (1222, -1);

    2) Disable after investigation:

       DBCC TRACEOFF (1222, -1);

    3) Read recent SQL error log deadlock entries.
       Requires permission to execute xp_readerrorlog:
*/

-- EXEC master.dbo.xp_readerrorlog 0, 1, N'deadlock', NULL, @FromDate, @ToDate, N'desc';
-- EXEC master.dbo.xp_readerrorlog 0, 1, N'victim', NULL, @FromDate, @ToDate, N'desc';

/*
    Current lock/blocking snapshot.
    Run while the issue is happening.
*/

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
    SUBSTRING(t.text, (r.statement_start_offset / 2) + 1,
        ((CASE r.statement_end_offset
            WHEN -1 THEN DATALENGTH(t.text)
            ELSE r.statement_end_offset
          END - r.statement_start_offset) / 2) + 1) AS running_statement
FROM sys.dm_exec_requests AS r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) AS t
WHERE r.database_id = DB_ID()
  AND r.session_id <> @@SPID
ORDER BY r.total_elapsed_time DESC;

