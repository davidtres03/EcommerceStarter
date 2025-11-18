# Unified API Configuration System - Implementation Guide

## Overview

The EcommerceStarter application now features a centralized, encrypted, and auditable API configuration management system. All external API credentials (Stripe, Cloudinary, USPS, UPS, FedEx, Claude, Ollama) are stored in a single unified database table with full encryption and change tracking.

## System Architecture

### Core Components

#### 1. **Database Models** (`Models/ApiConfiguration.cs`)

**ApiConfiguration Table**
- Unified schema supporting all API types (Stripe, Cloudinary, USPS, UPS, FedEx, Claude, Ollama)
- 5 encrypted value fields (`EncryptedValue1-5`) for flexible credential storage
- Metadata JSON field for API-specific settings
- Full audit trail support
- Composite unique index on (ApiType, Name) to prevent duplicate configurations

```csharp
public class ApiConfiguration
{
    public int Id { get; set; }
    public string ApiType { get; set; }           // "Stripe", "Cloudinary", "USPS", etc.
    public string Name { get; set; }              // "Stripe-Live", "Stripe-Test", etc.
    public bool IsActive { get; set; }            // Enable/disable without deleting
    public bool IsTestMode { get; set; }          // Test vs. production environment
    
    // Encrypted credential storage
    public string? EncryptedValue1 { get; set; }  // API Key, publishable key, account number, etc.
    public string? EncryptedValue2 { get; set; }  // Secret key, password, secondary key, etc.
    public string? EncryptedValue3 { get; set; }  // Webhook secret, meter number, etc.
    public string? EncryptedValue4 { get; set; }  // Reserved for future use
    public string? EncryptedValue5 { get; set; }  // Reserved for future use
    
    public string? MetadataJson { get; set; }     // API-specific JSON metadata
    public string? Description { get; set; }      // Admin notes
    
    // Lifecycle tracking
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime? LastValidated { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Navigation
    public ICollection<ApiConfigurationAuditLog> AuditLogs { get; set; } = new List<ApiConfigurationAuditLog>();
}
```

**ApiConfigurationAuditLog Table**
- Tracks all changes with complete accountability
- JSON serialization of what changed
- User, IP, and timestamp logging
- Test status tracking

```csharp
public class ApiConfigurationAuditLog
{
    public int Id { get; set; }
    public int ApiConfigurationId { get; set; }
    public string Action { get; set; }            // Created, Updated, Deleted, Tested, Activated, Deactivated
    public string? Changes { get; set; }          // JSON of what changed
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? TestStatus { get; set; }       // SUCCESS, FAILED, WARNING
    public string? Notes { get; set; }            // Additional context
    
    public ApiConfiguration? ApiConfiguration { get; set; }
}
```

#### 2. **Service Layer** (`Services/ApiConfigurationService.cs`)

Provides centralized CRUD, encryption/decryption, and audit logging.

**Key Methods:**

```csharp
// Retrieve configurations
Task<ApiConfiguration> GetConfigurationAsync(string apiType, string name)
Task<List<ApiConfiguration>> GetConfigurationsByTypeAsync(string apiType, bool activeOnly = false)
Task<List<ApiConfiguration>> GetAllConfigurationsAsync(bool activeOnly = false)

// Save with encryption
Task<ApiConfiguration> SaveConfigurationAsync(
    string apiType,
    string name,
    Dictionary<string, string?> encryptedValues,  // Auto-encrypted
    string? metadata = null,
    string? description = null,
    bool isTestMode = false,
    string? userId = null,
    string? userEmail = null,
    string? ipAddress = null)

// Decrypt values safely
Task<Dictionary<string, string?>> GetDecryptedValuesAsync(int configId)
Task<string?> GetDecryptedValueAsync(int configId, string fieldName)

// Management
Task DeleteConfigurationAsync(int configId, string? userId, string? userEmail, string? ipAddress)
Task SetActiveStatusAsync(int configId, bool isActive, string? userId, string? userEmail)
Task MarkAsTestedAsync(int configId, string testStatus, string? notes, string? userId, string? userEmail)

// Audit access
Task<List<ApiConfigurationAuditLog>> GetAuditLogsAsync(int configId, int limit = 50)
```

#### 3. **Admin Interface** (`Pages/Admin/Settings/ApiConfigurations.cshtml`)

Tabbed interface for managing all API configurations:

**Tabs Available:**
- **Stripe**: Manage test/live Stripe keys (Publishable, Secret, Webhook Secret)
- **Cloudinary**: Image processing API credentials (Cloud Name, API Key, Secret)
- **USPS**: Postal service carrier credentials
- **UPS**: Shipping carrier credentials
- **FedEx**: Shipping carrier credentials
- **Claude**: Anthropic AI API configuration (API Key, Model, Max Tokens)
- **Ollama**: Local AI model configuration (Endpoint, Model, Max Tokens)
- **Audit Log**: View all configuration changes with timestamps, users, and status

