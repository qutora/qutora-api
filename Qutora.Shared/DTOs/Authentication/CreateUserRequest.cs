using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// Data transfer object for new user creation request
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Email address (also used as username)
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User first name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Roles to be assigned to the user
    /// </summary>
    [Required(ErrorMessage = "At least one role must be assigned")]
    [MinLength(1, ErrorMessage = "At least one role must be assigned")]
    public List<string> Roles { get; set; } = new();
}