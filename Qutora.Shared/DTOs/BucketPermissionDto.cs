using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO carrying bucket permissions
/// </summary>
public class BucketPermissionDto
{
    /// <summary>
    /// Permission ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Bucket ID
    /// </summary>
    public Guid BucketId { get; set; }

    /// <summary>
    /// Bucket path
    /// </summary>
    public string BucketPath { get; set; }

    /// <summary>
    /// Permission holder (user or role) ID
    /// </summary>
    public string SubjectId { get; set; }

    /// <summary>
    /// Permission holder's display name (User name or role name)
    /// </summary>
    public string SubjectName { get; set; }

    /// <summary>
    /// Permission subject type (User or Role)
    /// </summary>
    public PermissionSubjectType SubjectType { get; set; }

    /// <summary>
    /// Permission level
    /// </summary>
    public PermissionLevel Permission { get; set; }

    /// <summary>
    /// User ID who created the permission
    /// </summary>
    public string CreatedBy { get; set; }

    /// <summary>
    /// Name of user who created the permission
    /// </summary>
    public string GrantedByName { get; set; }

    /// <summary>
    /// Permission creation date
    /// </summary>
    public DateTime GrantedAt { get; set; }
}