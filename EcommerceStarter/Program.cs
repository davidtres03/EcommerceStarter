using EcommerceStarter.Configuration;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using EcommerceStarter.Services.Tracking;
using EcommerceStarter.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add logging for IIS - CRITICAL for production debugging
builder.Logging.ClearProviders();

// Add console logging (works in IIS with stdout redirection)
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
        Filter = (x, level) => level >= LogLevel.Information
    });
#pragma warning restore CA1416
}

// Add debug logging in development
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

// Configure forwarded headers for Cloudflare proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Clear the default networks and proxies to accept headers from any source
    // This is safe when behind Cloudflare
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Add Response Caching for dynamic theme CSS
builder.Services.AddResponseCaching();

// Add HttpClient support for email services
builder.Services.AddHttpClient();

// Configure Stripe Settings
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Register Cart Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, CartService>();

// Register Encryption Service (for secure key storage)
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

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

// Register API Key Service (for carrier API credentials)
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

// Register Tracking Services
builder.Services.AddScoped<ITrackingStatusService, TrackingStatusService>();

// Register Visitor Analytics Service
builder.Services.AddScoped<EcommerceStarter.Services.Analytics.IVisitorTrackingService, EcommerceStarter.Services.Analytics.VisitorTrackingService>();

// Register Carrier Tracking Providers
builder.Services.AddScoped<ICarrierTrackingProvider, UspsTrackingProvider>();
// UPS and FedEx providers will be added later

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
builder.Services.AddControllers(); // Add support for API controllers

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

// Seed roles and admin user - PRODUCTION SAFE
// This prevents test data from overwriting production data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // Use production-safe seeding that checks environment
        await ProductionSafeSeedData.InitializeAsync(services, env);
    }
    catch (Exception ex)
    {
        // Database seeding failed - this is OK for first-time setup
        // The installer will create the database
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "??  Database seeding skipped - database may not exist yet. This is normal before installation.");
        logger.LogInformation("??  If you haven't run the installer yet, please use the EcommerceStarter installer to set up your database.");
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

    // Build a strict Content-Security-Policy
    var csp = new System.Text.StringBuilder();
    csp.Append("default-src 'self'; ");

    // Common allowed script/style/connect/image/font hosts (Stripe, CDNs, Google Pay, Google Analytics/Tag Manager, Cloudflare)
    var scriptSrc = "'self' 'unsafe-inline' https://js.stripe.com https://cdn.jsdelivr.net https://pay.google.com https://wallet.google.com https://google.com https://gstatic.com https://www.googletagmanager.com https://www.google-analytics.com https://ssl.google-analytics.com https://tagmanager.google.com https://static.cloudflareinsights.com https://cloudflareinsights.com";
    var styleSrc = "'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://fonts.gstatic.com https://tagmanager.google.com https://www.googletagmanager.com";
    var connectSrc = "'self' https://api.stripe.com https://hooks.stripe.com https://checkout.stripe.com https://pay.google.com https://wallet.google.com https://google.com https://www.google.com https://google.com/pay https://www.google.com/pay https://gstatic.com https://cdn.jsdelivr.net https://www.google-analytics.com https://analytics.google.com https://stats.g.doubleclick.net https://www.googletagmanager.com https://region1.google-analytics.com https://region1.analytics.google.com https://cloudflareinsights.com https://static.cloudflareinsights.com";
    var imgSrc = "'self' data: https: https://gstatic.com https://www.google-analytics.com https://ssl.google-analytics.com https://www.googletagmanager.com https://www.google.com";
    var fontSrc = "'self' https://cdn.jsdelivr.net https://fonts.gstatic.com https://fonts.googleapis.com";
    var frameSrc = "https://js.stripe.com https://hooks.stripe.com https://checkout.stripe.com https://pay.google.com https://wallet.google.com https://google.com";

    // In Development allow localhost and BrowserLink/websocket endpoints for tooling and testing
    if (app.Environment.IsDevelopment())
    {
        // Allow WebSocket connections for BrowserLink and ASP.NET Core hot reload
        // This includes ws:// and wss:// protocols on various localhost ports
        connectSrc += " ws://localhost:* wss://localhost:* http://localhost:* ws: wss:";
        // It's safe to allow frames from localhost in development for testing
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

    // Additional secure headers
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    
    // Control referrer information
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // Restrict browser features
    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    // Don't redirect to HTTPS here - Cloudflare handles HTTPS, we receive HTTP from Cloudflare
}

// app.UseHttpsRedirection();  // DISABLED: Cloudflare handles HTTPS, removing this prevents redirect loops

// Enable response caching for dynamic theme CSS
app.UseResponseCaching();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

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

app.MapRazorPages();
app.MapControllers(); // Map API controllers

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
