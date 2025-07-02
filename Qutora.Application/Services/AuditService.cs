using System.Text.Json;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.UnitOfWork;
using Qutora.Shared.DTOs;

namespace Qutora.Application.Services;

/// <summary>
/// Audit log service implementation
/// </summary>
public class AuditService(
    IUnitOfWork unitOfWork,
    ILogger<AuditService> logger,
    ICurrentUserService currentUserService,
    IMapper mapper)
    : IAuditService
{
    /// <summary>
    /// Adds an audit log entry
    /// </summary>
    public async Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding audit log entry");
        }
    }

    /// <inheritdoc/>
    public async Task LogApiRequestAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding API request audit log");
        }
    }

    /// <inheritdoc/>
    public async Task LogActivityAsync(
        string entityType,
        string entityId,
        string action,
        string details,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = currentUserService?.UserId ?? "system";

            var auditLog = new AuditLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                EventType = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = details,
                Data = additionalData != null
                    ? JsonSerializer.Serialize(additionalData)
                    : "{}"
            };

            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);
                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error occurred while adding activity log. EntityType: {EntityType}, EntityId: {EntityId}, Action: {Action}",
                entityType, entityId, action);
        }
    }

    /// <inheritdoc/>
    public async Task LogDocumentVersionCreatedAsync(
        string userId,
        Guid documentId,
        Guid versionId,
        int versionNumber,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);
            var documentName = document?.Name ?? documentId.ToString();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                EventType = "DocumentVersionCreated",
                EntityType = "DocumentVersion",
                EntityId = versionId.ToString(),
                Description = $"New version ({versionNumber}) created for document '{documentName}'.",
                Data = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };

            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error occurred while adding document version creation audit log. DocumentId: {DocumentId}, VersionId: {VersionId}",
                documentId, versionId);
        }
    }

    /// <inheritdoc/>
    public async Task LogDocumentVersionRolledBackAsync(
        string userId,
        Guid documentId,
        Guid versionId,
        int versionNumber,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);
            var documentName = document?.Name ?? documentId.ToString();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                EventType = "DocumentVersionRolledBack",
                EntityType = "DocumentVersion",
                EntityId = versionId.ToString(),
                Description = $"Document '{documentName}' rolled back to version {versionNumber}.",
                Data = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };

            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error occurred while adding document version rollback audit log. DocumentId: {DocumentId}, VersionId: {VersionId}",
                documentId, versionId);
        }
    }

    /// <inheritdoc/>
    public async Task LogDocumentCreatedAsync(
        string userId,
        Guid documentId,
        string documentName,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                EventType = "DocumentCreated",
                EntityType = "Document",
                EntityId = documentId.ToString(),
                Description = $"Document '{documentName}' created.",
                Data = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };

            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding document creation audit log. DocumentId: {DocumentId}",
                documentId);
        }
    }

    /// <inheritdoc/>
    public async Task LogDocumentUpdatedAsync(
        string userId,
        Guid documentId,
        string documentName,
        Dictionary<string, object> changes,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                EventType = "DocumentUpdated",
                EntityType = "Document",
                EntityId = documentId.ToString(),
                Description = $"Document '{documentName}' updated.",
                Data = JsonSerializer.Serialize(new
                {
                    Changes = changes,
                    AdditionalData = additionalData
                })
            };

            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding document update audit log. DocumentId: {DocumentId}",
                documentId);
        }
    }

    /// <inheritdoc/>
    public async Task LogDocumentDeletedAsync(
        string userId,
        Guid documentId,
        string documentName,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                EventType = "DocumentDeleted",
                EntityType = "Document",
                EntityId = documentId.ToString(),
                Description = $"Document '{documentName}' deleted.",
                Data = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };

            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding document deletion audit log. DocumentId: {DocumentId}",
                documentId);
        }
    }

    /// <inheritdoc/>
    public async Task LogUserActionAsync(
        string userId,
        string targetUserId,
        string action,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                EventType = $"User_{action}",
                EntityType = "User",
                EntityId = targetUserId,
                Description = $"User action: {action}",
                Data = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };

            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error occurred while adding user action audit log. UserId: {UserId}, Action: {Action}", userId,
                action);
        }
    }

    /// <inheritdoc/>
    public async Task LogSettingsChangedAsync(
        string userId,
        string settingName,
        string oldValue,
        string newValue,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                EventType = "SettingsChanged",
                EntityType = "SystemSettings",
                EntityId = settingName,
                Description = $"System setting changed: {settingName}",
                Data = JsonSerializer.Serialize(new
                {
                    OldValue = oldValue,
                    NewValue = newValue,
                    AdditionalData = additionalData
                })
            };

            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding settings change audit log. SettingName: {SettingName}",
                settingName);
        }
    }

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    public async Task<IEnumerable<AuditLogDto>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var allLogs = await unitOfWork.AuditLogs.GetByUserIdAsync(userId);

        var pagedLogs = allLogs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return mapper.Map<IEnumerable<AuditLogDto>>(pagedLogs);
    }

    /// <summary>
    /// Gets API Key activities
    /// </summary>
    public async Task<(IEnumerable<AuditLogDto> Activities, int TotalCount)> GetApiKeyActivitiesAsync(
        string apiKeyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var activities = await unitOfWork.AuditLogs.GetByEntityAsync("API_KEY_Request", apiKeyId);

        if (startDate.HasValue || endDate.HasValue)
            activities = activities.Where(log =>
                (!startDate.HasValue || log.Timestamp >= startDate.Value) &&
                (!endDate.HasValue || log.Timestamp <= endDate.Value)
            ).ToList();

        var totalCount = activities.Count();

        var pagedActivities = activities
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var activitiesDto = mapper.Map<IEnumerable<AuditLogDto>>(pagedActivities);

        return (activitiesDto, totalCount);
    }
}
