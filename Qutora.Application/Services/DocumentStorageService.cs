using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Caching.Services;
using Qutora.Infrastructure.Interfaces;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.DocumentOrchestration;
using Qutora.Shared.Enums;

namespace Qutora.Application.Services;

/// <summary>
/// Document storage service implementation
/// </summary>
public class DocumentStorageService(
    IStorageProviderService storageProviderService,
    IStorageBucketService bucketService,
    IBucketPermissionManager permissionManager,
    IHttpContextAccessor httpContextAccessor,
    IApiKeyCacheService apiKeyCacheService,
    IMemoryCache cache,
    ILogger<DocumentStorageService> logger)
    : IDocumentStorageService
{
    private const int CacheExpirationMinutes = 10;

    public async Task<StorageSelectionResult> SelectOptimalStorageAsync(string userId, Guid? providerId, Guid? bucketId)
    {
        try
        {
            // Check if this is API Key authentication
            var user = httpContextAccessor.HttpContext?.User;
            var apiKeyIdClaim = user?.FindFirst("ApiKeyId");
            Guid apiKeyId = Guid.Empty;
            var isApiKeyAuth = apiKeyIdClaim != null && Guid.TryParse(apiKeyIdClaim.Value, out apiKeyId);
            
            // Initialize apiKeyId to empty Guid if not API Key auth
            if (!isApiKeyAuth)
            {
                apiKeyId = Guid.Empty;
            }

            // If both provider and bucket are specified, validate and return
            if (providerId.HasValue && bucketId.HasValue)
            {
                return StorageSelectionResult.Success(providerId.Value, bucketId.Value);
            }

            // If no provider specified, find default provider and its default bucket
            if (!providerId.HasValue && !bucketId.HasValue)
            {
                var defaultProvider = await storageProviderService.GetDefaultProviderAsync();
                if (defaultProvider != null)
                {
                    var defaultBucket = await bucketService.GetDefaultBucketForProviderAsync(defaultProvider.Id.ToString());
                    if (defaultBucket != null)
                    {
                        if (isApiKeyAuth)
                        {
                            // For API Key auth, check API Key bucket permission
                            var apiKeyPermissionCheck = await CheckApiKeyBucketPermissionAsync(apiKeyId, defaultBucket.Id, PermissionLevel.Write);
                            if (apiKeyPermissionCheck)
                            {
                                return StorageSelectionResult.Success(defaultProvider.Id, defaultBucket.Id);
                            }
                        }
                        else
                        {
                            // For user auth, check user bucket permission
                            var permissionCheck = await permissionManager.CheckUserBucketOperationPermissionAsync(
                                userId, defaultBucket.Id, PermissionLevel.Write);

                            if (permissionCheck.IsAllowed)
                            {
                                return StorageSelectionResult.Success(defaultProvider.Id, defaultBucket.Id);
                            }
                        }
                    }
                }

                if (isApiKeyAuth)
                {
                    // For API Key, find first accessible bucket from allowed providers
                    return await SelectApiKeyAccessibleStorageAsync(apiKeyId);
                }
                else
                {
                    // If still no bucket selected, find user's first accessible bucket
                    var allProviders = await GetAllProvidersWithCacheAsync();
                    foreach (var provider in allProviders)
                    {
                        var userAccessibleBuckets = await GetUserAccessibleBucketsWithCacheAsync(userId, provider.Id.ToString());
                        var firstBucket = userAccessibleBuckets.FirstOrDefault();
                        if (firstBucket != null)
                        {
                            return StorageSelectionResult.Success(provider.Id, firstBucket.Id);
                        }
                    }
                }

                return StorageSelectionResult.Failure("No accessible storage found. You don't have permission to upload to any storage location. Please contact your administrator to grant you access to at least one storage bucket.");
            }

            // If only provider specified, find default bucket for that provider
            if (providerId.HasValue && !bucketId.HasValue)
            {
                var defaultBucket = await bucketService.GetDefaultBucketForProviderAsync(providerId.Value.ToString());
                if (defaultBucket != null)
                {
                    if (isApiKeyAuth)
                    {
                        // For API Key auth, check API Key bucket permission
                        var apiKeyPermissionCheck = await CheckApiKeyBucketPermissionAsync(apiKeyId, defaultBucket.Id, PermissionLevel.Write);
                        if (apiKeyPermissionCheck)
                        {
                            return StorageSelectionResult.Success(providerId.Value, defaultBucket.Id);
                        }
                    }
                    else
                    {
                        // For user auth, check user bucket permission
                        var permissionCheck = await permissionManager.CheckUserBucketOperationPermissionAsync(
                            userId, defaultBucket.Id, PermissionLevel.Write);

                        if (permissionCheck.IsAllowed)
                        {
                            return StorageSelectionResult.Success(providerId.Value, defaultBucket.Id);
                        }
                    }
                }

                if (isApiKeyAuth)
                {
                    // For API Key, find accessible bucket for the specific provider
                    return await SelectApiKeyAccessibleStorageForProviderAsync(apiKeyId, providerId.Value);
                }
                else
                {
                    // Find first accessible bucket for the provider
                    var userAccessibleBuckets = await GetUserAccessibleBucketsWithCacheAsync(userId, providerId.Value.ToString());
                    var firstBucket = userAccessibleBuckets.FirstOrDefault();
                    if (firstBucket != null)
                    {
                        return StorageSelectionResult.Success(providerId.Value, firstBucket.Id);
                    }
                }

                return StorageSelectionResult.Failure($"No accessible bucket found for the selected provider.");
            }

            // If only bucket specified, get its provider
            if (!providerId.HasValue && bucketId.HasValue)
            {
                var bucket = await bucketService.GetBucketByIdAsync(bucketId.Value);
                if (bucket != null)
                {
                    return StorageSelectionResult.Success(bucket.ProviderId, bucketId.Value);
                }

                return StorageSelectionResult.Failure("Selected bucket not found.");
            }

            return StorageSelectionResult.Failure("Invalid storage selection parameters.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error selecting optimal storage for user {UserId}", userId);
            return StorageSelectionResult.Failure("Storage selection failed due to an internal error.");
        }
    }

    public async Task<IEnumerable<StorageProviderDto>> GetUserAccessibleProvidersAsync(string userId)
    {
        try
        {
            var allProviders = await storageProviderService.GetAllActiveAsync();
            var accessibleProviders = new List<StorageProviderDto>();

            foreach (var provider in allProviders)
            {
                var userAccessibleBuckets = await bucketService.GetUserAccessibleBucketsForProviderAsync(userId, provider.Id.ToString());
                if (userAccessibleBuckets.Any())
                {
                    accessibleProviders.Add(provider);
                }
            }

            return accessibleProviders;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user accessible providers for user {UserId}", userId);
            return new List<StorageProviderDto>();
        }
    }

    public async Task<IEnumerable<BucketInfoDto>> GetUserAccessibleBucketsAsync(string userId, Guid? providerId)
    {
        try
        {
            if (providerId.HasValue)
            {
                return await bucketService.GetUserAccessibleBucketsForProviderAsync(userId, providerId.Value.ToString());
            }
            else
            {
                // Get all accessible buckets from all providers
                var allProviders = await storageProviderService.GetAllActiveAsync();
                var allAccessibleBuckets = new List<BucketInfoDto>();

                foreach (var provider in allProviders)
                {
                    var providerBuckets = await bucketService.GetUserAccessibleBucketsForProviderAsync(userId, provider.Id.ToString());
                    allAccessibleBuckets.AddRange(providerBuckets);
                }

                return allAccessibleBuckets;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user accessible buckets for user {UserId}, provider {ProviderId}", userId, providerId);
            return new List<BucketInfoDto>();
        }
    }

    /// <summary>
    /// Gets all providers with caching to avoid repeated DB calls
    /// </summary>
    private async Task<IEnumerable<StorageProviderDto>> GetAllProvidersWithCacheAsync()
    {
        const string cacheKey = "all_storage_providers";
        
        if (cache.TryGetValue(cacheKey, out IEnumerable<StorageProviderDto>? cachedProviders))
        {
            return cachedProviders!;
        }

        var providers = await storageProviderService.GetAllAsync();
        
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
            Size = 1
        };
        cache.Set(cacheKey, providers, cacheEntryOptions);
        
        return providers;
    }

    /// <summary>
    /// Gets user accessible buckets with caching per user+provider combination
    /// </summary>
    private async Task<IEnumerable<BucketInfoDto>> GetUserAccessibleBucketsWithCacheAsync(string userId, string providerId)
    {
        var cacheKey = $"user_buckets_{userId}_{providerId}";
        
        if (cache.TryGetValue(cacheKey, out IEnumerable<BucketInfoDto>? cachedBuckets))
        {
            return cachedBuckets!;
        }

        var buckets = await bucketService.GetUserAccessibleBucketsForProviderAsync(userId, providerId);
        
        // Cache for shorter time since permissions can change
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes / 2),
            Size = 1
        };
        cache.Set(cacheKey, buckets, cacheEntryOptions);
        
        return buckets;
    }

    /// <summary>
    /// Check API Key bucket permission using cache
    /// </summary>
    private async Task<bool> CheckApiKeyBucketPermissionAsync(Guid apiKeyId, Guid bucketId, PermissionLevel requiredPermission)
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
                return false;
            }

            // Get bucket from cache
            var cachedBucket = await apiKeyCacheService.GetBucketAsync(bucketId);
            if (cachedBucket == null || !cachedBucket.IsActive)
            {
                return false;
            }

            // Check if API Key has access to the provider
            if (!cachedApiKey.AllowedProviderIds.Contains(cachedBucket.ProviderId))
            {
                return false;
            }

            // Check specific bucket permission from cache
            var cachedPermission = await apiKeyCacheService.GetPermissionAsync(apiKeyId, bucketId);
            if (cachedPermission != null)
            {
                return cachedPermission.Permission >= requiredPermission;
            }

            // Check general API Key permissions
            return (PermissionLevel)cachedApiKey.Permissions >= requiredPermission;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking API Key permission from cache, falling back to database");
            var dbResult = await permissionManager.CheckApiKeyBucketOperationPermissionAsync(apiKeyId, bucketId, requiredPermission);
            return dbResult.IsAllowed;
        }
    }

    /// <summary>
    /// Select accessible storage for API Key from any provider
    /// </summary>
    private async Task<StorageSelectionResult> SelectApiKeyAccessibleStorageAsync(Guid apiKeyId)
    {
        try
        {
            var cachedApiKey = await apiKeyCacheService.GetApiKeyByIdAsync(apiKeyId);
            if (cachedApiKey == null || !cachedApiKey.IsActive)
            {
                return StorageSelectionResult.Failure("API Key not found or inactive.");
            }

            // Check each allowed provider for accessible buckets
            foreach (var providerId in cachedApiKey.AllowedProviderIds)
            {
                var provider = await storageProviderService.GetByIdAsync(providerId);
                if (provider == null || !provider.IsActive) continue;

                // Get default bucket for this provider
                var defaultBucket = await bucketService.GetDefaultBucketForProviderAsync(providerId.ToString());
                if (defaultBucket != null)
                {
                    var hasPermission = await CheckApiKeyBucketPermissionAsync(apiKeyId, defaultBucket.Id, PermissionLevel.Write);
                    if (hasPermission)
                    {
                        return StorageSelectionResult.Success(providerId, defaultBucket.Id);
                    }
                }

                // If no default bucket or no permission, try to find any accessible bucket for this provider
                var allBuckets = await bucketService.ListProviderBucketsAsync(providerId.ToString());
                foreach (var bucket in allBuckets)
                {
                    var hasPermission = await CheckApiKeyBucketPermissionAsync(apiKeyId, bucket.Id, PermissionLevel.Write);
                    if (hasPermission)
                    {
                        return StorageSelectionResult.Success(providerId, bucket.Id);
                    }
                }
            }

            return StorageSelectionResult.Failure("No accessible storage found for API Key.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error selecting API Key accessible storage");
            return StorageSelectionResult.Failure("Storage selection failed due to an internal error.");
        }
    }

    /// <summary>
    /// Select accessible storage for API Key from specific provider
    /// </summary>
    private async Task<StorageSelectionResult> SelectApiKeyAccessibleStorageForProviderAsync(Guid apiKeyId, Guid providerId)
    {
        try
        {
            var cachedApiKey = await apiKeyCacheService.GetApiKeyByIdAsync(apiKeyId);
            if (cachedApiKey == null || !cachedApiKey.IsActive)
            {
                return StorageSelectionResult.Failure("API Key not found or inactive.");
            }

            // Check if API Key has access to this provider
            if (!cachedApiKey.AllowedProviderIds.Contains(providerId))
            {
                return StorageSelectionResult.Failure("API Key does not have access to the selected provider.");
            }

            // Try to find any accessible bucket for this provider
            var allBuckets = await bucketService.ListProviderBucketsAsync(providerId.ToString());
            foreach (var bucket in allBuckets)
            {
                var hasPermission = await CheckApiKeyBucketPermissionAsync(apiKeyId, bucket.Id, PermissionLevel.Write);
                if (hasPermission)
                {
                    return StorageSelectionResult.Success(providerId, bucket.Id);
                }
            }

            return StorageSelectionResult.Failure("No accessible bucket found for the selected provider.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error selecting API Key accessible storage for provider {ProviderId}", providerId);
            return StorageSelectionResult.Failure("Storage selection failed due to an internal error.");
        }
    }
} 