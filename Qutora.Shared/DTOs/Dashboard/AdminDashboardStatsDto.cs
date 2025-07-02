namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// Admin dashboard statistics data transfer object
    /// </summary>
    public class AdminDashboardStatsDto
    {
        // Document Statistics
        public int TotalDocuments { get; set; }
        public int MonthlyUploads { get; set; }
        public decimal StorageUsage { get; set; }

        // User Statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersMonth { get; set; }
        public string AvgSessionTime { get; set; } = string.Empty;
        public int DailyActiveUsers { get; set; }

        // Approval Statistics
        public int PendingApprovals { get; set; }
        public int TodayApproved { get; set; }
        public int UrgentApprovals { get; set; }
        public string AvgApprovalTime { get; set; } = string.Empty;

        // API Key Statistics
        public int TotalApiKeys { get; set; }
        public int ActiveApiKeys { get; set; }
        public int DailyApiCalls { get; set; }
        public decimal ApiErrorRate { get; set; }
    }
} 