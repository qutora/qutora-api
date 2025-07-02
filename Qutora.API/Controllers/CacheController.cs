using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Infrastructure.Caching.Events;
using Qutora.Infrastructure.Caching.Services;

namespace Qutora.API.Controllers;

/// <summary>
/// Controller for managing API Key cache operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Only admins can manage cache
public class CacheController : ControllerBase
{
    private readonly IApiKeyCacheService _cacheService;
    private readonly CacheInvalidationService _invalidationService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        IApiKeyCacheService cacheService,
        CacheInvalidationService invalidationService,
        ILogger<CacheController> logger)
    {
        _cacheService = cacheService;
        _invalidationService = invalidationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets cache statistics and health information
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _cacheService.GetStatisticsAsync();
            
            return Ok(new
            {
                success = true,
                data = new
                {
                    initialized = _cacheService.IsInitialized,
                    statistics = stats,
                    healthy = await _invalidationService.IsHealthyAsync()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get cache statistics");
            return StatusCode(500, new { success = false, message = "Failed to get cache statistics" });
        }
    }

    /// <summary>
    /// Forces a full cache refresh
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshCache([FromQuery] string? reason = null)
    {
        try
        {
            var refreshReason = reason ?? "Manual admin refresh";
            _logger.LogInformation("🔄 Manual cache refresh initiated by admin: {Reason}", refreshReason);

            await _invalidationService.ForceRefreshAsync(refreshReason);

            var stats = await _cacheService.GetStatisticsAsync();

            return Ok(new
            {
                success = true,
                message = "Cache refreshed successfully",
                data = new
                {
                    refresh_time = stats.LastRefreshTime,
                    refresh_duration_ms = stats.LastRefreshDurationMs,
                    api_key_count = stats.ApiKeyCount,
                    permission_count = stats.PermissionCount,
                    bucket_count = stats.BucketCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to refresh cache");
            return StatusCode(500, new { success = false, message = "Failed to refresh cache", error = ex.Message });
        }
    }

    /// <summary>
    /// Checks cache health status
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> CheckHealth()
    {
        try
        {
            var isHealthy = await _invalidationService.IsHealthyAsync();
            var stats = await _cacheService.GetStatisticsAsync();

            var healthStatus = new
            {
                healthy = isHealthy,
                initialized = _cacheService.IsInitialized,
                warnings = stats.HealthWarnings,
                uptime_minutes = (DateTime.UtcNow - stats.LastRefreshTime).TotalMinutes,
                hit_ratio = stats.HitRatio,
                memory_usage_mb = Math.Round(stats.EstimatedMemoryUsageBytes / 1024.0 / 1024.0, 2)
            };

            if (isHealthy)
            {
                return Ok(new { success = true, data = healthStatus });
            }
            else
            {
                return StatusCode(503, new { success = false, message = "Cache is unhealthy", data = healthStatus });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to check cache health");
            return StatusCode(500, new { success = false, message = "Health check failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets detailed cache metrics for monitoring
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var stats = await _cacheService.GetStatisticsAsync();
            var uptime = DateTime.UtcNow - stats.LastRefreshTime;

            var metrics = new
            {
                // Basic counts
                api_keys = stats.ApiKeyCount,
                permissions = stats.PermissionCount,
                buckets = stats.BucketCount,
                
                // Performance metrics
                cache_hits = stats.CacheHits,
                cache_misses = stats.CacheMisses,
                hit_ratio_percent = Math.Round(stats.HitRatio, 2),
                total_requests = stats.CacheHits + stats.CacheMisses,
                
                // Memory metrics
                memory_usage_bytes = stats.EstimatedMemoryUsageBytes,
                memory_usage_mb = Math.Round(stats.EstimatedMemoryUsageBytes / 1024.0 / 1024.0, 2),
                
                // Timing metrics
                last_refresh_time = stats.LastRefreshTime,
                last_refresh_duration_ms = stats.LastRefreshDurationMs,
                uptime_minutes = Math.Round(uptime.TotalMinutes, 1),
                uptime_hours = Math.Round(uptime.TotalHours, 2),
                
                // Health metrics
                is_healthy = stats.IsHealthy,
                health_warnings = stats.HealthWarnings,
                initialized = _cacheService.IsInitialized,
                
                // Calculated metrics
                requests_per_minute = uptime.TotalMinutes > 0 ? Math.Round((stats.CacheHits + stats.CacheMisses) / uptime.TotalMinutes, 2) : 0,
                hits_per_minute = uptime.TotalMinutes > 0 ? Math.Round(stats.CacheHits / uptime.TotalMinutes, 2) : 0,
                average_memory_per_key_bytes = stats.ApiKeyCount > 0 ? stats.EstimatedMemoryUsageBytes / stats.ApiKeyCount : 0
            };

            return Ok(new { success = true, data = metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get cache metrics");
            return StatusCode(500, new { success = false, message = "Failed to get cache metrics" });
        }
    }

    /// <summary>
    /// Clears all cache data (emergency operation)
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCache([FromQuery] string? reason = null)
    {
        try
        {
            var clearReason = reason ?? "Manual admin clear";
            _logger.LogWarning("🗑️ Cache clear initiated by admin: {Reason}", clearReason);

            _cacheService.ClearAll();

            return Ok(new
            {
                success = true,
                message = "Cache cleared successfully",
                data = new
                {
                    cleared_at = DateTime.UtcNow,
                    reason = clearReason,
                    note = "Cache will be reloaded automatically by background service"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to clear cache");
            return StatusCode(500, new { success = false, message = "Failed to clear cache", error = ex.Message });
        }
    }
} 