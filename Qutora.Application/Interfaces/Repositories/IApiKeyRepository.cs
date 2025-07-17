using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

/// <summary>
/// ApiKey repository interface
/// Compliant with Clean Architecture standards
/// </summary>
public interface IApiKeyRepository : IRepository<ApiKey>
{
    /// <summary>
    /// Gets user's API keys
    /// </summary>
    Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets API key by key
    /// </summary>
    Task<ApiKey?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets API key by secret hash
    /// </summary>
    Task<ApiKey?> GetBySecretHashAsync(string secretHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes API key
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates last used date
    /// </summary>
    Task<bool> UpdateLastUsedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates API key
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates key and secret hash
    /// </summary>
    Task<bool> ValidateKeySecretAsync(string key, string secretHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets API keys by provider ID
    /// </summary>
    Task<IEnumerable<ApiKey>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);
}