[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ConnectionString,
    [int]$UsersCount = 100,
    [int]$InvoicesCount = 100,
    [int]$BranchId,
    [int]$ItemId,
    [int]$UnitId = 1,
    [int]$StoreId,
    [int]$BoxId,
    [int]$PaymentType = 1,
    [decimal]$Price = 100,
    [decimal]$Vat = 0,
    [decimal]$Quantity = 1,
    [string]$UserNamePrefix = "",
    [switch]$AllowReuseUsers,
    [switch]$AllowWrites,
    [switch]$AllowNonTestDatabase,
    [string]$DangerousConfirmation = "",
    [string]$OutputPath = ""
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Invoke-Scalar {
    param([string]$Sql, [hashtable]$Parameters = @{})

    $cn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = $Sql

    foreach ($k in $Parameters.Keys) {
        [void]$cmd.Parameters.AddWithValue($k, $Parameters[$k])
    }

    try {
        $cn.Open()
        return $cmd.ExecuteScalar()
    }
    finally {
        $cn.Dispose()
    }
}

function Invoke-Query {
    param([string]$Sql, [hashtable]$Parameters = @{})

    $cn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = $Sql

    foreach ($k in $Parameters.Keys) {
        [void]$cmd.Parameters.AddWithValue($k, $Parameters[$k])
    }

    $da = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable

    try {
        [void]$da.Fill($dt)
        Write-Output -NoEnumerate $dt
        return
    }
    finally {
        $cn.Dispose()
    }
}

function Assert-GuardedDatabase {
    if (-not $AllowWrites) {
        throw "Refusing to run: pass -AllowWrites"
    }

    $dbName = [string](Invoke-Scalar "SELECT DB_NAME();")
    Write-Host "Database: $dbName"

    $safeName = $dbName -match "(?i)(test|dev|sandbox|local|qa)"
    if (-not $safeName) {
        if (-not $AllowNonTestDatabase -or $DangerousConfirmation -ne "I_UNDERSTAND_THIS_WRITES_DATA") {
            throw "Refusing to write to database '$dbName'."
        }
    }

    $procExists = Invoke-Scalar "SELECT OBJECT_ID(N'dbo.usp_POS_SaveTransaction', N'P');"
    if ($null -eq $procExists -or [DBNull]::Value.Equals($procExists)) {
        throw "dbo.usp_POS_SaveTransaction was not found."
    }

    return $dbName
}

function Get-TestUsers {
    $sql = @"
SELECT TOP (@take)
    UserID,
    COALESCE(NULLIF(Empid, 0), 0) AS Emp_ID,
    COALESCE(NULLIF(BranchId, 0), @branchId) AS BranchId,
    COALESCE(NULLIF(StoreID, 0), @storeId) AS StoreID,
    COALESCE(NULLIF(BoxID, 0), @boxId) AS BoxID,
    UserName
FROM dbo.TblUsers
WHERE
    (@prefix = N'' OR UserName LIKE @prefix)
ORDER BY UserID;
"@

    $dt = Invoke-Query $sql @{
        "@take" = $UsersCount
        "@prefix" = $UserNamePrefix
        "@branchId" = $BranchId
        "@storeId" = $StoreId
        "@boxId" = $BoxId
    }

    if ($dt -isnot [System.Data.DataTable]) {
        throw "Get-TestUsers did not return DataTable"
    }

    Write-Host "Matched users: $($dt.Rows.Count)"

    if ($dt.Rows.Count -eq 0) {
        throw "No users found in dbo.TblUsers. Try checking table name/data."
    }

    if ($dt.Rows.Count -lt $UsersCount -and -not $AllowReuseUsers) {
        throw "Only $($dt.Rows.Count) users found. Pass -AllowReuseUsers."
    }

    Write-Output -NoEnumerate $dt
    return
}

$databaseName = Assert-GuardedDatabase

if ($BranchId -le 0) { throw "BranchId is required" }
if ($ItemId -le 0) { throw "ItemId is required" }
if ($StoreId -le 0) { throw "StoreId is required" }
if ($BoxId -le 0) { throw "BoxId is required" }

$users = Get-TestUsers

$runId = "POS-STRESS-" + (Get-Date -Format "yyyyMMdd-HHmmss")
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot ("pos-stress-" + $runId + ".csv")
}

