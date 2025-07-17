namespace Qutora.Shared.Enums;

/// <summary>
/// Permission level
/// </summary>
public enum PermissionLevel
{
    /// <summary>
    /// No permission
    /// </summary>
    None = 0,

    /// <summary>
    /// Read permission
    /// </summary>
    Read = 1,

    /// <summary>
    /// Write permission
    /// </summary>
    Write = 2,

    /// <summary>
    /// Read and write permission
    /// </summary>
    ReadWrite = 3,

    /// <summary>
    /// Delete permission (includes read and write permissions)
    /// </summary>
    Delete = 4,

    /// <summary>
    /// Full administrative permission (all permissions)
    /// </summary>
    Admin = 15
}