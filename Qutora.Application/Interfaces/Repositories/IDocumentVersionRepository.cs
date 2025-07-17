using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for document versions
/// </summary>
public interface IDocumentVersionRepository : IRepository<DocumentVersion>
{
    /// <summary>
    /// Gets all versions of a document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Document versions</returns>
    Task<IEnumerable<DocumentVersion>> GetByDocumentIdAsync(Guid documentId);

    /// <summary>
    /// Gets the latest version number of a document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Latest version number, 0 if none exists</returns>
    Task<int> GetLastVersionNumberAsync(Guid documentId);

    /// <summary>
    /// Gets version details completely (with related entities)
    /// </summary>
    /// <param name="versionId">Version ID</param>
    /// <returns>Version details</returns>
    Task<DocumentVersion?> GetDetailAsync(Guid versionId);
}