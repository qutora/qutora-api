namespace Qutora.Shared.DTOs;

/// <summary>
/// Data transfer object for document information
/// </summary>
public class DocumentDto
{
    /// <summary>
    /// Document ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Document name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Document description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// File size for display
    /// </summary>
    public string Size => FileSize < 1024 * 1024
        ? $"{FileSize / 1024:N1} KB"
        : $"{FileSize / (1024 * 1024):N1} MB";

    /// <summary>
    /// Storage path
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// File hash value
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Storage provider ID
    /// </summary>
    public Guid StorageProviderId { get; set; }

    /// <summary>
    /// Storage provider name
    /// </summary>
    public string? StorageProviderName { get; set; }

    /// <summary>
    /// Bucket ID
    /// </summary>
    public Guid? BucketId { get; set; }

    /// <summary>
    /// Bucket path
    /// </summary>
    public string? BucketPath { get; set; }

    /// <summary>
    /// Category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Last access date
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Modification date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Created by user ID
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Created by user name
    /// </summary>
    public string? CreatedByName { get; set; }

    /// <summary>
    /// Modified by user
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Is document deleted?
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Deletion date
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Current version ID
    /// </summary>
    public Guid? CurrentVersionId { get; set; }

    /// <summary>
    /// Document metadata information
    /// </summary>
    public MetadataDto? Metadata { get; set; }

    /// <summary>
    /// Document share information
    /// </summary>
    public List<Guid>? ShareIds { get; set; }

    /// <summary>
    /// File extension
    /// </summary>
    public string FileExtension =>
        !string.IsNullOrEmpty(FileName) && FileName.Contains('.')
            ? FileName.Substring(FileName.LastIndexOf('.')).ToLower()
            : string.Empty;


}