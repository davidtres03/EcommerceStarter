using EcommerceStarter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Data
{
    /// <summary>
    /// Seeds initial database with roles and a default admin user.
    /// This is for DEVELOPMENT ONLY - production should use the Setup Wizard.
    /// </summary>
    public static class SeedData
    {
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

            // Seed Default Admin User (DEVELOPMENT ONLY)
            // ?? SECURITY WARNING: This is a default development password!
            // ?? For production deployments:
            //    1. Use the Setup Wizard to create your own secure admin account
            //    2. OR delete this user and create your own via the registration page
            //    3. Never use this default password in production!
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

                // Default password for LOCAL DEVELOPMENT ONLY
                // CHANGE IMMEDIATELY in production or use Setup Wizard!
                var result = await userManager.CreateAsync(newAdminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdminUser, "Admin");
                }
            }

            // NO PRODUCTS ARE SEEDED
            // Users should add their own products through the admin panel
            // or use the Setup Wizard to configure their store

            await context.SaveChangesAsync();
        }
    }
}
