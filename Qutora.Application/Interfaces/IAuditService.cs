using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for audit logging operations.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Adds an audit log entry
    /// </summary>
    Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an API request audit log entry.
    /// </summary>
    /// <param name="auditLog">Audit log entry</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogApiRequestAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a general activity log entry
    /// </summary>
    /// <param name="entityType">Type of entity being operated on</param>
    /// <param name="entityId">ID of entity being operated on</param>
    /// <param name="action">Type of operation</param>
    /// <param name="details">Operation details</param>
    /// <param name="additionalData">Additional information (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogActivityAsync(
        string entityType,
        string entityId,
        string action,
        string details,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an audit log entry for document version creation.
    /// </summary>
    /// <param name="userId">ID of user performing the operation</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="versionId">Created version ID</param>
    /// <param name="versionNumber">Version number</param>
    /// <param name="additionalData">Additional information (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogDocumentVersionCreatedAsync(
        string userId,
        Guid documentId,
        Guid versionId,
        int versionNumber,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an audit log entry for document version rollback.
    /// </summary>
    /// <param name="userId">ID of user performing the operation</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="versionId">Rolled back version ID</param>
    /// <param name="versionNumber">Version number</param>
    /// <param name="additionalData">Additional information (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogDocumentVersionRolledBackAsync(
        string userId,
        Guid documentId,
        Guid versionId,
        int versionNumber,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an audit log entry for document creation.
    /// </summary>
    /// <param name="userId">ID of user performing the operation</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="documentName">Document name</param>
    /// <param name="additionalData">Additional information (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogDocumentCreatedAsync(
        string userId,
        Guid documentId,
        string documentName,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an audit log entry for document update.
    /// </summary>
    /// <param name="userId">ID of user performing the operation</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="documentName">Document name</param>
    /// <param name="changes">Changes made</param>
    /// <param name="additionalData">Additional information (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogDocumentUpdatedAsync(
        string userId,
        Guid documentId,
        string documentName,
        Dictionary<string, object> changes,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an audit log entry for document deletion.
    /// </summary>
    /// <param name="userId">ID of user performing the operation</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="documentName">Document name</param>
    /// <param name="additionalData">Additional information (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogDocumentDeletedAsync(
        string userId,
        Guid documentId,
        string documentName,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an audit log entry for user operations.
    /// </summary>
    /// <param name="userId">ID of user performing the operation</param>
    /// <param name="targetUserId">Target user ID</param>
    /// <param name="action">Type of operation (create, update, delete, etc.)</param>
    /// <param name="additionalData">Additional information (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogUserActionAsync(
        string userId,
        string targetUserId,
        string action,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an audit log entry for system settings changes.
    /// </summary>
    /// <param name="userId">ID of user performing the operation</param>
    /// <param name="settingName">Setting name</param>
    /// <param name="oldValue">Old value</param>
    /// <param name="newValue">New value</param>
    /// <param name="additionalData">Additional information (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogSettingsChangedAsync(
        string userId,
        string settingName,
        string oldValue,
        string newValue,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    Task<IEnumerable<AuditLogDto>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets API Key activities
    /// </summary>
    /// <param name="apiKeyId">API Key ID</param>
    /// <param name="startDate">Start date (optional)</param>
    /// <param name="endDate">End date (optional)</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API Key activity logs and total count</returns>
    Task<(IEnumerable<AuditLogDto> Activities, int TotalCount)> GetApiKeyActivitiesAsync(
        string apiKeyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}