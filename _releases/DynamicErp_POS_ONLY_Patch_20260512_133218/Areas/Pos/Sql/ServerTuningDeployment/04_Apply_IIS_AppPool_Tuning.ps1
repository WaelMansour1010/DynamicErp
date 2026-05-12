param(
    [string]$AppPoolName = "cayshny"
)

$ErrorActionPreference = "Stop"

Import-Module WebAdministration

$appPoolPath = "IIS:\AppPools\$AppPoolName"
if (-not (Test-Path $appPoolPath)) {
    throw "App Pool '$AppPoolName' was not found. Pass -AppPoolName with the real production app pool name."
}

Write-Host "Applying IIS App Pool tuning to $AppPoolName..." -ForegroundColor Cyan

Set-ItemProperty $appPoolPath -Name startMode -Value AlwaysRunning
Set-ItemProperty $appPoolPath -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
Set-ItemProperty $appPoolPath -Name recycling.periodicRestart.time -Value ([TimeSpan]::Zero)
Set-ItemProperty $appPoolPath -Name queueLength -Value 5000

Write-Host "Done. Current App Pool settings:" -ForegroundColor Green
Get-ItemProperty $appPoolPath |
    Select-Object name,startMode,queueLength,
        @{Name="IdleTimeout";Expression={$_.processModel.idleTimeout}},
        @{Name="PeriodicRestart";Expression={$_.recycling.periodicRestart.time}} |
    Format-List

Write-Host ""
Write-Host "Important: schedule manual recycle during quiet hours after deploying Web.config." -ForegroundColor Yellow

