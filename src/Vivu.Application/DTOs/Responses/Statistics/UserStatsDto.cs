namespace Vivu.Application.DTOs.Responses.Statistics;

public class UserStatsDto
{
    public int TotalUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int ExpiringSubscriptions { get; set; }
}
