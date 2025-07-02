using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs;

/// <summary>
/// API Key bucket permission DTO
/// </summary>
public class ApiKeyBucketPermissionDto
{
    /// <summary>
    /// Permission ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// API Key ID
    /// </summary>
    public Guid ApiKeyId { get; set; }

    /// <summary>
    /// API Key name
    /// </summary>
    public string ApiKeyName { get; set; }

    /// <summary>
    /// Bucket ID
    /// </summary>
    public Guid BucketId { get; set; }

    /// <summary>
    /// Bucket path
    /// </summary>
    public string BucketPath { get; set; }

    /// <summary>
    /// Permission level
    /// </summary>
    public PermissionLevel Permission { get; set; }

    /// <summary>
    /// Permission granted date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who granted the permission
    /// </summary>
    public string CreatedBy { get; set; }

    /// <summary>
    /// Name of user who granted the permission
    /// </summary>
    public string GrantedByName { get; set; }
}