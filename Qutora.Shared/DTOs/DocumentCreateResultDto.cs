namespace Qutora.Shared.DTOs;

public class DocumentCreateResultDto
{
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;

    public DocumentShareDto? Share { get; set; }
}