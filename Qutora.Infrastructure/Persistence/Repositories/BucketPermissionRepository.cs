using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;

using Qutora.Shared.Enums;

namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// BucketPermission repository class
/// </summary>
public class BucketPermissionRepository : Repository<BucketPermission>, IBucketPermissionRepository
{
    public BucketPermissionRepository(ApplicationDbContext context, ILogger<BucketPermissionRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BucketPermission>> GetPermissionsByBucketIdAsync(Guid bucketId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.BucketId == bucketId)
            .Include(p => p.Bucket)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BucketPermission>> GetUserPermissionsAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.SubjectId == userId && p.SubjectType == PermissionSubjectType.User)
            .Include(p => p.Bucket)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BucketPermission>> GetUserPermissionsPaginatedAsync(string userId, int page,
        int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Bucket)
            .Where(p => p.SubjectId == userId && p.SubjectType == PermissionSubjectType.User)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BucketPermission>> GetAllPermissionsPaginatedAsync(int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Bucket)
            .OrderBy(p => p.SubjectType)
            .ThenBy(p => p.SubjectId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> CountUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.SubjectId == userId && p.SubjectType == PermissionSubjectType.User)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> CountAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BucketPermission?> GetUserPermissionForBucketAsync(string userId, Guid bucketId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.SubjectId == userId &&
                        p.SubjectType == PermissionSubjectType.User &&
                        p.BucketId == bucketId)
            .Include(p => p.Bucket)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BucketPermission>> GetRolePermissionsAsync(string roleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.SubjectId == roleId && p.SubjectType == PermissionSubjectType.Role)
            .Include(p => p.Bucket)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BucketPermission?> GetRolePermissionForBucketAsync(string roleId, Guid bucketId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.SubjectId == roleId &&
                        p.SubjectType == PermissionSubjectType.Role &&
                        p.BucketId == bucketId)
            .Include(p => p.Bucket)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveAllPermissionsForBucketAsync(Guid bucketId, string subjectId,
        PermissionSubjectType subjectType, CancellationToken cancellationToken = default)
    {
        var permissions = await _dbSet
            .Where(p => p.BucketId == bucketId &&
                        p.SubjectId == subjectId &&
                        p.SubjectType == subjectType)
            .ToListAsync(cancellationToken);

        if (permissions.Any()) _dbSet.RemoveRange(permissions);
    }
}
