#!/bin/bash

# EcommerceStarter v1.0.9.7 Release Uploader
# This script uploads the release package to GitHub

VERSION="1.0.9.7"
REPO="davidtres03/EcommerceStarter"
PACKAGE_FILE="./Packages/EcommerceStarter-Installer-v$VERSION.zip"

echo "[INFO] EcommerceStarter Release Uploader v$VERSION"
echo "[INFO] Repository: $REPO"
echo "[INFO] Package: $PACKAGE_FILE"
echo ""

# Verify package exists
if [ ! -f "$PACKAGE_FILE" ]; then
    echo "[ERROR] Package not found: $PACKAGE_FILE"
    exit 1
fi

PACKAGE_SIZE=$(du -h "$PACKAGE_FILE" | cut -f1)
echo "[INFO] Package size: $PACKAGE_SIZE"
echo ""

echo "NEXT STEPS:"
echo "=========================================="
echo "1. The v1.0.9.7 package is ready:"
echo "   File: $PACKAGE_FILE"
echo "   Size: $PACKAGE_SIZE"
echo ""
echo "2. To publish the GitHub Release, run:"
echo "   gh release create v$VERSION --title \"Release v$VERSION\" --notes \"Production release with unified API configuration\" $PACKAGE_FILE"
echo ""
echo "3. Or use the PowerShell script to upload with token:"
echo "   ./Upload-Release-Asset.ps1 -Version $VERSION"
echo ""
echo "4. Verify release:"
echo "   gh release view v$VERSION --repo $REPO"
echo ""
