using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Qutora.Domain.Base;

namespace Qutora.Domain.Entities;

/// <summary>
/// Stores metadata information associated with documents.
/// Uses a flexible schema structure and stores metadata values in JSON format.
/// </summary>
public class Metadata : BaseEntity
{
    /// <summary>
    /// Associated document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Associated document (navigation property)
    /// </summary>
    public virtual Document? Document { get; set; }

    /// <summary>
    /// Metadata schema name (e.g., "invoice", "contract", "project")
    /// Can be used to group similar documents
    /// </summary>
    [MaxLength(100)]
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Metadata schema ID
    /// </summary>
    public Guid? MetadataSchemaId { get; set; }

    /// <summary>
    /// Metadata version, to track schema changes
    /// </summary>
    [MaxLength(20)]
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// Metadata values (stored in JSON format)
    /// </summary>
    public string MetadataJson { get; set; } = "{}";

    /// <summary>
    /// Dictionary for easy access to metadata values
    /// This property is not stored in the database
    /// </summary>
    [NotMapped]
    public Dictionary<string, object> Values
    {
        get
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson) ??
                       new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
        set => MetadataJson = JsonSerializer.Serialize(value);
    }



    /// <summary>
    /// Tags for metadata
    /// Comma-separated tag list
    /// </summary>
    [MaxLength(2000)]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Array access to tags
    /// </summary>
    [NotMapped]
    public string[] TagArray
    {
        get => string.IsNullOrEmpty(Tags)
            ? []
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
        set => Tags = string.Join(',', value);
    }
}