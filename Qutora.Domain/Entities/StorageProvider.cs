using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;

namespace Qutora.Domain.Entities;

/// <summary>
/// Entity that holds storage provider information
/// </summary>
public class StorageProvider : BaseEntity
{
    /// <summary>
    /// Provider name (user-friendly)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider type (minio, s3, filesystem, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>
    /// Is this the default provider?
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Active status
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Provider configuration (in JSON format)
    /// </summary>
    [Required]
    [StringLength(4000)]
    public string ConfigJson { get; set; } = "{}";

    /// <summary>
    /// Maximum file size (bytes) - 0 means unlimited
    /// </summary>
    public long MaxFileSize { get; set; } = 0;
}