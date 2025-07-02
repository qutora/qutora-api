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

    /// <summary>
    /// Top shared document data transfer object
    /// </summary>
    public class TopSharedDocumentDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public string DocumentName { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int ShareCount { get; set; }
        public DateTime LastViewed { get; set; }
    }
} 