using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Interfaces.Repositories;

namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// AuditLog repository implementation following Clean Architecture standards
/// </summary>
public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context, ILogger<AuditLogRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets user's audit logs
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(al => al.User)
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by action type
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(al => al.User)
            .Where(al => al.EventType == action)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by entity
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(al => al.User)
            .Where(al => al.EntityType == entityType && al.EntityId == entityId)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by date range
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(al => al.User)
            .Where(al => al.Timestamp >= startDate && al.Timestamp <= endDate)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets recent audit logs
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(al => al.User)
            .OrderByDescending(al => al.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds audit log
    /// </summary>
    public override async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(auditLog, cancellationToken);
    }
}
