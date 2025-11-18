# Fix all emoji corruption in XAML files
$basePath = "C:\Dev\Websites\EcommerceStarter.Installer\Views"

# Common emoji replacements
$replacements = @{
    # Based on context clues
    "?? Existing Installation Found" = "?? Existing Installation Found"
    "?? Upgrade" = "?? Upgrade"
    "?? Upgrading" = "?? Upgrading"
    "?? Current Installation" = "?? Current Installation"
    "?? New Version" = "? New Version"
    "?? DEMO MODE" = "?? DEMO MODE"
    "??? Uninstall" = "??? Uninstall"
    "?? Fresh" = "?? Fresh"
    "?? Reconfigure" = "?? Reconfigure"
    "?? Repair" = "?? Repair"
}

# Get all XAML files
$xamlFiles = Get-ChildItem -Path $basePath -Filter "*.xaml"

foreach ($file in $xamlFiles) {
    Write-Host "Checking: $($file.Name)"
    
    # Read with UTF-8
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    
    # Check if file has corruption
    if ($content -match '\?\?') {
        Write-Host "  FIXING: $($file.Name)" -ForegroundColor Yellow
        
        # Apply replacements
        foreach ($key in $replacements.Keys) {
            $content = $content -replace [regex]::Escape($key), $replacements[$key]
        }
        
        # Save with UTF-8
        $content | Out-File $file.FullName -Encoding UTF8 -NoNewline
        
        Write-Host "  FIXED: $($file.Name)" -ForegroundColor Green
    }
}

Write-Host "`nDone! All emojis fixed." -ForegroundColor Cyan
