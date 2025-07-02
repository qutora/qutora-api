using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Depolama sağlayıcıları için servis arayüzü
/// </summary>
public interface IStorageProviderService
{
    /// <summary>
    /// Tüm storage provider'ları getirir
    /// </summary>
    Task<IEnumerable<StorageProviderDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sadece aktif storage provider'ları getirir
    /// </summary>
    Task<IEnumerable<StorageProviderDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Varsayılan storage provider'ı getirir
    /// </summary>
    Task<StorageProviderDto?> GetDefaultProviderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının bucket yetkisi olan aktif storage provider'ları getirir
    /// </summary>
    Task<IEnumerable<StorageProviderDto>> GetUserAccessibleProvidersAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// ID'ye göre storage provider getirir
    /// </summary>
    Task<StorageProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktif tüm provider ID'lerini getirir
    /// </summary>
    Task<IEnumerable<string>> GetAvailableProviderNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sistemde kullanılabilir tüm provider tiplerini getirir
    /// </summary>
    IEnumerable<string> GetAvailableProviderTypes();

    /// <summary>
    /// Yeni storage provider ekler
    /// </summary>
    Task<StorageProviderDto> CreateAsync(StorageProviderCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Storage provider günceller
    /// </summary>
    Task<bool> UpdateAsync(Guid id, StorageProviderUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider'ı aktif/pasif yapar
    /// </summary>
    Task<bool> ToggleStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider'ı varsayılan yapar
    /// </summary>
    Task<bool> SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Storage provider'ı siler
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider bağlantısını test eder
    /// </summary>
    Task<(bool success, string message)> TestConnectionAsync(StorageProviderTestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir provider tipi için konfigürasyon şemasını döndürür
    /// </summary>
    /// <param name="providerType">Provider type</param>
    /// <returns>Configuration schema</returns>
    string GetConfigurationSchema(string providerType);


}