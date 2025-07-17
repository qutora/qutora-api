using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;

using Qutora.Shared.Enums;

namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// StorageBucket repository class
/// </summary>
public class StorageBucketRepository : Repository<StorageBucket>, IStorageBucketRepository
{
    public StorageBucketRepository(ApplicationDbContext context, ILogger<StorageBucketRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StorageBucket>> GetBucketsByProviderIdAsync(Guid providerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.ProviderId == providerId && b.IsActive)
            .OrderBy(b => b.Path)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<StorageBucket?> GetDefaultBucketForProviderAsync(Guid providerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.ProviderId == providerId && b.IsDefault && b.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<StorageBucket?> GetBucketByPathAndProviderAsync(string bucketPath, Guid providerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Path == bucketPath && b.ProviderId == providerId && b.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StorageBucket>> GetUserAccessibleBucketsAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        var userBuckets = await _context.BucketPermissions
            .Where(p => p.SubjectId == userId && p.SubjectType == PermissionSubjectType.User)
            .Select(p => p.Bucket)
            .Where(b => b.IsActive)
            .ToListAsync(cancellationToken);

        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        if (userRoleIds.Any())
        {
            var roleBuckets = await _context.BucketPermissions
                .Where(p => userRoleIds.Contains(p.SubjectId) && p.SubjectType == PermissionSubjectType.Role)
                .Select(p => p.Bucket)
                .Where(b => b.IsActive)
                .ToListAsync(cancellationToken);

            var allBuckets = userBuckets.Union(roleBuckets, new BucketComparer()).ToList();
            return allBuckets.OrderBy(b => b.Path);
        }

        return userBuckets.OrderBy(b => b.Path);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StorageBucket>> GetRoleAccessibleBucketsAsync(string roleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.BucketPermissions
            .Where(p => p.SubjectId == roleId && p.SubjectType == PermissionSubjectType.Role)
            .Select(p => p.Bucket)
            .Where(b => b.IsActive)
            .OrderBy(b => b.Path)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StorageBucket>> GetByCreatorIdAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.CreatedBy == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StorageBucket>> GetByProviderIdAsync(Guid providerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.ProviderId == providerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<StorageBucket> Items, int TotalCount)> GetPaginatedBucketsAsync(
        int page, int pageSize, string? searchTerm = null, Guid? providerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(b => b.Path.ToLower().Contains(searchTerm) ||
                                     b.Description.ToLower().Contains(searchTerm));
        }

        if (providerId.HasValue) query = query.Where(b => b.ProviderId == providerId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StorageBucket>> GetPaginatedAsync(int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<StorageBucket?> GetByProviderAndPathAsync(string providerId, string bucketPath,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(b => b.ProviderId.ToString() == providerId && b.Path == bucketPath, cancellationToken);
    }

    /// <summary>
    /// Bucket comparer class for ID-based comparison
    /// </summary>
    private class BucketComparer : IEqualityComparer<StorageBucket>
    {
        public bool Equals(StorageBucket? x, StorageBucket? y)
        {
            if (x == null || y == null)
                return false;

            return x.Id == y.Id;
        }

        public int GetHashCode(StorageBucket obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
