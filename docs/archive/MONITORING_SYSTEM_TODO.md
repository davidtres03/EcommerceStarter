# Monitoring System Implementation TODO

**Status**: Placeholder pages exist, need background services to populate data  
**Priority**: Medium  
**Estimated Time**: ~75 minutes

## Current State
- ✅ Database tables created (`ServiceStatusLogs`, `ServiceErrorLogs`, `UpdateHistories`)
- ✅ Admin pages built and functional (ServiceDashboard, Updates, Errors, Metrics)
- ✅ Pages show "No data available" messages when empty
- ❌ No background service collecting metrics
- ❌ No error logging middleware
- ❌ No update tracking system

## Implementation Tasks

### 1. Background Health Monitoring Service (~30 min)
**File**: `Services/HealthMonitoringService.cs`

Create `IHostedService` that runs every 30-60 seconds:
- Check web service health (response time via HttpClient self-ping)
- Monitor system resources:
  ```csharp
  var process = Process.GetCurrentProcess();
  var cpuUsage = // Calculate CPU percentage
  var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB
  ```
- Test database connectivity (simple query)
- Calculate uptime percentage
- Write to `ServiceStatusLogs` table
- Keep last 7 days of data, purge older entries

**Register in Program.cs**:
```csharp
builder.Services.AddHostedService<HealthMonitoringService>();
```

### 2. Global Error Logging Middleware (~15 min)
**File**: `Middleware/ErrorLoggingMiddleware.cs`

Catch unhandled exceptions and log to `ServiceErrorLogs`:
- Capture exception message, stack trace, source
- Determine severity (Critical/Warning/Info)
- Log request path and user context
- Don't interfere with existing error handling
- Auto-purge acknowledged errors after 30 days

**Register in Program.cs**:
```csharp
app.UseMiddleware<ErrorLoggingMiddleware>();
```

### 3. Update Tracking System (~20 min)
**File**: `Services/UpdateTrackingService.cs`

Options for implementation:
1. Manual: Create admin page to log deployments
2. Automated: PowerShell script adds entry on publish
3. CI/CD: GitHub Actions writes to database on deployment

Record in `UpdateHistories` table:
- Version number (from assembly)
- Applied timestamp
- Status (Success/Failed/Rollback)
- Release notes
- Duration

### 4. Management API Endpoints (~10 min)
**File**: `Controllers/ServiceMonitoringController.cs` (already exists)

Add missing endpoints:
- `PUT /api/admin/service/errors/{id}/acknowledge` - Mark error as resolved
- `POST /api/admin/service/health-check` - Manual health check trigger
- `DELETE /api/admin/service/logs/cleanup` - Purge old logs

### 5. Configuration Settings
Add to `appsettings.json`:
```json
"Monitoring": {
  "HealthCheckIntervalSeconds": 60,
  "DataRetentionDays": 7,
  "EnableAutoCleanup": true,
  "AlertThresholds": {
    "CpuUsagePercent": 80,
    "MemoryUsageMB": 1024,
    "ResponseTimeMs": 1000
  }
}
```

## Testing Checklist
- [ ] Background service starts on application launch
- [ ] Status logs written every minute
- [ ] Errors captured and logged with correct severity
- [ ] Service Dashboard shows live data
- [ ] Metrics page displays 24-hour trends
- [ ] Updates page shows deployment history
- [ ] Errors page filters work correctly
- [ ] Acknowledge button marks errors as resolved
- [ ] Old logs auto-purge after retention period

## Future Enhancements (Optional)
- Email/SMS alerts for critical errors
- Integration with external monitoring (Application Insights, Datadog)
- Performance trend analysis and forecasting
- Automated rollback on failed updates
- Real-time WebSocket updates for dashboard
- Export logs to CSV/JSON
- Custom alerting rules engine

## Notes
- Pages currently work but show empty states
- All database migrations already applied
- No breaking changes required
- Can be implemented incrementally (start with health monitoring)
- Consider resource usage impact of frequent health checks

## Related Files
- `Pages/Admin/ServiceDashboard.cshtml` + `.cs`
- `Pages/Admin/Updates.cshtml` + `.cs`
- `Pages/Admin/Errors.cshtml` + `.cs`
- `Pages/Admin/Metrics.cshtml` + `.cs`
- `Models/Service/ServiceStatusLog.cs`
- `Models/Service/ServiceErrorLog.cs`
- `Models/Service/UpdateHistory.cs`
- `Data/ApplicationDbContext.cs` (DbSets already configured)
