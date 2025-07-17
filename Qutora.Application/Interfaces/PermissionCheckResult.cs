using Qutora.Shared.Enums;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Permission check result
/// </summary>
public class PermissionCheckResult
{
    /// <summary>
    /// Is permission granted?
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Reason if permission is denied
    /// </summary>
    public string? DeniedReason { get; set; }

    /// <summary>
    /// Required permission level
    /// </summary>
    public PermissionLevel RequiredPermission { get; set; }

    /// <summary>
    /// User's current permission level
    /// </summary>
    public PermissionLevel UserPermission { get; set; }
}