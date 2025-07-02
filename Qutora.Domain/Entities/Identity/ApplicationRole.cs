using Microsoft.AspNetCore.Identity;

namespace Qutora.Domain.Entities.Identity;

/// <summary>
/// Custom role class for ASP.NET Identity
/// </summary>
public class ApplicationRole : IdentityRole
{
    public string Description { get; set; } = string.Empty;

    public ApplicationRole() : base()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    public ApplicationRole(string roleName, string description) : base(roleName)
    {
        Description = description;
    }
}