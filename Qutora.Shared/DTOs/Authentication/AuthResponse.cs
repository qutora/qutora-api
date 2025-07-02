namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// Data transfer object for authentication response
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Is operation successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message (error message etc.)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// JWT access token
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Refresh token
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiration time (Unix time)
    /// </summary>
    public long? ExpiresAt { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    public UserDto? User { get; set; }
}