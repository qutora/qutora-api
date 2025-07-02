using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qutora.Infrastructure.Caching.Services;

namespace Qutora.Infrastructure.Caching.Jobs;

/// <summary>
/// Background service that periodically refreshes the API Key cache
/// </summary>
public class ApiKeyCacheRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApiKeyCacheRefreshService> _logger;
    
    // Configuration
    private readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(1); // Wait 5s before first refresh
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(30); // Refresh every 30 minutes
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(5); // Check health every 5 minutes

    public ApiKeyCacheRefreshService(
        IServiceProvider serviceProvider,
        ILogger<ApiKeyCacheRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 API Key Cache Refresh Service starting...");

        try
        {
            // Wait before initial load to let the application fully start
            await Task.Delay(_initialDelay, stoppingToken);

            // Perform initial cache load
            await PerformCacheRefresh();

            // Start periodic refresh timer
            using var refreshTimer = new PeriodicTimer(_refreshInterval);
            using var healthTimer = new PeriodicTimer(_healthCheckInterval);

            var refreshTask = PeriodicRefreshAsync(refreshTimer, stoppingToken);
            var healthTask = PeriodicHealthCheckAsync(healthTimer, stoppingToken);

            // Wait for either task to complete (shouldn't happen unless cancellation)
            await Task.WhenAny(refreshTask, healthTask);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 API Key Cache Refresh Service stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "💥 API Key Cache Refresh Service failed critically");
            throw;
        }
    }

    private async Task PeriodicRefreshAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PerformCacheRefresh();
        }
    }

    private async Task PeriodicHealthCheckAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PerformHealthCheck();
        }
    }

    private async Task PerformCacheRefresh()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<IApiKeyCacheService>();

            _logger.LogDebug("🔄 Starting scheduled cache refresh...");
            
            await cacheService.LoadAllDataAsync();
            
            var stats = await cacheService.GetStatisticsAsync();
            _logger.LogInformation("✅ Scheduled cache refresh completed: {Stats}", stats);

            // Log performance metrics
            if (stats.LastRefreshDurationMs > 5000) // Warn if refresh takes more than 5 seconds
            {
                _logger.LogWarning("⚠️ Cache refresh took longer than expected: {Duration}ms", stats.LastRefreshDurationMs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Scheduled cache refresh failed - will retry on next interval");
        }
    }

    private async Task PerformHealthCheck()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<IApiKeyCacheService>();

            if (!cacheService.IsInitialized)
            {
                _logger.LogWarning("⚠️ Cache is not initialized, attempting refresh...");
                await PerformCacheRefresh();
                return;
            }

            var stats = await cacheService.GetStatisticsAsync();
            
            if (!stats.IsHealthy)
            {
                _logger.LogWarning("⚠️ Cache health check failed: {Warnings}", string.Join(", ", stats.HealthWarnings));
                
                // If cache is really stale, force refresh
                var timeSinceRefresh = DateTime.UtcNow - stats.LastRefreshTime;
                if (timeSinceRefresh.TotalHours > 2)
                {
                    _logger.LogWarning("🔄 Cache is stale ({Hours:F1}h), forcing refresh...", timeSinceRefresh.TotalHours);
                    await PerformCacheRefresh();
                }
            }
            else
            {
                _logger.LogDebug("💚 Cache health check passed: {Stats}", stats);
            }

            // Performance monitoring
            if (stats.HitRatio < 70)
            {
                _logger.LogWarning("📉 Low cache hit ratio detected: {HitRatio:F1}%", stats.HitRatio);
            }

            if (stats.EstimatedMemoryUsageBytes > 100 * 1024 * 1024) // 100MB
            {
                _logger.LogWarning("💾 High cache memory usage: {MemoryMB:F1}MB", 
                    stats.EstimatedMemoryUsageBytes / 1024.0 / 1024.0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Cache health check failed");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 API Key Cache Refresh Service is stopping...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("✋ API Key Cache Refresh Service stopped");
    }
} 