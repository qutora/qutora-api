using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;


namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// DocumentShare repository implementation following Clean Architecture standards
/// </summary>
public class DocumentShareRepository : Repository<DocumentShare>, IDocumentShareRepository
{
    public DocumentShareRepository(ApplicationDbContext context, ILogger<DocumentShareRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets share by share code
    /// </summary>
    public async Task<DocumentShare?> GetByShareCodeAsync(string shareCode,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Document)
            .Include(s => s.CreatedByUser)
            .FirstOrDefaultAsync(s => s.ShareCode == shareCode, cancellationToken);
    }

    /// <summary>
    /// Gets shares for a document
    /// </summary>
    public async Task<IEnumerable<DocumentShare>> GetByDocumentIdAsync(Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.CreatedByUser)
            .Where(s => s.DocumentId == documentId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets user's shares
    /// </summary>
    public async Task<IEnumerable<DocumentShare>> GetByUserIdAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Document)
            .Where(s => s.CreatedBy == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Increments view count
    /// </summary>
    public async Task<bool> IncrementViewCountAsync(Guid shareId, CancellationToken cancellationToken = default)
    {
        var share = await _dbSet.FindAsync([shareId], cancellationToken);
        if (share == null)
            return false;

        share.ViewCount += 1;
        return true;
    }

    /// <summary>
    /// Deactivates share
    /// </summary>
    public async Task<bool> DeactivateShareAsync(Guid shareId, CancellationToken cancellationToken = default)
    {
        var share = await _dbSet.FindAsync([shareId], cancellationToken);
        if (share == null)
            return false;

        share.IsActive = false;
        return true;
    }
}
