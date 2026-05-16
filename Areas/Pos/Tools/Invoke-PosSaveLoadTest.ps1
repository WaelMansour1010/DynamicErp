param(
    [string]$WebConfigPath = (Join-Path (Resolve-Path "$PSScriptRoot\..\..\..").Path "Web.config"),
    [string]$ConnectionStringName = "KishnyCashConnection",
    [int]$Users = 100,
    [int]$InvoicesPerUser = 1,
    [int]$MaxDegreeOfParallelism = 100,
    [switch]$CreateDatabaseCopy,
    [string]$TestDatabaseName,
    [switch]$AllowMutatingTarget,
    [switch]$IncludeCardInvoices,
    [switch]$IncludeViolations,
    [switch]$SimulateUserFlow,
    [switch]$SkipApplyPosSqlScripts,
    [int]$DoubleClickPercent = 10,
    [int]$MaxThinkTimeMs = 1500,
    [int]$CommandTimeoutSeconds = 90
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data

function Get-ConnectionString {
    param([string]$ConfigPath, [string]$Name)

    [xml]$web = Get-Content $ConfigPath
    $node = $web.configuration.connectionStrings.add | Where-Object { $_.name -eq $Name } | Select-Object -First 1
    if (-not $node) {
        throw "Connection string '$Name' was not found in $ConfigPath."
    }

    return $node.connectionString
}

function Invoke-SqlNonQuery {
    param([string]$ConnectionString, [string]$Sql, [int]$Timeout = 300)

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $connection.Open()
    try {
        $command = $connection.CreateCommand()
        $command.CommandTimeout = $Timeout
        $command.CommandText = $Sql
        [void]$command.ExecuteNonQuery()
    }
    finally {
        $connection.Close()
    }
}

function Invoke-SqlQuery {
    param([string]$ConnectionString, [string]$Sql, [hashtable]$Parameters = @{}, [int]$Timeout = 300)

    $table = New-Object System.Data.DataTable
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $connection.Open()
    try {
        $command = $connection.CreateCommand()
        $command.CommandTimeout = $Timeout
        $command.CommandText = $Sql
        foreach ($key in $Parameters.Keys) {
            [void]$command.Parameters.AddWithValue($key, $Parameters[$key])
        }
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
        [void]$adapter.Fill($table)
    }
    finally {
        $connection.Close()
    }
    return ,$table
}

function Split-SqlBatches {
    param([string]$Sql)
    return [regex]::Split($Sql, "(?im)^\s*GO\s*$") | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
}

function Apply-PosSqlScripts {
    param([string]$ConnectionString)

    $root = Resolve-Path "$PSScriptRoot\..\Sql"
    $scripts = @(
        "30_POS_SaveTransaction_UnicodeText.sql",
        "55_POS_SaveTransaction_Allocator_Hardening.sql",
        "66_POS_SaveTransaction_DeadlockIndexes.sql",
        "79_POS_KycAvailableCards_Procedure.sql",
        "83_POS_SaveAttempt_DeadlockDiagnostics.sql",
        "84_POS_SaveAttemptDiagnostics_ScreenDetails.sql",
        "85_POS_Save_Idempotency.sql"
    )

    foreach ($script in $scripts) {
        $path = Join-Path $root $script
        if (-not (Test-Path $path)) {
            continue
        }

        $sql = Get-Content $path -Raw
        foreach ($batch in (Split-SqlBatches $sql)) {
            Invoke-SqlNonQuery -ConnectionString $ConnectionString -Sql $batch -Timeout 300
        }
    }
}

function New-TestDatabaseCopy {
    param([string]$SourceConnectionString, [string]$NewDatabaseName)

    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $SourceConnectionString
    $sourceDatabase = $builder.InitialCatalog
    if ([string]::IsNullOrWhiteSpace($NewDatabaseName)) {
        $NewDatabaseName = $sourceDatabase + "_LoadTest_" + (Get-Date -Format "yyyyMMdd_HHmmss")
    }

    $master = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $SourceConnectionString
    $master["Initial Catalog"] = "master"
    $masterConnectionString = $master.ConnectionString

    $sourceFiles = Invoke-SqlQuery -ConnectionString $masterConnectionString -Sql "SELECT type_desc, physical_name FROM sys.master_files WHERE database_id = DB_ID(N'$sourceDatabase');"
    $dataFolder = Split-Path (($sourceFiles | Where-Object { $_.type_desc -eq "ROWS" } | Select-Object -First 1).physical_name) -Parent
    $logFolder = Split-Path (($sourceFiles | Where-Object { $_.type_desc -eq "LOG" } | Select-Object -First 1).physical_name) -Parent
    $backupPath = Join-Path $dataFolder ($NewDatabaseName + ".bak")
    if (Test-Path $backupPath) {
        Remove-Item -LiteralPath $backupPath -Force
    }

    Invoke-SqlNonQuery -ConnectionString $masterConnectionString -Sql "BACKUP DATABASE [$sourceDatabase] TO DISK = N'$backupPath' WITH COPY_ONLY, INIT, COMPRESSION, STATS = 10;" -Timeout 3600

    $fileList = Invoke-SqlQuery -ConnectionString $masterConnectionString -Sql "RESTORE FILELISTONLY FROM DISK = N'$backupPath';" -Timeout 3600
    $dataLogical = ($fileList | Where-Object { $_.Type -eq "D" } | Select-Object -First 1).LogicalName
    $logLogical = ($fileList | Where-Object { $_.Type -eq "L" } | Select-Object -First 1).LogicalName
    if (-not $dataLogical -or -not $logLogical) {
        throw "Could not read logical file names from backup."
    }

    $dataPath = Join-Path $dataFolder ($NewDatabaseName + ".mdf")
    $logPath = Join-Path $logFolder ($NewDatabaseName + "_log.ldf")

    Invoke-SqlNonQuery -ConnectionString $masterConnectionString -Sql "IF DB_ID(N'$NewDatabaseName') IS NOT NULL BEGIN ALTER DATABASE [$NewDatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$NewDatabaseName]; END;" -Timeout 300
    Invoke-SqlNonQuery -ConnectionString $masterConnectionString -Sql "RESTORE DATABASE [$NewDatabaseName] FROM DISK = N'$backupPath' WITH MOVE N'$dataLogical' TO N'$dataPath', MOVE N'$logLogical' TO N'$logPath', RECOVERY, STATS = 10;" -Timeout 3600

    $builder["Initial Catalog"] = $NewDatabaseName
    return $builder.ConnectionString
}

function New-ItemsTable {
    $table = New-Object System.Data.DataTable
    [void]$table.Columns.Add("Item_ID", [int])
    [void]$table.Columns.Add("Quantity", [double])
    [void]$table.Columns.Add("Price", [double])
    [void]$table.Columns.Add("UnitId", [int])
    [void]$table.Columns.Add("ShowQty", [double])
    [void]$table.Columns.Add("QtyBySmalltUnit", [double])
    [void]$table.Columns.Add("showPrice", [double])
    [void]$table.Columns.Add("TotalPrice", [double])
    [void]$table.Columns.Add("StoreID2", [int])
    [void]$table.Columns.Add("Vat", [double])
    [void]$table.Columns.Add("Vatyo", [double])
    [void]$table.Columns.Add("discountvalue", [decimal])
    [void]$table.Columns.Add("TotalDiscountPerLine", [decimal])
    [void]$table.Columns.Add("ItemCase", [int])
    [void]$table.Columns.Add("CostPrice", [decimal])
    [void]$table.Columns.Add("SavedItemType", [int])
    return ,$table
}

function New-PaymentsTable {
    $table = New-Object System.Data.DataTable
    [void]$table.Columns.Add("PaymentID", [int])
    [void]$table.Columns.Add("Value", [double])
    [void]$table.Columns.Add("CardNo", [string])
    [void]$table.Columns.Add("MaxValue", [double])
    return ,$table
}

function Add-Parameter {
    param($Command, [string]$Name, [System.Data.SqlDbType]$Type, $Value)
    $parameter = $Command.Parameters.Add($Name, $Type)
    $parameter.Value = if ($null -eq $Value) { [DBNull]::Value } else { $Value }
}

function Invoke-PreSaveUserFlow {
    param([string]$ConnectionString, [pscustomobject]$Scenario, [int]$Timeout)

    $sql = @"
SELECT TOP (1) UserID, Empid, BranchID, StoreID, BoxID FROM dbo.TblUsers WITH (NOLOCK) WHERE UserID = @UserID;
SELECT TOP (10) Transaction_ID, NoteSerial1 FROM dbo.Transactions WITH (NOLOCK) WHERE Transaction_Type = 21 AND UserID = @UserID ORDER BY Transaction_ID DESC;
SELECT TOP (1) ItemID, ItemName FROM dbo.TblItems WITH (NOLOCK) WHERE ItemID = @ItemId;
SELECT TOP (1) BoxID, Account_Code FROM dbo.TblBoxesData WITH (NOLOCK) WHERE BoxID = @BoxID;
"@
    [void](Invoke-SqlQuery -ConnectionString $ConnectionString -Sql $sql -Parameters @{
        "@UserID" = $Scenario.UserID
        "@ItemId" = $Scenario.ItemId
        "@BoxID" = $Scenario.BoxID
    } -Timeout $Timeout)
}

function Invoke-PrintAfterSave {
    param([string]$ConnectionString, [int]$TransactionId, [int]$Timeout)

    if ($TransactionId -le 0) { return }
    $sql = @"
SELECT TOP (1) * FROM dbo.Transactions WITH (NOLOCK) WHERE Transaction_ID = @TransactionId;
SELECT * FROM dbo.Transaction_Details WITH (NOLOCK) WHERE Transaction_ID = @TransactionId;
SELECT * FROM dbo.Notes WITH (NOLOCK) WHERE Transaction_ID = @TransactionId;
"@
    [void](Invoke-SqlQuery -ConnectionString $ConnectionString -Sql $sql -Parameters @{
        "@TransactionId" = $TransactionId
    } -Timeout $Timeout)
}

function Invoke-SaveTransaction {
    param([string]$ConnectionString, [pscustomobject]$Scenario, [int]$Timeout)

    $items = New-ItemsTable
    [void]$items.Rows.Add($Scenario.ItemId, 1.0, [double]$Scenario.Fee, 1, 1.0, 1.0, [double]$Scenario.Fee, [double]$Scenario.TotalFee, [DBNull]::Value, [double]$Scenario.Vat, [double]$Scenario.VatPercent, 0, 0, 1, 0, $Scenario.SavedItemType)

    $payments = New-PaymentsTable
    [void]$payments.Rows.Add(1, [double]$Scenario.PayedValue, $Scenario.CardToken, [double]$Scenario.PayedValue)

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $connection.Open()
    try {
        $command = New-Object System.Data.SqlClient.SqlCommand "dbo.usp_POS_SaveTransaction", $connection
        $command.CommandType = [System.Data.CommandType]::StoredProcedure
        $command.CommandTimeout = $Timeout

        Add-Parameter $command "@TransactionDate" ([System.Data.SqlDbType]::SmallDateTime) ([DateTime]::Today)
        Add-Parameter $command "@BranchId" ([System.Data.SqlDbType]::Int) $Scenario.BranchId
        Add-Parameter $command "@StoreID" ([System.Data.SqlDbType]::Int) $Scenario.StoreID
        Add-Parameter $command "@UserID" ([System.Data.SqlDbType]::Int) $Scenario.UserID
        Add-Parameter $command "@Emp_ID" ([System.Data.SqlDbType]::Int) $Scenario.EmpID
        Add-Parameter $command "@CustomerID" ([System.Data.SqlDbType]::Int) 2
        Add-Parameter $command "@PaymentType" ([System.Data.SqlDbType]::Int) 1
        Add-Parameter $command "@BoxID" ([System.Data.SqlDbType]::Int) $Scenario.BoxID
        Add-Parameter $command "@PayedValue" ([System.Data.SqlDbType]::Money) $Scenario.PayedValue
        Add-Parameter $command "@NetValue" ([System.Data.SqlDbType]::Money) $Scenario.NetValue
        Add-Parameter $command "@RemainValue" ([System.Data.SqlDbType]::Money) 0
        Add-Parameter $command "@PaymentNetid" ([System.Data.SqlDbType]::Int) $null
        Add-Parameter $command "@IsCashOut" ([System.Data.SqlDbType]::Bit) $Scenario.IsCashOut
        Add-Parameter $command "@IsPOS" ([System.Data.SqlDbType]::Bit) $Scenario.IsPOS
        Add-Parameter $command "@OtherItems" ([System.Data.SqlDbType]::Bit) $false
        Add-Parameter $command "@PayType" ([System.Data.SqlDbType]::Int) 1
        Add-Parameter $command "@POSBillType" ([System.Data.SqlDbType]::Int) 0
        Add-Parameter $command "@STableID" ([System.Data.SqlDbType]::Int) $null
        Add-Parameter $command "@SessionD" ([System.Data.SqlDbType]::Int) $null
        Add-Parameter $command "@BillBasedOn" ([System.Data.SqlDbType]::Int) $null
        Add-Parameter $command "@CashCustomerName" ([System.Data.SqlDbType]::NVarChar) ("LOADTEST " + $Scenario.Ordinal)
        Add-Parameter $command "@CashCustomerPhone" ([System.Data.SqlDbType]::NVarChar) ("0109" + $Scenario.Ordinal.ToString("0000000"))
        Add-Parameter $command "@Phone2" ([System.Data.SqlDbType]::NVarChar) $null
        Add-Parameter $command "@IPN" ([System.Data.SqlDbType]::NVarChar) ("LT-ID-" + $Scenario.RunId + "-" + $Scenario.Ordinal)
        Add-Parameter $command "@ManualNO" ([System.Data.SqlDbType]::NVarChar) ("LT-IPN-" + $Scenario.RunId + "-" + $Scenario.Ordinal)
        Add-Parameter $command "@NoID" ([System.Data.SqlDbType]::VarChar) $Scenario.ClientRequestId
        Add-Parameter $command "@ManualNo2" ([System.Data.SqlDbType]::NVarChar) "Load test"
        Add-Parameter $command "@VisaNumber" ([System.Data.SqlDbType]::NVarChar) $Scenario.CardToken
        Add-Parameter $command "@RechargeValue" ([System.Data.SqlDbType]::Float) $Scenario.RechargeValue
        Add-Parameter $command "@Tet_NumPoket" ([System.Data.SqlDbType]::Float) $Scenario.TetNumPoket
        Add-Parameter $command "@AccountTypeName1" ([System.Data.SqlDbType]::NVarChar) $Scenario.AccountTypeName1
        Add-Parameter $command "@TrafficViolations" ([System.Data.SqlDbType]::Bit) $Scenario.TrafficViolations
        Add-Parameter $command "@ViolationsValue" ([System.Data.SqlDbType]::Float) $(if ($Scenario.TrafficViolations) { $Scenario.RechargeValue } else { $null })
        Add-Parameter $command "@ItemIDService" ([System.Data.SqlDbType]::Int) $Scenario.ItemId
        Add-Parameter $command "@ItemIDService2" ([System.Data.SqlDbType]::Int) $Scenario.ItemId2
        Add-Parameter $command "@isRecharg" ([System.Data.SqlDbType]::Bit) $Scenario.IsRecharge
        Add-Parameter $command "@IsWallet" ([System.Data.SqlDbType]::Bit) $Scenario.IsWallet
        Add-Parameter $command "@HaveGuarantee" ([System.Data.SqlDbType]::Bit) $false
        Add-Parameter $command "@CardSerial" ([System.Data.SqlDbType]::NVarChar) $Scenario.CardToken
        Add-Parameter $command "@ExistingTransactionID" ([System.Data.SqlDbType]::Int) $null
        Add-Parameter $command "@Prefix" ([System.Data.SqlDbType]::VarChar) $null

        $itemsParameter = $command.Parameters.Add("@Items", [System.Data.SqlDbType]::Structured)
        $itemsParameter.TypeName = "dbo.POS_TransactionItems"
        $itemsParameter.Value = $items
        $paymentsParameter = $command.Parameters.Add("@SalesPayments", [System.Data.SqlDbType]::Structured)
        $paymentsParameter.TypeName = "dbo.POS_SalesPayments"
        $paymentsParameter.Value = $payments

        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $reader = $command.ExecuteReader()
        try {
            [void]$reader.Read()
            $transactionId = [int]$reader["Transaction_ID"]
        }
        finally {
            $reader.Close()
        }
        $sw.Stop()

            return [pscustomobject]@{
                Success = $true
                Ordinal = $Scenario.Ordinal
                ClientRequestId = $Scenario.ClientRequestId
                UserID = $Scenario.UserID
                BranchId = $Scenario.BranchId
                StoreID = $Scenario.StoreID
                ServiceType = $Scenario.ServiceType
                TransactionId = $transactionId
            DurationMs = $sw.ElapsedMilliseconds
            SqlErrorNumber = $null
            Error = $null
        }
    }
    catch [System.Data.SqlClient.SqlException] {
        $ex = $_.Exception
        $number = if ($ex.Errors.Count -gt 0) { $ex.Errors[0].Number } else { $ex.Number }
        return [pscustomobject]@{
            Success = $false
            Ordinal = $Scenario.Ordinal
            ClientRequestId = $Scenario.ClientRequestId
            UserID = $Scenario.UserID
            BranchId = $Scenario.BranchId
            StoreID = $Scenario.StoreID
            ServiceType = $Scenario.ServiceType
            TransactionId = $null
            DurationMs = $null
            SqlErrorNumber = $number
            Error = $ex.Message
        }
    }
    catch {
        return [pscustomobject]@{
            Success = $false
            Ordinal = $Scenario.Ordinal
            ClientRequestId = $Scenario.ClientRequestId
            UserID = $Scenario.UserID
            BranchId = $Scenario.BranchId
            StoreID = $Scenario.StoreID
            ServiceType = $Scenario.ServiceType
            TransactionId = $null
            DurationMs = $null
            SqlErrorNumber = $null
            Error = $_.Exception.Message
        }
    }
    finally {
        if ($connection.State -eq "Open") {
            $connection.Close()
        }
    }
}

function Invoke-SaveTransactionWithRetry {
    param([string]$ConnectionString, [pscustomobject]$Scenario, [int]$Timeout, [bool]$SimulateFlow, [int]$MaxThinkTime)

    $delays = @(200, 500, 1000, 2000, 3500, 5000, 8000, 12000)
    if ($SimulateFlow) {
        if ($MaxThinkTime -gt 0) { Start-Sleep -Milliseconds (Get-Random -Minimum 0 -Maximum $MaxThinkTime) }
        Invoke-PreSaveUserFlow -ConnectionString $ConnectionString -Scenario $Scenario -Timeout $Timeout
        if ($MaxThinkTime -gt 0) { Start-Sleep -Milliseconds (Get-Random -Minimum 0 -Maximum $MaxThinkTime) }
    }

    for ($attempt = 1; $attempt -le ($delays.Count + 1); $attempt++) {
        $result = Invoke-SaveTransaction -ConnectionString $ConnectionString -Scenario $Scenario -Timeout $Timeout
        if ($result.Success) {
            $result | Add-Member -NotePropertyName Attempts -NotePropertyValue $attempt -Force
            $result | Add-Member -NotePropertyName DuplicateSubmitted -NotePropertyValue ([bool]$Scenario.DoubleClick) -Force
            if ($Scenario.DoubleClick) {
                if ($MaxThinkTime -gt 0) { Start-Sleep -Milliseconds (Get-Random -Minimum 0 -Maximum ([Math]::Min($MaxThinkTime, 300))) }
                $duplicate = Invoke-SaveTransaction -ConnectionString $ConnectionString -Scenario $Scenario -Timeout $Timeout
                $result | Add-Member -NotePropertyName DuplicateSuccess -NotePropertyValue ([bool]$duplicate.Success) -Force
                $result | Add-Member -NotePropertyName DuplicateTransactionId -NotePropertyValue $duplicate.TransactionId -Force
                $result | Add-Member -NotePropertyName DuplicateSqlErrorNumber -NotePropertyValue $duplicate.SqlErrorNumber -Force
                $result | Add-Member -NotePropertyName DuplicateError -NotePropertyValue $duplicate.Error -Force
            }
            else {
                $result | Add-Member -NotePropertyName DuplicateSuccess -NotePropertyValue $false -Force
                $result | Add-Member -NotePropertyName DuplicateTransactionId -NotePropertyValue $null -Force
                $result | Add-Member -NotePropertyName DuplicateSqlErrorNumber -NotePropertyValue $null -Force
                $result | Add-Member -NotePropertyName DuplicateError -NotePropertyValue $null -Force
            }
            if ($SimulateFlow) {
                Invoke-PrintAfterSave -ConnectionString $ConnectionString -TransactionId $result.TransactionId -Timeout $Timeout
            }
            return $result
        }

        $isRetriable = $result.SqlErrorNumber -eq 1205 -or (($result.Error -as [string]) -match "deadlocked on lock resources|Unable to allocate .*deadlocked|sys\.sp_getapplock")
        if (-not $isRetriable -or $attempt -gt $delays.Count) {
            $result | Add-Member -NotePropertyName Attempts -NotePropertyValue $attempt -Force
            return $result
        }

        $jitter = [math]::Abs((($Scenario.UserID * 17) + ($Scenario.BranchId * 31) + ($attempt * 53) + [Environment]::TickCount) % 350)
        Start-Sleep -Milliseconds ($delays[$attempt - 1] + $jitter)
    }
}

$sourceConnectionString = Get-ConnectionString -ConfigPath $WebConfigPath -Name $ConnectionStringName
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $sourceConnectionString
$sourceDatabase = $builder.InitialCatalog

if ($CreateDatabaseCopy) {
    Write-Host "Creating load-test database copy from '$sourceDatabase'..." -ForegroundColor Cyan
    $targetConnectionString = New-TestDatabaseCopy -SourceConnectionString $sourceConnectionString -NewDatabaseName $TestDatabaseName
    $targetBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $targetConnectionString
    Write-Host "Created database '$($targetBuilder.InitialCatalog)'." -ForegroundColor Cyan
}
else {
    $targetConnectionString = $sourceConnectionString
    $targetBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $targetConnectionString
    if (-not [string]::IsNullOrWhiteSpace($TestDatabaseName)) {
        $targetBuilder["Initial Catalog"] = $TestDatabaseName
        $targetConnectionString = $targetBuilder.ConnectionString
    }
    if (-not $AllowMutatingTarget) {
        throw "This test inserts real POS invoices into '$($targetBuilder.InitialCatalog)'. Use -CreateDatabaseCopy or pass -AllowMutatingTarget intentionally."
    }
}

if (-not $SkipApplyPosSqlScripts) {
    Apply-PosSqlScripts -ConnectionString $targetConnectionString
}

$runId = Get-Date -Format "yyyyMMddHHmmss"
$candidateSql = @"
SELECT TOP ($Users)
    t.UserID,
    COALESCE(t.Emp_ID, u.Empid) AS EmpID,
    t.BranchId,
    t.StoreID,
    t.BoxID
FROM dbo.Transactions t WITH (NOLOCK)
LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = t.UserID
INNER JOIN dbo.TblStore s WITH (NOLOCK) ON s.StoreID = t.StoreID
INNER JOIN dbo.TblBoxesData bx WITH (NOLOCK) ON bx.BoxID = t.BoxID
WHERE t.Transaction_Type = 21
  AND ISNULL(t.IsCancelled, 0) = 0
  AND t.UserID IS NOT NULL
  AND COALESCE(t.Emp_ID, u.Empid) IS NOT NULL
  AND t.BranchId IS NOT NULL
  AND t.StoreID IS NOT NULL
  AND t.BoxID IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(ISNULL(bx.Account_Code, N''))), N'') IS NOT NULL
