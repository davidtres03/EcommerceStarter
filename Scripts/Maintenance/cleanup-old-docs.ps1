# Documentation Cleanup Script
# Safely removes old documentation files that have been consolidated
# MyStore Supply Co.

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Documentation Consolidation Cleanup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Files to be removed (consolidated into new guides)
$filesToRemove = @(
    # Stripe/Payment files (17) ? STRIPE_PAYMENT_GUIDE.md
    "STRIPE_SETUP.md",
    "STRIPE_INTEGRATION_SUMMARY.md",
    "STRIPE_KEYS_QUICK_SETUP.md",
    "HOW_TO_ACCESS_STRIPE_KEYS.md",
    "SECURE_STRIPE_KEY_MANAGEMENT_GUIDE.md",
    "STRIPE_CUSTOMER_INTEGRATION_GUIDE.md",
    "STRIPE_ERROR_FIX_RESTART_REQUIRED.md",
    "MULTIPLE_PAYMENT_METHODS_GUIDE.md",
    "PAYMENT_METHODS_QUICK_REF.md",
    "PAYMENT_METHOD_SELECTION_GUIDE.md",
    "PAYMENT_METHOD_SELECTION_FIXES.md",
    "PAYMENT_METHOD_PERSISTENCE_FIX.md",
    "PAYMENT_METHOD_SPECIFIC_FIX.md",
    "CASHAPP_REDIRECT_FIX.md",
    "GOOGLE_APPLE_PAY_EXPLAINED.md",
    "CHECKOUT_QUICK_REFERENCE.md",
    "TWO_STEP_CHECKOUT_SUMMARY.md",
    
    # Admin files (5) ? ADMIN_GUIDE.md
    "ADMIN_PANEL_README.md",
    "ADMIN_USER_MANAGEMENT.md",
    "ADMIN_DETAILS_PAGES.md",
    "ADMIN_PAYMENT_MANAGEMENT_GUIDE.md",
    "INVENTORY_MANAGEMENT.md",
    
    # UI/Design files (10) ? UI_DESIGN_GUIDE.md
    "DARK_MODE_IMPLEMENTATION.md",
    "DARK_MODE_QUICK_REFERENCE.md",
    "DARK_MODE_TEXT_IMPROVEMENTS.md",
    "COLOR_PALETTE_REFERENCE.md",
    "NEW_COLOR_PALETTE_TERRACOTTA.md",
    "CONTRAST_IMPROVEMENT_FIX.md",
    "UI_ENHANCEMENT_GUIDE.md",
    "UI_ENHANCEMENTS_IMPLEMENTED.md",
    "UI_ENHANCEMENTS_QUICK_REFERENCE.md",
    "UI_VISUAL_COMPARISON.md",
    
    # Features files (8) ? FEATURES_GUIDE.md
    "SHOPPING_CART_README.md",
    "PRODUCT_DETAILS_PAGE.md",
    "CUSTOMER_ORDER_DETAILS.md",
    "SALES_TAX_IMPLEMENTATION.md",
    "ORDER_SUMMARY_FIX.md",
    "CANCELLED_ORDER_TIMELINE_FIX.md",
    "SQL_TRANSLATION_FIX.md",
    "PRODUCTS_DATABASE_UPDATE.md",
    
    # Configuration files (4) ? CONFIGURATION_GUIDE.md
    "ENVIRONMENT_VARIABLES_SETUP.md",
    "CONFIGURATION_CLEANUP_COMPLETE.md",
    "APPSETTINGS_DEVELOPMENT_TEMPLATE.md",
    "DATABASE_MIGRATION_FIXED.md",
    
    # Duplicate file
    "QUICKSTART.md"
)

Write-Host "This script will remove 38 old documentation files that have been" -ForegroundColor Yellow
Write-Host "consolidated into 5 comprehensive guides:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  - STRIPE_PAYMENT_GUIDE.md    (17 files)" -ForegroundColor Green
Write-Host "  - ADMIN_GUIDE.md             (5 files)" -ForegroundColor Green
Write-Host "  - UI_DESIGN_GUIDE.md         (10 files)" -ForegroundColor Green
Write-Host "  - FEATURES_GUIDE.md          (8 files)" -ForegroundColor Green
Write-Host "  - CONFIGURATION_GUIDE.md     (4 files)" -ForegroundColor Green
Write-Host ""

# Check if new files exist
$newFiles = @(
    "STRIPE_PAYMENT_GUIDE.md",
    "ADMIN_GUIDE.md",
    "UI_DESIGN_GUIDE.md",
    "FEATURES_GUIDE.md",
    "CONFIGURATION_GUIDE.md",
    "DOCUMENTATION_CONSOLIDATION.md"
)

$allNewFilesExist = $true
foreach ($file in $newFiles) {
    if (-not (Test-Path $file)) {
        Write-Host "ERROR: New file not found: $file" -ForegroundColor Red
        $allNewFilesExist = $false
    }
}

if (-not $allNewFilesExist) {
    Write-Host ""
    Write-Host "Please ensure all new consolidated guides are created before running cleanup." -ForegroundColor Red
    exit 1
}

Write-Host "All new consolidated guides found! ?" -ForegroundColor Green
Write-Host ""

# Ask for confirmation
$confirmation = Read-Host "Do you want to proceed with deletion? (yes/no)"

if ($confirmation -ne "yes") {
    Write-Host ""
    Write-Host "Cleanup cancelled. No files were deleted." -ForegroundColor Yellow
    exit 0
}

# Create backup directory
$backupDir = "old-docs-backup-$(Get-Date -Format 'yyyy-MM-dd-HHmmss')"
Write-Host ""
Write-Host "Creating backup directory: $backupDir" -ForegroundColor Cyan
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

# Move files to backup
$movedCount = 0
$notFoundCount = 0

foreach ($file in $filesToRemove) {
    if (Test-Path $file) {
        Write-Host "Moving: $file" -ForegroundColor Gray
        Move-Item -Path $file -Destination $backupDir -Force
        $movedCount++
    } else {
        Write-Host "Not found (already deleted?): $file" -ForegroundColor DarkGray
        $notFoundCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Cleanup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Files moved to backup: $movedCount" -ForegroundColor Green
Write-Host "Files not found: $notFoundCount" -ForegroundColor Yellow
Write-Host ""
Write-Host "Backup location: .\$backupDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your documentation is now organized into 13 files:" -ForegroundColor Green
Write-Host "  ? 5 Deployment guides (kept as-is)" -ForegroundColor White
Write-Host "  ? 5 New consolidated guides (Stripe, Admin, UI, Features, Config)" -ForegroundColor White
Write-Host "  ? 3 Core docs (README, START_HERE, DOCUMENTATION_CONSOLIDATION)" -ForegroundColor White
Write-Host ""
Write-Host "If you're satisfied with the consolidation, you can delete the backup folder:" -ForegroundColor Yellow
Write-Host "  Remove-Item -Path '$backupDir' -Recurse -Force" -ForegroundColor Gray
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review the new consolidated guides" -ForegroundColor White
Write-Host "  2. Test that all information is accessible" -ForegroundColor White
Write-Host "  3. Commit changes to Git" -ForegroundColor White
Write-Host "  4. Delete backup folder once satisfied" -ForegroundColor White
Write-Host ""
