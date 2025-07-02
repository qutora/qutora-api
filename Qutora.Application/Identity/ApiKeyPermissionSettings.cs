using Qutora.Shared.Enums;

namespace Qutora.Application.Identity;

/// <summary>
/// Class that holds API Key permission settings
/// </summary>
public class ApiKeyPermissionSettings
{
    /// <summary>
    /// Permissions for ReadOnly permission type
    /// </summary>
    public List<string> ReadOnly { get; set; } = new();

    /// <summary>
    /// Permissions for ReadWrite permission type
    /// </summary>
    public List<string> ReadWrite { get; set; } = new();

    /// <summary>
    /// Permissions for FullAccess permission type
    /// </summary>
    public List<string> FullAccess { get; set; } = new();

    /// <summary>
    /// Returns permissions for the given permission type
    /// </summary>
    public List<string> GetPermissionsForType(ApiKeyPermission permissionType)
    {
        return permissionType switch
        {
            ApiKeyPermission.ReadOnly => ReadOnly,
            ApiKeyPermission.ReadWrite => ReadWrite,
            ApiKeyPermission.FullAccess => FullAccess,
            _ => new List<string>()
        };
    }
}
