/*
    POS / Keshni SQL Server tuning for a 64GB server.

    Run in SQL Server Management Studio as sysadmin.
    Safe target when SQL Server and IIS are on the same server:
      - max server memory = 49152 MB (48GB)
      - leaves around 16GB for Windows, IIS, file cache, antivirus, and drivers.

    If SQL Server is on a dedicated DB-only server, review README before raising
    max server memory above this value.
*/

USE [master];
GO

EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO

EXEC sp_configure 'min server memory (MB)', 8192;
RECONFIGURE;
GO

EXEC sp_configure 'max server memory (MB)', 49152;
RECONFIGURE;
GO

EXEC sp_configure 'optimize for ad hoc workloads', 1;
RECONFIGURE;
GO

SELECT
    name,
    value_in_use
FROM sys.configurations
WHERE name IN (
    'min server memory (MB)',
    'max server memory (MB)',
    'optimize for ad hoc workloads',
    'max degree of parallelism',
    'cost threshold for parallelism'
)
ORDER BY name;
GO

