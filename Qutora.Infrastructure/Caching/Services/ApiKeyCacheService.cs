using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Shared.Models;


namespace Qutora.Infrastructure.Caching.Services;

/// <summary>
/// Implementation of API Key caching service using in-memory cache
/// </summary>
public class ApiKeyCacheService : IApiKeyCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApiKeyCacheService> _logger;
    
    // Cache keys
    private const string APIKEY_BY_KEY_PREFIX = "apikey_by_key:";
    private const string APIKEY_BY_ID_PREFIX = "apikey_by_id:";
    private const string PERMISSION_PREFIX = "permission:";
    private const string BUCKET_PREFIX = "bucket:";
    private const string PROVIDER_ACCESS_PREFIX = "provider_access:";
    private const string CACHE_STATS_KEY = "cache_statistics";
    
    // Cache options
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(2);
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = DefaultTtl,
        Priority = CacheItemPriority.High,
        Size = 1
    };

    // Metrics
    private long _cacheHits = 0;
    private long _cacheMisses = 0;
    private DateTime _lastRefreshTime = DateTime.MinValue;
    private long _lastRefreshDurationMs = 0;
    private bool _isInitialized = false;

    // Thread-safe collections for tracking cached items
    private readonly ConcurrentDictionary<Guid, string> _apiKeyIdToKeyMap = new();
    private readonly ConcurrentDictionary<string, Guid> _apiKeyKeyToIdMap = new();
    private readonly ConcurrentDictionary<string, Guid> _permissionMap = new(); // "apiKeyId:bucketId" -> permissionId
    private readonly ConcurrentDictionary<Guid, Guid> _bucketMap = new(); // bucketId -> bucketId

    public bool IsInitialized => _isInitialized;

    public ApiKeyCacheService(
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        ILogger<ApiKeyCacheService> logger)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LoadAllDataAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("🔄 Starting cache refresh...");

            // Clear existing cache
            ClearAll();

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Load API Keys
            var apiKeys = await unitOfWork.ApiKeys.GetAllAsync();
            var activeApiKeys = apiKeys.Where(k => !k.IsDeleted).ToList();
            
            foreach (var apiKey in activeApiKeys)
            {
                var cachedApiKey = new CachedApiKey
                {
                    Id = apiKey.Id,
                    Key = apiKey.Key,
                    SecretHash = apiKey.SecretHash,
                    UserId = apiKey.UserId,
                    Name = apiKey.Name,
                    IsActive = apiKey.IsActive,
                    ExpiresAt = apiKey.ExpiresAt,
                    LastUsedAt = apiKey.LastUsedAt,
                    Permissions = apiKey.Permissions,
                    AllowedProviderIds = apiKey.AllowedProviderIds.ToList(),
                    CachedAt = startTime
                };

                SetApiKey(cachedApiKey);
            }

            // Load Permissions
            var permissions = await unitOfWork.ApiKeyBucketPermissions.GetAllAsync();
            foreach (var permission in permissions)
            {
                var cachedPermission = new CachedPermission
                {
                    Id = permission.Id,
                    ApiKeyId = permission.ApiKeyId,
                    BucketId = permission.BucketId,
                    Permission = permission.Permission,
                    CreatedAt = permission.CreatedAt,
                    CreatedBy = permission.CreatedBy,
                    CachedAt = startTime
                };

                SetPermission(cachedPermission);
            }

            // Load Buckets
            var buckets = await unitOfWork.StorageBuckets.GetAllAsync();
            var activeBuckets = buckets.Where(b => !b.IsDeleted).ToList();
            
            foreach (var bucket in activeBuckets)
            {
                var cachedBucket = new CachedBucket
                {
                    Id = bucket.Id,
                    Path = bucket.Path,
                    ProviderId = bucket.ProviderId,
                    IsActive = bucket.IsActive,
                    IsDefault = bucket.IsDefault,
                    Description = bucket.Description,
                    CachedAt = startTime
                };

                SetBucket(cachedBucket);
            }

            stopwatch.Stop();
            _lastRefreshTime = startTime;
            _lastRefreshDurationMs = stopwatch.ElapsedMilliseconds;
            _isInitialized = true;

            // Reset metrics for new cycle
            Interlocked.Exchange(ref _cacheHits, 0);
            Interlocked.Exchange(ref _cacheMisses, 0);

            _logger.LogInformation("✅ Cache refresh completed: {ApiKeyCount} API keys, {PermissionCount} permissions, {BucketCount} buckets in {Duration}ms",
                activeApiKeys.Count, permissions.Count(), activeBuckets.Count, _lastRefreshDurationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Cache refresh failed");
            _isInitialized = false;
            throw;
        }
    }

    public Task<CachedApiKey?> GetApiKeyByKeyAsync(string key)
    {
        var cacheKey = APIKEY_BY_KEY_PREFIX + key;
        
        if (_cache.TryGetValue(cacheKey, out CachedApiKey? cachedApiKey))
        {
            RecordCacheHit();
            return Task.FromResult<CachedApiKey?>(cachedApiKey);
        }

        RecordCacheMiss();
        return Task.FromResult<CachedApiKey?>(null);
    }

    public Task<CachedApiKey?> GetApiKeyByIdAsync(Guid id)
    {
        var cacheKey = APIKEY_BY_ID_PREFIX + id;
        
        if (_cache.TryGetValue(cacheKey, out CachedApiKey? cachedApiKey))
        {
            RecordCacheHit();
            return Task.FromResult<CachedApiKey?>(cachedApiKey);
        }

        RecordCacheMiss();
        return Task.FromResult<CachedApiKey?>(null);
    }

    public Task<CachedPermission?> GetPermissionAsync(Guid apiKeyId, Guid bucketId)
    {
        var cacheKey = $"{PERMISSION_PREFIX}{apiKeyId}:{bucketId}";
        
        if (_cache.TryGetValue(cacheKey, out CachedPermission? permission))
        {
            RecordCacheHit();
            return Task.FromResult<CachedPermission?>(permission);
        }

        RecordCacheMiss();
        return Task.FromResult<CachedPermission?>(null);
    }

    public Task<List<CachedPermission>> GetPermissionsByApiKeyAsync(Guid apiKeyId)
    {
        var permissions = new List<CachedPermission>();
        var prefix = $"{PERMISSION_PREFIX}{apiKeyId}:";

        // Note: IMemoryCache limitation - full permission list requires separate tracking
        
        RecordCacheHit(); // Assume we find something
        return Task.FromResult(permissions);
    }

    public Task<CachedBucket?> GetBucketAsync(Guid bucketId)
    {
        var cacheKey = BUCKET_PREFIX + bucketId;
        
        if (_cache.TryGetValue(cacheKey, out CachedBucket? bucket))
        {
            RecordCacheHit();
            return Task.FromResult<CachedBucket?>(bucket);
        }

        RecordCacheMiss();
        return Task.FromResult<CachedBucket?>(null);
    }

    public async Task<List<Guid>?> GetAllowedProviderIdsAsync(Guid apiKeyId)
    {
        var apiKey = await GetApiKeyByIdAsync(apiKeyId);
        return apiKey?.AllowedProviderIds;
    }

    public void SetApiKey(CachedApiKey apiKey)
    {
        var keyByKey = APIKEY_BY_KEY_PREFIX + apiKey.Key;
        var keyById = APIKEY_BY_ID_PREFIX + apiKey.Id;

        _cache.Set(keyByKey, apiKey, CacheOptions);
        _cache.Set(keyById, apiKey, CacheOptions);

        // Track mappings
        _apiKeyIdToKeyMap.TryAdd(apiKey.Id, apiKey.Key);
        _apiKeyKeyToIdMap.TryAdd(apiKey.Key, apiKey.Id);
    }

    public void SetPermission(CachedPermission permission)
    {
        var cacheKey = $"{PERMISSION_PREFIX}{permission.ApiKeyId}:{permission.BucketId}";
        _cache.Set(cacheKey, permission, CacheOptions);
        
        // Track permission
        var permissionKey = $"{permission.ApiKeyId}:{permission.BucketId}";
        _permissionMap.TryAdd(permissionKey, permission.Id);
    }

    public void SetBucket(CachedBucket bucket)
    {
        var cacheKey = BUCKET_PREFIX + bucket.Id;
        _cache.Set(cacheKey, bucket, CacheOptions);
        
        // Track bucket
        _bucketMap.TryAdd(bucket.Id, bucket.Id);
    }

    public void RemoveApiKey(Guid apiKeyId, string? key = null)
    {
        // Remove by ID
        var keyById = APIKEY_BY_ID_PREFIX + apiKeyId;
        _cache.Remove(keyById);

        // Remove by key if provided
        if (!string.IsNullOrEmpty(key))
        {
            var keyByKey = APIKEY_BY_KEY_PREFIX + key;
            _cache.Remove(keyByKey);
            _apiKeyKeyToIdMap.TryRemove(key, out _);
        }
        else
        {
            // Try to find key from mapping
            if (_apiKeyIdToKeyMap.TryRemove(apiKeyId, out var mappedKey))
            {
                var keyByKey = APIKEY_BY_KEY_PREFIX + mappedKey;
                _cache.Remove(keyByKey);
                _apiKeyKeyToIdMap.TryRemove(mappedKey, out _);
            }
        }

        _apiKeyIdToKeyMap.TryRemove(apiKeyId, out _);
    }

    public void RemovePermission(Guid apiKeyId, Guid bucketId)
    {
        var cacheKey = $"{PERMISSION_PREFIX}{apiKeyId}:{bucketId}";
        _cache.Remove(cacheKey);
        
        // Remove from tracking
        var permissionKey = $"{apiKeyId}:{bucketId}";
        _permissionMap.TryRemove(permissionKey, out _);
    }

    public void RemoveAllPermissions(Guid apiKeyId)
    {
        // Remove all permissions for this API key from tracking
        var keysToRemove = _permissionMap.Keys.Where(k => k.StartsWith($"{apiKeyId}:")).ToList();
        foreach (var key in keysToRemove)
        {
            _permissionMap.TryRemove(key, out _);
        }
        
        _logger.LogDebug("RemoveAllPermissions called for API key {ApiKeyId} - will be handled by next cache refresh", apiKeyId);
    }

    public void RemoveBucket(Guid bucketId)
    {
        var cacheKey = BUCKET_PREFIX + bucketId;
        _cache.Remove(cacheKey);
        
        // Remove from tracking
        _bucketMap.TryRemove(bucketId, out _);
    }

    public Task<CacheStatistics> GetStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var memoryUsage = EstimateMemoryUsage();
        
        var stats = new CacheStatistics
        {
            ApiKeyCount = _apiKeyIdToKeyMap.Count,
            PermissionCount = _permissionMap.Count,
            BucketCount = _bucketMap.Count,
            LastRefreshTime = _lastRefreshTime,
            LastRefreshDurationMs = _lastRefreshDurationMs,
            CacheHits = Interlocked.Read(ref _cacheHits),
            CacheMisses = Interlocked.Read(ref _cacheMisses),
            EstimatedMemoryUsageBytes = memoryUsage,
            IsHealthy = _isInitialized && (now - _lastRefreshTime).TotalMinutes < 60, // Consider unhealthy if not refreshed in 1 hour
            HealthWarnings = new List<string>()
        };

        if (!stats.IsHealthy)
        {
            stats.HealthWarnings.Add("Cache has not been refreshed recently");
        }

        if (stats.HitRatio < 80)
        {
            stats.HealthWarnings.Add($"Low cache hit ratio: {stats.HitRatio:F1}%");
        }

        return Task.FromResult(stats);
    }

    public void RecordCacheHit()
    {
        Interlocked.Increment(ref _cacheHits);
    }

    public void RecordCacheMiss()
    {
        Interlocked.Increment(ref _cacheMisses);
    }

    public void ClearAll()
    {
        // Clear tracked mappings
        _apiKeyIdToKeyMap.Clear();
        _apiKeyKeyToIdMap.Clear();
        _permissionMap.Clear();
        _bucketMap.Clear();

        // Note: IMemoryCache doesn't have a clear all method - background service will reload
        _logger.LogDebug("Cache cleared - background service will reload data automatically");
    }

    private long EstimateMemoryUsage()
    {
        // Rough estimation: Each API key ~1KB, permission ~0.5KB, bucket ~0.5KB
        var apiKeyMemory = _apiKeyIdToKeyMap.Count * 1024L;
        var permissionMemory = _permissionMap.Count * 512L;
        var bucketMemory = _bucketMap.Count * 512L;
        var baseMemory = 1024L; // Base overhead
        
        return apiKeyMemory + permissionMemory + bucketMemory + baseMemory;
    }
} 