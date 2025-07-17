namespace Qutora.Application.Interfaces;

public interface ICacheInvalidationService
{
    /// <summary>
    /// Handles API key creation - adds to cache
    /// </summary>
    Task OnApiKeyCreatedAsync(Guid apiKeyId);

    /// <summary>
    /// Handles API key updates - updates cache entry
    /// </summary>
    Task OnApiKeyUpdatedAsync(Guid apiKeyId);

    /// <summary>
    /// Handles API key deletion - removes from cache
    /// </summary>
    Task OnApiKeyDeletedAsync(Guid apiKeyId, string? key = null);

    /// <summary>
    /// Handles bucket permission creation
    /// </summary>
    Task OnBucketPermissionCreatedAsync(Guid apiKeyId, Guid bucketId);

    /// <summary>
    /// Handles bucket permission updates
    /// </summary>
    Task OnBucketPermissionUpdatedAsync(Guid apiKeyId, Guid bucketId);

    /// <summary>
    /// Handles bucket permission deletion
    /// </summary>
    Task OnBucketPermissionDeletedAsync(Guid apiKeyId, Guid bucketId);

    /// <summary>
    /// Handles storage bucket changes
    /// </summary>
    Task OnStorageBucketChangedAsync(Guid bucketId, string changeType);

    /// <summary>
    /// Forces a full cache refresh
    /// </summary>
    Task ForceRefreshAsync(string reason = "Manual refresh");

    /// <summary>
    /// Handles batch operations that might affect multiple cache entries
    /// </summary>
    Task OnBatchOperationAsync(string operationType, int affectedCount);

    /// <summary>
    /// Handles system startup - ensures cache is loaded
    /// </summary>
    Task OnSystemStartupAsync();

    /// <summary>
    /// Gets current cache health status
    /// </summary>
    Task<bool> IsHealthyAsync();
}