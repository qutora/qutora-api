namespace Qutora.Shared.Events;

/// <summary>
/// Event fired when an approval request is created
/// </summary>
public class ApprovalRequestCreatedEvent
{
    public Guid ApprovalRequestId { get; set; }
    public Guid DocumentShareId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string RequesterUserId { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string ShareCode { get; set; } = string.Empty;
    public string RequestReason { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 