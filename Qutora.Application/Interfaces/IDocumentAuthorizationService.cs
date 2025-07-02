using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.DocumentOrchestration;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service for document authorization operations
/// </summary>
public interface IDocumentAuthorizationService
{
    /// <summary>
    /// Checks if user can create document with given parameters
    /// </summary>
    Task<DocumentAuthorizationResult> CanCreateDocumentAsync(string userId, Guid? providerId, Guid? bucketId);
    
    /// <summary>
    /// Checks if user can access document
    /// </summary>
    Task<DocumentAuthorizationResult> CanAccessDocumentAsync(string userId, DocumentDto document);
    
    /// <summary>
    /// Checks if user can update document
    /// </summary>
    Task<DocumentAuthorizationResult> CanUpdateDocumentAsync(string userId, DocumentDto document, UpdateDocumentDto update);
    
    /// <summary>
    /// Checks if user can delete document
    /// </summary>
    Task<DocumentAuthorizationResult> CanDeleteDocumentAsync(string userId, DocumentDto document);
    
    /// <summary>
    /// Validates if user can access document based on provider status (inactive provider check)
    /// </summary>
    Task<DocumentAuthorizationResult> ValidateProviderAccessAsync(DocumentDto document, System.Security.Claims.ClaimsPrincipal user);
    
    /// <summary>
    /// Validates if user can access document based on provider status (inactive provider check)
    /// </summary>
    Task<DocumentAuthorizationResult> ValidateProviderAccessAsync(Guid documentId, System.Security.Claims.ClaimsPrincipal user);
} 