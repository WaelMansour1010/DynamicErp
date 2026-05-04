param(
    [string]$WebConfigPath = "F:\Source Code\DynamicErp\Web.config",
    [string]$ConnectionStringName = "KishnyCashConnection",
    [ValidateSet("SaveBurst","Mixed","ReportsOnly","DashboardOnly")]
    [string]$Scenario = "Mixed",
    [int]$DurationMinutes = 5,
    [int]$SaveWorkers = 40,
    [int]$ReportWorkers = 40,
    [int]$DashboardWorkers = 40,
    [int]$BranchId,
    [int]$ItemId,
    [int]$StoreId,
    [int]$BoxId,
    [int]$PaymentType = 1,
    [decimal]$Price = 100,
    [decimal]$Vat = 0,
    [decimal]$Quantity = 1,
    [int]$ThinkTimeMs = 1000,
    [switch]$AllowWrites,
    [switch]$AllowNonTestDatabase,
    [string]$DangerousConfirmation = "",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

if (-not (Test-Path -LiteralPath $WebConfigPath)) {
    throw "Web.config not found: $WebConfigPath"
}

[xml]$config = Get-Content -LiteralPath $WebConfigPath
$connectionNode = $config.configuration.connectionStrings.add | Where-Object { $_.name -eq $ConnectionStringName } | Select-Object -First 1
if (-not $connectionNode) {
    throw "Connection string not found: $ConnectionStringName"
}

$ConnectionString = $connectionNode.connectionString
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($ConnectionString)
$databaseName = $builder.InitialCatalog
$safeName = $databaseName -match "(?i)(test|dev|sandbox|local|qa)"

if (($Scenario -eq "SaveBurst" -or $Scenario -eq "Mixed") -and -not $AllowWrites) {
    throw "This scenario writes sales invoices. Pass -AllowWrites only on a safe test database."
}

if (($Scenario -eq "SaveBurst" -or $Scenario -eq "Mixed") -and -not $safeName) {
    if (-not $AllowNonTestDatabase -or $DangerousConfirmation -ne "I_UNDERSTAND_THIS_WRITES_DATA") {
        throw "Refusing write load against database '$databaseName'. Use a restored test copy, or pass -AllowNonTestDatabase with the exact confirmation."
    }
}

if (($Scenario -eq "SaveBurst" -or $Scenario -eq "Mixed") -and ($BranchId -le 0 -or $ItemId -le 0 -or $StoreId -le 0 -or $BoxId -le 0)) {
    throw "BranchId, ItemId, StoreId, and BoxId are required for save scenarios."
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot ("pos-load-" + $Scenario + "-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".csv")
}

$endAtUtc = [DateTime]::UtcNow.AddMinutes($DurationMinutes)
$runId = "POS-LOAD-" + (Get-Date -Format "yyyyMMdd-HHmmss")
$gate = New-Object System.Threading.ManualResetEventSlim($false)
$workerCount = 0
if ($Scenario -eq "SaveBurst") { $workerCount = $SaveWorkers }
elseif ($Scenario -eq "ReportsOnly") { $workerCount = $ReportWorkers }
elseif ($Scenario -eq "DashboardOnly") { $workerCount = $DashboardWorkers }
else { $workerCount = $SaveWorkers + $ReportWorkers + $DashboardWorkers }

$pool = [RunspaceFactory]::CreateRunspacePool(1, [Math]::Min([Math]::Max($workerCount, 1), 160))
$pool.Open()
$jobs = New-Object System.Collections.Generic.List[object]

$worker = {
    param(
        [string]$ConnectionString,
        [object]$Gate,
        [DateTime]$EndAtUtc,
        [string]$Kind,
        [string]$RunId,
        [int]$Index,
        [int]$BranchId,
        [int]$ItemId,
        [int]$StoreId,
        [int]$BoxId,
        [int]$PaymentType,
        [decimal]$Price,
        [decimal]$Vat,
        [decimal]$Quantity,
        [int]$ThinkTimeMs
    )

    function AddParam($cmd, $name, $type, $value) {
        $null = $cmd.Parameters.Add($name, $type)
        $cmd.Parameters[$name].Value = if ($null -eq $value) { [DBNull]::Value } else { $value }
    }

    function RunQuery($sql) {
        $cn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $cmd = $cn.CreateCommand()
        $cmd.CommandText = $sql
        $cmd.CommandTimeout = 90
        try {
            $cn.Open()
            $null = $cmd.ExecuteScalar()
        }
        finally {
            $cn.Dispose()
        }
    }

    function RunReportProc {
        $cn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $cmd = New-Object System.Data.SqlClient.SqlCommand("dbo.usp_POS_Report_Run", $cn)
        $cmd.CommandType = [System.Data.CommandType]::StoredProcedure
        $cmd.CommandTimeout = 90
        AddParam $cmd "@reportKey" ([System.Data.SqlDbType]::NVarChar) "daily-movements"
        $cmd.Parameters["@reportKey"].Size = 80
        AddParam $cmd "@fromDate" ([System.Data.SqlDbType]::DateTime) ([DateTime]::Today)
        AddParam $cmd "@toDate" ([System.Data.SqlDbType]::DateTime) ([DateTime]::Today)
        AddParam $cmd "@branchId" ([System.Data.SqlDbType]::Int) ([DBNull]::Value)
        AddParam $cmd "@userId" ([System.Data.SqlDbType]::Int) 1
        AddParam $cmd "@canChangeDefaults" ([System.Data.SqlDbType]::Bit) $true
        try {
            $cn.Open()
            $reader = $cmd.ExecuteReader()
            $reader.Close()
        }
        finally {
            $cn.Dispose()
        }
    }

    function SaveInvoice() {
        $cn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $cmd = New-Object System.Data.SqlClient.SqlCommand("dbo.usp_POS_SaveTransaction", $cn)
        $cmd.CommandType = [System.Data.CommandType]::StoredProcedure
        $cmd.CommandTimeout = 120

        $totalPrice = [decimal]::Round(($Price * $Quantity) + $Vat, 2)
        $items = New-Object System.Data.DataTable
        foreach ($c in @(
            @("Item_ID",[int]),@("Quantity",[double]),@("Price",[double]),@("UnitId",[int]),@("ShowQty",[double]),
            @("QtyBySmalltUnit",[double]),@("showPrice",[double]),@("TotalPrice",[double]),@("StoreID2",[int]),
            @("Vat",[double]),@("Vatyo",[double]),@("discountvalue",[decimal]),@("TotalDiscountPerLine",[decimal]),
            @("ItemCase",[int]),@("CostPrice",[decimal]),@("SavedItemType",[int])
        )) { [void]$items.Columns.Add($c[0], $c[1]) }
        [void]$items.Rows.Add($ItemId, [double]$Quantity, [double]$Price, 1, [double]$Quantity, 1.0, [double]$Price, [double]$totalPrice, $StoreId, [double]$Vat, 14.0, 0, 0, 1, 0, 0)

        $payments = New-Object System.Data.DataTable
        [void]$payments.Columns.Add("PaymentID", [int])
        [void]$payments.Columns.Add("Value", [double])
        [void]$payments.Columns.Add("CardNo", [string])
        [void]$payments.Columns.Add("MaxValue", [double])
        [void]$payments.Rows.Add($PaymentType, [double]$totalPrice, [DBNull]::Value, [double]$totalPrice)

        AddParam $cmd "@TransactionDate" ([System.Data.SqlDbType]::SmallDateTime) ([DateTime]::Today)
        AddParam $cmd "@BranchId" ([System.Data.SqlDbType]::Int) $BranchId
        AddParam $cmd "@StoreID" ([System.Data.SqlDbType]::Int) $StoreId
        AddParam $cmd "@UserID" ([System.Data.SqlDbType]::Int) 1
        AddParam $cmd "@Emp_ID" ([System.Data.SqlDbType]::Int) 1
        AddParam $cmd "@CustomerID" ([System.Data.SqlDbType]::Int) 2
        AddParam $cmd "@PaymentType" ([System.Data.SqlDbType]::Int) $PaymentType
        AddParam $cmd "@BoxID" ([System.Data.SqlDbType]::Int) $BoxId
        AddParam $cmd "@PayedValue" ([System.Data.SqlDbType]::Money) $totalPrice
        AddParam $cmd "@NetValue" ([System.Data.SqlDbType]::Money) $totalPrice
        AddParam $cmd "@RemainValue" ([System.Data.SqlDbType]::Money) 0
        AddParam $cmd "@PaymentNetid" ([System.Data.SqlDbType]::Int) $PaymentType
        foreach ($name in "@IsCashOut","@IsPOS","@OtherItems","@TrafficViolations","@isRecharg","@IsWallet","@HaveGuarantee") { AddParam $cmd $name ([System.Data.SqlDbType]::Bit) $false }
        AddParam $cmd "@PayType" ([System.Data.SqlDbType]::Int) 1
        AddParam $cmd "@POSBillType" ([System.Data.SqlDbType]::Int) 0
        AddParam $cmd "@STableID" ([System.Data.SqlDbType]::Int) -1
        AddParam $cmd "@SessionD" ([System.Data.SqlDbType]::Int) -1
        AddParam $cmd "@BillBasedOn" ([System.Data.SqlDbType]::Int) 0
        AddParam $cmd "@ItemIDService" ([System.Data.SqlDbType]::Int) $ItemId
        AddParam $cmd "@ItemIDService2" ([System.Data.SqlDbType]::Int) ([DBNull]::Value)
        AddParam $cmd "@CashCustomerName" ([System.Data.SqlDbType]::NVarChar) ("Load Test")
        $cmd.Parameters["@CashCustomerName"].Size = 100
        AddParam $cmd "@CashCustomerPhone" ([System.Data.SqlDbType]::NVarChar) ("010" + $Index.ToString("00000000"))
        $cmd.Parameters["@CashCustomerPhone"].Size = 100
        foreach ($name in "@Phone2","@IPN","@ManualNO","@ManualNo2","@VisaNumber","@CardSerial") {
            AddParam $cmd $name ([System.Data.SqlDbType]::NVarChar) ([DBNull]::Value)
            $cmd.Parameters[$name].Size = 255
        }
        AddParam $cmd "@NoID" ([System.Data.SqlDbType]::VarChar) $RunId
        $cmd.Parameters["@NoID"].Size = 50
        AddParam $cmd "@Prefix" ([System.Data.SqlDbType]::VarChar) ([DBNull]::Value)
        $cmd.Parameters["@Prefix"].Size = 10
        $cmd.Parameters["@IPN"].Value = "LOAD-" + $RunId + "-" + $Index.ToString("000000")
        $cmd.Parameters["@ManualNO"].Value = $RunId + "-" + $Index.ToString("000000")
        foreach ($name in "@RechargeValue","@Tet_NumPoket","@ViolationsValue") { AddParam $cmd $name ([System.Data.SqlDbType]::Float) ([DBNull]::Value) }
        $itemsParam = $cmd.Parameters.Add("@Items", [System.Data.SqlDbType]::Structured)
        $itemsParam.TypeName = "dbo.POS_TransactionItems"
        $itemsParam.Value = $items
        $paymentsParam = $cmd.Parameters.Add("@SalesPayments", [System.Data.SqlDbType]::Structured)
        $paymentsParam.TypeName = "dbo.POS_SalesPayments"
        $paymentsParam.Value = $payments

        try {
            $cn.Open()
            $reader = $cmd.ExecuteReader()
            $reader.Close()
        }
        finally {
            $cn.Dispose()
        }
    }

    $Gate.Wait()
    $results = New-Object System.Collections.Generic.List[object]
    $iteration = 0
    while ([DateTime]::UtcNow -lt $EndAtUtc) {
        $iteration++
        $sw = [Diagnostics.Stopwatch]::StartNew()
        $ok = $true
        $err = ""
        try {
            if ($Kind -eq "save") { SaveInvoice }
            elseif ($Kind -eq "report") { RunReportProc }
            elseif ($Kind -eq "dashboard") { RunQuery "SELECT COUNT(1) FROM dbo.Transactions WITH (NOLOCK) WHERE Transaction_Type=21 AND Transaction_Date>=CONVERT(date, GETDATE())" }
            else { Start-Sleep -Milliseconds 500 }
        }
        catch {
            $ok = $false
            $err = $_.Exception.Message
        }
        finally {
            $sw.Stop()
        }
        $results.Add([pscustomobject]@{ Worker=$Index; Kind=$Kind; Iteration=$iteration; Success=$ok; ElapsedMs=$sw.ElapsedMilliseconds; Error=$err })
        if ($ThinkTimeMs -gt 0) {
            Start-Sleep -Milliseconds $ThinkTimeMs
        }
    }
    return $results
}

function Add-Worker([string]$kind, [int]$count, [int]$startIndex) {
    for ($i = 1; $i -le $count; $i++) {
        $ps = [PowerShell]::Create()
        $ps.RunspacePool = $pool
        [void]$ps.AddScript($worker).
            AddArgument($ConnectionString).
            AddArgument($gate).
            AddArgument($endAtUtc).
            AddArgument($kind).
            AddArgument($runId).
            AddArgument($startIndex + $i).
            AddArgument($BranchId).
            AddArgument($ItemId).
            AddArgument($StoreId).
            AddArgument($BoxId).
            AddArgument($PaymentType).
            AddArgument($Price).
            AddArgument($Vat).
            AddArgument($Quantity).
            AddArgument($ThinkTimeMs)
        $jobs.Add([pscustomobject]@{ PowerShell=$ps; Handle=$ps.BeginInvoke() })
    }
}

$idx = 0
if ($Scenario -eq "SaveBurst") { Add-Worker "save" $SaveWorkers $idx }
elseif ($Scenario -eq "ReportsOnly") { Add-Worker "report" $ReportWorkers $idx }
elseif ($Scenario -eq "DashboardOnly") { Add-Worker "dashboard" $DashboardWorkers $idx }
else {
    Add-Worker "save" $SaveWorkers $idx; $idx += $SaveWorkers
    Add-Worker "report" $ReportWorkers $idx; $idx += $ReportWorkers
    Add-Worker "dashboard" $DashboardWorkers $idx
}

Write-Host "Starting $Scenario for $DurationMinutes minute(s), workers=$workerCount, runId=$runId"
$gate.Set()

$allResults = foreach ($job in $jobs) {
    try { $job.PowerShell.EndInvoke($job.Handle) }
    finally { $job.PowerShell.Dispose() }
}

$pool.Close()
$pool.Dispose()

$flat = @($allResults | ForEach-Object { $_ })
$flat | Export-Csv -Path $OutputPath -NoTypeInformation -Encoding UTF8

$summary = $flat | Group-Object Kind | ForEach-Object {
    $rows = @($_.Group)
    $fail = @($rows | Where-Object { -not $_.Success })
    $lat = $rows | Measure-Object ElapsedMs -Minimum -Maximum -Average
    [pscustomobject]@{
        Kind = $_.Name
        Count = $rows.Count
        Failures = $fail.Count
        FailurePercent = if ($rows.Count) { [math]::Round($fail.Count * 100.0 / $rows.Count, 2) } else { 0 }
        MinMs = $lat.Minimum
        AvgMs = [math]::Round($lat.Average, 2)
        MaxMs = $lat.Maximum
    }
}

$summary | Format-Table -AutoSize
Write-Host "Result file: $OutputPath"
