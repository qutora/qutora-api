namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// User profile information DTO
/// </summary>
public class UserProfileDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email (also username)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Account creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last login date
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Is account active?
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Combined first and last name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
} 