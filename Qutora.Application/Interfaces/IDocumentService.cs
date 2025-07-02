using Microsoft.AspNetCore.Http;
using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for document operations.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Gets all documents
    /// </summary>
    Task<IEnumerable<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated and filtered documents
    /// </summary>
    Task<PagedDto<DocumentDto>> GetDocumentsAsync(string? query = null, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document by ID
    /// </summary>
    Task<DocumentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by category
    /// </summary>
    Task<PagedDto<DocumentDto>> GetByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents from a specific bucket
    /// </summary>
    Task<PagedDto<DocumentDto>> GetDocumentsByBucketAsync(Guid bucketId, int page = 1, int pageSize = 10,
        string? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents from a specific provider
    /// </summary>
    Task<PagedDto<DocumentDto>> GetDocumentsByProviderAsync(Guid providerId, int page = 1, int pageSize = 10,
        string? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents from multiple buckets (for user accessible documents)
    /// </summary>
    Task<PagedDto<DocumentDto>> GetDocumentsByBucketIdsAsync(IEnumerable<Guid> bucketIds, int page = 1, int pageSize = 10,
        string? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a document with DTO
    /// </summary>
    Task<DocumentDto> CreateAsync(DocumentCreateDto documentDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a document with file and additional information
    /// </summary>
    Task<(DocumentDto Document, DocumentShareDto? Share)> CreateDocumentWithFileAsync(
        IFormFile file,
        string name,
        Guid? categoryId = null,
        Guid? storageProviderId = null,
        string? metadataJson = null,
        string? schemaName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a document with file and additional information (with bucket)
    /// </summary>
    Task<(DocumentDto Document, DocumentShareDto? Share)> CreateDocumentWithFileAsync(
        IFormFile file,
        string name,
        Guid? categoryId = null,
        Guid? storageProviderId = null,
        Guid? bucketId = null,
        string? metadataJson = null,
        string? schemaName = null,
        DocumentShareCreateDto? shareOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a document with ID and DTO
    /// </summary>
    Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto documentDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document content by ID for sharing
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document content with file metadata</returns>
    Task<DocumentContentDto> GetDocumentContentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads document content
    /// </summary>
    Task<Stream> DownloadAsync(Guid id, CancellationToken cancellationToken = default);
}