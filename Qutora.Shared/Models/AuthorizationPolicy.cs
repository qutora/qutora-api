namespace Qutora.Shared.Models;

/// <summary>
/// Authorization policy
/// </summary>
public class AuthorizationPolicy
{
    public string Name { get; set; } = string.Empty;
    public bool RequiresAuthenticatedUser { get; set; }
    public List<string>? RequiredPermissions { get; set; }
    public List<string>? AuthenticationSchemes { get; set; }
}
