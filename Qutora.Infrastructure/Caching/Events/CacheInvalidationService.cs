using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Caching.Services;

namespace Qutora.Infrastructure.Caching.Events;

/// <summary>
/// Service responsible for handling cache invalidation events
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IApiKeyCacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        IApiKeyCacheService cacheService,
        ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Handles API key creation - adds to cache
    /// </summary>
    public async Task OnApiKeyCreatedAsync(Guid apiKeyId)
    {
        _logger.LogDebug("🔄 API key created, triggering cache refresh: {ApiKeyId}", apiKeyId);
        
        // For new items, we could reload entire cache or try to load just this item
        // For simplicity, we'll do a full refresh
        await RefreshCacheAsync("API key created");
    }

    /// <summary>
    /// Handles API key updates - updates cache entry
    /// </summary>
    public async Task OnApiKeyUpdatedAsync(Guid apiKeyId)
    {
        _logger.LogDebug("🔄 API key updated, triggering cache refresh: {ApiKeyId}", apiKeyId);
        
        // We could be more granular here and just update the specific key
        // But for data consistency, full refresh is safer
        await RefreshCacheAsync("API key updated");
    }

    /// <summary>
    /// Handles API key deletion - removes from cache
    /// </summary>
    public async Task OnApiKeyDeletedAsync(Guid apiKeyId, string? key = null)
    {
        try
        {
            _logger.LogDebug("🗑️ API key deleted, removing from cache: {ApiKeyId}", apiKeyId);
            
            _cacheService.RemoveApiKey(apiKeyId, key);
            
            // Also remove all permissions for this API key
            _cacheService.RemoveAllPermissions(apiKeyId);
            
            _logger.LogDebug("✅ Removed API key and its permissions from cache: {ApiKeyId}", apiKeyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to remove API key from cache: {ApiKeyId}", apiKeyId);
            // Fallback to full refresh
            await RefreshCacheAsync("API key deletion cleanup failed");
        }
    }

    /// <summary>
    /// Handles bucket permission creation
    /// </summary>
    public async Task OnBucketPermissionCreatedAsync(Guid apiKeyId, Guid bucketId)
    {
        _logger.LogDebug("🔄 Bucket permission created, triggering cache refresh: ApiKey={ApiKeyId}, Bucket={BucketId}", 
            apiKeyId, bucketId);
        
        await RefreshCacheAsync("Bucket permission created");
    }

    /// <summary>
    /// Handles bucket permission updates
    /// </summary>
    public async Task OnBucketPermissionUpdatedAsync(Guid apiKeyId, Guid bucketId)
    {
        _logger.LogDebug("🔄 Bucket permission updated, triggering cache refresh: ApiKey={ApiKeyId}, Bucket={BucketId}", 
            apiKeyId, bucketId);
        
        await RefreshCacheAsync("Bucket permission updated");
    }

    /// <summary>
    /// Handles bucket permission deletion
    /// </summary>
    public async Task OnBucketPermissionDeletedAsync(Guid apiKeyId, Guid bucketId)
    {
        try
        {
            _logger.LogDebug("🗑️ Bucket permission deleted, removing from cache: ApiKey={ApiKeyId}, Bucket={BucketId}", 
                apiKeyId, bucketId);
            
            _cacheService.RemovePermission(apiKeyId, bucketId);
            
            _logger.LogDebug("✅ Removed bucket permission from cache: ApiKey={ApiKeyId}, Bucket={BucketId}", 
                apiKeyId, bucketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to remove bucket permission from cache: ApiKey={ApiKeyId}, Bucket={BucketId}", 
                apiKeyId, bucketId);
                
            // Fallback to full refresh
            await RefreshCacheAsync("Bucket permission deletion cleanup failed");
        }
    }

    /// <summary>
    /// Handles storage bucket changes
    /// </summary>
    public async Task OnStorageBucketChangedAsync(Guid bucketId, string changeType)
    {
        _logger.LogDebug("🔄 Storage bucket {ChangeType}, triggering cache refresh: {BucketId}", 
            changeType, bucketId);
        
        if (changeType.Equals("deleted", StringComparison.OrdinalIgnoreCase))
        {
            _cacheService.RemoveBucket(bucketId);
        }
        
        await RefreshCacheAsync($"Storage bucket {changeType}");
    }

    /// <summary>
    /// Forces a full cache refresh
    /// </summary>
    public async Task ForceRefreshAsync(string reason = "Manual refresh")
    {
        await RefreshCacheAsync($"Force refresh: {reason}");
    }

    /// <summary>
    /// Internal method to perform cache refresh with error handling
    /// </summary>
    private async Task RefreshCacheAsync(string reason)
    {
        try
        {
            _logger.LogDebug("🔄 Starting cache refresh: {Reason}", reason);
            
            await _cacheService.LoadAllDataAsync();
            
            _logger.LogDebug("✅ Cache refresh completed: {Reason}", reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Cache refresh failed: {Reason} - background service will retry", reason);
        }
    }

    /// <summary>
    /// Handles batch operations that might affect multiple cache entries
    /// </summary>
    public async Task OnBatchOperationAsync(string operationType, int affectedCount)
    {
        _logger.LogInformation("🔄 Batch operation detected: {OperationType} affected {Count} items, refreshing cache", 
            operationType, affectedCount);
            
        await RefreshCacheAsync($"Batch operation: {operationType}");
    }

    /// <summary>
    /// Handles system startup - ensures cache is loaded
    /// </summary>
    public async Task OnSystemStartupAsync()
    {
        _logger.LogInformation("🚀 System startup detected, loading cache...");
        
        try
        {
            if (!_cacheService.IsInitialized)
            {
                await _cacheService.LoadAllDataAsync();
                _logger.LogInformation("✅ Cache loaded successfully on system startup");
            }
            else
            {
                _logger.LogDebug("💡 Cache already initialized on system startup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to load cache on system startup");
            throw; // This is critical enough to fail startup
        }
    }

    /// <summary>
    /// Gets current cache health status
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            if (!_cacheService.IsInitialized)
                return false;
                
            var stats = await _cacheService.GetStatisticsAsync();
            return stats.IsHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to check cache health");
            return false;
        }
    }
} 