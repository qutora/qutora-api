using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Caching.Services;
using Qutora.Infrastructure.Interfaces;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.DocumentOrchestration;
using Qutora.Shared.Enums;

namespace Qutora.Application.Services;

/// <summary>
/// Document authorization service implementation
/// </summary>
public class DocumentAuthorizationService(
    IAuthorizationService authorizationService,
    IBucketPermissionManager permissionManager,
    IStorageBucketService bucketService,
    IStorageProviderService storageProviderService,
    IDocumentService documentService,
    IHttpContextAccessor httpContextAccessor,
    IApiKeyCacheService apiKeyCacheService,
    ILogger<DocumentAuthorizationService> logger)
    : IDocumentAuthorizationService
{
    /// <summary>
    /// Check API Key bucket permission using cache
    /// </summary>
    private async Task<bool> CheckApiKeyBucketPermissionFromCacheAsync(Guid apiKeyId, Guid bucketId, PermissionLevel requiredPermission)
    {
        try
        {
            // First check if cache is initialized
            if (!apiKeyCacheService.IsInitialized)
            {
                logger.LogWarning("API Key cache not initialized, falling back to database check");
                var dbResult = await permissionManager.CheckApiKeyBucketOperationPermissionAsync(apiKeyId, bucketId, requiredPermission);
                return dbResult.IsAllowed;
            }

            // Get API Key from cache
            var cachedApiKey = await apiKeyCacheService.GetApiKeyByIdAsync(apiKeyId);
            if (cachedApiKey == null || !cachedApiKey.IsActive)
            {
                logger.LogDebug("API Key {ApiKeyId} not found in cache or inactive", apiKeyId);
                return false;
            }

            // Check if API Key has expired
            if (cachedApiKey.ExpiresAt.HasValue && cachedApiKey.ExpiresAt.Value < DateTime.UtcNow)
            {
                logger.LogDebug("API Key {ApiKeyId} has expired", apiKeyId);
                return false;
            }

            // Get bucket from cache
            var cachedBucket = await apiKeyCacheService.GetBucketAsync(bucketId);
            if (cachedBucket == null || !cachedBucket.IsActive)
            {
                logger.LogDebug("Bucket {BucketId} not found in cache or inactive", bucketId);
                return false;
            }

            // Check if API Key has access to the provider
            if (!cachedApiKey.AllowedProviderIds.Contains(cachedBucket.ProviderId))
            {
                logger.LogDebug("API Key {ApiKeyId} does not have access to provider {ProviderId}", apiKeyId, cachedBucket.ProviderId);
                return false;
            }

            // Check specific bucket permission from cache
            var cachedPermission = await apiKeyCacheService.GetPermissionAsync(apiKeyId, bucketId);
            if (cachedPermission != null)
            {
                var hasPermission = HasRequiredPermissionLevel(cachedPermission.Permission, requiredPermission);
                logger.LogDebug("API Key {ApiKeyId} bucket permission check: {HasPermission} (Required: {RequiredPermission}, Actual: {ActualPermission})", 
                    apiKeyId, hasPermission, requiredPermission, cachedPermission.Permission);
                return hasPermission;
            }

            // Check general API Key permissions
            var hasGeneralPermission = HasRequiredPermissionLevel((PermissionLevel)cachedApiKey.Permissions, requiredPermission);
            logger.LogDebug("API Key {ApiKeyId} general permission check: {HasPermission} (Required: {RequiredPermission}, Actual: {ActualPermission})", 
                apiKeyId, hasGeneralPermission, requiredPermission, (PermissionLevel)cachedApiKey.Permissions);
            
            return hasGeneralPermission;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking API Key permission from cache, falling back to database");
            var dbResult = await permissionManager.CheckApiKeyBucketOperationPermissionAsync(apiKeyId, bucketId, requiredPermission);
            return dbResult.IsAllowed;
        }
    }

    /// <summary>
    /// Check if actual permission level meets the required permission level
    /// </summary>
    private static bool HasRequiredPermissionLevel(PermissionLevel actualPermission, PermissionLevel requiredPermission)
    {
        return actualPermission >= requiredPermission;
    }

    public async Task<DocumentAuthorizationResult> CanCreateDocumentAsync(string userId, Guid? providerId, Guid? bucketId)
    {
        try
        {
            // Admin access check
            var user = httpContextAccessor.HttpContext?.User;
            var hasAdminAccess = user != null ? await authorizationService.AuthorizeAsync(user, "Admin.Access") : Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed();
            var hasDocumentAdmin = user != null ? await authorizationService.AuthorizeAsync(user, "Document.Admin") : Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed();
            
            if (hasAdminAccess.Succeeded || hasDocumentAdmin.Succeeded)
            {
                return DocumentAuthorizationResult.Success();
            }

            // Check if this is API Key authentication
            var apiKeyIdClaim = user?.FindFirst("ApiKeyId");
            if (apiKeyIdClaim != null && Guid.TryParse(apiKeyIdClaim.Value, out var apiKeyId))
            {
                // Use API Key permission system
                if (bucketId.HasValue)
                {
                    var apiKeyPermissionCheck = await CheckApiKeyBucketPermissionFromCacheAsync(apiKeyId, bucketId.Value, PermissionLevel.Write);

                    if (!apiKeyPermissionCheck)
                    {
                        return DocumentAuthorizationResult.Failure("You don't have write permission to this bucket.");
                    }
                }
                
                return DocumentAuthorizationResult.Success();
            }

            // Provider permission validation (for user-based auth)
            if (providerId.HasValue)
            {
                var userAccessibleBuckets = await bucketService.GetUserAccessibleBucketsForProviderAsync(userId, providerId.Value.ToString());
                if (!userAccessibleBuckets.Any())
                {
                    return DocumentAuthorizationResult.Failure("You don't have permission to use this storage provider.");
                }
            }

            // Bucket permission validation (for user-based auth)
            if (bucketId.HasValue)
            {
                var permissionCheck = await permissionManager.CheckUserBucketOperationPermissionAsync(
                    userId, bucketId.Value, PermissionLevel.Write);

                if (!permissionCheck.IsAllowed)
                {
                    return DocumentAuthorizationResult.Failure("You don't have write permission to this bucket.");
                }
            }

            return DocumentAuthorizationResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking create document authorization for user {UserId}", userId);
            return DocumentAuthorizationResult.Failure("Authorization check failed due to an internal error.");
        }
    }

    public async Task<DocumentAuthorizationResult> CanAccessDocumentAsync(string userId, DocumentDto document)
    {
        try
        {
            // Admin access check
            var user = httpContextAccessor.HttpContext?.User;
            var hasAdminAccess = user != null ? await authorizationService.AuthorizeAsync(user, "Admin.Access") : Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed();
            var hasDocumentAdmin = user != null ? await authorizationService.AuthorizeAsync(user, "Document.Admin") : Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed();
            
            if (hasAdminAccess.Succeeded || hasDocumentAdmin.Succeeded)
            {
                return DocumentAuthorizationResult.Success();
            }

            // Check if this is API Key authentication
            var apiKeyIdClaim = user?.FindFirst("ApiKeyId");
            if (apiKeyIdClaim != null && Guid.TryParse(apiKeyIdClaim.Value, out var apiKeyId))
            {
                // Use API Key permission system
                if (document.BucketId.HasValue)
                {
                    var apiKeyPermissionCheck = await CheckApiKeyBucketPermissionFromCacheAsync(apiKeyId, document.BucketId.Value, PermissionLevel.Read);
                    
                    if (apiKeyPermissionCheck)
                    {
                        return DocumentAuthorizationResult.Success();
                    }
                }
                else
                {
                    // Document has no bucket assigned, check if API Key has general read permission
                    // This handles documents uploaded without specific bucket assignment
                    logger.LogDebug("Document {DocumentId} has no bucket assigned, checking general API Key permissions", document.Id);
                    
                    // Get API Key from cache to check general permissions
                    var cachedApiKey = await apiKeyCacheService.GetApiKeyByIdAsync(apiKeyId);
                    if (cachedApiKey != null && cachedApiKey.IsActive)
                    {
                        var hasReadPermission = HasRequiredPermissionLevel((PermissionLevel)cachedApiKey.Permissions, PermissionLevel.Read);
                        if (hasReadPermission)
                        {
                            logger.LogDebug("API Key {ApiKeyId} has general read permission for document without bucket", apiKeyId);
                            return DocumentAuthorizationResult.Success();
                        }
                    }
                }
                
                return DocumentAuthorizationResult.Failure("You don't have permission to access this document.");
            }

            // Document owner check (for user-based auth)
            if (userId == document.CreatedBy)
            {
                return DocumentAuthorizationResult.Success();
            }

            // Bucket permission check (for user-based auth)
            if (document.BucketId.HasValue)
            {
                var permissionCheck = await permissionManager.CheckUserBucketOperationPermissionAsync(
                    userId, document.BucketId.Value, PermissionLevel.Read);
                
                if (permissionCheck.IsAllowed)
                {
                    return DocumentAuthorizationResult.Success();
                }
            }

            return DocumentAuthorizationResult.Failure("You don't have permission to access this document.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking document access authorization for user {UserId}, document {DocumentId}", userId, document.Id);
            return DocumentAuthorizationResult.Failure("Authorization check failed due to an internal error.");
        }
    }

    public async Task<DocumentAuthorizationResult> CanUpdateDocumentAsync(string userId, DocumentDto document, UpdateDocumentDto update)
    {
        try
        {
            // Admin access check
            var user = httpContextAccessor.HttpContext?.User;
            var hasAdminAccess = user != null ? await authorizationService.AuthorizeAsync(user, "Admin.Access") : Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed();
            var hasDocumentAdmin = user != null ? await authorizationService.AuthorizeAsync(user, "Document.Admin") : Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed();
            
            if (hasAdminAccess.Succeeded || hasDocumentAdmin.Succeeded)
            {
                return DocumentAuthorizationResult.Success();
            }

            // Check if this is API Key authentication
            var apiKeyIdClaim = user?.FindFirst("ApiKeyId");
            if (apiKeyIdClaim != null && Guid.TryParse(apiKeyIdClaim.Value, out var apiKeyId))
            {
                // Use API Key permission system
                if (document.BucketId.HasValue)
                {
                    var apiKeyPermissionCheck = await CheckApiKeyBucketPermissionFromCacheAsync(apiKeyId, document.BucketId.Value, PermissionLevel.Write);
                    
                    if (!apiKeyPermissionCheck)
                    {
                        return DocumentAuthorizationResult.Failure("You don't have write permission to this bucket.");
                    }
                }

                // Check target bucket permission if bucket is being changed
                if (update.BucketId.HasValue && document.BucketId != update.BucketId)
                {
                    var targetBucketPermissionCheck = await CheckApiKeyBucketPermissionFromCacheAsync(apiKeyId, update.BucketId.Value, PermissionLevel.Write);

                    if (!targetBucketPermissionCheck)
                    {
                        return DocumentAuthorizationResult.Failure("You don't have write permission to the target bucket.");
                    }
                }
                
                return DocumentAuthorizationResult.Success();
            }

            // Document owner check (for user-based auth)
            if (userId == document.CreatedBy)
            {
                // Check target bucket permission if bucket is being changed
                if (update.BucketId.HasValue && document.BucketId != update.BucketId)
                {
                    var targetBucketPermissionCheck = await permissionManager.CheckUserBucketOperationPermissionAsync(
                        userId, update.BucketId.Value, PermissionLevel.Write);

                    if (!targetBucketPermissionCheck.IsAllowed)
                    {
                        return DocumentAuthorizationResult.Failure("You don't have write permission to the target bucket.");
                    }
                }
                
                return DocumentAuthorizationResult.Success();
            }

            // Bucket permission check (for user-based auth)
            if (document.BucketId.HasValue)
            {
                var permissionCheck = await permissionManager.CheckUserBucketOperationPermissionAsync(
                    userId, document.BucketId.Value, PermissionLevel.Write);
                
                if (permissionCheck.IsAllowed)
                {
                    // Check target bucket permission if bucket is being changed
                    if (update.BucketId.HasValue && document.BucketId != update.BucketId)
                    {
                        var targetBucketPermissionCheck = await permissionManager.CheckUserBucketOperationPermissionAsync(
                            userId, update.BucketId.Value, PermissionLevel.Write);

                        if (!targetBucketPermissionCheck.IsAllowed)
                        {
                            return DocumentAuthorizationResult.Failure("You don't have write permission to the target bucket.");
                        }
                    }
                    
                    return DocumentAuthorizationResult.Success();
                }
            }

            return DocumentAuthorizationResult.Failure("You don't have permission to update this document.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking document update authorization for user {UserId}, document {DocumentId}", userId, document.Id);
            return DocumentAuthorizationResult.Failure("Authorization check failed due to an internal error.");
        }
    }

    public async Task<DocumentAuthorizationResult> CanDeleteDocumentAsync(string userId, DocumentDto document)
    {
        try
        {
            // Admin access check
            var user = httpContextAccessor.HttpContext?.User;
            var hasAdminAccess = user != null ? await authorizationService.AuthorizeAsync(user, "Admin.Access") : Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed();
            var hasDocumentAdmin = user != null ? await authorizationService.AuthorizeAsync(user, "Document.Admin") : Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed();
            
            if (hasAdminAccess.Succeeded || hasDocumentAdmin.Succeeded)
            {
                return DocumentAuthorizationResult.Success();
            }

            // Check if this is API Key authentication
            var apiKeyIdClaim = user?.FindFirst("ApiKeyId");
            if (apiKeyIdClaim != null && Guid.TryParse(apiKeyIdClaim.Value, out var apiKeyId))
            {
                // Use API Key permission system
                if (document.BucketId.HasValue)
                {
                    var apiKeyPermissionCheck = await CheckApiKeyBucketPermissionFromCacheAsync(apiKeyId, document.BucketId.Value, PermissionLevel.Delete);
                    
                    if (apiKeyPermissionCheck)
                    {
                        return DocumentAuthorizationResult.Success();
                    }
                }
                
                return DocumentAuthorizationResult.Failure("You don't have permission to delete this document.");
            }

            // Document owner check (for user-based auth)
            if (userId == document.CreatedBy)
            {
                return DocumentAuthorizationResult.Success();
            }

            // Bucket permission check (for user-based auth)
            if (document.BucketId.HasValue)
            {
                var permissionCheck = await permissionManager.CheckUserBucketOperationPermissionAsync(
                    userId, document.BucketId.Value, PermissionLevel.Delete);
                
                if (permissionCheck.IsAllowed)
                {
                    return DocumentAuthorizationResult.Success();
                }
            }

            return DocumentAuthorizationResult.Failure("You don't have permission to delete this document.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking document delete authorization for user {UserId}, document {DocumentId}", userId, document.Id);
            return DocumentAuthorizationResult.Failure("Authorization check failed due to an internal error.");
        }
    }

    public async Task<DocumentAuthorizationResult> ValidateProviderAccessAsync(DocumentDto document, System.Security.Claims.ClaimsPrincipal user)
    {
        try
        {
                var provider = await storageProviderService.GetByIdAsync(document.StorageProviderId);
                if (provider != null && !provider.IsActive)
                {
                    // Inactive provider - only authorized users can access
                    var hasInactiveProviderAccess = await authorizationService.AuthorizeAsync(user, "Document.ViewInactiveProvider");
                    if (!hasInactiveProviderAccess.Succeeded)
                    {
                        return DocumentAuthorizationResult.Failure("Access denied. Storage provider is inactive.");
                    }
                }

                return DocumentAuthorizationResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating provider access for document {DocumentId}", document.Id);
            return DocumentAuthorizationResult.Failure("Authorization check failed due to an internal error.");
        }
    }

    public async Task<DocumentAuthorizationResult> ValidateProviderAccessAsync(Guid documentId, System.Security.Claims.ClaimsPrincipal user)
    {
        try
        {
            var document = await documentService.GetByIdAsync(documentId);
            if (document == null)
            {
                return DocumentAuthorizationResult.Failure("Document not found.");
            }

            return await ValidateProviderAccessAsync(document, user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating provider access for document {DocumentId}", documentId);
            return DocumentAuthorizationResult.Failure("Authorization check failed due to an internal error.");
        }
    }
} 