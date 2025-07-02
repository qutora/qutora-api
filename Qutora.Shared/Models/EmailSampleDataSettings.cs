namespace Qutora.Shared.Models
{
    public class EmailSampleDataSettings
    {
        public ApprovalRequestSampleData ApprovalRequest { get; set; } = new();
        public ApprovalDecisionSampleData ApprovalDecision { get; set; } = new();
        public DocumentShareNotificationSampleData DocumentShareNotification { get; set; } = new();
    }

    public class ApprovalRequestSampleData
    {
        public string RequesterName { get; set; } 
        public string DocumentName { get; set; }
        public string PolicyName { get; set; } 
        public string RequestReason { get; set; } 
        public string ApproveUrl { get; set; }
        public string RejectUrl { get; set; }
        public string ViewUrl { get; set; }
        public string ShareUrl { get; set; } 
    }

    public class ApprovalDecisionSampleData
    {
        public string ApproverName { get; set; } 
        public string Decision { get; set; } 
        public string Comments { get; set; } 
        public string DocumentName { get; set; } 
        public string ViewUrl { get; set; } 
    }

    public class DocumentShareNotificationSampleData
    {
        public string SenderName { get; set; }
        public string DocumentName { get; set; } 
        public string RecipientName { get; set; }   
        public string Message { get; set; } 
        public string ShareUrl { get; set; } 
        public string ViewUrl { get; set; } 
    }
} 