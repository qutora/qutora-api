using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;
using Qutora.Shared.Enums;

namespace Qutora.Application.Services;

/// <summary>
/// Cached implementation of API Key service that wraps the original service with caching capabilities
/// </summary>
public class CachedApiKeyService(
    IApiKeyService originalService,
    IApiKeyCacheService cacheService,
    ILogger<CachedApiKeyService> logger)
    : IApiKeyService
{
    public async Task<IEnumerable<ApiKey>> GetAllApiKeysAsync()
    {
        return await originalService.GetAllApiKeysAsync();
    }

    public async Task<IEnumerable<ApiKey>> GetApiKeysByUserIdAsync(string userId)
    {
        return await originalService.GetApiKeysByUserIdAsync(userId);
    }

    public async Task<ApiKey> GetApiKeyByIdAsync(Guid id)
    {
        // Try cache first
        var cachedApiKey = await cacheService.GetApiKeyByIdAsync(id);
        if (cachedApiKey != null)
        {
            logger.LogDebug("✅ API key retrieved from cache for ID: {Id}", id);
            return new ApiKey
            {
                Id = cachedApiKey.Id,
                Key = cachedApiKey.Key,
                SecretHash = cachedApiKey.SecretHash,
                UserId = cachedApiKey.UserId,
                Name = cachedApiKey.Name,
                IsActive = cachedApiKey.IsActive,
                ExpiresAt = cachedApiKey.ExpiresAt,
                LastUsedAt = cachedApiKey.LastUsedAt,
                Permissions = cachedApiKey.Permissions,
                AllowedProviderIds = cachedApiKey.AllowedProviderIds
            };
        }

        // Fallback to original service
        logger.LogDebug("⚠️ API key fallback to database for ID: {Id}", id);
        return await originalService.GetApiKeyByIdAsync(id);
    }

    public async Task<IEnumerable<ApiKeyBucketPermissionDto>> GetApiKeyBucketPermissionsAsync(Guid apiKeyId)
    {
        return await originalService.GetApiKeyBucketPermissionsAsync(apiKeyId);
    }

    public async Task<(string Key, string Secret, ApiKey ApiKey)> CreateApiKeyAsync(
        string userId, string name, DateTime? expiresAt, IEnumerable<Guid> allowedProviderIds, ApiKeyPermission permission)
    {
        var result = await originalService.CreateApiKeyAsync(userId, name, expiresAt, allowedProviderIds, permission);
        
        // Invalidate cache to force refresh
        cacheService.RemoveApiKey(result.ApiKey.Id);
        
        return result;
    }

    public async Task UpdateApiKeyAsync(ApiKey apiKey)
    {
        await originalService.UpdateApiKeyAsync(apiKey);
        
        // Invalidate cache to force refresh
        cacheService.RemoveApiKey(apiKey.Id);
    }

    public async Task DeleteApiKeyAsync(Guid id)
    {
        await originalService.DeleteApiKeyAsync(id);
        
        // Invalidate cache
        cacheService.RemoveApiKey(id);
    }

    public async Task DeactivateApiKeyAsync(Guid id)
    {
        await originalService.DeactivateApiKeyAsync(id);
        
        // Invalidate cache
        cacheService.RemoveApiKey(id);
    }

    public async Task<bool> ValidateApiKeyAsync(string key, string secret)
    {
        // Try cache first
        var cachedApiKey = await cacheService.GetApiKeyByKeyAsync(key);
        if (cachedApiKey != null)
        {
            logger.LogDebug("✅ API key validation from cache for key: {Key}", key);
            return BCrypt.Net.BCrypt.Verify(secret, cachedApiKey.SecretHash) && 
                   cachedApiKey.IsActive && 
                   (cachedApiKey.ExpiresAt == null || cachedApiKey.ExpiresAt > DateTime.UtcNow);
        }

        // Fallback to original service
        logger.LogDebug("⚠️ API key validation fallback to database for key: {Key}", key);
        return await originalService.ValidateApiKeyAsync(key, secret);
    }

    public string HashSecret(string secret)
    {
        return originalService.HashSecret(secret);
    }

    public string GenerateKey()
    {
        return originalService.GenerateKey();
    }

    public string GenerateSecret()
    {
        return originalService.GenerateSecret();
    }
} 