using Qutora.Shared.Enums;

namespace Qutora.Shared.Events;

/// <summary>
/// Event fired when an approval decision is made
/// </summary>
public class ApprovalDecisionMadeEvent
{
    public Guid ApprovalRequestId { get; set; }
    public string RequesterUserId { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string ShareCode { get; set; } = string.Empty;
    public ApprovalStatus Decision { get; set; }
    public string DecisionComment { get; set; } = string.Empty;
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 