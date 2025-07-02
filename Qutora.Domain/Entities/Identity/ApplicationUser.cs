using Microsoft.AspNetCore.Identity;

namespace Qutora.Domain.Entities.Identity;

/// <summary>
/// Custom user class for ASP.NET Identity
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }

    public virtual ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}