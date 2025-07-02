using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// Create role request DTO
/// </summary>
public class CreateRoleRequest
{
    /// <summary>
    /// Role name
    /// </summary>
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string? Description { get; set; }
} 