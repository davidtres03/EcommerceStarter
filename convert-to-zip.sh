#!/bin/bash
set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

log_success() { echo -e "${GREEN}[✓]${NC} $1"; }
log_error() { echo -e "${RED}[✗]${NC} $1"; }
log_info() { echo -e "${CYAN}[•]${NC} $1"; }

SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGES_DIR="$SOURCE_DIR/Packages"

# Find and convert archive to ZIP if needed
convert_archive_to_zip() {
    local version="$1"
    local tgz_file="$PACKAGES_DIR/EcommerceStarter-Installer-v$version.tar.gz"
    local zip_file="$PACKAGES_DIR/EcommerceStarter-Installer-v$version.zip"
    
    # If ZIP already exists, we're done
    if [ -f "$zip_file" ]; then
        log_success "ZIP archive already exists: $(basename "$zip_file")"
        return 0
    fi
    
    # If tar.gz exists, convert it
    if [ -f "$tgz_file" ]; then
        log_info "Converting tar.gz to ZIP..."
        
        # Create temporary extraction directory
        local temp_dir=$(mktemp -d)
        trap "rm -rf $temp_dir" EXIT
        
        # Extract tar.gz
        tar -xzf "$tgz_file" -C "$temp_dir"
        
        # Create ZIP using archiver if available, otherwise use Node module directly
        if command -v archiver &> /dev/null; then
            archiver create --format zip "$zip_file" "$temp_dir/"*
        else
            # Use Node.js with archiver module
            cd "$temp_dir"
            node -e "
            const fs = require('fs');
            const archiver = require('archiver');
            const output = fs.createWriteStream('$zip_file');
            const archive = archiver('zip', { zlib: { level: 9 } });
            output.on('close', () => console.log('ZIP created'));
            archive.on('error', (err) => { throw err; });
            archive.pipe(output);
            archive.directory('.', 'EcommerceStarter-Installer-v$version');
            archive.finalize();
            " 2>/dev/null || {
                log_error "Could not convert archive"
                return 1
            }
            cd "$SOURCE_DIR"
        fi
        
        log_success "Converted to ZIP: $(basename "$zip_file") ($(du -h "$zip_file" | cut -f1))"
        return 0
    fi
    
    log_error "No archive found for version $version"
    return 1
}

# Main
if [ $# -eq 0 ]; then
    log_error "Usage: $0 <version>"
    echo "Example: $0 1.0.9.7"
    exit 1
fi

convert_archive_to_zip "$1"
