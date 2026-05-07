param(
    [Parameter(Mandatory=$true)][int]$BranchId,
    [Parameter(Mandatory=$true)][string]$LocalDbConnectionString,
    [Parameter(Mandatory=$true)][string]$CentralApiBaseUrl,
    [string]$TokenEnvironmentVariableName,
    [string]$OutputPath = "$(Split-Path -Parent $PSScriptRoot)\SyncBranchAgent.exe.config",
    [bool]$EnableSend = $false,
    [bool]$DryRunSend = $true,
    [bool]$RequireHttps = $true,
    [int]$ScanIntervalSeconds = 60,
    [string]$OutboxFolder = "%ProgramData%\Satriah\BranchSyncAgent\outbox",
    [string]$LogFolder = "%ProgramData%\Satriah\BranchSyncAgent\logs"
)

$ErrorActionPreference = "Stop"
$templatePath = Join-Path (Split-Path -Parent $PSScriptRoot) "Config\BranchSyncAgent.config.template"
if (-not (Test-Path -LiteralPath $templatePath)) {
    throw "Template not found: $templatePath"
}

if ([string]::IsNullOrWhiteSpace($TokenEnvironmentVariableName)) {
    $TokenEnvironmentVariableName = "SATRIAH_BRANCH_SYNC_TOKEN_$BranchId"
}

[xml]$config = Get-Content -LiteralPath $templatePath
$config.configuration.connectionStrings.add.connectionString = $LocalDbConnectionString

function Set-AppSetting([xml]$xml, [string]$key, [string]$value) {
    $node = $xml.configuration.appSettings.add | Where-Object { $_.key -eq $key }
    if ($node) { $node.value = $value }
}

Set-AppSetting $config "BranchAgent.BranchId" ([string]$BranchId)
Set-AppSetting $config "BranchAgent.CentralApiBaseUrl" $CentralApiBaseUrl
Set-AppSetting $config "BranchAgent.ApiTokenEnvironmentVariable" $TokenEnvironmentVariableName
Set-AppSetting $config "BranchAgent.EnableSend" ([string]$EnableSend).ToLowerInvariant()
Set-AppSetting $config "BranchAgent.DryRunSend" ([string]$DryRunSend).ToLowerInvariant()
Set-AppSetting $config "BranchAgent.RequireHttps" ([string]$RequireHttps).ToLowerInvariant()
Set-AppSetting $config "BranchAgent.PollSeconds" ([string]$ScanIntervalSeconds)
Set-AppSetting $config "BranchAgent.OutboxPath" $OutboxFolder
Set-AppSetting $config "BranchAgent.LogPath" $LogFolder
Set-AppSetting $config "BranchAgent.WatermarkPath" (Join-Path (Split-Path -Parent $OutboxFolder) "watermark.json")

$config.Save($OutputPath)
Write-Host "[BranchSyncAgent] Config written to $OutputPath"
Write-Host "[BranchSyncAgent] Token value was not written. Set environment variable '$TokenEnvironmentVariableName' on the service account or machine."
