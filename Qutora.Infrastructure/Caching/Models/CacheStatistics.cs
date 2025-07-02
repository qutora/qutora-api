namespace Qutora.Infrastructure.Caching.Models;

/// <summary>
/// Cache performance statistics
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Number of API keys in the cache
    /// </summary>
    public int ApiKeyCount { get; set; }

    /// <summary>
    /// Number of permissions in the cache
    /// </summary>
    public int PermissionCount { get; set; }

    /// <summary>
    /// Number of buckets in the cache
    /// </summary>
    public int BucketCount { get; set; }

    /// <summary>
    /// Last cache refresh time
    /// </summary>
    public DateTime LastRefreshTime { get; set; }

    /// <summary>
    /// Duration of last refresh in milliseconds
    /// </summary>
    public long LastRefreshDurationMs { get; set; }

    /// <summary>
    /// Total number of cache hits
    /// </summary>
    public long CacheHits { get; set; }

    /// <summary>
    /// Total number of cache misses
    /// </summary>
    public long CacheMisses { get; set; }

    /// <summary>
    /// Cache hit ratio as percentage
    /// </summary>
    public double HitRatio => CacheHits + CacheMisses > 0 ? (double)CacheHits / (CacheHits + CacheMisses) * 100 : 0;

    /// <summary>
    /// Estimated memory usage in bytes
    /// </summary>
    public long EstimatedMemoryUsageBytes { get; set; }

    /// <summary>
    /// Health warnings
    /// </summary>
    public List<string> HealthWarnings { get; set; } = new();

    /// <summary>
    /// Whether the cache is healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Cache status
    /// </summary>
    public string Status { get; set; } = "Unknown";

    public override string ToString()
    {
        return $"API Keys: {ApiKeyCount}, Permissions: {PermissionCount}, Buckets: {BucketCount}, " +
               $"Hit Ratio: {HitRatio:F1}%, Memory: {EstimatedMemoryUsageBytes / 1024 / 1024:F1}MB, " +
               $"Last Refresh: {LastRefreshTime:yyyy-MM-dd HH:mm:ss}";
    }
} 