using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for storage bucket operations
/// </summary>
public interface IStorageBucketRepository : IRepository<StorageBucket>
{
    /// <summary>
    /// Gets bucket list by storage provider
    /// </summary>
    Task<IEnumerable<StorageBucket>> GetBucketsByProviderIdAsync(Guid providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns default bucket information by provider ID
    /// </summary>
    Task<StorageBucket?> GetDefaultBucketForProviderAsync(Guid providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns bucket information by provider and bucket path
    /// </summary>
    Task<StorageBucket?> GetBucketByPathAndProviderAsync(string bucketPath, Guid providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets buckets that a user has access to
    /// </summary>
    Task<IEnumerable<StorageBucket>> GetUserAccessibleBucketsAsync(string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets buckets that a role has access to
    /// </summary>
    Task<IEnumerable<StorageBucket>> GetRoleAccessibleBucketsAsync(string roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns paginated bucket list
    /// </summary>
    Task<(IEnumerable<StorageBucket> Items, int TotalCount)> GetPaginatedBucketsAsync(
        int page, int pageSize, string? searchTerm = null, Guid? providerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets buckets by user ID
    /// </summary>
    Task<IEnumerable<StorageBucket>> GetByCreatorIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets buckets by provider ID
    /// </summary>
    Task<IEnumerable<StorageBucket>> GetByProviderIdAsync(Guid providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets buckets paginated
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of buckets</returns>
    Task<IEnumerable<StorageBucket>> GetPaginatedAsync(int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bucket by provider ID and bucket path
    /// </summary>
    /// <param name="providerId">Storage Provider ID</param>
    /// <param name="bucketPath">Bucket path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Found bucket or null</returns>
    Task<StorageBucket?> GetByProviderAndPathAsync(string providerId, string bucketPath,
        CancellationToken cancellationToken = default);
}