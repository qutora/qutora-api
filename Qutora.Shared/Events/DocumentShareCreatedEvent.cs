namespace Qutora.Shared.Events;

public class DocumentShareCreatedEvent
{
    public Guid ShareId { get; set; }
    public Guid DocumentId { get; set; }
    public string ShareCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string[] NotificationEmails { get; set; } = Array.Empty<string>();
    public bool IsDirectShare { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
} 