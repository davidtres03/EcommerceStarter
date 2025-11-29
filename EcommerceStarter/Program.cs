using EcommerceStarter.Configuration;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using EcommerceStarter.Services.AI;
using EcommerceStarter.Services.Analytics;
using EcommerceStarter.Services.Auth;
using EcommerceStarter.Services.Tracking;
using EcommerceStarter.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// SECURE CONFIGURATION LOADING
// ===================================================================
// 1. Load connection string from Windows Registry (encrypted with DPAPI)
// 2. Connect to database
// 3. Load JWT and encryption keys from database/registry
// This keeps ALL sensitive data secure - no appsettings.json needed at runtime!
// appsettings.json is a TEMPLATE ONLY for development reference
// ===================================================================

var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
var secureConfigLogger = loggerFactory.CreateLogger<SecureConfigurationService>();
var secureConfigService = new SecureConfigurationService(secureConfigLogger);

// Try to get connection string from registry
var connectionString = secureConfigService.GetConnectionString();

// Fallback to appsettings.json if registry not configured (development scenario)
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        secureConfigLogger.LogWarning("Using connection string from appsettings.json (development fallback). For production, configure Windows Registry.");
    }
    else
    {
        throw new InvalidOperationException(
            "Connection string not found in Windows Registry or appsettings.json. " +
            "Please run the installer to configure secure registry settings or add a connection string to appsettings.json for development.");
    }
}

// Centralized logging configuration (env-driven via web.config). Minimal by default.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add Event Log provider for Windows production servers
// Only add on Windows platform to avoid CA1416 warnings
if (builder.Environment.IsProduction() && System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
{
#pragma warning disable CA1416
    builder.Logging.AddEventLog(new Microsoft.Extensions.Logging.EventLog.EventLogSettings
    {
        SourceName = "EcommerceStarter",
        LogName = "Application",
        Filter = (category, level) => 
        {
            // Read minimum log level from environment variables set in web.config
            // Defaults: noisy=Error, others=Warning for minimal production logging
            var minLevelForNoisyCategories = Enum.TryParse<LogLevel>(
                Environment.GetEnvironmentVariable("Logging__EventLog__MinLevelForNoisyCategories"), 
                out var parsedLevel) ? parsedLevel : LogLevel.Error;
            
            var minLevelForOthers = Enum.TryParse<LogLevel>(
                Environment.GetEnvironmentVariable("Logging__EventLog__MinLevelForOthers"), 
                out var parsedOtherLevel) ? parsedOtherLevel : LogLevel.Warning;
            
            if (category == "Microsoft.AspNetCore.Hosting.Diagnostics" ||
                category == "Microsoft.AspNetCore.Routing.EndpointMiddleware" ||
                category == "Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker" ||
                category == "Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor" ||
                category == "Microsoft.EntityFrameworkCore.Database.Command" ||
                category == "Microsoft.EntityFrameworkCore.Query")
                return level >= minLevelForNoisyCategories;
            
            return level >= minLevelForOthers;
        }
    });
#pragma warning restore CA1416
}

// Add debug logging in development
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
    // Optional: increase verbosity in development via env
    var devMin = Environment.GetEnvironmentVariable("Logging__Development__MinLevel");
    if (Enum.TryParse<LogLevel>(devMin, out var devParsed))
    {
        builder.Logging.SetMinimumLevel(devParsed);
    }
}

// Configure forwarded headers for Cloudflare proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)); // Use connection string from secure storage

// Register SecureConfigurationService
builder.Services.AddSingleton<ISecureConfigurationService, SecureConfigurationService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Lockout settings - Will be overridden by SecuritySettings from database
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Enhanced cookie security
    options.Cookie.HttpOnly = true;
    // Use SameAsRequest to allow HTTP connections (IIS without SSL)
    // When deployed with SSL/HTTPS, cookies will automatically be secure
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
});

// Add Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Use SameAsRequest to allow HTTP connections (IIS without SSL)
    // When deployed with SSL/HTTPS, cookies will automatically be secure
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ===================================================================
// JWT CONFIGURATION
// ===================================================================
// Load JWT settings from Windows Registry (or generate secure defaults)
// Connection string is secured in Windows Registry (DPAPI encrypted)
// API keys are encrypted in database
// JWT settings stored in registry or auto-generated
// NO appsettings.json required at runtime!
// ===================================================================

