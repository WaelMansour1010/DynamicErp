$databases = @("Eng", "MyErp")
foreach ($db in $databases) {
    $connString = "Server=Wael\sql2019;Database=$db;User Id=sa;Password=Admin@123;TrustServerCertificate=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connString)
    try {
        $conn.Open()
        Write-Output "--- Database: $db ---"
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('projects', 'project_billl', 'project_bill_details', 'Notes', 'DOUBLE_ENTREY_VOUCHERS') ORDER BY TABLE_NAME"
        $reader = $cmd.ExecuteReader()
        while ($reader.Read()) {
            Write-Output "Found Table: $($reader[0])"
        }
        $reader.Close()
    } catch {
        Write-Error "$db Error: $_.Exception.Message"
    } finally {
        $conn.Close()
    }
}
