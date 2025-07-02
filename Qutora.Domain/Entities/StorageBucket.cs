using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Qutora.Domain.Base;

namespace Qutora.Domain.Entities;

/// <summary>
/// Entity that holds bucket/folder information within storage provider
/// </summary>
public class StorageBucket : BaseEntity
{
    /// <summary>
    /// Bucket/folder path within provider
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Bucket description
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Associated storage provider ID
    /// </summary>
    [Required]
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Is this the default bucket/folder for the provider?
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Is it active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Is bucket public?
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Is direct access endpoint allowed for this bucket?
    /// If this flag is true, files in this bucket can be accessed via direct URL
    /// </summary>
    public bool AllowDirectAccess { get; set; } = false;

    /// <summary>
    /// Associated storage provider
    /// </summary>
    [JsonIgnore]
    public virtual StorageProvider? Provider { get; set; }

    /// <summary>
    /// Documents belonging to this bucket
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    /// <summary>
    /// Permissions defined for this bucket
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<BucketPermission> Permissions { get; set; } = new List<BucketPermission>();
}