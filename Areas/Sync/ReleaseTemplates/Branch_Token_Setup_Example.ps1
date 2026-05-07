<# 
  Data Sync Pilot - branch token setup example.
  Replace placeholders at deployment time. Do not commit real token values.
#>

$BranchId = 10
$TokenEnvironmentVariableName = "SATRIAH_BRANCH_SYNC_TOKEN_$BranchId"
$TokenValue = "<token-from-central-admin>"

# Run as Administrator on the branch machine, or set the variable for the service account.
setx $TokenEnvironmentVariableName $TokenValue /M

Write-Host "Configured token environment variable: $TokenEnvironmentVariableName"
Write-Host "Token value was not printed."
