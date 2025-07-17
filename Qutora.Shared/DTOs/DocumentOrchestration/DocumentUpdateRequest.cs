namespace Qutora.Shared.DTOs.DocumentOrchestration;

/// <summary>
/// Document update request
/// </summary>
public class DocumentUpdateRequest
{
    public Guid Id { get; set; }
    public UpdateDocumentDto UpdateDto { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
}