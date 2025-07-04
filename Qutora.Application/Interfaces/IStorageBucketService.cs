using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for managing bucket/folder operations
/// </summary>
public interface IStorageBucketService
{
    /// <summary>
    /// Lists buckets/folders in a specific storage provider
    /// </summary>
    /// <param name="providerId">Storage provider ID</param>
    /// <returns>List of bucket information</returns>
    Task<IEnumerable<BucketInfoDto>> ListProviderBucketsAsync(string providerId);

    /// <summary>
    /// Gets buckets that a user has access to in a specific provider
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="providerId">Storage provider ID</param>
    /// <returns>List of bucket information that user has access to</returns>
    Task<IEnumerable<BucketInfoDto>> GetUserAccessibleBucketsForProviderAsync(string userId, string providerId);

    /// <summary>
    /// Gets the default bucket for a specific provider
    /// </summary>
    /// <param name="providerId">Storage provider ID</param>
    /// <returns>Default bucket information or null</returns>
    Task<BucketInfoDto?> GetDefaultBucketForProviderAsync(string providerId);

    /// <summary>
    /// Checks if a bucket/folder exists
    /// </summary>
    /// <param name="providerId">Storage provider ID</param>
    /// <param name="bucketPath">Bucket/folder path</param>
    /// <returns>True if bucket/folder exists, false otherwise</returns>
    Task<bool> BucketExistsAsync(string providerId, string bucketPath);

    /// <summary>
    /// Gets bucket information for a specific bucket ID
    /// </summary>
    /// <param name="bucketId">Bucket ID</param>
    /// <returns>Bucket entity</returns>
    Task<StorageBucket> GetBucketByIdAsync(Guid bucketId);

    /// <summary>
    /// Deletes a bucket/folder
    /// </summary>
    /// <param name="providerId">Storage provider ID</param>
    /// <param name="bucketPath">Bucket/folder path</param>
    /// <param name="force">Force delete even if bucket has content</param>
    /// <returns>True if operation succeeds, false otherwise</returns>
    Task<bool> RemoveBucketAsync(string providerId, string bucketPath, bool force = false);

    /// <summary>
    /// Gets bucket permissions
    /// </summary>
    /// <param name="bucketId">Bucket ID</param>
    /// <returns>List of bucket permissions</returns>
    Task<IEnumerable<BucketPermissionDto>> GetBucketPermissionsAsync(Guid bucketId);

    /// <summary>
    /// Gets user's bucket permissions in paginated format
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <returns>Paginated bucket permissions result object</returns>
    Task<PagedDto<BucketPermissionDto>> GetUserBucketPermissionsPaginatedAsync(string userId, int page, int pageSize);

    /// <summary>
    /// Gets all bucket permissions including both user and role permissions (paginated) - Admin only
    /// </summary>
    Task<PagedDto<BucketPermissionDto>> GetAllBucketPermissionsPaginatedAsync(int page, int pageSize);

    /// <summary>
    /// Gets paginated bucket list
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <returns>Paginated bucket list</returns>
    Task<IEnumerable<StorageBucket>> GetPaginatedBucketsAsync(int page, int pageSize);

    /// <summary>
    /// Gets user's accessible buckets in paginated format
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <returns>Paginated bucket list</returns>
    Task<PagedDto<BucketInfoDto>> GetUserAccessiblePaginatedBucketsAsync(string userId, int page, int pageSize);

    /// <summary>
    /// Gets bucket ID by provider ID and bucket path
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="bucketPath">Bucket path</param>
    /// <returns>Bucket ID or null</returns>
    Task<Guid?> GetBucketIdByProviderAndPathAsync(string providerId, string bucketPath);

    /// <summary>
    /// Creates a new bucket (detailed)
    /// </summary>
    /// <param name="dto">Bucket creation DTO</param>
    /// <param name="userId">Creating user ID</param>
    /// <returns>Created bucket entity</returns>
    Task<StorageBucket> CreateBucketAsync(BucketCreateDto dto, string userId);

    /// <summary>
    /// Gets bucket path by bucket ID
    /// </summary>
    /// <param name="bucketId">Bucket ID</param>
    /// <returns>Bucket path or null</returns>
    Task<string?> GetBucketPathByIdAsync(Guid bucketId);

    /// <summary>
    /// Gets bucket ID by bucket path
    /// </summary>
    /// <param name="bucketPath">Bucket path</param>
    /// <returns>Bucket ID or null</returns>
    Task<Guid?> GetBucketIdByPathAsync(string bucketPath);

    /// <summary>
    /// Checks if bucket has any documents
    /// </summary>
    /// <param name="bucketId">Bucket ID</param>
    /// <returns>True if bucket has documents, false otherwise</returns>
    Task<bool> HasDocumentsAsync(Guid bucketId);
}