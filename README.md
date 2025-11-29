# EcommerceStarter 🛒

**A modern, production-ready e-commerce platform built with ASP.NET Core 8**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

---

## ✨ Features

### 🛍️ **Complete E-Commerce Platform**
- Product catalog with categories and subcategories
- Shopping cart and checkout workflow
- Order management and tracking
- Customer accounts and profiles
- Admin dashboard for store management

### 🚀 **Enterprise-Ready**
- ASP.NET Core 8 with Entity Framework Core
- SQL Server database with migrations
- Background service for queue processing
- Auto-update system for deployments
- Health monitoring and diagnostics

### 🔧 **Developer-Friendly**
- Clean architecture with dependency injection
- Comprehensive logging and error handling
- Entity Framework migrations for database management
- Self-contained Windows installer
- Automated upgrade system

---

## 🎯 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (Express or higher)
- Windows 10/11 or Windows Server 2019+

### Installation

#### Option 1: Using the Installer (Recommended)
1. Download the latest release from [Releases](https://github.com/davidtres03/EcommerceStarter/releases)
2. Extract `EcommerceStarter-Installer.zip`
3. Run `EcommerceStarter.Installer.exe`
4. Follow the setup wizard

#### Option 2: Manual Development Setup
```bash
# Clone the repository
git clone https://github.com/davidtres03/EcommerceStarter.git
cd EcommerceStarter

# Restore dependencies
dotnet restore

# Update database connection string in appsettings.json
# Then apply migrations
dotnet ef database update --project EcommerceStarter

# Run the application
dotnet run --project EcommerceStarter
```

Navigate to `https://localhost:5001` to access the application.

---

## 📚 Documentation

- **[Installation Guide](docs/INSTALLATION.md)** - Detailed installation instructions
- **[Configuration Guide](docs/CONFIGURATION.md)** - Configuration options and settings
- **[Development Guide](docs/DEVELOPMENT.md)** - Set up your development environment
- **[API Documentation](docs/API.md)** - REST API endpoints
- **[Deployment Guide](docs/DEPLOYMENT.md)** - Production deployment strategies

---

## 🏗️ Architecture

```
EcommerceStarter/
├── EcommerceStarter/              # Main web application
│   ├── Controllers/               # MVC controllers
│   ├── Models/                    # Data models
│   ├── Services/                  # Business logic services
│   ├── Views/                     # Razor views
│   └── wwwroot/                   # Static files (CSS, JS, images)
├── EcommerceStarter.Installer/    # WPF installer application
├── EcommerceStarter.Upgrader/     # Auto-update utility
├── EcommerceStarter.WindowsService/ # Background processing service
└── migrations/                    # Database migration bundles
```

### Key Technologies
- **Backend:** ASP.NET Core 8 MVC
- **Database:** SQL Server with Entity Framework Core
- **Frontend:** Razor Pages, Bootstrap 5, JavaScript
- **Authentication:** ASP.NET Core Identity
- **Payment Processing:** Stripe integration (configurable)
- **Image Hosting:** Cloudinary integration (configurable)

---

## 🚀 Deployment

### IIS Deployment
```bash
# Publish the application
dotnet publish -c Release -o ./publish

# Deploy to IIS
# See docs/DEPLOYMENT.md for detailed IIS configuration
```

### Windows Service Deployment
The Windows Service component processes background jobs (email notifications, order processing, etc.)

```bash
# Install the service
sc create EcommerceStarter binPath="C:\Path\To\EcommerceStarter.WindowsService.exe"
sc start EcommerceStarter
```

---

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Workflow
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Built with [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
- UI components from [Bootstrap](https://getbootstrap.com/)
- Icons from [Font Awesome](https://fontawesome.com/)

---

## 📧 Support

- **Issues:** [GitHub Issues](https://github.com/davidtres03/EcommerceStarter/issues)
- **Discussions:** [GitHub Discussions](https://github.com/davidtres03/EcommerceStarter/discussions)
- **Email:** support@ecommercestarter.com

---

## 🌟 Show Your Support

If you find this project helpful, please give it a ⭐ on GitHub!

---

**Made with ❤️ for the developer community**
