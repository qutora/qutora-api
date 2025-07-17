using Microsoft.EntityFrameworkCore;
using Qutora.Domain.Entities;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;


namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Document entity
/// </summary>
public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(ApplicationDbContext context, ILogger<DocumentRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<Document>> GetByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.Category)
            .Include(d => d.StorageProvider)
            .Include(d => d.Bucket)
            .Where(d => d.CategoryId == categoryId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetByCategoryCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.CategoryId == categoryId && !d.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetDocumentsAsync(string? query = null, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var queryable = _dbSet
            .Include(d => d.Category)
            .Include(d => d.StorageProvider)
            .Include(d => d.Bucket)
            .Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(d =>
                d.Name.Contains(query) ||
                d.FileName.Contains(query) ||
                (d.Category != null && d.Category.Name.Contains(query)));

        return await queryable
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetDocumentsCountAsync(string? query = null, CancellationToken cancellationToken = default)
    {
        var queryable = _dbSet
            .Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(d =>
                d.Name.Contains(query) ||
                d.FileName.Contains(query) ||
                (d.Category != null && d.Category.Name.Contains(query)));

        return await queryable.CountAsync(cancellationToken);
    }

    public async Task<Document?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.Category)
            .Include(d => d.Metadata)
            .Include(d => d.CurrentVersion)
            .Include(d => d.Bucket)
            .Include(d => d.StorageProvider)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Checks if document exists
    /// </summary>
    public override async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Updates a document
    /// </summary>
    public override async Task<bool> UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        try
        {
            _dbSet.Update(document);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document with ID {DocumentId}", document.Id);
            return false;
        }
    }

    /// <summary>
    /// Gets documents by bucket ID
    /// </summary>
    public async Task<IEnumerable<Document>> GetByBucketIdAsync(Guid bucketId, int page = 1, int pageSize = 10,
        string? query = null, CancellationToken cancellationToken = default)
    {
        var queryable = _dbSet
            .Include(d => d.Category)
            .Include(d => d.StorageProvider)
            .Include(d => d.Bucket)
            .Where(d => d.BucketId == bucketId && !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(d =>
                d.Name.Contains(query) ||
                d.FileName.Contains(query) ||
                (d.Category != null && d.Category.Name.Contains(query)));

        return await queryable
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetByBucketIdCountAsync(Guid bucketId, string? query = null, CancellationToken cancellationToken = default)
    {
        var queryable = _dbSet
            .Where(d => d.BucketId == bucketId && !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(d =>
                d.Name.Contains(query) ||
                d.FileName.Contains(query) ||
                (d.Category != null && d.Category.Name.Contains(query)));

        return await queryable.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets documents by provider ID
    /// </summary>
    public async Task<IEnumerable<Document>> GetByProviderIdAsync(Guid providerId, int page = 1, int pageSize = 10,
        string? query = null, CancellationToken cancellationToken = default)
    {
        var queryable = _dbSet
            .Include(d => d.Category)
            .Include(d => d.StorageProvider)
            .Include(d => d.Bucket)
            .Where(d => d.StorageProviderId == providerId && !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(d =>
                d.Name.Contains(query) ||
                d.FileName.Contains(query) ||
                (d.Category != null && d.Category.Name.Contains(query)));

        return await queryable
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetByProviderIdCountAsync(Guid providerId, string? query = null, CancellationToken cancellationToken = default)
    {
        var queryable = _dbSet
            .Where(d => d.StorageProviderId == providerId && !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(d =>
                d.Name.Contains(query) ||
                d.FileName.Contains(query) ||
                (d.Category != null && d.Category.Name.Contains(query)));

        return await queryable.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets documents from multiple buckets
    /// </summary>
    public async Task<IEnumerable<Document>> GetByBucketIdsAsync(IEnumerable<Guid> bucketIds, int page = 1, 
        int pageSize = 10, string? query = null, CancellationToken cancellationToken = default)
    {
        if (!bucketIds.Any())
            return new List<Document>();

        var queryable = _dbSet
            .Include(d => d.Category)
            .Include(d => d.StorageProvider)
            .Include(d => d.Bucket)
            .Where(d => bucketIds.Contains(d.BucketId.Value) && !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(d =>
                d.Name.Contains(query) ||
                d.FileName.Contains(query) ||
                (d.Category != null && d.Category.Name.Contains(query)));

        return await queryable
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetByBucketIdsCountAsync(IEnumerable<Guid> bucketIds, string? query = null, 
        CancellationToken cancellationToken = default)
    {
        if (!bucketIds.Any())
            return 0;

        var queryable = _dbSet
            .Where(d => bucketIds.Contains(d.BucketId.Value) && !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(d =>
                d.Name.Contains(query) ||
                d.FileName.Contains(query) ||
                (d.Category != null && d.Category.Name.Contains(query)));

        return await queryable.CountAsync(cancellationToken);
    }
}
