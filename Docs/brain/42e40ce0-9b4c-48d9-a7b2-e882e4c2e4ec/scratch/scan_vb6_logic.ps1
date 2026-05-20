$projectsFrm = "F:\Source Code\SatriahMain\Frm\New frm\projects\projects.frm"
$projectsbillFrm = "F:\Source Code\SatriahMain\Frm\New frm\projectsbill.frm"
$outputFile = "C:\Users\Wael\.gemini\antigravity\brain\42e40ce0-9b4c-48d9-a7b2-e882e4c2e4ec\scratch\vb6_findings.txt"

$sb = New-Object System.Text.StringBuilder

function Scan-File($filePath, $fileName) {
    [void]$sb.AppendLine("=================================================================")
    [void]$sb.AppendLine("SCANNING FILE: $fileName")
    [void]$sb.AppendLine("=================================================================")
    
    if (-not (Test-Path $filePath)) {
        [void]$sb.AppendLine("File not found at: $filePath")
        return
    }
    
    $lines = Get-Content $filePath
    [void]$sb.AppendLine("Total lines: $($lines.Length)")
    
    # 1. Extract all Sub and Function declarations
    [void]$sb.AppendLine("`n--- SUB & FUNCTION DECLARATIONS ---")
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i].Trim()
        if ($line -match "^(Private|Public|Static)?\s*(Sub|Function)\s+([a-zA-Z0-9_]+)") {
            [void]$sb.AppendLine("Line $($i+1): $line")
        }
    }
    
    # 2. Search for specific interest keywords and print their context
    $keywords = @("calcnet", "Savetemp", "SaveData", "rs!", "Execute", "DOUBLE_ENTREY", "Notes", "advancedPayment", "Deduction", "VAT", "PerforValue", "PerformanceBond")
    [void]$sb.AppendLine("`n--- KEYWORDS CONTEXT SEARCH ---")
    foreach ($keyword in $keywords) {
        [void]$sb.AppendLine("`nSearching for: $keyword")
        [void]$sb.AppendLine("-----------------------------------------------------------------")
        for ($i = 0; $i -lt $lines.Length; $i++) {
            if ($lines[$i] -match [regex]::Escape($keyword)) {
                [void]$sb.AppendLine("--- Match at line $($i+1) ---")
                $start = [Math]::Max(0, $i - 5)
                $end = [Math]::Min($lines.Length - 1, $i + 5)
                for ($j = $start; $j -le $end; $j++) {
                    $prefix = if ($j -eq $i) { "> " } else { "  " }
                    [void]$sb.AppendLine("$prefix$($j+1): $($lines[$j])")
                }
            }
        }
    }
}

Scan-File $projectsFrm "projects.frm"
Scan-File $projectsbillFrm "projectsbill.frm"

[System.IO.File]::WriteAllText($outputFile, $sb.ToString())
Write-Output "Successfully wrote scanned VB6 logic to $outputFile"
