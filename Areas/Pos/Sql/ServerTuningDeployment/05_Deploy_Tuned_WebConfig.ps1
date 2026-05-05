param(
    [string]$SiteRoot = "C:\WWWSite\cayshny",
    [string]$SourceConfig = "$PSScriptRoot\Web.Byte.Production.Tuned.config"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SourceConfig)) {
    throw "Source config not found: $SourceConfig"
}

if (-not (Test-Path $SiteRoot)) {
    throw "Site root not found: $SiteRoot"
}

$target = Join-Path $SiteRoot "Web.config"
$backupDir = Join-Path $SiteRoot "App_Data\ConfigBackups"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
if (Test-Path $target) {
    $backup = Join-Path $backupDir "Web.config.$stamp.bak"
    Copy-Item -LiteralPath $target -Destination $backup -Force
    Write-Host "Backup created: $backup" -ForegroundColor Green
}

Copy-Item -LiteralPath $SourceConfig -Destination $target -Force
Write-Host "Deployed tuned Web.config to: $target" -ForegroundColor Green
Write-Host "Users should Logout/Login once after deployment to receive fresh POSCTX cookie." -ForegroundColor Yellow

