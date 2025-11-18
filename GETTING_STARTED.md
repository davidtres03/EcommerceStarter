# 🚀 Getting Started with EcommerceStarter

**Welcome to EcommerceStarter!** This guide will walk you through setting up your e-commerce platform from scratch to selling your first product.

**Estimated Time:** 15-30 minutes (depending on your setup)

---

## 📋 Table of Contents

1. [Prerequisites](#-prerequisites)
2. [Quick Start (Windows)](#-quick-start-windows)
3. [Quick Start (Manual)](#-quick-start-manual)
4. [Initial Configuration](#-initial-configuration)
5. [Adding Your First Product](#-adding-your-first-product)
6. [Setting Up Payments](#-setting-up-payments)
7. [Customizing Your Store](#-customizing-your-store)
8. [Going Live](#-going-live)
9. [Troubleshooting](#-troubleshooting)

---

## 📦 Prerequisites

### Required

- **Windows 10/11** OR **Linux/macOS** (for manual install)
- **Git** - [Download here](https://git-scm.com/downloads)
- **Internet connection** - For downloading dependencies

### Will Be Installed Automatically (Windows)

- **.NET 8 SDK** - Installed by our deployment script
- **SQL Server Express** - Installed by our deployment script
- **IIS** - Configured by our deployment script

### For Manual Installation

- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** - Required
- **SQL Server** or **SQL Server Express** - Required ([Download](https://www.microsoft.com/sql-server/sql-server-downloads))
- **(Windows) IIS** OR **(Linux) Nginx** - Web server

---

## ⚡ Quick Start (Windows)

**This is the EASIEST way to get started!** Our PowerShell script handles everything.

### Step 1: Clone the Repository

```powershell
# Open PowerShell as Administrator (right-click → Run as Administrator)

# Clone the repository
git clone https://github.com/davidtres03/EcommerceStarter.git
cd EcommerceStarter
```

### Step 2: Run Automated Deployment

```powershell
# Navigate to Scripts folder
cd Scripts

# Run the deployment script
.\Deploy-Windows.ps1
```

**The script will:**
- ✅ Check for .NET 8 SDK (installs if missing)
- ✅ Check for SQL Server Express (installs if missing)
- ✅ Check for IIS (enables if missing)
- ✅ Install URL Rewrite Module
- ✅ Create database and run migrations
- ✅ Configure connection strings
- ✅ Prompt you for:
  - Company name
  - Admin email
  - Admin password
  - Database name (default: EcommerceDB)

### Step 3: Manual IIS Configuration (5 minutes)

The deployment script prepares everything, but you need to configure IIS manually:

```powershell
# Load IIS helper functions
. .\IIS-Helpers.ps1

# Create the IIS site (follow the prompts)
New-IISSite -SiteName "EcommerceStarter" -Port 80
```

**OR follow these manual steps:**

1. Open **IIS Manager** (search "IIS" in Start menu)
2. Right-click **Sites** → **Add Website**
3. **Site name:** EcommerceStarter
4. **Physical path:** `C:\EcommerceStarter\EcommerceStarter` (or your install path)
5. **Port:** 80 (or 8080 if 80 is taken)
6. Click **OK**
7. Select your new site → **Application Pools** → Set **.NET CLR Version** to **No Managed Code**
8. **Restart the site**

### Step 4: Access Your Store

```
🌐 Website: http://localhost
🔧 Admin Panel: http://localhost/Admin/Dashboard

📧 Default Login:
   Email: admin@example.com
   Password: Admin@123

🔒 IMPORTANT: Change the admin password immediately!
```

**🎉 That's it! Your store is live!**

---

## 🛠️ Quick Start (Manual)

**For Linux/macOS users or those who prefer manual control.**

### Step 1: Clone and Build

```bash
# Clone the repository
git clone https://github.com/davidtres03/EcommerceStarter.git
cd EcommerceStarter

# Navigate to the main project
cd EcommerceStarter

# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### Step 2: Configure Database

```bash
# Update connection string in appsettings.json
# Open EcommerceStarter/appsettings.json

# Find this section:
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EcommerceDB;Trusted_Connection=True;"
}

# For SQL Server Express, use:
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=EcommerceDB;Trusted_Connection=True;"

# For SQL Server with username/password:
"DefaultConnection": "Server=localhost;Database=EcommerceDB;User Id=your_user;Password=your_password;"
```

### Step 3: Run Migrations

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create database and run migrations
dotnet ef database update
```

### Step 4: Run the Application

```bash
# Development mode (with hot reload)
dotnet run

# Production mode
dotnet run --configuration Release
```

### Step 5: Access Your Store

```
🌐 Website: https://localhost:7001 (or https://localhost:5001)
🔧 Admin Panel: https://localhost:7001/Admin/Dashboard

📧 Default Login:
   Email: admin@example.com
   Password: Admin@123

🔒 IMPORTANT: Change the admin password immediately!
```

---

## ⚙️ Initial Configuration

### Step 1: Change Admin Password

1. Go to **Admin Dashboard** → **Settings** → **User Profile**
2. Enter new password (min 8 characters, 1 uppercase, 1 number, 1 special char)
3. Save changes
4. Log out and log back in with new password

### Step 2: Configure Site Settings

1. Go to **Admin** → **Settings** → **Site Settings**
2. Update:
   - **Company Name** - Your business name
   - **Site Title** - Appears in browser tab
   - **Tagline** - Short description (appears in header)
   - **Contact Email** - Customer support email
   - **Phone Number** - Optional

### Step 3: Upload Your Logo

1. Go to **Admin** → **Settings** → **Branding**
2. Upload:
   - **Logo** (recommended: 200x50px PNG with transparent background)
   - **Favicon** (recommended: 32x32px ICO or PNG)
   - **Hero Image** (optional: homepage banner, 1920x600px recommended)
3. Save changes

### Step 4: Configure Email (Optional but Recommended)

Choose one of these options:

#### Option A: Resend (Recommended - Easy Setup)

1. Sign up at [resend.com](https://resend.com) (free tier: 100 emails/day)
2. Get your API key
3. Go to **Admin** → **Settings** → **Email Configuration**
4. Select **Resend**
5. Enter API key
6. Set **From Email** (must be verified in Resend)
7. Test email
8. Save

#### Option B: SMTP (Gmail, Outlook, etc.)

1. Get SMTP credentials from your email provider
   - **Gmail:** Enable "App Passwords" in Google Account settings
   - **Outlook:** Use your regular password
2. Go to **Admin** → **Settings** → **Email Configuration**
3. Select **SMTP**
4. Enter:
   - SMTP Server (e.g., `smtp.gmail.com` for Gmail)
   - Port (465 for SSL, 587 for TLS)
   - Username (your email)
   - Password (or app password)
   - From Email
5. Test email
6. Save

---

## 🛍️ Adding Your First Product

### Step 1: Create a Category

1. Go to **Admin** → **Products** → **Categories**
2. Click **Create New Category**
3. Enter:
   - **Name** (e.g., "T-Shirts")
   - **Description** (optional)
   - **Icon** (optional - choose from Bootstrap Icons)
4. Click **Create**

### Step 2: Add a Product

1. Go to **Admin** → **Products** → **All Products**
2. Click **Create New Product**
3. Fill in:
   - **Name** (e.g., "Classic Cotton T-Shirt")
   - **Description** (detailed product info)
   - **Category** (select from dropdown)
   - **Base Price** (e.g., 19.99)
   - **SKU** (optional - stock keeping unit)
   - **Stock Quantity** (e.g., 100)
4. Upload images:
   - Click **Choose Files**
   - Select 1-5 product images
   - First image becomes the main image
5. Click **Create**

### Step 3: Add Product Variants (Optional)

If your product has variations (size, color, etc.):

1. Edit your product
2. Scroll to **Variants** section
3. Click **Add Variant**
4. For each variant:
   - **Name** (e.g., "Small - Red")
   - **SKU** (unique identifier)
   - **Price** (can differ from base price)
   - **Stock** (inventory for this variant)
   - **Attributes** (key-value pairs: Size=Small, Color=Red)
5. Save

**Example Variants:**
- Small - Blue ($19.99, 50 in stock)
- Medium - Blue ($19.99, 75 in stock)
- Large - Blue ($19.99, 60 in stock)
- Small - Red ($19.99, 40 in stock)

### Step 4: Feature Your Product (Optional)

1. Edit your product
2. Check **Is Featured**
3. Save

Featured products appear on the homepage!

---

## 💳 Setting Up Payments

### Step 1: Create a Stripe Account

1. Go to [stripe.com](https://stripe.com)
2. Sign up for an account
3. Complete verification (takes 1-2 business days for full approval)

### Step 2: Get Your Stripe Keys

1. Log in to [Stripe Dashboard](https://dashboard.stripe.com)
2. Go to **Developers** → **API keys**
3. You'll see:
   - **Publishable key** (starts with `pk_test_` for testing)
   - **Secret key** (starts with `sk_test_` for testing, click "Reveal" to see it)
4. Copy both keys

### Step 3: Configure Stripe in EcommerceStarter

1. Go to **Admin** → **Settings** → **API Keys**
2. Find **Stripe** section
3. Enter:
   - **Publishable Key** (pk_test_...)
   - **Secret Key** (sk_test_...)
   - **Webhook Secret** (optional for now)
4. Click **Save**

### Step 4: Test a Purchase

1. Open your store in a **new incognito/private window**
2. Add a product to cart
3. Go to checkout
4. Use Stripe test card:
   - **Card Number:** 4242 4242 4242 4242
   - **Expiry:** Any future date (e.g., 12/25)
   - **CVC:** Any 3 digits (e.g., 123)
   - **ZIP:** Any 5 digits (e.g., 12345)
5. Complete purchase
6. Check **Admin** → **Orders** to see your test order!

### Step 5: Go Live with Stripe (When Ready)

1. In Stripe Dashboard, complete business verification
2. Switch to **Live mode** (toggle in top right)
3. Get your **live keys** (pk_live_... and sk_live_...)
4. Update keys in **Admin** → **Settings** → **API Keys**
5. **Now all payments are REAL!**

---

## 🎨 Customizing Your Store

### Theme Customization (No Code Required!)

1. Go to **Admin** → **Settings** → **Theme**
2. Customize:
   - **Primary Color** (main brand color)
   - **Secondary Color** (accents)
   - **Heading Font** (choose from Google Fonts)
   - **Body Font** (choose from Google Fonts)
3. Click **Preview** to see changes
4. Click **Save** when happy

### Advanced Customization (For Developers)

#### Custom CSS

1. Go to **Admin** → **Settings** → **Advanced**
2. Add custom CSS:
```css
/* Example: Change button hover color */
.btn-primary:hover {
    background-color: #ff6b6b;
    border-color: #ff6b6b;
}

/* Example: Custom homepage hero */
.hero-section {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}
```
3. Save and refresh your site

#### Custom HTML Blocks

1. Go to **Admin** → **Settings** → **Advanced**
2. Add custom HTML for:
   - **Header Injection** (e.g., Google Analytics, Facebook Pixel)
   - **Footer Injection** (e.g., live chat widgets)
   - **Homepage Banner** (promotional content)

---

## 🚀 Going Live

### Pre-Launch Checklist

- [ ] Admin password changed from default
- [ ] Company information updated
- [ ] Logo and branding uploaded
- [ ] At least 5-10 products added with images
- [ ] Stripe configured with LIVE keys
- [ ] Email configured and tested
- [ ] Test purchase completed successfully
- [ ] Shipping rules configured (if applicable)
- [ ] Tax settings configured (if applicable)
- [ ] Privacy policy and terms of service pages created
- [ ] SSL certificate installed (HTTPS)

### Domain and Hosting

#### Option 1: Windows Server with IIS

1. Purchase a domain (e.g., from Namecheap, GoDaddy)
2. Get a Windows VPS (e.g., from Vultr, DigitalOcean, Azure)
3. Point domain DNS to your server IP
4. Install SSL certificate (use Let's Encrypt - free!)
5. Configure IIS with your domain
6. Update **Site Settings** with your domain

#### Option 2: Azure App Service

```bash
# Install Azure CLI
# https://docs.microsoft.com/cli/azure/install-azure-cli

# Login
az login

# Create resource group
az group create --name EcommerceRG --location eastus

# Create app service plan
az appservice plan create --name EcommercePlan --resource-group EcommerceRG --sku B1

# Create web app
az webapp create --name your-store-name --resource-group EcommerceRG --plan EcommercePlan

# Deploy
cd EcommerceStarter
dotnet publish -c Release
cd bin/Release/net8.0/publish
az webapp deployment source config-zip --resource-group EcommerceRG --name your-store-name --src publish.zip
```

### SSL Certificate (HTTPS)

**For IIS on Windows:**

1. Install **Certify The Web** - [Download](https://certifytheweb.com/)
2. Follow wizard to get free Let's Encrypt certificate
3. Certificate auto-renews every 90 days

**For Linux/Nginx:**

```bash
# Install Certbot
sudo apt install certbot python3-certbot-nginx

# Get certificate
sudo certbot --nginx -d yourdomain.com -d www.yourdomain.com

# Certificate auto-renews
```

---

## 🆘 Troubleshooting

### "Cannot connect to database"

**Solution:**
1. Check SQL Server is running:
   - Open **Services** (Windows + R → `services.msc`)
   - Find **SQL Server (SQLEXPRESS)** or **SQL Server**
   - Ensure it's **Running** (right-click → Start if not)
2. Verify connection string in `appsettings.json`
3. Try running migrations: `dotnet ef database update`

### "404 Not Found" on all pages

**Solution (IIS):**
1. Install **URL Rewrite Module** - [Download](https://www.iis.net/downloads/microsoft/url-rewrite)
2. Ensure **web.config** exists in `EcommerceStarter` folder
3. Restart IIS site

**Solution (Nginx):**
Check your Nginx config has proper proxy settings for ASP.NET Core.

### Stripe payments not working

**Checklist:**
- [ ] Keys entered correctly (no extra spaces)
- [ ] Using test keys (pk_test_...) for testing
- [ ] Using live keys (pk_live_...) for production
- [ ] Stripe account verified (for live payments)
- [ ] Test card used: 4242 4242 4242 4242

### Images not uploading

**Check:**
1. **Cloudinary configured** (Admin → Settings → API Keys → Cloudinary)
   - Get free account at [cloudinary.com](https://cloudinary.com)
   - Add Cloud Name, API Key, API Secret
2. **File size** - Max 10MB per image
3. **File format** - JPG, PNG, GIF, WebP supported

### Can't access Admin panel

**Solution:**
1. Ensure you're using the admin account (admin@example.com)
2. If password forgotten:
   ```bash
   # Reset via SQL Server Management Studio
   # or use Scripts/Test-AdminCreation.ps1
   ```
3. Check **Admin role** is assigned to your user

### Site running slow

**Quick fixes:**
1. **Enable caching** - Already enabled by default
2. **Optimize images** - Configure Cloudinary (Admin → Settings)
3. **Add indexes** - Already included in migrations
4. **Use production mode** - Ensure `ASPNETCORE_ENVIRONMENT=Production`

### Need more help?

1. **Check the docs:** [docs/](docs/)
2. **Open an issue:** [GitHub Issues](https://github.com/davidtres03/EcommerceStarter/issues)
3. **Join discussions:** [GitHub Discussions](https://github.com/davidtres03/EcommerceStarter/discussions)

---

## 🎯 Next Steps

Now that your store is running:

1. **Add more products** - Build your catalog
2. **Configure shipping** - Set up shipping rules and carriers
3. **Set up tax** - Configure sales tax by state (if required)
4. **Enable analytics** - Add Google Analytics (Admin → Settings → Analytics)
5. **Test everything** - Do a complete purchase flow test
6. **Go live!** - Switch to production domain and live Stripe keys

---

## 📚 Additional Resources

- **[API Configuration Guide](docs/guides/API_CONFIGURATION_SYSTEM.md)** - Setting up all integrations
- **[Admin Guide](docs/features/ADMIN_GUIDE.md)** - Complete admin panel documentation
- **[Security Guide](docs/deployment/SECURE_DEPLOYMENT_GUIDE.md)** - Production security best practices
- **[Deployment Guide](Scripts/README.md)** - Detailed deployment instructions
- **[CHANGELOG](CHANGELOG.md)** - Version history and updates

---

## 💡 Tips for Success

1. **Start small** - Add 5-10 products initially, then grow
2. **Test thoroughly** - Complete multiple test purchases before going live
3. **Use test mode** - Stripe test keys until you're 100% ready
4. **Backup regularly** - Your SQL Server database contains everything
5. **Monitor orders** - Check Admin dashboard daily at first
6. **Customer communication** - Respond to order inquiries promptly
7. **Update inventory** - Keep stock levels accurate
8. **Analyze metrics** - Use Admin dashboard to track sales trends

---

**Questions? Feedback? Issues?**

Open an issue on [GitHub](https://github.com/davidtres03/EcommerceStarter/issues) or start a discussion!

---

**Version:** 1.2.0  
**Last Updated:** November 17, 2025  
**License:** MIT

🎉 **Congratulations! You're now ready to start selling!** 🎉
