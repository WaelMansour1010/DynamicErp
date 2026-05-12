/*
    Verify the POS application SQL login/user after deploying Web.Byte.Production.Tuned.config.
    Run as sysadmin.
*/

USE [master];
GO

SELECT
    name AS LoginName,
    is_disabled,
    default_database_name
FROM sys.server_principals
WHERE name = N'cayshny_pos_app';
GO

USE [Byte];
GO

SELECT
    dp.name AS DatabaseUser,
    dp.type_desc,
    dp.default_schema_name
FROM sys.database_principals dp
WHERE dp.name = N'cayshny_pos_app';
GO

SELECT
    USER_NAME(drm.role_principal_id) AS RoleName
FROM sys.database_role_members drm
WHERE drm.member_principal_id = USER_ID(N'cayshny_pos_app');
GO

SELECT
    HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'EXECUTE') AS HasExecuteOnDboForCurrentLogin;
GO

EXECUTE AS LOGIN = N'cayshny_pos_app';
GO

USE [Byte];
GO

SELECT TOP (1)
    N'TblUsers read OK' AS CheckName
FROM dbo.TblUsers;
GO

SELECT
    HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'EXECUTE') AS AppLoginHasExecuteOnDbo,
    HAS_PERMS_BY_NAME(N'dbo.TblUsers', N'OBJECT', N'SELECT') AS AppLoginCanReadTblUsers;
GO

REVERT;
GO
