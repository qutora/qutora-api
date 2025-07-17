using Qutora.Shared.Enums;

namespace Qutora.Infrastructure.Storage;

/// <summary>
/// Interface for caching storage provider capabilities
/// </summary>
public interface IStorageCapabilityCache
{
    /// <summary>
    /// Creates cache key
    /// </summary>
    string CreateCacheKey(string providerId);

    /// <summary>
    /// Checks if capability is supported from cache
    /// </summary>
    bool? GetCachedCapability(string cacheKey, StorageCapability capability);

    /// <summary>
    /// Adds capability to cache
    /// </summary>
    void SetCachedCapability(string cacheKey, StorageCapability capability, bool isSupported);

    /// <summary>
    /// Clears cache for a specific provider
    /// </summary>
    void ClearCache(string cacheKey);

    /// <summary>
    /// Clears all cache
    /// </summary>
    void ClearAllCache();
}