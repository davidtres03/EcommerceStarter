<#
.SYNOPSIS
    IIS Configuration Helper Functions for EcommerceStarter

.DESCRIPTION
    Contains all IIS-related configuration functions used by the main deployment script.
    This includes application pool creation, website setup, SSL certificates, and permissions.

.NOTES
    This file is imported by Deploy-Windows.ps1
    Version: 1.0.0
#>

# ============================================================================
# IIS CONFIGURATION FUNCTIONS
# ============================================================================

function New-IISAppPool {
    param(
        [hashtable]$Config,
        [string]$AppPoolName
    )
    
    Write-ColorOutput "Creating IIS Application Pool..." -Type "Step"
    
    try {
        Import-Module WebAdministration -ErrorAction Stop
        
        # Check if app pool exists
        if (Test-Path "IIS:\AppPools\$AppPoolName") {
            Write-ColorOutput "Application pool '$AppPoolName' already exists" -Type "Warning"
            $overwrite = Read-Host "Recreate application pool? (y/N)"
            
            if ($overwrite -eq 'y' -or $overwrite -eq 'Y') {
                Remove-WebAppPool -Name $AppPoolName
                Write-ColorOutput "Removed existing application pool" -Type "Info"
            }
            else {
                Write-ColorOutput "Using existing application pool" -Type "Info"
                return $true
            }
        }
        
        # Create new application pool
        New-WebAppPool -Name $AppPoolName | Out-Null
        
        # Configure app pool for .NET Core
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "startMode" -Value "AlwaysRunning"
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "processModel.idleTimeout" -Value ([TimeSpan]::FromMinutes(0))
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "recycling.periodicRestart.time" -Value ([TimeSpan]::FromHours(29))
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "processModel.loadUserProfile" -Value $true
        
        Write-ColorOutput "Application pool '$AppPoolName' created successfully" -Type "Success"
        Write-ColorOutput "  Runtime: No Managed Code (.NET Core)" -Type "Info"
        Write-ColorOutput "  Start Mode: Always Running" -Type "Info"
        Write-ColorOutput "  Idle Timeout: Disabled" -Type "Info"
        Write-ColorOutput "  User Profile: Loaded" -Type "Info"
        
        return $true
    }
    catch {
        Write-ColorOutput "Failed to create application pool: $($_.Exception.Message)" -Type "Error"
        return $false
    }
}

function Publish-Application {
    param(
        [string]$AppPath,
        [string]$PublishPath
    )
    
    Write-ColorOutput "Publishing application..." -Type "Step"
    
    try {
        if (-not (Test-Path $PublishPath)) {
            New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null
        }
        
        Push-Location $AppPath
        
        Write-ColorOutput "Running dotnet publish..." -Type "Info"
        $output = dotnet publish -c Release --output $PublishPath 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            throw "Publish failed with exit code $LASTEXITCODE"
        }
        
        Pop-Location
        
        Write-ColorOutput "Application published successfully" -Type "Success"
        Write-ColorOutput "  Output: $PublishPath" -Type "Info"
        
        return $true
    }
    catch {
        Pop-Location -ErrorAction SilentlyContinue
        Write-ColorOutput "Failed to publish application: $($_.Exception.Message)" -Type "Error"
        return $false
    }
}

function New-IISSite {
    param(
        [hashtable]$Config,
        [string]$AppPoolName,
        [string]$PhysicalPath
    )
    
    Write-ColorOutput "Creating IIS Website..." -Type "Step"
    
    try {
        Import-Module WebAdministration -ErrorAction Stop
        
        $siteName = $Config.SiteName
        $port = [int]$Config.Port
        $domain = $Config.Domain
        
        # Check if site exists
        if (Test-Path "IIS:\Sites\$siteName") {
            Write-ColorOutput "Website '$siteName' already exists" -Type "Warning"
            $overwrite = Read-Host "Recreate website? (y/N)"
            
            if ($overwrite -eq 'y' -or $overwrite -eq 'Y') {
                Remove-Website -Name $siteName
                Write-ColorOutput "Removed existing website" -Type "Info"
            }
            else {
                Write-ColorOutput "Using existing website" -Type "Info"
                return $true
            }
        }
        
        # Create website with HTTP binding
        New-Website -Name $siteName `
                    -PhysicalPath $PhysicalPath `
                    -ApplicationPool $AppPoolName `
                    -Port 80 `
                    -HostHeader $domain | Out-Null
        
        Write-ColorOutput "Website '$siteName' created successfully" -Type "Success"
        Write-ColorOutput "  Physical Path: $PhysicalPath" -Type "Info"
        Write-ColorOutput "  App Pool: $AppPoolName" -Type "Info"
        Write-ColorOutput "  HTTP Binding: http://${domain}:80" -Type "Info"
        
        # Add HTTPS binding if requested
        if ($port -eq 443) {
            try {
                $cert = New-IISSelfSignedCertificate -Config $Config
                
                if ($cert) {
                    # Add HTTPS binding
                    New-WebBinding -Name $siteName -Protocol "https" -Port 443 -HostHeader $domain -ErrorAction Stop
                    
                    # Bind certificate
                    $binding = Get-WebBinding -Name $siteName -Protocol "https"
                    $binding.AddSslCertificate($cert.Thumbprint, "My")
                    
                    Write-ColorOutput "  HTTPS Binding: https://${domain}:443" -Type "Info"
                    Write-ColorOutput "  Certificate: Self-Signed (? Replace in production!)" -Type "Warning"
                }
            }
            catch {
                Write-ColorOutput "HTTPS binding failed: $($_.Exception.Message)" -Type "Warning"
                Write-ColorOutput "Site is accessible via HTTP only" -Type "Info"
            }
        }
        
        # Start the website
        Start-Website -Name $siteName -ErrorAction Stop
        Write-ColorOutput "Website started successfully" -Type "Success"
        
        return $true
    }
    catch {
        Write-ColorOutput "Failed to create website: $($_.Exception.Message)" -Type "Error"
        return $false
    }
}

