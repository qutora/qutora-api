using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Interfaces.Repositories;

namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// StorageProvider repository implementation following Clean Architecture standards
/// </summary>
public class StorageProviderRepository : Repository<StorageProvider>, IStorageProviderRepository
{
    public StorageProviderRepository(ApplicationDbContext context, ILogger<StorageProviderRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets active storage providers
    /// </summary>
    public async Task<IEnumerable<StorageProvider>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets active providers (alias)
    /// </summary>
    public async Task<IEnumerable<StorageProvider>> GetActiveProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetAllActiveAsync(cancellationToken);
    }

    /// <summary>
    /// Gets default storage provider - returns null if not found
    /// </summary>
    public async Task<StorageProvider?> GetDefaultProviderAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.IsDefault && p.IsActive, cancellationToken);
    }

    /// <summary>
    /// Sets provider as default
    /// </summary>
    public async Task<bool> SetAsDefaultAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var provider = await _dbSet
            .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);

        if (provider == null || !provider.IsActive) return false;

        var allProviders = await _dbSet.ToListAsync(cancellationToken);
        foreach (var p in allProviders) p.IsDefault = p.Id == providerId;

        return true;
    }

    /// <summary>
    /// Sets provider active status
    /// </summary>
    public async Task<bool> SetActiveStatusAsync(Guid providerId, bool isActive,
        CancellationToken cancellationToken = default)
    {
        var provider = await _dbSet
            .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);

        if (provider == null) return false;

        if (!isActive && provider.IsDefault) return false;

        provider.IsActive = isActive;
        return true;
    }
}
