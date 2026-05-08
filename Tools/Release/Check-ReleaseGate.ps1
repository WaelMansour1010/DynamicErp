param(
    [string]$PackagePath,
    [string]$ConfigPath,
    [string]$Module = "KishnyPOS",
    [string]$BuildStampPath,
    [switch]$SkipGitClean
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$issues = New-Object System.Collections.Generic.List[string]

function Add-Issue([string]$message) {
    $script:issues.Add($message) | Out-Null
}

function Test-TextFileContains([string]$path, [string[]]$patterns) {
    if (!(Test-Path -LiteralPath $path)) { return @() }
    $text = Get-Content -LiteralPath $path -Raw -ErrorAction Stop
    $hits = @()
    foreach ($pattern in $patterns) {
        if ($text -match $pattern) { $hits += $pattern }
    }
    return $hits
}

Push-Location $repoRoot
try {
    $branch = (git branch --show-current).Trim()
    if ($branch -notmatch '^(release|hotfix)/') {
        Add-Issue "Current branch '$branch' is not release/* or hotfix/*."
    }

    if (!$SkipGitClean) {
        $status = git status --porcelain
        if ($status) {
            Add-Issue "Git worktree is not clean. Commit or create package from a clean release branch."
        }
    }
}
finally {
    Pop-Location
}

if ($PackagePath) {
    if (!(Test-Path -LiteralPath $PackagePath)) {
        Add-Issue "PackagePath does not exist: $PackagePath"
    }
    else {
        $riskyPackageFiles = Get-ChildItem -LiteralPath $PackagePath -Recurse -File -Force | Where-Object {
            $_.FullName -match '\\Areas\\MainErp\\' -or
            $_.FullName -match '\\AI_Docs\\' -or
            $_.FullName -match '\\Excel\\' -or
            $_.FullName -match '\\App_Data\\PosExcelImports\\' -or
            $_.Name -match 'Backup_\d{8}|_Backup_|\.bak$|\.xlsx$'
        }
        foreach ($file in $riskyPackageFiles) {
            Add-Issue "Risky file in package: $($file.FullName)"
        }

        if ($Module -eq "KishnyPOS") {
            $mainErpSql = Get-ChildItem -LiteralPath $PackagePath -Recurse -File -Filter *.sql | Where-Object { $_.FullName -match '\\MainErp\\|MainErp_' }
            foreach ($file in $mainErpSql) {
                Add-Issue "MainErp SQL found in Kishny package: $($file.FullName)"
            }
        }
    }
}

if ($ConfigPath) {
    if (!(Test-Path -LiteralPath $ConfigPath)) {
        Add-Issue "ConfigPath does not exist: $ConfigPath"
    }
    else {
        $badConfigPatterns = @(
            'EnableDevMasterPassword"\s+value="true"',
            'EnableDevStart"\s+value="true"',
            'EnableRunModeSelector"\s+value="true"',
            'EnableMainErpMigration"\s+value="true"',
            'debug="true"',
            'Wael\\Sql2019',
            'Initial Catalog=Eng',
            'Initial Catalog=Cash;',
            'key="DevMasterPassword"\s+value="[^"]+'
        )
        foreach ($hit in (Test-TextFileContains $ConfigPath $badConfigPatterns)) {
            Add-Issue "Production config contains risky pattern: $hit"
        }
    }
}

if ($BuildStampPath -and !(Test-Path -LiteralPath $BuildStampPath)) {
    Add-Issue "Build stamp not found: $BuildStampPath"
}

if ($issues.Count -eq 0) {
    Write-Host "GO"
    exit 0
}

Write-Host "NO-GO"
foreach ($issue in $issues) {
    Write-Host "- $issue"
}
exit 1
