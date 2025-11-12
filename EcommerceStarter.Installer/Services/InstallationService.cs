using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using EcommerceStarter.Installer.Models;
using Microsoft.AspNetCore.Identity;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Service for performing the actual installation
/// </summary>
public class InstallationService
{
    public event EventHandler<InstallationProgress>? ProgressUpdate;
    public event EventHandler<string>? StatusUpdate;
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Perform the full installation
    /// </summary>
    public async Task<InstallationResult> InstallAsync(InstallationConfig config, bool isDebugMode)
    {
        // 🎬 DEMO MODE PROTECTION - Refuse to install!
        if (App.IsDemoMode)
        {
            // Simulate installation for demo purposes only
            await SimulateDemoInstallAsync(config);
            
            return new InstallationResult
            {
                Success = true,
                Message = "Demo installation completed successfully (no real changes were made)"
            };
        }
        
        var result = new InstallationResult { Success = true };

        try
        {
            // Step 1: Verify Prerequisites (0-20%)
            ReportProgress(1, 10, "Checking prerequisites...");
            await Task.Delay(500);
            ReportProgress(1, 20, "Prerequisites verified!", true);

            // Step 2: Create Database (20-40%)
            ReportProgress(2, 25, "Creating database...");
            if (!isDebugMode)
            {
                var dbResult = await CreateDatabaseAsync(config.DatabaseServer, config.DatabaseName);
                if (!dbResult.Success)
                {
                    throw new Exception($"Database creation failed: {dbResult.ErrorMessage}");
                }
                
                // Grant IIS Application Pool permissions to database
                ReportProgress(2, 35, "Configuring database permissions...");
                var permResult = await GrantDatabasePermissionsAsync(config.DatabaseServer, config.DatabaseName, config.SiteName);
                if (!permResult.Success)
                {
                    // Non-fatal warning - user can fix manually
                    StatusUpdate?.Invoke(this, $"Warning: Could not auto-configure database permissions: {permResult.ErrorMessage}");
                    StatusUpdate?.Invoke(this, "You may need to grant permissions manually after installation.");
                }
            }
            await Task.Delay(1000);
            ReportProgress(2, 40, "Database created successfully!", true);

            // Step 3: Deploy Application Files (40-60%)
            ReportProgress(3, 45, "Preparing application files...");
            if (!isDebugMode)
            {
                var deployResult = await DeployApplicationAsync(config.InstallationPath);
                if (!deployResult.Success)
                {
                    throw new Exception($"Application deployment failed: {deployResult.ErrorMessage}");
                }
            }
            await Task.Delay(1000);
            ReportProgress(3, 55, "Deploying files...");
            await Task.Delay(1000);
            ReportProgress(3, 60, "Application deployed!", true);

            // Step 4: Configure IIS (60-80%)
            ReportProgress(4, 65, "Creating IIS application pool...");
            if (!isDebugMode)
            {
                var iisResult = await ConfigureIISAsync(config);
                if (!iisResult.Success)
                {
                    throw new Exception($"IIS configuration failed: {iisResult.ErrorMessage}");
                }
            }
            await Task.Delay(1000);
            ReportProgress(4, 72, "Configuring website...");
            await Task.Delay(1000);
            ReportProgress(4, 80, "IIS configured successfully!", true);

            // Step 5: Apply Configuration (80-90%)
            ReportProgress(5, 85, "Applying your settings...");
            if (!isDebugMode)
            {
                var configResult = await ApplyConfigurationAsync(config);
                if (!configResult.Success)
                {
                    throw new Exception($"Configuration failed: {configResult.ErrorMessage}");
                }
                
                // Create admin user
                ReportProgress(5, 88, "Creating admin user...");
                
                // Only create admin if credentials were provided
                if (!string.IsNullOrWhiteSpace(config.AdminEmail) && !string.IsNullOrWhiteSpace(config.AdminPassword))
                {
                    var adminResult = await CreateAdminUserAsync(config);
                    if (!adminResult.Success)
                    {
                        // Non-fatal warning - user can create manually
                        StatusUpdate?.Invoke(this, $"Warning: Could not create admin user: {adminResult.ErrorMessage}");
                        StatusUpdate?.Invoke(this, "You can create an admin user manually after installation.");
                    }
                    else
                    {
                        StatusUpdate?.Invoke(this, adminResult.Message);
                    }
                }
                else
                {
                    StatusUpdate?.Invoke(this, "Skipping admin user creation (no credentials provided - existing database)");
                }
            }
            await Task.Delay(1000);
            ReportProgress(5, 90, "Configuration applied!", true);

            // Step 6: Finalization (90-100%)
            ReportProgress(6, 95, "Finalizing installation...");
            
            // Register in Windows Programs & Features
            if (!isDebugMode)
            {
                var registryResult = await RegisterInWindowsAsync(config);
                if (!registryResult.Success)
                {
                    // Non-fatal - log warning but continue
                    StatusUpdate?.Invoke(this, $"Warning: Could not register in Programs & Features: {registryResult.ErrorMessage}");
                }
            }
            
            await Task.Delay(500);
            ReportProgress(6, 100, "Installation complete!", true);

            result.Success = true;
            result.Message = "Installation completed successfully!";
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            ErrorOccurred?.Invoke(this, ex.Message);
            return result;
        }
    }

