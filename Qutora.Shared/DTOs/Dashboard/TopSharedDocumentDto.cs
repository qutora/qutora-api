namespace Qutora.Shared.DTOs.Dashboard;

/// <summary>
/// Top shared document data transfer object
/// </summary>
public class TopSharedDocumentDto
{
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int ShareCount { get; set; }
    public DateTime LastViewed { get; set; }
}