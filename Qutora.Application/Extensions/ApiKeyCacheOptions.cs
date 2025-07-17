namespace Qutora.Application.Extensions;

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