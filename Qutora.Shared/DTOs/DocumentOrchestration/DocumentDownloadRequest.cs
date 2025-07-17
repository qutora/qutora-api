namespace Qutora.Shared.DTOs.DocumentOrchestration;

/// <summary>
/// Document download request
/// </summary>
public class DocumentDownloadRequest
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
}