**Features:**
- Form validation for API-specific requirements (Stripe key format validation)
- Status indicators (Active/Inactive, Test/Live)
- Real-time encryption on save
- Bulk audit log display
- Delete with confirmation
- Enable/disable without deletion

## Setup Instructions

### Step 1: Database Migration

The database schema is created via Entity Framework Core migration:

```bash
cd EcommerceStarter-Source
dotnet ef database update
```

This creates:
- `ApiConfigurations` table with proper indexes and relationships
- `ApiConfigurationAuditLogs` table with foreign key to parent configuration

### Step 2: Environment Configuration

Ensure the encryption key is set in your environment:

```bash
# Windows PowerShell
$env:ENCRYPTION_KEY = "your-32-character-minimum-random-string"

# Linux/Mac
export ENCRYPTION_KEY="your-32-character-minimum-random-string"
```

The `ENCRYPTION_KEY` must be at least 32 characters for AES-256 encryption.

### Step 3: Service Registration

The `ApiConfigurationService` is already registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IApiConfigurationService, ApiConfigurationService>();
```

### Step 4: Migrate Existing Data (Optional)

If you have existing configurations in `StripeConfiguration` or `ApiKeySettings` tables:

**Option A: Automatic Migration**

Register the migration service in `Program.cs`:

```csharp
// In the services section
builder.Services.AddScoped<MigrateStripeConfigurationService>();

// In the app builder section (after database is ready)
using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<MigrateStripeConfigurationService>();
    
    var stripeResult = await migrationService.MigrateStripeConfigurationsAsync();
    var shippingResult = await migrationService.MigrateShippingConfigurationsAsync();
    
    if (stripeResult.Success)
        logger.LogInformation("Stripe migration: {Message}", stripeResult.Message);
    if (shippingResult.Success)
        logger.LogInformation("Shipping migration: {Message}", shippingResult.Message);
}
```

**Option B: Manual SQL Migration**

Use the SQL scripts provided in the migration files to copy data manually if needed.

## Usage Examples

### Storing Stripe Configuration

```csharp
// In admin page handler
await _apiConfigService.SaveConfigurationAsync(
    apiType: "Stripe",
    name: "Stripe-Live",
    encryptedValues: new Dictionary<string, string?>
    {
        { "Value1", publishableKey },  // Auto-encrypted
        { "Value2", secretKey },       // Auto-encrypted
        { "Value3", webhookSecret }    // Auto-encrypted
    },
    isTestMode: false,
    description: "Live Stripe configuration",
    userId: userId,
    userEmail: userEmail,
    ipAddress: ipAddress
);
```

### Retrieving and Decrypting Configuration

```csharp
// Get decrypted values
var decrypted = await _apiConfigService.GetDecryptedValuesAsync(configId);
var publishableKey = decrypted["Value1"];
var secretKey = decrypted["Value2"];
var webhookSecret = decrypted["Value3"];

// Use with Stripe
var requestOptions = new RequestOptions { ApiKey = secretKey };
var service = new ChargeService();
// ... use service with decrypted keys
```

### Retrieving Configuration by Type

```csharp
// Get all active Cloudinary configs
var cloudinaryConfigs = await _apiConfigService
    .GetConfigurationsByTypeAsync("Cloudinary", activeOnly: true);

foreach (var config in cloudinaryConfigs)
{
    var decrypted = await _apiConfigService.GetDecryptedValuesAsync(config.Id);
    var cloudName = decrypted["Value1"];
    var apiKey = decrypted["Value2"];
    // Initialize Cloudinary with these values
}
```

### Updating Service Dependencies

For services that currently use the old configuration tables:

**Before (using StripeConfiguration):**
```csharp
public class StripePaymentService
{
    private readonly StripeConfiguration _config;
    
    public async Task ProcessPayment(decimal amount)
    {
        StripeConfiguration.SetApiKey(_config.EncryptedSecretKey);  // Not ideal - encrypted key used directly
        // ...
    }
}
```

**After (using ApiConfigurationService):**
```csharp
public class StripePaymentService
{
    private readonly IApiConfigurationService _apiConfigService;
    
