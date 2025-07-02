using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;

namespace Qutora.Domain.Entities;

/// <summary>
/// Represents a specific version of a document.
/// </summary>
public class DocumentVersion : BaseEntity
{
    /// <summary>
    /// Associated document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Associated document
    /// </summary>
    public virtual Document Document { get; set; } = null!;

    /// <summary>
    /// Version number (starts from 1)
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Storage path
    /// </summary>
    [MaxLength(1000)]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// File size (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// File content type (MIME)
    /// </summary>
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;



    /// <summary>
    /// Change description
    /// </summary>
    [MaxLength(1000)]
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// File hash value
    /// </summary>
    [MaxLength(128)]
    public string? Hash { get; set; }
}