#!/bin/bash
set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m'

# Configuration
VERSION="${1:-1.0.9.7}"
RELEASE_TAG="v$VERSION"
REPO="davidtres03/EcommerceStarter"

SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SOURCE_DIR/Packages"
ZIP_FILE="$OUTPUT_DIR/EcommerceStarter-Installer-v$VERSION.zip"

# Functions
log_info() { echo -e "${BLUE}[i]${NC} $1"; }
log_success() { echo -e "${GREEN}[✓]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[!]${NC} $1"; }
log_error() { echo -e "${RED}[✗]${NC} $1"; }
log_section() {
    echo ""
    echo -e "${MAGENTA}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${MAGENTA}$1${NC}"
    echo -e "${MAGENTA}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

# Display usage
usage() {
    cat << EOF
${CYAN}EcommerceStarter Build & Release Manager${NC}

${BLUE}Usage:${NC}
  $0 [VERSION] [COMMAND]

${BLUE}Commands:${NC}
  build         Build and create release package (default)
  release       Create GitHub Release with asset upload
  both          Build and release (full workflow)
  verify        Verify package contents and GitHub release
  clean         Remove old builds and packages

${BLUE}Examples:${NC}
  $0 1.0.9.7              # Build v1.0.9.7 package
  $0 1.0.9.7 release      # Create GitHub Release for v1.0.9.7
  $0 1.0.9.7 both         # Complete build & release workflow

${CYAN}Default: Build v1.0.9.7${NC}

EOF
}

# Parse command line arguments
COMMAND="${2:-build}"
if [ "$COMMAND" = "-h" ] || [ "$COMMAND" = "--help" ]; then
    usage
    exit 0
fi

# Check prerequisites
check_prerequisites() {
    log_section "Checking Prerequisites"
    
    # Check dotnet
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET CLI not found. Install from: https://dotnet.microsoft.com"
        exit 1
    fi
    log_success ".NET: $(dotnet --version)"
    
    # Check git
    if ! command -v git &> /dev/null; then
        log_error "Git not found"
        exit 1
    fi
    log_success "Git: $(git --version | head -1)"
    
    # Check gh
    if ! command -v gh &> /dev/null; then
        log_error "GitHub CLI (gh) not found"
        exit 1
    fi
    log_success "GitHub CLI: $(gh --version | head -1)"
    
    # Check archiver or tar
    if ! command -v archiver &> /dev/null && ! command -v tar &> /dev/null; then
        log_error "archiver or tar command not found"
        exit 1
    fi
    if command -v archiver &> /dev/null; then
        log_success "Archiver: $(archiver --version 2>/dev/null | head -1 || echo 'available')"
    else
        log_success "Tar: available"
    fi
}

# Run build script
run_build() {
    log_section "Building Release Package"
    
    if [ ! -f "$SOURCE_DIR/build.sh" ]; then
        log_error "build.sh not found"
        exit 1
    fi
    
    chmod +x "$SOURCE_DIR/build.sh"
    
    if "$SOURCE_DIR/build.sh" "$VERSION" "Release"; then
        log_success "Build completed successfully"
    else
        log_error "Build failed"
        exit 1
    fi
}

# Run release script
run_release() {
    log_section "Publishing GitHub Release"
    
    if [ ! -f "$SOURCE_DIR/release.sh" ]; then
        log_error "release.sh not found"
        exit 1
    fi
    
    chmod +x "$SOURCE_DIR/release.sh"
    
    if "$SOURCE_DIR/release.sh"; then
        log_success "Release published successfully"
    else
        log_error "Release failed"
        exit 1
    fi
}

# Verify package
verify_package() {
    log_section "Verifying Package"
    
    if [ ! -f "$ZIP_FILE" ]; then
        log_error "Package file not found: $ZIP_FILE"
        return 1
    fi
    
    ZIP_SIZE=$(du -h "$ZIP_FILE" | cut -f1)
    
    log_info "Package: $(basename "$ZIP_FILE")"
    log_info "Size: $ZIP_SIZE"
    
    # Check if we can list contents
    if command -v unzip &> /dev/null; then
        FILE_COUNT=$(unzip -l "$ZIP_FILE" 2>/dev/null | tail -1 | awk '{print $2}' || echo "N/A")
        log_info "Files: $FILE_COUNT"
        
        # Check for critical files
        log_info "Checking critical files..."
        
        local required_files=("Application/" "migrations/efbundle.exe" "DEPLOYMENT_README.md")
        local all_found=true
        
        for file in "${required_files[@]}"; do
            if unzip -l "$ZIP_FILE" 2>/dev/null | grep -q "$file"; then
                log_success "Found: $file"
            else
                log_warn "Missing: $file"
                all_found=false
            fi
        done
        
        if [ "$all_found" = true ]; then
            log_success "All critical files present"
            return 0
        else
            log_error "Some critical files missing"
            return 1
        fi
    else
        log_success "Package verified by file size"
        return 0
    fi
}

# Verify GitHub release
verify_github_release() {
    log_section "Verifying GitHub Release"
    
    if ! gh release view "$RELEASE_TAG" --repo "$REPO" &> /dev/null; then
        log_warn "Release $RELEASE_TAG not found on GitHub"
        return 1
    fi
    
    log_success "Release found: $RELEASE_TAG"
    
    # Check assets
    ASSET_COUNT=$(gh release view "$RELEASE_TAG" --repo "$REPO" --json assets --jq '.assets | length')
    log_info "Assets: $ASSET_COUNT"
    
    if [ "$ASSET_COUNT" -gt 0 ]; then
        log_info "Release assets:"
        gh release view "$RELEASE_TAG" --repo "$REPO" --json assets --jq '.assets[] | "  - \(.name) (\(.size | tostring) bytes)"'
    fi
    
    return 0
}

# Clean old builds
clean_old_builds() {
    log_section "Cleaning Old Builds"
    
    log_info "Removing bin/ directories..."
    find "$SOURCE_DIR" -type d -name "bin" -delete -print | head -5
    
    log_info "Removing obj/ directories..."
    find "$SOURCE_DIR" -type d -name "obj" -delete -print | head -5
    
    log_info "Removing publish/ directories..."
    find "$SOURCE_DIR" -type d -name "publish" -delete -print | head -5
    
    log_success "Cleanup complete"
}

# Show help for commands
show_help() {
    log_section "Available Commands"
    
    echo ""
    echo "  build     - Build and create release package"
    echo "  release   - Create GitHub Release and upload asset"
    echo "  both      - Complete workflow (build + release)"
    echo "  verify    - Verify package and GitHub release"
    echo "  clean     - Clean old builds"
    echo "  help      - Show this help"
    echo ""
}

# Main execution
main() {
    echo -e "${CYAN}"
    echo "╔════════════════════════════════════════════╗"
    echo "║  EcommerceStarter Build & Release Manager  ║"
    echo "║  Version: $VERSION                                      ║"
    echo "╚════════════════════════════════════════════╝"
    echo -e "${NC}"
    
    check_prerequisites
    
    case "$COMMAND" in
        build)
            run_build
            verify_package
            ;;
        release)
            if [ ! -f "$ZIP_FILE" ]; then
                log_error "Package not found. Run 'build' first"
                exit 1
            fi
            run_release
            verify_github_release
            ;;
        both)
            run_build
            verify_package
            run_release
            verify_github_release
            ;;
        verify)
            verify_package
            verify_github_release
            ;;
        clean)
            clean_old_builds
            ;;
        help)
            show_help
            ;;
        *)
            log_error "Unknown command: $COMMAND"
            show_help
            exit 1
            ;;
    esac
    
    echo ""
    log_success "Task completed!"
    echo ""
}

main "$@"
