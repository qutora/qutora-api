namespace Qutora.Shared.DTOs;

public class AuditStatisticsDto
{
    public int TotalActivities { get; set; }
    public int ActiveUsers { get; set; }
    public int TodayActivities { get; set; }
    public int CriticalEvents { get; set; }
    public List<EventTypeBreakdownDto> EventTypeBreakdown { get; set; } = new();
    public List<EntityTypeBreakdownDto> EntityTypeBreakdown { get; set; } = new();
    public List<DailyActivityDto> DailyActivity { get; set; } = new();
    public int[] HourlyActivity { get; set; } = new int[24];
    public List<TopUserDto> TopUsers { get; set; } = new();
    public List<TopActivityDto> TopActivities { get; set; } = new();
}