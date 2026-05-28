namespace Vivu.Application.DTOs.Responses.BlogReports;

public class ReviewBlogReportResponse
{
    public Guid Id { get; set; }
    public Guid BlogId { get; set; }
    public string BlogTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public DateTime UpdatedAt { get; set; }
}
