[CmdletBinding()]
param(
    [string]$ConnectionString,
    [string]$Server,
    [string]$Database,
    [ValidateSet('DryRun', 'Apply', 'ReportOnly')]
    [string]$Mode = 'DryRun',
    [string]$ModuleName,
    [string[]]$MigrationPath,
    [switch]$StopOnError,
    [string]$BatchNo,
    [string]$ReleaseNo,
    [int]$CommandTimeoutSeconds = 0,
    [switch]$NoTransaction
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = Split-Path -Parent $PSCommandPath
    while ($current -and -not (Test-Path -LiteralPath (Join-Path $current 'MyERP.sln'))) {
        $parent = Split-Path -Parent $current
        if ($parent -eq $current) { break }
        $current = $parent
    }
    if (-not $current -or -not (Test-Path -LiteralPath (Join-Path $current 'MyERP.sln'))) {
        throw 'Cannot locate DynamicErp repository root from the runner path.'
    }
    return $current
}

function New-ConnectionString {
    if ($ConnectionString) { return $ConnectionString }
    if (-not $Server -or -not $Database) {
        throw 'Pass either -ConnectionString or both -Server and -Database.'
    }
    return "Data Source=$Server;Initial Catalog=$Database;Integrated Security=True;MultipleActiveResultSets=False"
}

function Invoke-ScalarSql {
    param([System.Data.SqlClient.SqlConnection]$Connection, [string]$Sql)
    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = $CommandTimeoutSeconds
    try { return $cmd.ExecuteScalar() }
    finally { $cmd.Dispose() }
}

function Invoke-NonQuerySql {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql,
        [System.Data.SqlClient.SqlTransaction]$Transaction = $null
    )
    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = $CommandTimeoutSeconds
    if ($Transaction) { $cmd.Transaction = $Transaction }
    try { [void]$cmd.ExecuteNonQuery() }
    finally { $cmd.Dispose() }
}

function Split-SqlBatches {
    param([string]$Sql)
    $batches = New-Object System.Collections.Generic.List[string]
    $buffer = New-Object System.Text.StringBuilder
    $reader = New-Object System.IO.StringReader($Sql)
    try {
        while (($line = $reader.ReadLine()) -ne $null) {
            if ($line -match '^\s*GO\s*(?:--.*)?$') {
                $batch = $buffer.ToString()
                if (-not [string]::IsNullOrWhiteSpace($batch)) { $batches.Add($batch) }
                [void]$buffer.Clear()
            } else {
                [void]$buffer.AppendLine($line)
            }
        }
        $last = $buffer.ToString()
        if (-not [string]::IsNullOrWhiteSpace($last)) { $batches.Add($last) }
    }
    finally {
        $reader.Dispose()
    }
    return $batches
}

function Get-Sha256Hash {
    param([string]$Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $bytes = $sha.ComputeHash($stream)
        return ([System.BitConverter]::ToString($bytes) -replace '-', '').ToUpperInvariant()
    }
    finally {
        $stream.Dispose()
        $sha.Dispose()
    }
}

function Get-MigrationModule {
    param([string]$FullPath, [string]$RepoRoot)
    $relative = Get-DisplayPath -FullPath $FullPath -RepoRoot $RepoRoot
    if ($relative -match '\\POS\\|\\Pos\\|_POS_') { return 'POS' }
    if ($relative -match '\\MainErp\\|_MainErp_') { return 'MainErp' }
    if ($relative -match '\\Reports\\|_Reports_|DynamicReports') { return 'Reports' }
    if ($relative -match '\\Sync\\|_Sync_') { return 'Sync' }
    return 'Shared'
}

