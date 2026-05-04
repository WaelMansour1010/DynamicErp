param(
    [string]$WebConfigPath = "F:\Source Code\DynamicErp\Web.config",
    [string]$ConnectionStringName = "KishnyCashConnection",
    [string]$OutputDirectory = "F:\Source Code\DynamicErp\Areas\Pos\Tools\audit-output"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $WebConfigPath)) {
    throw "Web.config not found: $WebConfigPath"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

[xml]$config = Get-Content -LiteralPath $WebConfigPath
$connectionNode = $config.configuration.connectionStrings.add | Where-Object { $_.name -eq $ConnectionStringName } | Select-Object -First 1
if (-not $connectionNode) {
    throw "Connection string not found: $ConnectionStringName"
}

$connectionString = $connectionNode.connectionString
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($connectionString)
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$summaryPath = Join-Path $OutputDirectory "pos-performance-audit-$stamp.txt"

function Invoke-DataTable {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Name,
        [string]$Sql,
        [int]$TimeoutSeconds = 90
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = $TimeoutSeconds
    $table = New-Object System.Data.DataTable $Name
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    [void]$adapter.Fill($table)
    return ,$table
}

function Export-Table {
    param(
        [System.Data.DataTable]$Table,
        [string]$Name
    )

    $path = Join-Path $OutputDirectory "$Name-$stamp.csv"
    $Table | Export-Csv -Path $path -NoTypeInformation -Encoding UTF8
    return $path
}

$report = New-Object System.Collections.Generic.List[string]
$report.Add("POS Performance Audit")
$report.Add("GeneratedAt: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$report.Add("Server: $($builder.DataSource)")
$report.Add("Database: $($builder.InitialCatalog)")
$report.Add("UserID: $($builder.UserID)")
$report.Add("Pooling: $($builder.Pooling); MinPool=$($builder.MinPoolSize); MaxPool=$($builder.MaxPoolSize); ConnectTimeout=$($builder.ConnectTimeout)")
$report.Add("")

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

try {
    $queries = [ordered]@{
        "db-size" = @"
SELECT DB_NAME() AS DatabaseName,
       CAST(SUM(size)*8.0/1024 AS DECIMAL(18,2)) AS SizeMB
FROM sys.database_files;
"@
        "top-tables" = @"
SELECT TOP (20)
    s.name + '.' + t.name AS TableName,
    SUM(p.rows) AS RowsCount,
    CAST(SUM(a.total_pages)*8.0/1024 AS DECIMAL(18,2)) AS TotalMB,
    CAST(SUM(a.used_pages)*8.0/1024 AS DECIMAL(18,2)) AS UsedMB
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id=t.schema_id
JOIN sys.indexes i ON i.object_id=t.object_id
JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
JOIN sys.allocation_units a ON a.container_id=p.partition_id
WHERE i.index_id IN (0,1)
GROUP BY s.name,t.name
ORDER BY TotalMB DESC;
"@
        "pos-daily-volume" = @"
SELECT TOP (45)
    CONVERT(date, Transaction_Date) AS TranDate,
    COUNT(*) AS TransactionsCount,
    COUNT(DISTINCT UserID) AS ActiveUsers,
    COUNT(DISTINCT BranchId) AS ActiveBranches,
    SUM(ISNULL(Transaction_NetValue, ISNULL(PayedValue,0))) AS NetValue,
    SUM(ISNULL(NetValue,0)) AS Fees,
    SUM(ISNULL(Vat,0)) AS Vat
FROM dbo.Transactions WITH (NOLOCK)
WHERE Transaction_Type = 21
GROUP BY CONVERT(date, Transaction_Date)
ORDER BY TranDate DESC;
"@
        "pos-growth-ratio" = @"
SELECT
    (SELECT COUNT(*) FROM dbo.Transactions WITH (NOLOCK) WHERE Transaction_Type=21) AS PosTransactions,
    (SELECT COUNT(*) FROM dbo.Transaction_Details d WITH (NOLOCK) INNER JOIN dbo.Transactions t WITH (NOLOCK) ON t.Transaction_ID=d.Transaction_ID WHERE t.Transaction_Type=21) AS PosDetails,
    (SELECT COUNT(*) FROM dbo.Notes WITH (NOLOCK)) AS NotesCount,
    (SELECT COUNT(*) FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (NOLOCK)) AS JournalLines;
"@
        "index-scan-pressure" = @"
SELECT TOP (30)
    OBJECT_SCHEMA_NAME(i.object_id)+'.'+OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    ISNULL(us.user_seeks,0) AS Seeks,
    ISNULL(us.user_scans,0) AS Scans,
    ISNULL(us.user_lookups,0) AS Lookups,
    ISNULL(us.user_updates,0) AS Updates
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats us ON us.object_id=i.object_id AND us.index_id=i.index_id AND us.database_id=DB_ID()
WHERE OBJECTPROPERTY(i.object_id,'IsUserTable')=1
ORDER BY (ISNULL(us.user_scans,0)+ISNULL(us.user_lookups,0)) DESC;
"@
        "current-requests" = @"
SELECT TOP (30)
    session_id, blocking_session_id, wait_type, wait_time, total_elapsed_time, command, status
FROM sys.dm_exec_requests
WHERE session_id <> @@SPID
ORDER BY total_elapsed_time DESC;
"@
        "top-query-stats" = @"
SELECT TOP (10)
    DB_NAME(st.dbid) AS DatabaseName,
    OBJECT_NAME(st.objectid, st.dbid) AS ObjectName,
    qs.execution_count AS ExecutionCount,
    CAST(qs.total_elapsed_time/1000.0 AS DECIMAL(18,2)) AS TotalElapsedMs,
    CAST((qs.total_elapsed_time/NULLIF(qs.execution_count,0))/1000.0 AS DECIMAL(18,2)) AS AvgElapsedMs,
    CAST(qs.max_elapsed_time/1000.0 AS DECIMAL(18,2)) AS MaxElapsedMs,
    LEFT(REPLACE(REPLACE(SUBSTRING(st.text, (qs.statement_start_offset/2)+1, ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text) ELSE qs.statement_end_offset END - qs.statement_start_offset)/2)+1), CHAR(13),' '), CHAR(10),' '), 500) AS SqlText
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
WHERE st.dbid = DB_ID() OR st.dbid IS NULL
ORDER BY qs.total_elapsed_time DESC;
"@
        "missing-indexes" = @"
SELECT TOP (20)
    DB_NAME(mid.database_id) AS DatabaseName,
    OBJECT_SCHEMA_NAME(mid.object_id, mid.database_id)+'.'+OBJECT_NAME(mid.object_id, mid.database_id) AS TableName,
    migs.user_seeks,
    CAST(migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans) AS DECIMAL(18,2)) AS ImprovementScore,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns
FROM sys.dm_db_missing_index_group_stats migs
JOIN sys.dm_db_missing_index_groups mig ON mig.index_group_handle=migs.group_handle
JOIN sys.dm_db_missing_index_details mid ON mid.index_handle=mig.index_handle
WHERE mid.database_id=DB_ID()
ORDER BY ImprovementScore DESC;
"@
    }

    foreach ($entry in $queries.GetEnumerator()) {
        try {
            $table = Invoke-DataTable -Connection $connection -Name $entry.Key -Sql $entry.Value
            $path = Export-Table -Table $table -Name $entry.Key
            $report.Add("[$($entry.Key)] rows=$($table.Rows.Count) file=$path")
        }
        catch {
            $report.Add("[$($entry.Key)] ERROR: $($_.Exception.Message)")
        }
    }
}
finally {
    $connection.Close()
}

$report | Set-Content -Path $summaryPath -Encoding UTF8
Write-Host "Audit complete: $summaryPath"
