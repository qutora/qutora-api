using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Qutora.Domain.Base;

namespace Qutora.Domain.Entities;

/// <summary>
/// Schema definition for metadata
/// </summary>
public class MetadataSchema : BaseEntity
{
    /// <summary>
    /// Schema name (must be unique)
    /// </summary>
    [MaxLength(450)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Schema description
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Schema version
    /// </summary>
    [MaxLength(20)]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Is schema active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Which file types this schema applies to (e.g., ".pdf", ".docx", "image/*")
    /// </summary>
    [MaxLength(500)]
    public string FileTypes { get; set; } = string.Empty;

    /// <summary>
    /// Which category this schema applies to (category ID) - required
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Schema fields in JSON format (field name, type, required, etc.)
    /// </summary>
    public string SchemaDefinitionJson { get; set; } = "[]";

    /// <summary>
    /// Array access to schema definition
    /// </summary>
    [NotMapped]
    public List<MetadataSchemaField> Fields
    {
        get
        {
            try
            {
                return JsonSerializer.Deserialize<List<MetadataSchemaField>>(SchemaDefinitionJson) ??
                       [];
            }
            catch
            {
                return [];
            }
        }
        set => SchemaDefinitionJson = JsonSerializer.Serialize(value);
    }



    /// <summary>
    /// Metadata records using this schema
    /// </summary>
    public virtual ICollection<Metadata> MetadataRecords { get; set; } = new List<Metadata>();

    /// <summary>
    /// Array access to file types
    /// </summary>
    [NotMapped]
    public string[] FileTypeArray
    {
        get => string.IsNullOrEmpty(FileTypes)
            ? []
            : FileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);
        set => FileTypes = string.Join(',', value);
    }
}