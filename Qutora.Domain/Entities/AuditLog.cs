using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;
using Qutora.Domain.Entities.Identity;

namespace Qutora.Domain.Entities;

/// <summary>
/// Audit log to record system events.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// User ID who performed the operation
    /// </summary>
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Date when the operation occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Operation type (e.g., "DocumentVersionCreated", "DocumentVersionRolledBack")
    /// </summary>
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Type of affected entity (e.g., "Document", "DocumentVersion")
    /// </summary>
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of affected entity
    /// </summary>
    [MaxLength(450)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Operation description
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional data (in JSON format)
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// User reference
    /// </summary>
    public virtual ApplicationUser? User { get; set; }
}