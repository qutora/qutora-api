using Microsoft.AspNetCore.Http;
using Qutora.Shared.DTOs.DocumentOrchestration;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service for document validation operations
/// </summary>
public interface IDocumentValidationService
{
    /// <summary>
    /// Validates document creation inputs and user permissions
    /// </summary>
    Task ValidateDocumentCreationAsync(IFormFile file, string name, Guid? storageProviderId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Validates file with all applicable rules
    /// </summary>
    Task<ValidationResult> ValidateFileAsync(IFormFile file, Guid? providerId = null, string? metadataSchemaId = null);
    
    /// <summary>
    /// Validates file size against provider limits
    /// </summary>
    Task<ValidationResult> ValidateFileSizeAsync(IFormFile file, Guid providerId);
    
    /// <summary>
    /// Validates file type against metadata schema restrictions
    /// </summary>
    Task<ValidationResult> ValidateFileTypeAsync(IFormFile file, string metadataSchemaId);
    
    /// <summary>
    /// Validates metadata JSON against schema
    /// </summary>
    Task<ValidationResult> ValidateMetadataAsync(string? metadataJson, string? schemaName);
} 