ORDER BY NEWID();
"@
$usersTable = Invoke-SqlQuery -ConnectionString $targetConnectionString -Sql $candidateSql
if ($usersTable.Rows.Count -eq 0) {
    throw "No valid POS users with branch/store/box were found."
}

$cardTokens = @()
if ($IncludeCardInvoices) {
    $cardSql = @"
;WITH CandidateCustomers AS
(
    SELECT TOP ($([Math]::Max($Users * 10, 200)))
        Token = LTRIM(RTRIM(ISNULL(c.CardNo, N'')))
    FROM dbo.TblCusCsh c WITH (NOLOCK)
    WHERE ISNULL(c.EasyCashType, 0) = 0
      AND NULLIF(LTRIM(RTRIM(ISNULL(c.CardNo, N''))), N'') IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.Transactions t WITH (NOLOCK)
          WHERE t.Transaction_Type = 21
            AND ISNULL(t.IsCancelled, 0) = 0
            AND LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))) = LTRIM(RTRIM(ISNULL(c.CardNo, N'')))
      )
    ORDER BY c.Id DESC
)
SELECT TOP ($Users)
    CardToken = cc.Token,
    st.StoreID
FROM CandidateCustomers cc
CROSS APPLY
(
    SELECT TOP (1)
        t.StoreID,
        AvailableQty = SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)),
        LastTransactionId = MAX(t.Transaction_ID)
    FROM dbo.Transaction_Details td WITH (NOLOCK)
    INNER JOIN dbo.Transactions t WITH (NOLOCK) ON t.Transaction_ID = td.Transaction_ID
    INNER JOIN dbo.TransactionTypes tt WITH (NOLOCK) ON tt.Transaction_Type = t.Transaction_Type
    WHERE LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = cc.Token
      AND ISNULL(tt.StockEffect, 0) <> 0
    GROUP BY t.StoreID
    HAVING SUM(ISNULL(td.Quantity, 0) * ISNULL(tt.StockEffect, 0)) > 0
    ORDER BY MAX(t.Transaction_ID) DESC
) st
ORDER BY st.LastTransactionId DESC;
"@
    $cardTokens = @(Invoke-SqlQuery -ConnectionString $targetConnectionString -Sql $cardSql -Timeout 120)
}

