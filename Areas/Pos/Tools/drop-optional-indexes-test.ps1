[xml]$xml=Get-Content 'F:\Source Code\DynamicErp\Web.config'
$cs=($xml.configuration.connectionStrings.add | Where-Object {$_.name -eq 'KishnyCashConnection'}).connectionString
$conn=New-Object System.Data.SqlClient.SqlConnection($cs);$conn.Open();
$indexes=@(
 @{Table='dbo.Transactions';Name='IX_POS_Transactions_TypeIPN'},
 @{Table='dbo.Transactions';Name='IX_POS_Transactions_TypeCustomerSearch'},
 @{Table='dbo.Transaction_Details';Name='IX_POS_TransactionDetails_ItemTransaction'}
)
foreach($ix in $indexes){
 $sql="IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'$($ix.Table)') AND name = N'$($ix.Name)') DROP INDEX [$($ix.Name)] ON $($ix.Table);"
 $cmd=$conn.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=300;[void]$cmd.ExecuteNonQuery();Write-Host "Dropped if existed: $($ix.Table).$($ix.Name)"
}
$conn.Close()
