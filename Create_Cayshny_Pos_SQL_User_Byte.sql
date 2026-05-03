USE [master];
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'cayshny_pos_app')
BEGIN
    CREATE LOGIN [cayshny_pos_app]
    WITH PASSWORD = N'Cayshny#Pos@2026!B7x$9Qm2',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF,
         DEFAULT_DATABASE = [Byte];
END
ELSE
BEGIN
    ALTER LOGIN [cayshny_pos_app]
    WITH PASSWORD = N'Cayshny#Pos@2026!B7x$9Qm2' UNLOCK,
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF,
         DEFAULT_DATABASE = [Byte];
END
GO

USE [Byte];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'cayshny_pos_app')
BEGIN
    CREATE USER [cayshny_pos_app] FOR LOGIN [cayshny_pos_app];
END
ELSE
BEGIN
    ALTER USER [cayshny_pos_app] WITH LOGIN = [cayshny_pos_app];
END
GO

EXEC sp_addrolemember N'db_datareader', N'cayshny_pos_app';
EXEC sp_addrolemember N'db_datawriter', N'cayshny_pos_app';
GRANT EXECUTE ON SCHEMA::[dbo] TO [cayshny_pos_app];
GO

-- Quick verification after execution
SELECT
    name AS LoginName,
    is_disabled,
    default_database_name
FROM sys.server_principals
WHERE name = N'cayshny_pos_app';

USE [Byte];
SELECT
    DP.name AS DatabaseUser,
    DP.type_desc
FROM sys.database_principals DP
WHERE DP.name = N'cayshny_pos_app';
GO