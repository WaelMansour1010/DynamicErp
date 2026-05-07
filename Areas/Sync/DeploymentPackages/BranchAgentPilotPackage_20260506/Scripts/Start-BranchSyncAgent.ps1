param([string]$ServiceName = "SatriahBranchSyncAgent", [switch]$DryRun)
$ErrorActionPreference = "Stop"
Write-Host "[BranchSyncAgent] Start service '$ServiceName'."
if ($DryRun) { Write-Host "[BranchSyncAgent] Dry run only."; exit 0 }
Start-Service -Name $ServiceName
