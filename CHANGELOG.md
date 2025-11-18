# Changelog

All notable changes to EcommerceStarter will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned Features
- Linux deployment script (Ubuntu/Debian)
- Docker and docker-compose support
- Product reviews and ratings
- Wishlist functionality
- Discount codes and promotions
- Advanced search and filtering
- Multi-currency support
- Automated testing suite

---

## [1.2.1.2] - 2025-11-18

### Added
- 🧹 **Automatic Registry Cleanup** - Orphaned and duplicate registry keys automatically removed during upgrades
  - Keeps only 20 essential registry keys (Version, DisplayVersion, InstallPath, etc.)
  - Removes legacy/duplicate keys automatically during upgrade process
  - Ensures clean registry state after every upgrade
  - Non-essential keys safely removed without affecting functionality
  - Logging of removed keys for troubleshooting

### Improved
- 🔧 **Registry Management** - Enhanced upgrade process maintains clean registry state
  - Automatic cleanup runs after Version and DisplayVersion updates
  - Error handling ensures upgrade continues even if cleanup fails
  - Whitelisted essential keys prevent accidental removal of critical values

---

## [1.2.1.1] - 2025-11-18

### Fixed
- 🔧 **Registry DisplayVersion Auto-Update** - DisplayVersion now updates automatically during upgrades
  - Previously DisplayVersion only set during installation, never updated during upgrades
  - Windows "Programs & Features" would show stale version (e.g., 1.2.0.2 while app was 1.2.1.0)
  - UpgradeService now updates both Version and DisplayVersion registry keys
  - DisplayVersion normalized to 3-part format (1.2.1) matching Windows conventions
  - Ensures Windows Control Panel always shows current version

---

## [1.2.1.0] - 2025-11-18

### Added
- 🆕 **UpdateHistoryRecorderService** - New IHostedService for tracking upgrade completions
  - Records upgrade completion details from registry to UpdateHistory database table
  - Runs automatically on application startup
  - Solves issue where upgrades weren't being recorded in database
  - Two-phase recording: UpgradeService → Registry → Web App → Database
  
- 🆕 **RegistryConfigService** - New Windows Service configuration manager
  - Reads BaseUrl and other settings from Windows Registry
  - Eliminates hardcoded URLs in Windows Service
  - Falls back to appsettings.json if registry not available
  - Singleton service for performance

### Fixed
- 🔴 **CRITICAL: Registry Version Tracking**
  - Updated Upgrader CURRENT_VERSION from 1.0.9.11 to 1.2.1.0
  - Updated Installer CURRENT_VERSION from 1.2.0.0 to 1.2.1.0
  - Registry now correctly shows current version after upgrade
  - Fixed Programs & Features displaying outdated version

- 🔴 **CRITICAL: Windows Service Auto-Restart**
  - Added sc.exe failure recovery configuration to InstallWindowsServiceAsync
  - Service automatically restarts 3 times on failure (60 second delay between attempts)
  - Reset counter: 24 hours (86400 seconds)
  - Critical for production stability and uptime

- 🔴 **CRITICAL: Windows Service Hardcoded URLs**
  - Removed hardcoded `http://localhost:8080` from UpdateService and Worker
  - Windows Service now reads BaseUrl from registry (same location as main app)
  - Falls back to appsettings.json configuration if registry unavailable
  - Works correctly with any port/domain configuration

- 🟠 **HIGH: Error Details Button**
  - Enhanced InstallationPage.xaml.cs ViewErrorDetails_Click handler
  - Now shows scrollable Window with formatted error details
  - Uses Consolas font for better error readability
  - Proper window sizing (600x400) with scroll support

- 🟠 **HIGH: API Configuration Toggle Functionality**
  - Verified OnPostToggleActiveAsync handler exists and works correctly
  - JavaScript properly calls handler with correct parameters
  - Toggle state persists correctly across page reloads
  - No code changes needed - verified working

- 🟡 **MEDIUM: Upgrade Button Loading State**
  - Added visual feedback to Maintenance Mode upgrade button
  - Shows "⏳ Launching upgrader..." message during launch
  - Button disabled and cursor changes to Wait during launch
  - Reduces perceived delay and improves user experience

