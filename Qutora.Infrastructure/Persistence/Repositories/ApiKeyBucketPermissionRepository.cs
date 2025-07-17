using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;


namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// ApiKeyBucketPermission repository class
/// </summary>
public class ApiKeyBucketPermissionRepository : Repository<ApiKeyBucketPermission>, IApiKeyBucketPermissionRepository
{
    public ApiKeyBucketPermissionRepository(ApplicationDbContext context,
        ILogger<ApiKeyBucketPermissionRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ApiKeyBucketPermission>> GetPermissionsByApiKeyIdAsync(Guid apiKeyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ApiKeyId == apiKeyId)
            .Include(p => p.Bucket)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ApiKeyBucketPermission>> GetPermissionsByBucketIdAsync(Guid bucketId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.BucketId == bucketId)
            .Include(p => p.ApiKey)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiKeyBucketPermission?> GetApiKeyPermissionForBucketAsync(Guid apiKeyId, Guid bucketId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.ApiKeyId == apiKeyId && p.BucketId == bucketId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveAllPermissionsForApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var permissions = await _dbSet
            .Where(p => p.ApiKeyId == apiKeyId)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(permissions);
    }

    /// <inheritdoc/>
    public async Task RemoveAllPermissionsForBucketAsync(Guid bucketId, CancellationToken cancellationToken = default)
    {
        var permissions = await _dbSet
            .Where(p => p.BucketId == bucketId)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(permissions);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ApiKeyBucketPermission>> GetByApiKeyIdAsync(Guid apiKeyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ApiKeyId == apiKeyId)
            .Include(p => p.Bucket)
            .ToListAsync(cancellationToken);
    }
}
