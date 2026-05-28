namespace Vivu.Application.DTOs.Responses.Statistics;

public class ContentStatsDto
{
    public int TotalBlogs { get; set; }
    public int PendingBlogs { get; set; }
    public int PublishedBlogs { get; set; }
    public int PendingBlogReports { get; set; }
    public int TotalTrips { get; set; }
}
