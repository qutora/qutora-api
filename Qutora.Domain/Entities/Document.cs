using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;

namespace Qutora.Domain.Entities;

/// <summary>
/// Represents a document stored in the system.
/// </summary>
public class Document : BaseEntity
{
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }

    public long FileSizeBytes => FileSize;
    
    [MaxLength(1000)]
    public string StoragePath { get; set; } = string.Empty;
    
    [MaxLength(128)]
    public string Hash { get; set; } = string.Empty;
    
    public Guid? CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    public Guid StorageProviderId { get; set; }
    public virtual StorageProvider? StorageProvider { get; set; }

    public DateTime? LastAccessedAt { get; set; }
    public new bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual Metadata? Metadata { get; set; }

    public Guid? CurrentVersionId { get; set; }
    public virtual DocumentVersion? CurrentVersion { get; set; }

    public virtual ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

    public Guid? BucketId { get; set; }

    public virtual StorageBucket? Bucket { get; set; }


}