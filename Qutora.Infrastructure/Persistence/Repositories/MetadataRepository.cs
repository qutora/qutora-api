using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using System.Text.Json;
using Qutora.Application.Interfaces.Repositories;


namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// Metadata repository implementation
/// </summary>
public class MetadataRepository(ApplicationDbContext context, ILogger<MetadataRepository> logger)
    : Repository<Metadata>(context, logger), IMetadataRepository
{
    /// <inheritdoc/>
    public async Task<Metadata?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Document)
            .FirstOrDefaultAsync(m => m.DocumentId == documentId && !m.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Metadata>> GetByTagsAsync(string[] tags, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        var query = _dbSet
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        foreach (var tag in tags)
        {
            var tagPattern = $"\"{tag}\"";
            query = query.Where(m => EF.Functions.Like(m.MetadataJson, $"%{tagPattern}%"));
        }

        return await query
            .Include(m => m.Document)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetByTagsCountAsync(string[] tags, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        foreach (var tag in tags)
        {
            var tagPattern = $"\"{tag}\"";
            query = query.Where(m => EF.Functions.Like(m.MetadataJson, $"%{tagPattern}%"));
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Metadata>> SearchAsync(Dictionary<string, object> searchCriteria, int page = 1,
        int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        var query = _dbSet
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        foreach (var criteria in searchCriteria)
        {
            string keyValuePattern;

            switch (criteria.Value)
            {
                case string stringValue:
                    keyValuePattern = $"\"{criteria.Key}\"\\s*:\\s*\"{stringValue.Replace("\"", "\\\"")}\"";
                    break;
                case bool boolValue:
                {
                    var boolString = boolValue ? "true" : "false";
                    keyValuePattern = $"\"{criteria.Key}\"\\s*:\\s*{boolString}";
                    break;
                }
                case int or double or decimal or float:
                    keyValuePattern = $"\"{criteria.Key}\"\\s*:\\s*{criteria.Value}";
                    break;
                default:
                {
                    var jsonValue = JsonSerializer.Serialize(criteria.Value);
                    keyValuePattern = $"\"{criteria.Key}\"\\s*:\\s*{jsonValue}";
                    break;
                }
            }

            query = query.Where(m => EF.Functions.Like(m.MetadataJson, $"%{keyValuePattern}%"));
        }

        return await query
            .Include(m => m.Document)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> SearchCountAsync(Dictionary<string, object> searchCriteria, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        foreach (var criteria in searchCriteria)
        {
            string keyValuePattern;

            switch (criteria.Value)
            {
                case string stringValue:
                    keyValuePattern = $"\"{criteria.Key}\"\\s*:\\s*\"{stringValue.Replace("\"", "\\\"")}\"";
                    break;
                case bool boolValue:
                {
                    var boolString = boolValue ? "true" : "false";
                    keyValuePattern = $"\"{criteria.Key}\"\\s*:\\s*{boolString}";
                    break;
                }
                case int or double or decimal or float:
                    keyValuePattern = $"\"{criteria.Key}\"\\s*:\\s*{criteria.Value}";
                    break;
                default:
                {
                    var jsonValue = JsonSerializer.Serialize(criteria.Value);
                    keyValuePattern = $"\"{criteria.Key}\"\\s*:\\s*{jsonValue}";
                    break;
                }
            }

            query = query.Where(m => EF.Functions.Like(m.MetadataJson, $"%{keyValuePattern}%"));
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Metadata?> GetByIdWithDocumentDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Document)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
    }
}