    private async Task<OperationResult> CreateDatabaseAsync(string server, string databaseName)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Ensuring SQL Server is ready...");

            // CRITICAL: Start SQL Browser service if using named instance
            if (server.Contains("\\", StringComparison.OrdinalIgnoreCase))
            {
                StatusUpdate?.Invoke(this, "Starting SQL Server Browser (required for named instances)...");
                
                var browserScript = @"
                    try {
                        $browser = Get-Service -Name 'SQLBrowser' -ErrorAction SilentlyContinue
                        if ($browser) {
                            Set-Service -Name 'SQLBrowser' -StartupType Automatic -ErrorAction SilentlyContinue
                            Start-Service -Name 'SQLBrowser' -ErrorAction SilentlyContinue
                            Write-Output 'SQL Browser started'
                        } else {
                            Write-Output 'SQL Browser service not found'
                        }
                    } catch {
                        Write-Output 'Warning: Could not start SQL Browser'
                    }
                ";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{browserScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var browserProcess = Process.Start(psi);
                if (browserProcess != null)
                {
                    await browserProcess.WaitForExitAsync();
                }
            }

            StatusUpdate?.Invoke(this, "Running database migrations...");

            // Find the bundled migration executable
            var installerDir = AppDomain.CurrentDomain.BaseDirectory;
            var migrationBundlePath = Path.Combine(installerDir, "migrations", "efbundle.exe");

            if (!File.Exists(migrationBundlePath))
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = $"Migration bundle not found at: {migrationBundlePath}. " +
                                  "Please ensure the installer package was built correctly using Build-PortableInstaller.ps1"
                };
            }

            // Build connection string for migration bundle
            var escapedServer = server.Replace("\"", "\\\"");
            var connectionString = $"Server={escapedServer};Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            // Run the bundled migration executable
            var psi2 = new ProcessStartInfo
            {
                FileName = migrationBundlePath,
                Arguments = $"--connection \"{connectionString}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(migrationBundlePath)
            };

            using var process = Process.Start(psi2);
            if (process == null)
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start migration process"
                };
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = $"Migration failed: {error}\n{output}"
                };
            }

            return new OperationResult
            {
                Success = true,
                Message = "Database created and migrations applied successfully"
            };
        }
        catch (Exception ex)
        {
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<OperationResult> GrantDatabasePermissionsAsync(string server, string databaseName, string appPoolName)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Granting IIS Application Pool database access...");

            var appPoolUser = $"IIS APPPOOL\\{appPoolName}";

            // SQL script to create login and grant permissions
            var sqlScript = $@"
USE [master];
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = N'{appPoolUser}')
BEGIN
    CREATE LOGIN [{appPoolUser}] FROM WINDOWS WITH DEFAULT_DATABASE=[master];