function New-IISSelfSignedCertificate {
    param(
        [hashtable]$Config
    )
    
    Write-ColorOutput "Creating self-signed SSL certificate..." -Type "Step"
    
    try {
        $domain = $Config.Domain
        $friendlyName = "EcommerceStarter - $($Config.CompanyName)"
        
        # Create self-signed certificate
        $cert = New-SelfSignedCertificate `
            -DnsName $domain, "localhost" `
            -CertStoreLocation "Cert:\LocalMachine\My" `
            -FriendlyName $friendlyName `
            -NotAfter (Get-Date).AddYears(5) `
            -KeyUsage DigitalSignature, KeyEncipherment `
            -KeyAlgorithm RSA `
            -KeyLength 2048 `
            -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")
        
        Write-ColorOutput "Self-signed certificate created" -Type "Success"
        Write-ColorOutput "  Subject: CN=$domain" -Type "Info"
        Write-ColorOutput "  Thumbprint: $($cert.Thumbprint)" -Type "Info"
        Write-ColorOutput "  Valid Until: $($cert.NotAfter.ToString('yyyy-MM-dd'))" -Type "Info"
        
        return $cert
    }
    catch {
        Write-ColorOutput "Failed to create SSL certificate: $($_.Exception.Message)" -Type "Error"
        return $null
    }
}

function Set-IISPermissions {
    param(
        [string]$PhysicalPath,
        [string]$AppPoolName
    )
    
    Write-ColorOutput "Configuring NTFS permissions..." -Type "Step"
    
    try {
        if (-not (Test-Path $PhysicalPath)) {
            throw "Physical path does not exist: $PhysicalPath"
        }
        
        $acl = Get-Acl $PhysicalPath
        
        # Grant IIS_IUSRS read access
        Write-ColorOutput "Granting IIS_IUSRS permissions..." -Type "Info"
        $iisUsersRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            "IIS_IUSRS",
            "ReadAndExecute",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow"
        )
        $acl.AddAccessRule($iisUsersRule)
        
        # Grant application pool identity access
        Write-ColorOutput "Granting '$AppPoolName' app pool permissions..." -Type "Info"
        $appPoolIdentity = "IIS AppPool\$AppPoolName"
        $appPoolRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $appPoolIdentity,
            "ReadAndExecute",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow"
        )
        $acl.AddAccessRule($appPoolRule)
        
        # Apply ACL
        Set-Acl $PhysicalPath $acl
        
        Write-ColorOutput "NTFS permissions configured successfully" -Type "Success"
        return $true
    }
    catch {
        Write-ColorOutput "Failed to set permissions: $($_.Exception.Message)" -Type "Warning"
        Write-ColorOutput "You may need to configure permissions manually" -Type "Info"
        return $false
    }
}

function Install-ASPNETCoreModule {
    Write-ColorOutput "Checking for ASP.NET Core Module..." -Type "Step"
    
    try {
        Import-Module WebAdministration -ErrorAction Stop
        
        # Check if module is installed
        $module = Get-WebGlobalModule -Name "AspNetCoreModuleV2" -ErrorAction SilentlyContinue
        
        if ($module) {
            Write-ColorOutput "ASP.NET Core Module V2 is already installed" -Type "Success"
            return $true
        }
        
        Write-ColorOutput "ASP.NET Core Module not found" -Type "Warning"
        Write-ColorOutput "This is typically installed with .NET Runtime Hosting Bundle" -Type "Info"
        Write-ColorOutput "The module may be installed after .NET SDK installation" -Type "Info"
        
        return $true
    }
    catch {
        Write-ColorOutput "Could not verify ASP.NET Core Module: $($_.Exception.Message)" -Type "Warning"
        return $true
    }
}

function Open-BrowserToSite {
    param(
        [hashtable]$Config
    )
    
    Write-ColorOutput "Opening browser..." -Type "Step"
    
    try {
        $domain = $Config.Domain
        $port = $Config.Port
        
        # Determine URL
        if ($port -eq "443" -or $port -eq 443) {
            $url = "https://$domain/Admin/Dashboard"
        }
        else {
            $url = "http://$domain/Admin/Dashboard"
        }
        
        Write-ColorOutput "Opening $url" -Type "Info"
        Start-Process $url
        
        return $true
    }
    catch {
        Write-ColorOutput "Could not open browser: $($_.Exception.Message)" -Type "Warning"
        return $false
    }
}

Export-ModuleMember -Function *
