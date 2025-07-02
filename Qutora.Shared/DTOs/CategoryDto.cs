namespace Qutora.Shared.DTOs;

/// <summary>
/// Data transfer object for category information
/// </summary>
public class CategoryDto
{
    /// <summary>
    /// Category ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Parent category ID
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>
    /// Parent category name
    /// </summary>
    public string? ParentCategoryName { get; set; }

    /// <summary>
    /// Number of documents in the category
    /// </summary>
    public int DocumentCount { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Category path
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Is direct access endpoint allowed for this category?
    /// If this flag is true, files in this category can be accessed via direct URL
    /// </summary>
    public bool AllowDirectAccess { get; set; } = false;
}