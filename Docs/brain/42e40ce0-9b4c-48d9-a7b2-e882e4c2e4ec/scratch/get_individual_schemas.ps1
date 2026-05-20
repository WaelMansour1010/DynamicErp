$db = "Eng"
$tables = @("projects", "project_billl", "project_bill_details", "Notes", "DOUBLE_ENTREY_VOUCHERS")
$connString = "Server=Wael\sql2019;Database=$db;User Id=sa;Password=Admin@123;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connString)

$outputFile = "C:\Users\Wael\.gemini\antigravity\brain\42e40ce0-9b4c-48d9-a7b2-e882e4c2e4ec\scratch\db_schemas.txt"
$sb = New-Object System.Text.StringBuilder

[void]$sb.AppendLine("=================================================================")
[void]$sb.AppendLine("DATABASE SCHEMAS FOR INCOMPLETE PROJECTS & EXTRACTS")
[void]$sb.AppendLine("=================================================================")

try {
    $conn.Open()
    foreach ($table in $tables) {
        [void]$sb.AppendLine("`n-----------------------------------------------------------------")
        [void]$sb.AppendLine("TABLE: $table")
        [void]$sb.AppendLine("-----------------------------------------------------------------")
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
        [void]$sb.AppendLine("ColumnName | DataType | MaxLen | IsNullable | DefaultValue")
        [void]$sb.AppendLine("--------------------------------------------------------")
        while ($reader.Read()) {
            [void]$sb.AppendLine("$($reader['COLUMN_NAME']) | $($reader['DATA_TYPE']) | $($reader['MAX_LEN']) | $($reader['IS_NULLABLE']) | $($reader['COL_DEF'])")
        }
        $reader.Close()
    }
    
    # Save to file
    [System.IO.File]::WriteAllText($outputFile, $sb.ToString())
    Write-Output "Successfully wrote schemas to $outputFile"
} catch {
    Write-Error $_.Exception.Message
} finally {
    $conn.Close()
}
