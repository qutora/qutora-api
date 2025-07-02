using System.ComponentModel.DataAnnotations;
using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for creating/updating bucket permissions
/// </summary>
public class BucketPermissionCreateDto
{
    /// <summary>
    /// Bucket ID
    /// </summary>
    [Required]
    public Guid BucketId { get; set; }

    /// <summary>
    /// Subject ID (user or role ID)
    /// </summary>
    [Required]
    public string SubjectId { get; set; }

    /// <summary>
    /// Subject type (user or role)
    /// </summary>
    [Required]
    public PermissionSubjectType SubjectType { get; set; }

    /// <summary>
    /// Permission level to be granted
    /// </summary>
    [Required]
    public PermissionLevel Permission { get; set; }
}