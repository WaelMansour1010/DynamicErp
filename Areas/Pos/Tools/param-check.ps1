[xml]$xml=Get-Content 'F:\Source Code\DynamicErp\Web.config'
$cs=($xml.configuration.connectionStrings.add | Where-Object {$_.name -eq 'KishnyCashConnection'}).connectionString
$conn=New-Object System.Data.SqlClient.SqlConnection($cs);$conn.Open();
foreach($sql in @(
"SELECT TOP (10) UserID, UserName, BranchId, StoreID, BoxID, Empid FROM dbo.TblUsers WHERE UserName LIKE N'EC%' ORDER BY UserID",
"SELECT TOP (20) ItemID, ItemName FROM dbo.TblItems WHERE ISNULL(ItemID,0)>0 ORDER BY ItemID",
"SELECT TOP (20) StoreID, StoreName FROM dbo.TblStoreData ORDER BY StoreID",
"SELECT TOP (20) BoxID, BoxName FROM dbo.TblBoxesData ORDER BY BoxID"
)){
  Write-Host "`n--- $sql ---"
  $cmd=$conn.CreateCommand();$cmd.CommandText=$sql;$dt=New-Object Data.DataTable;$ad=New-Object System.Data.SqlClient.SqlDataAdapter($cmd);[void]$ad.Fill($dt);$dt|Format-Table -AutoSize|Out-String -Width 220
}
$conn.Close()
