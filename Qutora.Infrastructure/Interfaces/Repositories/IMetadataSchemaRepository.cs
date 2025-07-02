using Qutora.Domain.Entities;

namespace Qutora.Infrastructure.Interfaces.Repositories;

/// <summary>
/// Repository interface for metadata schemas
/// </summary>
public interface IMetadataSchemaRepository : IRepository<MetadataSchema>
{
    /// <summary>
    /// Searches schema by name
    /// </summary>
    /// <param name="name">Schema name</param>
    /// <returns>Found schema</returns>
    Task<MetadataSchema?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all schemas paginated
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <param name="query">Search query</param>
    /// <returns>Schemas</returns>
    Task<IEnumerable<MetadataSchema>> GetAllPagedAsync(int page = 1, int pageSize = 10, string query = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns total schema count
    /// </summary>
    /// <param name="query">Search query</param>
    /// <returns>Total schema count</returns>
    Task<int> GetTotalCountAsync(string query = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets appropriate schemas by file type
    /// </summary>
    /// <param name="fileType">File extension (.pdf, .docx, etc.)</param>
    /// <returns>Schemas</returns>
    Task<IEnumerable<MetadataSchema>>
        GetByFileTypeAsync(string fileType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets appropriate schemas by category ID
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <returns>Schemas</returns>
    Task<IEnumerable<MetadataSchema>> GetByCategoryIdAsync(Guid categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active schemas
    /// </summary>
    /// <returns>Active schemas</returns>
    Task<IEnumerable<MetadataSchema>> GetActiveAsync(CancellationToken cancellationToken = default);
}