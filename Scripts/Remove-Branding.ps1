<#
.SYNOPSIS
    Removes all "MyStore" branding references from the codebase
    
.DESCRIPTION
    Replaces all MyStore references with generic EcommerceStarter or MyStore examples
#>

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Remove MyStore Branding" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Files to process (exclude obj, bin, node_modules)
$filesToCheck = Get-ChildItem -Path . -Recurse -Include *.md,*.cs,*.cshtml,*.ps1,*.json,*.txt,*.xml -Exclude bin,obj,node_modules |
    Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*node_modules*" }

$replacements = @{
    # Company names
    "MyStore Supply Co\." = "My Store"
    "MyStore Supply Co\." = "My Store"
    "MyStore" = "MyStore"
    "MyStore" = "MyStore"
    "MyStore" = "MyStore"
    
    # Email addresses
    "contact@MyStore\.com" = "contact@example.com"
    "support@MyStore\.com" = "support@example.com"
    "noreply@MyStore\.com" = "noreply@example.com"
    "dev@MyStore\.com" = "dev@example.com"
    "host@MyStore\.com" = "host@example.com"
    "admin@MyStore\.com" = "admin@example.com"
    
    # URLs and domains
    "MyStore\.com" = "example.com"
    "wwwroot\\MyStore" = "wwwroot\MyStore"
    "wwwroot/MyStore" = "wwwroot/MyStore"
    
    # IIS Pool names
    "MyStorePool" = "MyStorePool"
    
    # Descriptions
    "Modern e-commerce platform for mushroom enthusiasts and nature lovers\. Shop caps, collars, and growing supplies\." = "Modern e-commerce platform built with ASP.NET Core. Customize and launch your online store today."
    "e-commerce, online store, asp.net core, shopping" = "e-commerce, online store, asp.net core, shopping"
}

$totalReplacements = 0
$filesModified = 0

foreach ($file in $filesToCheck) {
    try {
        $content = Get-Content $file.FullName -Raw -ErrorAction Stop
        $originalContent = $content
        $fileModified = $false
        
        foreach ($pattern in $replacements.Keys) {
            $replacement = $replacements[$pattern]
            
            if ($content -match $pattern) {
                $matches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                $count = $matches.Count
                
                if ($count -gt 0) {
                    $content = $content -replace $pattern, $replacement
                    $totalReplacements += $count
                    $fileModified = $true
                    
                    Write-Host "  ?? $($file.Name): Replaced '$pattern' ? '$replacement' ($count times)" -ForegroundColor Yellow
                }
            }
        }
        
        if ($fileModified) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
            $filesModified++
        }
    }
    catch {
        Write-Host "  ? Error processing $($file.Name): $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Files scanned: $($filesToCheck.Count)" -ForegroundColor White
Write-Host "  Files modified: $filesModified" -ForegroundColor Green
Write-Host "  Total replacements: $totalReplacements" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($filesModified -gt 0) {
    Write-Host "? Branding cleanup complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Review the changes with 'git diff'" -ForegroundColor White
    Write-Host "  2. Test the application" -ForegroundColor White
    Write-Host "  3. Commit the changes" -ForegroundColor White
}
else {
    Write-Host "? No MyStore references found!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
