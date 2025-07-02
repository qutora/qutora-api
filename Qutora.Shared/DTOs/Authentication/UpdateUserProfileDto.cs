using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// User profile update DTO
/// </summary>
public class UpdateUserProfileDto
{
    /// <summary>
    /// First name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name can be at most 50 characters")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name can be at most 50 characters")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email (also username)
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(100, ErrorMessage = "Email can be at most 100 characters")]
    public string Email { get; set; } = string.Empty;
} 