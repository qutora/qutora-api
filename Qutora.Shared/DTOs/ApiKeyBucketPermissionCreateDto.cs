using System.ComponentModel.DataAnnotations;
using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for creating/updating bucket permissions for API Key
/// </summary>
public class ApiKeyBucketPermissionCreateDto
{
    /// <summary>
    /// API Key ID
    /// </summary>
    [Required]
    public Guid ApiKeyId { get; set; }

    /// <summary>
    /// Bucket ID
    /// </summary>
    [Required]
    public Guid BucketId { get; set; }

    /// <summary>
    /// Permission level to be granted
    /// </summary>
    [Required]
    public PermissionLevel Permission { get; set; }
}