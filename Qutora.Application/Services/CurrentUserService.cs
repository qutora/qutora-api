using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Interfaces;

namespace Qutora.Application.Services;

/// <summary>
/// Service that provides user information from the current HTTP request
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    /// <summary>
    /// ID of the current logged-in user
    /// </summary>
    public string UserId =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    /// <summary>
    /// Whether the user is authenticated
    /// </summary>
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// User claims principal object
    /// </summary>
    public ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    /// <summary>
    /// Returns the user's roles
    /// </summary>
    public IEnumerable<string> GetRoles()
    {
        return User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value) ?? [];
    }

    /// <summary>
    /// Checks whether the user has a specific role
    /// </summary>
    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }
}
