namespace Qutora.Shared.DTOs;

/// <summary>
/// Common data transfer object for document versions
/// </summary>
public class DocumentVersionDto
{
    /// <summary>
    /// Version ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Document name
    /// </summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Version number
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// File MIME type
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// File content type (used by UI)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size for display (used by UI)
    /// </summary>
    public string Size => FileSize < 1024 * 1024
        ? $"{FileSize / 1024:N1} KB"
        : $"{FileSize / (1024 * 1024):N1} MB";

    /// <summary>
    /// Storage path
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Created by user ID
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Created by user name
    /// </summary>
    public string CreatedByName { get; set; } = string.Empty;

    /// <summary>
    /// Change description
    /// </summary>
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// Is this version active (current version)?
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Is this version active (used by UI)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Version note
    /// </summary>
    public string Note { get; set; } = string.Empty;
}