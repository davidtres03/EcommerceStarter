#!/bin/bash
set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

# Set SOURCE_DIR FIRST before any other variable uses it
SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SOURCE_DIR/Packages"
SOLUTION_FILE="$SOURCE_DIR/EcommerceStarter.sln"

# Configuration - Extract version from csproj if not provided
if [ -z "$1" ]; then
    VERSION=$(grep "<Version>" "$SOURCE_DIR/EcommerceStarter.Installer/EcommerceStarter.Installer.csproj" | head -1 | sed 's/^[[:space:]]*<Version>\(.*\)<\/Version>[[:space:]]*$/\1/')
else
    VERSION="$1"
fi
CONFIGURATION="${2:-Release}"
PLATFORM="${3:-AnyCPU}"
RUNTIME="win-x64"

# Project paths
declare -A PROJECTS=(
    ["EcommerceStarter"]="$SOURCE_DIR/EcommerceStarter/EcommerceStarter.csproj"
    ["Installer"]="$SOURCE_DIR/EcommerceStarter.Installer/EcommerceStarter.Installer.csproj"
    ["WindowsService"]="$SOURCE_DIR/EcommerceStarter.WindowsService/EcommerceStarter.WindowsService.csproj"
    ["DemoLauncher"]="$SOURCE_DIR/EcommerceStarter.DemoLauncher/EcommerceStarter.DemoLauncher.csproj"
)

# Functions
log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[✓]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[⚠]${NC} $1"; }
log_error() { echo -e "${RED}[✗]${NC} $1"; }
log_step() {
    echo ""
    echo -e "${CYAN}════════════════════════════════════${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}════════════════════════════════════${NC}"
}

# Check prerequisites
check_prerequisites() {
    log_step "Checking Prerequisites"
    
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET CLI not found"
        exit 1
    fi
    DOTNET_VERSION=$(dotnet --version)
    log_success ".NET CLI: $DOTNET_VERSION"
    
    if [ ! -f "$SOLUTION_FILE" ]; then
        log_error "Solution file not found: $SOLUTION_FILE"
        exit 1
    fi
    log_success "Solution file found"
    
    if ! command -v git &> /dev/null; then
        log_error "Git not found"
        exit 1
    fi
    log_success "Git available"
}

# Clean previous builds
clean_builds() {
    log_step "Cleaning Previous Builds"
    
    log_info "Removing bin/ and obj/ directories..."
    find "$SOURCE_DIR" -type d -name "bin" -o -type d -name "obj" | while read -r dir; do
        rm -rf "$dir"
        log_info "Cleaned: $dir"
    done
    log_success "Clean complete"
}

# Restore dependencies
restore_packages() {
    log_step "Restoring NuGet Packages"
    
    log_info "Running: dotnet restore"
    cd "$SOURCE_DIR"
    if dotnet restore "$SOLUTION_FILE" 2>&1 | grep -q "Restore completed"; then
        log_success "Package restoration complete"
    else
        log_warn "Package restoration completed with warnings (this may be normal)"
    fi
}

# Build solution
build_solution() {
    log_step "Building Solution"
    
    log_info "Configuration: $CONFIGURATION"
    log_info "Running: dotnet build"
    
    cd "$SOURCE_DIR"
    if dotnet build "$SOLUTION_FILE" \
        --configuration "$CONFIGURATION" \
        --no-restore 2>&1 | tail -20; then
        log_success "Solution built successfully"
    else
        log_error "Build failed"
        exit 1
    fi
}

# Verify project versions
verify_versions() {
    log_step "Verifying Project Versions"
    
    for project_name in "${!PROJECTS[@]}"; do
        csproj_file="${PROJECTS[$project_name]}"
        version=$(grep -oP 'AssemblyVersion>\K[^<]*' "$csproj_file" 2>/dev/null || \
                  grep -oP '<Version>\K[^<]*' "$csproj_file" 2>/dev/null || echo "unknown")
        
        if [ "$version" = "$VERSION" ]; then
            log_success "$project_name: $version"
        else
            log_warn "$project_name: $version (expected $VERSION)"
        fi
    done
}

