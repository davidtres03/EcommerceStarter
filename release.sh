#!/bin/bash
set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Set SOURCE_DIR FIRST
SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Extract version from csproj
VERSION=$(grep "<Version>" "$SOURCE_DIR/EcommerceStarter.Installer/EcommerceStarter.Installer.csproj" | head -1 | sed 's/^[[:space:]]*<Version>\(.*\)<\/Version>[[:space:]]*$/\1/')

# Configuration - use extracted version
RELEASE_TAG="v$VERSION"
RELEASE_NAME="Release v$VERSION - AI Integration & Advanced Image Management"
RELEASE_NOTES="## Release v$VERSION - AI Integration & Advanced Image Management

### 🎉 Major Features

#### AI Integration Complete
- ✅ **Dual AI Backend System**
  - Ollama integration (local, free, privacy-focused)
  - Claude API integration (cloud-based, powerful)
  - Smart backend selection and fallback
  - Real-time cost tracking and usage monitoring

- ✅ **AI Control Panel**
  - Interactive chat interface with both AI backends
  - Chat history tracking with timestamps
  - Usage statistics (queries, tokens, estimated costs)
  - Backend status indicators
  - Configuration management with live updates

#### Cloudinary Image Management Enhanced
- ✅ **AI-Powered Image Optimization**
  - Automatic contrast adjustment
  - Smart color correction
  - Shadow removal/fill (40% strength)
  - Professional sharpening (100 for products, 50 for banners)
  - Retina display support (DPR auto)
  - Smart compression (quality auto:best)

- ✅ **Smart Image Workflow**
  - First variant image auto-sets as main product image
  - Image reuse selector - click existing variant images to set as main
  - Toggle between upload and selector modes
  - Saves bandwidth and improves workflow efficiency

### 🔧 Technical Improvements
- Fixed AI service lifetime issues (Singleton vs Scoped)
- Implemented IServiceScopeFactory for proper dependency injection
- Resolved disposed DbContext errors
- Added configuration cache invalidation (30-second cache with manual reset)
- Case-insensitive JSON property reading
- All product images now route through Cloudinary CDN

### 🐛 Bug Fixes
- Fixed variant images using local storage instead of Cloudinary
- Fixed AI configuration toggle not saving enabled state
- Fixed disposed DbContext causing AI backend failures
- Fixed service provider disposal errors in scoped services
- Resolved 5-minute configuration cache causing stale data

See RELEASE_NOTES_v1.1.0.md for complete details.

### 📦 Downloads
- EcommerceStarter-Installer-v$VERSION.zip"
REPO="davidtres03/EcommerceStarter"

ZIP_FILE="$SOURCE_DIR/Packages/EcommerceStarter-Installer-v$VERSION.zip"
UPLOAD_DIR="$SOURCE_DIR/Packages/EcommerceStarter-Installer-v$VERSION"

# Functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[✓]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[⚠]${NC} $1"
}

log_error() {
    echo -e "${RED}[✗]${NC} $1"
}

log_step() {
    echo ""
    echo -e "${CYAN}────────────────────────────────────${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}────────────────────────────────────${NC}"
}

# Verify prerequisites
verify_prerequisites() {
    log_step "Verifying Prerequisites"
    
    # Check for gh CLI
    if ! command -v gh &> /dev/null; then
        log_error "GitHub CLI not found. Install from: https://cli.github.com"
        exit 1
    fi
    GH_VERSION=$(gh --version | head -1)
    log_success "GitHub CLI found: $GH_VERSION"
    
    # Check if ZIP file exists
    if [ ! -f "$ZIP_FILE" ]; then
        log_error "ZIP file not found: $ZIP_FILE"
        log_info "Run ./build.sh first to create the release package"
        exit 1
    fi
    TOTAL_SIZE=$(du -h "$ZIP_FILE" | cut -f1)
    log_success "Release package ready: $ZIP_FILE ($TOTAL_SIZE)"
}

