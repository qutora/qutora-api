namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for password validation of password-protected shares
/// </summary>
public class SharePasswordValidationDto
{
    /// <summary>
    /// Share code
    /// </summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>
    /// Share password
    /// </summary>
    public string Password { get; set; } = string.Empty;
}