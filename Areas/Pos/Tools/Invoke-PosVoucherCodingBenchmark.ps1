param(
    [string]$ConnectionString,
    [string]$Server = "Wael\Sql2019",
    [string]$Database = "Cash_FullSaveDEV_20260514",
    [string]$User = "sa",
    [string]$Password = "Admin@123",
    [int[]]$UserLevels = @(10, 25, 50, 100, 150),
    [int]$IterationsPerUser = 3,
    [int]$BranchId = 45,
    [int]$StoreId = 44,
    [datetime]$TransactionDate = (Get-Date),
    [ValidateSet("", "Company", "Branch", "BranchStore")]
    [string]$SerialScope = "",
    [switch]$IncludeIssueVoucher,
    [switch]$MultiBranch,
    [int]$MultiBranchSpreadCount = 100,
    [switch]$AllowMutatingTarget,
    [int]$CommandTimeoutSeconds = 60,
    [string]$OutputDirectory
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

if (-not $AllowMutatingTarget) {
    throw "This benchmark increments dbo.SerialCounters_V2. Run only on a copied/test database and pass -AllowMutatingTarget."
}

if (-not $ConnectionString) {
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder["Data Source"] = $Server
    $builder["Initial Catalog"] = $Database
    $builder["User ID"] = $User
    $builder["Password"] = $Password
    $builder["MultipleActiveResultSets"] = $false
    $builder["Application Name"] = "POS VoucherCoding Benchmark"
    $ConnectionString = $builder.ConnectionString
}

if (-not $OutputDirectory) {
    $runId = Get-Date -Format "yyyyMMdd_HHmmss"
    $OutputDirectory = Join-Path (Resolve-Path ".").Path "_Releases\POS_VoucherCodingBenchmark_$runId"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function Invoke-ScalarSql {
    param([string]$ConnString, [string]$SqlText)
    $conn = New-Object System.Data.SqlClient.SqlConnection $ConnString
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $SqlText
        $cmd.CommandTimeout = 60
        return $cmd.ExecuteScalar()
    }
    finally {
        if ($conn.State -ne [System.Data.ConnectionState]::Closed) { $conn.Close() }
        $conn.Dispose()
    }
}

function Invoke-NonQuerySql {
    param([string]$ConnString, [string]$SqlText)
    $conn = New-Object System.Data.SqlClient.SqlConnection $ConnString
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $SqlText
        $cmd.CommandTimeout = 60
        [void]$cmd.ExecuteNonQuery()
    }
    finally {
        if ($conn.State -ne [System.Data.ConnectionState]::Closed) { $conn.Close() }
        $conn.Dispose()
    }
}

function Get-BranchStorePairs {
    param([string]$ConnString, [int]$Take, [int]$FallbackBranch, [int]$FallbackStore)
    $pairs = New-Object System.Collections.Generic.List[object]
    $conn = New-Object System.Data.SqlClient.SqlConnection $ConnString
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = @"
SELECT TOP (@Take)
    BranchId,
    StoreID
FROM
(
    SELECT
        BranchId,
        StoreID = ISNULL(StoreID, 0),
        RowsCnt = COUNT_BIG(*)
    FROM dbo.Transactions WITH (NOLOCK)
    WHERE Transaction_Type = 21
      AND BranchId IS NOT NULL
      AND Transaction_Date >= DATEADD(MONTH, -3, GETDATE())
    GROUP BY BranchId, ISNULL(StoreID, 0)
) x
ORDER BY RowsCnt DESC, BranchId, StoreID;
"@
        [void]$cmd.Parameters.Add("@Take", [System.Data.SqlDbType]::Int)
        $cmd.Parameters["@Take"].Value = $Take
        $reader = $cmd.ExecuteReader()
        while ($reader.Read()) {
            $pairs.Add([pscustomobject]@{
                BranchId = [int]$reader["BranchId"]
                StoreId = [int]$reader["StoreID"]
            })
        }
        $reader.Close()
    }
    finally {
        if ($conn.State -ne [System.Data.ConnectionState]::Closed) { $conn.Close() }
        $conn.Dispose()
    }

    if ($pairs.Count -eq 0) {
        $pairs.Add([pscustomobject]@{ BranchId = $FallbackBranch; StoreId = $FallbackStore })
    }

    $pairs
}

