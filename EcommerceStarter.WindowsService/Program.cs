using EcommerceStarter.WindowsService;
using Microsoft.Extensions.Hosting.WindowsServices;

// Build host with Windows Service support
var builder = Host.CreateApplicationBuilder(args);

// Add Windows Service support if running as a service
var isService = !builder.Environment.IsDevelopment() || args.Contains("--service");
if (isService)
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "EcommerceStarter-Background-Service";
    });
}

// Add HTTP client for update checks and health monitoring
builder.Services.AddHttpClient();

// Add custom services
builder.Services.AddSingleton<EcommerceStarter.WindowsService.Services.RegistryConfigService>();
builder.Services.AddScoped<UpdateService>();
builder.Services.AddHostedService<BackgroundServiceWorker>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add EventLog provider if running as service
if (isService && OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog(new Microsoft.Extensions.Logging.EventLog.EventLogSettings
    {
        SourceName = "EcommerceStarter-Service",
        LogName = "Application"
    });
}

var host = builder.Build();
host.Run();
