namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// User dashboard statistics data transfer object
    /// </summary>
    public class UserDashboardStatsDto
    {
        // User Document Statistics
        public int MyDocuments { get; set; }
        public int MyMonthlyUploads { get; set; }
        public decimal MyStorageUsage { get; set; }

        // User Sharing Statistics
        public int MyShares { get; set; }
        public int MyActiveShares { get; set; }
        public int MyTotalViews { get; set; }
        public int MyWeeklyShares { get; set; }
        public decimal MyAvgViews { get; set; }

        // Recent Data
        public List<RecentDocumentDto> RecentDocuments { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }
} 