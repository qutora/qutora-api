namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for category creation request
/// </summary>
public class CreateCategoryDto
{
    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Parent category ID
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>
    /// Is direct access endpoint allowed for this category?
    /// If this flag is true, files in this category can be accessed via direct URL
    /// </summary>
    public bool AllowDirectAccess { get; set; } = false;
}