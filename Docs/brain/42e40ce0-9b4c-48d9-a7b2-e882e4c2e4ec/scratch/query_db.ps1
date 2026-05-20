$connString = "Server=Wael\sql2019;Database=master;User Id=sa;Password=Admin@123;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT name FROM sys.databases"
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Output $reader[0]
    }
    $reader.Close()
} catch {
    Write-Error $_.Exception.Message
} finally {
    $conn.Close()
}
