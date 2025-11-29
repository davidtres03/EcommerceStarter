using EcommerceStarter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Data
{
    /// <summary>
    /// Safe data seeding strategy that prevents test data from overwriting production data.
    /// This only seeds roles and a default admin user for development environments.
    /// Production environments should use the installer or manual setup.
    /// </summary>
    public static class ProductionSafeSeedData
    {
        /// <summary>
        /// Safe initialization that checks environment before seeding
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider, IWebHostEnvironment env)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Ensure database is created
                await context.Database.EnsureCreatedAsync();

                // Check if database has already been initialized by looking for any SiteSettings
                var existingSettings = await context.SiteSettings.FirstOrDefaultAsync();
                
                if (existingSettings != null)
                {
                    // Database already initialized (either by installer or previous seeding)
                    logger.LogInformation("Database already initialized - skipping seeding");
                    return;
                }

                // Only seed in Development environment
                if (env.IsDevelopment())
                {
                    logger.LogInformation("DEVELOPMENT ENVIRONMENT - Seeding roles and default admin");
                    
                    // Seed only roles and admin user (NO PRODUCTS)
                    await SeedRolesAndAdminAsync(serviceProvider, logger);
                    
                    // Create default site settings for development
                    await CreateDefaultSiteSettingsAsync(context, logger, serviceProvider, isDevelopment: true);
                }
                else if (env.IsProduction())
                {
                    logger.LogWarning("PRODUCTION ENVIRONMENT - Installer should handle setup");
                    logger.LogInformation("If you haven't run the installer, the site may not work correctly.");
                    logger.LogInformation("Site settings will be created by the installer or can be configured in Admin panel.");
                    
                    // Don't seed anything in production - let installer handle it
                    // Just create minimal site settings so the app doesn't crash
                    await CreateDefaultSiteSettingsAsync(context, logger, serviceProvider, isDevelopment: false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during data seeding: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Seeds roles and default admin user only (NO products, NO test data)
        /// </summary>
        private static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Seed Roles
            string[] roleNames = { "Admin", "Customer" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    logger.LogInformation($"Created role: {roleName}");
                }
            }

            // Seed Default Admin User (DEVELOPMENT ONLY)
            var adminEmail = "admin@example.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                var newAdminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newAdminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdminUser, "Admin");
                    logger.LogInformation($"Created default admin user: {adminEmail}");
                    logger.LogWarning("CHANGE DEFAULT PASSWORD IMMEDIATELY!");
                }
            }

            // NO PRODUCTS ARE SEEDED - Users add their own through admin panel
            logger.LogInformation("Product catalog is empty - add products through Admin Panel");
        }

        /// <summary>
        /// Create default site settings
        /// </summary>
        private static async Task CreateDefaultSiteSettingsAsync(ApplicationDbContext context, ILogger logger, IServiceProvider serviceProvider, bool isDevelopment)
        {
            // Generate a unique internal service key for API testing automation
            var serviceKey = Guid.NewGuid().ToString();
            
            // Get encryption service to encrypt the key before storing
            var encryptionService = serviceProvider.GetService<Services.IEncryptionService>();
            var encryptedServiceKey = encryptionService?.Encrypt(serviceKey);
            
            var settings = new SiteSettings
            {
                // Branding - will be customized by user
                SiteName = "My Store",
                SiteTagline = "Modern E-Commerce Platform Built with ASP.NET Core",
                SiteIcon = "??",
                
                // Colors (Professional blue theme)
                PrimaryColor = "#0d6efd",
                PrimaryDark = "#0a58ca",
                PrimaryLight = "#6ea8fe",
                SecondaryColor = "#6c757d",
                AccentColor = "#0dcaf0",
                
                // Typography
                PrimaryFont = "Segoe UI, Tahoma, Geneva, Verdana, sans-serif",
                HeadingFont = "Segoe UI, Tahoma, Geneva, Verdana, sans-serif",
                
                // Business Info
                CompanyName = "My Store",
                ContactEmail = "contact@example.com",
                SupportEmail = "support@example.com",
                
                // SEO
                MetaDescription = "Modern e-commerce platform built with ASP.NET Core. Customize and launch your online store today.",
                MetaKeywords = "e-commerce, online store, asp.net core, shopping",
                
                // Features
                EnableGuestCheckout = true,
                EnableProductReviews = false,
                EnableWishlist = false,
                ShowStockCount = true,
                AllowBackorders = false,
                
                // Email
                EmailFromName = "My Store",
                EmailFromAddress = "noreply@example.com",
                
                // Metadata
                LastModified = DateTime.UtcNow,
                LastModifiedBy = isDevelopment ? "System (Development)" : "System (Production)"
            };

            context.SiteSettings.Add(settings);
            await context.SaveChangesAsync();
            
            logger.LogInformation($"Created default site settings for {(isDevelopment ? "development" : "production")} environment");
            
            // Log the unencrypted service key ONLY in development
            if (isDevelopment)
            {
                logger.LogWarning("=".PadRight(80, '='));
                logger.LogWarning("INTERNAL SERVICE KEY (for automated testing):");
                logger.LogWarning($"  {serviceKey}");
                logger.LogWarning("Save this key to use with automated API testing (X-Internal-Service-Key header)");
                logger.LogWarning("This key is stored encrypted in the SiteSettings table");
                logger.LogWarning("=".PadRight(80, '='));
            }
            else
            {
                logger.LogInformation("Internal service key generated and stored encrypted");
                logger.LogInformation("View key through Admin > Settings > API Configuration page");
            }
        }
    }
}
