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

public class EventTypeBreakdownDto
{
    public string EventType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class EntityTypeBreakdownDto
{
    public string EntityType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DailyActivityDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class TopUserDto
{
    public string UserId { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopActivityDto
{
    public int Rank { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string Trend { get; set; } = "up";
} 