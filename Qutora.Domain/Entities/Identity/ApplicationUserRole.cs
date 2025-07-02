using Microsoft.AspNetCore.Identity;

namespace Qutora.Domain.Entities.Identity;

/// <summary>
/// User-role relationship class for ASP.NET Identity
/// </summary>
public class ApplicationUserRole : IdentityUserRole<string>
{
    public virtual ApplicationUser User { get; set; }
    public virtual ApplicationRole Role { get; set; }
}