using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using EcommerceStarter.Data;
using System.Security.Cryptography;
#pragma warning disable CA1416
using Microsoft.Win32;
#pragma warning restore CA1416

namespace EcommerceStarter;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Priority 1: Command line argument
        if (args.Length > 0 && args[0].StartsWith("Server="))
        {
            optionsBuilder.UseSqlServer(args[0]);
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        // Priority 2: Environment variable (matches app behavior)
        var fromEnv = Environment.GetEnvironmentVariable("ECOMMERCESTARTER_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            optionsBuilder.UseSqlServer(fromEnv);
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        // Priority 3: Windows Registry (matches app behavior)
        var fromRegistry = TryReadFromRegistry();
        if (!string.IsNullOrWhiteSpace(fromRegistry))
        {
            optionsBuilder.UseSqlServer(fromRegistry);
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        // Priority 4: Fallback to default connection string for localhost\SQLEXPRESS
        var connectionString = "Server=localhost\\SQLEXPRESS;Database=CapAndCollarSupplyCo;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        optionsBuilder.UseSqlServer(connectionString);
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string? TryReadFromRegistry()
    {
#if NET6_0_OR_GREATER
        try
        {
            // Try per-instance keys for all common site names
            var siteNames = new[] { "CapAndCollarSupplyCo", "FungalSupplyCo", "EcommerceStarter" };
            foreach (var siteName in siteNames)
            {
                var encrypted = ReadRegistryValue(Registry.LocalMachine, $@"Software\EcommerceStarter\{siteName}", "ConnectionStringEncrypted");
                if (!string.IsNullOrWhiteSpace(encrypted))
                {
                    return DecryptConnectionString(encrypted);
                }
            }

            // Try legacy shared keys
            var legacyPlain = ReadRegistryValue(Registry.LocalMachine, @"Software\EcommerceStarter", "ConnectionString")
                           ?? ReadRegistryValue(Registry.CurrentUser, @"Software\EcommerceStarter", "ConnectionString");
            if (!string.IsNullOrWhiteSpace(legacyPlain))
            {
                return legacyPlain;
            }

            var legacyEncrypted = ReadRegistryValue(Registry.LocalMachine, @"Software\EcommerceStarter", "ConnectionStringEncrypted")
                               ?? ReadRegistryValue(Registry.CurrentUser, @"Software\EcommerceStarter", "ConnectionStringEncrypted");
            if (!string.IsNullOrWhiteSpace(legacyEncrypted))
            {
                return DecryptConnectionString(legacyEncrypted);
            }
        }
        catch
        {
            // If registry access fails, fall through to default connection string
        }
#endif
        return null;
    }

    private static string? ReadRegistryValue(RegistryKey root, string subKey, string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(subKey, false);
            return key?.GetValue(valueName)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string? DecryptConnectionString(string encryptedBase64)
    {
        try
        {
            var bytes = Convert.FromBase64String(encryptedBase64);
            var unprotected = ProtectedData.Unprotect(bytes, optionalEntropy: null, DataProtectionScope.LocalMachine);
            return System.Text.Encoding.UTF8.GetString(unprotected);
        }
        catch
        {
            return null;
        }
    }
}
