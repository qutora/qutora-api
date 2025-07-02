using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Qutora.Domain.Base;
using Qutora.Shared.Enums;

namespace Qutora.Domain.Entities;

/// <summary>
/// Entity that holds bucket-based permissions for API Keys
/// </summary>
public class ApiKeyBucketPermission : BaseEntity
{
    /// <summary>
    /// Associated API Key ID
    /// </summary>
    [Required]
    public Guid ApiKeyId { get; set; }

    /// <summary>
    /// Bucket ID where permission is defined
    /// </summary>
    [Required]
    public Guid BucketId { get; set; }

    /// <summary>
    /// Permission level
    /// </summary>
    [Required]
    public PermissionLevel Permission { get; set; }

    /// <summary>
    /// Associated API Key
    /// </summary>
    [JsonIgnore]
    public virtual ApiKey ApiKey { get; set; }

    /// <summary>
    /// Associated bucket
    /// </summary>
    [JsonIgnore]
    public virtual StorageBucket Bucket { get; set; }
}