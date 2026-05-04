$ErrorActionPreference='Stop'
[xml]$xml=Get-Content 'F:\Source Code\DynamicErp\Web.config'
$cs=($xml.configuration.connectionStrings.add | Where-Object {$_.name -eq 'KishnyCashConnection'}).connectionString
$script=Get-Content 'F:\Source Code\DynamicErp\Areas\Pos\Sql\34_POS_PerformanceStoredProcedures.sql' -Raw
$batches=[regex]::Split($script, '(?im)^\s*GO\s*$') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
$conn=New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()
$idx=0
try{
 foreach($batch in $batches){
   $idx++
   $cmd=$conn.CreateCommand();$cmd.CommandText=$batch;$cmd.CommandTimeout=600
   try{ [void]$cmd.ExecuteNonQuery(); Write-Host "Batch $idx OK" }
   catch{ throw "Batch $idx failed: $($_.Exception.Message)" }
 }
}
finally{$conn.Close()}