$totalRequests = $Users * $InvoicesPerUser
$scenarios = New-Object System.Collections.Generic.List[object]
for ($i = 1; $i -le $totalRequests; $i++) {
    $u = $usersTable.Rows.Item(($i - 1) % $usersTable.Rows.Count)
    $serviceSlot = $i % 4
    $serviceType = if ($serviceSlot -eq 0 -and $IncludeCardInvoices -and $cardTokens.Count -gt 0) { "card" } elseif ($serviceSlot -eq 1) { "cash-out" } elseif ($serviceSlot -eq 2 -and $IncludeViolations) { "violations" } else { "cash-in" }
    $cardRow = $null
    if ($serviceType -eq "card") {
        $cardRow = $cardTokens[($i - 1) % $cardTokens.Count]
    }

    $itemId = if ($serviceType -eq "card") { 19 } elseif ($serviceType -eq "cash-out") { 10 } elseif ($serviceType -eq "violations") { 2 } else { 2 }
    $fee = if ($serviceType -eq "card") { 131.58 } elseif ($serviceType -eq "cash-out") { 30.00 } elseif ($serviceType -eq "violations") { 50.00 } else { 35.00 }
    $vat = if ($serviceType -eq "card") { 18.42 } elseif ($serviceType -eq "cash-out" -or $serviceType -eq "violations") { 0.00 } else { 4.90 }
    $totalFee = $fee + $vat

    $scenario = [pscustomobject]@{
        RunId = $runId
        Ordinal = $i
        UserID = [int]$u["UserID"]
        EmpID = [int]$u["EmpID"]
        BranchId = [int]$u["BranchId"]
        StoreID = if ($cardRow -ne $null) { [int]$cardRow["StoreID"] } else { [int]$u["StoreID"] }
        BoxID = [int]$u["BoxID"]
        ServiceType = $serviceType
        ItemId = $itemId
        ItemId2 = if ($serviceType -eq "cash-out") { 1 } else { $null }
        Fee = $fee
        Vat = $vat
        VatPercent = if ($vat -gt 0) { 14 } else { 0 }
        TotalFee = $totalFee
        PayedValue = $totalFee
        NetValue = $totalFee
        RechargeValue = if ($serviceType -eq "cash-in") { 1000 + $i } elseif ($serviceType -eq "cash-out") { 400 } else { 0 }
        TetNumPoket = if ($serviceType -eq "cash-in") { [double]("10" + $i.ToString("000000000")) } else { $null }
        AccountTypeName1 = if ($serviceType -eq "cash-out") { "010" + $i.ToString("00000000") } else { $null }
        IsCashOut = $serviceType -eq "cash-out"
        IsWallet = $serviceType -eq "cash-out"
        IsRecharge = $serviceType -eq "cash-in"
        IsPOS = $serviceType -eq "card"
        TrafficViolations = $serviceType -eq "violations"
        DoubleClick = (Get-Random -Minimum 1 -Maximum 101) -le $DoubleClickPercent
        ClientRequestId = ([guid]::NewGuid()).ToString()
        CardToken = if ($cardRow -ne $null) { [string]$cardRow["CardToken"] } else { $null }
        SavedItemType = if ($serviceType -eq "card") { 2 } else { 0 }
    }
    $scenarios.Add($scenario)
}

