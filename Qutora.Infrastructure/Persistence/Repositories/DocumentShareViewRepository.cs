using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;


namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// DocumentShareView repository implementation following Clean Architecture standards
/// </summary>
public class DocumentShareViewRepository : Repository<DocumentShareView>, IDocumentShareViewRepository
{
    public DocumentShareViewRepository(ApplicationDbContext context, ILogger<DocumentShareViewRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets views for a share
    /// </summary>
    public async Task<IEnumerable<DocumentShareView>> GetByShareIdAsync(Guid shareId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.ShareId == shareId)
            .OrderByDescending(v => v.ViewedAt)
            .ToListAsync(cancellationToken);
    }
}
