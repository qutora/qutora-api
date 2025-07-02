namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for document creation request
/// </summary>
public class CreateDocumentDto
{
    /// <summary>
    /// Document name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Storage provider ID
    /// </summary>
    public Guid StorageProviderId { get; set; }
}