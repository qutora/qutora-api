using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

/// <summary>
/// Document repository interface
/// Compliant with Clean Architecture standards
/// </summary>
public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>
    /// Gets documents by category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    Task<IEnumerable<Document>> GetByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document count by category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total document count</returns>
    Task<int> GetByCategoryCountAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches or gets documents
    /// </summary>
    /// <param name="query">Search term, if null all documents are returned</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    Task<IEnumerable<Document>> GetDocumentsAsync(string? query = null, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document count (including search results)
    /// </summary>
    /// <param name="query">Search term, if null all documents are counted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total document count</returns>
    Task<int> GetDocumentsCountAsync(string? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document by ID with details
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document or null</returns>
    Task<Document?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by bucket
    /// </summary>
    /// <param name="bucketId">Bucket ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <param name="query">Optional search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    Task<IEnumerable<Document>> GetByBucketIdAsync(Guid bucketId, int page = 1, int pageSize = 10, string? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document count by bucket
    /// </summary>
    /// <param name="bucketId">Bucket ID</param>
    /// <param name="query">Optional search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total document count</returns>
    Task<int> GetByBucketIdCountAsync(Guid bucketId, string? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by provider
    /// </summary>
    /// <param name="providerId">Storage provider ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <param name="query">Optional search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    Task<IEnumerable<Document>> GetByProviderIdAsync(Guid providerId, int page = 1, int pageSize = 10,
        string? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document count by provider
    /// </summary>
    /// <param name="providerId">Storage provider ID</param>
    /// <param name="query">Optional search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total document count</returns>
    Task<int> GetByProviderIdCountAsync(Guid providerId, string? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents from multiple buckets
    /// </summary>
    /// <param name="bucketIds">List of bucket IDs</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <param name="query">Optional search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    Task<IEnumerable<Document>> GetByBucketIdsAsync(IEnumerable<Guid> bucketIds, int page = 1, int pageSize = 10, 
        string? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document count from multiple buckets
    /// </summary>
    /// <param name="bucketIds">List of bucket IDs</param>
    /// <param name="query">Optional search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total document count</returns>
    Task<int> GetByBucketIdsCountAsync(IEnumerable<Guid> bucketIds, string? query = null, CancellationToken cancellationToken = default);
}