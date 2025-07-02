using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for bucket permission check results
/// </summary>
public class BucketPermissionCheckResultDto
{
    /// <summary>
    /// Whether the user has the required permission
    /// </summary>
    public bool HasPermission { get; set; }

    /// <summary>
    /// The actual permission level the user has (if any)
    /// </summary>
    public PermissionLevel? UserPermissionLevel { get; set; }

    /// <summary>
    /// The requested permission level for the check
    /// </summary>
    public PermissionLevel RequiredPermissionLevel { get; set; }

    /// <summary>
    /// Additional information about the permission check
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Bucket ID that was checked
    /// </summary>
    public Guid BucketId { get; set; }

    /// <summary>
    /// User ID that was checked
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// API Key ID if the check was for an API key
    /// </summary>
    public Guid? ApiKeyId { get; set; }
} 