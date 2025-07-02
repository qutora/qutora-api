using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Services;
using Qutora.Infrastructure.Caching.Events;
using Qutora.Infrastructure.Caching.Jobs;
using Qutora.Infrastructure.Caching.Services;

namespace Qutora.Application.Extensions;

/// <summary>
/// Extension methods for configuring API Key caching services
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// Adds API Key caching services to the DI container
    /// </summary>
    public static IServiceCollection AddApiKeyCaching(this IServiceCollection services)
    {
        // Add memory cache if not already added
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 10000; // Limit cache size to prevent memory issues
            options.TrackStatistics = true; // Enable statistics tracking
        });

        // Cache service (now uses IServiceScopeFactory for DI compatibility)
        services.AddSingleton<IApiKeyCacheService, ApiKeyCacheService>();

        // Cache event handler
        services.AddScoped<CacheInvalidationService>();

        // Background refresh service
        services.AddHostedService<ApiKeyCacheRefreshService>();

        // Register cached API key service with decorator pattern  
        services.AddScoped<CachedApiKeyService>(provider =>
        {
            // Get the original service that was registered
            var originalService = provider.GetRequiredService<ApiKeyService>();
            var cacheService = provider.GetRequiredService<IApiKeyCacheService>();
            var logger = provider.GetRequiredService<ILogger<CachedApiKeyService>>();
            
            return new CachedApiKeyService(originalService, cacheService, logger);
        });

        // Replace original API Key service with cached version
        services.Replace(ServiceDescriptor.Scoped<IApiKeyService>(provider =>
        {
            return provider.GetRequiredService<CachedApiKeyService>();
        }));

        return services;
    }

    /// <summary>
    /// Adds API Key caching services with custom configuration
    /// </summary>
    public static IServiceCollection AddApiKeyCaching(this IServiceCollection services, 
        Action<ApiKeyCacheOptions> configureOptions)
    {
        var options = new ApiKeyCacheOptions();
        configureOptions(options);

        // Configure memory cache with custom options
        services.AddMemoryCache(cacheOptions =>
        {
            cacheOptions.SizeLimit = options.MaxCacheSize;
            cacheOptions.TrackStatistics = true;
        });

        // Register options
        services.AddSingleton(options);

        // Add core caching services
        services.AddApiKeyCaching();

        return services;
    }

    /// <summary>
    /// Configures health checks for the caching system
    /// </summary>
    public static IServiceCollection AddApiKeyCacheHealthChecks(this IServiceCollection services)
    {
        // NOTE: Cache health check temporarily disabled for SDK generation
        // services.AddHealthChecks()
        //     .AddCheck<ApiKeyCacheHealthCheck>("apikey_cache", tags: new[] { "cache", "ready" });

        return services;
    }
}

/// <summary>
/// Configuration options for API Key caching
/// </summary>
public class ApiKeyCacheOptions
{
    /// <summary>
    /// Maximum number of items to cache (default: 10,000)
    /// </summary>
    public int MaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// Cache refresh interval in minutes (default: 30)
    /// </summary>
    public int RefreshIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Health check interval in minutes (default: 5)
    /// </summary>
    public int HealthCheckIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Cache entry TTL in hours (default: 2)
    /// </summary>
    public int CacheTtlHours { get; set; } = 2;

    /// <summary>
    /// Initial delay before first cache load in seconds (default: 30)
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable cache statistics tracking (default: true)
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// Whether to log cache operations (default: false for production)
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;
} 