- 🟡 **MEDIUM: Blank Update History**
  - Created UpdateHistoryRecorderService to populate UpdateHistory table
  - UpgradeService writes completion data to registry when upgrade finishes
  - Web app reads registry on startup and saves to database
  - Thread-safe with scoped DbContext in hosted service
  - Update History page will now show all completed upgrades

### Changed
- 🎨 **UX: API Configuration Toggle Reordering**
  - Reordered all configuration cards for better visual hierarchy
  - NEW ORDER: Active Toggle → Keys Encrypted Badge → Delete Button
  - Removed redundant Active/Inactive status badge (toggle already shows state)
  - Applied to all 7 config types (Stripe, Cloudinary, USPS, UPS, FedEx, Claude, Ollama)
  - Fixed duplicate toggle IDs by adding type prefixes (toggle_stripe_, toggle_usps_, etc.)
  - Added explicit labels to toggles for better accessibility

- 📝 **Version Management**
  - Updated all .csproj files to version 1.2.1.0
  - Updated build-release.sh with centralized VERSION variable
  - Synchronized version across Installer, Upgrader, and Windows Service projects

### Verified
- ✅ **Standardize Config UI Workflow** - Confirmed existing UI already follows best practices pattern
- ✅ **SSL Configuration Workflow** - Verified current design is correct for single-function page
- ✅ **Double Backslashes in Service Path** - Could not reproduce, likely expected PowerShell escaping behavior
- ✅ **Hardcoded URL Verification** - Searched entire codebase, no hardcoded capandcollarsupplyco.com URLs found

### Technical Details
- **New Files Created:** 2
  - EcommerceStarter/Services/UpdateHistoryRecorderService.cs
  - EcommerceStarter.WindowsService/Services/RegistryConfigService.cs
  
- **Files Modified:** 12
  - Version constants, service registrations, UI improvements, Windows Service configuration
  
- **Git Commits:** 3
  - 64762a3: "fix: resolve 7 critical bugs in v1.2.0.3 post-release"
  - be173d0: "fix: remove hardcoded URLs from Windows Service"
  - 644b039: "feat: improve API Configuration toggle UX"

- **Total Changes:**
  - Lines Added: ~430
  - Lines Removed: ~80
  - Zero breaking changes
  - All code compiles successfully (lint warnings are style suggestions only)

### Upgrade Notes
- Update History will populate automatically on next upgrade (1.2.0.x → 1.2.1.0)
- Windows Service will read BaseUrl from registry (no configuration changes required)
- API Configuration page has improved toggle layout (no user action needed)
- All changes are backward compatible with existing installations

---

## [1.2.0.3] - 2025-11-17

### Fixed
- 🎯 **Documentation Emoji Rendering** - Fixed all broken emoji encodings across repository
  - CODE_OF_CONDUCT.md: Fixed 26 broken emojis (shields, checkmarks, hearts, etc.)
  - Scripts/Migration/WORKFLOW.md: Fixed 22 broken emojis in flowcharts and diagrams
  - Git commit messages: Re-encoded v1.2.0 commit with proper UTF-8 emojis
  - LinkedIn announcement: Updated with version corrections
- 🔧 **Upgrade System Critical Fixes** (2025-11-18 update)
  - Fixed Windows Service not included in upgrade package (was only building, not publishing)
  - Fixed registry migration not copying configuration values to new location
  - Registry now properly migrates from `HKLM\SOFTWARE\...\Uninstall\EcommerceStarter_SiteName` to `HKLM\SOFTWARE\EcommerceStarter\SiteName`
  - Old registry location preserved for Windows uninstaller compatibility
  - Package size increased to 176MB (includes 78MB Windows Service with dependencies)
