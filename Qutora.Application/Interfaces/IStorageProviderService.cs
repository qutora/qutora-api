using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for storage providers
/// </summary>
public interface IStorageProviderService
{
    /// <summary>
    /// Gets all storage providers
    /// </summary>
    Task<IEnumerable<StorageProviderDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only active storage providers
    /// </summary>
    Task<IEnumerable<StorageProviderDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default storage provider
    /// </summary>
    Task<StorageProviderDto?> GetDefaultProviderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active storage providers that the user has bucket permissions for
    /// </summary>
    Task<IEnumerable<StorageProviderDto>> GetUserAccessibleProvidersAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets storage provider by ID
    /// </summary>
    Task<StorageProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active provider IDs
    /// </summary>
    Task<IEnumerable<string>> GetAvailableProviderNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all provider types available in the system
    /// </summary>
    IEnumerable<string> GetAvailableProviderTypes();

    /// <summary>
    /// Adds a new storage provider
    /// </summary>
    Task<StorageProviderDto> CreateAsync(StorageProviderCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates storage provider
    /// </summary>
    Task<bool> UpdateAsync(Guid id, StorageProviderUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes provider active/inactive
    /// </summary>
    Task<bool> ToggleStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes provider default
    /// </summary>
    Task<bool> SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes storage provider
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests provider connection
    /// </summary>
    Task<(bool success, string message)> TestConnectionAsync(StorageProviderTestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns configuration schema for a specific provider type
    /// </summary>
    /// <param name="providerType">Provider type</param>
    /// <returns>Configuration schema</returns>
    string GetConfigurationSchema(string providerType);


}