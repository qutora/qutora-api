namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// User sharing statistics data transfer object
    /// </summary>
    public class UserSharingStatsDto
    {
        public int MyShares { get; set; }
        public int MyActiveShares { get; set; }
        public int MyTotalViews { get; set; }
        public int MyTodayShares { get; set; }
        public int MyWeeklyShares { get; set; }
        public int MyMonthlyShares { get; set; }
        public int MyTodayViews { get; set; }
        public int MyWeeklyViews { get; set; }
        public int MyMonthlyViews { get; set; }
        public decimal MyAvgViews { get; set; }
        public List<TopSharedDocumentDto> MyTopSharedDocuments { get; set; } = new();
    }
} 