# Fix remaining emoji corruption - Simple approach

$basePath = "C:\Dev\Websites\EcommerceStarter.Installer\Views"

# DemoLauncherWindow.xaml - Build config emoji
Write-Host "Fixing DemoLauncherWindow.xaml..."
$file = "$basePath\DemoLauncherWindow.xaml"
$content = Get-Content $file -Raw -Encoding UTF8
$content = $content -replace 'Text="\?\?" FontSize="32"', 'Text="??" FontSize="32"'
$content | Out-File $file -Encoding UTF8 -NoNewline

# MaintenanceModePage.xaml
Write-Host "Fixing MaintenanceModePage.xaml..."
$file = "$basePath\MaintenanceModePage.xaml"
$content = Get-Content $file -Raw -Encoding UTF8
$content = $content -replace '\?\? Existing Installation Found', '?? Existing Installation Found'
$content = $content -replace 'Text="\?\?"', 'Text="??"'
$content | Out-File $file -Encoding UTF8 -NoNewline

# UpgradeProgressPage.xaml
Write-Host "Fixing UpgradeProgressPage.xaml..."
$file = "$basePath\UpgradeProgressPage.xaml"
$content = Get-Content $file -Raw -Encoding UTF8
$content = $content -replace '\?\? Upgrading', '?? Upgrading'
$content | Out-File $file -Encoding UTF8 -NoNewline

# UpgradeWelcomePage.xaml
Write-Host "Fixing UpgradeWelcomePage.xaml..."
$file = "$basePath\UpgradeWelcomePage.xaml"
$content = Get-Content $file -Raw -Encoding UTF8
$content = $content -replace '\?\? Upgrade Existing Installation', '?? Upgrade Existing Installation'
$content = $content -replace '\?\? Current Installation', '?? Current Installation'
$content = $content -replace 'Text="\?\?"/>', 'Text="?"/>'
$content | Out-File $file -Encoding UTF8 -NoNewline

Write-Host "Done! All remaining emojis fixed." -ForegroundColor Green
