using Qutora.Domain.Entities;

namespace Qutora.Infrastructure.Interfaces.Repositories;

/// <summary>
/// Repository interface for metadata entities
/// </summary>
public interface IMetadataRepository : IRepository<Metadata>
{
    /// <summary>
    /// Gets metadata information belonging to a document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Metadata belonging to the document</returns>
    Task<Metadata?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata records with specific tags
    /// </summary>
    /// <param name="tags">Array of tags</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <returns>Metadata records with the tags</returns>
    Task<IEnumerable<Metadata>> GetByTagsAsync(string[] tags, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of metadata records with specific tags
    /// </summary>
    /// <param name="tags">Array of tags</param>
    /// <returns>Record count</returns>
    Task<int> GetByTagsCountAsync(string[] tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches based on metadata values
    /// </summary>
    /// <param name="searchCriteria">Search criteria (key-value pairs)</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <returns>Metadata records matching the search criteria</returns>
    Task<IEnumerable<Metadata>> SearchAsync(Dictionary<string, object> searchCriteria, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets search result count based on metadata values
    /// </summary>
    /// <param name="searchCriteria">Search criteria (key-value pairs)</param>
    /// <returns>Search result count</returns>
    Task<int> SearchCountAsync(Dictionary<string, object> searchCriteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata information with document details
    /// </summary>
    /// <param name="id">Metadata ID</param>
    /// <returns>Metadata with document information</returns>
    Task<Metadata?> GetByIdWithDocumentDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}