using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;
using Qutora.Shared.Enums;

namespace Qutora.Application.Interfaces;

public interface IApiKeyService
{
    /// <summary>
    /// Tüm API anahtarlarını getirir
    /// </summary>
    Task<IEnumerable<ApiKey>> GetAllApiKeysAsync();

    /// <summary>
    /// Belirli bir kullanıcının API anahtarlarını getirir
    /// </summary>
    Task<IEnumerable<ApiKey>> GetApiKeysByUserIdAsync(string userId);

    /// <summary>
    /// ID'ye göre API anahtarını getirir
    /// </summary>
    Task<ApiKey> GetApiKeyByIdAsync(Guid id);

    /// <summary>
    /// API Key'in bucket izinlerini getirir
    /// </summary>
    /// <param name="apiKeyId">API Key ID'si</param>
    /// <returns>API Key bucket izinleri listesi</returns>
    Task<IEnumerable<ApiKeyBucketPermissionDto>> GetApiKeyBucketPermissionsAsync(Guid apiKeyId);

    /// <summary>
    /// Yeni bir API anahtarı oluşturur
    /// </summary>
    Task<(string Key, string Secret, ApiKey ApiKey)> CreateApiKeyAsync(
        string userId,
        string name,
        DateTime? expiresAt,
        IEnumerable<Guid> allowedProviderIds,
        ApiKeyPermission permission);

    /// <summary>
    /// API anahtarını günceller
    /// </summary>
    Task UpdateApiKeyAsync(ApiKey apiKey);

    /// <summary>
    /// API anahtarını siler
    /// </summary>
    Task DeleteApiKeyAsync(Guid id);

    /// <summary>
    /// API anahtarını devre dışı bırakır
    /// </summary>
    Task DeactivateApiKeyAsync(Guid id);

    /// <summary>
    /// API anahtarı ve secret kombinasyonunu doğrular
    /// </summary>
    Task<bool> ValidateApiKeyAsync(string key, string secret);

    /// <summary>
    /// API anahtarı secretini hash'e dönüştürür
    /// </summary>
    string HashSecret(string secret);

    /// <summary>
    /// Yeni bir API anahtarı oluşturur (rastgele string)
    /// </summary>
    string GenerateKey();

    /// <summary>
    /// Yeni bir API secret oluşturur (rastgele string)
    /// </summary>
    string GenerateSecret();
}