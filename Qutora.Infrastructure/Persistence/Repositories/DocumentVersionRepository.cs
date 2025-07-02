using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Interfaces.Repositories;

namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// Document version repository implementation
/// </summary>
public class DocumentVersionRepository(ApplicationDbContext dbContext, ILogger<DocumentVersionRepository> logger)
    : Repository<DocumentVersion>(dbContext, logger), IDocumentVersionRepository
{
    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentVersion>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _dbSet
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .Include(v => v.CreatedByUser)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> GetLastVersionNumberAsync(Guid documentId)
    {
        var lastVersion = await _dbSet
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();

        return lastVersion?.VersionNumber ?? 0;
    }

    /// <inheritdoc/>
    public async Task<DocumentVersion?> GetDetailAsync(Guid versionId)
    {
        return await _dbSet
            .Where(v => v.Id == versionId)
            .Include(v => v.Document)
            .Include(v => v.CreatedByUser)
            .FirstOrDefaultAsync();
    }
}
