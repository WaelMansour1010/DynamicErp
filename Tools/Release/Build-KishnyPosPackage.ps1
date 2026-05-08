param(
    [string]$ReleaseDate = (Get-Date -Format "yyyyMMdd"),
    [string]$OutputRoot,
    [string]$MSBuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (!$OutputRoot) {
    $OutputRoot = Join-Path $repoRoot "Releases\KishnyPOS_$ReleaseDate"
}

$releaseRoot = $OutputRoot
$packageRoot = Join-Path $releaseRoot "Package"
$sqlRoot = Join-Path $releaseRoot "Sql"
$configRoot = Join-Path $releaseRoot "Config"
$buildStamp = Join-Path $releaseRoot "BUILD_RELEASE_OK.txt"

New-Item -ItemType Directory -Force $packageRoot,$sqlRoot,$configRoot | Out-Null

$packagesPath = Join-Path $repoRoot "packages"
if (!(Test-Path -LiteralPath $packagesPath)) {
    $siblingPackages = Join-Path (Split-Path $repoRoot -Parent) "DynamicErp\packages"
    if (Test-Path -LiteralPath $siblingPackages) {
        New-Item -ItemType Junction -Path $packagesPath -Target $siblingPackages | Out-Null
    }
}

Push-Location $repoRoot
try {
    & $MSBuildPath "MyERP.sln" /p:Configuration=Release /p:Platform="Any CPU" /m /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}

Set-Content -LiteralPath $buildStamp -Value "Release build succeeded at $(Get-Date -Format s) from $repoRoot" -Encoding UTF8

Copy-Item -LiteralPath (Join-Path $repoRoot "bin") -Destination (Join-Path $packageRoot "bin") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "Content") -Destination (Join-Path $packageRoot "Content") -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -LiteralPath (Join-Path $repoRoot "Scripts") -Destination (Join-Path $packageRoot "Scripts") -Recurse -Force -ErrorAction SilentlyContinue

$posDest = Join-Path $packageRoot "Areas\Pos"
New-Item -ItemType Directory -Force $posDest | Out-Null
foreach ($name in @("Content","Scripts","Views","assets","Reports")) {
    $src = Join-Path $repoRoot "Areas\Pos\$name"
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $posDest $name) -Recurse -Force
    }
}

foreach ($remove in @(
    "Views\ExcelImport",
    "Views\Payments",
    "Views\Cashing",
    "AI_Docs",
    "Sql"
)) {
    $target = Join-Path $posDest $remove
    if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
}

Get-ChildItem -LiteralPath $packageRoot -Recurse -File | Where-Object {
    $_.FullName -match '\\Areas\\MainErp\\' -or
    $_.FullName -match '\\AI_Docs\\' -or
    $_.FullName -match '\\Excel\\' -or
    $_.FullName -match '\\App_Data\\PosExcelImports\\' -or
    $_.Name -match 'Backup_\d{8}|_Backup_|\.bak$|\.xlsx$'
} | Remove-Item -Force

$sqlNames = @(
    "31_POS_GetNextID_FromSequence_Concurrency.sql",
    "47_POS_SaveAttemptLog.sql",
    "46_POS_SaveTransaction_ConcurrencyIndexes.sql",
    "30_POS_SaveTransaction_UnicodeText.sql",
    "39_POS_Deadlock_Diagnostics.sql"
)
foreach ($sql in $sqlNames) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "Areas\Pos\Sql\$sql") -Destination $sqlRoot -Force
}

Copy-Item -LiteralPath (Join-Path $repoRoot "Tools\Release\Templates\00_BACKUP_BEFORE_APPLY.sql") -Destination (Join-Path $sqlRoot "00_BACKUP_BEFORE_APPLY.sql") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "Tools\Release\Templates\SQL_APPLY_ORDER.md") -Destination (Join-Path $sqlRoot "SQL_APPLY_ORDER.md") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "Tools\Release\Templates\SQL_ROLLBACK.md") -Destination (Join-Path $sqlRoot "SQL_ROLLBACK.md") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "ConfigTemplates\Web.KishnyPOS.Production.config.example") -Destination (Join-Path $configRoot "Web.config.production-ready") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "ConfigTemplates\Web.KishnyPOS.Production.config.example") -Destination (Join-Path $packageRoot "Web.config.production-ready") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "Tools\Release\Templates\README_DEPLOY_KISHNY_POS_20260508.txt") -Destination (Join-Path $releaseRoot "README_DEPLOY_KISHNY_POS_20260508.txt") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "Tools\Release\Templates\ROLLBACK_STEPS.txt") -Destination (Join-Path $releaseRoot "ROLLBACK_STEPS.txt") -Force

& (Join-Path $repoRoot "Tools\Release\Check-ReleaseGate.ps1") -PackagePath $packageRoot -ConfigPath (Join-Path $configRoot "Web.config.production-ready") -BuildStampPath $buildStamp -SkipGitClean
if ($LASTEXITCODE -ne 0) { throw "Release gate failed." }

Write-Host "Package created: $releaseRoot"
