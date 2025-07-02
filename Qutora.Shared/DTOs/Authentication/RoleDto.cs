namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// DTO containing role information
/// </summary>
public class RoleDto
{
    /// <summary>
    /// Role ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Role name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    public string Description { get; set; } = string.Empty;
}