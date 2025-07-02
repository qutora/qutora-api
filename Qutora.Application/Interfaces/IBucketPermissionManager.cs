using Qutora.Domain.Entities;
using Qutora.Shared.Enums;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Bucket permissions management interface
/// </summary>
public interface IBucketPermissionManager
{
    /// <summary>
    /// Checks if the user has specific permission on a specific bucket
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, Guid bucketId, PermissionLevel requiredPermission);

    /// <summary>
    /// Checks the user's permissions on bucket with detailed result
    /// </summary>
    Task<PermissionCheckResult> CheckUserBucketOperationPermissionAsync(
        string userId, Guid bucketId, PermissionLevel requiredPermission);

    /// <summary>
    /// Gets all permission levels of the user on bucket
    /// </summary>
    Task<IEnumerable<PermissionLevel>> GetUserPermissionsForBucketAsync(string userId, Guid bucketId);

    /// <summary>
    /// Checks if the user has admin permission on bucket
    /// </summary>
    Task<bool> IsUserBucketAdminAsync(string userId, Guid bucketId);

    /// <summary>
    /// Checks if the API Key has specific permission on bucket
    /// </summary>
    Task<bool> HasApiKeyPermissionAsync(Guid apiKeyId, Guid bucketId, PermissionLevel requiredPermission);

    /// <summary>
    /// Checks the API Key's permissions on bucket with detailed result
    /// </summary>
    Task<PermissionCheckResult> CheckApiKeyBucketOperationPermissionAsync(
        Guid apiKeyId, Guid bucketId, PermissionLevel requiredPermission);

    /// <summary>
    /// Assigns permission
    /// </summary>
    Task<BucketPermission> AssignPermissionAsync(
        Guid bucketId,
        string subjectId,
        PermissionSubjectType subjectType,
        PermissionLevel permission,
        string? grantedBy = null);

    /// <summary>
    /// Assigns permission without using transaction
    /// </summary>
    Task<BucketPermission> AssignPermissionWithoutTransactionAsync(
        Guid bucketId,
        string subjectId,
        PermissionSubjectType subjectType,
        PermissionLevel permission,
        string? grantedBy = null);

    /// <summary>
    /// Removes permission assignment
    /// </summary>
    Task RemovePermissionAsync(Guid permissionId);

    /// <summary>
    /// Removes permission assignment without using transaction
    /// </summary>
    Task RemovePermissionWithoutTransactionAsync(Guid permissionId);

    /// <summary>
    /// Removes all permissions for a bucket
    /// </summary>
    Task RemoveAllPermissionsForBucketAsync(Guid bucketId);

    /// <summary>
    /// Assigns bucket permission for API Key
    /// </summary>
    Task<ApiKeyBucketPermission> AssignApiKeyPermissionAsync(
        Guid apiKeyId,
        Guid bucketId,
        PermissionLevel permission,
        string? grantedBy = null);

    /// <summary>
    /// Assigns bucket permission for API Key without using transaction
    /// </summary>
    Task<ApiKeyBucketPermission> AssignApiKeyPermissionWithoutTransactionAsync(
        Guid apiKeyId,
        Guid bucketId,
        PermissionLevel permission,
        string? grantedBy = null);

    /// <summary>
    /// Removes bucket permission for API Key
    /// </summary>
    Task RemoveApiKeyPermissionAsync(Guid permissionId);
}

/// <summary>
/// Permission check result
/// </summary>
public class PermissionCheckResult
{
    /// <summary>
    /// Is permission granted?
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Reason if permission is denied
    /// </summary>
    public string? DeniedReason { get; set; }

    /// <summary>
    /// Required permission level
    /// </summary>
    public PermissionLevel RequiredPermission { get; set; }

    /// <summary>
    /// User's current permission level
    /// </summary>
    public PermissionLevel UserPermission { get; set; }
}