namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for storage provider information
/// </summary>
public class StorageProviderDto
{
    /// <summary>
    /// Provider ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Provider name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Provider type (S3, LocalStorage, etc.)
    /// </summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>
    /// Provider configuration information (in JSON format)
    /// </summary>
    public string ConfigJson { get; set; } = string.Empty;

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Is default provider
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Maximum file size in bytes (0 = unlimited)
    /// </summary>
    public long MaxFileSize { get; set; } = 0;
}