# Publish web application
publish_application() {
    log_step "Publishing Web Application"
    
    PROJECT_FILE="${PROJECTS['EcommerceStarter']}"
    PUBLISH_PATH="$SOURCE_DIR/EcommerceStarter/bin/$CONFIGURATION/net8.0/publish"
    
    log_info "Publishing EcommerceStarter..."
    cd "$SOURCE_DIR"
    
    if [ -d "$PUBLISH_PATH" ]; then
        rm -rf "$PUBLISH_PATH"
        log_info "Removed old publish directory"
    fi
    
    # Build first with version properties, then publish
    # This ensures ALL assemblies (EXE, DLLs) get the correct version embedded
    dotnet build "$PROJECT_FILE" \
        --configuration "$CONFIGURATION" \
        --no-restore \
        -p:Version="$VERSION" \
        -p:AssemblyVersion="$VERSION" \
        -p:FileVersion="$VERSION" \
        -p:InformationalVersion="$VERSION"
    
    dotnet publish "$PROJECT_FILE" \
        --configuration "$CONFIGURATION" \
        --no-build \
        --output "$PUBLISH_PATH" \
        --self-contained false
    
    if [ -d "$PUBLISH_PATH" ]; then
        FILE_COUNT=$(find "$PUBLISH_PATH" -type f | wc -l)
        DIR_SIZE=$(du -sh "$PUBLISH_PATH" | cut -f1)
        log_success "Published to: $PUBLISH_PATH ($DIR_SIZE, $FILE_COUNT files)"
    else
        log_error "Publish failed - directory not created"
        exit 1
    fi
}

# Build upgrader project
build_upgrader() {
    log_step "Building Upgrader"
    
    UPGRADER_PROJECT="$SOURCE_DIR/EcommerceStarter.Upgrader/EcommerceStarter.Upgrader.csproj"
    
    if [ ! -f "$UPGRADER_PROJECT" ]; then
        log_warn "Upgrader project not found - skipping"
        return 0
    fi
    
    log_info "Building and publishing upgrader..."
    cd "$SOURCE_DIR"
    
    if dotnet publish "$UPGRADER_PROJECT" \
        --configuration "$CONFIGURATION" \
        --runtime win-x64 \
        --self-contained true \
        -p:Version="$VERSION" \
        -p:AssemblyVersion="$VERSION" \
        -p:FileVersion="$VERSION" \
        -p:InformationalVersion="$VERSION"; then
        log_success "Upgrader built (v$VERSION)"
    else
        log_warn "Upgrader build failed - installer updates will not work during upgrades"
    fi
}

# Build installer project
build_installer() {
    log_step "Building Installer"
    
    PROJECT_FILE="${PROJECTS['Installer']}"
    
    log_info "Publishing installer as self-contained executable with version $VERSION..."
    cd "$SOURCE_DIR"
    
    if dotnet publish "$PROJECT_FILE" \
        --configuration "$CONFIGURATION" \
        --runtime win-x64 \
        --self-contained true \
        -p:Version="$VERSION" \
        -p:AssemblyVersion="$VERSION" \
        -p:FileVersion="$VERSION" \
        -p:InformationalVersion="$VERSION"; then
        log_success "Installer built (v$VERSION)"
    else
        log_error "Installer build failed"
        exit 1
    fi
}

