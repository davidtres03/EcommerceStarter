# Create GitHub Release v1.0.9.7
param(
    [string]$ReleaseTag = "v1.0.9.7",
    [string]$ReleaseName = "Release v1.0.9.7",
    [string]$ReleaseNotes = "Production release - Version synchronization and upgrade workflow fixes",
    [string]$ZipFile = "Packages\EcommerceStarter-Installer-v1.0.9.7.zip",
    [string]$Repository = "davidtres03/EcommerceStarter"
)

Write-Host "🔍 Checking GitHub Release status for $ReleaseTag..." -ForegroundColor Cyan

# Check if release exists
$releaseExists = gh release view $ReleaseTag --repo $Repository 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Release $ReleaseTag already exists on GitHub" -ForegroundColor Green
    
    # Check if asset exists
    $assetExists = gh release view $ReleaseTag --repo $Repository --json assets --jq '.assets[] | select(.name | contains("EcommerceStarter-Installer"))' 2>$null
    if ($assetExists) {
        Write-Host "✅ Release asset already uploaded" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Asset not found, uploading..." -ForegroundColor Yellow
        if (Test-Path $ZipFile) {
            gh release upload $ReleaseTag "$ZipFile" --repo $Repository --clobber
            Write-Host "✅ Asset uploaded successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ ZIP file not found: $ZipFile" -ForegroundColor Red
        }
    }
} else {
    Write-Host "📦 Creating new release $ReleaseTag..." -ForegroundColor Yellow
    
    if (Test-Path $ZipFile) {
        gh release create $ReleaseTag `
            --title "$ReleaseName" `
            --notes "$ReleaseNotes" `
            "$ZipFile" `
            --repo $Repository
        
        Write-Host "✅ Release $ReleaseTag created successfully" -ForegroundColor Green
        Write-Host "📥 Asset uploaded: $(Get-Item $ZipFile | ForEach-Object { "{0:N2} MB" -f ($_.Length / 1MB) })" -ForegroundColor Green
    } else {
        Write-Host "❌ ZIP file not found: $ZipFile" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "📋 Release Details:" -ForegroundColor Cyan
gh release view $ReleaseTag --repo $Repository --json tagName,name,body,assets --jq '.tagName + ": " + .name + " (" + (.assets | length | tostring) + " assets)"'
