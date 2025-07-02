using Qutora.Infrastructure.Caching.Models;

namespace Qutora.Infrastructure.Caching.Services;

/// <summary>
/// Interface for API Key caching operations
/// </summary>
public interface IApiKeyCacheService
{
    /// <summary>
    /// Loads all API keys, permissions, and buckets from database into cache
    /// </summary>
    Task LoadAllDataAsync();

    /// <summary>
    /// Gets a cached API key by its public key string
    /// </summary>
    Task<CachedApiKey?> GetApiKeyByKeyAsync(string key);

    /// <summary>
    /// Gets a cached API key by its unique identifier
    /// </summary>
    Task<CachedApiKey?> GetApiKeyByIdAsync(Guid id);

    /// <summary>
    /// Gets a cached permission for specific API key and bucket
    /// </summary>
    Task<CachedPermission?> GetPermissionAsync(Guid apiKeyId, Guid bucketId);

    /// <summary>
    /// Gets all permissions for a specific API key
    /// </summary>
    Task<List<CachedPermission>> GetPermissionsByApiKeyAsync(Guid apiKeyId);

    /// <summary>
    /// Gets a cached bucket by its identifier
    /// </summary>
    Task<CachedBucket?> GetBucketAsync(Guid bucketId);

    /// <summary>
    /// Gets allowed provider IDs for an API key
    /// </summary>
    Task<List<Guid>?> GetAllowedProviderIdsAsync(Guid apiKeyId);

    /// <summary>
    /// Sets/updates a cached API key
    /// </summary>
    void SetApiKey(CachedApiKey apiKey);

    /// <summary>
    /// Sets/updates a cached permission
    /// </summary>
    void SetPermission(CachedPermission permission);

    /// <summary>
    /// Sets/updates a cached bucket
    /// </summary>
    void SetBucket(CachedBucket bucket);

    /// <summary>
    /// Removes an API key from cache
    /// </summary>
    void RemoveApiKey(Guid apiKeyId, string? key = null);

    /// <summary>
    /// Removes a specific permission from cache
    /// </summary>
    void RemovePermission(Guid apiKeyId, Guid bucketId);

    /// <summary>
    /// Removes all permissions for an API key
    /// </summary>
    void RemoveAllPermissions(Guid apiKeyId);

    /// <summary>
    /// Removes a bucket from cache
    /// </summary>
    void RemoveBucket(Guid bucketId);

    /// <summary>
    /// Gets cache statistics and health information
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync();

    /// <summary>
    /// Checks if cache is properly initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Records a cache hit for metrics
    /// </summary>
    void RecordCacheHit();

    /// <summary>
    /// Records a cache miss for metrics
    /// </summary>
    void RecordCacheMiss();

    /// <summary>
    /// Clears all cached data
    /// </summary>
    void ClearAll();
} 