function Get-DisplayPath {
    param([string]$FullPath, [string]$RepoRoot)
    if ($FullPath.StartsWith($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $FullPath.Substring($RepoRoot.Length).TrimStart('\')
    }
    return $FullPath
}

function Get-MigrationNumber {
    param([string]$Name)
    if ($Name -match '^(\d+)') { return [int]$Matches[1] }
    return 2147483647
}

function Get-MigrationFiles {
    param([string]$RepoRoot)
    $paths = if ($MigrationPath -and $MigrationPath.Count -gt 0) {
        $MigrationPath
    } else {
        @(
            (Join-Path $RepoRoot 'Database\Migrations'),
            (Join-Path $RepoRoot 'Areas\Pos\Database\Migrations'),
            (Join-Path $RepoRoot 'Areas\MainErp\Database\Migrations'),
            (Join-Path $RepoRoot 'Areas\Reports\Database\Migrations'),
            (Join-Path $RepoRoot 'Areas\Sync\Database\Migrations')
        )
    }

    $items = New-Object System.Collections.Generic.List[object]
    foreach ($path in $paths) {
        $resolved = if ([System.IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $RepoRoot $path }
        if (-not (Test-Path -LiteralPath $resolved)) { continue }
        Get-ChildItem -LiteralPath $resolved -Filter '*.sql' -File -Recurse | ForEach-Object {
            $module = Get-MigrationModule -FullPath $_.FullName -RepoRoot $RepoRoot
            if ($ModuleName -and $module -ne $ModuleName) { return }
            $relative = Get-DisplayPath -FullPath $_.FullName -RepoRoot $RepoRoot
            $items.Add([pscustomobject]@{
                ScriptName = $_.Name
                ScriptPath = $relative
                FullPath = $_.FullName
                ScriptHash = Get-Sha256Hash -Path $_.FullName
                ModuleName = $module
                SortNo = Get-MigrationNumber -Name $_.Name
            })
        }
    }

    return $items | Sort-Object SortNo, ScriptName, ScriptPath
}

function Ensure-HistoryTable {
    param([System.Data.SqlClient.SqlConnection]$Connection, [string]$RepoRoot)
    $scriptPath = Join-Path $RepoRoot 'Database\Create_DatabaseMigrationHistory.sql'
    if (-not (Test-Path -LiteralPath $scriptPath)) {
        throw "History table script not found: $scriptPath"
    }
    $sql = [System.IO.File]::ReadAllText($scriptPath)
    foreach ($batch in (Split-SqlBatches -Sql $sql)) {
        Invoke-NonQuerySql -Connection $Connection -Sql $batch
    }
}

function Read-History {
    param([System.Data.SqlClient.SqlConnection]$Connection)
    $exists = Invoke-ScalarSql -Connection $Connection -Sql "SELECT CASE WHEN OBJECT_ID(N'dbo.DatabaseMigrationHistory', N'U') IS NULL THEN 0 ELSE 1 END"
    $history = @{}
    if ([int]$exists -eq 0) { return $history }

    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = "SELECT ScriptName, ScriptHash, Success, AppliedOn, ErrorMessage FROM dbo.DatabaseMigrationHistory"
    $cmd.CommandTimeout = $CommandTimeoutSeconds
    $reader = $cmd.ExecuteReader()
    try {
        while ($reader.Read()) {
            $name = [string]$reader['ScriptName']
            if (-not $history.ContainsKey($name)) {
                $history[$name] = @()
            }
            $history[$name] += [pscustomobject]@{
                ScriptHash = [string]$reader['ScriptHash']
                Success = [bool]$reader['Success']
                AppliedOn = $reader['AppliedOn']
                ErrorMessage = if ($reader['ErrorMessage'] -eq [DBNull]::Value) { $null } else { [string]$reader['ErrorMessage'] }
            }
        }
    }
    finally {
        $reader.Dispose()
        $cmd.Dispose()
    }
    return $history
}

function Write-HistoryRow {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [object]$Migration,
        [int]$DurationMs,
        [bool]$Success,
        [string]$ErrorMessage
    )
    $cmd = $Connection.CreateCommand()
    $cmd.CommandTimeout = $CommandTimeoutSeconds
    $cmd.CommandText = @"
INSERT INTO dbo.DatabaseMigrationHistory
(
    ScriptName, ScriptPath, ScriptHash, ModuleName, AppliedOn, AppliedBy,
    MachineName, DatabaseName, DurationMs, Success, ErrorMessage, BatchNo, ReleaseNo
)
VALUES
(
    @ScriptName, @ScriptPath, @ScriptHash, @ModuleName, GETDATE(), @AppliedBy,
    @MachineName, DB_NAME(), @DurationMs, @Success, @ErrorMessage, @BatchNo, @ReleaseNo
)
"@
    [void]$cmd.Parameters.AddWithValue('@ScriptName', $Migration.ScriptName)
    [void]$cmd.Parameters.AddWithValue('@ScriptPath', $Migration.ScriptPath)
    [void]$cmd.Parameters.AddWithValue('@ScriptHash', $Migration.ScriptHash)
    [void]$cmd.Parameters.AddWithValue('@ModuleName', $Migration.ModuleName)
    [void]$cmd.Parameters.AddWithValue('@AppliedBy', [System.Security.Principal.WindowsIdentity]::GetCurrent().Name)
    [void]$cmd.Parameters.AddWithValue('@MachineName', [System.Environment]::MachineName)
    [void]$cmd.Parameters.AddWithValue('@DurationMs', $DurationMs)
    [void]$cmd.Parameters.AddWithValue('@Success', $Success)
    [void]$cmd.Parameters.AddWithValue('@ErrorMessage', $(if ($ErrorMessage) { $ErrorMessage } else { [DBNull]::Value }))
    [void]$cmd.Parameters.AddWithValue('@BatchNo', $(if ($BatchNo) { $BatchNo } else { [DBNull]::Value }))
    [void]$cmd.Parameters.AddWithValue('@ReleaseNo', $(if ($ReleaseNo) { $ReleaseNo } else { [DBNull]::Value }))
    try { [void]$cmd.ExecuteNonQuery() }
    finally { $cmd.Dispose() }
}

function Get-MigrationState {
    param([object]$Migration, [hashtable]$History)
    if (-not $History.ContainsKey($Migration.ScriptName)) { return 'Pending' }
    $rows = @($History[$Migration.ScriptName])
    if ($rows | Where-Object { $_.Success -and $_.ScriptHash -eq $Migration.ScriptHash }) { return 'Skipped' }
    if ($rows | Where-Object { $_.Success -and $_.ScriptHash -ne $Migration.ScriptHash }) { return 'HashMismatch' }
    return 'Pending'
}

$repoRoot = Get-RepoRoot
$connectionText = New-ConnectionString
$migrations = @(Get-MigrationFiles -RepoRoot $repoRoot)

if ($migrations.Count -eq 0) {
    Write-Host 'No migration files found.'
    exit 0
}

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionText)
$connection.Open()
try {
    if ($Mode -eq 'Apply') {
        Ensure-HistoryTable -Connection $connection -RepoRoot $repoRoot
    }

    $history = Read-History -Connection $connection
    $pending = New-Object System.Collections.Generic.List[object]
    $skipped = New-Object System.Collections.Generic.List[object]
    $mismatches = New-Object System.Collections.Generic.List[object]
    $applied = New-Object System.Collections.Generic.List[object]
    $failed = New-Object System.Collections.Generic.List[object]

    foreach ($migration in $migrations) {
        $state = Get-MigrationState -Migration $migration -History $history
        if ($state -eq 'Skipped') { $skipped.Add($migration); continue }
        if ($state -eq 'HashMismatch') { $mismatches.Add($migration); continue }
        $pending.Add($migration)
    }

    Write-Host "DynamicErp Database Migration Runner"
    Write-Host "Mode: $Mode"
    Write-Host "Database: $($connection.Database)"
    Write-Host "Module: $(if ($ModuleName) { $ModuleName } else { 'All' })"
    Write-Host ''
    Write-Host "Pending scripts: $($pending.Count)"
    $pending | ForEach-Object { Write-Host "  PENDING  $($_.ScriptPath)  $($_.ScriptHash)" }
    Write-Host "Skipped scripts: $($skipped.Count)"
    $skipped | ForEach-Object { Write-Host "  SKIPPED  $($_.ScriptPath)" }
    Write-Host "Hash mismatch warnings: $($mismatches.Count)"
    $mismatches | ForEach-Object { Write-Host "  HASH MISMATCH  $($_.ScriptPath)  $($_.ScriptHash)" }
    Write-Host ''

    if ($Mode -ne 'Apply') {
        Write-Host 'No changes were applied.'
        exit $(if ($mismatches.Count -gt 0) { 2 } else { 0 })
    }

    if ($mismatches.Count -gt 0) {
        Write-Error 'Hash mismatch detected. Review changed scripts before applying new migrations.'
        exit 2
    }

    foreach ($migration in $pending) {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $transaction = $null
        try {
            Write-Host "APPLYING  $($migration.ScriptPath)"
            if (-not $NoTransaction) {
                $transaction = $connection.BeginTransaction()
            }
            $sql = [System.IO.File]::ReadAllText($migration.FullPath)
            foreach ($batch in (Split-SqlBatches -Sql $sql)) {
                Invoke-NonQuerySql -Connection $connection -Sql $batch -Transaction $transaction
            }
            if ($transaction) {
                $transaction.Commit()
                $transaction.Dispose()
                $transaction = $null
            }
            $stopwatch.Stop()
            Write-HistoryRow -Connection $connection -Migration $migration -DurationMs ([int]$stopwatch.ElapsedMilliseconds) -Success $true -ErrorMessage $null
            $applied.Add($migration)
        }
        catch {
            $stopwatch.Stop()
            if ($transaction) {
                try { $transaction.Rollback() } catch { }
                $transaction.Dispose()
                $transaction = $null
            }
            $message = $_.Exception.Message
            Write-HistoryRow -Connection $connection -Migration $migration -DurationMs ([int]$stopwatch.ElapsedMilliseconds) -Success $false -ErrorMessage $message
            $failed.Add([pscustomobject]@{ Migration = $migration; ErrorMessage = $message })
            Write-Host "FAILED    $($migration.ScriptPath)"
            Write-Host "          $message"
            if ($StopOnError) { break }
        }
    }

    Write-Host ''
    Write-Host "Applied scripts: $($applied.Count)"
    $applied | ForEach-Object { Write-Host "  APPLIED  $($_.ScriptPath)" }
    Write-Host "Failed scripts: $($failed.Count)"
    $failed | ForEach-Object { Write-Host "  FAILED   $($_.Migration.ScriptPath)  $($_.ErrorMessage)" }
    Write-Host "Skipped scripts: $($skipped.Count)"
    Write-Host "Hash mismatch warnings: $($mismatches.Count)"

    if ($failed.Count -gt 0) { exit 1 }
}
finally {
    $connection.Dispose()
}
