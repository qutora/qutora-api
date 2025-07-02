using Qutora.Domain.Entities;

namespace Qutora.Infrastructure.Interfaces.Repositories;

/// <summary>
/// Repository interface for API Key bucket permissions operations
/// </summary>
public interface IApiKeyBucketPermissionRepository : IRepository<ApiKeyBucketPermission>
{
    /// <summary>
    /// Gets bucket permissions by API Key ID
    /// </summary>
    Task<IEnumerable<ApiKeyBucketPermission>> GetPermissionsByApiKeyIdAsync(Guid apiKeyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets API Key permissions by bucket ID
    /// </summary>
    Task<IEnumerable<ApiKeyBucketPermission>> GetPermissionsByBucketIdAsync(Guid bucketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions of a specific API Key for a specific bucket
    /// </summary>
    Task<ApiKeyBucketPermission?> GetApiKeyPermissionForBucketAsync(Guid apiKeyId, Guid bucketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all bucket permissions for an API Key
    /// </summary>
    Task RemoveAllPermissionsForApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all API Key permissions for a bucket
    /// </summary>
    Task RemoveAllPermissionsForBucketAsync(Guid bucketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bucket permissions for a specific API Key
    /// </summary>
    /// <param name="apiKeyId">API Key ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bucket permissions for the API Key</returns>
    Task<IEnumerable<ApiKeyBucketPermission>> GetByApiKeyIdAsync(Guid apiKeyId,
        CancellationToken cancellationToken = default);
}