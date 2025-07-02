using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;

namespace Qutora.Domain.Entities;

/// <summary>
/// Category entity used to group documents.
/// </summary>
public class Category : BaseEntity
{
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Is direct access endpoint allowed for this category?
    /// If this flag is true, files in this category can be accessed via direct URL
    /// </summary>
    public bool AllowDirectAccess { get; set; } = false;
    
    public Guid? ParentCategoryId { get; set; }
    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}