$totalPrice = [decimal]::Round(($Price * $Quantity) + $Vat, 2)

$gate = New-Object System.Threading.ManualResetEventSlim($false)
$pool = [RunspaceFactory]::CreateRunspacePool(1, [Math]::Min($InvoicesCount, 100))
$pool.Open()
$jobs = New-Object System.Collections.Generic.List[object]

$worker = {
    param(
        [string]$ConnectionString,
        [object]$StartGate,
        [string]$RunId,
        [int]$Index,
        [int]$UserId,
        [int]$EmpId,
        [int]$BranchId,
        [int]$StoreId,
        [int]$BoxId,
        [int]$PaymentType,
        [int]$ItemId,
        [int]$UnitId,
        [decimal]$Quantity,
        [decimal]$Price,
        [decimal]$Vat,
        [decimal]$TotalPrice
    )

    $result = [ordered]@{
        Index = $Index
        UserId = $UserId
        BranchId = $BranchId
        Transaction_ID = $null
        NoteSerial1 = $null
        Success = $false
        ElapsedMs = 0
        Error = ""
    }

    $StartGate.Wait()
    $sw = [Diagnostics.Stopwatch]::StartNew()

    $cn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $cmd = New-Object System.Data.SqlClient.SqlCommand("dbo.usp_POS_SaveTransaction", $cn)
    $cmd.CommandType = [System.Data.CommandType]::StoredProcedure
    $cmd.CommandTimeout = 120

    $items = New-Object System.Data.DataTable
    [void]$items.Columns.Add("Item_ID", [int])
    [void]$items.Columns.Add("Quantity", [double])
    [void]$items.Columns.Add("Price", [double])
    [void]$items.Columns.Add("UnitId", [int])
    [void]$items.Columns.Add("ShowQty", [double])
    [void]$items.Columns.Add("QtyBySmalltUnit", [double])
    [void]$items.Columns.Add("showPrice", [double])
    [void]$items.Columns.Add("TotalPrice", [double])
    [void]$items.Columns.Add("StoreID2", [int])
    [void]$items.Columns.Add("Vat", [double])
    [void]$items.Columns.Add("Vatyo", [double])
    [void]$items.Columns.Add("discountvalue", [decimal])
    [void]$items.Columns.Add("TotalDiscountPerLine", [decimal])
    [void]$items.Columns.Add("ItemCase", [int])
    [void]$items.Columns.Add("CostPrice", [decimal])
    [void]$items.Columns.Add("SavedItemType", [int])

    [void]$items.Rows.Add(
        $ItemId,
        [double]$Quantity,
        [double]$Price,
        $UnitId,
        [double]$Quantity,
        1.0,
        [double]$Price,
        [double]$TotalPrice,
        $StoreId,
        [double]$Vat,
        14.0,
        0,
        0,
        1,
        0,
        0
    )

    $payments = New-Object System.Data.DataTable
    [void]$payments.Columns.Add("PaymentID", [int])
    [void]$payments.Columns.Add("Value", [double])
    [void]$payments.Columns.Add("CardNo", [string])
    [void]$payments.Columns.Add("MaxValue", [double])
    [void]$payments.Rows.Add($PaymentType, [double]$TotalPrice, [DBNull]::Value, [double]$TotalPrice)

    try {
        $null = $cmd.Parameters.Add("@TransactionDate", [System.Data.SqlDbType]::SmallDateTime)
        $cmd.Parameters["@TransactionDate"].Value = [DateTime]::Today

        $null = $cmd.Parameters.Add("@BranchId", [System.Data.SqlDbType]::Int)
        $cmd.Parameters["@BranchId"].Value = $BranchId

        $null = $cmd.Parameters.Add("@StoreID", [System.Data.SqlDbType]::Int)
        $cmd.Parameters["@StoreID"].Value = $StoreId

        $null = $cmd.Parameters.Add("@UserID", [System.Data.SqlDbType]::Int)
        $cmd.Parameters["@UserID"].Value = $UserId

        $null = $cmd.Parameters.Add("@Emp_ID", [System.Data.SqlDbType]::Int)
        $cmd.Parameters["@Emp_ID"].Value = $EmpId

        $null = $cmd.Parameters.Add("@CustomerID", [System.Data.SqlDbType]::Int)
        $cmd.Parameters["@CustomerID"].Value = 2

        $null = $cmd.Parameters.Add("@PaymentType", [System.Data.SqlDbType]::Int)
        $cmd.Parameters["@PaymentType"].Value = $PaymentType

        $null = $cmd.Parameters.Add("@BoxID", [System.Data.SqlDbType]::Int)
        $cmd.Parameters["@BoxID"].Value = $BoxId

        $null = $cmd.Parameters.Add("@PayedValue", [System.Data.SqlDbType]::Money)
        $cmd.Parameters["@PayedValue"].Value = $TotalPrice

        $null = $cmd.Parameters.Add("@NetValue", [System.Data.SqlDbType]::Money)
        $cmd.Parameters["@NetValue"].Value = $TotalPrice

        $null = $cmd.Parameters.Add("@RemainValue", [System.Data.SqlDbType]::Money)
        $cmd.Parameters["@RemainValue"].Value = 0

        $null = $cmd.Parameters.Add("@PaymentNetid", [System.Data.SqlDbType]::Int)
        $cmd.Parameters["@PaymentNetid"].Value = $PaymentType

        foreach ($name in "@IsCashOut","@IsPOS","@OtherItems","@TrafficViolations","@isRecharg","@IsWallet","@HaveGuarantee") {
            $null = $cmd.Parameters.Add($name, [System.Data.SqlDbType]::Bit)
            $cmd.Parameters[$name].Value = $false
        }

        foreach ($name in "@PayType","@POSBillType","@STableID","@SessionD","@BillBasedOn","@ItemIDService2") {
            $null = $cmd.Parameters.Add($name, [System.Data.SqlDbType]::Int)
            $cmd.Parameters[$name].Value = [DBNull]::Value
        }

        $cmd.Parameters["@PayType"].Value = 1
        $cmd.Parameters["@POSBillType"].Value = 0
        $cmd.Parameters["@STableID"].Value = -1
        $cmd.Parameters["@SessionD"].Value = -1
        $cmd.Parameters["@BillBasedOn"].Value = 0

        $null = $cmd.Parameters.Add("@ItemIDService", [System.Data.SqlDbType]::Int)
        $cmd.Parameters["@ItemIDService"].Value = $ItemId

        $null = $cmd.Parameters.Add("@CashCustomerName", [System.Data.SqlDbType]::NVarChar, 100)
        $cmd.Parameters["@CashCustomerName"].Value = "Stress Test"

        $null = $cmd.Parameters.Add("@CashCustomerPhone", [System.Data.SqlDbType]::NVarChar, 100)
        $cmd.Parameters["@CashCustomerPhone"].Value = "010" + $Index.ToString("00000000")

        foreach ($name in "@Phone2","@IPN","@ManualNO","@ManualNo2","@VisaNumber","@CardSerial") {
            $null = $cmd.Parameters.Add($name, [System.Data.SqlDbType]::NVarChar, 255)
            $cmd.Parameters[$name].Value = [DBNull]::Value
        }

        $null = $cmd.Parameters.Add("@NoID", [System.Data.SqlDbType]::VarChar, 50)
        $cmd.Parameters["@NoID"].Value = [DBNull]::Value

        $null = $cmd.Parameters.Add("@Prefix", [System.Data.SqlDbType]::VarChar, 10)
        $cmd.Parameters["@Prefix"].Value = [DBNull]::Value

        $cmd.Parameters["@IPN"].Value = "STRESS-ID-" + $RunId + "-" + $Index.ToString("000")
        $cmd.Parameters["@ManualNO"].Value = $RunId + "-" + $Index.ToString("000")
        $cmd.Parameters["@NoID"].Value = $RunId

        foreach ($name in "@RechargeValue","@Tet_NumPoket","@ViolationsValue") {
            $null = $cmd.Parameters.Add($name, [System.Data.SqlDbType]::Float)
            $cmd.Parameters[$name].Value = [DBNull]::Value
        }

        $itemsParam = $cmd.Parameters.Add("@Items", [System.Data.SqlDbType]::Structured)
        $itemsParam.TypeName = "dbo.POS_TransactionItems"
        $itemsParam.Value = $items

        $paymentsParam = $cmd.Parameters.Add("@SalesPayments", [System.Data.SqlDbType]::Structured)
        $paymentsParam.TypeName = "dbo.POS_SalesPayments"
        $paymentsParam.Value = $payments

        $cn.Open()
        $reader = $cmd.ExecuteReader()

        if ($reader.Read()) {
            $result.Transaction_ID = [int]$reader["Transaction_ID"]
            $result.NoteSerial1 = [string]$reader["NoteSerial1"]
            $result.Success = $true
        }
        else {
            $result.Error = "No result row returned."
        }

        $reader.Close()
    }
    catch {
        $result.Error = $_.Exception.ToString()
    }
    finally {
        $sw.Stop()
        $result.ElapsedMs = $sw.ElapsedMilliseconds
        $cn.Dispose()
    }

    return [pscustomobject]$result
}

