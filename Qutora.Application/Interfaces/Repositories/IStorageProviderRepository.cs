using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

/// <summary>
/// StorageProvider repository interface
/// Compliant with Clean Architecture standards
/// </summary>
public interface IStorageProviderRepository : IRepository<StorageProvider>
{
    /// <summary>
    /// Gets active storage providers
    /// </summary>
    Task<IEnumerable<StorageProvider>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets default storage provider - returns null if not found
    /// </summary>
    Task<StorageProvider?> GetDefaultProviderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets provider as default
    /// </summary>
    Task<bool> SetAsDefaultAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets provider's active status
    /// </summary>
    Task<bool> SetActiveStatusAsync(Guid providerId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active providers
    /// </summary>
    Task<IEnumerable<StorageProvider>> GetActiveProvidersAsync(CancellationToken cancellationToken = default);
}