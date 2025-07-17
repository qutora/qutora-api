using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

/// <summary>
/// AuditLog repository interface
/// Compliant with Clean Architecture standards
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Gets user's audit logs
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by action type
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by entity
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by date range
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent audit logs
    /// </summary>
    Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default);
}