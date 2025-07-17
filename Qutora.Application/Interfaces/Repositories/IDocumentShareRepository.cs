using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

/// <summary>
/// DocumentShare repository interface
/// Compliant with Clean Architecture standards
/// </summary>
public interface IDocumentShareRepository : IRepository<DocumentShare>
{
    /// <summary>
    /// Gets share by share code
    /// </summary>
    Task<DocumentShare?> GetByShareCodeAsync(string shareCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shares belonging to a document
    /// </summary>
    Task<IEnumerable<DocumentShare>> GetByDocumentIdAsync(Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user's shares
    /// </summary>
    Task<IEnumerable<DocumentShare>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments view count
    /// </summary>
    Task<bool> IncrementViewCountAsync(Guid shareId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates share
    /// </summary>
    Task<bool> DeactivateShareAsync(Guid shareId, CancellationToken cancellationToken = default);
}