# Create release package
create_release_package() {
    log_step "Creating Release Package"
    
    PUBLISH_PATH="$SOURCE_DIR/EcommerceStarter/bin/$CONFIGURATION/net8.0/publish"
    BUNDLE_PATH="$SOURCE_DIR/migrations/efbundle.exe"
    PACKAGE_DIR="$OUTPUT_DIR/EcommerceStarter-Installer-v$VERSION"
    ZIP_FILE="$OUTPUT_DIR/EcommerceStarter-Installer-v$VERSION.zip"
    
    # Verify dependencies
    if [ ! -d "$PUBLISH_PATH" ]; then
        log_error "Published app not found: $PUBLISH_PATH"
        exit 1
    fi
    log_success "Published app found"
    
    if [ ! -f "$BUNDLE_PATH" ]; then
        log_error "Migration bundle not found: $BUNDLE_PATH"
        log_info "Run: dotnet ef migrations bundle --self-contained -f"
        exit 1
    fi
    BUNDLE_SIZE=$(du -h "$BUNDLE_PATH" | cut -f1)
    log_success "Migration bundle found ($BUNDLE_SIZE)"
    
    # Clean and prepare package directory
    log_info "Preparing package directory..."
    if [ -d "$PACKAGE_DIR" ]; then
        rm -rf "$PACKAGE_DIR"
    fi
    mkdir -p "$PACKAGE_DIR"
    
    # Copy application contents (not the directory itself)
    log_info "Copying application files..."
    mkdir -p "$PACKAGE_DIR/Application"
    # Copy contents excluding the nested publish folder (ASP.NET Core static assets artifact)
    (cd "$PUBLISH_PATH" && find . -mindepth 1 -maxdepth 1 ! -name 'publish' -exec cp -r {} "$PACKAGE_DIR/Application/" \;)
    APP_SIZE=$(du -sh "$PACKAGE_DIR/Application" | cut -f1)
    log_success "Application copied ($APP_SIZE)"
    
    # Copy migration bundle
    log_info "Copying migration bundle..."
    mkdir -p "$PACKAGE_DIR/migrations"
    cp "$BUNDLE_PATH" "$PACKAGE_DIR/migrations/efbundle.exe"
    log_success "Migration bundle copied"
    
    # Copy installer with all runtime dependencies
    log_info "Copying installer with all runtime dependencies..."
    INSTALLER_PUBLISH_DIR="$SOURCE_DIR/EcommerceStarter.Installer/bin/$CONFIGURATION/net8.0-windows/win-x64/publish"
    if [ -d "$INSTALLER_PUBLISH_DIR" ]; then
        # Copy entire publish directory (includes .exe, DLLs, locale files, etc.)
        cp -r "$INSTALLER_PUBLISH_DIR" "$PACKAGE_DIR/Installer"
        INSTALLER_SIZE=$(du -sh "$PACKAGE_DIR/Installer" | cut -f1)
        log_success "Installer copied with all dependencies ($INSTALLER_SIZE)"
    else
        log_warn "Installer directory not found at: $INSTALLER_PUBLISH_DIR"
    fi
    
    # Copy upgrader with all runtime dependencies
    log_info "Copying upgrader with all runtime dependencies..."
    UPGRADER_PUBLISH_DIR="$SOURCE_DIR/EcommerceStarter.Upgrader/bin/$CONFIGURATION/net8.0-windows/win-x64/publish"
    if [ -d "$UPGRADER_PUBLISH_DIR" ]; then
        # Copy all files from upgrader publish directory to Installer folder
        cp -r "$UPGRADER_PUBLISH_DIR"/* "$PACKAGE_DIR/Installer/"
        UPGRADER_SIZE=$(du -sh "$UPGRADER_PUBLISH_DIR" | cut -f1)
        log_success "Upgrader copied with all dependencies ($UPGRADER_SIZE)"
    else
        log_warn "Upgrader not found at: $UPGRADER_PUBLISH_DIR - upgrade functionality will not work"
    fi
    
    # Create deployment README
    log_info "Creating deployment documentation..."
    cat > "$PACKAGE_DIR/DEPLOYMENT_README.md" << 'EOF'
# EcommerceStarter v1.0.9.7 Deployment Package

## Contents

- **Application/** - Published web application (ASP.NET Core)
- **migrations/efbundle.exe** - Entity Framework Core migration bundle
- **Installer/** - Windows installer with all runtime dependencies (DLLs, config files, locale files)
- **DEPLOYMENT_README.md** - This file

## Installation Methods

### Option 1: Using the Windows Installer (Recommended for Updates)

Run the installer executable:
```
Installer\EcommerceStarter.Installer.exe
```

This will:
- Detect the installed version
- Check for updates
- Handle installation and upgrades
- Apply database migrations automatically

### Option 2: Manual IIS Deployment

#### 1. Extract Package
```bash
unzip EcommerceStarter-Installer-v1.0.9.7.zip -d deployment/
```

#### 2. Prerequisites
- Windows Server 2016+ or Windows 10/11
- IIS 10+
- .NET 8 Runtime or Hosting Bundle
- SQL Server 2016+

#### 3. Deploy Application

Copy the `Application/` folder to your IIS server:
```
C:\inetpub\wwwroot\ecommerce\
```

#### 4. Run Migrations

Execute the migration bundle (run from the deployment directory):
```
migrations\efbundle.exe
```

#### 5. Configure IIS

- Create Application Pool (.NET CLR: No Managed Code)
- Create Web Application pointing to Application folder
- Set ASPNETCORE_ENVIRONMENT to Production
- Configure bindings (HTTP/HTTPS)

#### 6. Verify Installation

Open browser and navigate to your application URL. 

Database migrations will be applied automatically on first run.

## Upgrading from Previous Versions

Simply run `EcommerceStarter.Installer.exe` - it will detect your current version and upgrade.

## Troubleshooting

- Check IIS logs: `C:\inetpub\logs\LogFiles\`
- Check Application Event Log for errors
- Verify SQL Server connection string in appsettings.json
- Ensure application pool identity has database access
- Run installer in Administrator mode if needed

EOF
    log_success "Documentation created"
    
    # Create zip file using Node archiver
    log_info "Creating ZIP archive..."
    if [ -f "$ZIP_FILE" ]; then
        rm "$ZIP_FILE"
    fi
    
    if command -v node &> /dev/null; then
        node "$SOURCE_DIR/create-zip.js" "$PACKAGE_DIR" "$ZIP_FILE" && \
        ZIP_SIZE=$(du -h "$ZIP_FILE" | cut -f1) && \
        log_success "ZIP created: $ZIP_FILE ($ZIP_SIZE)" || \
        { log_error "ZIP creation failed"; exit 1; }
    else
        # Fallback to tar + gzip
        log_info "Node not available, using tar + gzip..."
        cd "$OUTPUT_DIR"
        tar czf "${ZIP_FILE%.zip}.tar.gz" "EcommerceStarter-Installer-v$VERSION/" && \
        cd "$SOURCE_DIR" && \
        TGZ_SIZE=$(du -h "${ZIP_FILE%.zip}.tar.gz" | cut -f1) && \
        log_success "Archive created: ${ZIP_FILE%.zip}.tar.gz ($TGZ_SIZE)" || \
        { log_error "Archive creation failed"; exit 1; }
    fi
    
    # Verify contents (if zip exists)
    if [ -f "$ZIP_FILE" ] && command -v unzip &> /dev/null; then
        log_info "Verifying archive contents..."
        unzip -l "$ZIP_FILE" | grep -E "(efbundle|Application|README)" | head -5
    fi
}

# Build Windows Service (optional)
build_service() {
    log_step "Building Windows Service (Optional)"
    
    PROJECT_FILE="${PROJECTS['WindowsService']}"
    
    log_info "Building WindowsService..."
    cd "$SOURCE_DIR"
    
    dotnet build "$PROJECT_FILE" \
        --configuration "$CONFIGURATION" \
        --no-restore || log_warn "Service build skipped (optional component)"
}

# Summary
show_summary() {
    log_step "Build Summary"
    
    ZIP_FILE="$OUTPUT_DIR/EcommerceStarter-Installer-v$VERSION.zip"
    
    if [ -f "$ZIP_FILE" ]; then
        ZIP_SIZE=$(du -h "$ZIP_FILE" | cut -f1)
        echo ""
        echo "Release package ready for deployment!"
        echo ""
        echo "  Package: $ZIP_FILE"
        echo "  Size: $ZIP_SIZE"
        echo "  Version: $VERSION"
        echo ""
        echo "Next steps:"
        echo "  1. Run: ./release.sh"
        echo "  2. Upload to GitHub Release"
        echo "  3. Test upgrade from v1.0.9.2"
        echo ""
    fi
}

# Main workflow
main() {
    echo -e "${CYAN}"
    echo "╔════════════════════════════════════════════╗"
    echo "║     EcommerceStarter Build System v$VERSION    ║"
    echo "╚════════════════════════════════════════════╝"
    echo -e "${NC}"
    echo ""
    echo "Configuration: $CONFIGURATION"
    echo "Source: $SOURCE_DIR"
    echo ""
    
    check_prerequisites
    clean_builds
    restore_packages
    build_solution
    verify_versions
    publish_application
    build_upgrader
    build_installer
    build_service
    create_release_package
    show_summary
    
    log_success "Build workflow completed successfully!"
}

main "$@"
