namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// Data transfer object for user information
/// </summary>
public class UserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User creation date
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// User last login date
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// User permissions/authorities
    /// </summary>
    public List<string> Permissions { get; set; } = new();


    public string FullName => FirstName + " " + LastName;
}