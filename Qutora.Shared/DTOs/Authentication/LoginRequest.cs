namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// Data transfer object for login request
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; } = string.Empty;
}