# ?? EcommerceStarter Deployment Scripts

This directory contains automated deployment scripts for setting up EcommerceStarter on various platforms.

## ?? Available Scripts

### **Deploy-Windows.ps1** (Windows + IIS)
Comprehensive PowerShell deployment wizard for Windows Server or Windows 10/11 with IIS.

**Features:**
- ? Automatic prerequisite detection and installation
- ? Interactive setup wizard with validation
- ? Database creation and migration
- ? Application publishing
- ? IIS configuration (use IIS-Helpers.ps1 for advanced setup)
- ? Detailed logging

**Requirements:**
- Windows 10/11 or Windows Server 2016+
- PowerShell 5.1 or higher
- Administrator privileges

**Usage:**
```powershell
# Interactive installation
.\Deploy-Windows.ps1

# With IIS configuration (advanced)
.\Deploy-Windows.ps1
# Then run:
. .\IIS-Helpers.ps1
# And call IIS functions manually
```

---

### **IIS-Helpers.ps1** (IIS Configuration Functions)
Helper functions for IIS configuration. Can be used standalone or imported by Deploy-Windows.ps1.

**Functions:**
- `New-IISAppPool` - Creates and configures application pool
- `Publish-Application` - Publishes .NET application
- `New-IISSite` - Creates IIS website with bindings
- `New-IISSelfSignedCertificate` - Creates SSL certificate
- `Set-IISPermissions` - Configures NTFS permissions
- `Install-ASPNETCoreModule` - Checks for ASP.NET Core Module
- `Open-BrowserToSite` - Opens browser to admin panel

**Usage:**
```powershell
# Import functions
. .\IIS-Helpers.ps1

# Example: Create app pool and site
$config = @{
    CompanyName = "My Store"
    SiteName = "MyStore"
    Domain = "localhost"
    Port = "443"
}

New-IISAppPool -Config $config -AppPoolName "MyStoreAppPool"
Publish-Application -AppPath "..\EcommerceStarter" -PublishPath "C:\inetpub\MyStore"
New-IISSite -Config $config -AppPoolName "MyStoreAppPool" -PhysicalPath "C:\inetpub\MyStore"
Set-IISPermissions -PhysicalPath "C:\inetpub\MyStore" -AppPoolName "MyStoreAppPool"
Open-BrowserToSite -Config $config
```

---

### **Deploy-Linux.sh** (Linux + Nginx) - Coming Soon
Bash deployment script for Ubuntu/Debian with Nginx reverse proxy.

---

### **docker-compose.yml** (Docker) - Coming Soon
One-command Docker deployment for any platform.

---

## ?? Quick Start

### Windows Deployment (Automated)

1. **Open PowerShell as Administrator**

2. **Navigate to the Scripts directory**
   ```powershell
   cd path\to\EcommerceStarter\Scripts
   ```

3. **Run the deployment script**
   ```powershell
   .\Deploy-Windows.ps1
   ```

4. **Follow the interactive prompts**
   - Enter your company information
   - Configure admin account
   - Set up database connection
   - Configure payment and email (optional)

5. **Manual IIS Setup** (After script completes)
   ```powershell
   # Import IIS helpers
   . .\IIS-Helpers.ps1
   
   # Get your configuration (adjust as needed)
   $config = @{
       CompanyName = "My Store"
       SiteName = "MyStore"
       Domain = "localhost"
       Port = "443"
   }
   
   # Create app pool
   New-IISAppPool -Config $config -AppPoolName "EcommerceStarterAppPool"
   
   # Publish application
   $publishPath = "C:\inetpub\EcommerceStarter"
   Publish-Application -AppPath "..\EcommerceStarter" -PublishPath $publishPath
   
   # Create website
   New-IISSite -Config $config -AppPoolName "EcommerceStarterAppPool" -PhysicalPath $publishPath
   
   # Set permissions
   Set-IISPermissions -PhysicalPath $publishPath -AppPoolName "EcommerceStarterAppPool"
   
   # Open browser
   Open-BrowserToSite -Config $config
   ```

