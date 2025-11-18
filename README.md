# 🚀 EcommerceStarter
### Open-Source E-Commerce Platform Built with ASP.NET Core 8

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4)](https://docs.microsoft.com/aspnet/core)
[![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap)](https://getbootstrap.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoft-sql-server)](https://www.microsoft.com/sql-server)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A **production-ready, fully-featured e-commerce platform** built with **ASP.NET Core 8 Razor Pages**. Perfect for launching your online store with professional features like Stripe payments, admin panel, dark mode, and comprehensive customization options.

**🚀 Quick Start:** Clone → Run Deployment Script → Sell!

📖 **[→ Full Getting Started Guide](GETTING_STARTED.md)** - Complete step-by-step instructions for beginners!

---

## ⚡ Quick Start (15 Minutes to Live Store!)

### **Windows (Recommended)**

```powershell
# 1. Clone the repository
git clone https://github.com/davidtres03/EcommerceStarter.git
cd EcommerceStarter

# 2. Run automated deployment (as Administrator)
cd Scripts
.\Deploy-Windows.ps1

# 3. Follow the interactive prompts
# - Enter company name
# - Set admin credentials
# - Configure database
# - (Optional) Stripe & Email

# 4. Manual IIS setup (5 minutes)
. .\IIS-Helpers.ps1
# See Scripts/README.md for detailed IIS configuration

# 5. Access your store!
# Open browser to: https://localhost/Admin/Dashboard
```

**That's it!** Your store is live! 🎉

### **Manual Installation**

If you prefer manual setup or are on Linux/Mac:

1. **Prerequisites**
   - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
   - [SQL Server](https://www.microsoft.com/sql-server) or SQL Server Express
   - (Windows) IIS or (Linux) Nginx

2. **Database Setup**
   ```bash
   cd EcommerceStarter
   dotnet ef database update
   ```

3. **Run Application**
   ```bash
   dotnet run
   ```

4. **Access**
   - Website: https://localhost:7001
   - Admin: https://localhost:7001/Admin/Dashboard
   - Login: admin@example.com / Admin@123
   - 🔒 **Change password immediately!**

🔒 **Detailed Instructions:** See [Scripts/README.md](Scripts/README.md) for complete deployment guide.

---

## 🎯 Features

### 🛒👤 **Customer Experience**
- **Modern Responsive UI** - Mobile-first design with Bootstrap 5
- **Dark Mode** - System-aware with manual toggle
- **Product Catalog** - Browse by category with search and filtering
- **Product Variants** - Size, color, and custom attributes
- **Shopping Cart** - Session-based with real-time calculations
- **Secure Checkout** - Two-step process with Stripe integration
- **Order Tracking** - View order history and status with tracking numbers
- **User Accounts** - Registration, login, and profile management
- **Guest Checkout** - No account required for purchases

### 💳 **Payment Processing**
- **Stripe Integration** - Industry-leading payment processing
- **Multiple Payment Methods:**
  - Credit/Debit Cards (Visa, Mastercard, Amex, Discover)
  - Digital Wallets (Apple Pay, Google Pay, Cash App Pay)
  - Link by Stripe
- **Secure Checkout** - PCI-compliant payment handling
- **Configurable Tax** - Optional sales tax with state-by-state rates
- **Flexible Shipping** - Configurable shipping rules

### 🔧⚙️📊 **Admin Panel**
- **Dashboard** - Real-time metrics and analytics
- **User Management** - Customer accounts and roles
- **Product Management** - Full CRUD with variants and attributes
- **Inventory Tracking** - Stock levels and availability
- **Order Management** - Process, fulfill, and track orders
- **Shipping Integration** - USPS tracking support
- **Settings** - Branding, theme, email, and integrations
- **Reports** - Sales analytics and top products
- **Security Audit Logs** - Track all admin actions

### 🎨 **Customization**
- **Theme System** - Customizable colors, fonts, and logos
- **Site Settings** - Configure everything from admin panel
- **Email Templates** - Branded transactional emails
- **Multi-Provider Email** - Resend, SMTP, SendGrid support
- **Google Analytics** - Optional analytics integration
- **Custom CSS/HTML** - Advanced customization options
- **No Branding** - Completely white-label ready

### 🔐 **Security & Performance**
- **ASP.NET Core Identity** - Secure authentication framework
- **Role-Based Access** - Admin and Customer roles
- **HTTPS Enforced** - SSL/TLS encryption
- **SQL Injection Protection** - Parameterized queries
- **XSS Prevention** - Input validation and sanitization
- **CSRF Protection** - Anti-forgery tokens
- **Rate Limiting** - DDoS protection
- **Security Audit Logs** - Track all admin actions
- **Secure Configuration** - User Secrets for sensitive data

---

## 📦 Deployment Options

### Option 1: Windows + IIS (Automated) ⭐ Recommended

Complete automation with one PowerShell script!

```powershell
cd Scripts
.\Deploy-Windows.ps1
```

**Installs:**
- ✅ .NET 8 SDK (if missing)
- ✅ SQL Server Express (if missing)
- ✅ IIS with all features (if missing)
- ✅ URL Rewrite Module
- ✅ Creates database and runs migrations
- ✅ Configures connection strings

**See:** [Scripts/README.md](Scripts/README.md)

### Option 2: Linux + Nginx 🐧

Coming Soon! Bash script for Ubuntu/Debian.

### Option 3: Docker 🐳

Coming Soon! One-command deployment.

### Option 4: Azure App Service ☁️

```bash
az webapp up --name mystore --resource-group MyStoreRG
```

### Option 5: Manual Deployment

For full control, see [docs/deployment/](docs/deployment)

---

## 🛠️💻 Technology Stack

### Backend
- ASP.NET Core 8.0 (Razor Pages)
- C# 12
- Entity Framework Core 8
- SQL Server / SQL Server Express
- ASP.NET Core Identity

### Frontend
- Bootstrap 5.3
- Vanilla JavaScript (ES6+)
- Bootstrap Icons
- CSS Variables for theming
- Responsive design (mobile-first)

### Third-Party Integrations
- **Stripe** - Payment processing
- **Resend** - Transactional emails (optional)
- **SMTP** - Gmail, Outlook, custom (optional)
- **Google Analytics** - Analytics (optional)
- **USPS** - Shipment tracking (optional)
- **Azure Key Vault** - Secrets management (optional)

---

## ✨ What's Included

### Core Features
- ✅ Complete product catalog with variants
- ✅ Shopping cart and checkout flow
- ✅ Order management system
- ✅ User authentication and authorization
- ✅ Admin dashboard with analytics
- ✅ Email notifications
- ✅ Dark mode support
- ✅ Mobile responsive design

### Advanced Features
- ✅ Product variants with attributes (size, color, etc.)
- ✅ Inventory management with stock tracking
- ✅ Order tracking with carrier integration
- ✅ Guest checkout (no account required)
- ✅ Sales tax calculation (optional, state-based)
- ✅ Theme customization system
- ✅ Security audit logging
- ✅ Rate limiting and DDoS protection

### Developer-Friendly
- ✅ Clean, documented code
- ✅ SOLID principles
- ✅ Dependency injection
- ✅ Service-oriented architecture
- ✅ Entity Framework migrations
- ✅ Comprehensive comments
- ✅ Easy to extend

---

## 📚 Documentation

Complete documentation is available in the `/docs` folder:

- **[Getting Started Guide](GETTING_STARTED.md)** ⭐ - Complete beginner-friendly walkthrough
- [Deployment Guide](Scripts/README.md) - Automated Windows deployment
- [Configuration Guide](docs/features/CONFIGURATION_GUIDE.md) - All settings explained
- [Stripe Integration](docs/features/STRIPE_PAYMENT_GUIDE.md) - Payment setup guide
- [Admin Guide](docs/features/ADMIN_GUIDE.md) - Admin panel documentation
- [Security Guide](docs/deployment/SECURE_DEPLOYMENT_GUIDE.md) - Production security
- [Contributing](CONTRIBUTING.md) - How to contribute
- [Code of Conduct](CODE_OF_CONDUCT.md) - Community guidelines

---

## 🎨 Customization

EcommerceStarter is designed to be fully customizable without touching code:

### Through Admin Panel
- Company name, logo, and tagline
- Custom color scheme (primary, secondary, accent)
- Custom fonts
- Hero images and icons
- Email templates
- Tax rates (by state)
- Shipping rules
- Google Analytics
- Custom CSS and HTML

### Theme Presets
- **Default:** Bootstrap Blue (neutral)
- Create your own themes easily
- Change colors with a few clicks

### For Developers
- Full access to source code
- Service-based architecture (easy to extend)
- Well-documented APIs
- Standard ASP.NET Core conventions

---

## 🔒 Security

- ✅ ASP.NET Core Identity with email confirmation
- ✅ Role-based authorization (Admin/Customer)
- ✅ HTTPS enforced in production
- ✅ SQL injection protection (parameterized queries)
- ✅ XSS prevention (input validation & encoding)
- ✅ CSRF protection (anti-forgery tokens)
- ✅ Rate limiting and IP blocking
- ✅ Security audit logging
- ✅ Secure password requirements
- ✅ User Secrets for sensitive data

**Default Admin:**
- Email: `admin@example.com`
- Password: `Admin@123`
- 🔒 **CHANGE IMMEDIATELY ON FIRST LOGIN!**

---

## 🤝 Contributing

We welcome contributions! Here's how you can help:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

### Ways to Contribute
- 🐛 Report bugs
- 💡 Suggest features
- 📝 Improve documentation
- 🔧 Submit pull requests
- ⭐ Star the repository
- 📢 Spread the word!

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**You are free to:**
- ✅ Use commercially
- ✅ Modify
- ✅ Distribute
- ✅ Use privately
- ✅ Sublicense

*Attribution appreciated but not required!*

---

## 🙏 Acknowledgments

- Built with [ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- Styled with [Bootstrap 5](https://getbootstrap.com/)
- Payments by [Stripe](https://stripe.com/)
- Icons from [Bootstrap Icons](https://icons.getbootstrap.com/)
- Email by [Resend](https://resend.com/)

---

## 📊 Project Status

- ✅ Core E-Commerce Features - Complete
- ✅ Payment Integration - Complete (Stripe)
- ✅ Admin Panel - Complete
- ✅ Windows Deployment - Complete (automated)
- ✅ Security - Production-ready
- ✅ Documentation - Comprehensive guides
- 🚧 Linux Deployment - Coming soon
- 🚧 Docker Support - Coming soon
- 🚧 Testing - In progress

---

## 🗺️ Roadmap

### Version 1.0 (Current)
- Product catalog with variants
- Shopping cart and checkout
- Stripe payment integration
- Admin panel with dashboard
- User authentication and roles
- Email notifications
- Dark mode support
- Windows automated deployment
- Complete documentation

### Version 1.1 (Planned)
- Linux deployment script
- Docker and docker-compose
- Product reviews and ratings
- Wishlist functionality
- Discount codes and promotions
- Advanced search and filtering
- Multi-currency support
- Automated testing suite

### Version 2.0 (Future)
- Multi-vendor marketplace
- Subscription products
- Advanced analytics dashboard
- Mobile app (React Native)
- AI-powered product recommendations
- Multi-language support

---

## 💬 Support

- **Documentation:** [docs/](docs)
- **Deployment Help:** [Scripts/README.md](Scripts/README.md)
- **Issues:** [GitHub Issues](https://github.com/davidtres03/EcommerceStarter/issues)
- **Discussions:** [GitHub Discussions](https://github.com/davidtres03/EcommerceStarter/discussions)

---

## ⭐ Star This Repository

If you find this project useful, please consider giving it a star! It helps others discover this project and motivates continued development.

[⭐ Star on GitHub](https://github.com/davidtres03/EcommerceStarter)

---

## 📸 Screenshots

*(Add screenshots of your application here)*

---

**Version:** 1.2.0  
**Last Updated:** November 17, 2025  
**Framework:** ASP.NET Core 8.0 (Razor Pages)  
**License:** MIT

🎯 **Built for entrepreneurs and developers who want to launch their online store quickly and professionally.**

Made with ❤️ by the open-source community