if ($SerialScope) {
    Invoke-NonQuerySql -ConnString $ConnectionString -SqlText @"
IF COL_LENGTH(N'dbo.TblOptions', N'POSVoucherSerialScope') IS NULL
    ALTER TABLE dbo.TblOptions ADD POSVoucherSerialScope NVARCHAR(20) NULL;
UPDATE dbo.TblOptions SET POSVoucherSerialScope = N'$SerialScope';
"@
}

$configuredScope = Invoke-ScalarSql -ConnString $ConnectionString -SqlText @"
DECLARE @scope NVARCHAR(20);
SET @scope = N'Company';
IF COL_LENGTH(N'dbo.TblOptions', N'POSVoucherSerialScope') IS NOT NULL
BEGIN
    EXEC sp_executesql
        N'SELECT TOP (1) @scopeOut = POSVoucherSerialScope FROM dbo.TblOptions',
        N'@scopeOut NVARCHAR(20) OUTPUT',
        @scopeOut = @scope OUTPUT;
END;
SELECT ISNULL(@scope, N'Company');
"@

$branchStorePairs = @(Get-BranchStorePairs -ConnString $ConnectionString -Take $MultiBranchSpreadCount -FallbackBranch $BranchId -FallbackStore $StoreId)

function Invoke-VoucherCodingCall {
    param(
        [string]$ConnString,
        [int]$Branch,
        [int]$Store,
        [int]$SanadNo,
        [int]$NoteType,
        [int]$TransactionType,
        [datetime]$DateValue,
        [int]$UserId,
        [int]$TimeoutSeconds
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $result = [ordered]@{
        StartedAt = (Get-Date).ToString("o")
        BranchId = $Branch
        StoreId = $Store
        SanadNo = $SanadNo
        NoteType = $NoteType
        TransactionType = $TransactionType
        UserId = $UserId
        Success = $false
        ReturnCode = $null
        Serial = $null
        Tail = $null
        DurationMs = 0
        ErrorNumber = $null
        ErrorMessage = $null
    }

    $conn = New-Object System.Data.SqlClient.SqlConnection $ConnString
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandType = [System.Data.CommandType]::StoredProcedure
        $cmd.CommandText = "dbo.usp_Voucher_coding_V2"
        $cmd.CommandTimeout = $TimeoutSeconds

        $p = $cmd.Parameters.Add("@my_branch", [System.Data.SqlDbType]::Int); $p.Value = $Branch
        $p = $cmd.Parameters.Add("@date1", [System.Data.SqlDbType]::Date); $p.Value = $DateValue.Date
        $p = $cmd.Parameters.Add("@Sanad_No", [System.Data.SqlDbType]::Int); $p.Value = $SanadNo
        $p = $cmd.Parameters.Add("@NoteType", [System.Data.SqlDbType]::Int); $p.Value = $NoteType
        $p = $cmd.Parameters.Add("@departement_name", [System.Data.SqlDbType]::Int); $p.Value = 1
        $p = $cmd.Parameters.Add("@Transaction_Type", [System.Data.SqlDbType]::Int); $p.Value = $TransactionType
        $p = $cmd.Parameters.Add("@Prefix", [System.Data.SqlDbType]::VarChar, 10); $p.Value = [DBNull]::Value
        $p = $cmd.Parameters.Add("@StoreID", [System.Data.SqlDbType]::Int); $p.Value = $Store
        $p = $cmd.Parameters.Add("@BillType", [System.Data.SqlDbType]::Int); $p.Value = 0
        $p = $cmd.Parameters.Add("@MosemID", [System.Data.SqlDbType]::BigInt); $p.Value = 0
        $p = $cmd.Parameters.Add("@mTableName", [System.Data.SqlDbType]::VarChar, 100); $p.Value = [DBNull]::Value
        $p = $cmd.Parameters.Add("@mUserID", [System.Data.SqlDbType]::BigInt); $p.Value = $UserId

        $serialParam = $cmd.Parameters.Add("@Result", [System.Data.SqlDbType]::VarChar, 50)
        $serialParam.Direction = [System.Data.ParameterDirection]::Output

        $tailParam = $cmd.Parameters.Add("@mSerInv", [System.Data.SqlDbType]::BigInt)
        $tailParam.Direction = [System.Data.ParameterDirection]::Output

        $returnParam = $cmd.Parameters.Add("@ReturnValue", [System.Data.SqlDbType]::Int)
        $returnParam.Direction = [System.Data.ParameterDirection]::ReturnValue

        [void]$cmd.ExecuteNonQuery()

        $result.Success = ([int]$returnParam.Value -eq 0)
        $result.ReturnCode = [int]$returnParam.Value
        $result.Serial = [string]$serialParam.Value
        if ($tailParam.Value -ne [DBNull]::Value) {
            $result.Tail = [int64]$tailParam.Value
        }
    }
    catch [System.Data.SqlClient.SqlException] {
        $result.ErrorNumber = $_.Exception.Number
        $result.ErrorMessage = $_.Exception.Message
    }
    catch {
        $result.ErrorMessage = $_.Exception.Message
    }
    finally {
        $sw.Stop()
        $result.DurationMs = [int]$sw.ElapsedMilliseconds
        if ($conn.State -ne [System.Data.ConnectionState]::Closed) {
            $conn.Close()
        }
        $conn.Dispose()
    }

    [pscustomobject]$result
}

