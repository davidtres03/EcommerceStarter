#!/bin/bash
set -e

VERSION="1.0.9.7"
SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SOURCE_DIR/Packages"
PUBLISH_PATH="$SOURCE_DIR/EcommerceStarter/bin/Release/net8.0/publish"
BUNDLE_PATH="$SOURCE_DIR/migrations/efbundle.exe"
PACKAGE_DIR="$OUTPUT_DIR/EcommerceStarter-Installer-v$VERSION"
ZIP_FILE="$OUTPUT_DIR/EcommerceStarter-Installer-v$VERSION.zip"

echo "[INFO] EcommerceStarter Release Build v$VERSION"
echo "[INFO] Source: $SOURCE_DIR"
echo "[INFO] Output: $OUTPUT_DIR"
echo ""

# Verify published app exists
if [ ! -d "$PUBLISH_PATH" ]; then
    echo "[ERROR] Published application not found at: $PUBLISH_PATH"
    echo "[INFO] Run the build script first to publish the application"
    exit 1
fi

# Verify migration bundle exists
if [ ! -f "$BUNDLE_PATH" ]; then
    echo "[ERROR] Migration bundle not found at: $BUNDLE_PATH"
    exit 1
fi

echo "[INFO] ✓ Published app found"
echo "[INFO] ✓ Migration bundle found ($(du -h "$BUNDLE_PATH" | cut -f1))"
echo ""

# Clean and recreate package directory
echo "[INFO] Preparing package directory..."
if [ -d "$PACKAGE_DIR" ]; then
    rm -rf "$PACKAGE_DIR"
fi
mkdir -p "$PACKAGE_DIR"

# Copy application files
echo "[INFO] Copying application files..."
cp -r "$PUBLISH_PATH" "$PACKAGE_DIR/Application"

# Copy migration bundle
echo "[INFO] Copying migration bundle..."
mkdir -p "$PACKAGE_DIR/migrations"
cp "$BUNDLE_PATH" "$PACKAGE_DIR/migrations/efbundle.exe"

# Create deployment README
echo "[INFO] Creating deployment documentation..."
cat > "$PACKAGE_DIR/DEPLOYMENT_README.md" << 'EOF'
# EcommerceStarter v1.0.9.7 Deployment Package

## Contents

- **Application/** - Published web application (ASP.NET Core)
- **migrations/efbundle.exe** - Entity Framework Core migration bundle
- **DEPLOYMENT_README.md** - This file

## Installation Steps

### 1. Extract Package
```bash
unzip EcommerceStarter-Installer-v1.0.9.7.zip -d deployment/
```

### 2. Prerequisites
- Windows Server 2016+ or Windows 10/11
- IIS 10+
- .NET 8 Runtime or Hosting Bundle
- SQL Server 2016+

### 3. Deploy Application

Copy the `Application/` folder to your IIS server:
```
C:\inetpub\wwwroot\ecommerce\
```

### 4. Run Migrations

Execute the migration bundle (run from the deployment directory):
```
migrations\efbundle.exe
```

### 5. Configure IIS

- Create Application Pool (.NET CLR: No Managed Code)
- Create Web Application pointing to Application folder
- Set ASPNETCORE_ENVIRONMENT to Production
- Configure bindings (HTTP/HTTPS)

### 6. Verify Installation

Open browser and navigate to your application URL. 

Database migrations will be applied automatically on first run.

## Troubleshooting

- Check IIS logs: `C:\inetpub\logs\LogFiles\`
- Check Application Event Log for errors
- Verify SQL Server connection string in appsettings.json
- Ensure application pool identity has database access

EOF

echo "[INFO] ✓ Package directory prepared at: $PACKAGE_DIR"
echo ""

# Create zip file
echo "[INFO] Creating zip archive..."
if [ -f "$ZIP_FILE" ]; then
    rm "$ZIP_FILE"
fi

cd "$OUTPUT_DIR"
zip -r -q "EcommerceStarter-Installer-v$VERSION.zip" "EcommerceStarter-Installer-v$VERSION/"
cd "$SOURCE_DIR"

ZIP_SIZE=$(du -h "$ZIP_FILE" | cut -f1)
echo "[INFO] ✓ Zip created: $ZIP_FILE ($ZIP_SIZE)"
echo ""

# Verify zip contents
echo "[INFO] Verifying zip contents..."
zip -l "$ZIP_FILE" | grep -E "(efbundle|Application|README)" | head -10

echo ""
echo "[SUCCESS] Release package ready!"
echo ""
echo "Package: $ZIP_FILE"
echo "Size: $ZIP_SIZE"
echo ""
echo "Next steps:"
echo "1. Upload to GitHub Release: gh release upload v$VERSION $ZIP_FILE"
echo "2. Or manually distribute the zip file"
echo ""
