using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;
using Qutora.Infrastructure.Caching.Events;
using Qutora.Infrastructure.Exceptions;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.UnitOfWork;
using Qutora.Shared.Enums;

namespace Qutora.Application.Services;

/// <summary>
/// Bucket permissions management class
/// </summary>
public class BucketPermissionManager(
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    CacheInvalidationService cacheInvalidationService,
    ILogger<BucketPermissionManager> logger)
    : IBucketPermissionManager
{
    /// <inheritdoc/>
    public async Task<bool> HasPermissionAsync(string userId, Guid bucketId, PermissionLevel requiredPermission)
    {
        var result = await CheckUserBucketOperationPermissionAsync(userId, bucketId, requiredPermission);
        return result.IsAllowed;
    }

    /// <inheritdoc/>
    public async Task<PermissionCheckResult> CheckUserBucketOperationPermissionAsync(
        string userId, Guid bucketId, PermissionLevel requiredPermission)
    {
        if (string.IsNullOrEmpty(userId))
            return new PermissionCheckResult
            {
                IsAllowed = false,
                DeniedReason = "Invalid user ID.",
                RequiredPermission = requiredPermission,
                UserPermission = PermissionLevel.None
            };

        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return new PermissionCheckResult
                {
                    IsAllowed = false,
                    DeniedReason = "User not found.",
                    RequiredPermission = requiredPermission,
                    UserPermission = PermissionLevel.None
                };

            var userClaims = await userManager.GetClaimsAsync(user);
            var hasAdminAccess = userClaims.Any(c => c.Type == "permissions" && c.Value == "Admin.Access");
            var hasBucketManage = userClaims.Any(c => c.Type == "permissions" && c.Value == "Bucket.Manage");
            
            if (hasAdminAccess || hasBucketManage)
                return new PermissionCheckResult
                {
                    IsAllowed = true,
                    RequiredPermission = requiredPermission,
                    UserPermission = PermissionLevel.Admin
                };

            var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(bucketId);
            if (bucket == null)
                return new PermissionCheckResult
                {
                    IsAllowed = false,
                    DeniedReason = "Bucket not found.",
                    RequiredPermission = requiredPermission,
                    UserPermission = PermissionLevel.None
                };

            var userPermission = await unitOfWork.BucketPermissions
                .GetUserPermissionForBucketAsync(userId, bucketId);

            if (userPermission != null && HasRequiredPermissionLevel(userPermission.Permission, requiredPermission))
                return new PermissionCheckResult
                {
                    IsAllowed = true,
                    RequiredPermission = requiredPermission,
                    UserPermission = userPermission.Permission
                };

            var userRoles = await userManager.GetRolesAsync(user);
            foreach (var roleName in userRoles)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var rolePermission = await unitOfWork.BucketPermissions
                        .GetRolePermissionForBucketAsync(role.Id, bucketId);

                    if (rolePermission != null &&
                        HasRequiredPermissionLevel(rolePermission.Permission, requiredPermission))
                        return new PermissionCheckResult
                        {
                            IsAllowed = true,
                            RequiredPermission = requiredPermission,
                            UserPermission = rolePermission.Permission
                        };
                }
            }

            return new PermissionCheckResult
            {
                IsAllowed = false,
                DeniedReason = "Insufficient permission level.",
                RequiredPermission = requiredPermission,
                UserPermission = PermissionLevel.None
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while getting user bucket permissions: {UserId}, {BucketId}",
                userId, bucketId);

            return new PermissionCheckResult
            {
                IsAllowed = false,
                DeniedReason = "An error occurred during permission check.",
                RequiredPermission = requiredPermission,
                UserPermission = PermissionLevel.None
            };
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PermissionLevel>> GetUserPermissionsForBucketAsync(string userId, Guid bucketId)
    {
        var permissions = new HashSet<PermissionLevel>();

        try
        {
            var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(bucketId);
            if (bucket == null) return permissions;

            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return permissions;

            var userClaims = await userManager.GetClaimsAsync(user);
            var hasAdminAccess = userClaims.Any(c => c.Type == "permissions" && c.Value == "Admin.Access");
            var hasBucketManage = userClaims.Any(c => c.Type == "permissions" && c.Value == "Bucket.Manage");
            
            if (hasAdminAccess || hasBucketManage)
            {
                permissions.Add(PermissionLevel.Admin);
                return permissions;
            }

            var userPermission = await unitOfWork.BucketPermissions
                .GetUserPermissionForBucketAsync(userId, bucketId);

            if (userPermission != null) permissions.Add(userPermission.Permission);

            var userRoles = await userManager.GetRolesAsync(user);
            foreach (var roleName in userRoles)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var rolePermission = await unitOfWork.BucketPermissions
                        .GetRolePermissionForBucketAsync(role.Id, bucketId);

                    if (rolePermission != null) permissions.Add(rolePermission.Permission);
                }
            }

            return permissions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while getting user bucket permissions: {UserId}, {BucketId}",
                userId, bucketId);
            return permissions;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsUserBucketAdminAsync(string userId, Guid bucketId)
    {
        return await HasPermissionAsync(userId, bucketId, PermissionLevel.Admin);
    }

    /// <inheritdoc/>
    public async Task<bool> HasApiKeyPermissionAsync(Guid apiKeyId, Guid bucketId, PermissionLevel requiredPermission)
    {
        var result = await CheckApiKeyBucketOperationPermissionAsync(apiKeyId, bucketId, requiredPermission);
        return result.IsAllowed;
    }

    /// <inheritdoc/>
    public async Task<PermissionCheckResult> CheckApiKeyBucketOperationPermissionAsync(
        Guid apiKeyId, Guid bucketId, PermissionLevel requiredPermission)
    {
        try
        {
            var apiKey = await unitOfWork.ApiKeys.GetByIdAsync(apiKeyId);
            if (apiKey == null || !apiKey.IsActive || apiKey.IsDeleted)
                return new PermissionCheckResult
                {
                    IsAllowed = false,
                    DeniedReason = "API Key not found or inactive.",
                    RequiredPermission = requiredPermission,
                    UserPermission = PermissionLevel.None
                };

            if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
                return new PermissionCheckResult
                {
                    IsAllowed = false,
                    DeniedReason = "API Key has expired.",
                    RequiredPermission = requiredPermission,
                    UserPermission = PermissionLevel.None
                };

            var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(bucketId);
            if (bucket == null)
                return new PermissionCheckResult
                {
                    IsAllowed = false,
                    DeniedReason = "Bucket not found.",
                    RequiredPermission = requiredPermission,
                    UserPermission = PermissionLevel.None
                };

            if (!apiKey.AllowedProviderIds.Contains(bucket.ProviderId))
                return new PermissionCheckResult
                {
                    IsAllowed = false,
                    DeniedReason = "This API Key does not have access permission to the provider associated with the bucket.",
                    RequiredPermission = requiredPermission,
                    UserPermission = PermissionLevel.None
                };

            var bucketPermission = await unitOfWork.ApiKeyBucketPermissions
                .GetApiKeyPermissionForBucketAsync(apiKeyId, bucketId);

            if (bucketPermission != null)
            {
                if (HasRequiredPermissionLevel(bucketPermission.Permission, requiredPermission))
                    return new PermissionCheckResult
                    {
                        IsAllowed = true,
                        RequiredPermission = requiredPermission,
                        UserPermission = bucketPermission.Permission
                    };
                else
                    return new PermissionCheckResult
                    {
                        IsAllowed = false,
                        DeniedReason = "API Key does not have sufficient permission on this bucket.",
                        RequiredPermission = requiredPermission,
                        UserPermission = bucketPermission.Permission
                    };
            }

            if (HasRequiredPermissionLevel((PermissionLevel)apiKey.Permissions, requiredPermission))
                return new PermissionCheckResult
                {
                    IsAllowed = true,
                    RequiredPermission = requiredPermission,
                    UserPermission = (PermissionLevel)apiKey.Permissions
                };

            return new PermissionCheckResult
            {
                IsAllowed = false,
                DeniedReason = "API Key does not have sufficient permission for this operation.",
                RequiredPermission = requiredPermission,
                UserPermission = (PermissionLevel)apiKey.Permissions
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during API Key permission check: {ApiKeyId}, {BucketId}, {RequiredPermission}",
                apiKeyId, bucketId, requiredPermission);

            return new PermissionCheckResult
            {
                IsAllowed = false,
                DeniedReason = "An error occurred during permission check.",
                RequiredPermission = requiredPermission,
                UserPermission = PermissionLevel.None
            };
        }
    }

    /// <summary>
    /// Assigns permission
    /// </summary>
    /// <param name="bucketId">Bucket ID</param>
    /// <param name="subjectId">Subject ID (user or role)</param>
    /// <param name="subjectType">Subject type</param>
    /// <param name="permission">Permission level</param>
    /// <param name="grantedBy">ID of user granting the permission</param>
    /// <returns>Created permission</returns>
    public async Task<BucketPermission> AssignPermissionAsync(
        Guid bucketId,
        string subjectId,
        PermissionSubjectType subjectType,
        PermissionLevel permission,
        string? grantedBy = null)
    {
        try
        {
            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                return await AssignPermissionWithoutTransactionAsync(bucketId, subjectId, subjectType, permission,
                    grantedBy);
            });
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex,
                "Concurrency error occurred during permission assignment: {BucketId}, {SubjectId}, {SubjectType}",
                bucketId, subjectId, subjectType);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during permission assignment: {BucketId}, {SubjectId}, {SubjectType}",
                bucketId, subjectId, subjectType);
            throw;
        }
    }

    /// <summary>
    /// Assigns permission without using transaction
    /// </summary>
    /// <param name="bucketId">Bucket ID</param>
    /// <param name="subjectId">Subject ID (user or role)</param>
    /// <param name="subjectType">Subject type</param>
    /// <param name="permission">Permission level</param>
    /// <param name="grantedBy">ID of user granting the permission</param>
    /// <returns>Created permission</returns>
    public async Task<BucketPermission> AssignPermissionWithoutTransactionAsync(
        Guid bucketId,
        string subjectId,
        PermissionSubjectType subjectType,
        PermissionLevel permission,
        string? grantedBy = null)
    {
        BucketPermission? existingPermission = null;

        if (subjectType == PermissionSubjectType.User)
            existingPermission = await unitOfWork.BucketPermissions
                .GetUserPermissionForBucketAsync(subjectId, bucketId);
        else
            existingPermission = await unitOfWork.BucketPermissions
                .GetRolePermissionForBucketAsync(subjectId, bucketId);

        if (existingPermission != null)
        {
            existingPermission.Permission = permission;
            existingPermission.CreatedBy = grantedBy;

            await unitOfWork.BucketPermissions.UpdateAsync(existingPermission);

            return existingPermission;
        }

        var newPermission = new BucketPermission
        {
            BucketId = bucketId,
            SubjectId = subjectId,
            SubjectType = subjectType,
            Permission = permission,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = grantedBy
        };

        await unitOfWork.BucketPermissions.AddAsync(newPermission);

        return newPermission;
    }

    /// <summary>
    /// Removes permission assignment
    /// </summary>
    public async Task RemovePermissionAsync(Guid permissionId)
    {
        try
        {
            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await RemovePermissionWithoutTransactionAsync(permissionId);
                return true;
            });
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred during permission removal: {PermissionId}",
                permissionId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during permission removal: {PermissionId}",
                permissionId);
            throw;
        }
    }

    /// <summary>
    /// Removes permission assignment without using transaction
    /// </summary>
    public async Task RemovePermissionWithoutTransactionAsync(Guid permissionId)
    {
        var permission = await unitOfWork.BucketPermissions.GetByIdAsync(permissionId);
        if (permission != null) unitOfWork.BucketPermissions.Remove(permission);
    }

    /// <summary>
    /// Removes all permissions for a bucket
    /// </summary>
    public async Task RemoveAllPermissionsForBucketAsync(Guid bucketId)
    {
        try
        {
            var permissions = await unitOfWork.BucketPermissions.GetPermissionsByBucketIdAsync(bucketId);

            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                foreach (var permission in permissions) unitOfWork.BucketPermissions.Remove(permission);

                return true;
            });
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex,
                "Concurrency error occurred while removing all permissions for bucket: {BucketId}",
                bucketId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while removing all permissions for bucket: {BucketId}",
                bucketId);
            throw;
        }
    }

    /// <summary>
    /// Assigns bucket permission for API Key
    /// </summary>
    public async Task<ApiKeyBucketPermission> AssignApiKeyPermissionAsync(
        Guid apiKeyId,
        Guid bucketId,
        PermissionLevel permission,
        string? grantedBy = null)
    {
        try
        {
            var result = await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                return await AssignApiKeyPermissionWithoutTransactionAsync(apiKeyId, bucketId, permission,
                    grantedBy);
            });

            // Trigger cache invalidation
            await cacheInvalidationService.OnBucketPermissionCreatedAsync(apiKeyId, bucketId);

            return result;
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex,
                "Concurrency error occurred during API Key permission assignment: {ApiKeyId}, {BucketId}",
                apiKeyId, bucketId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during API Key permission assignment: {ApiKeyId}, {BucketId}",
                apiKeyId, bucketId);
            throw;
        }
    }

    /// <summary>
    /// Assigns bucket permission for API Key without using transaction
    /// </summary>
    public async Task<ApiKeyBucketPermission> AssignApiKeyPermissionWithoutTransactionAsync(
        Guid apiKeyId,
        Guid bucketId,
        PermissionLevel permission,
        string? grantedBy = null)
    {
        var existingPermission = await unitOfWork.ApiKeyBucketPermissions
            .GetApiKeyPermissionForBucketAsync(apiKeyId, bucketId);

        if (existingPermission != null)
        {
            existingPermission.Permission = permission;
            existingPermission.CreatedBy = grantedBy;

            await unitOfWork.ApiKeyBucketPermissions.UpdateAsync(existingPermission);

            return existingPermission;
        }

        var newPermission = new ApiKeyBucketPermission
        {
            ApiKeyId = apiKeyId,
            BucketId = bucketId,
            Permission = permission,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = grantedBy
        };

        await unitOfWork.ApiKeyBucketPermissions.AddAsync(newPermission);

        return newPermission;
    }

    /// <summary>
    /// Removes bucket permission for API Key
    /// </summary>
    public async Task RemoveApiKeyPermissionAsync(Guid permissionId)
    {
        try
        {
            // Get permission details before deletion for cache invalidation
            var permission = await unitOfWork.ApiKeyBucketPermissions.GetByIdAsync(permissionId);
            
            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                if (permission != null) unitOfWork.ApiKeyBucketPermissions.Remove(permission);

                return true;
            });

            // Trigger cache invalidation
            if (permission != null)
            {
                await cacheInvalidationService.OnBucketPermissionDeletedAsync(permission.ApiKeyId, permission.BucketId);
            }
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex,
                "Concurrency error occurred while removing API Key permission: {PermissionId}",
                permissionId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while removing API Key permission: {PermissionId}",
                permissionId);
            throw;
        }
    }

    /// <summary>
    /// Checks whether the given permission level meets the required permission level
    /// </summary>
    private bool HasRequiredPermissionLevel(PermissionLevel actualPermission, PermissionLevel requiredPermission)
    {
        switch (actualPermission)
        {
            case PermissionLevel.Admin:
            case PermissionLevel.Delete when
                (requiredPermission == PermissionLevel.Read ||
                 requiredPermission == PermissionLevel.Write ||
                 requiredPermission == PermissionLevel.ReadWrite):
            case PermissionLevel.ReadWrite when
                (requiredPermission == PermissionLevel.Read ||
                 requiredPermission == PermissionLevel.Write):
                return true;
            default:
                return actualPermission == requiredPermission;
        }
    }
}
