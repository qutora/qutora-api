namespace Qutora.Shared.DTOs.DocumentOrchestration;

/// <summary>
/// Document delete request
/// </summary>
public class DocumentDeleteRequest
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
}