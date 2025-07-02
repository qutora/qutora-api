using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for document versioning operations.
/// </summary>
public interface IDocumentVersionService
{
    /// <summary>
    /// Creates a new document version.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="fileStream">File content</param>
    /// <param name="fileName">File name</param>
    /// <param name="contentType">File content type</param>
    /// <param name="userId">ID of the user performing the operation</param>
    /// <param name="changeDescription">Change description (optional)</param>
    /// <returns>Created version information</returns>
    Task<DocumentVersionDto> CreateNewVersionAsync(
        Guid documentId,
        Stream fileStream,
        string fileName,
        string contentType,
        string userId,
        string? changeDescription = null);

    /// <summary>
    /// Lists all versions of a document.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Version list</returns>
    Task<IEnumerable<DocumentVersionDto>> GetVersionsAsync(Guid documentId);

    /// <summary>
    /// Downloads a specific document version.
    /// </summary>
    /// <param name="versionId">Version ID</param>
    /// <returns>Download result</returns>
    Task<DocumentVersionDownloadDto> DownloadVersionAsync(Guid versionId);

    /// <summary>
    /// Reverts a document to a specific version.
    /// </summary>
    /// <param name="versionId">Version ID</param>
    /// <param name="userId">ID of the user performing the operation</param>
    /// <returns>Rollback operation result</returns>
    Task<DocumentVersionDto> RollbackToVersionAsync(Guid versionId, string userId);

    /// <summary>
    /// Gets version details.
    /// </summary>
    /// <param name="versionId">Version ID</param>
    /// <returns>Version details</returns>
    Task<DocumentVersionDto> GetVersionDetailAsync(Guid versionId);
}