Write-Host "Starting POS load test. Database=$($targetBuilder.InitialCatalog); Requests=$totalRequests; Parallel=$MaxDegreeOfParallelism; Cards=$IncludeCardInvoices" -ForegroundColor Cyan
$overall = [System.Diagnostics.Stopwatch]::StartNew()
$pool = [runspacefactory]::CreateRunspacePool(1, $MaxDegreeOfParallelism)
$pool.Open()
$jobs = @()
$functionText =
    "function Add-Parameter {`n" + ${function:Add-Parameter}.ToString() + "`n}`n" +
    "function New-ItemsTable {`n" + ${function:New-ItemsTable}.ToString() + "`n}`n" +
    "function New-PaymentsTable {`n" + ${function:New-PaymentsTable}.ToString() + "`n}`n" +
    "function Invoke-SqlQuery {`n" + ${function:Invoke-SqlQuery}.ToString() + "`n}`n" +
    "function Invoke-PreSaveUserFlow {`n" + ${function:Invoke-PreSaveUserFlow}.ToString() + "`n}`n" +
    "function Invoke-PrintAfterSave {`n" + ${function:Invoke-PrintAfterSave}.ToString() + "`n}`n" +
    "function Invoke-SaveTransaction {`n" + ${function:Invoke-SaveTransaction}.ToString() + "`n}`n" +
    "function Invoke-SaveTransactionWithRetry {`n" + ${function:Invoke-SaveTransactionWithRetry}.ToString() + "`n}`n"

