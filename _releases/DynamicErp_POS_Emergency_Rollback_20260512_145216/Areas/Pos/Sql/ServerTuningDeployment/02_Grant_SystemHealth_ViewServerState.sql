/*
    Grants the minimum server-level permission required by the POS System Health
    dashboard to read SQL Server DMV/server-state metrics.

    Run as sysadmin on master.
    Default app SQL login is the current POS app login used by Web.Byte.Production.Tuned.config.
*/

USE [master];
GO

DECLARE @LoginName SYSNAME;
SET @LoginName = N'cayshny_pos_app';

IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE name = @LoginName
)
BEGIN
    RAISERROR(N'Login was not found. Update @LoginName to match the Web.config SQL login.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.server_permissions p
    INNER JOIN sys.server_principals sp ON p.grantee_principal_id = sp.principal_id
    WHERE sp.name = @LoginName
      AND p.permission_name = N'VIEW SERVER STATE'
)
BEGIN
    DECLARE @GrantSql NVARCHAR(MAX);
    SET @GrantSql = N'GRANT VIEW SERVER STATE TO ' + QUOTENAME(@LoginName) + N';';
    EXEC (@GrantSql);
END
GO

SELECT
    sp.name AS LoginName,
    p.permission_name,
    p.state_desc
FROM sys.server_permissions p
INNER JOIN sys.server_principals sp ON p.grantee_principal_id = sp.principal_id
WHERE sp.name = N'cayshny_pos_app'
  AND p.permission_name = N'VIEW SERVER STATE';
GO

