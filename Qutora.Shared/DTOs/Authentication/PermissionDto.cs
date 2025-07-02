namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// DTO class that carries permission information
/// </summary>
public class PermissionDto
{
    /// <summary>
    /// Permission name (e.g., Document.Read)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Permission display name (e.g., Document View)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Permission description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Permission category (e.g., Document Management)
    /// </summary>
    public string Category { get; set; } = string.Empty;
}