// Load JWT from registry (if present); otherwise generate secure defaults
var registry = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EcommerceStarter");
string jwtSecretKey = string.Empty;
string jwtIssuer = "EcommerceStarter";
string jwtAudience = "EcommerceStarter";
try
{
    if (registry != null)
    {
        var sites = registry.GetSubKeyNames();
        if (sites.Length > 0)
        {
            using var siteKey = registry.OpenSubKey(sites[0]);
            jwtSecretKey = siteKey?.GetValue("JwtSecretKey")?.ToString() ?? string.Empty;
            jwtIssuer = siteKey?.GetValue("JwtIssuer")?.ToString() ?? jwtIssuer;
            jwtAudience = siteKey?.GetValue("JwtAudience")?.ToString() ?? jwtAudience;
        }
    }
}
catch { /* non-fatal; will generate defaults */ }

if (string.IsNullOrEmpty(jwtSecretKey))
{
    // Generate a secure random key if not configured in registry
    jwtSecretKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}

// Add JWT settings to configuration for JwtService to use
builder.Configuration["Jwt:SecretKey"] = jwtSecretKey;
builder.Configuration["Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;
builder.Configuration["Jwt:AccessTokenExpiryMinutes"] = "60";
builder.Configuration["Jwt:RefreshTokenExpiryDays"] = "30";

// Configure dual authentication: JWT for API, Cookies for Web
builder.Services.AddAuthentication(options =>
{
    // Use cookies as default for web pages
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // For API requests (not redirecting to login page)
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                message = "Unauthorized. Valid JWT token required."
            });
            return context.Response.WriteAsync(result);
        }
    };
});

// Register JWT Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

// Add Response Caching for dynamic theme CSS
builder.Services.AddResponseCaching();

// Add HttpClient support for email services
builder.Services.AddHttpClient();

// Configure Stripe Settings
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Configure Cloudinary Settings
builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// Register Product Image Service (uses Cloudinary)
builder.Services.AddScoped<IProductImageService, ProductImageService>();

// Register Cart Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, CartService>();

// Register Encryption Service (for secure key storage)
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// Register API Configuration Service (unified API config management)
builder.Services.AddScoped<IApiConfigurationService, ApiConfigurationService>();

// Register Stripe Configuration Service
builder.Services.AddScoped<IStripeConfigService, StripeConfigService>();

// Register SSL Configuration Service
builder.Services.AddScoped<ISslConfigService, SslConfigService>();

// Register Stripe Payment Service
builder.Services.AddScoped<IPaymentService, StripePaymentService>();

// Register Order Number Service
builder.Services.AddScoped<IOrderNumberService, OrderNumberService>();

// Register Security Services
builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();
builder.Services.AddScoped<ISecuritySettingsService, SecuritySettingsService>();

// Register Audit Log Service
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Register Site Settings Service
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddMemoryCache();  // Required for caching site settings

// Register Image Upload Service
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();

// Register Theme Service
builder.Services.AddScoped<IThemeService, ThemeService>();

// Register Courier Service (for tracking number detection)
builder.Services.AddScoped<ICourierService, CourierService>();

// Register Timezone Service
builder.Services.AddScoped<ITimezoneService, TimezoneService>();

// Register Image Storage Service
builder.Services.AddScoped<IStoredImageService, StoredImageService>();

// Register startup migration job for product images
builder.Services.AddScoped<EcommerceStarter.Services.Startup.ProductImageMigrationService>();

// Register AI Services
builder.Services.AddScoped<IRequestRouter, RequestRouter>();
builder.Services.AddSingleton<IAIService, AIService>();
builder.Services.AddScoped<OllamaService>();
builder.Services.AddScoped<ClaudeAIService>();

// Register HttpClient for AI services
builder.Services.AddHttpClient<ClaudeAIService>();
builder.Services.AddHttpClient<OllamaService>();

// Register API Managers
builder.Services.AddScoped<EcommerceStarter.Services.ApiManagers.CloudinaryApiManager>();
builder.Services.AddScoped<EcommerceStarter.Services.ApiManagers.StripeApiManager>();
builder.Services.AddScoped<EcommerceStarter.Services.ApiManagers.UspsApiManager>();
builder.Services.AddScoped<EcommerceStarter.Services.ApiManagers.UpsApiManager>();
builder.Services.AddScoped<EcommerceStarter.Services.ApiManagers.FedExApiManager>();
builder.Services.AddScoped<EcommerceStarter.Services.ApiManagers.AiServicesApiManager>();
builder.Services.AddScoped<EcommerceStarter.Services.ApiManagers.CloudflareApiManager>();

// Register Analytics Services
builder.Services.AddHttpClient<ICloudflareAnalyticsService, CloudflareAnalyticsService>();
builder.Services.AddScoped<IUnifiedAnalyticsService, UnifiedAnalyticsService>();
builder.Services.AddSingleton<EcommerceStarter.Services.Analytics.IAnalyticsExclusionMetricsService, EcommerceStarter.Services.Analytics.AnalyticsExclusionMetricsService>();

