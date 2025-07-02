namespace Qutora.Shared.DTOs;

public class DocumentShareViewTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public int ViewCount { get; set; }
}

public class DocumentShareTrendDataDto
{
    public List<DocumentShareViewTrendDto> MonthlyViews { get; set; } = new();
    public int TotalViews { get; set; }
    public double AverageViewsPerMonth { get; set; }
} 