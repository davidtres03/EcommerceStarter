# ?? Configuration Guide
## MyStore Supply Co.

Complete guide for configuring environment variables, database, and application settings.

---

## ?? Table of Contents

1. [Environment Setup](#environment-setup)
2. [Database Configuration](#database-configuration)
3. [Application Settings](#application-settings)
4. [Development vs Production](#development-vs-production)
5. [Troubleshooting](#troubleshooting)

---

## ?? Environment Setup

### Development Environment

**Requirements:**
- .NET 8 SDK
- SQL Server LocalDB (included with Visual Studio)
- Visual Studio 2022 or VS Code
- Git

**Verify Installation:**
```powershell
dotnet --version  # Should show 8.0.x
git --version
sqlcmd -L        # List SQL Server instances
```

### Environment Variables

**Option 1: User Secrets (Recommended for Development)**

Initialize user secrets:
```powershell
cd EcommerceStarter
dotnet user-secrets init
```

Set configuration values:
```powershell
# Stripe Keys
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY"

# Database Connection (optional if using LocalDB)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"

# Azure Key Vault (if using)
dotnet user-secrets set "KeyVault:VaultUri" "https://your-vault.vault.azure.net/"
```

View all secrets:
```powershell
dotnet user-secrets list
```

Remove a secret:
```powershell
dotnet user-secrets remove "Stripe:SecretKey"
```

Clear all secrets:
```powershell
dotnet user-secrets clear
```

**Option 2: System Environment Variables**

Windows:
```powershell
# Current session
$env:Stripe__PublishableKey = "pk_test_YOUR_KEY"
$env:Stripe__SecretKey = "sk_test_YOUR_KEY"

# Permanent (current user)
[System.Environment]::SetEnvironmentVariable("Stripe__PublishableKey", "pk_test_YOUR_KEY", "User")
[System.Environment]::SetEnvironmentVariable("Stripe__SecretKey", "sk_test_YOUR_KEY", "User")

# Permanent (system-wide, requires admin)
[System.Environment]::SetEnvironmentVariable("Stripe__PublishableKey", "pk_test_YOUR_KEY", "Machine")
```

Linux/Mac:
```bash
export Stripe__PublishableKey="pk_test_YOUR_KEY"
export Stripe__SecretKey="sk_test_YOUR_KEY"

# Add to ~/.bashrc or ~/.zshrc for persistence
echo 'export Stripe__PublishableKey="pk_test_YOUR_KEY"' >> ~/.bashrc
```

**Option 3: appsettings.Development.json**

?? **WARNING**: Only for local development. NEVER commit secrets to Git!

```json
{
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_KEY",
    "SecretKey": "sk_test_YOUR_KEY"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EcommerceStarter;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

Add to `.gitignore`:
```gitignore
appsettings.Development.json
appsettings.Production.json
appsettings.*.json
!appsettings.json
```

---

## ?? Database Configuration

### Connection Strings

**LocalDB (Development):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EcommerceStarter;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

**SQL Server Express (Production):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=EcommerceStarter;User Id=CapCollarApp;Password=YourStrongPassword;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true"
  }
}
```

**Azure SQL Database:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Database=EcommerceStarter;User ID=your-admin;Password=YourStrongPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

### Database Migrations

**Create Migration:**
```powershell
dotnet ef migrations add InitialCreate --project EcommerceStarter
```

**Update Database:**
```powershell
dotnet ef database update --project EcommerceStarter
```

**View Pending Migrations:**
```powershell
dotnet ef migrations list --project EcommerceStarter
```

**Remove Last Migration:**
```powershell
dotnet ef migrations remove --project EcommerceStarter
```

**Generate SQL Script:**
```powershell
dotnet ef migrations script -o migrations.sql --project EcommerceStarter
```

### Database Context Configuration

**Program.cs:**
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    ));
```

### Data Seeding

Automatic seeding happens on first run:

```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.Initialize(services);
}
```

**SeedData.cs:**
```csharp
public static async Task Initialize(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

    // Seed Roles
    string[] roleNames = { "Admin", "Customer" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Seed Admin User
    var adminEmail = "admin@example.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // Seed Products
    if (!await context.Products.AnyAsync())
    {
        var products = new List<Product>
        {
            new Product
            {
                Name = "Mushroom T-Shirt",
                Description = "100% cotton t-shirt with mushroom design",
                Price = 24.99M,
                Category = "Apparel",
                SubCategory = "Tshirts",
                ImageUrl = "/images/products/mushroom-tshirt.svg",
                StockQuantity = 100
            },
            // More products...
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}
```

---

## ?? Application Settings

### appsettings.json (Base Configuration)

**DO commit to Git - no secrets here!**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "ApplicationInsights": {
    "ConnectionString": ""
  }
}
```

### appsettings.Development.json Template

**DO NOT commit actual values! Use this as a template:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EcommerceStarter;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_PUBLISHABLE_KEY",
    "SecretKey": "sk_test_YOUR_SECRET_KEY",
    "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET"
  },
  "KeyVault": {
    "VaultUri": "https://your-vault.vault.azure.net/"
  }
}
```

### appsettings.Production.json Template

**NEVER commit to Git! Store securely!**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=EcommerceStarter;User Id=CapCollarApp;Password=STRONG_PASSWORD_HERE;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true"
  },
  "Stripe": {
    "PublishableKey": "pk_live_YOUR_LIVE_PUBLISHABLE_KEY",
    "SecretKey": "sk_live_YOUR_LIVE_SECRET_KEY",
    "WebhookSecret": "whsec_YOUR_LIVE_WEBHOOK_SECRET"
  },
  "KeyVault": {
    "VaultUri": "https://your-production-vault.vault.azure.net/"
  },
  "AllowedHosts": "yourdomain.com,www.yourdomain.com"
}
```

### Configuration Hierarchy

ASP.NET Core loads configuration in this order (later sources override earlier):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment Variables
5. Command-line arguments
6. Azure Key Vault (if configured)

Example:
```
appsettings.json has: "Stripe:SecretKey": "default_value"
User Secrets has:     "Stripe:SecretKey": "sk_test_123"
Environment Var has:  Stripe__SecretKey = "sk_test_456"

Result: "sk_test_456" (environment variable wins)
```

### Accessing Configuration

**In Program.cs:**
```csharp
var stripeKey = builder.Configuration["Stripe:SecretKey"];
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
```

**Via Dependency Injection:**
```csharp
public class MyService
{
    private readonly IConfiguration _configuration;
    
    public MyService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void DoSomething()
    {
        var stripeKey = _configuration["Stripe:SecretKey"];
    }
}
```

**Using Options Pattern (Recommended):**
```csharp
// StripeSettings.cs
public class StripeSettings
{
    public string PublishableKey { get; set; }
    public string SecretKey { get; set; }
    public string WebhookSecret { get; set; }
}

// Program.cs
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// In service
public class MyService
{
    private readonly StripeSettings _stripeSettings;
    
    public MyService(IOptions<StripeSettings> stripeSettings)
    {
        _stripeSettings = stripeSettings.Value;
    }
    
    public void DoSomething()
    {
        var key = _stripeSettings.SecretKey;
    }
}
```

---

## ?? Development vs Production

### Development Environment

**Features:**
- LocalDB for database
- Test Stripe keys
- Detailed logging
- Hot reload enabled
- Exception page with stack traces
- Browser dev tools enabled

**Configuration:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DetailedErrors": true,
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_..."
  }
}
```

**Run:**
```powershell
dotnet run
# Or
dotnet watch run  # With hot reload
```

### Production Environment

**Features:**
- SQL Server Express/Azure SQL
- Live Stripe keys
- Minimal logging
- Error pages (no stack traces)
- HTTPS enforced
- Security headers

**Configuration:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DetailedErrors": false,
  "Stripe": {
    "PublishableKey": "pk_live_...",
    "SecretKey": "sk_live_..."
  },
  "AllowedHosts": "yourdomain.com"
}
```

**Set Environment:**
```powershell
# Windows
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Production
```

**Run:**
```powershell
dotnet run --environment Production
```

### Environment Detection

**In code:**
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
```

**In Razor:**
```razor
<environment include="Development">
    <script src="~/js/app.js"></script>
</environment>

<environment exclude="Development">
    <script src="~/js/app.min.js" asp-append-version="true"></script>
</environment>
```

---

## ?? Troubleshooting

### Configuration Not Loading

**Issue:** Changes to appsettings not taking effect

**Solutions:**
```powershell
# 1. Restart application (Ctrl+C, then dotnet run)

# 2. Clear build cache
dotnet clean
dotnet build

# 3. Check environment
echo $env:ASPNETCORE_ENVIRONMENT

# 4. Verify configuration loading order
# User Secrets > Environment Variables > appsettings.json
```

### Database Connection Fails

**Issue:** Cannot connect to database

**Solutions:**
```powershell
# 1. Verify SQL Server is running
Get-Service -Name 'MSSQL$SQLEXPRESS'

# 2. Test connection string
sqlcmd -S "localhost\SQLEXPRESS" -U "CapCollarApp" -P "YourPassword" -Q "SELECT 1"

# 3. Check connection string format
# Escape backslashes in JSON: "Server=localhost\\SQLEXPRESS"

# 4. Verify database exists
sqlcmd -S "localhost\SQLEXPRESS" -Q "SELECT name FROM sys.databases"

# 5. Run migrations
dotnet ef database update
```

### User Secrets Not Working

**Issue:** User secrets not being read

**Solutions:**
```powershell
# 1. Verify user secrets are initialized
dotnet user-secrets list

# 2. Check project file for UserSecretsId
# Should have: <UserSecretsId>...</UserSecretsId>

# 3. Re-initialize if needed
dotnet user-secrets init

# 4. Verify you're in Development environment
$env:ASPNETCORE_ENVIRONMENT = "Development"

# 5. Check secrets location
# Windows: %APPDATA%\Microsoft\UserSecrets\{id}\secrets.json
# Mac/Linux: ~/.microsoft/usersecrets/{id}/secrets.json
```

### Stripe Keys Not Found

**Issue:** "Stripe API key not found" error

**Solutions:**
```powershell
# 1. Verify keys are set
dotnet user-secrets list | Select-String "Stripe"

# 2. Check key format
# Test keys start with: pk_test_, sk_test_
# Live keys start with: pk_live_, sk_live_

# 3. Set keys again
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY"

# 4. Restart application after setting keys

# 5. Verify in Startup/Program.cs
# builder.Configuration["Stripe:SecretKey"] should not be null
```

### Migration Errors

**Issue:** Entity Framework migration fails

**Solutions:**
```powershell
# 1. Verify connection string
dotnet ef dbcontext info

# 2. Check for pending migrations
dotnet ef migrations list

# 3. Remove failed migration
dotnet ef migrations remove

# 4. Re-create migration
dotnet ef migrations add MigrationName

# 5. Apply with verbose logging
dotnet ef database update --verbose

# 6. If SQL translation error (common issue):
# Check for client-side evaluation in queries
# Example: .Where(p => p.StartDate.Date == DateTime.Now.Date)
# Fix: .Where(p => p.StartDate.Date == DateTime.UtcNow.Date)
```

---

## ? Configuration Checklist

### Development Setup
- [ ] .NET 8 SDK installed
- [ ] SQL Server LocalDB available
- [ ] User secrets initialized
- [ ] Stripe test keys configured
- [ ] Database migrations applied
- [ ] Seed data created
- [ ] Application runs without errors

### Production Setup
- [ ] SQL Server Express installed
- [ ] Database created
- [ ] Application user created with minimal permissions
- [ ] Connection string configured (not in source code)
- [ ] Stripe live keys in secure storage (Azure Key Vault)
- [ ] HTTPS enabled
- [ ] AllowedHosts configured
- [ ] Logging configured appropriately
- [ ] Error pages configured
- [ ] Security headers enabled

### Security Checklist
- [ ] No secrets in source code
- [ ] appsettings.Production.json in .gitignore
- [ ] Different keys for dev/prod
- [ ] Strong passwords used
- [ ] Database uses SQL authentication (not Windows auth for app)
- [ ] Connection strings encrypted in production
- [ ] User secrets only for development
- [ ] Environment variables or Key Vault for production

---

## ?? Additional Resources

- [ASP.NET Core Configuration](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/)
- [User Secrets](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [Entity Framework Migrations](https://docs.microsoft.com/ef/core/managing-schemas/migrations/)
- [Azure Key Vault](https://docs.microsoft.com/azure/key-vault/)

---

**Everything consolidated from:**
- ENVIRONMENT_VARIABLES_SETUP.md
- CONFIGURATION_CLEANUP_COMPLETE.md
- APPSETTINGS_DEVELOPMENT_TEMPLATE.md
- DATABASE_MIGRATION_FIXED.md