foreach ($scenario in $scenarios) {
    $ps = [powershell]::Create()
    $ps.RunspacePool = $pool
    [void]$ps.AddScript("param(`$loadTestConnectionString, `$loadTestScenario, `$loadTestTimeout, `$loadTestSimulateFlow, `$loadTestMaxThinkTime)`n" + $functionText + "`nInvoke-SaveTransactionWithRetry -ConnectionString `$loadTestConnectionString -Scenario `$loadTestScenario -Timeout `$loadTestTimeout -SimulateFlow `$loadTestSimulateFlow -MaxThinkTime `$loadTestMaxThinkTime")
    [void]$ps.AddArgument($targetConnectionString)
    [void]$ps.AddArgument($scenario)
    [void]$ps.AddArgument($CommandTimeoutSeconds)
    [void]$ps.AddArgument([bool]$SimulateUserFlow)
    [void]$ps.AddArgument($MaxThinkTimeMs)
    $jobs += [pscustomobject]@{ PowerShell = $ps; Handle = $ps.BeginInvoke() }
}

$results = New-Object System.Collections.Generic.List[object]
foreach ($job in $jobs) {
    try {
        $output = $job.PowerShell.EndInvoke($job.Handle)
        foreach ($row in $output) {
            $results.Add($row)
        }
    }
    finally {
        $job.PowerShell.Dispose()
    }
}
$pool.Close()
$pool.Dispose()
$overall.Stop()

