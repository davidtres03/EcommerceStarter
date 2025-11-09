# ? Deployment Checklist
## MyStore Supply Co. - Windows 11 Pro Host

Use this checklist to track your deployment progress. Check off items as you complete them.

---

## ?? **Pre-Deployment (Before Host Machine Setup)**

### Development Machine Prep
- [ ] Committed all code to Git
- [ ] Pushed latest changes to GitHub (`master` branch)
- [ ] All tests passing locally
- [ ] No secrets in source code (check for hardcoded passwords/keys)
- [ ] `appsettings.Production.json` in `.gitignore`
- [ ] Database migrations created and tested locally
- [ ] Application builds successfully in Release mode
- [ ] Documentation updated

### Procurement
- [ ] Windows 11 Pro license purchased
- [ ] Hardware meets minimum requirements:
  - [ ] 8GB RAM minimum (16GB recommended)
  - [ ] 100GB free disk space
  - [ ] Gigabit network card
- [ ] Static IP or DHCP reservation planned
- [ ] Domain name registered (optional but recommended)
- [ ] SSL certificate plan decided (Let's Encrypt recommended)

---

## ??? **Host Machine Setup**

### Initial Windows Configuration
- [ ] Windows 11 Pro installed
- [ ] All Windows Updates applied
- [ ] Computer renamed to `CapCollar-Host`
- [ ] Static IP configured: `192.168.1.100` (or your IP)
- [ ] Time zone set correctly
- [ ] Windows Defender enabled
- [ ] Remote Desktop enabled (optional)

### User Accounts
- [ ] Administrator password set (strong 20+ chars)
- [ ] Deployment user `deploy` created
- [ ] Deploy user added to Administrators group
- [ ] Standard user accounts removed/disabled

### Required Software
- [ ] IIS installed with required features
- [ ] .NET 8 Hosting Bundle installed
- [ ] SQL Server Express 2022 installed
- [ ] Git for Windows installed
- [ ] Text editor installed (Notepad++ or VS Code)
- [ ] All software verified with version commands

### IIS Configuration
- [ ] IIS Management Console opens successfully
- [ ] Application Pool `MyStorePool` created
- [ ] App Pool runtime set to "No Managed Code"
- [ ] Website `MyStore` created
- [ ] Website points to `C:\inetpub\wwwroot\MyStore`
- [ ] Website bindings configured (port 80)

### SQL Server Configuration
- [ ] SQL Server service running
- [ ] SQL Server Browser running
- [ ] TCP/IP protocol enabled
- [ ] SQL Server restarted after TCP/IP enable
- [ ] Can connect with SSMS or sqlcmd
- [ ] Database `EcommerceStarter` created
- [ ] Application user `CapCollarApp` created
- [ ] User permissions granted (db_datareader, db_datawriter, execute)

---

## ?? **Security Configuration**

### SSH Keys - Development Machine
- [ ] SSH directory exists: `~\.ssh`
- [ ] GitHub key generated: `id_ed25519_github`
- [ ] Host access key generated: `id_ed25519_host`
- [ ] SSH config file created with both hosts
- [ ] Private key permissions restricted
- [ ] Public keys copied to safe location

### SSH Keys - Host Machine
- [ ] OpenSSH Server installed
- [ ] SSH service started and set to automatic
- [ ] GitHub key generated on host: `id_ed25519_github`
- [ ] Dev machine's public key added to `authorized_keys`
- [ ] `authorized_keys` permissions set correctly
- [ ] SSH config file created for GitHub
- [ ] SSH firewall rule created

### GitHub Configuration
- [ ] Dev machine's GitHub key added to GitHub account
- [ ] Host machine's key added as deploy key (read-only)
- [ ] SSH connection to GitHub tested from dev machine
- [ ] SSH connection to GitHub tested from host machine
- [ ] Repository cloned to `C:\Deploy\EcommerceStarter` on host

### Windows Firewall
- [ ] Firewall enabled on all profiles
- [ ] Default inbound policy: Block
- [ ] Default outbound policy: Allow
- [ ] HTTP (80) rule created - Allow
- [ ] HTTPS (443) rule created - Allow
- [ ] SSH (22) rule created - Allow from dev IP only
- [ ] SQL (1433) rule NOT created (localhost only)
- [ ] All other ports blocked

### SQL Server Security
- [ ] SQL Server encrypted connections enabled
- [ ] SQL Server service restarted
- [ ] Application user password is strong (20+ chars)
- [ ] Admin 'sa' account disabled or strong password set
- [ ] SQL Server audit configured (optional)
- [ ] No SQL Server accessible from outside host

### Application Security
- [ ] Connection strings use SQL authentication (not Windows)
- [ ] Connection string passwords are strong
- [ ] Stripe keys configured (test keys for testing)
- [ ] Azure Key Vault URI configured
- [ ] `appsettings.Production.json` created on host
- [ ] Production settings NOT in Git
- [ ] HTTPS redirection enabled in app
- [ ] Security headers configured

---

## ?? **Deployment**

### Repository Setup
- [ ] Repository cloned to `C:\Deploy\EcommerceStarter`
- [ ] Correct branch checked out (`master`)
- [ ] Latest code pulled
- [ ] Git status shows clean working directory

### Database Migrations
- [ ] Connection string updated in `appsettings.Production.json`
- [ ] Migrations run: `dotnet ef database update`
- [ ] Migrations completed without errors
- [ ] Database tables created and verified
- [ ] Seed data created (admin user, sample products)
- [ ] Can login to database and view data

### Application Build & Publish
- [ ] NuGet packages restored
- [ ] Application builds successfully
- [ ] Tests run and pass (if applicable)
- [ ] Application published to `C:\inetpub\wwwroot\MyStore`
- [ ] `appsettings.Production.json` copied to publish folder

### IIS Deployment
- [ ] Files deployed to `C:\inetpub\wwwroot\MyStore`
- [ ] File permissions set for app pool identity
- [ ] Uploads folder has write permissions
- [ ] App pool started
- [ ] Website started
- [ ] No errors in IIS

### Deployment Scripts
- [ ] `deploy-from-git.ps1` script copied to `C:\Deploy`
- [ ] Script execution policy set: `Set-ExecutionPolicy RemoteSigned`
- [ ] Script tested and runs successfully
- [ ] Backup directory created: `C:\Deploy\Backups`
- [ ] Logs directory created: `C:\Deploy\Logs`

---

## ?? **Testing**

### Basic Functionality
- [ ] Website accessible at `http://localhost`
- [ ] Website accessible from dev machine at `http://192.168.1.100`
- [ ] Homepage loads without errors
- [ ] CSS and JavaScript files loading
- [ ] Images loading correctly
- [ ] No 404 errors in browser console

### Application Features
- [ ] Product pages display
- [ ] Shopping cart works
- [ ] User registration works
- [ ] User login works
- [ ] Admin login works (`admin@example.com`)
- [ ] Admin panel accessible
- [ ] Can create new product
- [ ] Can view orders
- [ ] Can view customers
- [ ] Dark mode toggle works
- [ ] Toast notifications appear

### Database Connectivity
- [ ] Application connects to database
- [ ] Products load from database
- [ ] User registration saves to database
- [ ] Orders save to database
- [ ] No database connection errors in logs

### Security Testing
- [ ] Can SSH from dev machine to host
- [ ] Cannot SSH from other machines
- [ ] HTTPS redirects working (after SSL setup)
- [ ] SQL Server not accessible from outside
- [ ] Admin panel requires authentication
- [ ] No sensitive data in browser (F12 tools)

---

## ?? **SSL Certificate** (Optional but Recommended)

### Let's Encrypt with win-acme
- [ ] win-acme downloaded and extracted to `C:\win-acme`
- [ ] DNS A record points to host IP
- [ ] Port 80 accessible from internet
- [ ] win-acme run and certificate obtained
- [ ] Certificate installed in IIS
- [ ] HTTPS binding added to website
- [ ] HTTP to HTTPS redirect working
- [ ] Scheduled task for auto-renewal created
- [ ] Auto-renewal tested with dry-run

---

## ?? **Monitoring & Maintenance**

### Logging
- [ ] Application logs writing to expected location
- [ ] IIS logs accessible at `C:\inetpub\logs\LogFiles`
- [ ] Windows Event Viewer shows application events
- [ ] No critical errors in any logs

### Backups
- [ ] Database backup strategy defined
- [ ] File backup strategy defined
- [ ] Backup scripts created (if using custom solution)
- [ ] Backup scheduled (Task Scheduler)
- [ ] Backup restoration tested
- [ ] Backups stored on separate drive/location

### Monitoring Tools
- [ ] Windows Performance Monitor configured
- [ ] Application Insights configured (optional)
- [ ] Health check endpoint working (`/health`)
- [ ] Email alerts configured for critical errors (optional)

### Scheduled Tasks
- [ ] Automated deployment task created (optional)
- [ ] Database backup task created
- [ ] Log cleanup task created (optional)
- [ ] SSL renewal task verified

---

## ?? **Documentation**

### Created Documents
- [ ] `SECURE_DEPLOYMENT_GUIDE.md` - Complete deployment guide
- [ ] `deploy-from-git.ps1` - Automated deployment script
- [ ] `DEPLOYMENT_CHECKLIST.md` - This checklist
- [ ] Connection strings documented (securely)
- [ ] Admin credentials documented (securely)
- [ ] Network diagram created (optional)

### Knowledge Transfer
- [ ] Deployment process documented
- [ ] Rollback procedure documented
- [ ] Common issues and solutions documented
- [ ] Maintenance tasks documented
- [ ] All passwords stored in password manager

---

## ?? **Post-Deployment**

### Final Checks
- [ ] Website has been running for 24 hours without issues
- [ ] No memory leaks detected
- [ ] No excessive CPU usage
- [ ] Database growing at expected rate
- [ ] Logs show normal activity
- [ ] All scheduled tasks running successfully

### Handoff
- [ ] Admin credentials provided to appropriate people
- [ ] Deployment guide shared with team
- [ ] Backup/restore procedure verified
- [ ] Support contacts documented
- [ ] Maintenance schedule established

### Next Steps
- [ ] Plan for high availability (optional)
- [ ] Plan for load balancing (if needed)
- [ ] Plan for CDN integration (if needed)
- [ ] Plan for monitoring upgrades
- [ ] Schedule security audit (3-6 months)

---

## ?? **Emergency Contacts**

Document these for future reference:

**Host Machine:**
- IP Address: `_________________`
- Admin Username: `_________________`
- SSH Port: `_________________`

**Database:**
- Server: `_________________`
- Database Name: `_________________`
- App Username: `_________________`

**GitHub:**
- Repository: `https://github.com/davidtres03/EcommerceStarter`
- Deploy Key: `_________________`

**Domain/SSL:**
- Domain Registrar: `_________________`
- SSL Provider: `_________________`
- Renewal Date: `_________________`

---

## ?? **Maintenance Schedule**

### Daily
- [ ] Check website is accessible
- [ ] Review error logs

### Weekly
- [ ] Review performance metrics
- [ ] Check disk space
- [ ] Review security logs
- [ ] Test backups

### Monthly
- [ ] Apply Windows Updates
- [ ] Update .NET runtime (if new version)
- [ ] Review and clean logs
- [ ] Test disaster recovery

### Quarterly
- [ ] Security audit
- [ ] Review and rotate SSH keys
- [ ] Review database performance
- [ ] Load testing (if applicable)

### Annually
- [ ] Comprehensive security review
- [ ] Infrastructure assessment
- [ ] Disaster recovery drill
- [ ] Documentation update

---

## ? **Success Criteria**

Your deployment is successful when:

- ? Website accessible from internet
- ? All features working correctly
- ? No errors in logs
- ? HTTPS enabled and working
- ? Database backed up automatically
- ? Monitoring in place
- ? Deployment process documented
- ? Team trained on maintenance

---

## ?? **Support Resources**

- **ASP.NET Core Docs:** https://docs.microsoft.com/aspnet/core
- **IIS Hosting:** https://docs.microsoft.com/aspnet/core/host-and-deploy/iis
- **Let's Encrypt:** https://letsencrypt.org
- **Windows Server Security:** https://docs.microsoft.com/windows-server/security

---

**Estimated Total Time:** 4-6 hours for experienced admin, 8-12 hours for first-time deployment

**Last Updated:** Check date when you start: `________________`

---

**Good luck with your deployment! ??**
