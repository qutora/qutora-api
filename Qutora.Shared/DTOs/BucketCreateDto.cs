using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs;

public class BucketCreateDto
{
    [Required(ErrorMessage = "Provider ID is required")]
    public required string ProviderId { get; set; }

    [Required(ErrorMessage = "Bucket/Folder path is required")]
    [RegularExpression(@"^[a-z0-9][a-z0-9\-]+$",
        ErrorMessage = "Bucket/folder path must consist of lowercase letters, numbers and hyphens, starting with a letter or number")]
    public required string BucketPath { get; set; }

    /// <summary>
    /// Bucket description
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = "";

    /// <summary>
    /// Whether the bucket is public or not
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Is direct access endpoint allowed for this bucket?
    /// If this flag is true, files in this bucket can be accessed via direct URL
    /// </summary>
    public bool AllowDirectAccess { get; set; } = false;
}