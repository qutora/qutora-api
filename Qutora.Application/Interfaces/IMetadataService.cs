using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for document metadata operations
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Gets metadata information for a document
    /// </summary>
    Task<MetadataDto?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata information for a document
    /// </summary>
    Task<MetadataDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates new metadata
    /// </summary>
    Task<MetadataDto> CreateAsync(Guid documentId, CreateUpdateMetadataDto createMetadataDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates existing metadata
    /// </summary>
    Task<MetadataDto> UpdateAsync(Guid documentId, CreateUpdateMetadataDto updateMetadataDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes metadata
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document metadata with specific tags in paginated format
    /// </summary>
    Task<PagedDto<MetadataDto>> GetByTagsAsync(string[] tags, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches documents by metadata values in paginated format
    /// </summary>
    Task<PagedDto<MetadataDto>> SearchAsync(Dictionary<string, object> searchCriteria, int page = 1,
        int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates metadata
    /// </summary>
    Task<Dictionary<string, string>> ValidateMetadataAsync(string schemaName,
        Dictionary<string, object> metadata, CancellationToken cancellationToken = default);
}