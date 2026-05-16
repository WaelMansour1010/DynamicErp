param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,

    [int[]]$UserLevels = @(10, 25, 50, 100),

    [int]$IterationsPerUser = 20,

    [string[]]$Modes = @('cash', 'card'),

    [int]$CommandTimeoutSeconds = 90,

    [int]$WaitSampleIntervalMs = 200,

    [string]$SqlSetupPath = $null
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($SqlSetupPath)) {
    $SqlSetupPath = Join-Path $PSScriptRoot '..\Sql\MANUAL_90_POS_DoubleEntryVoucher_Diagnostics.sql'
}

function Invoke-SqlNonQuery {
    param([string]$Sql, [hashtable]$Parameters = @{})

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = $CommandTimeoutSeconds
        $command.CommandText = $Sql
        foreach ($key in $Parameters.Keys) {
            [void]$command.Parameters.AddWithValue($key, $Parameters[$key])
        }
        [void]$command.ExecuteNonQuery()
    }
    finally {
        $connection.Dispose()
    }
}

function Invoke-SqlDataTable {
    param([string]$Sql, [hashtable]$Parameters = @{})

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = $CommandTimeoutSeconds
        $command.CommandText = $Sql
        foreach ($key in $Parameters.Keys) {
            [void]$command.Parameters.AddWithValue($key, $Parameters[$key])
        }
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
        $table = New-Object System.Data.DataTable
        [void]$adapter.Fill($table)
        return ,$table
    }
    finally {
        $connection.Dispose()
    }
}

function Invoke-SqlScriptFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "SQL setup file not found: $Path"
    }

    $script = Get-Content -LiteralPath $Path -Raw
    $batches = [System.Text.RegularExpressions.Regex]::Split($script, '^\s*GO\s*$', [System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    foreach ($batch in $batches) {
        if (-not [string]::IsNullOrWhiteSpace($batch)) {
            Invoke-SqlNonQuery -Sql $batch
        }
    }
}

Write-Host "Installing DOUBLE_ENTREY_VOUCHERS diagnostics/benchmark helpers..."
Invoke-SqlScriptFile -Path $SqlSetupPath

$workerScript = {
    param(
        [string]$ConnectionString,
        [Guid]$RunId,
        [int]$WorkerId,
        [string]$Mode,
        [int]$Iterations,
        [int]$CommandTimeoutSeconds
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = $CommandTimeoutSeconds
        $command.CommandType = [System.Data.CommandType]::StoredProcedure
        $command.CommandText = 'dbo.usp_POS_DoubleEntryVoucherBenchmarkWorker'
        [void]$command.Parameters.AddWithValue('@RunId', $RunId)
        [void]$command.Parameters.AddWithValue('@WorkerId', $WorkerId)
        [void]$command.Parameters.AddWithValue('@Mode', $Mode)
        [void]$command.Parameters.AddWithValue('@Iterations', $Iterations)
        [void]$command.ExecuteNonQuery()
    }
    finally {
        $connection.Dispose()
    }
}

$monitorScript = {
    param(
        [string]$ConnectionString,
        [Guid]$RunId,
        [int]$IntervalMs,
        [int]$CommandTimeoutSeconds
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        while ($true) {
            $command = $connection.CreateCommand()
            $command.CommandTimeout = $CommandTimeoutSeconds
            $command.CommandType = [System.Data.CommandType]::StoredProcedure
            $command.CommandText = 'dbo.usp_POS_DoubleEntryBenchmarkCaptureWaits'
            [void]$command.Parameters.AddWithValue('@RunId', $RunId)
            try {
                [void]$command.ExecuteNonQuery()
            }
            catch {
            }
            Start-Sleep -Milliseconds $IntervalMs
        }
    }
    finally {
        $connection.Dispose()
    }
}

$summarySql = @"
;WITH Ordered AS
(
    SELECT
        r.*,
        ROW_NUMBER() OVER (PARTITION BY RunId, Mode ORDER BY InsertDurationMs) AS rn,
        COUNT(*) OVER (PARTITION BY RunId, Mode) AS cnt
    FROM dbo.POS_DoubleEntryBenchmarkResult AS r
    WHERE RunId = @RunId
)
SELECT
    Mode,
    COUNT(*) AS total_attempts,
    SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) AS success_count,
    SUM(CASE WHEN ErrorNumber = 1205 THEN 1 ELSE 0 END) AS deadlock_count,
    SUM(CASE WHEN ErrorNumber = -2 THEN 1 ELSE 0 END) AS timeout_count,
    AVG(InsertDurationMs) AS avg_insert_ms,
    MAX(InsertDurationMs) AS max_insert_ms,
    MIN(CASE WHEN rn >= CEILING(cnt * 0.95) THEN InsertDurationMs END) AS p95_insert_ms,
    MIN(CASE WHEN rn >= CEILING(cnt * 0.99) THEN InsertDurationMs END) AS p99_insert_ms,
    AVG(TotalDurationMs) AS avg_total_ms,
    MAX(TotalDurationMs) AS max_total_ms