# Check GitHub authentication
check_github_auth() {
    log_step "Checking GitHub Authentication"
    
    log_info "Loading GitHub token from Credential Manager..."
    
    # Windows Credential Manager requires PowerShell CredentialManager module to retrieve passwords
    # This is the only PowerShell call needed - retrieves token and exports to bash environment
    export GH_TOKEN=$(powershell.exe -NoProfile -Command 'Import-Module CredentialManager; $cred = Get-StoredCredential -Target "CatalystGitHubToken"; $cred.GetNetworkCredential().Password' 2>/dev/null)
    
    if [ -z "$GH_TOKEN" ]; then
        log_error "Could not load GitHub token from Credential Manager"
        log_info "Ensure CatalystGitHubToken credential exists in Windows Credential Manager"
        exit 1
    fi
    
    log_success "GitHub token loaded"
}

# Check if release exists
check_release_exists() {
    log_step "Checking Release Status"
    
    if gh release view "$RELEASE_TAG" --repo "$REPO" &> /dev/null; then
        return 0
    else
        return 1
    fi
}

# Create release
create_release() {
    log_info "Creating GitHub Release: $RELEASE_TAG"
    
    gh release create "$RELEASE_TAG" \
        --title "$RELEASE_NAME" \
        --notes "$RELEASE_NOTES" \
        "$ZIP_FILE" \
        --repo "$REPO"
    
    log_success "Release created successfully"
}

# Upload asset to existing release
upload_asset() {
    log_info "Uploading asset to existing release..."
    
    gh release upload "$RELEASE_TAG" "$ZIP_FILE" --repo "$REPO" --clobber
    
    log_success "Asset uploaded successfully"
}

# Display release information
show_release_info() {
    log_step "Release Information"
    
    echo ""
    gh release view "$RELEASE_TAG" --repo "$REPO" --json tagName,name,body,assets,createdAt,publishedAt \
        --jq '.tagName + ": " + .name + "\n" +
               "Created: " + .createdAt + "\n" +
               "Published: " + .publishedAt + "\n" +
               "Assets: " + (.assets | length | tostring) + "\n" +
               "Notes:\n" + .body'
    
    echo ""
    echo "Assets:"
    gh release view "$RELEASE_TAG" --repo "$REPO" --json assets \
        --jq '.assets[] | "  - \(.name) (\(.size | tostring) bytes)"'
}

# Verify package contents
verify_package_contents() {
    log_step "Verifying Package Contents"
    
    # Check if we can verify the archive
    if [ -f "$ZIP_FILE" ] && command -v unzip &> /dev/null; then
        log_info "Listing archive contents (sample):"
        unzip -l "$ZIP_FILE" | grep -E "(efbundle|Application|README)" | head -10
        
        TOTAL_FILES=$(unzip -l "$ZIP_FILE" | tail -1 | awk '{print $2}')
        log_success "Archive contains $TOTAL_FILES files"
    else
        TOTAL_SIZE=$(du -h "$ZIP_FILE" | cut -f1)
        log_success "Release package ready: $(basename "$ZIP_FILE") ($TOTAL_SIZE)"
    fi
}

# Main workflow
main() {
    echo -e "${CYAN}"
    echo "╔════════════════════════════════════════════╗"
    echo "║  EcommerceStarter Release Manager (v$RELEASE_TAG)  ║"
    echo "╚════════════════════════════════════════════╝"
    echo -e "${NC}"
    
    verify_prerequisites
    check_github_auth
    verify_package_contents
    
    if check_release_exists; then
        log_info "Release $RELEASE_TAG already exists"
        upload_asset
    else
        log_info "Release $RELEASE_TAG does not exist"
        create_release
    fi
    
    show_release_info
    
    echo ""
    log_success "Release workflow completed!"
    echo ""
    echo "🔗 GitHub Release URL:"
    echo "   https://github.com/$REPO/releases/tag/$RELEASE_TAG"
    echo ""
}

main "$@"
