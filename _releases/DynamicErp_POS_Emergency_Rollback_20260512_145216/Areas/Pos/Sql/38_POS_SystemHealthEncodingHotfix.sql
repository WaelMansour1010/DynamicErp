/*
    POS System Health Arabic text hotfix
    SQL Server 2012 compatible.

    Purpose:
    Re-create dbo.usp_POS_SystemHealth_Database with proper Unicode Arabic text
    for the server-state permission fallback message.

    Safe to run on production after backup. This script does not change tables,
    indexes, sales logic, accounting logic, or permissions.
*/

IF OBJECT_ID(N'dbo.usp_POS_SystemHealth_Database', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SystemHealth_Database;
GO

CREATE PROCEDURE dbo.usp_POS_SystemHealth_Database
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @hasViewServerState BIT;
    DECLARE @statusMessage NVARCHAR(400);
    SET @hasViewServerState = CASE WHEN HAS_PERMS_BY_NAME(NULL, NULL, 'VIEW SERVER STATE') = 1 THEN 1 ELSE 0 END;
    SET @statusMessage = N'';

    IF @hasViewServerState = 1
    BEGIN
        SELECT TOP (5)
            r.session_id,
            r.total_elapsed_time,
            r.command,
            ISNULL(OBJECT_NAME(st.objectid, st.dbid), N'') AS ProcedureName,
            ISNULL(r.wait_type, N'') AS WaitType
        FROM sys.dm_exec_requests r
        CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) st
        WHERE r.session_id <> @@SPID
          AND (r.database_id = DB_ID() OR r.database_id = 0)
        ORDER BY r.total_elapsed_time DESC;

        SELECT TOP (10)
            r.session_id,
            r.blocking_session_id,
            ISNULL(r.wait_type, N'') AS WaitType,
            r.wait_time,
            r.total_elapsed_time
        FROM sys.dm_exec_requests r
        WHERE r.blocking_session_id <> 0
        ORDER BY r.wait_time DESC;

        SELECT ISNULL((
            SELECT TOP (1) cntr_value
            FROM sys.dm_os_performance_counters
            WHERE counter_name = N'Number of Deadlocks/sec'
              AND (instance_name = N'_Total' OR instance_name = DB_NAME())
            ORDER BY CASE WHEN instance_name = N'_Total' THEN 0 ELSE 1 END
        ), 0) AS DeadlockCounter;
    END
    ELSE
    BEGIN
        SET @statusMessage = N'لا توجد صلاحية كافية لقراءة مؤشرات الخادم. يتطلب هذا الجزء صلاحية VIEW SERVER STATE.';

        SELECT TOP (0)
            CAST(0 AS INT) AS session_id,
            CAST(0 AS INT) AS total_elapsed_time,
            CAST(N'' AS NVARCHAR(60)) AS command,
            CAST(N'' AS NVARCHAR(256)) AS ProcedureName,
            CAST(N'' AS NVARCHAR(120)) AS WaitType;

        SELECT TOP (0)
            CAST(0 AS INT) AS session_id,
            CAST(0 AS INT) AS blocking_session_id,
            CAST(N'' AS NVARCHAR(120)) AS WaitType,
            CAST(0 AS INT) AS wait_time,
            CAST(0 AS INT) AS total_elapsed_time;

        SELECT CAST(0 AS BIGINT) AS DeadlockCounter;
    END

    SELECT COUNT(1) AS TransactionsPerMinute
    FROM dbo.Transactions
    WHERE Transaction_Type = 21
      AND Transaction_Date >= DATEADD(MINUTE, -1, GETDATE());

    SELECT @statusMessage AS StatusMessage;
END;
GO
