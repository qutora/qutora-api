namespace Qutora.Shared.DTOs;

public class TopActivityDto
{
    public int Rank { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string Trend { get; set; } = "up";
}