---

## ?? What Gets Installed

The Windows deployment script will automatically:

### Prerequisites (Auto-Installed if Missing)
- ? .NET 8 SDK
- ? SQL Server Express 2022
- ? IIS with required features
- ? URL Rewrite Module (recommended)

### Application Setup
- ? Creates SQL Server database
- ? Runs Entity Framework migrations
- ? Configures connection strings
- ? Builds application in Release mode
- ? Sets up initial admin user

### Manual IIS Configuration (using IIS-Helpers.ps1)
- Creates application pool (.NET Core, Always Running)
- Publishes application to physical path
- Configures website with HTTP/HTTPS bindings
- Creates self-signed SSL certificate
- Sets NTFS permissions for IIS
- Starts website

---

## ?? Configuration

The deployment script will prompt you for:

### Required Settings
- **Company Name** - Your store's name
- **Admin Email** - Administrator email address
- **Admin Password** - Secure password (min 6 characters)
- **Database Server** - SQL Server instance (default: localhost\SQLEXPRESS)
- **Database Name** - Database name (default: MyStore)

### Optional Settings
- **Stripe Keys** - Payment processing (can configure later)
- **Email Provider** - Transactional emails (Resend, SMTP, or skip)
- **Domain Name** - Your website domain
- **HTTPS Port** - SSL port (default: 443)

---

## ?? Logs

Deployment logs are saved to:
```
Scripts/Logs/deployment-YYYYMMDD-HHmmss.log
```

Last deployment configuration is saved to:
```
Scripts/last-deployment-config.json
```

---

## ?? Troubleshooting

### Common Issues

**Issue:** ".NET SDK not found after installation"
**Solution:** Close and reopen PowerShell to refresh environment variables

**Issue:** "SQL Server connection failed"
**Solution:** 
- Ensure SQL Server service is running
- Check firewall settings
- Verify instance name (usually `localhost\SQLEXPRESS`)

**Issue:** "Access denied" errors
**Solution:** Ensure you're running PowerShell as Administrator

**Issue:** "IIS installation requires restart"
**Solution:** Restart your computer and run the script again

**Issue:** "Website not accessible"
**Solution:**
- Check Windows Firewall (allow port 80/443)
- Verify IIS bindings match your domain
- Check application pool is running
- Review IIS logs in `C:\inetpub\logs\LogFiles`

---

## ?? Security Notes

- **Change default admin password immediately** after first login
- **Replace self-signed certificate** in production (use Let's Encrypt or commercial CA)
- **Configure firewall rules** to allow only necessary ports
- **Use User Secrets** for Stripe keys (never commit to source control)
- **Enable rate limiting** in Admin Panel ? Security Settings
- **Keep .NET runtime updated** for security patches

---

## ?? Next Steps After Installation

1. **Login to Admin Panel**
   - Navigate to `/Admin/Dashboard`
   - Use the email and password you configured

2. **Complete Setup Wizard** (if available)
   - Configure branding (logo, colors, fonts)
   - Set up payment processing
   - Configure email notifications
   - Add your first products

3. **Customize Your Store**
   - Update site settings
   - Upload logo and images
   - Configure tax rates (if needed)
   - Set up shipping rules

4. **Test Everything**
   - Create a test order
   - Test payment processing (use Stripe test cards)
   - Verify email notifications
   - Check mobile responsiveness

---

## ?? Contributing

Found a bug or have a suggestion? Please:
1. Check existing issues on GitHub
2. Create a new issue with detailed information
3. Or submit a pull request!

---

## ?? Support

- **Documentation:** [/docs](/docs)
- **Issues:** [GitHub Issues](https://github.com/yourusername/EcommerceStarter/issues)
- **Discussions:** [GitHub Discussions](https://github.com/yourusername/EcommerceStarter/discussions)

---

**Version:** 1.0.0  
**Last Updated:** January 2025  
**Maintained By:** EcommerceStarter Team
