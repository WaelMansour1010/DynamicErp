$db = "Eng"
$tables = @("projects", "project_billl", "project_bill_details", "Notes", "DOUBLE_ENTREY_VOUCHERS")
$connString = "Server=Wael\sql2019;Database=$db;User Id=sa;Password=Admin@123;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connString)

try {
    $conn.Open()
    foreach ($table in $tables) {
        Write-Output "========================================"
        Write-Output "TABLE SCHEMA: $table"
        Write-Output "========================================"
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = @"
            SELECT 
                C.COLUMN_NAME, 
                C.DATA_TYPE, 
                CASE 
                    WHEN C.CHARACTER_MAXIMUM_LENGTH IS NULL THEN '' 
                    ELSE CAST(C.CHARACTER_MAXIMUM_LENGTH AS VARCHAR(10)) 
                END AS MAX_LEN,
                C.IS_NULLABLE, 
                CASE 
                    WHEN C.COLUMN_DEFAULT IS NULL THEN '' 
                    ELSE C.COLUMN_DEFAULT 
                END AS COL_DEF
            FROM INFORMATION_SCHEMA.COLUMNS C
            WHERE C.TABLE_NAME = '$table'
            ORDER BY C.ORDINAL_POSITION
"@
        $reader = $cmd.ExecuteReader()
        Write-Output "ColumnName | DataType | MaxLen | IsNullable | DefaultValue"
        Write-Output "--------------------------------------------------------"
        while ($reader.Read()) {
            Write-Output "$($reader['COLUMN_NAME']) | $($reader['DATA_TYPE']) | $($reader['MAX_LEN']) | $($reader['IS_NULLABLE']) | $($reader['COL_DEF'])"
        }
        $reader.Close()
        Write-Output "`n"
    }
} catch {
    Write-Error $_.Exception.Message
} finally {
    $conn.Close()
}