$successIds = @($results | Where-Object { $_.Success -and $_.TransactionId } | ForEach-Object { [int]$_.TransactionId })
$idList = if ($successIds.Count -gt 0) { ($successIds -join ",") } else { "-1" }
$verificationSql = @"
SELECT
    SavedTransactions = COUNT(DISTINCT t.Transaction_ID),
    DetailRows = (SELECT COUNT(1) FROM dbo.Transaction_Details td WITH (NOLOCK) WHERE td.Transaction_ID IN ($idList)),
    NotesRows = (SELECT COUNT(1) FROM dbo.Notes n WITH (NOLOCK) WHERE n.Transaction_ID IN ($idList)),
    DevRows = (SELECT COUNT(1) FROM dbo.DOUBLE_ENTREY_VOUCHERS dev WITH (NOLOCK) WHERE dev.Transaction_ID IN ($idList)),
    IssueVouchers = (SELECT COUNT(1) FROM dbo.Transactions issueT WITH (NOLOCK) WHERE issueT.Transaction_Type = 19 AND issueT.Transaction_ID IN (SELECT CASE WHEN ISNUMERIC(NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N'')) = 1 THEN CONVERT(INT, NULLIF(LTRIM(RTRIM(ISNULL(t.NOTS, N''))), N'')) ELSE NULL END FROM dbo.Transactions t WITH (NOLOCK) WHERE t.Transaction_ID IN ($idList)))
