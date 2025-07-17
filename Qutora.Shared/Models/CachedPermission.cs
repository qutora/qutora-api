using Qutora.Shared.Enums;

namespace Qutora.Shared.Models;

/// <summary>
/// Cached version of API Key bucket permission
/// </summary>
public class CachedPermission
{
    /// <summary>
    /// Permission unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// API Key identifier
    /// </summary>
    public Guid ApiKeyId { get; set; }

    /// <summary>
    /// Bucket identifier
    /// </summary>
    public Guid BucketId { get; set; }

    /// <summary>
    /// Permission level granted
    /// </summary>
    public PermissionLevel Permission { get; set; }

    /// <summary>
    /// When this permission was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who granted this permission
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// When this entry was cached
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Checks if this permission satisfies the required level
    /// </summary>
    public bool SatisfiesRequirement(PermissionLevel required)
    {
        return Permission switch
        {
            PermissionLevel.Admin => true,
            PermissionLevel.Delete when required is PermissionLevel.Read or PermissionLevel.Write or PermissionLevel.ReadWrite => true,
            PermissionLevel.ReadWrite when required is PermissionLevel.Read or PermissionLevel.Write => true,
            _ => Permission == required
        };
    }
} 