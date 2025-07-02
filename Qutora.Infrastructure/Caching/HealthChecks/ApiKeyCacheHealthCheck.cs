using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Qutora.Infrastructure.Caching.Services;
using Qutora.Infrastructure.Caching.Models;

namespace Qutora.Infrastructure.Caching.HealthChecks;

/// <summary>
/// Health check for API Key cache system
/// </summary>
public class ApiKeyCacheHealthCheck : IHealthCheck
{
    private readonly IApiKeyCacheService _cacheService;
    private readonly ILogger<ApiKeyCacheHealthCheck> _logger;

    public ApiKeyCacheHealthCheck(
        IApiKeyCacheService cacheService,
        ILogger<ApiKeyCacheHealthCheck> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if cache is initialized
            if (!_cacheService.IsInitialized)
            {
                return HealthCheckResult.Unhealthy(
                    "API Key cache is not initialized",
                    data: new Dictionary<string, object>
                    {
                        ["initialized"] = false,
                        ["timestamp"] = DateTime.UtcNow
                    });
            }

            // Get cache statistics
            var stats = await _cacheService.GetStatisticsAsync();

            // Determine health status based on statistics
            var healthStatus = DetermineHealthStatus(stats);
            var statusMessage = CreateStatusMessage(stats);

            // Create health data
            var healthData = new Dictionary<string, object>
            {
                ["initialized"] = true,
                ["api_key_count"] = stats.ApiKeyCount,
                ["permission_count"] = stats.PermissionCount,
                ["bucket_count"] = stats.BucketCount,
                ["last_refresh_time"] = stats.LastRefreshTime,
                ["last_refresh_duration_ms"] = stats.LastRefreshDurationMs,
                ["cache_hits"] = stats.CacheHits,
                ["cache_misses"] = stats.CacheMisses,
                ["hit_ratio_percent"] = Math.Round(stats.HitRatio, 2),
                ["memory_usage_bytes"] = stats.EstimatedMemoryUsageBytes,
                ["memory_usage_mb"] = Math.Round(stats.EstimatedMemoryUsageBytes / 1024.0 / 1024.0, 2),
                ["is_healthy"] = stats.IsHealthy,
                ["health_warnings"] = stats.HealthWarnings,
                ["timestamp"] = DateTime.UtcNow
            };

            // Add performance metrics
            var timeSinceRefresh = DateTime.UtcNow - stats.LastRefreshTime;
            healthData["minutes_since_last_refresh"] = Math.Round(timeSinceRefresh.TotalMinutes, 1);

            return new HealthCheckResult(healthStatus, statusMessage, data: healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Health check failed for API Key cache");

            return HealthCheckResult.Unhealthy(
                "API Key cache health check failed",
                ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                });
        }
    }

    private static HealthStatus DetermineHealthStatus(CacheStatistics stats)
    {
        // Critical issues - Unhealthy
        if (!stats.IsHealthy)
            return HealthStatus.Unhealthy;

        // Performance issues - Degraded
        if (stats.HitRatio < 50) // Very low hit ratio
            return HealthStatus.Degraded;

        if (stats.LastRefreshDurationMs > 10000) // Refresh takes more than 10 seconds
            return HealthStatus.Degraded;

        var timeSinceRefresh = DateTime.UtcNow - stats.LastRefreshTime;
        if (timeSinceRefresh.TotalMinutes > 45) // No refresh in 45 minutes (refresh interval is 30min)
            return HealthStatus.Degraded;

        if (stats.EstimatedMemoryUsageBytes > 200 * 1024 * 1024) // More than 200MB
            return HealthStatus.Degraded;

        // Minor issues - Degraded
        if (stats.HitRatio < 70) // Low hit ratio
            return HealthStatus.Degraded;

        if (stats.HealthWarnings.Any())
            return HealthStatus.Degraded;

        // All good - Healthy
        return HealthStatus.Healthy;
    }

    private static string CreateStatusMessage(CacheStatistics stats)
    {
        if (!stats.IsHealthy)
        {
            var warnings = string.Join(", ", stats.HealthWarnings);
            return $"API Key cache is unhealthy: {warnings}";
        }

        var timeSinceRefresh = DateTime.UtcNow - stats.LastRefreshTime;
        var hitRatio = Math.Round(stats.HitRatio, 1);
        var memoryMB = Math.Round(stats.EstimatedMemoryUsageBytes / 1024.0 / 1024.0, 1);

        if (stats.HealthWarnings.Any())
        {
            var warnings = string.Join(", ", stats.HealthWarnings);
            return $"API Key cache is degraded: {warnings}. " +
                   $"Stats: {stats.ApiKeyCount} keys, {hitRatio}% hit ratio, {memoryMB}MB memory, " +
                   $"refreshed {timeSinceRefresh.TotalMinutes:F0}min ago";
        }

        return $"API Key cache is healthy: {stats.ApiKeyCount} keys, {hitRatio}% hit ratio, " +
               $"{memoryMB}MB memory, refreshed {timeSinceRefresh.TotalMinutes:F0}min ago " +
               $"({stats.LastRefreshDurationMs}ms)";
    }
} 