using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO used for document updating
/// </summary>
public class UpdateDocumentDto
{
    /// <summary>
    /// Document ID
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// Document name
    /// </summary>
    [Required]
    [StringLength(255)]
    public required string Name { get; set; }

    /// <summary>
    /// Category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Bucket ID
    /// </summary>
    public Guid? BucketId { get; set; }
}