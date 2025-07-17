using Qutora.Domain.Entities;
using Qutora.Shared.Enums;

namespace Qutora.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for bucket permissions operations
/// </summary>
public interface IBucketPermissionRepository : IRepository<BucketPermission>
{
    /// <summary>
    /// Gets permissions by bucket ID
    /// </summary>
    Task<IEnumerable<BucketPermission>> GetPermissionsByBucketIdAsync(Guid bucketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bucket permissions owned by a user
    /// </summary>
    Task<IEnumerable<BucketPermission>> GetUserPermissionsAsync(string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user permissions paginated
    /// </summary>
    Task<IEnumerable<BucketPermission>> GetUserPermissionsPaginatedAsync(string userId, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions including both user and role permissions (paginated)
    /// </summary>
    Task<IEnumerable<BucketPermission>> GetAllPermissionsPaginatedAsync(int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns total permission count for a specific user
    /// </summary>
    Task<int> CountUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all permissions including both user and role permissions
    /// </summary>
    Task<int> CountAllPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's permissions for a specific bucket
    /// </summary>
    Task<BucketPermission?> GetUserPermissionForBucketAsync(string userId, Guid bucketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bucket permissions owned by a role
    /// </summary>
    Task<IEnumerable<BucketPermission>> GetRolePermissionsAsync(string roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role's permissions for a specific bucket
    /// </summary>
    Task<BucketPermission?> GetRolePermissionForBucketAsync(string roleId, Guid bucketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all permissions for a user or role on a specific bucket
    /// </summary>
    Task RemoveAllPermissionsForBucketAsync(Guid bucketId, string subjectId, PermissionSubjectType subjectType,
        CancellationToken cancellationToken = default);
}