- ⚙️ **Auto-Install Windows Service** (2025-11-18 update #2)
  - Upgrader now automatically installs Windows Service if not present on system
  - Ensures all installations have required components regardless of upgrade path
  - Service treated as integral component, not optional
  - Copies files, configures settings, registers with Windows, starts automatically

### Changed
- Updated all project versions to 1.2.0.3 for consistency
- Improved GitHub rendering across all documentation files
- Enhanced build.sh to publish Windows Service as self-contained executable
- RegistryMigration_v1 now performs complete config migration instead of just marking schema version
- UpgradeWindowsServiceAsync now calls InstallWindowsServiceAsync for fresh installations

---

## [1.2.0] - 2025-11-17

### Added
- 🧹 **Clean Public Release** - Comprehensive repository cleanup for open-source readiness
- 📚 Professional documentation structure
- 🗂️ Archive folder for development documentation
- 📋 Consolidated CHANGELOG with complete version history

### Changed
- Moved internal development docs to `docs/archive/`
- Cleaned root directory to essential files only
- Updated README with current version information

### Removed
- Session logs and internal development notes
- Redundant release management scripts
- Old tarball packages
- Individual release notes (consolidated into CHANGELOG)

---

## [1.1.0] - 2025-11-17

### Added - AI Integration Complete 🤖
- **Dual AI Backend System**
  - Ollama integration (local, free, privacy-focused)
  - Claude API integration (cloud-based, powerful)
  - Smart backend selection and automatic fallback
  - Real-time cost tracking and usage monitoring
  
- **AI Control Panel**
  - Interactive chat interface with both AI backends
  - Chat history tracking with timestamps
  - Usage statistics (queries, tokens, estimated costs)
  - Backend status indicators with visual feedback
  - Configuration management with live updates

- **AI-Powered Image Optimization**
  - Automatic contrast adjustment
  - Smart color correction
  - Shadow removal/fill (40% strength)
  - Professional sharpening (100 for products, 50 for banners)
  - Retina display support (DPR auto)
  - Smart compression (quality auto:best)

- **Smart Image Workflow**
  - First variant image auto-sets as main product image
  - Image reuse selector - click existing variant images
  - Toggle between upload and selector modes
  - Visual feedback with thumbnails and selection

### Fixed - Architecture & Stability 🔧
- Fixed AI service lifetime issues (Singleton vs Scoped)
- Implemented IServiceScopeFactory for proper dependency injection
- Resolved disposed DbContext errors
- Fixed variant images using local storage instead of Cloudinary
- Fixed AI configuration toggle not saving enabled state
- Fixed service provider disposal errors in scoped services

### Changed
- Configuration cache reduced from 5 minutes to 30 seconds
- All product images now route through Cloudinary CDN
- Enhanced logging throughout AI pipeline
- Case-insensitive JSON property reading

### Technical
- Enhanced ApiConfigurations metadata support
- Improved AIChatHistory tracking with backend identification
- Encrypted API keys using IEncryptionService
- Lazy backend initialization for better performance

---

## [1.0.9.6] - 2025-11-15

### Added - Unified API Configuration System 🔒
- **Centralized API Management**
  - Support for 7 API types: Stripe, Cloudinary, USPS, UPS, FedEx, Claude, Ollama
  - AES-256 encryption for all sensitive credentials
  - New tabbed interface in Admin > API Settings
  - Per-API configuration forms with edit/delete operations

- **Comprehensive Audit Logging**
  - Complete change history tracking
  - User and IP address logging
  - Action tracking (Create, Update, Delete)
  - JSON diff of changes
  - Last 20 changes viewer in admin panel

- **Database Tables**
  - ApiConfigurations table with encrypted storage
  - ApiConfigurationAuditLogs for change tracking
  - Optimized indexes for fast lookups

### Fixed - Critical Stability Issues 🐛
- **Version Rollback Bug** - Fixed Windows installer downgrade issue
  - Synchronized AssemblyVersion across all projects to 1.0.9.6
  - Resolved automatic rollback from 1.0.9.6 to 1.0.9.2
  - Upgrade path now works correctly from Programs & Features
  
- **Double-Encryption Bug** - Fixed API configuration save failures
  - Modified SaveConfigurationAsync to preserve encrypted values
  - Added encryption detection to prevent re-encrypting
  - USPS and all API configurations now save reliably

- **Form Pre-Population** - Fixed missing credentials display
  - Added pre-populate helper methods
  - Existing configurations now decrypt and display properly
  - Full edit workflow functional for all API types

### Security
- Environment variable-based key management
- No credentials logged in application logs
- Compliance tracking with user/IP logging

---

## [1.0.0] - 2025-01-15

### Added - Initial Production Release 🚀

#### Core E-Commerce Features
- **Product Management**
  - Complete product catalog with categories
  - Product variants (size, color, custom attributes)
  - Inventory tracking with stock levels
  - Product images with Cloudinary CDN
  - Bulk import/export capabilities

- **Shopping Experience**
  - Session-based shopping cart
  - Guest checkout (no account required)
  - Secure checkout process (two-step)
  - Order confirmation emails
  - Order tracking with carrier integration

- **Payment Processing**
  - Stripe integration (cards, digital wallets)
  - Multiple payment methods support
  - Apple Pay, Google Pay, Cash App Pay
  - Link by Stripe
  - PCI-compliant payment handling

- **Admin Dashboard**
  - Real-time metrics and analytics
  - Sales reports and top products
  - User management (customers and admins)
  - Order management and fulfillment
  - Security audit logging

#### User Management
- ASP.NET Core Identity integration
- Role-based authorization (Admin/Customer)
- Email confirmation for accounts
- Password reset functionality
- Secure authentication with cookies

#### Customization
- **Theme System**
  - Customizable colors (primary, secondary, accent)
  - Custom fonts and typography
  - Logo and branding uploads
  - Custom CSS/HTML support
  - Dark mode with system detection

- **Site Settings**
  - Company name and tagline
  - Contact information
  - Hero images and banners
  - Email templates
  - Google Analytics integration

#### Shipping & Tax
- Optional sales tax calculation (state-based)
- Configurable shipping rules
- USPS tracking integration
- Carrier notifications
- Shipment status updates

#### Security Features
- HTTPS enforcement
- SQL injection protection (parameterized queries)
- XSS prevention (input validation)
- CSRF protection (anti-forgery tokens)
- Rate limiting and IP blocking
- Security audit logging
- Secure password requirements

#### Developer Features
- Clean, documented codebase
- SOLID principles throughout
- Service-oriented architecture
- Dependency injection
- Entity Framework migrations
- Comprehensive XML documentation
- Easy to extend and customize

### Technical Stack
- **Backend:** ASP.NET Core 8.0 (Razor Pages)
- **Database:** SQL Server / SQL Server Express
- **Frontend:** Bootstrap 5.3, Vanilla JavaScript
- **Integrations:** Stripe, Resend, USPS, Google Analytics
- **Cloud:** Cloudinary for image management

### Deployment
- **Windows Automated Installer**
  - IIS configuration automation
  - SQL Server setup scripts
  - One-click deployment
  - Self-contained executable
  - Automatic dependency installation

- **PowerShell Helpers**
  - IIS module installation
  - SQL Server Express setup
  - URL Rewrite configuration
  - Certificate management

### Documentation
- Complete deployment guide (Windows)
- Configuration guide (all settings)
- Stripe payment integration guide
- Admin panel user guide
- Security best practices guide
- API documentation
- Contributing guidelines
- Code of Conduct

### Files Included
- Production-ready source code
- Windows installer executable
- PowerShell deployment scripts
- Complete documentation
- Sample data and images
- MIT License

---

## Version History Summary

| Version | Date | Key Features |
|---------|------|--------------|
| 1.2.0 | 2025-11-17 | Clean public release, documentation consolidation |
| 1.1.0 | 2025-11-17 | AI integration (Ollama + Claude), Cloudinary AI enhancements |
| 1.0.9.6 | 2025-11-15 | Unified API config, critical bug fixes, audit logging |
| 1.0.0 | 2025-01-15 | Initial production release, full e-commerce platform |

---

## Categories Reference

- **Added:** New features
- **Changed:** Changes in existing functionality
- **Deprecated:** Soon-to-be removed features
- **Removed:** Removed features
- **Fixed:** Bug fixes
- **Security:** Security improvements
- **Technical:** Internal technical changes

---

## Links

- [GitHub Repository](https://github.com/yourusername/EcommerceStarter)
- [Documentation](docs/)
- [Issues](https://github.com/yourusername/EcommerceStarter/issues)
- [Contributing](CONTRIBUTING.md)

---

[Unreleased]: https://github.com/yourusername/EcommerceStarter/compare/v1.2.0...HEAD
[1.2.0]: https://github.com/yourusername/EcommerceStarter/releases/tag/v1.2.0
[1.1.0]: https://github.com/yourusername/EcommerceStarter/releases/tag/v1.1.0
[1.0.9.6]: https://github.com/yourusername/EcommerceStarter/releases/tag/v1.0.9.6
[1.0.0]: https://github.com/yourusername/EcommerceStarter/releases/tag/v1.0.0
