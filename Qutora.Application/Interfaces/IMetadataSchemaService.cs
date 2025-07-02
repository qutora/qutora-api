using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for metadata schema operations
/// </summary>
public interface IMetadataSchemaService
{
    /// <summary>
    /// Gets all schemas in paginated format
    /// </summary>
    Task<PagedDto<MetadataSchemaDto>> GetAllAsync(int page = 1, int pageSize = 10, string query = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all schemas in simple format (ID, name, category information)
    /// </summary>
    Task<IEnumerable<MetadataSchemaDto>> GetAllSchemasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schema by ID
    /// </summary>
    Task<MetadataSchemaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schema by name
    /// </summary>
    Task<MetadataSchemaDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new schema
    /// </summary>
    Task<MetadataSchemaDto> CreateAsync(CreateUpdateMetadataSchemaDto createSchemaDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing schema
    /// </summary>
    Task<MetadataSchemaDto> UpdateAsync(Guid id, CreateUpdateMetadataSchemaDto updateSchemaDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a schema
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suitable schemas by file type
    /// </summary>
    Task<IEnumerable<MetadataSchemaDto>> GetByFileTypeAsync(string fileType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suitable schemas by category ID
    /// </summary>
    Task<IEnumerable<MetadataSchemaDto>> GetByCategoryIdAsync(Guid categoryId,
        CancellationToken cancellationToken = default);
}