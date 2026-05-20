$projectsbillFrm = "F:\Source Code\SatriahMain\Frm\New frm\projectsbill.frm"
$outputFile = "C:\Users\Wael\.gemini\antigravity\brain\42e40ce0-9b4c-48d9-a7b2-e882e4c2e4ec\scratch\projectsbill_methods.txt"

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("=================================================================")
[void]$sb.AppendLine("SUB & FUNCTION DECLARATIONS IN projectsbill.frm")
[void]$sb.AppendLine("=================================================================")

if (Test-Path $projectsbillFrm) {
    $lines = Get-Content $projectsbillFrm
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i].Trim()
        if ($line -match "^(Private|Public|Static)?\s*(Sub|Function)\s+([a-zA-Z0-9_]+)") {
            [void]$sb.AppendLine("Line $($i+1): $line")
        }
    }
} else {
    [void]$sb.AppendLine("File not found: $projectsbillFrm")
}

[System.IO.File]::WriteAllText($outputFile, $sb.ToString())
Write-Output "Successfully wrote declarations to $outputFile"
