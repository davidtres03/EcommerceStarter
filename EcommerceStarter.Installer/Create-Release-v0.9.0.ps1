# Create GitHub Release v0.9.0

$repo = "davidtres03/CapAndCollarSupplyCo"
$tag = "v0.9.0"
$name = "EcommerceStarter v0.9.0 - Demo Release"
$body = Get-Content "C:\Dev\Websites\EcommerceStarter.Installer\release-notes-v0.9.0.md" -Raw

$json = @{
    tag_name = $tag
    name = $name
    body = $body
    draft = $false
    prerelease = $true
} | ConvertTo-Json -Depth 10

Write-Host "`n?? Creating GitHub Release v0.9.0..." -ForegroundColor Cyan
Write-Host "Repository: $repo" -ForegroundColor Yellow
Write-Host "Tag: $tag" -ForegroundColor Yellow
Write-Host ""

# Note: This requires authentication
# You'll need to provide a GitHub Personal Access Token
# Or use GitHub CLI (gh) which handles auth automatically

Write-Host "??  Authentication Required!" -ForegroundColor Yellow
Write-Host ""
Write-Host "To complete this, you have two options:" -ForegroundColor White
Write-Host ""
Write-Host "Option 1: Install GitHub CLI (Recommended)" -ForegroundColor Green
Write-Host "  1. Download from: https://cli.github.com/" -ForegroundColor Gray
Write-Host "  2. Install it" -ForegroundColor Gray
Write-Host "  3. Run: gh auth login" -ForegroundColor Gray
Write-Host "  4. Run: gh release create v0.9.0 --title 'EcommerceStarter v0.9.0 - Demo Release' --notes-file release-notes-v0.9.0.md --prerelease" -ForegroundColor Gray
Write-Host ""
Write-Host "Option 2: Manual via GitHub Website (Easiest)" -ForegroundColor Green
Write-Host "  1. Go to: https://github.com/$repo/releases/new" -ForegroundColor Gray
Write-Host "  2. Tag: v0.9.0" -ForegroundColor Gray
Write-Host "  3. Title: EcommerceStarter v0.9.0 - Demo Release" -ForegroundColor Gray
Write-Host "  4. Copy description from: release-notes-v0.9.0.md" -ForegroundColor Gray
Write-Host "  5. Check ? 'Set as a pre-release'" -ForegroundColor Gray
Write-Host "  6. Upload: bin\Release\net8.0-windows\win-x64\publish\EcommerceStarter.Installer.exe" -ForegroundColor Gray
Write-Host "  7. Click 'Publish release'" -ForegroundColor Gray
Write-Host ""
Write-Host "Opening GitHub releases page in browser..." -ForegroundColor Cyan
Start-Process "https://github.com/$repo/releases/new?tag=v0.9.0&prerelease=1"
