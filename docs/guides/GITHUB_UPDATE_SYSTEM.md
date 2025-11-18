# GitHub Update System - Complete Guide

## Overview

The EcommerceStarter installer now automatically detects and downloads updates from GitHub releases. The system works with **private repositories** and requires zero manual intervention.

## How It Works

### 1. **GitHub Actions Workflow** (Automatic on Tag Push)

When you push a new tag (e.g., `git push --tags origin`), GitHub Actions automatically:

1. **Builds the application** in Release mode
2. **Creates an installer executable**
3. **Packages everything into a ZIP** (app + migrations + installer)
4. **Creates a GitHub Release** with the tag name
5. **Uploads the ZIP as a release asset**

**Workflow Files:**
- `.github/workflows/release.yml` - Main EcommerceStarter repo
- `EcommerceStarter-Source/.github/workflows/release.yml` - Source repo

### 2. **Installer Discovery** (Automatic on Installation)

When the user runs the installer, it:

1. **Queries GitHub API** for the latest release
2. **Uses stored credentials** from Windows Credential Manager
3. **Finds the ZIP asset** matching `EcommerceStarter-*.zip` pattern
4. **Downloads the package** with progress reporting
5. **Extracts to installation directory**
6. **Installs and configures** the application

**Key Code:**
- `InstallationService.cs` - Main installation logic
- `GitHubReleaseService.cs` - GitHub API queries
- `CredentialManagerService.cs` - Credential retrieval

### 3. **Auto-Update Mechanism** (Background Service)

After installation, the Windows Background Service periodically checks for updates:

1. **Windows Service Worker** checks every 24 hours
2. **Queries GitHub API** for newer versions
3. **If update available**, downloads during low-traffic hours (2-4 AM)
4. **Creates application backup** before upgrading
5. **Updates application files**
6. **Automatic rollback on failure**

**Key Code:**
- `EcommerceStarter.WindowsService/Worker.cs` - Check scheduler
- `EcommerceStarter.WindowsService/UpdateService.cs` - Update handler

---

## Release Management

### Creating a New Release

**Step 1: Update Version**
```csharp
// In EcommerceStarter.Installer/EcommerceStarter.Installer.csproj
<PropertyGroup>
    <Version>1.0.3</Version>  <!-- Bump version -->
</PropertyGroup>
```

**Step 2: Create Tag**
```bash
git tag -a v1.0.3 -m "Release v1.0.3 - Your release notes here"
git push origin v1.0.3  # Push only the tag
```

**Step 3: GitHub Actions Automatic Release**
- Tag push triggers `.github/workflows/release.yml`
- Automatically builds, packages, and creates release
- Creates release with installer ZIP attached
- Check: https://github.com/davidtres03/EcommerceStarter/releases

### Manual Release (if Workflow Fails)

```bash
# Build locally
cd EcommerceStarter-Source
.\Build-PortableInstaller.ps1

# Create release with asset
cd Packages
gh release create v1.0.3 "EcommerceStarter-Installer-v1.0.0.zip" `
  --title "Release v1.0.3" `
  --notes "Release notes here" `
  --repo davidtres03/EcommerceStarter
```

---

## GitHub Authentication

### For Private Repositories

The installer automatically reads GitHub credentials from **Windows Credential Manager**.

**Stored Credential Targets (Checked in Order):**
1. `git:https://github.com` (Git credential helper format)
2. `github.com/git` (Legacy format)
3. `git:https://davidtres03@github.com` (With username)
4. `gh:github.com:` (GitHub CLI format)
5. `CatalystGitHubToken` (Custom name)

**Setup (One-time):**
```bash
# Generate Personal Access Token at:
# https://github.com/settings/tokens

# Store in Windows Credential Manager:
cmdkey /add:github.com/git /user:your_token /pass:***REMOVED***...

# Or via PowerShell:
$cred = New-Object System.Management.Automation.PSCredential(
  "token",
  (ConvertTo-SecureString "***REMOVED***..." -AsPlainText -Force)
)
$cred | Export-Clixml -Path credential.xml
```

### For Public Repositories

No authentication needed. The installer works without stored credentials.

---

## Asset Naming Convention

The installer looks for ZIP files matching these patterns:

**Primary Pattern:** `EcommerceStarter-*.zip`
- Examples: `EcommerceStarter-v1.0.0.zip`, `EcommerceStarter-Installer-v1.0.0.zip`

**Fallback Pattern:** `EcommerceStarter-Installer-*.zip`
- Ensures compatibility with different naming schemes

**Migration Files:** `migrations-*.sql`
- Optional: Database migration scripts

---

## Troubleshooting

### Installer Says "Manually Download from GitHub"

**Causes:**
1. No releases on GitHub - Create a release with assets
2. Release has no assets - Upload the ZIP file
3. Asset name doesn't match pattern - Rename to `EcommerceStarter-*.zip`
4. Authentication failed - Verify GitHub token in Credential Manager
5. Network issue - Check internet connectivity

**Debug:**
- Check Windows Event Viewer for service logs
- Look for debug output in `%TEMP%` directory
- Verify credentials: `cmdkey /list | Select-String github`

### Release Creation Fails

**Common Issues:**
- Tag doesn't exist locally - `git tag v1.0.3`
- Tag not pushed - `git push origin v1.0.3`
- Workflow file syntax error - Check YAML indentation
- Missing permissions - Verify GitHub token has `repo` scope

**Check Workflow Status:**
https://github.com/davidtres03/EcommerceStarter/actions

### Update Detection Fails

**Debug Steps:**
1. Verify internet connectivity
2. Check credentials in Credential Manager
3. Verify release exists on GitHub
4. Check Windows Event Viewer logs
5. Run installer with verbose logging enabled

---

## Current Releases

**Latest:** v1.0.2
- Release: https://github.com/davidtres03/EcommerceStarter/releases/tag/v1.0.2
- Asset: `EcommerceStarter-Installer-v1.0.0.zip` (54.95 MB)

**Previous:** v1.0.1
- Release: https://github.com/davidtres03/EcommerceStarter/releases/tag/v1.0.1
- Asset: `EcommerceStarter-Installer-v1.0.0.zip` (54.95 MB)

---

## Next Steps

### For Ongoing Development

1. **Always bump version** before creating release
2. **Tag with semantic versioning** (v1.0.0, v1.0.1, v1.1.0, etc.)
3. **Push tags to GitHub** to trigger workflow
4. **Wait for Actions to complete** (builds take ~5-10 minutes)
5. **Verify release** at GitHub releases page

### For Better Release Notes

Edit `.github/workflows/release.yml` to customize release body:
- Add feature lists
- Link to issues/PRs
- Include installation instructions
- Add known issues/limitations

### For Staged Rollouts

```bash
# Create prerelease
gh release create v1.0.3 assets... --prerelease
```

Installers can check `release.prerelease` flag to opt-in to beta versions.

---

## Architecture Summary

```
Developer commits + tags
        ↓
GitHub Actions triggered on tag push
        ↓
Builds application, creates installer, packages ZIP
        ↓
Creates GitHub Release with ZIP asset
        ↓
Installer starts and queries GitHub API
        ↓
Finds latest release and downloads ZIP
        ↓
Extracts and installs application
        ↓
Background Service monitors for updates
```

---

**Last Updated:** November 13, 2025
**Status:** ✅ Production Ready
**Support:** Private Repository with GitHub Authentication
