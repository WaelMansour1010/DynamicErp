# ≈⁄œ«œ«  «·„·› «·‰« Ã
$outputFile = "FULL_PROJECT_CODE.txt"

# «·«„ œ«œ«  «··Ì „Õ «ÃÌ‰Â« (“Êœ √Ê ‰ﬁ’ »—«Õ ﬂ)
$extensions = @(".cs", ".cshtml", ".config", ".json", ".xml", ".js", ".css", ".sql", ".asax", ".user")

# «·„Ã·œ«  «··Ì Â‰ Ã«Â·Â« (⁄‘«‰ «·“Õ„…)
$excludeFolders = @("bin", "obj", ".vs", ".git", "packages", "node_modules", "lib", "fonts", "images")

# Õ–› «·„·› «·ﬁœÌ„ ·Ê „ÊÃÊœ
if (Test-Path $outputFile) { Remove-Item $outputFile }

#  ÕœÌœ «·„”«— «·Õ«·Ì
$currentPath = Get-Location

#  Ã„Ì⁄ «·„·›« 
$files = Get-ChildItem -Path $currentPath -Recurse -File | Where-Object {
    $file = $_
    $isValidExtension = $extensions -contains $file.Extension
    
    # «· √ﬂœ ≈‰ «·„”«— „‘  »⁄ «·›Ê·œ—«  «·„” »⁄œ…
    $isNotExcluded = $true
    foreach ($exclude in $excludeFolders) {
        if ($file.FullName -match "\\$exclude\\") {
            $isNotExcluded = $false
            break
        }
    }

    $isValidExtension -and $isNotExcluded -and ($file.Name -ne $outputFile)
}

# «·ﬂ «»… ›Ì «·„·› «·‰’Ì
$totalFiles = $files.Count
$currentFile = 0

Write-Host "Found $totalFiles files. Starting extraction..." -ForegroundColor Green

foreach ($file in $files) {
    $currentFile++
    $percentage = [math]::Round(($currentFile / $totalFiles) * 100)
    Write-Progress -Activity "Collecting Code..." -Status "$percentage% Complete" -CurrentOperation "Reading: $($file.Name)" -PercentComplete $percentage

    # ﬂ «»… ›«’· Ê«”„ «·„·› ⁄‘«‰ √⁄—› √‰« »ﬁ—√ ›Ì‰
    Add-Content -Path $outputFile -Value "`n==============================================================================" -Encoding UTF8
    Add-Content -Path $outputFile -Value "FILE PATH: $($file.FullName.Replace($currentPath, ''))" -Encoding UTF8
    Add-Content -Path $outputFile -Value "==============================================================================`n" -Encoding UTF8

    # ﬁ—«¡… „Õ ÊÏ «·„·› Êﬂ «» Â
    try {
        $content = Get-Content -Path $file.FullName -Raw -ErrorAction Stop
        Add-Content -Path $outputFile -Value $content -Encoding UTF8
    }
    catch {
        Add-Content -Path $outputFile -Value "[Error reading file or file is binary]" -Encoding UTF8
    }
}

Write-Host "Done! File saved as $outputFile" -ForegroundColor Cyan
Start-Sleep -Seconds 3