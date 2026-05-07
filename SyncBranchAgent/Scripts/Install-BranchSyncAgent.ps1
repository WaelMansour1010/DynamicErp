param(
    [string]$ServiceName = "SatriahBranchSyncAgent",
    [string]$DisplayName = "Satriah Branch Sync Agent",
    [string]$BinaryPath = "$(Split-Path -Parent $PSScriptRoot)\bin\Release\SyncBranchAgent.exe",
    [string]$ServiceAccount = "NT AUTHORITY\LocalService",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$message) {
    Write-Host "[BranchSyncAgent] $message"
}

$resolvedBinary = [System.IO.Path]::GetFullPath($BinaryPath)
if (-not (Test-Path -LiteralPath $resolvedBinary)) {
    throw "Service binary was not found: $resolvedBinary"
}

Write-Step "Install service '$ServiceName' from '$resolvedBinary'."
Write-Step "Service account: $ServiceAccount"
Write-Step "Recovery: restart after 60 seconds for the first three failures."

if ($DryRun) {
    Write-Step "Dry run only. No service was created."
    exit 0
}

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    throw "Service '$ServiceName' already exists. Uninstall it first or use a different name."
}

New-Service -Name $ServiceName -BinaryPathName "`"$resolvedBinary`"" -DisplayName $DisplayName -StartupType Automatic
if ($ServiceAccount -and $ServiceAccount -ne "LocalSystem") {
    if ($ServiceAccount -in @("NT AUTHORITY\LocalService", "NT AUTHORITY\NetworkService")) {
        sc.exe config $ServiceName obj= "$ServiceAccount" | Out-Null
    } else {
        Write-Step "Custom service account requested. Set the password securely outside this script:"
        Write-Step "sc.exe config $ServiceName obj= `"$ServiceAccount`" password= <securely supplied password>"
    }
}
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
sc.exe failureflag $ServiceName 1 | Out-Null
Write-Step "Service installed. Configure the service account in Services.msc or with sc.exe if a domain account is required."
