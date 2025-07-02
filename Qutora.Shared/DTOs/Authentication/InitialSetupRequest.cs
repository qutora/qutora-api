namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// Data transfer object for initial setup
/// </summary>
public class InitialSetupRequest
{
    /// <summary>
    /// Administrator email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Administrator password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Administrator first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Administrator last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Organization/company name
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;
}