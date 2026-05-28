namespace Vivu.Application.DTOs.Responses.Statistics;

public class AdminDashboardDto
{
    public UserStatsDto Users { get; set; } = new();
    public RevenueStatsDto Revenue { get; set; } = new();
    public ContentStatsDto Content { get; set; } = new();
    public LocationStatsDto Locations { get; set; } = new();
}