FROM Ordered
GROUP BY Mode
"@

$waitSql = @"
SELECT TOP (30)
    wait_type,
    object_name,
    index_name,
    request_mode,
    request_status,
    resource_description,
    COUNT(*) AS samples,
    MAX(wait_duration_ms) AS max_wait_ms
FROM dbo.POS_DoubleEntryBenchmarkWaitSample
WHERE RunId = @RunId
GROUP BY wait_type, object_name, index_name, request_mode, request_status, resource_description
ORDER BY samples DESC, max_wait_ms DESC
"@

$logDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'Logs'
if (-not (Test-Path -LiteralPath $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
}

$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$csvPath = Join-Path $logDir "DoubleEntryVoucherBenchmark_$stamp.csv"
$allRows = @()

foreach ($mode in $Modes) {
    foreach ($level in $UserLevels) {
        $runId = [Guid]::NewGuid()
        $label = "$mode-$level-users-$IterationsPerUser-iterations"

        Write-Host ""
        Write-Host "Starting $label RunId=$runId"

        Invoke-SqlNonQuery -Sql @"
INSERT INTO dbo.POS_DoubleEntryBenchmarkRun
(
    RunId, Label, Mode, WorkerCount, IterationsPerWorker, StartedAt
)
VALUES
(
    @RunId, @Label, @Mode, @WorkerCount, @IterationsPerWorker, GETDATE()
)
"@ -Parameters @{
            '@RunId' = $runId
            '@Label' = $label
            '@Mode' = $mode
            '@WorkerCount' = $level
            '@IterationsPerWorker' = $IterationsPerUser
        }

        $monitor = Start-Job -ScriptBlock $monitorScript -ArgumentList $ConnectionString, $runId, $WaitSampleIntervalMs, $CommandTimeoutSeconds
        $jobs = @()

        for ($i = 1; $i -le $level; $i++) {
            $jobs += Start-Job -ScriptBlock $workerScript -ArgumentList $ConnectionString, $runId, $i, $mode, $IterationsPerUser, $CommandTimeoutSeconds
        }

        Wait-Job -Job $jobs | Out-Null
        foreach ($job in $jobs) {
            Receive-Job -Job $job -ErrorAction SilentlyContinue | Out-Null
            Remove-Job -Job $job -Force
        }

        Stop-Job -Job $monitor -ErrorAction SilentlyContinue
        Remove-Job -Job $monitor -Force -ErrorAction SilentlyContinue

        Invoke-SqlNonQuery -Sql "UPDATE dbo.POS_DoubleEntryBenchmarkRun SET FinishedAt = GETDATE() WHERE RunId = @RunId" -Parameters @{ '@RunId' = $runId }

        $summary = Invoke-SqlDataTable -Sql $summarySql -Parameters @{ '@RunId' = $runId }
        $waits = Invoke-SqlDataTable -Sql $waitSql -Parameters @{ '@RunId' = $runId }

        Write-Host "Summary:"
        $summary | Format-Table -AutoSize | Out-String | Write-Host

        Write-Host "Top waits:"
        $waits | Format-Table -AutoSize | Out-String | Write-Host

        foreach ($row in $summary.Rows) {
            $allRows += [pscustomobject]@{
                RunId = $runId
                Label = $label
                Mode = $row.Mode
                Workers = $level
                IterationsPerWorker = $IterationsPerUser
                TotalAttempts = $row.total_attempts
                SuccessCount = $row.success_count
                DeadlockCount = $row.deadlock_count
                TimeoutCount = $row.timeout_count
                AvgInsertMs = $row.avg_insert_ms
                MaxInsertMs = $row.max_insert_ms
                P95InsertMs = $row.p95_insert_ms
                P99InsertMs = $row.p99_insert_ms
                AvgTotalMs = $row.avg_total_ms
                MaxTotalMs = $row.max_total_ms
            }
        }
    }
}

$allRows | Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding UTF8
Write-Host ""
Write-Host "Benchmark CSV: $csvPath"
