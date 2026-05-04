/*
    Optional SQL Server memory cap for the POS deployment.
    SQL Server 2012 compatible.

    Use this only after confirming the server RAM and whether IIS and SQL Server
    are hosted on the same machine.

    Recommended starting points from the 2026-05-04 local Cash test:
    - IIS + SQL Server on the same 34 GB server: 24576 MB
      Leaves roughly 8-10 GB for Windows, IIS, antivirus, backups, and file cache.
    - Dedicated SQL Server with about 34 GB RAM: 28672 MB
      Leaves roughly 5-6 GB for Windows and SQL OS overhead.

    Do not change MAXDOP or cost threshold in this package. The load test did
    not capture clean before/after wait-stat deltas that justify those changes.
*/

EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO

/*
    Default recommended value when IIS and SQL Server share the same 34 GB host.
    Change to 28672 only if SQL Server is dedicated and the OS has enough memory.
*/
EXEC sp_configure 'max server memory (MB)', 24576;
RECONFIGURE;
GO

/*
    Verification:
    SELECT name, value_in_use
    FROM sys.configurations
    WHERE name IN ('max server memory (MB)', 'max degree of parallelism', 'cost threshold for parallelism');
*/
