$projectsbillFrm = "F:\Source Code\SatriahMain\Frm\New frm\projectsbill.frm"
$outputFile = "C:\Users\Wael\.gemini\antigravity\brain\42e40ce0-9b4c-48d9-a7b2-e882e4c2e4ec\scratch\extracted_subs.txt"

$targetMethods = @(
    @{ Name = "calcnet"; StartLine = 11100 },
    @{ Name = "Calculte"; StartLine = 11210 },
    @{ Name = "Savetemp"; StartLine = 13046 },
    @{ Name = "SaveData"; StartLine = 6821 }
)

$sb = New-Object System.Text.StringBuilder

if (Test-Path $projectsbillFrm) {
    $lines = Get-Content $projectsbillFrm
    
    foreach ($method in $targetMethods) {
        $name = $method.Name
        $startIdx = $method.StartLine - 1
        
        [void]$sb.AppendLine("=================================================================")
        [void]$sb.AppendLine("EXTRACTED METHOD: $name (Original Start Line: $($method.StartLine))")
        [void]$sb.AppendLine("=================================================================")
        
        # Verify if the line matches the method signature, otherwise search near it
        $foundIdx = -1
        # Search from startIdx - 50 to startIdx + 50 just in case line numbers shifted slightly
        $searchStart = [Math]::Max(0, $startIdx - 50)
        $searchEnd = [Math]::Min($lines.Length - 1, $startIdx + 50)
        
        for ($i = $searchStart; $i -le $searchEnd; $i++) {
            if ($lines[$i] -match "(Sub|Function)\s+$name\b") {
                $foundIdx = $i
                break
            }
        }
        
        if ($foundIdx -eq -1) {
            # Search whole file as backup
            for ($i = 0; $i -lt $lines.Length; $i++) {
                if ($lines[$i] -match "(Sub|Function)\s+$name\b") {
                    $foundIdx = $i
                    break
                }
            }
        }
        
        if ($foundIdx -ne -1) {
            [void]$sb.AppendLine("Actual found line: $($foundIdx + 1)")
            # Extract until End Sub or End Function
            for ($j = $foundIdx; $j -lt $lines.Length; $j++) {
                [void]$sb.AppendLine($lines[$j])
                if ($lines[$j].Trim() -match "^End\s+(Sub|Function)") {
                    break
                }
            }
        } else {
            [void]$sb.AppendLine("Method $name not found in file.")
        }
        [void]$sb.AppendLine("`n`n")
    }
} else {
    [void]$sb.AppendLine("File not found: $projectsbillFrm")
}

[System.IO.File]::WriteAllText($outputFile, $sb.ToString())
Write-Output "Successfully extracted subroutines to $outputFile"