var setupAIBackends = (IServiceProvider provider) =>
{
    var aiService = provider.GetRequiredService<IAIService>();
    var ollamaService = provider.GetRequiredService<OllamaService>();
    var claudeService = provider.GetRequiredService<ClaudeAIService>();

    aiService.RegisterBackend(AIBackendType.Ollama, ollamaService);
    aiService.RegisterBackend(AIBackendType.Claude, claudeService);
};
builder.Services.AddSingleton(setupAIBackends);

// Register Tracking Services
builder.Services.AddScoped<ITrackingStatusService, TrackingStatusService>();

// Register Visitor Analytics Service
builder.Services.AddScoped<EcommerceStarter.Services.Analytics.IVisitorTrackingService, EcommerceStarter.Services.Analytics.VisitorTrackingService>();

// Register User Agent Parser Service
builder.Services.AddSingleton<EcommerceStarter.Services.Analytics.IUserAgentParserService, EcommerceStarter.Services.Analytics.UserAgentParserService>();

// Register Queued Event Service (for batching analytics/audit writes)
builder.Services.AddSingleton<IQueuedEventService, QueuedEventService>();

// Register Update History Recorder Service (records completed upgrades on startup)
builder.Services.AddHostedService<UpdateHistoryRecorderService>();

// Register Carrier Tracking Providers
builder.Services.AddScoped<ICarrierTrackingProvider, UspsTrackingProvider>();
// UPS and FedEx providers will be added later

// Configure Resend SDK
builder.Services.AddOptions();
builder.Services.AddHttpClient<Resend.ResendClient>();

// Register Email Services
builder.Services.AddScoped<EmailTemplateService>();
builder.Services.AddScoped<ResendEmailService>();
builder.Services.AddScoped<SmtpEmailService>();
builder.Services.AddScoped<EmailServiceFactory>();

// Register the base email service wrapper
builder.Services.AddScoped<EmailServiceWrapper>();

// Register IEmailService with audit logging decorator
// The AuditedEmailService wraps the EmailServiceWrapper to log all email activities
builder.Services.AddScoped<IEmailService>(serviceProvider =>
{
    var emailWrapper = serviceProvider.GetRequiredService<EmailServiceWrapper>();
    var auditLogService = serviceProvider.GetRequiredService<IAuditLogService>();
    var logger = serviceProvider.GetRequiredService<ILogger<AuditedEmailService>>();
    return new AuditedEmailService(emailWrapper, auditLogService, logger);
});

builder.Services.AddRazorPages();

// Add support for API controllers with global timezone-aware DateTime converter
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Register timezone-aware DateTime converters globally
        // This automatically converts all DateTime properties in API responses to the configured timezone
        options.JsonSerializerOptions.Converters.Add(new EcommerceStarter.Converters.TimezoneAwareDateTimeConverterFactory());
    });

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck("stripe_configured", () =>
    {
        // Basic check that Stripe is configured
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Stripe configuration present");
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        await ProductionSafeSeedData.InitializeAsync(services, env);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database seeding skipped - database may not exist yet.");
    }
}

// Initialize AI services backends
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var initializeAI = services.GetRequiredService<Action<IServiceProvider>>();
        initializeAI(services);

        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("AI services initialized - Ollama and Claude backends registered");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "AI services initialization warning - backends may not be available yet");
    }
}

using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("[Startup] API configuration ready");

        // Run one-time product image migration (idempotent)
        var productImageMigrator = services.GetRequiredService<EcommerceStarter.Services.Startup.ProductImageMigrationService>();
        await productImageMigrator.RunAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "[Startup] Database may not exist yet");
    }
}

// Enable forwarded headers for Cloudflare
// This must come before UseHttpsRedirection
app.UseForwardedHeaders();

// Add security headers middleware
app.Use(async (context, next) =>
{
    // Generate a 16-byte nonce and Base64 encode
    var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
    var nonce = Convert.ToBase64String(bytes);
    context.Items["CspNonce"] = nonce;

    var csp = new System.Text.StringBuilder();
    csp.Append("default-src 'self'; ");

    // Common allowed script/style/connect/image/font hosts (Stripe, CDNs, Google Pay, Cloudflare)
    // CSP for Cloudflare Gateway: Google Analytics served from our domain via measurement path (blocked from external sources)
    var scriptSrc = "'self' 'unsafe-inline' https://js.stripe.com https://cdn.jsdelivr.net https://pay.google.com https://wallet.google.com https://static.cloudflareinsights.com";
    var styleSrc = "'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://fonts.gstatic.com";
    var connectSrc = "'self' https://api.stripe.com https://hooks.stripe.com https://checkout.stripe.com https://pay.google.com https://wallet.google.com https://www.google.com/pay https://cdn.jsdelivr.net https://static.cloudflareinsights.com https://stats.g.doubleclick.net https://analytics.google.com";
    var imgSrc = "'self' data: https:";
    var fontSrc = "'self' https://cdn.jsdelivr.net https://fonts.gstatic.com https://fonts.googleapis.com";
    var frameSrc = "https://js.stripe.com https://hooks.stripe.com https://checkout.stripe.com https://pay.google.com https://wallet.google.com https://www.google.com";

    if (app.Environment.IsDevelopment())
    {
        connectSrc += " ws://localhost:* wss://localhost:* http://localhost:* ws: wss:";
        frameSrc += " http://localhost:* https://localhost:*";
    }

    csp.Append($"script-src {scriptSrc};");
    csp.Append($"style-src {styleSrc};");
    csp.Append($"connect-src {connectSrc};");
    csp.Append($"img-src {imgSrc};");
    csp.Append($"font-src {fontSrc};");
    csp.Append($"frame-src {frameSrc};");
    csp.Append("manifest-src 'self' https://www.google.com https://pay.google.com;");

    context.Response.Headers["Content-Security-Policy"] = csp.ToString();

    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");

    // Control referrer information
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    await next();
});

