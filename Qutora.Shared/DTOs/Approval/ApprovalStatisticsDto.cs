namespace Qutora.Shared.DTOs.Approval;

public class ApprovalStatisticsDto
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int ExpiredRequests { get; set; }
    public double AverageApprovalTimeHours { get; set; }
    public double ApprovalRate => TotalRequests > 0 ? (double)ApprovedRequests / TotalRequests * 100 : 0;
    public double RejectionRate => TotalRequests > 0 ? (double)RejectedRequests / TotalRequests * 100 : 0;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public int PendingCount => PendingRequests;
    public int ApprovedCount => ApprovedRequests;
    public int RejectedCount => RejectedRequests;
    public int ExpiredCount => ExpiredRequests;
    public double AverageProcessingTimeHours => AverageApprovalTimeHours;
    public int ActivePoliciesCount { get; set; }
}