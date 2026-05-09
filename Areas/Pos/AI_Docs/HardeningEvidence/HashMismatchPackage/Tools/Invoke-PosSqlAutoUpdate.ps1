param(
    [string]$ConnectionString,
    [string]$ConnectionName = "KishnyCashConnection",
    [string]$WebConfigPath = (Join-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent) "Web.config"),
    [string]$ManifestPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "Sql\POS_SQL_AutoUpdate_Manifest.json"),
    [ValidateSet("DryRun", "Apply", "ReportOnly")][string]$Mode = "DryRun",
    [switch]$StopOnError,
    [switch]$ForceNonPosDatabase,
    [string]$ReleaseNo = "",
    [string]$LogPath
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"
function Write-Log([string]$Message,[string]$Level="INFO") { $line = "{0:s} [{1}] {2}" -f (Get-Date),$Level,$Message; Write-Host $line; if($script:LogPath){ Add-Content -LiteralPath $script:LogPath -Value $line -Encoding UTF8 } }
function Get-ConnectionStringFromConfig([string]$ConfigPath,[string]$Name) { [xml]$xml = Get-Content -LiteralPath $ConfigPath -Raw; $node = $xml.configuration.connectionStrings.add | Where-Object { $_.name -eq $Name } | Select-Object -First 1; if(-not $node){ throw "Connection string '$Name' was not found in $ConfigPath." }; [string]$node.connectionString }
function New-Connection([string]$Cs) { $c = New-Object System.Data.SqlClient.SqlConnection($Cs); $c.Open(); $c }
function Invoke-Scalar($Connection,[string]$Sql) { $cmd=$Connection.CreateCommand(); $cmd.CommandTimeout=0; $cmd.CommandText=$Sql; $cmd.ExecuteScalar() }
function Invoke-NonQuery($Connection,[string]$Sql) { $cmd=$Connection.CreateCommand(); $cmd.CommandTimeout=0; $cmd.CommandText=$Sql; [void]$cmd.ExecuteNonQuery() }
function Split-SqlBatches([string]$Sql) { $sb=New-Object System.Text.StringBuilder; foreach($line in ($Sql -split "`r?`n")){ if($line -match '^\s*GO\s*(--.*)?$'){ $batch=$sb.ToString(); if(-not [string]::IsNullOrWhiteSpace($batch)){ $batch }; [void]$sb.Clear() } else { [void]$sb.AppendLine($line) } }; $tail=$sb.ToString(); if(-not [string]::IsNullOrWhiteSpace($tail)){ $tail } }
function Get-FileHashSha256([string]$Path) { $sha=[System.Security.Cryptography.SHA256]::Create(); $stream=[System.IO.File]::OpenRead($Path); try { (($sha.ComputeHash($stream) | ForEach-Object { $_.ToString("x2") }) -join "").ToUpperInvariant() } finally { $stream.Dispose(); $sha.Dispose() } }
function Ensure-HistoryTables($Connection) { Invoke-NonQuery $Connection @"
IF OBJECT_ID(N'dbo.POS_SqlUpdateRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SqlUpdateRun
    (RunId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SqlUpdateRun PRIMARY KEY, StartedAt DATETIME NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_StartedAt DEFAULT (GETDATE()), FinishedAt DATETIME NULL, Mode NVARCHAR(20) NOT NULL, Status NVARCHAR(30) NOT NULL, StartedBy NVARCHAR(256) NOT NULL, MachineName NVARCHAR(128) NOT NULL, DatabaseName SYSNAME NOT NULL, ServerName NVARCHAR(128) NOT NULL, ReleaseNo NVARCHAR(100) NULL, TotalScripts INT NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_TotalScripts DEFAULT (0), AppliedCount INT NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_AppliedCount DEFAULT (0), SkippedCount INT NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_SkippedCount DEFAULT (0), FailedCount INT NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_FailedCount DEFAULT (0), WarningCount INT NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_WarningCount DEFAULT (0));
END;
IF OBJECT_ID(N'dbo.POS_SqlUpdateHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SqlUpdateHistory
    (HistoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SqlUpdateHistory PRIMARY KEY, RunId INT NULL, ScriptOrder DECIMAL(10,2) NOT NULL, ScriptName NVARCHAR(260) NOT NULL, ScriptPath NVARCHAR(1000) NOT NULL, ScriptHash CHAR(64) NOT NULL, AppliedOn DATETIME NOT NULL CONSTRAINT DF_POS_SqlUpdateHistory_AppliedOn DEFAULT (GETDATE()), AppliedBy NVARCHAR(256) NOT NULL, MachineName NVARCHAR(128) NOT NULL, DatabaseName SYSNAME NOT NULL, DurationMs INT NULL, Success BIT NOT NULL, ErrorMessage NVARCHAR(MAX) NULL, ReleaseNo NVARCHAR(100) NULL);
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_POS_SqlUpdateHistory_ScriptName_Hash_Success' AND object_id = OBJECT_ID(N'dbo.POS_SqlUpdateHistory', N'U'))
BEGIN
    CREATE UNIQUE INDEX UX_POS_SqlUpdateHistory_ScriptName_Hash_Success ON dbo.POS_SqlUpdateHistory (ScriptName, ScriptHash, Success) WHERE Success = 1;
END;
"@ }
function Get-History($Connection) { $exists=Invoke-Scalar $Connection "SELECT CASE WHEN OBJECT_ID(N'dbo.POS_SqlUpdateHistory', N'U') IS NULL THEN 0 ELSE 1 END"; if([int]$exists -eq 0){ return @() }; $cmd=$Connection.CreateCommand(); $cmd.CommandText="SELECT ScriptName, ScriptHash, Success, AppliedOn, ErrorMessage FROM dbo.POS_SqlUpdateHistory"; $r=$cmd.ExecuteReader(); try{ $rows=@(); while($r.Read()){ $rows += [pscustomobject]@{ ScriptName=$r.GetString(0); ScriptHash=$r.GetString(1); Success=$r.GetBoolean(2); AppliedOn=$r.GetDateTime(3); ErrorMessage=$(if($r.IsDBNull(4)){""}else{$r.GetString(4)}) } }; $rows } finally { $r.Close() } }
function Test-PosDatabase($Connection,$Manifest) { foreach($o in $Manifest.requiredProbeObjects){ $safe=$o.Replace("'","''"); $exists=Invoke-Scalar $Connection "SELECT CASE WHEN OBJECT_ID(N'$safe') IS NULL THEN 0 ELSE 1 END"; if([int]$exists -ne 1){ return $false } }; $true }
function Invoke-ScriptFile($Connection,[string]$Path) { $batches=@(Split-SqlBatches ([System.IO.File]::ReadAllText($Path,[System.Text.Encoding]::UTF8))); $tx=$Connection.BeginTransaction(); try{ foreach($batch in $batches){ $cmd=$Connection.CreateCommand(); $cmd.Transaction=$tx; $cmd.CommandTimeout=0; $cmd.CommandText=$batch; [void]$cmd.ExecuteNonQuery() }; $tx.Commit() } catch { try { $tx.Rollback() } catch {}; throw } }
if(-not $ConnectionString){ $ConnectionString = Get-ConnectionStringFromConfig $WebConfigPath $ConnectionName }
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$manifestDir = Split-Path -Path $ManifestPath -Parent
$scriptRoot = (Resolve-Path (Join-Path $manifestDir $manifest.scriptRoot)).Path
if(-not $LogPath){ $logDir=Join-Path (Split-Path $PSScriptRoot -Parent) "Logs"; New-Item -ItemType Directory -Force -Path $logDir | Out-Null; $LogPath=Join-Path $logDir ("POS_SQL_AutoUpdate_{0:yyyyMMdd_HHmmss}.log" -f (Get-Date)) }
$script:LogPath=$LogPath
Write-Log "POS SQL auto-update starting. Mode=$Mode ConnectionName=$ConnectionName Manifest=$ManifestPath"
$connection = New-Connection $ConnectionString
try {
    $dbName=$connection.Database; $serverName=Invoke-Scalar $connection "SELECT CONVERT(NVARCHAR(128), @@SERVERNAME)"; Write-Log "Connected to $serverName / $dbName"
    if(-not (Test-PosDatabase $connection $manifest)){ $message="Database '$dbName' does not match POS probe objects. No scripts will be applied."; if($ForceNonPosDatabase){ Write-Log "$message ForceNonPosDatabase was supplied, continuing." "WARN" } else { Write-Log $message "WARN"; return } }
    if($Mode -eq "Apply"){ Ensure-HistoryTables $connection }
    $history=@(Get-History $connection)
    $scripts=@()
    foreach($entry in ($manifest.scripts | Sort-Object order,file)){
        if(-not $entry.autoApply){ continue }
        $path=Join-Path $scriptRoot ([string]$entry.file)
        if(-not (Test-Path -LiteralPath $path)){ throw "Manifest script not found: $path" }
        $hash=Get-FileHashSha256 $path
        $sameName=@($history | Where-Object { $_.ScriptName -eq $entry.file -and $_.Success })
        $sameHash=@($sameName | Where-Object { $_.ScriptHash -eq $hash })
        $hashMismatch=($sameName.Count -gt 0 -and $sameHash.Count -eq 0)
        $status=if($hashMismatch){"HashMismatch"}elseif($sameHash.Count -gt 0){"SkippedAlreadyApplied"}else{"Pending"}
        $scripts += [pscustomobject]@{ Order=[decimal]$entry.order; File=[string]$entry.file; Path=$path; Hash=$hash; Purpose=[string]$entry.purpose; Status=$status }
    }
    $pending=@($scripts | Where-Object {$_.Status -eq "Pending"}); $skipped=@($scripts | Where-Object {$_.Status -eq "SkippedAlreadyApplied"}); $mismatch=@($scripts | Where-Object {$_.Status -eq "HashMismatch"})
    Write-Log "Pending=$($pending.Count) Skipped=$($skipped.Count) HashMismatch=$($mismatch.Count)"
    foreach($s in $scripts){ Write-Log ("{0,6} {1,-22} {2}" -f $s.Order,$s.Status,$s.File) }
    if($mismatch.Count -gt 0){ throw "Hash mismatch detected. Refusing to apply changed scripts." }
    if($Mode -ne "Apply"){ return }
    $rel=$ReleaseNo.Replace("'","''")
    $runId=Invoke-Scalar $connection "INSERT dbo.POS_SqlUpdateRun (Mode,Status,StartedBy,MachineName,DatabaseName,ServerName,ReleaseNo,TotalScripts,SkippedCount) VALUES (N'Apply',N'Started',SUSER_SNAME(),HOST_NAME(),DB_NAME(),CONVERT(NVARCHAR(128),@@SERVERNAME),N'$rel',$($pending.Count),$($skipped.Count)); SELECT SCOPE_IDENTITY();"
    $applied=0; $failed=0
    foreach($s in $pending){
        Write-Log "Applying $($s.File)"; $sw=[System.Diagnostics.Stopwatch]::StartNew()
        try { Invoke-ScriptFile $connection $s.Path; $sw.Stop(); $file=$s.File.Replace("'","''"); $path=$s.Path.Replace("'","''"); Invoke-NonQuery $connection "INSERT dbo.POS_SqlUpdateHistory (RunId,ScriptOrder,ScriptName,ScriptPath,ScriptHash,AppliedBy,MachineName,DatabaseName,DurationMs,Success,ReleaseNo) VALUES ($runId,$($s.Order),N'$file',N'$path','$($s.Hash)',SUSER_SNAME(),HOST_NAME(),DB_NAME(),$([int]$sw.ElapsedMilliseconds),1,N'$rel');"; $applied++; Write-Log "Applied $($s.File) in $([int]$sw.ElapsedMilliseconds) ms" }
        catch { $sw.Stop(); $failed++; $err=$_.Exception.Message; if($err.Length -gt 3500){$err=$err.Substring(0,3500)}; $file=$s.File.Replace("'","''"); $path=$s.Path.Replace("'","''"); $safeErr=$err.Replace("'","''"); Invoke-NonQuery $connection "INSERT dbo.POS_SqlUpdateHistory (RunId,ScriptOrder,ScriptName,ScriptPath,ScriptHash,AppliedBy,MachineName,DatabaseName,DurationMs,Success,ErrorMessage,ReleaseNo) VALUES ($runId,$($s.Order),N'$file',N'$path','$($s.Hash)',SUSER_SNAME(),HOST_NAME(),DB_NAME(),$([int]$sw.ElapsedMilliseconds),0,N'$safeErr',N'$rel');"; Write-Log "FAILED $($s.File): $err" "ERROR"; if($StopOnError){ break } }
    }
    $status=if($failed -gt 0){"Failed"}else{"Completed"}
    Invoke-NonQuery $connection "UPDATE dbo.POS_SqlUpdateRun SET FinishedAt=GETDATE(),Status=N'$status',AppliedCount=$applied,FailedCount=$failed WHERE RunId=$runId"
    if($failed -gt 0){ throw "POS SQL auto-update failed. Check dbo.POS_SqlUpdateHistory RunId=$runId and log $LogPath." }
    Write-Log "POS SQL auto-update completed. Applied=$applied Skipped=$($skipped.Count) Log=$LogPath"
} finally { $connection.Dispose() }

