namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// Data transfer object for token refresh request
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Current access token (for validation)
    /// </summary>
    public string? AccessToken { get; set; }
}