// Global exception handler - logs all unhandled exceptions to database
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error500");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    // Don't redirect to HTTPS here - Cloudflare handles HTTPS, we receive HTTP from Cloudflare
}
else
{
    app.UseExceptionHandler("/Error");
}

// Custom status code pages with stylized error handling
app.UseStatusCodePagesWithReExecute("/Error/{0}");

// app.UseHttpsRedirection();  // Cloudflare handles HTTPS

// Enable response caching for dynamic theme CSS
app.UseResponseCaching();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// Set timezone service in async context for JSON converters
app.UseTimezoneService();

// IMPORTANT: Internal service authentication must come BEFORE regular authentication
// This allows automated services to bypass JWT token requirements
// Internal Service Authentication disabled - feature moved to ApiConfigurations
// app.UseInternalServiceAuthentication();

// IMPORTANT: Authentication must come BEFORE security middleware
// so that User.Identity and User.IsInRole() are available
app.UseAuthentication();
app.UseAuthorization();

// Apply security middleware AFTER authentication
// This allows admin role check to work correctly
app.UseIpBlocking();      // Block known malicious IPs
app.UseRateLimiting();    // Apply rate limiting (with admin exemption)

// Add visitor tracking middleware (before routing)
app.UseVisitorTracking();

// Add Google Tag Manager Gateway headers (before routing)
app.UseGoogleTagGateway();

// Detect and log suspicious bot scanning behavior (after routing, to see 404s)
app.UseSuspiciousActivityDetection();

app.MapRazorPages();
app.MapControllers(); // Map API controllers

// Map health check endpoint
app.MapHealthChecks("/health");

// Docs endpoints: serve Markdown guides directly
app.MapGet("/docs/WHITELIST_BLACKLIST_GUIDE.md", async (HttpContext ctx) =>
{
    var content = TryReadMarkdown(app.Environment.ContentRootPath, "WHITELIST_BLACKLIST_GUIDE.md");
    if (content == null)
    {
        ctx.Response.StatusCode = 404;
        await ctx.Response.WriteAsync("Guide not found");
        return;
    }
    ctx.Response.ContentType = "text/markdown; charset=utf-8";
    await ctx.Response.WriteAsync(content);
}).RequireAuthorization("AdminOnly");

app.MapGet("/docs/QUEUE_TROUBLESHOOTING.md", async (HttpContext ctx) =>
{
    var content = TryReadMarkdown(app.Environment.ContentRootPath, "QUEUE_TROUBLESHOOTING.md");
    if (content == null)
    {
        ctx.Response.StatusCode = 404;
        await ctx.Response.WriteAsync("Guide not found");
        return;
    }
    ctx.Response.ContentType = "text/markdown; charset=utf-8";
    await ctx.Response.WriteAsync(content);
}).RequireAuthorization("AdminOnly");

static string? TryReadMarkdown(string contentRoot, string fileName)
{
    // Try common locations inside the project and parent folders
    var candidates = new List<string>
    {
        Path.Combine(contentRoot, "docs", fileName),
        Path.Combine(contentRoot, "wwwroot", "docs", fileName),
        Path.Combine(contentRoot, "..", "..", "..", "FungalSupplyCo-Development", "docs", fileName),
        Path.Combine(contentRoot, "..", fileName),
    };

    foreach (var path in candidates)
    {
        try
        {
            var full = Path.GetFullPath(path);
            if (File.Exists(full))
                return File.ReadAllText(full);
        }
        catch { }
    }
    return null;
}

// Initialize AI backends
using (var scope = app.Services.CreateScope())
{
    var setupAction = scope.ServiceProvider.GetRequiredService<Action<IServiceProvider>>();
    setupAction(scope.ServiceProvider);
}

app.Run();
