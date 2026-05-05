/*
    Verification script after applying tuning.
    Read-only except DBCC SQLPERF(LOGSPACE), which only reports usage.
*/

USE [master];
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

SELECT
    physical_memory_in_use_kb / 1024 AS SqlMemoryInUseMB,
    locked_page_allocations_kb / 1024 AS LockedPagesMB,
    total_virtual_address_space_kb / 1024 AS TotalVirtualAddressSpaceMB,
    process_physical_memory_low,
    process_virtual_memory_low
FROM sys.dm_os_process_memory;
GO

DBCC SQLPERF(LOGSPACE);
GO

