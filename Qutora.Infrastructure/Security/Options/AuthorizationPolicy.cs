namespace Qutora.Infrastructure.Security.Options;

/// <summary>
/// Authorization policy configuration class
/// </summary>
public class AuthorizationPolicy
{
    /// <summary>
    /// Policy name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Required permissions for the policy
    /// </summary>
    public List<string>? RequiredPermissions { get; set; }

    /// <summary>
    /// Required roles for the policy
    /// </summary>
    public List<string>? RequiredRoles { get; set; }
}
