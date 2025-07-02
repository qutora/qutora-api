namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// User statistics data transfer object
    /// </summary>
    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersMonth { get; set; }
        public int NewUsersWeek { get; set; }
        public int NewUsersToday { get; set; }
        public string AvgSessionTime { get; set; } = string.Empty;
        public int DailyActiveUsers { get; set; }
        public int WeeklyActiveUsers { get; set; }
        public int MonthlyActiveUsers { get; set; }
    }
} 