FROM dbo.Transactions t WITH (NOLOCK)
WHERE t.Transaction_ID IN ($idList);
"@
$verification = Invoke-SqlQuery -ConnectionString $targetConnectionString -Sql $verificationSql

$deadlocks = @($results | Where-Object { $_.SqlErrorNumber -eq 1205 })
$timeouts = @($results | Where-Object { $_.SqlErrorNumber -eq -2 })
$failed = @($results | Where-Object { -not $_.Success })
$durations = @($results | Where-Object { $_.Success -and $_.DurationMs -ne $null } | ForEach-Object { [long]$_.DurationMs })

$summary = [pscustomobject]@{
    Database = $targetBuilder.InitialCatalog
    RunId = $runId
    Requests = $totalRequests
    Success = @($results | Where-Object { $_.Success }).Count
    Failed = $failed.Count
    Deadlocks = $deadlocks.Count
    Timeouts = $timeouts.Count
    TotalDurationMs = $overall.ElapsedMilliseconds
    AvgSuccessMs = if ($durations.Count -gt 0) { [math]::Round(($durations | Measure-Object -Average).Average, 2) } else { $null }
    MaxSuccessMs = if ($durations.Count -gt 0) { ($durations | Measure-Object -Maximum).Maximum } else { $null }
    RetriedSuccess = @($results | Where-Object { $_.Success -and $_.Attempts -gt 1 }).Count
    MaxAttempts = if ($results.Count -gt 0) { ($results | Measure-Object -Property Attempts -Maximum).Maximum } else { $null }
    DuplicateSubmits = @($results | Where-Object { $_.DuplicateSubmitted }).Count
    DuplicateCreatedInvoices = @($results | Where-Object { $_.DuplicateSuccess -and $_.DuplicateTransactionId }).Count
    SavedTransactions = if ($verification.Rows.Count -gt 0) { $verification.Rows[0].SavedTransactions } else { 0 }
    DetailRows = if ($verification.Rows.Count -gt 0) { $verification.Rows[0].DetailRows } else { 0 }
    NotesRows = if ($verification.Rows.Count -gt 0) { $verification.Rows[0].NotesRows } else { 0 }
    DevRows = if ($verification.Rows.Count -gt 0) { $verification.Rows[0].DevRows } else { 0 }
    IssueVouchers = if ($verification.Rows.Count -gt 0) { $verification.Rows[0].IssueVouchers } else { 0 }
}

$outDir = Join-Path (Resolve-Path "$PSScriptRoot\..\..\..\_Releases").Path ("POS_LoadTest_" + $runId)
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$summary | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $outDir "summary.json") -Encoding UTF8
$results | Export-Csv (Join-Path $outDir "results.csv") -NoTypeInformation -Encoding UTF8
$failed | Select-Object -First 50 | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $outDir "failed-sample.json") -Encoding UTF8

$summary | Format-List
Write-Host "Artifacts: $outDir" -ForegroundColor Cyan
