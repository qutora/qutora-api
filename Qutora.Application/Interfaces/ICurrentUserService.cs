using System.Security.Claims;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for accessing the currently logged-in user
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Identity ID of the logged-in user
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Whether the user is logged in
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Claims containing user identity information
    /// </summary>
    ClaimsPrincipal User { get; }

    /// <summary>
    /// Returns the user's roles
    /// </summary>
    IEnumerable<string> GetRoles();

    /// <summary>
    /// Checks whether the user has a specific role
    /// </summary>
    bool IsInRole(string role);
}