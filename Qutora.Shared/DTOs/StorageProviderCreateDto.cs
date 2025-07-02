using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs;

/// <summary>
/// Storage provider creation DTO
/// </summary>
public class StorageProviderCreateDto
{
    /// <summary>
    /// Provider name
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Provider type (filesystem, minio, ftp, sftp)
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string ProviderType { get; set; }

    /// <summary>
    /// Provider description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Is default provider?
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Provider configuration (in JSON format)
    /// </summary>
    [Required]
    public string ConfigJson { get; set; } = "{}";

    /// <summary>
    /// Dictionary for custom configuration values (used to create configJson)
    /// </summary>
    public Dictionary<string, object?> ConfigurationValues { get; set; } = new();

    /// <summary>
    /// Maximum file size in bytes (0 = unlimited)
    /// </summary>
    public long MaxFileSize { get; set; } = 0;
}