for ($i = 1; $i -le $InvoicesCount; $i++) {
    $user = $users.Rows[($i - 1) % $users.Rows.Count]

    $ps = [PowerShell]::Create()
    $ps.RunspacePool = $pool

    [void]$ps.AddScript($worker).
        AddArgument($ConnectionString).
        AddArgument($gate).
        AddArgument($runId).
        AddArgument($i).
        AddArgument([int]$user.UserID).
        AddArgument([int]$user.Emp_ID).
        AddArgument($BranchId).
        AddArgument($StoreId).
        AddArgument($BoxId).
        AddArgument($PaymentType).
        AddArgument($ItemId).
        AddArgument($UnitId).
        AddArgument($Quantity).
        AddArgument($Price).
        AddArgument($Vat).
        AddArgument($totalPrice)

    $jobs.Add([pscustomobject]@{
        PowerShell = $ps
        Handle = $ps.BeginInvoke()
    })
}

Write-Host "Starting $InvoicesCount concurrent invoice saves with run id $runId..."
$gate.Set()

$results = foreach ($job in $jobs) {
    try {
        $job.PowerShell.EndInvoke($job.Handle)
    }
    finally {
        $job.PowerShell.Dispose()
    }
}

$pool.Close()
$pool.Dispose()

$results | Export-Csv -Path $OutputPath -NoTypeInformation -Encoding UTF8

$successes = @($results | Where-Object { $_.Success })
$failures = @($results | Where-Object { -not $_.Success })
$duplicates = @($successes | Group-Object NoteSerial1 | Where-Object { $_.Count -gt 1 })

$summary = [pscustomobject]@{
    Database = $databaseName
    RunId = $runId
    RequestedInvoices = $InvoicesCount
    SuccessCount = $successes.Count
    FailureCount = $failures.Count
    DuplicateSerialGroups = $duplicates.Count
    ResultFile = $OutputPath
}

$summary | Format-List

if ($failures.Count -gt 0) {
    Write-Warning "Failures detected. First 5:"
    $failures | Select-Object -First 5 | Format-List
}

if ($duplicates.Count -gt 0) {
    Write-Warning "Duplicate NoteSerial1 groups detected:"
    $duplicates | Format-Table Name, Count
}
