namespace Qutora.Shared.DTOs.Common;

/// <summary>
/// DTO representing the capabilities of a storage provider
/// </summary>
public class StorageProviderCapabilitiesDto
{
    /// <summary>
    /// Provider identifier
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Support for bucket creation
    /// </summary>
    public bool SupportsCreateBucket { get; set; } = true;

    /// <summary>
    /// Support for bucket deletion
    /// </summary>
    public bool SupportsDeleteBucket { get; set; } = true;

    /// <summary>
    /// Support for bucket listing
    /// </summary>
    public bool SupportsListBuckets { get; set; } = true;

    /// <summary>
    /// Support for checking bucket existence
    /// </summary>
    public bool SupportsCheckBucketExists { get; set; } = true;

    /// <summary>
    /// Support for force deletion even if bucket content is not empty
    /// </summary>
    public bool SupportsForceDelete { get; set; } = true;

    /// <summary>
    /// Support for object metadata
    /// </summary>
    public bool SupportsObjectMetadata { get; set; } = false;

    /// <summary>
    /// Support for object versioning
    /// </summary>
    public bool SupportsObjectVersioning { get; set; } = false;

    /// <summary>
    /// Support for nested buckets
    /// </summary>
    public bool SupportsNestedBuckets { get; set; } = false;
}