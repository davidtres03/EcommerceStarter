# Changelog

All notable changes to EcommerceStarter will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.4] - 2025-11-29

### Fixed
- **Complete Package Structure** - Now includes Upgrader.exe in package
  - Added Upgrader project to package.ps1 build process
  - Upgrader published to `Upgrader/` folder in package
  - MaintenanceModePage now correctly finds Upgrader at `Upgrader/EcommerceStarter.Upgrader.exe`
  - All components now packaged: Installer, Upgrader, Application, WindowsService, migrations

## [1.0.3] - 2025-11-29

### Fixed
- **Auto-Update System** - Fixed upgrader executable not found in package
  - Changed MaintenanceModePage to use `EcommerceStarter.Installer.exe` for upgrades (already in package)
  - Installer.exe now handles in-place upgrades directly from downloaded package
  - Resolves "Upgrader Not Found" error after successful package download

## [1.0.2] - 2025-11-29

### Fixed
- **Auto-Update Package Detection** - Fixed upgrader failing to find installer package in releases
  - Updated asset filename pattern matching to support `EcommerceStarter-v*.zip` format
  - Added fallback patterns for legacy naming conventions
  - Resolves "Package Not Found" error during in-place upgrades

## [1.0.1] - 2025-11-29

### Fixed
- **Image Display** - Added missing `ImagesController` to serve encrypted images from database
  - Fixed logo display issues in header and branding pages
  - Fixed product image display
  - Added `/images/stored/{id}` API endpoint with decryption logic
  - Implements 1-hour response caching for images

## [1.0.0] - 2025-11-29

### Initial Public Release

**EcommerceStarter** - A complete, production-ready ASP.NET Core 8 e-commerce platform with professional installer and auto-update capabilities.

#### Features
- **Complete E-Commerce Platform**
  - Product catalog with categories and variants
  - Shopping cart and checkout flow
  - Order management system
  - Stripe payment integration
  - Guest and registered user checkout

- **Professional Installer**
  - WPF installer with modern UI
  - IIS site configuration
  - SQL Server database setup
  - Automatic prerequisite checking
  - Uninstall and repair capabilities

- **Auto-Update System**
  - GitHub release integration
  - Automatic update detection
  - Seamless upgrade process
  - Configuration preservation

- **Security Features**
  - Rate limiting and IP blocking
  - Suspicious activity detection
  - Security audit logging
  - Encrypted configuration storage

- **Admin Dashboard**
  - Product management with variant support
  - Order tracking and fulfillment
  - Customer management
  - Analytics and metrics
  - Site configuration

- **Additional Capabilities**
  - Email notifications (SMTP/Resend)
  - Google Analytics integration
  - Visitor tracking
  - Cloudinary image hosting
  - USPS shipping integration
  - Tax calculation

#### Technical Stack
- ASP.NET Core 8.0
- Entity Framework Core
- SQL Server
- WPF .NET 8
- Stripe API
- Bootstrap 5

---

[1.0.0]: https://github.com/davidtres03/EcommerceStarter/releases/tag/v1.0.0
