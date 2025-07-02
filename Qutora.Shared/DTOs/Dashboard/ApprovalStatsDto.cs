namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// Approval statistics data transfer object
    /// </summary>
    public class ApprovalStatsDto
    {
        public int PendingApprovals { get; set; }
        public int TodayApproved { get; set; }
        public int UrgentApprovals { get; set; }
        public string AvgApprovalTime { get; set; } = string.Empty;
        public int TotalRequestsMonth { get; set; }
        public int TotalRequestsWeek { get; set; }
        public int ApprovedMonth { get; set; }
        public int RejectedMonth { get; set; }
        public decimal ApprovalRate { get; set; }
        public int OverdueApprovals { get; set; }
    }
} 