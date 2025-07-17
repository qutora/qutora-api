namespace Qutora.Shared.Models
{
    public class EmailSampleDataSettings
    {
        public ApprovalRequestSampleData ApprovalRequest { get; set; } = new();
        public ApprovalDecisionSampleData ApprovalDecision { get; set; } = new();
        public DocumentShareNotificationSampleData DocumentShareNotification { get; set; } = new();
    }
} 