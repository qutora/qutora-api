using Qutora.Domain.Entities;

namespace Qutora.Infrastructure.Interfaces.Repositories;

/// <summary>
/// DocumentShareView repository interface
/// Compliant with Clean Architecture standards
/// </summary>
public interface IDocumentShareViewRepository : IRepository<DocumentShareView>
{
    /// <summary>
    /// Gets views belonging to a share
    /// </summary>
    Task<IEnumerable<DocumentShareView>> GetByShareIdAsync(Guid shareId, CancellationToken cancellationToken = default);
}