using System.ComponentModel.DataAnnotations;
using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO used for granting bucket permissions
/// </summary>
public class GrantPermissionDto
{
    /// <summary>
    /// User or role ID to grant permission to
    /// </summary>
    [Required(ErrorMessage = "Subject ID field is required")]
    public string SubjectId { get; set; }

    /// <summary>
    /// Specifies the subject type (User or Role)
    /// </summary>
    [Required(ErrorMessage = "Subject type is required")]
    public PermissionSubjectType SubjectType { get; set; }

    /// <summary>
    /// Permission level to be granted
    /// </summary>
    [Required(ErrorMessage = "Permission level is required")]
    public PermissionLevel Permission { get; set; }
}