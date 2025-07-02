using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Qutora.Domain.Base;
using Qutora.Shared.Enums;

namespace Qutora.Domain.Entities;

/// <summary>
/// Entity that holds bucket/folder permissions
/// </summary>
public class BucketPermission : BaseEntity
{
    /// <summary>
    /// Bucket ID where permission is defined
    /// </summary>
    [Required]
    public Guid BucketId { get; set; }

    /// <summary>
    /// Subject ID with permission (user or role)
    /// </summary>
    [Required]
    [StringLength(128)]
    public string SubjectId { get; set; }

    /// <summary>
    /// Subject type (user or role)
    /// </summary>
    [Required]
    public PermissionSubjectType SubjectType { get; set; }

    /// <summary>
    /// Permission level
    /// </summary>
    [Required]
    public PermissionLevel Permission { get; set; }

    /// <summary>
    /// Associated bucket
    /// </summary>
    [JsonIgnore]
    public virtual StorageBucket Bucket { get; set; }
}