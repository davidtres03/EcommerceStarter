# EcommerceStarter Background Service

## Overview

The Background Service is a Windows Service that runs continuously on the server to:
- Monitor application health (GET requests to the main web server every 5 minutes)
- Check for available updates (every 24 hours)
- Log activity to Windows Event Log for operational visibility
- Automatically restart if connectivity is lost

## Installation

### Prerequisites
- Windows Server 2016 or higher
- Administrator privileges
- .NET 9.0 Runtime installed

### Install the Service

```powershell
# Run from an Administrator PowerShell prompt
& "C:\Path\To\Install-WindowsService.ps1" -ServicePath "C:\Path\To\EcommerceStarter.WindowsService.exe"
```

**Service Details After Installation:**
- **Service Name**: EcommerceStarter-Background-Service
- **Display Name**: EcommerceStarter Background Service
- **Startup Type**: Automatic
- **Run As**: NETWORK SERVICE (default)
- **Recovery**: Automatically restarts after 60 seconds if stopped unexpectedly

### Verify Installation

```powershell
# Check service status
Get-Service "EcommerceStarter-Background-Service"

# View in Services.msc (GUI)
services.msc
```

## Uninstallation

```powershell
# Run from an Administrator PowerShell prompt
& "C:\Path\To\Uninstall-WindowsService.ps1"
```

## Management

### Start/Stop Service

```powershell
# Start
Start-Service "EcommerceStarter-Background-Service"

# Stop
Stop-Service "EcommerceStarter-Background-Service" -Force

# Restart
Restart-Service "EcommerceStarter-Background-Service"
```

### View Event Log

All service activities are logged to Windows Event Log under:
- **Event Viewer** → **Windows Logs** → **Application**
- **Source**: EcommerceStarter-Background-Service

Look for entries with:
- **Informational**: Normal operations (health checks, update checks)
- **Warning**: Temporary issues (connection timeouts)
- **Error**: Service failures requiring attention

### Configuration

Edit the service behavior by modifying constants in `BackgroundServiceWorker.cs`:

```csharp
// Health check interval (seconds)
private const int HEALTH_CHECK_INTERVAL_SECONDS = 5 * 60; // 5 minutes

// Update check interval (seconds)
private const int UPDATE_CHECK_INTERVAL_SECONDS = 24 * 60 * 60; // 24 hours

// HTTP request timeout (seconds)
private const int HTTP_TIMEOUT_SECONDS = 30;

// Failure threshold before alerting
private const int MAX_CONSECUTIVE_FAILURES = 5;
```

After modifying, rebuild the project and reinstall the service:

```powershell
dotnet build --configuration Release
& "C:\Path\To\Uninstall-WindowsService.ps1"
& "C:\Path\To\Install-WindowsService.ps1" -ServicePath "C:\Path\To\EcommerceStarter.WindowsService.exe"
```

## Troubleshooting

### Service Won't Start

1. **Check event log** for error details
   ```powershell
   Get-WinEvent -LogName Application -InstanceId 1000 -MaxEvents 10 | Format-Table TimeCreated, Message
   ```

2. **Verify executable path** is correct and file exists
   ```powershell
   Test-Path "C:\Path\To\EcommerceStarter.WindowsService.exe"
   ```

3. **Check .NET runtime** is installed
   ```powershell
   dotnet --list-runtimes
   ```

### Service Keeps Stopping

1. Check application logs in Event Log for exceptions
2. Verify the web server (port 5000) is running for health checks
3. Check disk space and memory availability
4. Review firewall rules - service needs HTTP access to localhost:5000

### High CPU Usage

- Service should use <5% CPU normally
- Check if health checks are timing out (indicates web server issues)
- If elevated during update checks, that's normal and temporary

## Architecture

The service uses .NET's `BackgroundService` pattern:

```
BackgroundServiceWorker
├── ExecuteAsync() - Main loop
│   ├── Health Check Task (runs every 5 minutes)
│   │   ├── GET http://localhost:5000
│   │   ├── 30-second timeout
│   │   └── Failure tracking
│   │
│   └── Update Check Task (runs every 24 hours)
│       ├── Query update endpoint
│       └── Log available updates
│
└── Graceful Shutdown
    ├── Cancel health/update check tasks
    ├── Dispose HttpClient
    └── Exit cleanly
```

## Security Considerations

- Service runs as **NETWORK SERVICE** (least-privilege account)
- HTTP health checks use **localhost:5000** (internal only)
- Event Log requires admin to view errors
- No sensitive data logged (only timestamps and status)
- Configuration in appsettings.json can contain credentials (protect file permissions)

## Performance

- **Memory**: ~50-100 MB base + runtime
- **CPU**: <5% during normal operation
- **Network**: One HTTP request every 5 minutes (~80 bytes payload)
- **Disk**: Minimal (Event Log rotation handled by Windows)

## Monitoring & Alerting

To integrate with third-party monitoring (Datadog, New Relic, etc.):

1. **Event Log Integration**: Most monitoring tools can read Windows Event Log
2. **Health Endpoint**: Service checks `/api/ai/status` - same endpoint available for external monitoring
3. **Custom Metrics**: Event ID patterns in Event Log can trigger alerts

## Support

For issues, check:
1. Windows Event Log (Application source)
2. Service status: `Get-Service "EcommerceStarter-Background-Service"`
3. Web server logs at `localhost:5000`
4. Firewall rules allowing localhost traffic
