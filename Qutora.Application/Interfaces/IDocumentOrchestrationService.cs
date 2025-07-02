using Qutora.Shared.DTOs.DocumentOrchestration;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service for orchestrating document operations
/// </summary>
public interface IDocumentOrchestrationService
{
    /// <summary>
    /// Creates document with full validation and authorization
    /// </summary>
    Task<DocumentCreateResult> CreateDocumentAsync(DocumentCreateRequest request);
    
    /// <summary>
    /// Updates document with validation and authorization
    /// </summary>
    Task<DocumentUpdateResult> UpdateDocumentAsync(DocumentUpdateRequest request);
    
    /// <summary>
    /// Deletes document with authorization
    /// </summary>
    Task<DocumentDeleteResult> DeleteDocumentAsync(DocumentDeleteRequest request);
    
    /// <summary>
    /// Downloads document with authorization
    /// </summary>
    Task<DocumentDownloadResult> DownloadDocumentAsync(DocumentDownloadRequest request);
} 