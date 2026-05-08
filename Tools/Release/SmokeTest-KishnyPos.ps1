param(
    [Parameter(Mandatory=$true)][string]$BaseUrl,
    [switch]$AllowSaveTest
)

$ErrorActionPreference = "Stop"
$issues = New-Object System.Collections.Generic.List[string]

function Add-Issue([string]$message) {
    $script:issues.Add($message) | Out-Null
}

function Test-Route([string]$path, [int[]]$AllowedStatus, [string]$ExpectedFinalPath) {
    try {
        $response = Invoke-WebRequest -Uri ($BaseUrl.TrimEnd("/") + $path) -UseBasicParsing -TimeoutSec 20 -ErrorAction Stop
        $status = [int]$response.StatusCode
        $finalPath = $response.BaseResponse.ResponseUri.AbsolutePath
    }
    catch {
        $status = $null
        $finalPath = ""
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
            $finalPath = $_.Exception.Response.ResponseUri.AbsolutePath
        }
    }

    Write-Host "$path => $status $finalPath"
    if ($AllowedStatus -notcontains $status) {
        Add-Issue "$path returned unexpected status '$status'."
    }
    if ($ExpectedFinalPath -and $finalPath -ne $ExpectedFinalPath) {
        Add-Issue "$path ended at '$finalPath', expected '$ExpectedFinalPath'."
    }
}

Test-Route "/Pos/Login" @(200) $null
Test-Route "/Pos" @(200,302) $null
Test-Route "/Pos/PosTransaction/Index" @(200,302) $null

Test-Route "/MainErp" @(403,404) $null
Test-Route "/DevStart" @(403,404) $null
Test-Route "/RunMode" @(403,404) $null

if ($AllowSaveTest) {
    Write-Warning "Save test is intentionally not implemented here. Run it manually against a safe test DB."
}

if ($issues.Count -eq 0) {
    Write-Host "GO"
    exit 0
}

Write-Host "NO-GO"
foreach ($issue in $issues) {
    Write-Host "- $issue"
}
exit 1
