namespace Qutora.Shared.DTOs;

public class DocumentShareTrendDataDto
{
    public List<DocumentShareViewTrendDto> MonthlyViews { get; set; } = new();
    public int TotalViews { get; set; }
    public double AverageViewsPerMonth { get; set; }
}