/*
    POS System Health permissions
    SQL Server 2012 compatible.

    Purpose:
    - The System Health Dashboard reads SQL Server DMVs:
      sys.dm_exec_requests
      sys.dm_exec_sql_text
      sys.dm_os_performance_counters
    - These require VIEW SERVER STATE.

    Run this script as a sysadmin/master-level login.
    Replace @AppLoginName with the SQL login used by the POS application connection string.

    How to find the login:
    - Check Web.config / deployed Web.config connection string used by KishnyCashConnection.
    - If Integrated Security is used, grant to the IIS AppPool Windows identity or service account.
*/

USE [master];
GO

DECLARE @AppLoginName SYSNAME;
SET @AppLoginName = N'REPLACE_WITH_POS_APP_SQL_LOGIN';

IF @AppLoginName = N'REPLACE_WITH_POS_APP_SQL_LOGIN'
BEGIN
    RAISERROR(N'Please replace @AppLoginName with the POS application SQL login before running this script.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @AppLoginName)
BEGIN
    RAISERROR(N'The specified SQL login does not exist.', 16, 1);
    RETURN;
END;

DECLARE @sql NVARCHAR(MAX);
SET @sql = N'GRANT VIEW SERVER STATE TO ' + QUOTENAME(@AppLoginName) + N';';
EXEC sp_executesql @sql;

SELECT
    @AppLoginName AS AppLoginName,
    HAS_PERMS_BY_NAME(NULL, NULL, 'VIEW SERVER STATE') AS CurrentRunnerHasViewServerState,
    N'Granted VIEW SERVER STATE. Verify from the application login by opening the System Health Dashboard.' AS ResultMessage;
GO
