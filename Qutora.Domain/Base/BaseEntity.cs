using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Entities.Identity;

namespace Qutora.Domain.Base;

/// <summary>
/// Base entity class with concurrency control and audit fields
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Entity ID
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User ID who created this entity
    /// </summary>
    [StringLength(128)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User ID who last updated this entity
    /// </summary>
    [StringLength(128)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Navigation property for the user who created this entity
    /// </summary>
    public virtual ApplicationUser? CreatedByUser { get; set; }

    /// <summary>
    /// Navigation property for the user who last updated this entity
    /// </summary>
    public virtual ApplicationUser? UpdatedByUser { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; }
}