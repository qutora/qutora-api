namespace Qutora.Infrastructure.Caching.Models;

/// <summary>
/// Cached version of storage bucket for fast provider lookups
/// </summary>
public class CachedBucket
{
    /// <summary>
    /// Bucket unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Bucket path/name
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Storage provider identifier
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Whether the bucket is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether this is the default bucket for the provider
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Bucket description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When this entry was cached
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Checks if bucket is available for operations
    /// </summary>
    public bool IsAvailable => IsActive;
} 