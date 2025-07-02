using System.Security.Cryptography;
using System.Text;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;
using Qutora.Infrastructure.Caching.Events;
using Qutora.Infrastructure.Exceptions;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.UnitOfWork;
using Qutora.Shared.DTOs;
using Qutora.Shared.Enums;

namespace Qutora.Application.Services;

public class ApiKeyService(
    IUnitOfWork unitOfWork,
    ILogger<ApiKeyService> logger,
    IMapper mapper,
    UserManager<ApplicationUser> userManager,
    IStorageProviderService storageProviderService,
    CacheInvalidationService cacheInvalidationService) : IApiKeyService
{
    public async Task<IEnumerable<ApiKey>> GetAllApiKeysAsync()
    {
        try
        {
            return await unitOfWork.ApiKeys.GetAllAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all API keys");
            throw;
        }
    }

    public async Task<IEnumerable<ApiKey>> GetApiKeysByUserIdAsync(string userId)
    {
        try
        {
            return await unitOfWork.ApiKeys.GetByUserIdAsync(userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting API keys for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ApiKey> GetApiKeyByIdAsync(Guid id)
    {
        try
        {
            return await unitOfWork.ApiKeys.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting API key with ID {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Validates if user has access to specified providers
    /// </summary>
    private async Task<bool> ValidateUserProviderAccessAsync(string userId, ICollection<Guid> allowedProviderIds)
    {
        try
        {
            if (allowedProviderIds == null || !allowedProviderIds.Any())
                return true; // Empty list means access to all providers user has access to

            var userAccessibleProviders = await storageProviderService.GetUserAccessibleProvidersAsync(userId);
            var userProviderIds = userAccessibleProviders.Select(p => p.Id).ToHashSet();

            return allowedProviderIds.All(providerId => userProviderIds.Contains(providerId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating user provider access for user {UserId}", userId);
            return false;
        }
    }

    public async Task<(string Key, string Secret, ApiKey ApiKey)> CreateApiKeyAsync(
        string userId,
        string name,
        DateTime? expiresAt,
        IEnumerable<Guid> allowedProviderIds,
        ApiKeyPermission permission)
    {
        try
        {
            var providerIds = allowedProviderIds?.ToList() ?? new List<Guid>();

            // If no specific providers are provided, use all accessible providers for the user
            if (!providerIds.Any())
            {
                var userAccessibleProviders = await storageProviderService.GetUserAccessibleProvidersAsync(userId);
                providerIds = userAccessibleProviders.Select(p => p.Id).ToList();
                logger.LogInformation("No specific providers provided for API key, using all accessible providers for user {UserId}: {ProviderCount} providers", 
                    userId, providerIds.Count);
            }

            // Validate user has access to specified providers
            if (!await ValidateUserProviderAccessAsync(userId, providerIds))
            {
                throw new UnauthorizedAccessException("You can only create API keys for storage providers you have access to.");
            }

            var key = GenerateKey();

            var secret = GenerateSecret();

            var secretHash = HashSecret(secret);

            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = key,
                SecretHash = secretHash,
                UserId = userId,
                Name = name,
                ExpiresAt = expiresAt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                AllowedProviderIds = providerIds,
                Permissions = permission
            };

            var result = await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.ApiKeys.AddAsync(apiKey);

                return (key, secret, apiKey);
            });

            // Trigger cache invalidation
            await cacheInvalidationService.OnApiKeyCreatedAsync(apiKey.Id);

            return result;
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while creating API key: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating API key for user {UserId}", userId);
            throw;
        }
    }

    public async Task UpdateApiKeyAsync(ApiKey apiKey)
    {
        try
        {
            // Validate user has access to specified providers
            if (!await ValidateUserProviderAccessAsync(apiKey.UserId, apiKey.AllowedProviderIds))
            {
                throw new UnauthorizedAccessException("You can only assign storage providers you have access to.");
            }

            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.ApiKeys.UpdateAsync(apiKey);

                return true;
            });

            // Trigger cache invalidation
            await cacheInvalidationService.OnApiKeyUpdatedAsync(apiKey.Id);
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while updating API key: {Id}", apiKey.Id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating API key {Id}", apiKey.Id);
            throw;
        }
    }

    public async Task DeleteApiKeyAsync(Guid id)
    {
        try
        {
            // Get the API key first to get the key value for cache removal
            var apiKey = await unitOfWork.ApiKeys.GetByIdAsync(id);
            
            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.ApiKeys.DeleteAsync(id);

                return true;
            });

            // Trigger cache invalidation
            await cacheInvalidationService.OnApiKeyDeletedAsync(id, apiKey?.Key);
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while deleting API key: {Id}", id);
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "API key not found while attempting to delete: {Id}", id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting API key {Id}", id);
            throw;
        }
    }

    public async Task DeactivateApiKeyAsync(Guid id)
    {
        try
        {
            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.ApiKeys.DeactivateAsync(id);

                return true;
            });
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while deactivating API key: {Id}", id);
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "API key not found while attempting to deactivate: {Id}", id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deactivating API key {Id}", id);
            throw;
        }
    }

    public async Task<bool> ValidateApiKeyAsync(string key, string secret)
    {
        try
        {
            var secretHash = HashSecret(secret);

            return await unitOfWork.ApiKeys.ValidateKeySecretAsync(key, secretHash);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating API key and secret");
            return false;
        }
    }

    public string HashSecret(string secret)
    {
        using var sha256 = SHA256.Create();
        var secretBytes = Encoding.UTF8.GetBytes(secret);

        var hashBytes = sha256.ComputeHash(secretBytes);

        return Convert.ToBase64String(hashBytes);
    }

    public string GenerateKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[16];
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("/", "-")
            .Replace("+", "_")
            .Replace("=", "");
    }

    public string GenerateSecret()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[32];
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("/", "-")
            .Replace("+", "_")
            .Replace("=", "");
    }

    public async Task<IEnumerable<ApiKeyBucketPermissionDto>> GetApiKeyBucketPermissionsAsync(Guid apiKeyId)
    {
        try
        {
            var apiKey = await unitOfWork.ApiKeys.GetByIdAsync(apiKeyId);
            if (apiKey == null)
            {
                logger.LogWarning("API Key not found: {ApiKeyId}", apiKeyId);
                return [];
            }

            var permissions = await unitOfWork.ApiKeyBucketPermissions.GetByApiKeyIdAsync(apiKeyId);

            var permissionDtos = new List<ApiKeyBucketPermissionDto>();
            foreach (var permission in permissions)
            {
                var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(permission.BucketId);

                var dto = mapper.Map<ApiKeyBucketPermissionDto>(permission);
                dto.BucketPath = bucket?.Path ?? "Unknown Bucket";

                if (!string.IsNullOrEmpty(permission.CreatedBy))
                {
                    var user = await userManager.FindByIdAsync(permission.CreatedBy);
                    dto.GrantedByName = user?.UserName ?? "Unknown User";
                }

                permissionDtos.Add(dto);
            }

            return permissionDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while retrieving API Key bucket permissions: {ApiKeyId}", apiKeyId);
            return [];
        }
    }
}
