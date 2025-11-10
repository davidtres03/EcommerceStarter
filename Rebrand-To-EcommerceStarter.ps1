# EcommerceStarter Rebranding Script
# Renames all CapAndCollar references to EcommerceStarter

$ErrorActionPreference = "Stop"

Write-Host "?? Starting EcommerceStarter Rebranding..." -ForegroundColor Cyan

# Define replacements
$replacements = @{
    "CapAndCollarSupplyCo" = "EcommerceStarter"
    "Cap and Collar Supply Co" = "EcommerceStarter"
    "Cap & Collar" = "EcommerceStarter"
    "capandcollar" = "ecommercestarter"
}

# File extensions to process
$extensions = @("*.cs", "*.csproj", "*.sln", "*.xaml", "*.json", "*.md", "*.config", "*.xml")

# Get all files (excluding .git, bin, obj directories)
$files = Get-ChildItem -Path . -Recurse -File -Include $extensions | 
    Where-Object { $_.FullName -notmatch '\\\.git\\|\\bin\\|\\obj\\' }

Write-Host "?? Processing $($files.Count) files..." -ForegroundColor Yellow

$filesChanged = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # Apply all replacements
    foreach ($old in $replacements.Keys) {
        $new = $replacements[$old]
        $content = $content -replace [regex]::Escape($old), $new
    }
    
    # Save if changed
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        $filesChanged++
        Write-Host "  ? Updated: $($file.Name)" -ForegroundColor Green
    }
}

Write-Host "`n?? Renaming directories and files..." -ForegroundColor Yellow

# Rename directories (do this bottom-up to avoid path issues)
$dirs = Get-ChildItem -Path . -Recurse -Directory | 
    Where-Object { $_.Name -like "*CapAndCollar*" } |
    Sort-Object { $_.FullName.Length } -Descending

foreach ($dir in $dirs) {
    $newName = $dir.Name -replace "CapAndCollarSupplyCo", "EcommerceStarter"
    $newPath = Join-Path $dir.Parent.FullName $newName
    if ($newName -ne $dir.Name) {
        Rename-Item $dir.FullName $newPath
        Write-Host "  ?? Renamed dir: $($dir.Name) ? $newName" -ForegroundColor Cyan
    }
}

# Rename files
$filesToRename = Get-ChildItem -Path . -Recurse -File | 
    Where-Object { $_.Name -like "*CapAndCollar*" }

foreach ($file in $filesToRename) {
    $newName = $file.Name -replace "CapAndCollarSupplyCo", "EcommerceStarter"
    $newPath = Join-Path $file.Directory.FullName $newName
    if ($newName -ne $file.Name) {
        Rename-Item $file.FullName $newPath
        Write-Host "  ?? Renamed file: $($file.Name) ? $newName" -ForegroundColor Cyan
    }
}

Write-Host "`n?? Rebranding Complete!" -ForegroundColor Green
Write-Host "   Files changed: $filesChanged" -ForegroundColor White
Write-Host "   Ready for fresh commit!" -ForegroundColor White
