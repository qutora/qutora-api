using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for document creation
/// </summary>
public class DocumentCreateDto
{
    /// <summary>
    /// File to be uploaded
    /// </summary>
    [Required(ErrorMessage = "File must be selected")]
    public IFormFile File { get; set; }

    /// <summary>
    /// Document name
    /// </summary>
    [Required(ErrorMessage = "Document name is required")]
    [StringLength(255, ErrorMessage = "Document name can be at most 255 characters")]
    public string Name { get; set; }

    /// <summary>
    /// Document description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Document category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Metadata schema ID
    /// </summary>
    public Guid? MetadataSchemaId { get; set; }

    /// <summary>
    /// Metadata values (in JSON format)
    /// </summary>
    public string? MetadataValues { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Storage provider ID
    /// </summary>
    public Guid? StorageProviderId { get; set; }

    /// <summary>
    /// Document ID (for version creation)
    /// </summary>
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// Bucket ID
    /// </summary>
    public Guid? BucketId { get; set; }

    /// <summary>
    /// Share options
    /// </summary>
    public DocumentUploadShareOptionsDto? ShareOptions { get; set; }
}