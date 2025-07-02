using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;
using Qutora.Shared.Enums;

namespace Qutora.Domain.Entities;

/// <summary>
/// Represents an API key used for authentication with storage services
/// </summary>
public class ApiKey : BaseEntity
{
    /// <summary>
    /// The public identifier for the API key
    /// </summary>
    [MaxLength(450)]
    public string Key { get; set; }

    /// <summary>
    /// The hashed value of the API secret
    /// </summary>
    [MaxLength(500)]
    public string SecretHash { get; set; }

    /// <summary>
    /// The ID of the user who created this API key
    /// </summary>
    [MaxLength(450)]
    public string UserId { get; set; }

    /// <summary>
    /// A friendly name/description for this API key
    /// </summary>
    [MaxLength(200)]
    public string Name { get; set; }

    /// <summary>
    /// Indicates if this API key is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The date and time when this API key expires (null for never)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// The date and time when this API key was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// List of provider IDs this key is allowed to access (empty means all providers)
    /// </summary>
    public ICollection<Guid> AllowedProviderIds { get; set; } = new List<Guid>();

    /// <summary>
    /// Permission level of the API key
    /// </summary>
    public ApiKeyPermission Permissions { get; set; } = ApiKeyPermission.ReadOnly;
}