    public async Task ProcessPayment(decimal amount, int stripeConfigId)
    {
        var decrypted = await _apiConfigService.GetDecryptedValuesAsync(stripeConfigId);
        StripeConfiguration.SetApiKey(decrypted["Value2"]);  // Decrypted secret key
        // ...
    }
}
```

## Field Mapping by API Type

### Stripe
- **Value1**: Publishable Key (pk_live_... or pk_test_...)
- **Value2**: Secret Key (sk_live_... or sk_test_...)
- **Value3**: Webhook Secret (whsec_...)
- **MetadataJson**: `{ "mode": "live|test", "description": "..." }`

### Cloudinary
- **Value1**: Cloud Name
- **Value2**: API Key
- **Value3**: API Secret
- **Value4**: Optional: Secure URL parameter
- **MetadataJson**: `{ "uploadPath": "products/", "transformations": [...] }`

### USPS
- **Value1**: API/User ID
- **Value2**: Password
- **Value3**: Optional: Account number
- **MetadataJson**: `{ "useSandbox": false, "endpoint": "..." }`

### UPS
- **Value1**: Client ID
- **Value2**: Client Secret
- **Value3**: Account Number
- **Value4**: Optional: Parameter
- **MetadataJson**: `{ "useSandbox": false, "rateEndpoint": "..." }`

### FedEx
- **Value1**: API Key
- **Value2**: Password
- **Value3**: Account Number
- **Value4**: Meter Number
- **MetadataJson**: `{ "useSandbox": false, "customerTransactionId": "..." }`

### Claude
- **Value1**: API Key (sk-ant-...)
- **Value2**: Optional: Secondary key
- **Value3**: Not used
- **MetadataJson**: `{ "model": "claude-3-sonnet-20240229", "maxTokens": 2000, "enabled": true }`

### Ollama
- **Value1**: API Key (optional for local)
- **Value2**: Optional: Authentication header
- **Value3**: Not used
- **MetadataJson**: `{ "endpoint": "http://localhost:11434", "model": "llama2", "maxTokens": 2000, "enabled": true }`

## Security Considerations

### Encryption

- All credential values are encrypted using AES-256 before database storage
- Encryption key is stored in environment variable (never hardcoded)
- Minimum 32-character encryption key required
- Decryption happens only when values are explicitly requested

### Access Control

- Admin-only access via `[Authorize(Roles = "Admin")]` attribute
- All API configuration operations are logged with user identity
- IP address of requesting admin is logged for audit trail

### Best Practices

1. **Never log decrypted values**: The service methods handle decryption safely
2. **Use configuration by ID in code**: Store references to configuration IDs, not the values themselves
3. **Rotate keys regularly**: Change the ENCRYPTION_KEY periodically and re-encrypt all configurations
4. **Review audit logs**: Regularly check `ApiConfigurationAuditLogs` for unauthorized access attempts
5. **Restrict admin access**: Limit who can access `/Admin/Settings/ApiConfigurations` page

## Monitoring and Troubleshooting

### Check Configuration Status

```csharp
// List all active configurations
var allConfigs = await _apiConfigService.GetAllConfigurationsAsync(activeOnly: true);

foreach (var config in allConfigs)
{
    var lastValidated = config.LastValidated;
    var isActive = config.IsActive;
    var isTest = config.IsTestMode;
    
    Console.WriteLine($"{config.ApiType} ({config.Name}): Active={isActive}, LastValidated={lastValidated}");
}
```

### View Audit Trail

```csharp
// Get recent changes for specific configuration
var auditLogs = await _apiConfigService.GetAuditLogsAsync(configId, limit: 100);

foreach (var log in auditLogs)
{
    Console.WriteLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss} - {log.Action} by {log.UserEmail} from {log.IpAddress}");
    Console.WriteLine($"  Changes: {log.Changes}");
}
```

### Test Configuration

```csharp
// Mark a configuration as tested and record result
await _apiConfigService.MarkAsTestedAsync(
    configId: configId,
    testStatus: "SUCCESS",  // or "FAILED", "WARNING"
    notes: "API credentials validated successfully",
    userId: userId,
    userEmail: userEmail
);
```

## Migration Checklist

- [ ] Database migration applied (`dotnet ef database update`)
- [ ] Encryption key set in environment variables
- [ ] Service registered in DI container (already done in Program.cs)
- [ ] Admin page accessible at `/Admin/Settings/ApiConfigurations`
- [ ] Test navigation menu link appears correctly
- [ ] Existing data migrated (if applicable):
  - [ ] Stripe configuration moved to unified table
  - [ ] USPS/UPS/FedEx configurations moved
  - [ ] Audit logs preserved
- [ ] Services updated to use new configuration source:
  - [ ] StripePaymentService uses ApiConfigurationService
  - [ ] CloudinaryService uses ApiConfigurationService
  - [ ] ShippingService uses ApiConfigurationService
  - [ ] AIService uses ApiConfigurationService
- [ ] Audit logs reviewed to verify migrations succeeded
- [ ] Backup of old configuration tables created (before deprecation)
- [ ] Monitoring alerts set up for configuration changes

## Future Enhancements

1. **Configuration Versioning**: Track multiple versions of credentials with rollback capability
2. **Key Rotation**: Automatically rotate encryption keys on a schedule
3. **Configuration Sync**: Sync configurations across multiple deployments
4. **API Testing**: Built-in test buttons for each API type to validate configurations
5. **Usage Analytics**: Track which configurations are actively being used
6. **Cost Tracking**: Monitor spending by API type with warnings for unusual patterns
7. **Configuration Templates**: Pre-configured templates for common API types

## Support and Questions

For issues or questions about the unified API configuration system, refer to:
- `Services/ApiConfigurationService.cs` - Core service implementation
- `Models/ApiConfiguration.cs` - Data model definitions
- `Pages/Admin/Settings/ApiConfigurations.cshtml.cs` - Admin page logic
- Database migration files in `Migrations/` folder
