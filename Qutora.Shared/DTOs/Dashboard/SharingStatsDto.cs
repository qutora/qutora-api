namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// Sharing statistics data transfer object
    /// </summary>
    public class SharingStatsDto
    {
        public int TotalShares { get; set; }
        public int ActiveShares { get; set; }
        public int TotalViews { get; set; }
        public int TodayShares { get; set; }
        public int WeeklyShares { get; set; }
        public int MonthlyShares { get; set; }
        public int TodayViews { get; set; }
        public int WeeklyViews { get; set; }
        public int MonthlyViews { get; set; }
        public decimal AvgViewsPerShare { get; set; }
        public List<TopSharedDocumentDto> TopSharedDocuments { get; set; } = new();
    }
} 