END

USE [{databaseName}];
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'{appPoolUser}')
BEGIN
    CREATE USER [{appPoolUser}] FOR LOGIN [{appPoolUser}];
END

ALTER ROLE [db_owner] ADD MEMBER [{appPoolUser}];
";

            // Save SQL script to temp file
            var tempSqlFile = Path.GetTempFileName() + ".sql";
            await File.WriteAllTextAsync(tempSqlFile, sqlScript);

            try
            {
                // Execute SQL script using sqlcmd
                var psi = new ProcessStartInfo
                {
                    FileName = "sqlcmd",
                    Arguments = $"-S \"{server}\" -E -i \"{tempSqlFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return new OperationResult { Success = false, ErrorMessage = "Failed to start sqlcmd" };
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    return new OperationResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"SQL permission script failed: {error}" 
                    };
                }

                return new OperationResult 
                { 
                    Success = true, 
                    Message = $"Granted database access to {appPoolUser}" 
                };
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempSqlFile))
                {
                    File.Delete(tempSqlFile);
                }
            }
        }
        catch (Exception ex)
        {
            return new OperationResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    private async Task<OperationResult> DeployApplicationAsync(string installPath)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Deploying application files...");

            // Find the bundled application files
            var installerDir = AppDomain.CurrentDomain.BaseDirectory;
            var bundledAppPath = Path.Combine(installerDir, "app");

            if (!Directory.Exists(bundledAppPath))
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = $"Bundled application files not found at: {bundledAppPath}. " +
                                  "Please ensure the installer package was built correctly using Build-PortableInstaller.ps1"
                };
            }

            // Verify critical files exist in bundle
            var criticalFiles = new[]
            {
                "EcommerceStarter.dll",
                "EcommerceStarter.deps.json",
                "EcommerceStarter.runtimeconfig.json",
                "wwwroot"
            };

            foreach (var file in criticalFiles)
            {
                var filePath = Path.Combine(bundledAppPath, file);
                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                {
                    return new OperationResult
                    {
                        Success = false,
                        ErrorMessage = $"Critical file/folder missing in bundle: {file}"
                    };
                }
            }

            // Create installation directory
            StatusUpdate?.Invoke(this, "Creating installation directory...");
            Directory.CreateDirectory(installPath);

            // Copy all files from bundled app to installation directory
            StatusUpdate?.Invoke(this, "Copying application files...");
            await Task.Run(() =>
            {
                CopyDirectory(bundledAppPath, installPath, true);
            });

            // Verify files were copied successfully
            foreach (var file in criticalFiles)
            {
                var filePath = Path.Combine(installPath, file);
                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                {
                    return new OperationResult
                    {
                        Success = false,
                        ErrorMessage = $"Critical file/folder missing after deployment: {file}"
                    };
                }
            }

            StatusUpdate?.Invoke(this, "Application deployed successfully");
            return new OperationResult { Success = true, Message = "Application deployed successfully" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = $"Deployment error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Recursively copy directory contents
    /// </summary>
    private void CopyDirectory(string sourceDir, string destDir, bool overwrite = false)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        }

        // Create destination directory
        Directory.CreateDirectory(destDir);

        // Copy files
        foreach (var file in dir.GetFiles())
        {
            var targetPath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetPath, overwrite);
        }

        // Copy subdirectories
        foreach (var subDir in dir.GetDirectories())
        {
            var newDestDir = Path.Combine(destDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestDir, overwrite);
        }
    }

    private async Task<OperationResult> ConfigureIISAsync(InstallationConfig config)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Configuring IIS...");

            var appName = config.SiteName; // Use site name (e.g., "MyStore")

            // PowerShell script to configure IIS - create APPLICATION under Default Web Site
            var script = $@"
                Import-Module WebAdministration;
                
                # Create App Pool
                $appPoolName = '{appName}';
                if (Test-Path IIS:\AppPools\$appPoolName) {{
                    Remove-WebAppPool -Name $appPoolName;
                }}
                New-WebAppPool -Name $appPoolName -Force;
                Set-ItemProperty IIS:\AppPools\$appPoolName -Name managedRuntimeVersion -Value '';
                Set-ItemProperty IIS:\AppPools\$appPoolName -Name enable32BitAppOnWin64 -Value $false;
                
                # Create Application under Default Web Site
                $appPath = '/{appName}';
                if (Test-Path ""IIS:\Sites\Default Web Site$appPath"") {{
                    Remove-WebApplication -Name '{appName}' -Site 'Default Web Site';
                }}
                New-WebApplication -Name '{appName}' -Site 'Default Web Site' -PhysicalPath '{config.InstallationPath}' -ApplicationPool $appPoolName -Force;
                
                # Start the app pool
                Start-WebAppPool -Name $appPoolName;
                
                Write-Output 'IIS configured successfully - Application created at http://localhost/{appName}';
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return new OperationResult { Success = false, ErrorMessage = "Failed to start PowerShell" };
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return new OperationResult { Success = false, ErrorMessage = $"IIS configuration failed: {error}\nOutput: {output}" };
            }

            return new OperationResult { Success = true, Message = $"IIS configured successfully. URL: http://localhost/{appName}" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<OperationResult> ApplyConfigurationAsync(InstallationConfig config)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Writing configuration files...");

            var appsettingsPath = Path.Combine(config.InstallationPath, "appsettings.json");

            // Create appsettings.json with the user's configuration
            // IMPORTANT: Escape backslashes in the connection string for JSON
            var escapedServer = config.DatabaseServer.Replace(@"\", @"\\");
            var connectionString = $"Server={escapedServer};Database={config.DatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            var appsettings = $@"{{
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""{connectionString}""
  }},
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning"",
      ""Microsoft.EntityFrameworkCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*""
}}";

            await File.WriteAllTextAsync(appsettingsPath, appsettings);

            // Create appsettings.Production.json with production-specific settings
            var appsettingsProductionPath = Path.Combine(config.InstallationPath, "appsettings.Production.json");
            var appsettingsProduction = $@"{{
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning"",
      ""Microsoft.EntityFrameworkCore"": ""Warning"",
      ""Microsoft.AspNetCore.Authentication"": ""Information"",
      ""Microsoft.AspNetCore.Authorization"": ""Information""
    }}
  }},
  ""DetailedErrors"": false,
  ""IncludeExceptionDetails"": false
}}";

            await File.WriteAllTextAsync(appsettingsProductionPath, appsettingsProduction);

            // Create web.config with comprehensive production settings
            var webConfigPath = Path.Combine(config.InstallationPath, "web.config");
            var httpsVariable = "{HTTPS}";  // Escape IIS variable reference
            var webConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <location path=""."" inheritInChildApplications=""false"">
    <system.webServer>
      <!-- Compression Settings -->
      <urlCompression doStaticCompression=""true"" doDynamicCompression=""true"" />
      <httpCompression>
        <dynamicTypes>
          <add mimeType=""application/json"" enabled=""true"" />
          <add mimeType=""application/json; charset=utf-8"" enabled=""true"" />
          <add mimeType=""application/javascript"" enabled=""true"" />
          <add mimeType=""text/plain"" enabled=""true"" />
          <add mimeType=""text/html"" enabled=""true"" />
          <add mimeType=""text/css"" enabled=""true"" />
        </dynamicTypes>
      </httpCompression>

      <!-- Module Handlers -->
      <handlers>
        <add name=""aspNetCore"" path=""*"" verb=""*"" modules=""AspNetCoreModuleV2"" resourceType=""Unspecified"" />
      </handlers>

      <!-- ASP.NET Core Application Configuration -->
      <aspNetCore processPath=""dotnet"" arguments="".\EcommerceStarter.dll"" stdoutLogEnabled=""false"" stdoutLogFile="".\logs\stdout"" hostingModel=""inprocess"">
        <environmentVariables>
          <environmentVariable name=""ASPNETCORE_ENVIRONMENT"" value=""Production"" />
          <environmentVariable name=""ASPNETCORE_HTTPS_PORT"" value="""" />
          <environmentVariable name=""ASPNETCORE_DETAILEDEERRORS"" value=""false"" />
        </environmentVariables>
      </aspNetCore>

      <!-- Request Filtering -->
      <security>
        <requestFiltering>
          <!-- Limit max upload size to 100 MB -->
          <requestLimits maxAllowedContentLength=""104857600"" maxQueryString=""4096"" />
        </requestFiltering>
      </security>

      <!-- Response Headers -->
      <httpProtocol>
        <customHeaders>
          <add name=""X-Frame-Options"" value=""DENY"" />
          <add name=""X-Content-Type-Options"" value=""nosniff"" />
          <add name=""X-XSS-Protection"" value=""1; mode=block"" />
          <add name=""Referrer-Policy"" value=""strict-origin-when-cross-origin"" />
          <add name=""Permissions-Policy"" value=""geolocation=(), microphone=(), camera=()"" />
        </customHeaders>
      </httpProtocol>

      <!-- Caching Configuration -->
      <caching>
        <profiles>
          <add extension="".css"" policy=""CacheUntilChange"" kernelCachePolicy=""DontCache"" duration=""3600"" />
          <add extension="".js"" policy=""CacheUntilChange"" kernelCachePolicy=""DontCache"" duration=""3600"" />
          <add extension="".jpg"" policy=""CacheUntilChange"" kernelCachePolicy=""CacheUntilChange"" duration=""86400"" />
          <add extension="".png"" policy=""CacheUntilChange"" kernelCachePolicy=""CacheUntilChange"" duration=""86400"" />
          <add extension="".gif"" policy=""CacheUntilChange"" kernelCachePolicy=""CacheUntilChange"" duration=""86400"" />
          <add extension="".woff"" policy=""CacheUntilChange"" kernelCachePolicy=""CacheUntilChange"" duration=""604800"" />
          <add extension="".woff2"" policy=""CacheUntilChange"" kernelCachePolicy=""CacheUntilChange"" duration=""604800"" />
        </profiles>
      </caching>

      <!-- Static Content Compression -->
      <staticContent>
        <clientCache cacheControlMode=""UseMaxAge"" cacheControlMaxAge=""31536000"" />
      </staticContent>

      <!-- Application Pool Recycling -->
      <applicationPool recycleOnConfigChange=""true"" />

      <!-- HTTP Strict Transport Security (HSTS) - Applied if HTTPS is used upstream -->
      <rewrite>
        <outboundRules>
          <rule name=""Add Strict-Transport-Security when HTTPS"" patternSyntax=""Wildcard"">
            <match serverVariable=""RESPONSE_HEADER_LOCATION"" pattern=""*"" negate=""false"" />
            <conditions>
              <add input=""{httpsVariable}"" pattern=""on"" ignoreCase=""true"" />
            </conditions>
            <action type=""Rewrite"" value=""max-age=31536000; includeSubDomains"" />
          </rule>
        </outboundRules>
      </rewrite>
    </system.webServer>
  </location>
</configuration>";

            await File.WriteAllTextAsync(webConfigPath, webConfig);

            // Cleanup debug files that shouldn't be in production
            var debugFiles = new[]
            {
                Path.Combine(config.InstallationPath, "appsettings.Development.json"),
                Path.Combine(config.InstallationPath, "appsettings.*.Development.json")
            };

            foreach (var pattern in debugFiles)
            {
                try
                {
                    // For simple filename, just delete if exists
                    if (File.Exists(pattern))
                    {
                        File.Delete(pattern);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            return new OperationResult { Success = true, Message = "Production configuration applied successfully" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<OperationResult> RegisterInWindowsAsync(InstallationConfig config)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Registering in Windows...");

            // Get installer path
            var installerPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var installerDir = Path.GetDirectoryName(installerPath) ?? "";
            var uninstallerPath = Path.Combine(installerDir, "EcommerceStarter.Installer.exe");

            // PowerShell script to register in Windows Registry
            var script = $@"
                $regPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_{config.SiteName}';
                
                # Create registry key
                New-Item -Path $regPath -Force | Out-Null;
                
                # Set values
                Set-ItemProperty -Path $regPath -Name 'DisplayName' -Value 'EcommerceStarter - {config.CompanyName}';
                Set-ItemProperty -Path $regPath -Name 'DisplayVersion' -Value '1.0.0';
                Set-ItemProperty -Path $regPath -Name 'Publisher' -Value 'EcommerceStarter';
                Set-ItemProperty -Path $regPath -Name 'InstallLocation' -Value '{config.InstallationPath}';
                Set-ItemProperty -Path $regPath -Name 'UninstallString' -Value '\""{uninstallerPath}\"" --uninstall --sitename=\""{config.SiteName}\""';
                Set-ItemProperty -Path $regPath -Name 'DisplayIcon' -Value '{uninstallerPath},0';
                Set-ItemProperty -Path $regPath -Name 'NoModify' -Value 1 -Type DWord;
                Set-ItemProperty -Path $regPath -Name 'NoRepair' -Value 1 -Type DWord;
                Set-ItemProperty -Path $regPath -Name 'InstallDate' -Value (Get-Date -Format 'yyyyMMdd');
                
                Write-Output 'Registered in Windows successfully';
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return new OperationResult { Success = false, ErrorMessage = "Failed to start PowerShell" };
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                return new OperationResult { Success = false, ErrorMessage = $"Registry registration failed: {error}" };
            }

            return new OperationResult { Success = true, Message = "Registered in Windows successfully" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<OperationResult> CreateAdminUserAsync(InstallationConfig config)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Checking for existing admin users...");

            // First, check if admin users already exist
            var checkAdminSql = @"
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- Check if there are any users with Admin role
SELECT COUNT(*) 
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin'
";

            var tempCheckFile = Path.GetTempFileName() + ".sql";
            await File.WriteAllTextAsync(tempCheckFile, checkAdminSql);

            try
            {
                // Execute check
                var checkPsi = new ProcessStartInfo
                {
                    FileName = "sqlcmd",
                    Arguments = $"-S \"{config.DatabaseServer}\" -d \"{config.DatabaseName}\" -E -i \"{tempCheckFile}\" -h -1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var checkProcess = Process.Start(checkPsi);
                if (checkProcess == null)
                {
                    return new OperationResult { Success = false, ErrorMessage = "Failed to check for admin users" };
                }

                var checkOutput = await checkProcess.StandardOutput.ReadToEndAsync();
                await checkProcess.WaitForExitAsync();

                File.Delete(tempCheckFile);

                // Parse count
                var adminCount = 0;
                if (int.TryParse(checkOutput.Trim(), out var count))
                {
                    adminCount = count;
                }

                if (adminCount > 0)
                {
                    StatusUpdate?.Invoke(this, $"Found {adminCount} existing admin user(s) - skipping admin creation");
                    return new OperationResult
                    {
                        Success = true,
                        Message = $"Admin users already exist ({adminCount} found). Existing admins preserved."
                    };
                }

                // No admins found - create one
                StatusUpdate?.Invoke(this, "No admin users found - creating new admin account...");
            }
            catch
            {
                // If check fails, proceed with creation (safer to try creating than skip)
                StatusUpdate?.Invoke(this, "Could not verify existing admins - attempting to create admin user...");
            }

            // Use ASP.NET Core Identity's PasswordHasher to hash the password  
            var passwordHasher = new PasswordHasher<string>();
            var hashedPassword = passwordHasher.HashPassword(config.AdminEmail, config.AdminPassword);

            // Escape strings for SQL
            var escapedEmail = config.AdminEmail.Replace("'", "''");
            var escapedHashedPassword = hashedPassword.Replace("'", "''");

            // SQL script to create admin user with hashed password
            var sqlScript = $@"
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- Ensure roles exist
IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Admin')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID())
END

IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Customer')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Customer', 'CUSTOMER', NEWID())
END

-- Check if this specific admin user exists
IF NOT EXISTS (SELECT * FROM AspNetUsers WHERE Email = '{escapedEmail}')
BEGIN
    DECLARE @UserId NVARCHAR(450) = CAST(NEWID() AS NVARCHAR(450))
    DECLARE @AdminRoleId NVARCHAR(450)
    
    SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin'
    
    -- Insert admin user with hashed password
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
        CreatedAt
    )
    VALUES (
        @UserId, 
        '{escapedEmail}', 
        UPPER('{escapedEmail}'), 
        '{escapedEmail}', 
        UPPER('{escapedEmail}'),
        1, 
        '{escapedHashedPassword}', 
        NEWID(), 
        NEWID(),
        0, 0, 1, 0,
        GETUTCDATE()
    )
    
    -- Assign Admin role
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@UserId, @AdminRoleId)
    
    PRINT 'Admin user created successfully'
END
ELSE
BEGIN
    PRINT 'Admin user already exists with this email'
END
";

            // Save SQL script to temp file
            var tempSqlFile = Path.GetTempFileName() + ".sql";
            await File.WriteAllTextAsync(tempSqlFile, sqlScript);
            
            // ALSO save to a fixed location for debugging
            var debugSqlFile = Path.Combine(Path.GetTempPath(), "AdminUserCreation_DEBUG.sql");
            File.Copy(tempSqlFile, debugSqlFile, true);

            try
            {
                // Execute SQL script using sqlcmd
                var psi = new ProcessStartInfo
                {
                    FileName = "sqlcmd",
                    Arguments = $"-S \"{config.DatabaseServer}\" -d \"{config.DatabaseName}\" -E -i \"{tempSqlFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return new OperationResult { Success = false, ErrorMessage = "Failed to start sqlcmd" };
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    return new OperationResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Admin user creation failed: {error}" 
                    };
                }
                
                // Even on success, save output for debugging
                var outputFile = Path.Combine(Path.GetTempPath(), "AdminUserCreation_OUTPUT.txt");
                await File.WriteAllTextAsync(outputFile, $"Exit Code: {process.ExitCode}\n\nOutput:\n{output}\n\nError:\n{error}");

                return new OperationResult 
                { 
                    Success = true, 
                    Message = $"Admin user created successfully: {config.AdminEmail}" 
                };
            }
            finally
            {
                // Clean up temp file only if successful
                if (File.Exists(tempSqlFile))
                {
                    try { File.Delete(tempSqlFile); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            return new OperationResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    private void ReportProgress(int step, int percentage, string message, bool completed = false)
    {
        ProgressUpdate?.Invoke(this, new InstallationProgress
        {
            CurrentStep = step,
            Percentage = percentage,
            Message = message,
            StepCompleted = completed
        });
    }
    
    /// <summary>
    /// Simulate installation for demo mode (no real changes)
    /// </summary>
    private async Task SimulateDemoInstallAsync(InstallationConfig config)
    {
        var steps = new[]
        {
            $"Creating database {config.DatabaseName}... (simulated)",
            $"Setting up IIS website at {config.InstallationPath}... (simulated)",
            "Configuring application settings... (simulated)",
            "Creating admin user... (simulated)",
            "Registering installation... (simulated)",
            "Finalizing setup... (simulated)"
        };

        for (int i = 0; i < steps.Length; i++)
        {
            ReportProgress(i + 1, (i + 1) * 100 / steps.Length, $"🎬 DEMO: {steps[i]}", i == steps.Length - 1);
            await Task.Delay(1000); // Simulate work
        }
    }
}

public class InstallationProgress
{
    public int CurrentStep { get; set; }
    public int Percentage { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool StepCompleted { get; set; }
}

public class InstallationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
