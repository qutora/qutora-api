namespace Qutora.Shared.DTOs;

/// <summary>
/// Audit log DTO for data transfer
/// </summary>
public class AuditLogDto
{
    /// <summary>
    /// Audit log unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username for display
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Type of event (e.g., "DocumentCreated", "DocumentDeleted")
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected (e.g., "Document", "User")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the action
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional data in JSON format
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}