function Get-Percentile {
    param([int64[]]$Values, [double]$Percent)
    [int64[]]$localValues = @($Values)
    if (-not $localValues -or $localValues.Length -eq 0) { return 0 }
    [int64[]]$sorted = @($localValues | Sort-Object)
    $index = [Math]::Ceiling($sorted.Length * $Percent) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $sorted.Length) { $index = $sorted.Length - 1 }
    return $sorted[$index]
}

$allResults = New-Object System.Collections.Generic.List[object]
$invokeVoucherCodingCallDefinition = "function Invoke-VoucherCodingCall {`r`n" + ${function:Invoke-VoucherCodingCall}.ToString() + "`r`n}"

foreach ($level in $UserLevels) {
    Write-Host "Running voucher coding benchmark: users=$level iterations=$IterationsPerUser"
    $runspacePool = [runspacefactory]::CreateRunspacePool(1, [Math]::Min($level, 64))
    $runspacePool.Open()
    $workers = New-Object System.Collections.Generic.List[object]

    for ($userIndex = 1; $userIndex -le $level; $userIndex++) {
        $scriptBlock = {
            param(
                $ConnString,
                $Iterations,
                $BaseBranch,
                $BaseStore,
                $DateValue,
                $IncludeIssue,
                $UseMultiBranch,
                $TimeoutSeconds,
                $UserIndex,
                $InvokeDefinition,
                $Pairs
            )

            $localResults = New-Object System.Collections.Generic.List[object]
            . ([scriptblock]::Create($InvokeDefinition))

            for ($i = 1; $i -le $Iterations; $i++) {
                $branch = $BaseBranch
                $store = $BaseStore
                if ($UseMultiBranch) {
                    $pair = $Pairs[($UserIndex - 1) % $Pairs.Length]
                    $branch = [int]$pair.BranchId
                    $store = [int]$pair.StoreId
                }

                Start-Sleep -Milliseconds (Get-Random -Minimum 0 -Maximum 90)
                $localResults.Add((Invoke-VoucherCodingCall -ConnString $ConnString -Branch $branch -Store $store -SanadNo 7 -NoteType 170 -TransactionType 21 -DateValue $DateValue -UserId (100000 + $UserIndex) -TimeoutSeconds $TimeoutSeconds))

                if ($IncludeIssue) {
                    Start-Sleep -Milliseconds (Get-Random -Minimum 0 -Maximum 50)
                    $localResults.Add((Invoke-VoucherCodingCall -ConnString $ConnString -Branch $branch -Store $store -SanadNo 10 -NoteType 180 -TransactionType 19 -DateValue $DateValue -UserId (100000 + $UserIndex) -TimeoutSeconds $TimeoutSeconds))
                }
            }

            $localResults
        }

        $ps = [powershell]::Create()
        $ps.RunspacePool = $runspacePool
        [void]$ps.AddScript($scriptBlock)
        [void]$ps.AddArgument($ConnectionString)
        [void]$ps.AddArgument($IterationsPerUser)
        [void]$ps.AddArgument($BranchId)
        [void]$ps.AddArgument($StoreId)
        [void]$ps.AddArgument($TransactionDate)
        [void]$ps.AddArgument([bool]$IncludeIssueVoucher)
        [void]$ps.AddArgument([bool]$MultiBranch)
        [void]$ps.AddArgument($CommandTimeoutSeconds)
        [void]$ps.AddArgument($userIndex)
        [void]$ps.AddArgument($invokeVoucherCodingCallDefinition)
        [void]$ps.AddArgument($branchStorePairs)

        $workers.Add([pscustomobject]@{
            PowerShell = $ps
            Handle = $ps.BeginInvoke()
        })
    }

    try {
        foreach ($worker in $workers) {
            $levelResults = $worker.PowerShell.EndInvoke($worker.Handle)
            foreach ($r in $levelResults) {
                $r | Add-Member -NotePropertyName UserLevel -NotePropertyValue $level -Force
                $r | Add-Member -NotePropertyName Mode -NotePropertyValue $(if ($MultiBranch) { "multi-branch" } else { "same-branch" }) -Force
                $r | Add-Member -NotePropertyName ConfiguredScope -NotePropertyValue $configuredScope -Force
                $allResults.Add($r)
            }
            $worker.PowerShell.Dispose()
        }
    }
    finally {
        foreach ($worker in $workers) {
            if ($worker.PowerShell) {
                $worker.PowerShell.Dispose()
            }
        }
        $runspacePool.Close()
        $runspacePool.Dispose()
    }
}

