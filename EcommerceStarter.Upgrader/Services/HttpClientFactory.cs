using System;
using System.Net.Http;

namespace EcommerceStarter.Upgrader.Services;

/// <summary>
/// Shared HttpClient factory to avoid creating multiple instances
/// Using multiple HttpClient instances can cause InvalidOperationException
/// Best practice: reuse a single HttpClient throughout the application
/// </summary>
public static class HttpClientFactory
{
    private static readonly Lazy<HttpClient> _instance = new(() =>
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "EcommerceStarter-Installer");

        // Set reasonable timeouts
        client.Timeout = TimeSpan.FromSeconds(30);

        return client;
    });

    /// <summary>
    /// Get the shared HttpClient instance
    /// </summary>
    public static HttpClient GetHttpClient()
    {
        var client = _instance.Value;
        // Timeout is already set during initialization (line 18)
        // Do NOT modify it here to prevent InvalidOperationException after first request
        return client;
    }
}
