<#
    POS performance verification commands.

    Run from an elevated PowerShell prompt on a test copy first. The Mixed load
    command writes POS invoices, so do not run it on production unless the DBA
    and business owner approve the maintenance/test window.
#>

$ProjectRoot = "F:\Source Code\DynamicErp"
$ToolsRoot = Join-Path $ProjectRoot "Areas\Pos\Tools"

# 1) Read-only audit.
powershell -NoProfile -ExecutionPolicy Bypass `
    -File (Join-Path $ToolsRoot "Invoke-PosPerformanceAudit.ps1") `
    -OutputRoot (Join-Path $ToolsRoot "audit-output")

# 2) Mixed 120-user load scenario.
#    This writes test invoices. Use only on a safe test database/copy.
powershell -NoProfile -ExecutionPolicy Bypass `
    -File (Join-Path $ToolsRoot "Invoke-PosLoadScenario.ps1") `
    -Scenario Mixed `
    -DurationMinutes 10 `
    -SaveWorkers 40 `
    -ReportWorkers 40 `
    -DashboardWorkers 40 `
    -BranchId 1 `
    -ItemId 2 `
    -StoreId 1 `
    -BoxId 1 `
    -PaymentType 1 `
    -Price 100 `
    -Vat 14 `
    -Quantity 1 `
    -AllowWrites `
    -AllowNonTestDatabase `
    -DangerousConfirmation I_UNDERSTAND_THIS_WRITES_DATA `
    -OutputPath (Join-Path $ToolsRoot ("pos-load-mixed120-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".csv"))

<#
Expected success criteria
-------------------------
1. Failure count = 0 or explicitly explained.
2. No SQL deadlocks.
3. No connection pool timeout errors.
4. No duplicate Transaction_ID or invoice serials.
5. Save average should not regress against the approved baseline.
6. Report/dashboard requests should not auto-load heavy data on page open.
#>