$csvPath = Join-Path $OutputDirectory "voucher-coding-results.csv"
$allResults | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8

$summary = $allResults |
    Group-Object UserLevel, TransactionType |
    ForEach-Object {
        [object[]]$items = @($_.Group)
        [int64[]]$durations = @($items | ForEach-Object { [int64]$_.DurationMs })
        [object[]]$successItems = @($items | Where-Object { $_.Success })
        [object[]]$deadlockItems = @($items | Where-Object { $_.ErrorNumber -eq 1205 })
        [object[]]$timeoutItems = @($items | Where-Object { $_.ErrorNumber -eq -2 })
        [pscustomobject]@{
            UserLevel = ($items[0].UserLevel)
            TransactionType = ($items[0].TransactionType)
            ConfiguredScope = ($items[0].ConfiguredScope)
            Mode = ($items[0].Mode)
            Attempts = $items.Length
            Success = $successItems.Length
            Deadlocks = $deadlockItems.Length
            Timeouts = $timeoutItems.Length
            AvgMs = [int](($durations | Measure-Object -Average).Average)
            MaxMs = [int](($durations | Measure-Object -Maximum).Maximum)
            P95Ms = Get-Percentile -Values $durations -Percent 0.95
            P99Ms = Get-Percentile -Values $durations -Percent 0.99
        }
    } |
    Sort-Object UserLevel, TransactionType

$summaryPath = Join-Path $OutputDirectory "voucher-coding-summary.csv"
$summary | Export-Csv -Path $summaryPath -NoTypeInformation -Encoding UTF8

Write-Host "Results: $csvPath"
Write-Host "Summary: $summaryPath"
$summary | Format-Table -AutoSize
