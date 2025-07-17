using System.Collections.Concurrent;
using Qutora.Shared.Enums;

namespace Qutora.Infrastructure.Storage;

/// <summary>
/// Class that caches storage provider capabilities
/// </summary>
public class StorageCapabilityCache : IStorageCapabilityCache
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<StorageCapability, bool>> _cache = new();

    public StorageCapabilityCache()
    {
    }

    /// <summary>
    /// Creates cache key
    /// </summary>
    public string CreateCacheKey(string providerId)
    {
        return $"capability_{providerId}";
    }

    /// <summary>
    /// Checks if capability is supported from cache
    /// </summary>
    public bool? GetCachedCapability(string cacheKey, StorageCapability capability)
    {
        if (_cache.TryGetValue(cacheKey, out var capabilities) &&
            capabilities.TryGetValue(capability, out var isSupported))
            return isSupported;
        return null;
    }

    /// <summary>
    /// Adds capability to cache
    /// </summary>
    public void SetCachedCapability(string cacheKey, StorageCapability capability, bool isSupported)
    {
        var capabilities = _cache.GetOrAdd(cacheKey, _ => new ConcurrentDictionary<StorageCapability, bool>());
        capabilities[capability] = isSupported;
    }

    /// <summary>
    /// Clears cache for a specific provider
    /// </summary>
    public void ClearCache(string cacheKey)
    {
        _cache.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// Clears all cache
    /// </summary>
    public void ClearAllCache()
    {
        _cache.Clear();
    }
}
