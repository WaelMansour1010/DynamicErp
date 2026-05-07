param([string]$ServiceName = "SatriahBranchSyncAgent", [switch]$DryRun)
$ErrorActionPreference = "Stop"
Write-Host "[BranchSyncAgent] Stop service '$ServiceName'."
if ($DryRun) { Write-Host "[BranchSyncAgent] Dry run only."; exit 0 }
Stop-Service -Name $ServiceName -Force
