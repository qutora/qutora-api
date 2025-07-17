using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;


namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// MetadataSchema repository implementation
/// </summary>
public class MetadataSchemaRepository(ApplicationDbContext context, ILogger<MetadataSchemaRepository> logger)
    : Repository<MetadataSchema>(context, logger), IMetadataSchemaRepository
{
    /// <inheritdoc/>
    public async Task<MetadataSchema?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower() && !s.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MetadataSchema>> GetAllPagedAsync(int page = 1, int pageSize = 10, string query = "",
        CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        var queryable = _dbSet
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLower();
            queryable = queryable.Where(s =>
                s.Name.ToLower().Contains(lowerQuery) ||
                (s.Description != null && s.Description.ToLower().Contains(lowerQuery)));
        }

        return await queryable
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalCountAsync(string query = "", CancellationToken cancellationToken = default)
    {
        var queryable = _dbSet
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLower();
            queryable = queryable.Where(s =>
                s.Name.ToLower().Contains(lowerQuery) ||
                (s.Description != null && s.Description.ToLower().Contains(lowerQuery)));
        }

        return await queryable.CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MetadataSchema>> GetByFileTypeAsync(string fileType,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(fileType) && !fileType.StartsWith(".")) fileType = "." + fileType;

        return await _dbSet
            .Where(s => !s.IsDeleted && s.IsActive &&
                        (EF.Functions.Like(s.FileTypes, $"%{fileType}%") || string.IsNullOrEmpty(s.FileTypes)))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MetadataSchema>> GetByCategoryIdAsync(Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => !s.IsDeleted && s.IsActive && s.CategoryId == categoryId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MetadataSchema>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => !s.IsDeleted && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
