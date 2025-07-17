using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;


namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// ApiKey repository implementation following Clean Architecture standards
/// </summary>
public class ApiKeyRepository : Repository<ApiKey>, IApiKeyRepository
{
    public ApiKeyRepository(ApplicationDbContext context, ILogger<ApiKeyRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets all API keys
    /// </summary>
    public override async Task<IEnumerable<ApiKey>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets API key by ID
    /// </summary>
    public override async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    /// <summary>
    /// Gets user's API keys
    /// </summary>
    public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(k => k.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets API key by key
    /// </summary>
    public async Task<ApiKey?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(k => k.Key == key, cancellationToken);
    }

    /// <summary>
    /// Gets API key by secret hash
    /// </summary>
    public async Task<ApiKey?> GetBySecretHashAsync(string secretHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(k => k.SecretHash == secretHash, cancellationToken);
    }

    /// <summary>
    /// Adds API key
    /// </summary>
    public override async Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(apiKey, cancellationToken);
    }

    /// <summary>
    /// Deletes API key
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetByIdAsync(id, cancellationToken);
        if (apiKey == null)
            return false;

        _dbSet.Remove(apiKey);
        return true;
    }

    /// <summary>
    /// Updates last used date
    /// </summary>
    public async Task<bool> UpdateLastUsedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetByIdAsync(id, cancellationToken);
        if (apiKey == null)
            return false;

        apiKey.LastUsedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Deactivates API key
    /// </summary>
    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetByIdAsync(id, cancellationToken);
        if (apiKey == null)
            return false;

        apiKey.IsActive = false;
        return true;
    }

    /// <summary>
    /// Key and secret hash validate
    /// </summary>
    public async Task<bool> ValidateKeySecretAsync(string key, string secretHash,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _dbSet
            .FirstOrDefaultAsync(k => k.Key == key && k.SecretHash == secretHash, cancellationToken);

        if (apiKey == null)
            return false;

        if (!apiKey.IsActive)
            return false;

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Gets API keys by provider ID
    /// </summary>
    public async Task<IEnumerable<ApiKey>> GetByProviderIdAsync(Guid providerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(k => k.IsActive
                        && (!k.ExpiresAt.HasValue || k.ExpiresAt > DateTime.UtcNow)
                        && (k.AllowedProviderIds.Contains(providerId) || !k.AllowedProviderIds.Any()))
            .ToListAsync(cancellationToken);
    }
}
