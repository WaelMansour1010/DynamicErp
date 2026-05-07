param(
    [string]$ServiceName = "SatriahBranchSyncAgent",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
Write-Host "[BranchSyncAgent] Uninstall service '$ServiceName'."

if ($DryRun) {
    Write-Host "[BranchSyncAgent] Dry run only. No service was removed."
    exit 0
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "[BranchSyncAgent] Service does not exist."
    exit 0
}

if ($service.Status -ne "Stopped") {
    Stop-Service -Name $ServiceName -Force -ErrorAction Stop
}

sc.exe delete $ServiceName | Out-Null
Write-Host "[BranchSyncAgent] Service removed."
