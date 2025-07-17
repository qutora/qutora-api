namespace Qutora.Shared.Enums;

/// <summary>
/// API Key permission levels
/// </summary>
public enum ApiKeyPermission
{
    /// <summary>
    /// Read-only permission
    /// </summary>
    ReadOnly = 0,

    /// <summary>
    /// Read and write permission
    /// </summary>
    ReadWrite = 1,

    /// <summary>
    /// Full access permission
    /// </summary>
    FullAccess = 2
}